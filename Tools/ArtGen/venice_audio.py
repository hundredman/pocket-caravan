"""베네치아 음악·소리 합성 (2026-10-26, 제안). 새로 지은 짧은 곡과 효과음을 코드로 만든다(외부 음원 없음).
    사용: python Tools/ArtGen/venice_audio.py
    출력: Assets/PocketCaravan/Resources/Venice/Audio/
      (시간대 음악 venice_morning/day/evening/night.wav는 venice_arrange.py가 만든다. 아래 곡 함수는 이전 시안)
      venice_day.wav   낮: 3/4 왈츠, 뜯는 현(만돌린풍, Karplus-Strong) 선율 + 기타풍 반주 (반복 재생)
      venice_night.wav 밤: 느린 바르카롤레, 아코디언풍 부드러운 화음 + 선율
      bell.wav 종탑 종 / gull.wav 갈매기 / water.wav 물결(반복)
      venice_morning.wav 아침 피리 뱃노래 / venice_evening.wav 저녁 만돌린 세레나데 / crowd.wav 장터 웅성거림 / voice_0~5.wav NPC 말소리 (2026-10-27)
    VeniceSound가 Resources.Load로 읽는다."""
import os, wave
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets/PocketCaravan/Resources/Venice/Audio")
SR = 22050
rng = np.random.default_rng(7)


def write(name, x):
    os.makedirs(OUT, exist_ok=True)
    x = np.clip(x / (np.max(np.abs(x)) + 1e-9) * 0.85, -1, 1)
    data = (x * 32767).astype(np.int16)
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(data.tobytes())


def hz(note):
    """'A4' 같은 이름 → 진동수"""
    names = {"C": -9, "D": -7, "E": -5, "F": -4, "G": -2, "A": 0, "B": 2}
    n = names[note[0]]
    i = 1
    if note[1] in "#b":
        n += 1 if note[1] == "#" else -1
        i = 2
    octave = int(note[i:])
    return 440.0 * 2 ** ((n + (octave - 4) * 12) / 12)


def pluck(f, dur, bright=0.5):
    """Karplus-Strong 뜯는 현"""
    n = int(SR * dur)
    p = max(2, int(SR / f))
    buf = rng.uniform(-1, 1, p) * (0.6 + 0.4 * bright)
    out = np.zeros(n)
    for i in range(n):
        out[i] = buf[i % p]
        buf[i % p] = 0.5 * (buf[i % p] + buf[(i + 1) % p]) * 0.996
    return out


def reed(f, dur, vib=5.0):
    """아코디언풍: 배음 몇 개 + 살짝 어긋난 두 리드 + 느린 비브라토, 부드러운 시작·끝"""
    t = np.arange(int(SR * dur)) / SR
    v = 1 + 0.004 * np.sin(2 * np.pi * vib * t)
    x = np.zeros_like(t)
    for k, a in ((1, 1.0), (2, .45), (3, .25), (4, .12)):
        x += a * (np.sin(2 * np.pi * f * k * t * v) + .7 * np.sin(2 * np.pi * f * k * 1.003 * t * v))
    env = np.minimum(1, t / .08) * np.minimum(1, (dur - t) / .15)
    return x * env


def mix(track, sig, start):
    i = int(start * SR)
    end = min(len(track), i + len(sig))
    track[i:end] += sig[:end - i]


