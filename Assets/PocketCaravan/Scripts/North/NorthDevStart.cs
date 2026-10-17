using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 개발용: 북유럽 씬에서 바로 Play하면 출신을 바이킹으로 바꾼다 (시작 출신은 GameDatabase가 고려로 정해 둠).
    /// 빌드 0번 시작 맵에서 시작한 게임에는 손대지 않는다. 지역을 합치고 출신 고르기가 생기면 지운다.
    /// </summary>
    public class NorthDevStart : MonoBehaviour
    {
        public OriginData origin;
        static bool applied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => applied = false;

        void Start()
        {
            if (applied) return;
            applied = true;
            var gm = GameManager.I;
            if (gm == null || origin == null) return;
            if (SceneManager.GetActiveScene().buildIndex == 0) return;
            if (gm.Player.origin == origin) return;
            gm.Player.origin = origin;
            gm.Player.credit = origin.startingCredit;
            Debug.Log($"[북유럽 테스트] 출신을 {origin.displayName}(으)로 바꿨습니다. 신용 {origin.startingCredit}");
        }
    }
}
