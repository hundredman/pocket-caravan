using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 돌아가며 열리는 큰 장 — 서쪽 조각 장식 (2026-10-26, 2026-11-04 규칙은 공용 CentralFair로 옮김). 팀 공통 규칙 제안(Docs/NorthFair.md, 북유럽 브랜치)을 따른다.
    /// 1) 하루에 한 곳: 게임 날짜 % 3 → 대상 숙소 → 본토 나루터 → 포도밭 양조장.
    /// 2) 큰 장 마을 장터에서 팔면 판 값의 2할을 덤으로 더 준다(GameEvents.ItemSold를 듣고 돈을 더함). 사는 값은 그대로.
    /// 3) 큰 장 마을 장터 둘레에 줄무늬 좌판·깃발 줄을 더 세운다.
    /// 4) 본토 서부에 들어서면 하루 한 번 "오늘 큰 장: ○○" 알림.
    /// 통합 뒤 "중앙 전체에서 하루 한 곳"으로 바꾸면 Towns 목록에 다른 조각 장터를 더하면 된다. 공용 코드는 고치지 않는다.
    /// </summary>
    public class VeniceFair : MonoBehaviour
    {
        // 2026-11-04 통합: 어디서 언제 서는지·덤·알림·화면 띠는 공용 CentralFair. 여기는 서쪽 장식(줄무늬 좌판·깃발)만.
        static float Bonus => VeniceLegacy.LionBlessed ? .25f : .2f;   // 사자 문장 일곱 개를 다 찾으면 2할 5푼
        Transform deco;

        public static CentralFair.Town Today => CentralFair.Today("west");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            CentralFair.BonusOverride["west"] = () => Bonus;
            if (GameManager.I != null && GameManager.I.GetComponent<VeniceFair>() == null)
                GameManager.I.gameObject.AddComponent<VeniceFair>();
        }

        void OnEnable() { SceneManager.sceneLoaded += OnLoaded; Decorate(); }
        void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; }

        void OnLoaded(Scene s, LoadSceneMode m)
        {
            Decorate();
        }

        void Decorate()
        {
            if (deco != null) Destroy(deco.gameObject);
            deco = null;
            if (VeniceUtil.Map != Today.mapId) return;
            var market = VeniceData.Get(VeniceUtil.Map)?.market;
            if (market == null || market.Length < 2) return;
            deco = new GameObject("큰 장 장식").transform;
            var at = VeniceUtil.Cell(market[0], market[1]);
            void Put(string sprite, Vector3 offset, int order)
            {
                var go = new GameObject(sprite);
                go.transform.SetParent(deco, false);
                go.transform.position = at + offset;
                var r = go.AddComponent<SpriteRenderer>();
                r.sprite = VeniceUtil.Sprite(sprite);
                r.sortingOrder = order;
            }
            Put("fair_stall", new Vector3(-3f, 1.2f, 0f), 9);
            Put("fair_stall", new Vector3(3.5f, 1.2f, 0f), 9);
            Put("bunting", new Vector3(-1f, 2.6f, 0f), 21);
            Put("bunting", new Vector3(1f, 2.6f, 0f), 21);
        }
    }
}