def day():
    bpm, bars = 132, 16
    beat = 60 / bpm
    total = bars * 3 * beat
    tr = np.zeros(int(SR * (total + 1)))
    # 화음 진행 (D단조 → F장조 바르카롤레 느낌): 마디마다
    chords = [("D3", "F3", "A3"), ("A2", "E3", "C#4"), ("D3", "F3", "A3"), ("G2", "Bb3", "D4"),
              ("F3", "A3", "C4"), ("C3", "E3", "G3"), ("F3", "A3", "C4"), ("A2", "E3", "C#4")] * 2
    melody = ["A4", "D5", "F5", "E5", "D5", "C#5", "D5", "A4", "F4", "G4", "Bb4", "D5",
              "C5", "A4", "F4", "E4", "G4", "C5", "A4", "F4", "C5", "E5", "C#5", "A4"] * 2
    for b in range(bars):
        t0 = b * 3 * beat
        root, *upper = chords[b % len(chords)]
        mix(tr, .7 * pluck(hz(root), 3 * beat, .3), t0)                    # 쿵
        for k in (1, 2):                                                    # 짝 짝
            for u in upper:
                mix(tr, .3 * pluck(hz(u), beat, .4), t0 + k * beat)
        for k in range(3):                                                  # 선율 (만돌린 트레몰로: 같은 음을 빠르게 두 번)
            n = melody[(b * 3 + k) % len(melody)]
            mix(tr, .55 * pluck(hz(n), beat * .55, .9), t0 + k * beat)
            mix(tr, .35 * pluck(hz(n), beat * .45, .9), t0 + k * beat + beat * .5)
    return tr[:int(SR * total)]


def night():
    bpm, bars = 72, 12
    beat = 60 / bpm
    total = bars * 3 * beat
    tr = np.zeros(int(SR * (total + 1)))
    chords = [("D3", "F3", "A3"), ("Bb2", "D3", "F3"), ("G2", "Bb2", "D3"), ("A2", "C#3", "E3")] * 3
    melody = ["F4", "E4", "D4", "A4", "G4", "F4", "D4", "E4", "F4", "E4", "C#4", "E4"] * 3
    for b in range(bars):
        t0 = b * 3 * beat
        for u in chords[b % len(chords)]:
            mix(tr, .18 * reed(hz(u), 3 * beat, 4.5), t0)
        mix(tr, .35 * reed(hz(melody[b % len(melody)]), 2 * beat, 5.5), t0)
        mix(tr, .25 * reed(hz(melody[(b + 3) % len(melody)]), beat, 5.5), t0 + 2 * beat)
        mix(tr, .25 * pluck(hz(chords[b % len(chords)][0]) / 2, 2 * beat, .2), t0)
    return tr[:int(SR * total)]


def flute(f, dur, vib=5.0):
    """나무 피리풍: 기본음 + 약한 2배음 + 숨소리, 늦게 들어오는 비브라토"""
    t = np.arange(int(SR * dur)) / SR
    v = 1 + 0.006 * np.sin(2 * np.pi * vib * t) * np.minimum(1, t / .3)
    x = np.sin(2 * np.pi * f * t * v) + .18 * np.sin(4 * np.pi * f * t * v)
    breath = np.convolve(rng.standard_normal(len(t)), np.ones(8) / 8, mode="same") * .06
    env = np.minimum(1, t / .05) * np.minimum(1, (dur - t) / .12)
    return (x + breath) * env


def morning():
    """아침: 6/8 가벼운 뱃노래, 피리 선율 + 뜯는 현 분산화음 (F장조)"""
    bpm, bars = 100, 12
    eighth = 60 / bpm / 2
    total = bars * 6 * eighth
    tr = np.zeros(int(SR * (total + 1)))
    chords = [("F3", "A3", "C4"), ("C3", "E3", "G3"), ("D3", "F3", "A3"), ("Bb2", "D3", "F3")] * 3
    melody = [("C5", 3), ("A4", 1), ("Bb4", 2), ("C5", 2), ("D5", 1), ("C5", 3), ("G4", 3), ("A4", 1), ("Bb4", 2),
              ("A4", 3), ("F4", 3), ("D5", 2), ("C5", 1), ("A4", 3), ("Bb4", 3), ("G4", 3), ("F4", 6)]
    for b in range(bars):
        t0 = b * 6 * eighth
        ch = chords[b % len(chords)]
        mix(tr, .5 * pluck(hz(ch[0]) / 2, 6 * eighth, .3), t0)
        for k in range(6):
            mix(tr, .22 * pluck(hz(ch[(0, 1, 2, 1, 2, 1)[k]]), eighth * 1.5, .5), t0 + k * eighth)
    t = 0.0
    while t < total - .1:
        for n, d in melody:
            if t >= total - .1:
                break
            mix(tr, .42 * flute(hz(n), d * eighth * .95), t)
            t += d * eighth
    return tr[:int(SR * total)]


