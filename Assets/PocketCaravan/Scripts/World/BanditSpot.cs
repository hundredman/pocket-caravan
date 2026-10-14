using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 도적 출몰 자리. 짐을 들고 이 맵에 들어오면 도적이 나타날 수 있다 (밤에 더 자주).
    /// 한 번 만나면 그 맵은 한동안(게임 시간 12시간) 조용하다.
    /// </summary>
    public class BanditSpot : MonoBehaviour
    {
        [Tooltip("이 거리(칸) 안에 들어오면 쫓아온다")] public float sightRange = 6f;
        [Range(0, 1)] public float dayChance = 0.55f;
        [Range(0, 1)] public float nightChance = 0.9f;
        public CharacterAnimator prefabAnimator;   // 빌더가 넣은 도적 모습 (꺼진 채로 둠)

        void Start()
        {
            var gm = GameManager.I;
            if (gm == null || prefabAnimator == null || MapRoot.Current == null) return;
            string map = MapRoot.Current.mapId;
            if (!gm.Player.HasAnyCargo || !gm.BanditReady(map)) return;
            float chance = gm.Clock.Daylight < 0.5f ? nightChance : dayChance;
            if (Random.value > chance) return;
            var go = prefabAnimator.gameObject;
            go.SetActive(true);
            var b = go.GetComponent<Bandit>();
            b.home = transform.position;
            b.sightRange = sightRange;
        }
    }
}
