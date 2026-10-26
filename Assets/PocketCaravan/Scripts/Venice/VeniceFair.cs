using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 돌아가며 열리는 큰 장 — 서쪽 조각(본토 서부) (2026-10-26). 팀 공통 규칙 제안(Docs/NorthFair.md, 북유럽 브랜치)을 따른다.
    /// 1) 하루에 한 곳: 게임 날짜 % 3 → 대상 숙소 → 본토 나루터 → 포도밭 양조장.
    /// 2) 큰 장 마을 장터에서 팔면 판 값의 2할을 덤으로 더 준다(GameEvents.ItemSold를 듣고 돈을 더함). 사는 값은 그대로.
    /// 3) 큰 장 마을 장터 둘레에 줄무늬 좌판·깃발 줄을 더 세운다.
    /// 4) 본토 서부에 들어서면 하루 한 번 "오늘 큰 장: ○○" 알림.
    /// 통합 뒤 "중앙 전체에서 하루 한 곳"으로 바꾸면 Towns 목록에 다른 조각 장터를 더하면 된다. 공용 코드는 고치지 않는다.
    /// </summary>
    public class VeniceFair : MonoBehaviour
    {
        // (장터 마을 id, 그 장터가 있는 맵 id, 이름)
        static readonly (string town, string map, string name)[] Towns =
        {
            ("venice_c_caravanserai", "venice_c_post", "대상 숙소"),
            ("venice_c_landing", "venice_c_landing", "본토 나루터"),
            ("venice_c_vineyard", "venice_c_vineyard", "포도밭 양조장"),
        };
        static float Bonus => VeniceLion.Blessed ? .25f : .2f;   // 사자 문장 일곱 개를 다 찾으면 2할 5푼
        Transform deco;

        public static (string town, string map, string name) Today =>
            Towns[(GameManager.I != null ? GameManager.I.Clock.day : 0) % Towns.Length];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (GameManager.I != null && GameManager.I.GetComponent<VeniceFair>() == null)
                GameManager.I.gameObject.AddComponent<VeniceFair>();
        }

        void OnEnable() { SceneManager.sceneLoaded += OnLoaded; Hook(); Decorate(); }
        void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; GameEvents.ItemSold -= OnSold; }

        void OnLoaded(Scene s, LoadSceneMode m)
        {
            Hook();          // 새 게임에서 GameEvents.ClearAll이 구독을 지우므로 씬마다 다시 건다
            Decorate();
            Announce();
        }

        void Hook()
        {
            GameEvents.ItemSold -= OnSold;
            GameEvents.ItemSold += OnSold;
        }

        void OnSold(ItemData item, TownData town, int price, int profit)
        {
            if (town == null || town.id != Today.town || price <= 0) return;
            int bonus = Mathf.Max(1, Mathf.RoundToInt(price * Bonus));
            GameManager.I.Player.money += bonus;
            Notify.Show($"큰 장 덤 +{bonus}냥 ({Today.name})", 2.5f);
        }

        void Announce()
        {
            if (!VeniceUtil.InMainland || VeniceSave.DoneToday("fair_notice")) return;
            VeniceSave.MarkToday("fair_notice");
            Notify.Show($"오늘 큰 장: {Today.name} — 팔면 2할 덤", 5f);
        }

        void Decorate()
        {
            if (deco != null) Destroy(deco.gameObject);
            deco = null;
            if (VeniceUtil.Map != Today.map) return;
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

        void OnGUI()
        {
            if (deco == null) return;
            UIStyle.Begin();
            var r = new Rect(UIStyle.VirtualWidth / 2f - 70f, 34f, 140f, 18f);
            GUI.Box(r, "", UIStyle.Box);
            GUI.Label(r, "  오늘 큰 장 · 팔면 2할 덤", UIStyle.Rich);
        }
    }
}
