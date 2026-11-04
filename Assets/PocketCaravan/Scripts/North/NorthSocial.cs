using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 북유럽 사람들과 어울리기 — 2026-11-04 통합으로 대부분 공용으로 옮겼다.
    /// - 리액션 바퀴(R): 공용 Reactions / 친밀도·선물(G)·친구 수첩(F)·사진(P): 공용 Social (북유럽 친구 8명도 거기).
    /// 여기 남은 것: 북유럽 코드가 부르던 리액션 이름(배우면 알림)과, 날씨 기도의 실제 효과(눈보라 누그러뜨리기).
    /// </summary>
    public class NorthSocial : MonoBehaviour
    {
        public static string Teach(string id) => Reactions.Teach(id, true);
        public static string TeachRandom() => Reactions.TeachRandom(true);

        static int Today => GameManager.I != null ? GameManager.I.Clock.day : 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthSocial>() != null) return;
            var go = new GameObject("NorthSocial");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthSocial>();
        }

        void OnEnable() { Reactions.Performed += OnReaction; }
        void OnDisable() { Reactions.Performed -= OnReaction; }

        /// <summary>날씨 기도: 하루 한 번, 함박눈·눈보라를 지금 네 시간 동안 가랑눈으로 (NorthAtmosphere가 "calm_block"을 봄)</summary>
        static void OnReaction(string id)
        {
            if (id != "pray" || GameManager.I == null || !NorthUtil.InNorth) return;
            int block = Mathf.FloorToInt(GameManager.I.Clock.TotalHours / 4f);
            if (NorthSave.Get("prayday", -1f) < Today && NorthAtmosphere.Shown >= NorthAtmosphere.Weather.Heavy)
            {
                NorthSave.Set("prayday", Today); NorthSave.Set("calm_block", block);
                Notify.Show("기도가 닿았다… 눈보라가 잦아든다.", 3f);
            }
            else Notify.Show(NorthSave.Get("prayday", -1f) >= Today ? "오늘은 이미 기도를 올렸다." : "하늘이 이미 잔잔하다.", 2f);
        }
    }
}
