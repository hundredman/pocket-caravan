using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 북유럽 소리 (10/25, 동물의 숲 오마주). 북유럽·중앙 북쪽 맵에서만.
    /// - 시간대 배경 음악: 원래 배경 음악(naru)의 멜로디·화음을 북유럽 악기로 다시 연주한 4곡 (Resources/North/Audio).
    ///   아침=나무 피리 + 칸텔레(뜯는 현) + 작은 북, 낮=민속 바이올린 + 칸텔레 + 북 장단,
    ///   저녁=니켈하르파(공명 현 바이올린) + 부드러운 현, 밤=얼음 종 자장가(조금 느리게).
    ///   북유럽에 있는 동안은 공용 음악 볼륨을 0으로 내려 두고(위험 음악일 땐 그대로), 벗어나면 되돌린다 (공용 코드는 그대로).
    /// - 마을 생활 소리: 장터 웅성거림(마을·낮), 썰매 장인 망치(순록 고원·낮·가까이 갈 때만), 갈매기(바닷가·낮), 까마귀(숲·낮), 부엉이(밤). 코드로 만든 소리.
    /// - NPC 말소리: 대화창이 열리거나 다음 쪽으로 넘어갈 때, 글자 수만큼 "우물우물" (말하는 사람마다 목소리 높이가 다름).
    /// </summary>
    public class NorthSound : MonoBehaviour
    {
        const int Rate = 22050;
        const float MusicLevel = 0.45f;   // 공용 음악 기본 볼륨(0.45)과 같게 (곡 크기도 원곡에 맞춤)
        static readonly string[] SongNames = { "north_bgm_morning", "north_bgm_day", "north_bgm_evening", "north_bgm_night" };
        AudioSource musA, musB, cur, amb, voice;
        readonly AudioClip[] songs = new AudioClip[4];
        int playing = -1;
        AudioClip[] babble, chatter;
        AudioClip clank, gull, owl, caw;
        float nextChat, nextClank, nextGull, nextOwl, nextCaw;
        // 공용 음악 (리플렉션으로 볼륨만)
        object audioMgr; FieldInfo musicVolField, curClipField; float savedMusicVol = -1f;
        // 대화창 관찰
        object dlg; FieldInfo dlgOpen, dlgPage, dlgSpeaker, dlgPages;
        bool wasOpen; int lastPage = -1; float babbleLeft, nextSyl;
        bool built;

        static readonly HashSet<string> Coast = new HashSet<string> { "north_birka", "north_naru", "north_c_road_ne", "north_c_creek" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthSound>() != null) return;
            var go = new GameObject("NorthSound");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthSound>();
        }

        void Awake()
        {
            musA = gameObject.AddComponent<AudioSource>(); musB = gameObject.AddComponent<AudioSource>();
            foreach (var m in new[] { musA, musB }) { m.loop = true; m.playOnAwake = false; m.volume = 0f; }
            amb = gameObject.AddComponent<AudioSource>(); amb.playOnAwake = false;
            voice = gameObject.AddComponent<AudioSource>(); voice.playOnAwake = false;
            for (int i = 0; i < songs.Length; i++) songs[i] = Resources.Load<AudioClip>("North/Audio/" + SongNames[i]);
            StartCoroutine(Build());
        }

        IEnumerator Build()
        {
            yield return null;
            babble = new AudioClip[6];
            for (int i = 0; i < babble.Length; i++) babble[i] = Syllable("north_syl" + i, 260f + i * 38f, i);
            chatter = new AudioClip[3];
            for (int i = 0; i < chatter.Length; i++) chatter[i] = Chatter("north_chatter" + i, i);
            clank = Clank(); gull = Gull(); owl = Owl(); caw = Caw();
            built = true;
        }

        void Update()
        {
            bool north = NorthUtil.InNorth && GameManager.I != null;
            UpdateMusic(north);
            if (!built) return;
            if (north && !AudioManager.Muted) { UpdateAmbient(); UpdateBabble(); }
        }

        // ---------------- 배경 음악 (시간대별 북유럽 편곡) ----------------

        /// <summary>0 아침(5~10시), 1 낮(10~17시), 2 저녁(17~21시), 3 밤</summary>
        static int TimeSlot()
        {
            float h = Mathf.Repeat(GameManager.I.Clock.TotalHours, 24f);
            return h >= 5f && h < 10f ? 0 : h >= 10f && h < 17f ? 1 : h >= 17f && h < 21f ? 2 : 3;
        }

        void UpdateMusic(bool north)
        {
            if (audioMgr == null)
            {
                var tp = typeof(AudioManager);
                var f = tp.GetField("I", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
                audioMgr = f != null ? f.GetValue(null) : null;
                if (audioMgr != null)
                {
                    musicVolField = tp.GetField("musicVolume", BindingFlags.Public | BindingFlags.Instance);
                    curClipField = tp.GetField("currentClip", BindingFlags.NonPublic | BindingFlags.Instance);
                }
            }
            string shared = audioMgr != null && curClipField != null ? curClipField.GetValue(audioMgr) as string : null;
            bool ours = north && shared != "danger" && songs[1] != null;   // 위험 음악은 공용 그대로

            // 공용 음악: 우리 곡을 트는 동안만 볼륨 0, 아니면 원래대로
            if (audioMgr != null && musicVolField != null)
            {
                float v = (float)musicVolField.GetValue(audioMgr);
                if (ours && v > 0f) { savedMusicVol = v; musicVolField.SetValue(audioMgr, 0f); }
                else if (!ours && savedMusicVol >= 0f) { musicVolField.SetValue(audioMgr, savedMusicVol); savedMusicVol = -1f; }
            }

            int want = ours ? TimeSlot() : -1;
            if (want >= 0 && songs[want] == null) want = 1;
            if (want != playing)
            {
                playing = want;
                if (want >= 0)
                {
                    var next = cur == musA ? musB : musA;
                    next.clip = songs[want]; next.volume = 0f;
                    // 같은 박자의 곡끼리는 같은 마디 위치에서 이어 받기 (밤 곡만 느려서 처음부터)
                    if (cur != null && cur.clip != null && cur.isPlaying && want != 3 && cur.clip.length > 0f && Mathf.Abs(cur.clip.length - next.clip.length) < 0.1f)
                        next.time = Mathf.Repeat(cur.time, next.clip.length - 0.01f);
                    next.Play();
                    cur = next;
                }
                else cur = null;
            }
            float target = AudioManager.Muted ? 0f : MusicLevel;
            foreach (var m in new[] { musA, musB })
            {
                float goal = m == cur ? target : 0f;
                m.volume = Mathf.MoveTowards(m.volume, goal, Time.unscaledDeltaTime * 0.2f);   // 5초쯤 걸려 천천히 바뀜
                if (m != cur && m.volume <= 0f && m.isPlaying) m.Stop();
            }
        }

        // ---------------- 마을 생활 소리 ----------------

        void UpdateAmbient()
        {
            string map = NorthUtil.Map;
            float h = Mathf.Repeat(GameManager.I.Clock.TotalHours, 24f), t = Time.time;
            bool day = h >= 7f && h < 19f, night = h >= 21f || h < 5f;
            bool town = NorthUtil.TownMaps.Contains(map);
            bool storm = NorthAtmosphere.Shown >= NorthAtmosphere.Weather.Blizzard;
            if (town && day && t >= nextChat) { nextChat = t + Random.Range(4f, 9f); amb.PlayOneShot(chatter[Random.Range(0, chatter.Length)], 0.12f); }
            // 망치 소리: 순록 고원 썰매 장인(11,14) 곁에서만, 가까울수록 크게 (6칸 밖이면 안 들림)
            if (map == "north_herd" && day && t >= nextClank)
            {
                nextClank = t + Random.Range(4f, 8f);
                var pc = PlayerController.Instance;
                if (pc != null)
                {
                    float dist = Vector2.Distance(pc.transform.position, NorthUtil.Cell(map, 11, 14));
                    if (dist < 6f) StartCoroutine(Hammer(0.08f * (1f - dist / 6f)));
                }
            }
            if (Coast.Contains(map) && day && !storm && t >= nextGull) { nextGull = t + Random.Range(6f, 14f); amb.PlayOneShot(gull, 0.13f); }
            if (!town && day && !storm && t >= nextCaw) { nextCaw = t + Random.Range(9f, 18f); amb.PlayOneShot(caw, 0.1f); }
            if (night && !storm && t >= nextOwl) { nextOwl = t + Random.Range(10f, 22f); amb.PlayOneShot(owl, 0.14f); }
        }

        IEnumerator Hammer(float vol)
        {
            int n = Random.Range(2, 4);
            for (int i = 0; i < n; i++) { amb.PlayOneShot(clank, vol); yield return new WaitForSeconds(0.45f); }
        }

        // ---------------- NPC 말소리 ----------------

        void UpdateBabble()
        {
            if (dlg == null)
            {
                var f = typeof(DialogueUI).GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
                dlg = f != null ? f.GetValue(null) : null;
                if (dlg == null) return;
                var tp = typeof(DialogueUI);
                dlgOpen = tp.GetField("open", BindingFlags.NonPublic | BindingFlags.Instance);
                dlgPage = tp.GetField("pageIndex", BindingFlags.NonPublic | BindingFlags.Instance);
                dlgSpeaker = tp.GetField("speaker", BindingFlags.NonPublic | BindingFlags.Instance);
                dlgPages = tp.GetField("pages", BindingFlags.NonPublic | BindingFlags.Instance);
                if (dlgOpen == null) { dlg = null; return; }
            }
            bool open = (bool)dlgOpen.GetValue(dlg);
            int page = dlgPage != null ? (int)dlgPage.GetValue(dlg) : 0;
            if (open && (!wasOpen || page != lastPage))
            {
                string speaker = dlgSpeaker != null ? dlgSpeaker.GetValue(dlg) as string : "";
                int len = 40;
                if (dlgPages != null && dlgPages.GetValue(dlg) is IList pages && page < pages.Count && pages[page] is string s) len = s.Length;
                babbleLeft = Mathf.Min(1.8f, len * 0.035f);
                int hsh = string.IsNullOrEmpty(speaker) ? 0 : speaker.GetHashCode();
                bool kid = speaker != null && (speaker.Contains("아이") || speaker.Contains("소녀") || speaker.Contains("꼬마"));
                bool old = speaker != null && (speaker.Contains("할머니") || speaker.Contains("노인") || speaker.Contains("할아"));
                voice.pitch = kid ? 1.45f : old ? 0.8f : 0.9f + (Mathf.Abs(hsh) % 40) / 100f;
            }
            if (!open) babbleLeft = 0f;
            wasOpen = open; lastPage = page;
            if (babbleLeft > 0f)
            {
                babbleLeft -= Time.unscaledDeltaTime;
                if (Time.unscaledTime >= nextSyl)
                {
                    nextSyl = Time.unscaledTime + Random.Range(0.065f, 0.095f);
                    voice.PlayOneShot(babble[Random.Range(0, babble.Length)], 0.22f);
                }
            }
        }

        // ---------------- 소리 만들기 ----------------

        static AudioClip Make(string name, float[] d) { var c = AudioClip.Create(name, d.Length, 1, Rate, false); c.SetData(d, 0); return c; }

        /// <summary>말소리 한 음절: 짧은 모음 같은 소리 (기음 + 배음, 끝이 살짝 내려감)</summary>
        static AudioClip Syllable(string name, float f0, int seed)
        {
            int n = (int)(0.07f * Rate); var d = new float[n];
            float formant = 2.2f + (seed % 3) * 0.6f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate, k = (float)i / n, f = f0 * (1f - 0.12f * k);
                float env = Mathf.Sin(Mathf.PI * k);
                d[i] = env * 0.5f * (Mathf.Sin(2f * Mathf.PI * f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * f * formant * t) + 0.25f * Mathf.Sin(2f * Mathf.PI * f * 3f * t));
            }
            return Make(name, d);
        }

        /// <summary>장터 웅성거림: 여러 목소리 음절을 겹쳐 2초</summary>
        static AudioClip Chatter(string name, int seed)
        {
            var rng = new System.Random(seed + 100);
            int n = Rate * 2; var d = new float[n];
            for (int v = 0; v < 18; v++)
            {
                int start = rng.Next(0, n - 3000);
                float f0 = 180f + (float)rng.NextDouble() * 220f;
                for (int i = 0; i < 1500; i++)
                {
                    float t = (float)i / Rate, k = i / 1500f;
                    d[start + i] += Mathf.Sin(Mathf.PI * k) * 0.18f * (Mathf.Sin(2f * Mathf.PI * f0 * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * f0 * 2.4f * t));
                }
            }
            for (int i = 0; i < n; i++) d[i] *= Mathf.Clamp01(Mathf.Min(i, n - i) / 4000f);
            return Make(name, d);
        }

        static AudioClip Clank()
        {
            int n = (int)(0.5f * Rate); var d = new float[n];
            float[] partials = { 523f, 1312f, 2117f, 3290f };
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate, v = 0f;
                for (int p = 0; p < partials.Length; p++) v += Mathf.Sin(2f * Mathf.PI * partials[p] * t) * Mathf.Exp(-t * (8f + p * 4f)) / (p + 1);
                d[i] = v * 0.6f;
            }
            return Make("north_clank", d);
        }

        static AudioClip Gull()
        {
            int n = (int)(0.9f * Rate); var d = new float[n]; float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float k = (float)i / n;
                float f = k < 0.3f ? Mathf.Lerp(1400f, 2100f, k / 0.3f) : Mathf.Lerp(2100f, 1200f, (k - 0.3f) / 0.7f);
                ph += 2f * Mathf.PI * f / Rate;
                d[i] = (Mathf.Sin(ph) + 0.3f * Mathf.Sin(ph * 2f)) * Mathf.Sin(Mathf.PI * Mathf.Min(1f, k * 1.2f)) * 0.35f * (0.8f + 0.2f * Mathf.Sin(k * 60f));
            }
            return Make("north_gull", d);
        }

        static AudioClip Owl()
        {
            int n = (int)(1.3f * Rate); var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float e1 = t < 0.45f ? Mathf.Sin(Mathf.PI * t / 0.45f) : 0f;
                float e2 = t > 0.6f && t < 1.25f ? Mathf.Sin(Mathf.PI * (t - 0.6f) / 0.65f) : 0f;
                d[i] = Mathf.Sin(2f * Mathf.PI * (t < 0.5f ? 392f : 349f) * t) * (e1 + e2) * 0.4f;
            }
            return Make("north_owl", d);
        }

        static AudioClip Caw()
        {
            int n = (int)(0.35f * Rate); var d = new float[n]; var rng = new System.Random(5); float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float k = (float)i / n; ph += 2f * Mathf.PI * Mathf.Lerp(700f, 520f, k) / Rate;
                float saw = (ph / (2f * Mathf.PI)) % 1f * 2f - 1f;
                d[i] = (saw * 0.5f + ((float)rng.NextDouble() - 0.5f) * 0.3f) * Mathf.Sin(Mathf.PI * k) * 0.35f;
            }
            return Make("north_caw", d);
        }
    }
}
