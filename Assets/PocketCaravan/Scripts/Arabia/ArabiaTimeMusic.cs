using System.Reflection;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 아라비아 시간대 배경 음악 (2026-11-07 기우). 북 NorthSound·베 VeniceSound처럼 지역마다 한 벌.
    /// 아침(5~10시)·낮(10~17)·밤(17시~) 세 곡, Resources/Arabia/Audio/arabia_bgm_*.wav. (11/07 저녁 곡은 빼고 17시부터 밤 곡)
    /// 곡은 음원이 없어 직접 합성한 임시곡(히자즈 음계 D: 우드 뜯기·네이 피리·다르부카, Claude outputs/arabia_music/synth.py) —
    /// 진짜 음원이 생기면 같은 이름으로 바꿔 넣으면 된다.
    /// 아라비아(arabia_*)에 있는 동안 공용 음악 볼륨을 0으로 내리고(위험 음악일 땐 그대로), 벗어나면 되돌린다. 공용 코드는 안 건드림.
    /// </summary>
    public class ArabiaTimeMusic : MonoBehaviour
    {
        const float Level = 0.42f;
        static readonly string[] Names = { "arabia_bgm_morning", "arabia_bgm_day", "arabia_bgm_night" };
        readonly AudioClip[] songs = new AudioClip[3];
        AudioSource a, b, cur; int playing = -1;
        AudioManager common; float savedVolume = -1f;
        static FieldInfo curClipField;
        /// <summary>공용 음악이 위험(danger) 곡이면 우리 곡을 틀지 않는다 (NorthSound와 같은 방식: 비공개 필드만 읽음)</summary>
        static bool CommonDanger(AudioManager m)
        {
            if (m == null) return false;
            curClipField ??= typeof(AudioManager).GetField("currentClip", BindingFlags.NonPublic | BindingFlags.Instance);
            return curClipField != null && curClipField.GetValue(m) as string == "danger";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<ArabiaTimeMusic>() != null) return;
            var go = new GameObject("ArabiaTimeMusic");
            DontDestroyOnLoad(go);
            go.AddComponent<ArabiaTimeMusic>();
        }

        void Awake()
        {
            a = gameObject.AddComponent<AudioSource>(); b = gameObject.AddComponent<AudioSource>();
            foreach (var m in new[] { a, b }) { m.loop = true; m.playOnAwake = false; m.volume = 0f; }
            for (int i = 0; i < 3; i++) songs[i] = Resources.Load<AudioClip>("Arabia/Audio/" + Names[i]);
        }

        static bool InArabia => MapRoot.Current != null && !string.IsNullOrEmpty(MapRoot.Current.mapId) && MapRoot.Current.mapId.StartsWith("arabia_");

        static int Slot()
        {
            float h = Mathf.Repeat(GameManager.I.Clock.TotalHours, 24f);
            return h >= 5f && h < 10f ? 0 : h >= 10f && h < 17f ? 1 : 2;   // 2 = 밤 (17시부터)
        }

        void Update()
        {
            var gm = GameManager.I;
            if (common == null) common = FindAnyObjectByType<AudioManager>();
            bool here = InArabia && gm != null && songs[1] != null && !CommonDanger(common);

            // 공용 음악: 우리 곡을 트는 동안만 0 (북·베처럼 0보다 클 때만 저장)
            if (here && common != null)
            {
                if (savedVolume < 0f && common.musicVolume > 0f) savedVolume = common.musicVolume;
                common.musicVolume = 0f;
            }
            if (!here && common != null && savedVolume >= 0f) { common.musicVolume = savedVolume; savedVolume = -1f; }

            int want = here ? Slot() : -1;
            if (want >= 0 && songs[want] == null) want = 1;
            if (want != playing)
            {
                playing = want;
                if (want >= 0)
                {
                    var next = cur == a ? b : a;
                    next.clip = songs[want]; next.volume = 0f; next.Play();
                    cur = next;
                }
                else cur = null;
            }
            float target = AudioManager.Muted ? 0f : Level;
            foreach (var m in new[] { a, b })
            {
                float goal = m == cur ? target : 0f;
                m.volume = Mathf.MoveTowards(m.volume, goal, Time.unscaledDeltaTime * 0.2f);
                if (m != cur && m.volume <= 0f && m.isPlaying) m.Stop();
            }
        }
    }
}
