using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 베네치아 소리 (2026-10-26). 베네치아·본토 서부(venice_*) 맵에서만.
    /// - 시간대 음악: 낮(6~19시) 만돌린풍 왈츠, 밤 아코디언풍 바르카롤레 (Tools/ArtGen/venice_audio.py가 합성한 새 곡).
    ///   베네치아에 있는 동안 공용 AudioManager 음악 볼륨을 0으로 낮춰 두고(위험 음악은 그대로 들리게 하지 않음), 벗어나면 되돌린다.
    /// - 물결: 석호 맵에서 잔잔히. 종탑 종: 석호 마을에서 정각(9·12·18시)에 울림. 갈매기: 물이 넓은 맵에서 가끔.
    /// 공용 코드는 고치지 않는다. 음소거(AudioManager.Muted)를 따른다.
    /// </summary>
    public class VeniceSound : MonoBehaviour
    {
        AudioSource music, water, sfx;
        AudioManager common;
        float savedVolume = -1f;
        int lastHour = -1;
        float nextGull;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (GameManager.I != null && GameManager.I.GetComponent<VeniceSound>() == null)
                GameManager.I.gameObject.AddComponent<VeniceSound>();
        }

        void Awake()
        {
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true; music.playOnAwake = false; music.volume = 0f;
            water = gameObject.AddComponent<AudioSource>();
            water.loop = true; water.playOnAwake = false; water.volume = 0f;
            water.clip = Resources.Load<AudioClip>("Venice/Audio/water");
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
        }

        void Update()
        {
            var gm = GameManager.I;
            bool here = VeniceUtil.InVenice && gm != null;
            if (common == null) common = FindAnyObjectByType<AudioManager>();
            if (here && common != null && savedVolume < 0f) { savedVolume = common.musicVolume; common.musicVolume = 0f; }
            if (!here && common != null && savedVolume >= 0f) { common.musicVolume = savedVolume; savedVolume = -1f; }

            float vol = AudioManager.Muted || !here ? 0f : .4f;
            if (here)
            {
                int h = gm.Clock.Hour;
                var clip = Resources.Load<AudioClip>(h >= 6 && h < 19 ? "Venice/Audio/venice_day" : "Venice/Audio/venice_night");
                if (clip != null && music.clip != clip)
                {
                    if (music.volume > .02f) vol = 0f;          // 바뀔 때는 먼저 줄였다가
                    else { music.clip = clip; music.Play(); }
                }
                else if (!music.isPlaying && clip != null) music.Play();
            }
            music.volume = Mathf.MoveTowards(music.volume, vol, Time.unscaledDeltaTime * .5f);

            var data = here ? VeniceData.Get(VeniceUtil.Map) : null;
            bool lagoon = here && !VeniceUtil.InMainland;
            float wv = AudioManager.Muted || !lagoon ? 0f : .18f;
            if (wv > 0f && !water.isPlaying && water.clip != null) water.Play();
            water.volume = Mathf.MoveTowards(water.volume, wv, Time.unscaledDeltaTime * .3f);
            if (water.volume <= 0f && water.isPlaying) water.Stop();

            if (!here || AudioManager.Muted) { lastHour = -1; return; }
            int hour = gm.Clock.Hour;
            if (lastHour >= 0 && hour != lastHour && lagoon && (hour == 9 || hour == 12 || hour == 18))
                sfx.PlayOneShot(Resources.Load<AudioClip>("Venice/Audio/bell"), .5f);
            lastHour = hour;
            if (data != null && data.gulls && Time.time > nextGull)
            {
                if (nextGull > 0f) sfx.PlayOneShot(Resources.Load<AudioClip>("Venice/Audio/gull"), .25f);
                nextGull = Time.time + Random.Range(14f, 30f);
            }
        }
    }
}