def evening():
    """저녁: 느린 세레나데, 만돌린 트레몰로 선율 + 부드러운 리드 화음 (D단조)"""
    bpm, bars = 80, 12
    beat = 60 / bpm
    total = bars * 3 * beat
    tr = np.zeros(int(SR * (total + 1)))
    chords = [("D3", "F3", "A3"), ("G2", "Bb2", "D3"), ("C3", "E3", "G3"), ("F2", "A2", "C3")] * 3
    melody = ["A4", "Bb4", "A4", "G4", "F4", "G4", "E4", "F4", "D4", "E4", "C#4", "D4"]
    for b in range(bars):
        t0 = b * 3 * beat
        for u in chords[b % len(chords)]:
            mix(tr, .12 * reed(hz(u), 3 * beat, 4.0), t0)
        n = hz(melody[b % len(melody)])
        k = 0.0
        while k < 2.6 * beat:                       # 트레몰로: 같은 음을 빠르게 거듭 뜯는다
            mix(tr, .3 * pluck(n, .16, .8), t0 + k)
            k += .09
    return tr[:int(SR * total)]


def crowd():
    """장터 웅성거림 6초 반복: 낮게 웅얼대는 말소리 여러 겹 + 그릇 부딪는 소리"""
    n = int(SR * 6)
    t = np.arange(n) / SR
    x = np.zeros(n)
    for _ in range(14):
        f0 = rng.uniform(140, 260)
        start = rng.uniform(0, 5.4)
        dur = rng.uniform(.5, 1.4)
        seg = (t >= start) & (t < start + dur)
        tt = t[seg] - start
        f = f0 * (1 + .08 * np.sin(2 * np.pi * rng.uniform(3, 6) * tt))
        syl = (np.sin(np.pi * np.mod(tt * rng.uniform(4, 7), 1)) ** 2)
        x[seg] += (np.sin(2 * np.pi * np.cumsum(f) / SR) + .4 * np.sin(4 * np.pi * np.cumsum(f) / SR)) * syl * np.sin(np.pi * tt / dur)
    for _ in range(4):
        i = int(rng.uniform(0, 5.8) * SR)
        m = np.arange(int(SR * .12)) / SR
        x[i:i + len(m)] += .5 * np.sin(2 * np.pi * rng.uniform(1800, 2600) * m) * np.exp(-m / .03)
    x = np.convolve(x, np.ones(12) / 12, mode="same")
    fade = int(SR * .5)
    x[:fade] = x[:fade] * np.linspace(0, 1, fade) + x[-fade:] * np.linspace(1, 0, fade)
    return x[:-fade]


def syllable(f, i):
    """NPC 말소리 한 음절 0.08초: 살짝 오르내리는 목소리 + 모음 배음"""
    t = np.arange(int(SR * .08)) / SR
    glide = f * (1 + (.12 if i % 2 else -.1) * t / .08)
    ph = 2 * np.pi * np.cumsum(glide) / SR
    x = np.sin(ph) + .5 * np.sin(2 * ph) + .25 * np.sin(3 * ph)
    return x * np.sin(np.pi * t / .08) ** 1.5


# ---------------- 2026-10-27 귀여운 편곡 (밝은 장조, 마림바·종소리·우쿨렐레·피치카토, 통통 튀는 박자) ----------------
def marimba(f, dur=.5):
    t = np.arange(int(SR * dur)) / SR
    x = np.sin(2 * np.pi * f * t) + .25 * np.sin(2 * np.pi * f * 3.93 * t) * np.exp(-t / .05)
    return x * np.exp(-t / .32) * np.minimum(1, t / .003)


