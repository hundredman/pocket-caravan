"""베네치아 음악·소리 합성 (2026-10-26, 제안). 새로 지은 짧은 곡과 효과음을 코드로 만든다(외부 음원 없음).
    사용: python Tools/ArtGen/venice_audio.py
    출력: Assets/PocketCaravan/Resources/Venice/Audio/
      venice_day.wav   낮: 3/4 왈츠, 뜯는 현(만돌린풍, Karplus-Strong) 선율 + 기타풍 반주 (반복 재생)
      venice_night.wav 밤: 느린 바르카롤레, 아코디언풍 부드러운 화음 + 선율
      bell.wav 종탑 종 / gull.wav 갈매기 / water.wav 물결(반복)
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
    write("venice_day", day())
    write("venice_night", night())
    write("bell", bell())
    write("gull", gull())
    write("water", water())
    print("saved to", OUT)
