"""베네치아 시간대 배경 음악 (2026-10-27): 원래 배경 음악 naru.ogg의 선율·화음·빠르기를 그대로 두고 악기만 베네치아풍으로 바꾼다.
    음표: venice_naru_notes.csv — naru.ogg를 basic-pitch로 받아 적은 표 (북유럽 브랜치 Tools/Audio/north_naru_notes.csv와 같은 자료)
    악기: 아침 만돌린 + 기타 / 낮 만돌린 트레몰로 + 아코디언 화음 + 탬버린 / 저녁 아코디언 선율 + 기타 / 밤 부드러운 아코디언 + 하프 (조금 느리게)
    사용: python Tools/ArtGen/venice_arrange.py   → Assets/PocketCaravan/Resources/Venice/Audio/venice_{morning,day,evening,night}.wav
    numpy만 쓴다(필터·잔향을 직접 계산)."""
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
    for n in notes:                                 # 같은 음이 겹치면 하나만
        if any(c[2] == n[2] and c[0] <= n[0] < c[0] + c[1] for c in clean[-12:]):
            continue
        clean.append(n)
    return clean


def hz(p):
    return 440 * 2 ** ((p - 69) / 12)


rng = np.random.default_rng(11)


def env(n, a, rel):
    e = np.ones(n); ai = min(n, int(a * R)); ri = min(n, int(rel * R))
    if ai: e[:ai] = np.linspace(0, 1, ai)
    if ri: e[n - ri:] *= np.linspace(1, 0, ri)
    return e


def pluck(f, dur, bright=.5, decay=.996):
    """Karplus-Strong (한 주기씩 묶어 빠르게)"""
    n = int((min(dur, 1.5) + .7) * R); P = max(2, int(R / f))
    b = rng.uniform(-1, 1, P)
    b = bright * b + (1 - bright) * np.concatenate([[b[-1]], b[:-1]])
    out = []
    for _ in range(n // P + 1):
        out.append(b.copy()); b = decay * .5 * (b + np.roll(b, -1))
    x = np.concatenate(out)[:n]
    return x * np.exp(-np.arange(n) / R * 1.4) * .6


def mandolin(f, dur, tremolo=False):
    """만돌린: 겹줄을 살짝 어긋나게 뜯는다. 트레몰로면 같은 음을 빠르게 거듭"""
    if not tremolo:
        a = pluck(f, dur, .75); b = pluck(f * 1.003, dur, .75)
        return (a + b) * .5
    n = int((dur + .5) * R); x = np.zeros(n); t = 0.0; i = 0
    while t < max(dur, .12):
        c = pluck(f * (1.002 if i % 2 else 1.0), .14, .8, .99) * (.75 if i % 2 else 1.0)
        at = int(t * R); x[at:at + len(c)] += c[:n - at]; t += .07; i += 1
    return x * .6


def guitar(f, dur):
    return pluck(f, dur, .35, .997) * .9


def harp(f, dur):
    return pluck(f, dur, .25, .998)


def accordion(f, dur, soft=1.0):
    """아코디언: 살짝 어긋난 두 리드 + 배음 + 느린 떨림, 부드러운 시작"""
    n = int((dur + .2) * R); t = np.arange(n) / R
    v = 1 + .003 * np.sin(2 * np.pi * 5 * t)
    x = np.zeros(n)
    for k, a in ((1, 1.0), (2, .5 * soft), (3, .3 * soft), (4, .15 * soft), (5, .08 * soft)):
        x += a * (np.sin(2 * np.pi * f * k * t * v) + .8 * np.sin(2 * np.pi * f * k * 1.004 * t * v))
    return x * env(n, .05, .15) * .22


def tambourine(strength):
    n = int(.18 * R); t = np.arange(n) / R
    jingle = rng.standard_normal(n); jingle = jingle - np.convolve(jingle, np.ones(3) / 3, mode="same")
    return strength * jingle * np.exp(-t / .045) * .5


def lowpass(x, fc):
    a = np.exp(-2 * np.pi * fc / R)                 # 한 극 저역 통과 (두 번 거친다)
    for _ in range(2):
        y = np.empty_like(x); acc = 0.0
        # 빠르게: 누적 필터를 블록 대신 벡터식으로 쓰기 어려워 직접 돈다 (곡 길이 40초 안팎이라 충분)
        for i in range(len(x)):
            acc = (1 - a) * x[i] + a * acc; y[i] = acc
        x = y
    return x


def reverb(x, decay=1.5, wet=.22):
    n = int(decay * R); t = np.arange(n) / R
    ir = rng.normal(0, 1, n) * np.exp(-t * 6.9 / decay)
    ir = np.convolve(ir, np.ones(6) / 6, mode="same"); ir /= np.sqrt(np.sum(ir ** 2))
    size = 1 << int(np.ceil(np.log2(len(x) + n)))
    wetx = np.fft.irfft(np.fft.rfft(x, size) * np.fft.rfft(ir, size), size)[:len(x)]
    return x * (1 - wet) + wetx * wet * 1.4


def add(buf, sig, at):
    end = min(len(buf), at + len(sig)); buf[at:end] += sig[:end - at]


def render(slot, notes):
    tempo = 1.12 if slot == 3 else 1.0
    st = S * tempo; L = int(BARS * 16 * st * R); tail = int(3 * R)
    mel = np.zeros(L + tail); mid = np.zeros(L + tail); bas = np.zeros(L + tail); per = np.zeros(L + tail)
    for k, d, p, v in notes:
        f = hz(p); dur = d * st; at = int(k * st * R); vel = (v / 100) ** 1.2
        if p >= 67:                                 # 선율
            c = (mandolin(f, dur) if slot == 0 else mandolin(f, dur, tremolo=dur > .3) if slot == 1
                 else accordion(f, dur) * 1.6 if slot == 2 else accordion(f, dur, .4) * 1.3)
            add(mel, c * vel, at)
        elif p >= 52:                               # 가운데 화음
            c = guitar(f, dur) if slot in (0, 2) else accordion(f, dur, .6) * .7 if slot == 1 else harp(f, dur)
            add(mid, c * vel, at)
        else:                                       # 베이스
            add(bas, guitar(f, dur) * vel, at)
    if slot in (0, 1):                              # 탬버린 (아침은 가볍게 한 번, 낮은 두 번)
        for b in range(BARS):
            for beat, sv in (((0, .5), (2, .35)) if slot == 1 else ((2, .25),)):
                add(per, tambourine(sv), int((b * 16 + beat * 4) * st * R))
                if slot == 1: add(per, tambourine(sv * .5), int((b * 16 + beat * 4 + 2) * st * R))
    g = {0: (.6, .42, .4, .25), 1: (.55, .38, .42, .3), 2: (.5, .34, .4, 0), 3: (.55, .3, .25, 0)}[slot]
    mix = mel * g[0] + mid * g[1] + bas * g[2] + per * g[3]
    mix = lowpass(mix, 6500 if slot < 2 else 4500)
    mix = reverb(mix, 1.3 if slot < 2 else 2.0, .2 if slot < 2 else .3)
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