def glock(f, dur=.9):
    """글로켄슈필·오르골: 맑은 종소리"""
    t = np.arange(int(SR * dur)) / SR
    x = np.sin(2 * np.pi * f * t) + .35 * np.sin(2 * np.pi * f * 2.76 * t) * np.exp(-t / .12) + .15 * np.sin(2 * np.pi * f * 5.4 * t) * np.exp(-t / .05)
    return x * np.exp(-t / .45) * np.minimum(1, t / .002)


def pizz(f, dur=.3):
    """피치카토 베이스: 짧고 둥근 소리"""
    t = np.arange(int(SR * dur)) / SR
    x = np.sin(2 * np.pi * f * t) + .3 * np.sin(4 * np.pi * f * t)
    return x * np.exp(-t / .11) * np.minimum(1, t / .004)


def ukulele(chord, dur=.25):
    """우쿨렐레 한 번 긁기: 화음을 아주 조금씩 늦게 뜯는다"""
    out = np.zeros(int(SR * (dur + .05)))
    for i, n in enumerate(chord):
        mix(out, .5 * pluck(hz(n), dur, .7), i * .012)
    return out


def shaker(dur=.06):
    n = int(SR * dur)
    x = rng.standard_normal(n)
    x = x - np.convolve(x, np.ones(4) / 4, mode="same")          # 높은 소리만
    return x * np.exp(-np.arange(n) / SR / .015)


def woodblock(f=880, dur=.08):
    t = np.arange(int(SR * dur)) / SR
    return np.sin(2 * np.pi * f * t) * np.exp(-t / .018)


def loop_song(total, render):
    """total초 길이로 만들되 끝에서 넘친 소리를 앞에 겹쳐 이음매 없이 반복되게"""
    tr = np.zeros(int(SR * (total + 2)))
    render(tr)
    n = int(SR * total)
    tail = tr[n:]
    tr[:len(tail)] += tail
    return tr[:n]


def notes(track, seq, step, start, inst, vol, octave_shift=1.0):
    """seq: ['C5', '-', ...] 한 칸이 step초. '-'는 쉼표"""
    for i, n in enumerate(seq):
        if n != "-":
            mix(track, vol * inst(hz(n) * octave_shift), start + i * step)


def cute_morning():
    """아침: C장조 4/4, 마림바 선율 + 우쿨렐레 엇박 + 셰이커 (상쾌하게)"""
    bpm = 112; e = 60 / bpm / 2; bars = 16
    chords = [("C4", "E4", "G4"), ("A3", "C4", "E4"), ("F3", "A3", "C4"), ("G3", "B3", "D4")]
    roots = ["C3", "A2", "F2", "G2"]
    A = ["E5", "G5", "E5", "C5", "D5", "E5", "G5", "-",
         "A5", "G5", "E5", "C5", "D5", "-", "C5", "-",
         "F5", "A5", "F5", "C5", "D5", "F5", "A5", "-",
         "G5", "F5", "E5", "D5", "B4", "-", "D5", "-"]
    B = ["E5", "G5", "C6", "G5", "A5", "G5", "E5", "-",
         "C5", "D5", "E5", "G5", "A5", "-", "G5", "-",
         "F5", "E5", "D5", "C5", "D5", "E5", "F5", "A5",
         "G5", "-", "D5", "-", "C5", "-", "-", "-"]
    def r(tr):
        for b in range(bars):
            t0 = b * 8 * e
            k = b % 4
            mix(tr, .55 * pizz(hz(roots[k])), t0); mix(tr, .4 * pizz(hz(roots[k]) * 1.5), t0 + 4 * e)
            for j in (1, 3, 5, 7):
                mix(tr, .22 * ukulele(chords[k], .2), t0 + j * e)
            for j in range(8):
                mix(tr, (.10 if j % 2 else .06) * shaker(), t0 + j * e)
        for half in range(2):
            notes(tr, A + B, e, half * 16 * 4 * e * 0 + half * 8 * 8 * e, marimba, .5)
    return loop_song(bars * 8 * e, r)


