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
        public float sfxVolume = 0.7f;

        static readonly Dictionary<string, string> MapMusic = new Dictionary<string, string>
        {
            ["Naru"] = "naru", ["Route01"] = "road", ["Gaegyeong"] = "capital", ["Dakmok"] = "dakmok",
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
            float target = Muted ? 0f : musicVolume;
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
