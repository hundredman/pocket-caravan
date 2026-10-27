using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 베네치아 하루 생활 (2026-10-27, 제안). 맵에 들어올 때마다 그 맵의 생활 자리를 놓는다(VeniceLife.json, venice_life.py가 만든다).
    /// - 떠돌이 상인: 날마다 장터 마을 8곳 중 한 곳(VeniceMerchant). 오늘의 특가·동작 비법서·사자 문장 소문.
    /// - 벤치 앞 칸에 앉기(VeniceSeat), 여관 주인·숙소 주인 옆 '여관에서 쉬기'(VeniceInn), 석호 나루의 곤돌라 배(VeniceTraghetto).
    /// - 하루 처음 베네치아 맵에 들어오면 떠돌이 상인·특가 마을을 알린다.
    /// - 다음 이야기 길잡이: 진행 중인 베네치아 퀘스트가 없을 때 다음 퀘스트를 누가 주는지 화면 위에 한 줄.
    /// - 푹 쉼(여관): 8시간 동안 달릴 때 체력이 절반만 준다(공용 PlayerController.staminaDrain 값을 버프 동안만 바꿨다 되돌린다).
    /// 저장: VeniceSave (rested_until, deal_n 오늘 산 특가 수, announce 날짜).
    /// </summary>
    public class VeniceDaily : MonoBehaviour
    {
        public static readonly string[] MerchantMaps =
            { "venice_rialto", "venice_murano", "venice_c_landing", "venice_burano", "venice_c_post", "venice_chioggia", "venice_c_vineyard", "venice_santerasmo" };
        public static readonly string[] DealItems = { "venice_glass", "venice_salt", "venice_wine", "venice_fruit", "venice_lace", "venice_fish", "venice_glass_art" };
        public static readonly (string map, string name)[] Lagoon =
            { ("venice_rialto", "리알토"), ("venice_murano", "무라노"), ("venice_burano", "부라노"), ("venice_chioggia", "키오자"), ("venice_santerasmo", "산테라스모") };

        static int Day => GameManager.I != null ? GameManager.I.Clock.day : 0;
        public static string MerchantMap => MerchantMaps[(Day * 3 + 1) % MerchantMaps.Length];
        public static string DealItem => DealItems[(Day * 5 + 2) % DealItems.Length];
        public static string DealMap => MerchantMap;      // 특가는 떠돌이 상인이 판다

        public static float RestedUntil => VeniceSave.Get("rested_until", -1f);
        public static bool Rested => GameManager.I != null && GameManager.I.Clock.TotalHours < RestedUntil;

        Transform root;
        float baseDrain = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (GameManager.I != null && GameManager.I.GetComponent<VeniceDaily>() == null)
                GameManager.I.gameObject.AddComponent<VeniceDaily>();
        }

        void OnEnable() { SceneManager.sceneLoaded += OnLoaded; Build(); }
        void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; if (root != null) Destroy(root.gameObject); }
        void OnLoaded(Scene s, LoadSceneMode m) => Build();

        public static string MapName(string id)
        {
            var wi = WorldIndex.Get();
            return wi != null ? wi.MapName(id) : id;
        }

        void Build()
        {
            if (root != null) Destroy(root.gameObject);
            root = null;
            if (!VeniceUtil.InVenice || MapRoot.Current == null) return;
            var data = VeniceData.Get(VeniceUtil.Map);
            if (data == null) return;
            root = new GameObject("베네치아 하루 생활").transform;

            if (VeniceUtil.Map == MerchantMap && data.wander != null && data.wander.Length >= 2) Spot<VeniceMerchant>(data.wander, "떠돌이 상인");
            foreach (var p in VeniceData.Points(data.seats)) Spot<VeniceSeat>(new[] { p.x, p.y }, "벤치");
            if (data.dock != null && data.dock.Length >= 2) Spot<VeniceTraghetto>(data.dock, "곤돌라 나루");
            Invoke(nameof(PlaceInn), .1f);                 // 씬의 NPC가 다 켜진 뒤

            if (GameManager.I != null && !VeniceSave.Has("keyhint"))
            {
                VeniceSave.Set("keyhint", 1f);
                Notify.Show("베네치아: R 동작 · G 선물 · F 친구 수첩 · P 사진 · 벤치에서 Space로 앉기", 7f);
            }
            else if (GameManager.I != null && (int)VeniceSave.Get("announce", -1) != Day)
            {
                VeniceSave.Set("announce", Day);
                var item = VeniceUtil.Item(DealItem);
                Notify.Show($"오늘 떠돌이 상인은 {MapName(MerchantMap)}에. 특가: {(item != null ? item.displayName : DealItem)} 4할 싸게", 5f);
            }
        }

        void PlaceInn()
        {
            if (root == null || MapRoot.Current == null) return;
            Interactable host = null;                      // 먼저 찾고 나서 만든다 (목록을 도는 중에 새 Interactable이 들어가지 않게)
            foreach (var it in Interactable.All)
                if (it != null && (it.DisplayName == "여관 주인" || it.DisplayName == "숙소 주인")) { host = it; break; }
            if (host == null) return;
            var c = host.Cell;
            foreach (var d in new[] { Vector3Int.right, Vector3Int.left, new Vector3Int(1, -1, 0), new Vector3Int(-1, -1, 0), Vector3Int.down })
            {
                var cell = c + d;
                if (!MapRoot.Current.IsWalkable(cell) || Interactable.At(cell) != null) continue;
                var go = new GameObject("여관에서 쉬기");
                go.transform.SetParent(root, false);
                go.transform.position = MapRoot.Current.CellCenter(cell);
                go.AddComponent<VeniceInn>();
                return;
            }
        }

        T Spot<T>(int[] xy, string name) where T : Interactable
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = VeniceUtil.Cell(xy[0], xy[1]);
            return go.AddComponent<T>();
        }

        void Update()
        {
            var pc = PlayerController.Instance;
            if (pc == null) return;
            if (baseDrain < 0f) baseDrain = pc.staminaDrain;
            pc.staminaDrain = !VeniceUtil.InVenice ? baseDrain : Rested ? baseDrain * .5f : VeniceSeat.Breather ? baseDrain * .75f : baseDrain;
        }

        // ---------------- 다음 이야기 길잡이 ----------------
        static readonly (string npc, string name)[] Givers =
        {
            ("venice_innkeeper", "여관 주인"), ("venice_clerk", "상인회 서기"), ("venice_gardener", "과수원 일꾼"),
            ("venice_glassmaker", "유리 장인"), ("venice_fisher", "부라노 어부"), ("venice_warehouse", "창고지기"),
        };

        static string NextStory()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Quests == null) return null;
            foreach (var p in gm.Quests.Active) if (p.quest != null && p.quest.id.StartsWith("venice_")) return null;
            QuestData best = null;
            foreach (var q in gm.Db.quests)
                if (q != null && q.id != null && q.id.StartsWith("venice_q") && gm.Quests.CanStart(q) && (best == null || string.CompareOrdinal(q.id, best.id) < 0))
                    best = q;
            if (best == null) return null;
            string who = best.giverNpc;
            foreach (var g in Givers) if (g.npc == best.giverNpc) who = g.name;
            var map = WorldIndex.Get()?.MapOf("npc", best.giverNpc);
            return $"다음 이야기 「{best.title}」 — {who}{(map != null ? $" ({MapName(map)})" : "")}";
        }

        void OnGUI()
        {
            if (!VeniceUtil.InVenice || UIState.Blocking || GameManager.I == null || GameManager.I.IsTravelling) return;
            var text = NextStory();
            if (Rested) text = (text != null ? text + "   " : "") + "<color=#9FE0A0>푹 쉼</color>";
            if (text == null) return;
            UIStyle.Begin();
            GUI.depth = -5;
            // 왼쪽 위 상태 창 바로 아래 (가운데 위는 알림, 가운데 아래는 공용 '목표' 줄이 쓴다)
            var content = new GUIContent($"<size=9><color=#FCE6A8>{text}</color></size>");
            float w = Mathf.Min(260f, UIStyle.Rich.CalcSize(content).x + 10f);
            var r = new Rect(4, 94, w, 14);
            var c = GUI.color; GUI.color = new Color(0f, 0f, 0f, .5f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = c;
            GUI.Label(new Rect(r.x + 5, r.y - 1, w, 16), content, UIStyle.Rich);
        }
    }
}