def cute_day():
    """낮: F장조 4/4 통통 튀는 장터 노래, 만돌린+종소리 선율, 우드블록·피치카토 (가장 신나게)"""
    bpm = 132; e = 60 / bpm / 2; bars = 16
    chords = [("F4", "A4", "C5"), ("C4", "E4", "G4"), ("D4", "F4", "A4"), ("A#3", "D4", "F4"),
              ("F4", "A4", "C5"), ("C4", "E4", "G4"), ("A#3", "D4", "F4"), ("F4", "A4", "C5")]
    roots = ["F2", "C3", "D3", "A#2", "F2", "C3", "A#2", "F2"]
    mel = ["C5", "F5", "A5", "F5", "C5", "F5", "A5", "C6",
           "A#5", "A5", "G5", "E5", "G5", "-", "C5", "-",
           "D5", "F5", "A5", "F5", "D5", "F5", "A5", "D6",
           "C6", "A#5", "A5", "G5", "F5", "-", "D5", "-",
           "C5", "F5", "A5", "C6", "A5", "F5", "G5", "A5",
           "G5", "E5", "C5", "E5", "G5", "A#5", "A5", "G5",
           "F5", "D5", "A#4", "D5", "E5", "G5", "C6", "A#5",
           "A5", "-", "F5", "-", "F5", "-", "-", "-"]
    def r(tr):
        for b in range(bars):
            t0 = b * 8 * e
            k = b % 8
            for j, m in ((0, 1.0), (3, 1.5), (4, 1.0), (6, 2.0)):
                mix(tr, .5 * pizz(hz(roots[k]) * m), t0 + j * e)
            for j in (2, 6):
                mix(tr, .28 * woodblock(), t0 + j * e)
            for j in (1, 3, 5, 7):
                mix(tr, .2 * ukulele(chords[k], .15), t0 + j * e)
        for rep in range(2):
            t0 = rep * 8 * 8 * e
            notes(tr, mel, e, t0, lambda f: pluck(f, e * .9, .9), .42)          # 만돌린
            notes(tr, mel, e, t0, glock, .16, .5 if rep == 0 else 1.0)          # 종소리로 한 번은 한 옥타브 아래, 한 번은 같게
    return loop_song(bars * 8 * e, r)


def cute_evening():
    """저녁: G장조 3/4 느긋한 왈츠, 마림바 선율 + 부드러운 아코디언 + 피치카토 (노을빛)"""
    bpm = 100; q = 60 / bpm; bars = 16
    chords = [("G3", "B3", "D4"), ("E3", "G3", "B3"), ("C3", "E3", "G3"), ("D3", "F#3", "A3")]
    roots = ["G2", "E2", "C3", "D3"]
    mel = ["B4", "D5", "G5", "E5", "D5", "B4", "C5", "E5", "G5", "F#5", "E5", "D5",
           "B4", "D5", "G5", "B5", "A5", "G5", "E5", "G5", "C6", "A5", "F#5", "D5",
           "G5", "-", "B5", "A5", "G5", "E5", "E5", "G5", "C6", "B5", "A5", "F#5",
           "G5", "B5", "D6", "C6", "B5", "G5", "A5", "-", "F#5", "G5", "-", "-"]
    def r(tr):
        for b in range(bars):
            t0 = b * 3 * q
            k = b % 4
            mix(tr, .5 * pizz(hz(roots[k])), t0)
            for j in (1, 2):
                for u in chords[k]:
                    mix(tr, .045 * reed(hz(u) * 2, q * .8, 4.0), t0 + j * q)
            mix(tr, .08 * shaker(), t0 + q); mix(tr, .08 * shaker(), t0 + 2 * q)
        notes(tr, mel, q, 0, marimba, .5)
        notes(tr, mel, q, 0, glock, .1)
    return loop_song(bars * 3 * q, r)


