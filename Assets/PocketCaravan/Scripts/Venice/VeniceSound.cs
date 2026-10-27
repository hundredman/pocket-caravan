using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 베네치아 소리 (2026-10-26). 베네치아·본토 서부(venice_*) 맵에서만.
    /// - 시간대 음악 4곡: 원래 배경 음악(naru)의 선율·화음을 베네치아 악기로 다시 연주 (Tools/ArtGen/venice_arrange.py).
    ///   아침(5~10시) 만돌린+기타, 낮(10~17시) 만돌린 트레몰로+아코디언+탬버린, 저녁(17~20시) 아코디언 선율, 밤 부드러운 아코디언+하프
    /// - 장터 웅성거림: 낮(8~19시) 장터에서 10칸 안이면 가까울수록 크게. NPC 말소리: 대화창이 열리거나 쪽이 넘어갈 때 글자 수만큼
    ///   짧은 음절(말하는 사람 이름마다 목소리 높이가 다름). 대화창 상태는 공용 DialogueUI를 읽기만 한다(비공개 open 값은 리플렉션).
    ///   베네치아에 있는 동안 공용 AudioManager 음악 볼륨을 0으로 낮춰 두고(위험 음악은 그대로 들리게 하지 않음), 벗어나면 되돌린다.
    /// - 물결: 석호 맵에서 잔잔히. 종탑 종: 석호 마을에서 정각(9·12·18시)에 울림. 갈매기: 물이 넓은 맵에서 가끔.
    /// 공용 코드는 고치지 않는다. 음소거(AudioManager.Muted)를 따른다.
    /// </summary>
    public class VeniceSound : MonoBehaviour
    {
        AudioSource music, water, sfx, crowd;
        DialogueUI dialogue;
        System.Reflection.FieldInfo openField;
        bool wasOpen; int lastPage = -1; string lastText; float babbleLeft, nextSyl; int voice;
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
            crowd = gameObject.AddComponent<AudioSource>();
            crowd.loop = true; crowd.playOnAwake = false; crowd.volume = 0f;
            crowd.clip = Resources.Load<AudioClip>("Venice/Audio/crowd");
            openField = typeof(DialogueUI).GetField("open", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        }

        static string TrackFor(int h) => h >= 5 && h < 10 ? "venice_morning" : h >= 10 && h < 17 ? "venice_day" : h >= 17 && h < 20 ? "venice_evening" : "venice_night";

        void Babble(bool here)
        {
            if (dialogue == null) dialogue = FindAnyObjectByType<DialogueUI>();
            bool open = here && dialogue != null && openField != null && (bool)openField.GetValue(dialogue);
            if (open)
            {
                string text = dialogue.CurrentPage;
                if (!wasOpen || dialogue.PageIndex != lastPage || text != lastText)
                {
                    lastPage = dialogue.PageIndex; lastText = text;
                    babbleLeft = Mathf.Min(1.6f, (text?.Length ?? 0) * .03f);
                    var who = PlayerController.Instance != null && PlayerController.Instance.TalkTarget != null ? PlayerController.Instance.TalkTarget.DisplayName : text;
                    voice = Mathf.Abs((who ?? "").GetHashCode()) % 6;
                }
            }
            else babbleLeft = 0f;
            wasOpen = open;
            if (babbleLeft <= 0f || AudioManager.Muted) return;
            babbleLeft -= Time.unscaledDeltaTime;
            if (Time.unscaledTime < nextSyl) return;
            nextSyl = Time.unscaledTime + Random.Range(.075f, .11f);
            var clip = Resources.Load<AudioClip>("Venice/Audio/voice_" + ((voice + Random.Range(0, 2)) % 6));
            if (clip != null) sfx.PlayOneShot(clip, .22f);
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
                var clip = Resources.Load<AudioClip>("Venice/Audio/" + TrackFor(h));
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

            // 장터 웅성거림 (낮, 장터 가까이)
            float cv = 0f;
            var pcx = PlayerController.Instance;
            if (here && !AudioManager.Muted && data != null && data.market != null && data.market.Length >= 2 && pcx != null && gm.Clock.Hour >= 8 && gm.Clock.Hour < 19)
            {
                float d = Vector3.Distance(pcx.transform.position, VeniceUtil.Cell(data.market[0], data.market[1]));
                cv = Mathf.Clamp01(1f - d / 10f) * .22f;
            }
            if (cv > 0f && !crowd.isPlaying && crowd.clip != null) crowd.Play();
            crowd.volume = Mathf.MoveTowards(crowd.volume, cv, Time.unscaledDeltaTime * .4f);
            if (crowd.volume <= 0f && crowd.isPlaying) crowd.Stop();
            Babble(here);

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
