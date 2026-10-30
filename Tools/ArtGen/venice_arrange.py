"""베네치아 시간대 배경 음악. 북유럽 편곡(Tools/Audio/north_arrange.py)과 같은 짜임으로, 악기만 베네치아(이탈리아)풍으로 바꾼다.
    음표: venice_naru_notes.csv — naru.ogg를 basic-pitch로 받아 적은 표 (북유럽 north_naru_notes.csv와 같은 자료)
    짜임(북유럽과 같음): 받아 적은 음을 그대로 쓰고 높이에 따라 선율(67↑)·가운데(52~66)·베이스로 나눠 다른 악기로 연주, 아침·낮만 북.
    악기 (2026-10-29, 종소리·오르골은 쓰지 않는다):
      아침 만돌린 선율 + 기타 + 기타 베이스, 탬버린 한 번
      낮   바이올린 선율 + 만돌린 + 첼로, 북 + 탬버린
      저녁 부드러운 바이올린 + 기타 + 첼로
      밤   리코더 선율 + 하프 + 기타 베이스 (조금 느리게)
    사용: python Tools/ArtGen/venice_arrange.py   → Assets/PocketCaravan/Resources/Venice/Audio/venice_{morning,day,evening,night}.wav
    numpy만 쓴다(북유럽은 scipy를 쓰지만 같은 계산을 numpy로 옮겼다)."""
import csv, os, wave
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "..", "Assets/PocketCaravan/Resources/Venice/Audio")
R = 32000
S = 0.1875          # 16분음표 한 칸 (원곡 빠르기)
BARS = 12


def load_notes():
    rows = list(csv.reader(open(os.path.join(HERE, "venice_naru_notes.csv"), encoding="utf-8")))[1:]
    notes = []
    for r in rows:
        k = round(float(r[0]) / S); d = max(1, round((float(r[1]) - float(r[0])) / S)); p = int(r[2]); v = float(r[3])
        if 0 <= k < BARS * 16 and v >= 50:          # 약한 것은 배음 잡음
            notes.append([k, d, p, v])
    notes.sort(); clean = []
    for n in notes:                                 # 같은 음 겹침 정리
        if any(c[2] == n[2] and c[0] <= n[0] < c[0] + c[1] for c in clean[-12:]):
            continue
        clean.append(n)
    return clean


def hz(p):
    return 440 * 2 ** ((p - 69) / 12)


def env(n, a, rel):
    e = np.ones(n); ai = min(n, int(a * R)); ri = min(n, int(rel * R))
    if ai: e[:ai] = np.linspace(0, 1, ai)
    if ri: e[n - ri:] *= np.linspace(1, 0, ri)
    return e


def bowed(f, dur, bright=1.0, vibd=.005, noise=.015, seed=0):
    """바이올린·첼로: 톱니파 배음 + 늦게 들어오는 비브라토 + 활 잡음"""
    n = int((dur + .25) * R); t = np.arange(n) / R
    vib = 1 + vibd * np.sin(2 * np.pi * 5.5 * t) * np.clip((t - .15) / .3, 0, 1)
    ph = np.cumsum(2 * np.pi * f * vib / R); v = np.zeros(n)
    for h in range(1, 12):
        if f * h > 9000:
            break
        v += np.sin(ph * h) / h ** (1.0 + .6 / bright)
    v += np.random.default_rng(seed).normal(0, noise, n)
    return v * env(n, .06, .25) * .5


def recorder(f, dur, seed=0):
    n = int((dur + .2) * R); t = np.arange(n) / R
    vib = 1 + .005 * np.sin(2 * np.pi * 5 * t) * np.clip((t - .12) / .3, 0, 1)
    ph = np.cumsum(2 * np.pi * f * vib / R)
    v = np.sin(ph) + .3 * np.sin(2 * ph) + .1 * np.sin(3 * ph)
    breath = np.random.default_rng(seed).normal(0, .03, n)
    breath = np.convolve(breath, np.ones(8) / 8, mode="same")
    return (v + breath) * env(n, .04, .18) * .5


