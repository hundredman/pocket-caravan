using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 아라비아 NPC 말소리 (2026-11-19). arabia_* 맵에서 대화창 글자가 한 자씩 나올 때(DialogueUI.Typing),
    /// 나온 한글 글자의 모음(ㅏ·ㅔ·ㅣ·ㅗ·ㅜ·ㅓ·ㅡ)에 맞춘 부드러운 "웅얼" 음절을 몇 글자마다 하나씩 낸다.
    /// - 삑 소리가 아니라 사람 목소리처럼: 기음 + 배음을 모음 울림(포먼트)으로 거르고, 시작·끝을 둥글게, 숨소리를 조금 섞는다.
    /// - 사람마다 다른 목소리 (이름으로 정함): 아이 = 높고 빠름 · 어르신 = 낮고 느리고 살짝 떨림 · 경비 = 낮고 짧고 단단함 ·
    ///   점술가·시인·악사 = 숨이 섞인 노래하듯 꺾임 · 상인 = 높낮이가 오르내리는 흥정 말투 · 여자 이름/역할은 한 단계 높게.
    ///   같은 부류라도 이름마다 높이가 조금씩 달라 둘이 똑같지 않다.
    /// - 억양: 문장 끝(.)은 내려가고 물음(?)은 올라가며 느낌표(!)는 살짝 위로.
    /// - 잔잔하게: 효과음 볼륨의 약 15%. 음소거(AudioManager.Muted)를 따른다. 공용 코드는 DialogueUI의 공개 값만 읽는다.
    /// </summary>
    public class ArabiaVoice : MonoBehaviour
    {
        const int Rate = 22050;
        const float BaseVolume = .16f;

        struct Profile
        {
            public float f0, len, tilt, breath, vib, bend, swing, vol, gap;
            public int every;
        }

        AudioSource[] srcs; int nextSrc;   // 둘을 번갈아 써서 앞 음절 꼬리의 높이가 바뀌지 않게
        string speaker; Profile prof; int lastPage = -1, lastRevealed, count; float lastSyl;
        AudioManager common;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (GameManager.I != null && GameManager.I.GetComponent<ArabiaVoice>() == null)
                GameManager.I.gameObject.AddComponent<ArabiaVoice>();
        }

        static bool InArabia => MapRoot.Current != null && !string.IsNullOrEmpty(MapRoot.Current.mapId) && MapRoot.Current.mapId.StartsWith("arabia_");

        void Awake()
        {
            srcs = new AudioSource[2];
            for (int i = 0; i < 2; i++) { srcs[i] = gameObject.AddComponent<AudioSource>(); srcs[i].playOnAwake = false; srcs[i].spatialBlend = 0f; }
        }

        void Update()
        {
            var d = DialogueUI.Instance;
            if (d == null || !d.IsOpen || !InArabia) { lastPage = -1; speaker = null; return; }
            string who = d.Speaker ?? "";
            if (who != speaker) { speaker = who; prof = ProfileFor(who); count = 0; }
            if (Silent(who)) { lastRevealed = d.Revealed; return; }   // 11/19 이정표·팻말 같은 물건은 말소리 없이
            if (d.PageIndex != lastPage) { lastPage = d.PageIndex; lastRevealed = 0; count = 0; }
            int n = d.Revealed;
            if (n < lastRevealed) lastRevealed = 0;
            if (!d.Typing) { lastRevealed = n; return; }
            string page = d.CurrentPage;
            for (int i = lastRevealed; i < n && i < page.Length; i++)
            {
                int v = Vowel(page[i]);
                if (v < 0) continue;                       // 빈칸·문장부호·숫자는 소리 없이
                if (count++ % prof.every != 0) continue;   // 몇 글자마다 한 음절
                if (Time.unscaledTime - lastSyl < prof.gap) continue;
                lastSyl = Time.unscaledTime;
                Say(v, page, i);
            }
            lastRevealed = n;
        }

        void Say(int vowel, string page, int at)
        {
            if (AudioManager.Muted) return;
            if (common == null) common = FindAnyObjectByType<AudioManager>();
            float level = common != null ? common.sfxVolume : .7f;
            // 억양: 바로 뒤 문장부호를 보고 끝을 내리거나 올린다. 문장 안에서는 사람 말처럼 천천히 오르내림
            char end = ' ';
            for (int j = at + 1; j < page.Length && j <= at + 3; j++)
                if (".?!…".IndexOf(page[j]) >= 0) { end = page[j]; break; }
            float pitch = 1f + prof.swing * Mathf.Sin(at * .9f + speaker.Length) + Random.Range(-.02f, .02f);
            if (end == '?') pitch *= 1.08f; else if (end == '.' || end == '…') pitch *= .93f; else if (end == '!') pitch *= 1.04f;
            var src = srcs[nextSrc]; nextSrc = 1 - nextSrc;
            src.pitch = pitch;
            src.PlayOneShot(Clip(vowel), level * BaseVolume * prof.vol);
        }

        AudioClip Clip(int vowel)
        {
            string key = speaker + "#" + vowel;
            if (!clips.TryGetValue(key, out var c)) clips[key] = c = Syllable(key, vowel, prof, Mathf.Abs(key.GetHashCode()));
            return c;
        }

        // ---------------- 사람마다 목소리 ----------------

        /// <summary>사람이 아닌 것 (이정표·팻말·게시판·비석 등, 이름이 없는 글) → 말소리 없음</summary>
        public static bool Silent(string name) => string.IsNullOrEmpty(name) || Has(name, "이정표", "표지판", "안내판", "게시판", "팻말", "비석", "푯말", "알림판");

        static bool Has(string s, params string[] keys) { foreach (var k in keys) if (s.Contains(k)) return true; return false; }

        static Profile ProfileFor(string name)
        {
            name = name ?? "";
            var p = new Profile { f0 = 215f, len = .1f, tilt = .62f, breath = .07f, vib = 0f, bend = .03f, swing = .04f, vol = 1f, gap = .075f, every = 2 };
            if (Has(name, "아이", "꼬마", "소녀", "소년"))
            { p.f0 = 360f; p.len = .075f; p.tilt = .55f; p.breath = .04f; p.bend = .07f; p.swing = .06f; p.gap = .06f; p.vol = .85f; }
            else if (Has(name, "할머니", "할아버지", "노인", "은자", "어르신"))
            { p.f0 = 170f; p.len = .135f; p.breath = .16f; p.vib = .022f; p.bend = .02f; p.swing = .03f; p.gap = .11f; p.every = 3; p.vol = .9f; }
            else if (Has(name, "경비", "수비", "순찰", "대장", "문지기", "성채", "병"))
            { p.f0 = 148f; p.len = .085f; p.tilt = .58f; p.breath = .04f; p.bend = -.03f; p.swing = .02f; p.gap = .09f; p.every = 3; p.vol = .9f; }
            else if (Has(name, "점술가", "시인", "악사", "학자", "춤꾼", "곡예사"))
            { p.f0 = 205f; p.len = .14f; p.breath = .2f; p.vib = .014f; p.bend = .06f; p.swing = .06f; p.gap = .1f; p.every = 3; }
            else if (Has(name, "상인", "장수", "주인", "지배인", "조합장", "노점상", "지기", "사공"))
            { p.f0 = 220f; p.len = .095f; p.bend = .05f; p.swing = .08f; p.gap = .07f; }
            else if (Has(name, "농부", "양치기", "낙타몰이", "나무꾼", "캐는", "채집꾼", "길잡이", "안내인", "나그네", "대상"))
            { p.f0 = 185f; p.len = .11f; p.breath = .1f; p.bend = .03f; p.swing = .05f; p.gap = .085f; }
            // 여자 이름·역할은 한 단계 높게 (할머니는 어르신 목소리 그대로 조금만)
            if (Has(name, "레일라", "야스민", "셀린", "살마", "아주머니", "춤꾼", "소녀")) p.f0 *= 1.38f;
            else if (Has(name, "할머니")) p.f0 *= 1.25f;
            // 이름마다 조금씩 다른 높이 (±7%)
            int h = Mathf.Abs(name.GetHashCode());
            p.f0 *= .93f + (h % 15) / 100f;
            p.len *= .95f + (h / 15 % 10) / 100f;
            return p;
        }

        // ---------------- 소리 만들기 ----------------

        // 모음 울림 (F1, F2, F3 Hz): ㅏ ㅔ ㅣ ㅗ ㅜ ㅓ ㅡ. 작은 동물 상인들이라 사람 값보다 조금 높게
        static readonly float[,] Formants =
        {
            { 850f, 1300f, 2700f }, { 560f, 1950f, 2700f }, { 340f, 2350f, 3000f },
            { 520f, 900f, 2600f }, { 370f, 820f, 2500f }, { 640f, 1100f, 2600f }, { 380f, 1450f, 2600f },
        };

        /// <summary>한글 글자 → 모음 갈래 (0~6). 한글이 아니면 영문 모음 정도만, 나머지는 -1</summary>
        static int Vowel(char c)
        {
            if (c >= 0xAC00 && c <= 0xD7A3)
            {
                int jung = (c - 0xAC00) / 28 % 21;
                switch (jung)
                {
                    case 0: case 2: case 9: return 0;                                 // ㅏ ㅑ ㅘ
                    case 1: case 3: case 5: case 7: case 10: case 11: case 15: return 1; // ㅐ ㅒ ㅔ ㅖ ㅙ ㅚ ㅞ
                    case 16: case 19: case 20: return 2;                              // ㅟ ㅢ ㅣ
                    case 8: case 12: return 3;                                        // ㅗ ㅛ
                    case 13: case 17: return 4;                                       // ㅜ ㅠ
                    case 4: case 6: case 14: return 5;                                // ㅓ ㅕ ㅝ
                    default: return 6;                                                // ㅡ
                }
            }
            if (char.IsLetter(c)) { int k = "aeiou".IndexOf(char.ToLowerInvariant(c)); return k >= 0 ? k : 6; }
            return -1;
        }

        static float G(float f, float centre, float bw) { float x = (f - centre) / bw; return Mathf.Exp(-x * x); }

        /// <summary>한 음절: 기음+배음을 모음 울림으로 거르고(사람 목소리처럼), 둥근 시작·끝, 숨소리 조금, 부드럽게 한 번 더 거른다</summary>
        static AudioClip Syllable(string name, int vowel, Profile p, int seed)
        {
            var rng = new System.Random(seed);
            int n = Mathf.RoundToInt(p.len * Rate), tail = Mathf.RoundToInt(.03f * Rate);
            var d = new float[n + tail];
            float F1 = Formants[vowel, 0], F2 = Formants[vowel, 1], F3 = Formants[vowel, 2];
            int H = Mathf.Min(28, Mathf.FloorToInt(4200f / p.f0));
            var amp = new float[H + 1]; var ph = new double[H + 1];
            float noise = 0f, lp = 0f, vibRate = 5.2f + (seed % 7) * .2f;
            for (int i = 0; i < n; i++)
            {
                float k = (float)i / n, t = (float)i / Rate;
                // 음절 안 높이: 살짝 오르다 내려오는 꺾임 + 끝이 조금 처짐 + 어르신·점술가는 떨림
                float f = p.f0 * (1f + p.bend * Mathf.Sin(Mathf.PI * k) - .05f * k * k + p.vib * Mathf.Sin(2f * Mathf.PI * vibRate * t));
                float s = 0f;
                for (int hI = 1; hI <= H; hI++)
                {
                    float fh = f * hI;
                    if ((i & 63) == 0 || amp[hI] == 0f)
                        amp[hI] = Mathf.Pow(p.tilt, hI - 1) * (G(fh, F1, 160f) + .55f * G(fh, F2, 220f) + .18f * G(fh, F3, 300f) + .04f);
                    ph[hI] += 2.0 * System.Math.PI * fh / Rate;
                    s += amp[hI] * (float)System.Math.Sin(ph[hI]);
                }
                // 숨소리: 낮게 거른 잡음
                noise += ((float)rng.NextDouble() * 2f - 1f - noise) * .25f;
                s += noise * p.breath * 2.2f;
                // 둥근 모양: 앞 18ms는 천천히 커지고, 뒤 45%는 부드럽게 사라짐
                float att = Mathf.Clamp01(t / .018f); att = att * att * (3f - 2f * att);
                float rel = k < .55f ? 1f : Mathf.Cos((k - .55f) / .45f * Mathf.PI * .5f);
                s *= att * rel;
                lp += (s - lp) * .45f;   // 한 번 더 부드럽게 (고음 줄이기)
                d[i] = lp;
            }
            for (int i = n; i < d.Length; i++) { lp *= .9f; d[i] = lp; }
            float m = 0f; foreach (var v in d) m = Mathf.Max(m, Mathf.Abs(v));
            if (m > 0f) for (int i = 0; i < d.Length; i++) d[i] = d[i] / m * .6f;
            var c = AudioClip.Create("arabia_voice_" + name, d.Length, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }
    }
}
