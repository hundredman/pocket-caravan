using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 전시실 기증 (전 지역 공용, 2026-11-05 통합: 북유럽 NorthDaily 회관 전시실 + 베네치아 VeniceMuseum).
    /// 지역마다 전시실 한 곳. 진열 목록(6칸)을 하나씩 채우면 되고, 다 채우면 칭호 + 200냥 + 신용 +1.
    /// - 북유럽·베네치아: 자리는 지역 코드가 예전처럼 세우고(Shelf에 자기 목록과 옛 저장 키를 등록), 기증 창은 공용.
    /// - 고려·아라비아: 진열 목록은 그 지역 산지 물건 6개(데이터에서 자동), 자리는 으뜸 마을(개경·사나) 장터 옆에 공용 Runner가 세운다.
    ///   지역 담당이 이름·자리·그림을 바꾸고 싶으면 Shelf/Place에 등록하면 된다.
    /// 저장: 공용 메모 "museum:지역:물건" (북 "north:museum:키", 베 "venice:museum:id"는 KeyFor로 그대로 이어받음).
    /// </summary>
    public static class Museum
    {
        public const int Reward = 200;

        /// <summary>진열 한 칸. item이 있으면 짐에서 1개 빼서 기증, 없으면 have/take로 특별 조건(북 룬 탁본 등).</summary>
        public class Entry
        {
            public string key, name, item;
            public Func<bool> have;       // item이 비었을 때 기증 가능한지
            public Action take;           // 기증 시 할 일 (없어도 됨)
        }

        public static readonly Dictionary<Reactions.Region, Entry[]> Shelf = new Dictionary<Reactions.Region, Entry[]>();
        /// <summary>저장 키 규칙 (옛 저장 호환용). 없으면 "museum:지역:키"</summary>
        public static readonly Dictionary<Reactions.Region, Func<string, string>> KeyFor = new Dictionary<Reactions.Region, Func<string, string>>();
        /// <summary>다 채웠을 때 지역이 더 할 일 (북: 리액션 '기쁨' 가르치기)</summary>
        public static readonly Dictionary<Reactions.Region, Action> OnComplete = new Dictionary<Reactions.Region, Action>();
        /// <summary>칭호</summary>
        public static readonly Dictionary<Reactions.Region, string> Title = new Dictionary<Reactions.Region, string>
        {
            { Reactions.Region.North, "북쪽의 큐레이터" }, { Reactions.Region.Venice, "전시실 후원자" },
            { Reactions.Region.Goryeo, "개경 보물 지킴이" }, { Reactions.Region.Arabia, "사나 향료 수집가" },
        };
        /// <summary>공용 Runner가 자리를 세울 맵 (지역 코드가 직접 세우는 북·베는 등록하지 않는다)</summary>
        public static readonly Dictionary<Reactions.Region, string> Place = new Dictionary<Reactions.Region, string>
        {
            { Reactions.Region.Goryeo, "Gaegyeong" }, { Reactions.Region.Arabia, "arabia_sanaa" },
        };
        /// <summary>11/08 기우: 장터 옆이 붐비면 자리를 맵 JSON 좌표(위가 y=0)로 고정한다. 그 칸이 막혀 있으면 예전처럼 장터 옆</summary>
        public static readonly Dictionary<Reactions.Region, Vector2Int> FixedCell = new Dictionary<Reactions.Region, Vector2Int>
        {
            { Reactions.Region.Arabia, new Vector2Int(40, 27) },   // 사나 광장 오른쪽 좌판 앞 (왼쪽 장터에 사나 상인·나디아·전시실이 몰려 이름표가 겹쳐서)
        };
        public static readonly Dictionary<Reactions.Region, string> Keeper = new Dictionary<Reactions.Region, string>
        {
            { Reactions.Region.North, "회관 전시실 관리인" }, { Reactions.Region.Venice, "전시실 관리인" },
            { Reactions.Region.Goryeo, "개경 보물창고 지기" }, { Reactions.Region.Arabia, "사나 향료관 지기" },
        };

        static string Key(Reactions.Region r, string key) => KeyFor.TryGetValue(r, out var f) && f != null ? f(key) : $"museum:{(int)r}:{key}";
        static bool Has(Reactions.Region r, string key) => GameManager.I != null && GameManager.I.HasMemo(Key(r, key));

        public static Entry[] Entries(Reactions.Region r)
        {
            if (Shelf.TryGetValue(r, out var e) && e != null && e.Length > 0) return e;
            // 기본: 이 지역 산지 물건(originTown이 이 지역) 중 이름순 6개
            var gm = GameManager.I;
            if (gm == null) return new Entry[0];
            var list = new List<ItemData>();
            foreach (var it in gm.Db.items)
                if (it != null && !it.questItem && it.originTown != null && it.originTown.region != null && RegionOf(it.originTown.region) == r) list.Add(it);
            list.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            int n = Mathf.Min(6, list.Count);
            var arr = new Entry[n];
            for (int i = 0; i < n; i++) arr[i] = new Entry { key = list[i].id, name = list[i].displayName, item = list[i].id };
            Shelf[r] = arr;   // 한 번 정하면 고정 (저장 키가 흔들리지 않게)
            return arr;
        }

        static Reactions.Region RegionOf(RegionData rd)
        {
            string id = rd != null ? rd.id : "";
            if (id.StartsWith("north") || id.StartsWith("center_north") || id == "centerNorth") return Reactions.Region.North;
            if (id.StartsWith("venice") || id.StartsWith("center_venice")) return Reactions.Region.Venice;
            if (id.StartsWith("arabia")) return Reactions.Region.Arabia;
            return Reactions.Region.Goryeo;
        }

        public static int Count(Reactions.Region r) { int n = 0; foreach (var e in Entries(r)) if (Has(r, e.key)) n++; return n; }
        public static int Total(Reactions.Region r) => Entries(r).Length;
        public static bool Complete(Reactions.Region r) => Total(r) > 0 && Count(r) >= Total(r);
        public static string Label(Reactions.Region r) => $"전시실 ({Count(r)}/{Total(r)})";

        static bool CanGive(Entry e)
        {
            var gm = GameManager.I;
            if (!string.IsNullOrEmpty(e.item)) { var it = gm.ItemById(e.item); return it != null && gm.Player.cargo.Count(it) > 0; }
            return e.have != null && e.have();
        }

        public static void Open(Reactions.Region r)
        {
            var gm = GameManager.I;
            var entries = Entries(r);
            string keeper = Keeper.TryGetValue(r, out var k) ? k : "전시실 관리인";
            if (entries.Length == 0) { DialogueUI.Show(keeper, "아직 진열할 목록이 정해지지 않았네."); return; }
            var lines = new List<string>();
            var choices = new List<DialogueUI.Choice>();
            foreach (var e in entries)
            {
                bool done = Has(r, e.key);
                lines.Add((done ? "■ " : "□ ") + e.name);
                if (done || choices.Count >= 3 || !CanGive(e)) continue;
                var ex = e;
                choices.Add(new DialogueUI.Choice($"{e.name} 기증하기", () => Donate(r, ex)));
            }
            string head = Complete(r) ? "모두 채웠네. 자네 이름이 후원자 판에 올랐지." : "이 땅의 보물을 모으고 있다네. 자네가 찾은 걸 기증해 주면 길이길이 전하겠네.";
            if (!Complete(r) && choices.Count == 0) head += "\n(빈 칸에 놓을 물건을 가져오면 받겠네.)";
            choices.Add(new DialogueUI.Choice("구경만 할게요", null));
            DialogueUI.Show(keeper, head + "\n\n" + string.Join("\n", lines), choices.ToArray());
        }

        static void Donate(Reactions.Region r, Entry e)
        {
            var gm = GameManager.I;
            if (!string.IsNullOrEmpty(e.item))
            {
                var it = gm.ItemById(e.item);
                if (it == null || !gm.Player.cargo.Remove(it, 1)) return;
            }
            else if (e.have == null || !e.have()) return;
            e.take?.Invoke();
            gm.SetMemo(Key(r, e.key), 1f);
            Sfx.Play("success");
            string doneKey = Key(r, "complete");
            if (Complete(r) && !gm.HasMemo(doneKey))
            {
                gm.SetMemo(doneKey, 1f);
                gm.Player.money += Reward; gm.Player.credit += 1;
                Sfx.Play("levelup");
                if (OnComplete.TryGetValue(r, out var more)) more?.Invoke();
                string title = Title.TryGetValue(r, out var t) ? t : "전시실 후원자";
                Notify.Show($"전시실이 다 찼다! '{title}' 상금 {Reward}냥, 신용 +1", 6f);
            }
            else Notify.Show($"'{e.name}'을(를) 기증했다 ({Count(r)}/{Total(r)})", 3f);
        }

        /// <summary>전시실 자리 (지역 코드가 세우거나, 고려·아라비아는 Runner가 세운다)</summary>
        public class Spot : Interactable
        {
            public Reactions.Region region;
            public override string DisplayName => Label(region);
            public override Kind Category => Kind.Board;
            public override void Interact(PlayerController player) => Open(region);
        }

        class Runner : MonoBehaviour
        {
            void OnEnable() { SceneManager.sceneLoaded += OnLoaded; }
            void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; }
            void OnLoaded(Scene s, LoadSceneMode m)
            {
                if (GameManager.I == null || MapRoot.Current == null) return;
                var r = Reactions.Here;
                if (!Place.TryGetValue(r, out var map) || map != MapRoot.Current.mapId) return;
                var mr = MapRoot.Current;
                var market = UnityEngine.Object.FindAnyObjectByType<TownMarket>();
                if (market == null) return;
                var c = mr.WorldToCell(market.transform.position);
                var road = mr.GetComponent<RoadGrid>();
                if (FixedCell.TryGetValue(r, out var fc) && road != null && road.height > 0)
                {
                    var fixedCell = new Vector3Int(fc.x, road.height - 1 - fc.y, c.z);   // JSON 좌표 → 씬 (MapJsonBuilder처럼 위아래 뒤집힘)
                    if (mr.IsWalkable(fixedCell) && Interactable.At(fixedCell) == null) { Build(fixedCell); return; }
                }
                int[] dx = { -2, 2, -3, 3, -2, 2, 0 }, dy = { 1, 1, 0, 0, -1, -1, 2 };
                for (int i = 0; i < dx.Length; i++)
                {
                    var cell = new Vector3Int(c.x + dx[i], c.y + dy[i], c.z);
                    if (!mr.IsWalkable(cell) || Interactable.At(cell) != null) continue;
                    Build(cell);
                    return;
                }
                Debug.LogWarning($"[전시실] {map}: 장터 옆에 세울 자리가 없다");

                void Build(Vector3Int cell)
                {
                    var go = new GameObject("전시실");
                    go.transform.position = mr.CellCenter(cell);
                    var sp = go.AddComponent<Spot>(); sp.region = r; sp.blocksMovement = true;
                    var t = Resources.Load<Texture2D>("Common/pedestal");
                    if (t != null) { t.filterMode = FilterMode.Point; var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.1f), 16f); sr.sortingOrder = 9; }
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (UnityEngine.Object.FindFirstObjectByType<Runner>() != null) return;
            var go = new GameObject("Museum");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
        }
    }
}
