using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 배경 음악(맵마다, 위험할 때는 긴장 음악)과 효과음. 소리는 Resources/Audio (Ninja Adventure, CC0).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        static AudioManager I;
        public static bool Muted { get; private set; }

        AudioSource musicA, musicB, sfx;
        AudioSource current;
        string currentClip;
        bool danger;
        AudioListener fallbackListener;
        readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        public float musicVolume = 0.45f;

        // 2026-11-08 공통화 5단계: 지역 시간대 음악이 도는 동안 공용 음악을 잠시 끄는 자리.
        // 예전엔 베네치아·북유럽·아라비아가 musicVolume을 0으로 바꿨다가 저장해 둔 값으로 되돌려서,
        // 지역을 바로 넘나들면 0이 저장되는 등 서로 꼬일 수 있었다(각 지역 주석에 그 흔적). 이제 musicVolume은 건드리지 않는다.
        static readonly HashSet<object> silencers = new HashSet<object>();
        /// <summary>owner가 on이면 공용 음악을 끈다(여러 지역이 동시에 켜도 된다). 끝나면 꼭 false로.</summary>
        public static void Silence(object owner, bool on)
        {
            if (owner == null) return;
            if (on) silencers.Add(owner); else silencers.Remove(owner);
        }
        /// <summary>지역 음악이 쓸 음량 (음소거·음악 볼륨 반영). 공용 음악이 꺼져 있어도 원래 설정 값이다.</summary>
        public static float MusicLevel => Muted ? 0f : I != null ? I.musicVolume : 0.45f;
        /// <summary>지금 공용 음악 이름 ("danger" = 위험 음악)</summary>
        public static string CurrentClip => I != null ? I.currentClip : null;
        public static bool DangerPlaying => CurrentClip == "danger";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetSilence() => silencers.Clear();
        public float sfxVolume = 0.7f;

        static readonly Dictionary<string, string> MapMusic = new Dictionary<string, string>
        {
            // 고려·중앙 동쪽 13맵(M4, 2026-11-03 정리). 표에 없는 맵(다른 지역)은 예전처럼 naru
            ["Naru"] = "naru", ["Gaegyeong"] = "capital", ["Dakmok"] = "dakmok",
            ["goryeo_songak"] = "capital", ["goryeo_jangdan"] = "naru",
            ["transit_woodtown"] = "dakmok", ["transit_ferry"] = "naru", ["transit_southport"] = "naru", ["transit_crossroads"] = "dakmok",
            ["transit_wetroad"] = "road", ["transit_pass"] = "road", ["transit_central_approach"] = "road", ["transit_south_inland"] = "road",
        };

        void Awake()
        {
            I = this;
            musicA = gameObject.AddComponent<AudioSource>();
            musicB = gameObject.AddComponent<AudioSource>();
            foreach (var m in new[] { musicA, musicB }) { m.loop = true; m.playOnAwake = false; m.volume = 0f; }
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            Muted = PlayerPrefs.GetInt("pc_muted", 0) == 1;
        }

        void Start() => EnsureListener();

        // 기존/생성 맵에 청취점이 없어도 2D 소리가 재생되게 한다. 씬 자체 청취점이 있으면 양보한다.
        public void EnsureListener()
        {
            if (fallbackListener == null) fallbackListener = gameObject.AddComponent<AudioListener>();
            bool other = false;
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (listener != fallbackListener && listener.isActiveAndEnabled) { other = true; break; }
            fallbackListener.enabled = !other;
        }

        AudioClip Clip(string path)
        {
            if (!cache.TryGetValue(path, out var c)) { c = Resources.Load<AudioClip>(path); cache[path] = c; }
            return c;
        }

        public static void OnMapEntered(string mapId)
        {
            if (I == null) return;
            I.EnsureListener();
            I.danger = false;
            I.PlayMusic(MapMusic.TryGetValue(mapId, out var m) ? m : "naru");
        }

        public static void SetDanger(bool on)
        {
            if (I == null || I.danger == on) return;
            I.danger = on;
            var map = MapRoot.Current != null ? MapRoot.Current.mapId : "";
            I.PlayMusic(on ? "danger" : (MapMusic.TryGetValue(map, out var m) ? m : "naru"));
        }

        void PlayMusic(string name)
        {
            if (currentClip == name) return;
            var clip = Clip("Audio/Music/" + name);
            if (clip == null) return;
            currentClip = name;
            var next = current == musicA ? musicB : musicA;
            next.clip = clip;
            next.volume = 0f;
            next.Play();
            current = next;
        }

        void Update()
        {
            silencers.RemoveWhere(o => o == null || (o is Object u && u == null));   // 사라진 지역 부품
            float target = Muted || silencers.Count > 0 ? 0f : musicVolume;
            foreach (var m in new[] { musicA, musicB })
            {
                float goal = m == current ? target : 0f;
                m.volume = Mathf.MoveTowards(m.volume, goal, Time.unscaledDeltaTime * 0.6f);
                if (m != current && m.volume <= 0f && m.isPlaying) m.Stop();
            }
        }

        public static void ToggleMute()
        {
            Muted = !Muted;
            PlayerPrefs.SetInt("pc_muted", Muted ? 1 : 0);
        }

        public static void PlaySfx(string name, float volume = 1f)
        {
            if (I == null || Muted) return;
            var c = I.Clip("Audio/Sfx/" + name);
            if (c != null) I.sfx.PlayOneShot(c, I.sfxVolume * volume);
        }
    }

    /// <summary>효과음 짧은 이름: coin, accept, cancel, open, move, alert, drop, bump, success, fail, levelup</summary>
    public static class Sfx
    {
        public static void Play(string name, float volume = 1f) => AudioManager.PlaySfx(name, volume);
    }
}