def pluck(f, dur, decay=.996, seed=0, bright=.5):
    """기타·만돌린·하프 (Karplus-Strong)"""
    n = int((min(dur, 1.5) + .9) * R); P = max(2, int(R / f))
    b = np.random.default_rng(seed).uniform(-1, 1, P)
    b = bright * b + (1 - bright) * np.roll(b, 1)
    out = []
    for _ in range(n // P + 1):
        out.append(b.copy()); b = decay * .5 * (b + np.roll(b, -1))
    return np.concatenate(out)[:n] * np.exp(-np.arange(n) / R * 1.2) * .6


def mandolin(f, dur, seed=0):
    """겹줄: 살짝 어긋난 두 줄을 같이 뜯는다"""
    a = pluck(f, dur, .995, seed, .75); b = pluck(f * 1.002, dur, .995, seed + 7, .75)
    return (a + b) * .5


def drum(strength, seed=3):
    n = int(.7 * R); t = np.arange(n) / R; f = 60 + 55 * np.exp(-t * 18); ph = np.cumsum(2 * np.pi * f / R)
    return strength * (np.sin(ph) * np.exp(-t * 6) * .9 + np.random.default_rng(seed).uniform(-1, 1, n) * np.exp(-t * 45) * .2)


def tambourine(strength, seed=5):
    n = int(.14 * R); t = np.arange(n) / R
    j = np.random.default_rng(seed).standard_normal(n); j = j - np.convolve(j, np.ones(3) / 3, mode="same")
    return strength * j * np.exp(-t / .035) * .45


def lowpass(x, fc):
    """2차 버터워스와 같은 크기 응답을 주파수 영역에서 곱한다"""
    X = np.fft.rfft(x); f = np.fft.rfftfreq(len(x), 1 / R)
    return np.fft.irfft(X / np.sqrt(1 + (f / fc) ** 4), len(x))


def reverb(x, decay=1.6, wet=.22, seed=1):
    n = int(decay * R); t = np.arange(n) / R
    ir = np.random.default_rng(seed).normal(0, 1, n) * np.exp(-t * 6.9 / decay); ir = lowpass(ir, 5000); ir /= np.sqrt(np.sum(ir ** 2))
    size = 1 << int(np.ceil(np.log2(len(x) + n)))
    wetx = np.fft.irfft(np.fft.rfft(x, size) * np.fft.rfft(ir, size), size)[:len(x)]
    return x * (1 - wet) + wetx * wet * 1.4


def add(buf, sig, at):
    end = min(len(buf), at + len(sig)); buf[at:end] += sig[:end - at]


def render(slot, notes):
    st = S * (1.08 if slot == 3 else 1.0)           # 밤은 조금 느리게 (북유럽 1.12보다 덜)
    L = int(BARS * 16 * st * R); tail = int(3 * R)
    mel, mid, bas, per = (np.zeros(L + tail) for _ in range(4))
    for i, (k, d, p, v) in enumerate(notes):
        f = hz(p); dur = d * st; at = int(k * st * R); vel = (v / 100) ** 1.2
        if p >= 67:
            c = (mandolin(f, dur, i) if slot == 0 else bowed(f, dur, 1.0, .006, .012, i) if slot == 1
                 else bowed(f, dur, .7, .007, .01, i) * .9 if slot == 2 else recorder(f, dur, i))
            add(mel, c * vel, at)
        elif p >= 52:
            c = (pluck(f, dur, .997, i, .5) if slot in (0, 2) else mandolin(f, dur, i) * .9 if slot == 1
                 else pluck(f, dur, .998, i, .28))
            add(mid, c * vel, at)
        else:
            c = bowed(f, dur, .5, .003, .008, i) * .7 if slot in (1, 2) else pluck(f, dur, .998, i, .3)
            add(bas, c * vel, at)
    if slot in (0, 1):
        for b in range(BARS):
            if slot == 1:
                add(per, drum(1.0, b), int((b * 16) * st * R)); add(per, drum(.55, b + 50), int((b * 16 + 8) * st * R))
                for beat in (4, 12):
                    add(per, tambourine(.8, b * 2 + beat), int((b * 16 + beat) * st * R))
            else:
                add(per, drum(.5, b), int((b * 16) * st * R))
                add(per, tambourine(.5, b), int((b * 16 + 8) * st * R))
    g = {0: (.55, .42, .38, .25), 1: (.5, .42, .42, .35), 2: (.5, .32, .4, 0), 3: (.6, .32, .25, 0)}[slot]
    mix = mel * g[0] + mid * g[1] + bas * g[2] + per * g[3]
    mix = lowpass(mix, 7000 if slot < 2 else 5500)
    mix = reverb(mix, 1.4 if slot < 2 else 1.9, .2 if slot < 2 else .26)
    loop = mix[:L].copy(); loop[:tail] += mix[L:L + tail]        # 꼬리를 앞으로 감아 이음매 없이 반복
    loop = loop / np.sqrt(np.mean(loop ** 2)) * .085
    return np.clip(loop, -.95, .95), L / R


def write(name, x):
    os.makedirs(OUT, exist_ok=True)
    data = (x * 32767).astype(np.int16)
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(R); w.writeframes(data.tobytes())


if __name__ == "__main__":
    notes = load_notes()
    print("notes", len(notes))
    for slot, name in enumerate(("morning", "day", "evening", "night")):
        x, sec = render(slot, notes)
        write("venice_" + name, x)
        print("venice_" + name, round(sec, 1), "s, peak", round(float(np.max(np.abs(x))), 2))