def cute_night():
    """밤: D장조 3/4 오르골 자장가 (종소리 선율 + 부드러운 화음, 타악기 없음)"""
    bpm = 76; q = 60 / bpm; bars = 16
    chords = [("D4", "F#4", "A4"), ("B3", "D4", "F#4"), ("G3", "B3", "D4"), ("A3", "C#4", "E4")]
    mel = ["F#5", "A5", "D6", "B5", "A5", "F#5", "G5", "B5", "D6", "C#6", "B5", "A5",
           "F#5", "E5", "D5", "F#5", "E5", "D5", "B4", "D5", "G5", "A5", "-", "-",
           "F#5", "A5", "D6", "B5", "A5", "F#5", "G5", "B5", "E6", "D6", "C#6", "A5",
           "B5", "A5", "F#5", "G5", "E5", "C#5", "D5", "-", "-", "-", "-", "-"]
    def r(tr):
        for b in range(bars):
            t0 = b * 3 * q
            k = b % 4
            mix(tr, .3 * pizz(hz(chords[k][0]) / 2, .6), t0)
            for j, u in enumerate(chords[k]):
                mix(tr, .12 * glock(hz(u), 1.2), t0 + q + j * q / 3)            # 오르골처럼 화음을 흩어 뜯는다
        notes(tr, mel, q, 0, glock, .55)
    return loop_song(bars * 3 * q, r)


def bell():
    t = np.arange(int(SR * 3.2)) / SR
    f = 330.0
    x = np.zeros_like(t)
    for ratio, a, d in ((0.5, .6, 2.5), (1, 1, 1.8), (1.19, .5, 1.2), (1.5, .35, 1.0), (2, .4, .8), (2.74, .2, .5), (3.76, .1, .35)):
        x += a * np.sin(2 * np.pi * f * ratio * t) * np.exp(-t / d)
    return x * np.minimum(1, t / .005)


def gull():
    t = np.arange(int(SR * .9)) / SR
    x = np.zeros_like(t)
    for start in (0, .32, .58):
        seg = (t >= start) & (t < start + .25)
        tt = t[seg] - start
        f = 1500 - 900 * tt / .25 + 120 * np.sin(2 * np.pi * 30 * tt)
        ph = 2 * np.pi * np.cumsum(f) / SR
        x[seg] += np.sin(ph) * np.sin(np.pi * tt / .25) * (1 - .3 * start)
    return x


def water():
    n = int(SR * 6)
    noise = rng.standard_normal(n)
    # 낮은 소리만 남긴 물결 + 천천히 오르내림, 끝과 시작을 겹쳐 이어지게
    k = np.ones(60) / 60
    low = np.convolve(noise, k, mode="same")
    t = np.arange(n) / SR
    env = .6 + .4 * np.sin(2 * np.pi * t / 3) ** 2
    x = low * env
    fade = int(SR * .5)
    x[:fade] = x[:fade] * np.linspace(0, 1, fade) + x[-fade:] * np.linspace(1, 0, fade)
    return x[:-fade]


if __name__ == "__main__":
    write("bell", bell())
    write("gull", gull())
    write("water", water())
    # 시간대 음악 4곡은 Tools/ArtGen/venice_arrange.py가 만든다 (원래 배경 음악 naru를 베네치아 악기로 편곡, 2026-10-27).
    # 이 파일의 day·night·morning·evening·cute_* 곡 함수는 이전 시안으로 남겨 둔다.
    write("crowd", crowd())
    for i, f in enumerate((210, 250, 290, 330, 380, 440)):
        write(f"voice_{i}", syllable(f, i))
    print("saved to", OUT)
