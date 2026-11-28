using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace PocketCaravan
{
    /// <summary>
    /// 11/28 아라비아 다섯 마을 장터의 작은 사건 (고려 ArabiaTownEvents와 같은 규칙, 아라비아에는 손으로 돕는 일이 없어서 넣음).
    /// 날마다 마을마다 많아야 하나, 낮(8~17시)에 마을에 들어서면 생긴다. 안 해도 손해는 없다.
    /// - 급한 길손: 다른 마을 물건 두 개를 장터 판매가의 1.5배에 사 간다.
    /// - 달아난 양: 장터 둘레를 도망 다니는 양을 붙잡아 주인에게 데려다준다.
    /// - 쏟아진 자루: 짐꾼이 떨어뜨린 향신료 자루 세 개를 주워 준다.
    /// 어느 사건인지·생기는지는 날짜와 맵으로 정한다(Random 안 씀). 끝낸 날은 공용 메모 "aevent:맵"(값 = 날짜)에 남는다. 씬·저장 형식은 바꾸지 않는다.
    /// 그림은 이미 있는 아라비아 그림을 Tools/ArtGen/arabia_events_art.py로 옮긴 것(Resources/Arabia/Events).
    /// </summary>
    public sealed class ArabiaTownEvents : MonoBehaviour
    {
        static readonly string[] Towns = { "arabia_ahsa", "arabia_zabid", "arabia_taiz", "arabia_jeddah", "arabia_sanaa" };
        // 다른 마을에서 가져와야 하는 물건 (이 마을 특산이면 건너뜀)
        static readonly string[] RushWants = { "arabia_dates", "arabia_cloth", "arabia_spice", "arabia_coffee", "arabia_frankincense", "arabia_leather" };
        const int RushCount = 2, SheepReward = 12, SackReward = 15, SackCount = 3;

        enum EventKind { None, Rush, Sheep, Sacks }

        static ArabiaTownEvents instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() { instance = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (instance != null || FindFirstObjectByType<ArabiaTownEvents>() != null) return;
            var go = new GameObject("Arabia town events");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<ArabiaTownEvents>();
        }

        MapRoot checkedMap;
        int checkedDay = -1;
        Transform root;
        EventKind kind;
        Host host;
        Sheep sheep;
        readonly List<Sack> sacks = new List<Sack>();
        bool carryingSheep;
        int picked;
        ItemData rushItem;
        TownData town;

        void OnEnable() => SceneManager.sceneLoaded += OnLoaded;
        void OnDisable() => SceneManager.sceneLoaded -= OnLoaded;
        void OnLoaded(Scene s, LoadSceneMode m) { checkedMap = null; Clear(); }

        static GameManager GM => GameManager.I;
        static string DoneKey(string map) => "aevent:" + map;
        static int Hash(int a, int b) { unchecked { uint h = (uint)(a * 73856093) ^ (uint)(b * 19349663); h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15; return (int)(h & 0x7fffffff); } }

        void Update()
        {
            var map = MapRoot.Current;
            if (map == null || GM == null || GM.IsTravelling || PlayerController.Instance == null) return;
            int day = GM.Clock.day;
            if (map == checkedMap && day == checkedDay) return;
            if (UIState.Blocking || ArabiaIntro.Active) return;
            int hour = GM.Clock.Hour;
            if (hour < 8 || hour >= 17) return;   // 낮에 장터에 들어설 때만 (그 맵·그 날에 한 번 정한다)
            checkedMap = map; checkedDay = day;
            Clear();
            int ti = System.Array.IndexOf(Towns, map.mapId);
            if (ti < 0 || Mathf.RoundToInt(GM.GetMemo(DoneKey(map.mapId), -1f)) == day) return;
            int h = Hash(day, ti + 7);
            if (h % 100 >= 65) return;            // 열에 여섯쯤만 생긴다
            town = FindFirstObjectByType<TownMarket>()?.town;
            if (town == null) return;
            kind = (EventKind)(1 + (h / 100) % 3);
            Begin(map, h);
        }

        void Clear()
        {
            if (root != null) Destroy(root.gameObject);
            root = null; host = null; sheep = null; sacks.Clear(); kind = EventKind.None; carryingSheep = false; picked = 0; rushItem = null;
        }

        // ---------------- 세우기 ----------------

        void Begin(MapRoot map, int h)
        {
            root = new GameObject("Arabia town event").transform;
            root.SetParent(map.transform, false);
            var market = FindFirstObjectByType<TownMarket>();
            var mc = map.WorldToCell(market.transform.position);
            if (kind == EventKind.Rush)
            {
                var wants = new List<ItemData>();
                foreach (var id in RushWants) { var it = GM.ItemById(id); if (it != null && it.originTown != town) wants.Add(it); }
                if (wants.Count == 0) { kind = EventKind.None; return; }
                rushItem = wants[h % wants.Count];
                if (!MakeHost(map, mc, "급한 길손", "traveler")) return;
                Notify.Show($"{town.displayName} 장터에 {Josa.Eul(rushItem.displayName)} 급히 찾는 길손이 있다!", 4f);
            }
            else if (kind == EventKind.Sheep)
            {
                if (!MakeHost(map, mc, "양 주인 할머니", "shepherd")) return;
                var cell = FreeCell(map, mc, 4, 9, h);
                if (cell == null) { Clear(); return; }
                sheep = new GameObject("달아난 양").AddComponent<Sheep>();
                sheep.transform.SetParent(root, false);
                sheep.transform.position = map.CellCenter(cell.Value);
                sheep.owner = this;
                Notify.Show($"{town.displayName} 장터에서 양이 달아났다!", 4f);
            }
            else
            {
                if (!MakeHost(map, mc, "향신료 짐꾼", "porter")) return;
                for (int i = 0; i < SackCount; i++)
                {
                    var cell = FreeCell(map, mc, 3 + i * 2, 7 + i * 2, h + i * 31);
                    if (cell == null) continue;
                    var s = new GameObject("쏟아진 자루").AddComponent<Sack>();
                    s.transform.SetParent(root, false);
                    s.transform.position = map.CellCenter(cell.Value);
                    s.owner = this;
                    var sr = s.gameObject.AddComponent<SpriteRenderer>(); sr.sprite = Resources.Load<Sprite>(i == 1 ? "Arabia/Events/sack_pepper" : "Arabia/Events/sack_spice"); sr.sortingOrder = 9;
                    sacks.Add(s);
                }
                if (sacks.Count == 0) { Clear(); return; }
                Notify.Show($"{town.displayName} 장터에서 짐꾼이 향신료 자루를 쏟았다!", 4f);
            }
        }

        bool MakeHost(MapRoot map, Vector3Int mc, string name, string sprite)
        {
            var cell = FreeCell(map, mc, 2, 4, 0);
            if (cell == null) { Clear(); return false; }
            host = new GameObject(name).AddComponent<Host>();
            host.transform.SetParent(root, false);
            host.transform.position = map.CellCenter(cell.Value);
            host.label = name; host.owner = this;
            var sr = host.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Arabia/Events/" + sprite); sr.sortingOrder = 10;
            return true;
        }

        /// <summary>장터에서 min~max칸 떨어진, 걷는 칸이고 다른 대상과 겹치지 않는 칸 (seed로 고른다)</summary>
        static Vector3Int? FreeCell(MapRoot map, Vector3Int mc, int min, int max, int seed)
        {
            var list = new List<Vector3Int>();
            for (int dy = -max; dy <= max; dy++)
                for (int dx = -max; dx <= max; dx++)
                {
                    int d = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (d < min || d > max) continue;
                    var c = new Vector3Int(mc.x + dx, mc.y + dy, mc.z);
                    if (!map.IsWalkable(c) || MapExit.At(c) != null) continue;
                    if (!Clean(map, c)) continue;
                    bool near = false;
                    foreach (var it in Interactable.All) if (it != null && Mathf.Abs(it.Cell.x - c.x) <= 1 && Mathf.Abs(it.Cell.y - c.y) <= 1) { near = true; break; }
                    var pc = PlayerController.Instance;
                    if (!near && pc != null && map.WorldToCell(pc.transform.position) == c) near = true;
                    if (!near) list.Add(c);
                }
            if (list.Count == 0) return null;
            return list[Mathf.Abs(seed) % list.Count];
        }

        static Tilemap[] layers; static MapRoot layersOf;
        /// <summary>사람·물건을 세워도 어색하지 않은 칸: 양탄자·소품·나무 그늘 같은 꾸밈 타일이 없고, 좌판·건물 바로 앞(막힌 칸 바로 아래)이 아니고, 경비병 순찰길이 아닌 칸</summary>
        static bool Clean(MapRoot map, Vector3Int c)
        {
            if (layersOf != map) { layersOf = map; layers = map.GetComponentsInChildren<Tilemap>(); }
            foreach (var tm in layers)
                if (tm != null && tm != map.ground && tm != map.road && tm != map.blocked && tm.HasTile(c)) return false;
            var floor = map.ground != null ? map.ground.GetTile(c) : null;
            if (floor != null && floor.name.Contains("tzellige")) return false;   // 무늬 타일 마당(분수 광장) 위에는 두지 않는다
            if (!map.IsWalkable(c + Vector3Int.up)) return false;
            foreach (var patrol in FindObjectsByType<ArabiaPatrol>(FindObjectsSortMode.None))
                if (patrol != null) foreach (var pt in patrol.path) if (map.WorldToCell(pt) == c) return false;
            return true;
        }

        void Finish(string line)
        {
            GM.SetMemo(DoneKey(MapRoot.Current.mapId), GM.Clock.day);
            var label = host != null ? host.label : "";
            DialogueUI.Show(label, line);
            var r = root; root = null; kind = EventKind.None;
            if (r != null) Destroy(r.gameObject, 0.05f);
            host = null; sheep = null; sacks.Clear();
        }

        // ---------------- 말 걸기 ----------------

        void TalkHost()
        {
            var p = GM.Player;
            if (kind == EventKind.Rush)
            {
                int each = Mathf.Max(1, Mathf.RoundToInt(GM.GetMarket(town).SellPrice(rushItem) * 1.5f));
                int have = p.cargo.Count(rushItem);
                string ask = $"{rushItem.displayName} {RushCount}개만 구해 줄 수 있소? 해 지기 전에 대상이 떠나서 급하오.\n하나에 {each}냥씩 쳐 드리겠소. (장터 판매가의 1.5배 · 내 짐 {have}개)";
                if (have < RushCount) { DialogueUI.Show(host.label, ask, new DialogueUI.Choice("구해 올게요", null)); return; }
                DialogueUI.Show(host.label, ask, new DialogueUI.Choice($"{RushCount}개 건네기 ({each * RushCount}냥)", () =>
                {
                    int total = 0; float profitSum = 0;
                    bool ok = SavedEconomyChange.Try(GM, () =>
                    {
                        for (int i = 0; i < RushCount; i++)
                        {
                            float cost = p.cargo.AverageCost(rushItem);
                            int profit = Mathf.RoundToInt(each - cost);
                            p.cargo.Remove(rushItem); p.money += each; total += each; profitSum += profit;
                            p.ledger.Record(true, town, rushItem, each, profit, GM.Clock.day);
                            GameEvents.RaiseItemSold(rushItem, town, each, profit);
                        }
                        GM.SetMemo(DoneKey(MapRoot.Current.mapId), GM.Clock.day);
                    });
                    if (!ok) { Notify.Show(SavedEconomyChange.Failure); return; }
                    Sfx.Play("coin");
                    Finish($"살았소! 덕분에 대상을 놓치지 않겠소. ({total}냥 · 손익 {(profitSum >= 0 ? "+" : "")}{Mathf.RoundToInt(profitSum)}냥)");
                }), new DialogueUI.Choice("다음에요", null));
            }
            else if (kind == EventKind.Sheep)
            {
                if (!carryingSheep) { DialogueUI.Show(host.label, "아이고, 우리 양이 장터로 달아났어요! 붙잡아다 주면 사례할게요.\n(양 가까이 가서 Space)"); return; }
                Reward(SheepReward, "아이고, 요 녀석! 고마워요. 젖 짠 값이라도 받아 줘요.");
            }
            else
            {
                if (picked < SackCount && sacks.Count > 0) { DialogueUI.Show(host.label, $"수레가 기우는 바람에 자루를 쏟았어요… 장터 둘레에 흩어진 걸 주워 주면 고맙겠어요. ({picked}/{sacks.Count + picked})"); return; }
                Reward(SackReward, "다 찾았네요! 얼마 안 되지만 받아 줘요.");
            }
        }

        void Reward(int money, string line)
        {
            var p = GM.Player;
            if (!SavedEconomyChange.Try(GM, () => { p.money += money; GM.SetMemo(DoneKey(MapRoot.Current.mapId), GM.Clock.day); }))
            { Notify.Show(SavedEconomyChange.Failure); return; }
            Sfx.Play("coin");
            Finish(line + $" (+{money}냥)");
        }

        void CatchSheep()
        {
            if (sheep == null) return;
            Destroy(sheep.gameObject); sheep = null; carryingSheep = true;
            Sfx.Play("accept");
            Notify.Show("양을 붙잡았다! 주인에게 데려다주자.", 3f);
        }

        void PickSack(Sack s)
        {
            sacks.Remove(s); Destroy(s.gameObject); picked++;
            Sfx.Play("accept");
            Notify.Show(sacks.Count > 0 ? $"자루를 주웠다 ({picked}/{picked + sacks.Count})" : "자루를 다 주웠다! 짐꾼에게 돌려주자.", 2.5f);
        }

        // ---------------- 대상 ----------------

        public sealed class Host : Interactable
        {
            public string label; public ArabiaTownEvents owner;
            public override string DisplayName => label;
            public override Kind Category => Kind.Quest;
            public override void Interact(PlayerController player) { if (owner != null) owner.TalkHost(); }
        }

        public sealed class Sack : Interactable
        {
            public ArabiaTownEvents owner;
            void Awake() => blocksMovement = false;
            public override string DisplayName => "향신료 자루";
            public override string ActionVerb => "줍기";
            public override void Interact(PlayerController player) { if (owner != null) owner.PickSack(this); }
        }

        /// <summary>장터 둘레를 도망 다니는 양. 가까이 오면 반대로 몇 칸 달아나고, 달아난 다음엔 잠깐 멈춘다(그때 붙잡기 쉽다)</summary>
        public sealed class Sheep : Interactable
        {
            public ArabiaTownEvents owner;
            SpriteRenderer sr; Sprite[] frames;
            Vector3 target; float rest, flap; Vector3Int home;
            void Awake() => blocksMovement = false;
            public override string DisplayName => "달아난 양";
            public override string ActionVerb => "붙잡기";
            public override Kind Category => Kind.Person;
            public override void Interact(PlayerController player) { if (owner != null) owner.CatchSheep(); }
            void Start()
            {
                sr = gameObject.AddComponent<SpriteRenderer>(); sr.sortingOrder = 10;
                frames = new[] { Resources.Load<Sprite>("Arabia/Events/sheep_0"), Resources.Load<Sprite>("Arabia/Events/sheep_1") };
                sr.sprite = frames[0]; target = transform.position;
                home = MapRoot.Current != null ? MapRoot.Current.WorldToCell(transform.position) : Vector3Int.zero;
            }
            void Update()
            {
                var map = MapRoot.Current; var pc = PlayerController.Instance;
                if (map == null || pc == null || UIState.Blocking) return;
                flap += Time.deltaTime;
                Vector3 me = transform.position, delta = target - me;
                if (delta.magnitude > 0.02f)
                {
                    transform.position = me + Vector3.ClampMagnitude(delta, 3.2f * Time.deltaTime);
                    sr.flipX = delta.x < 0;   // 양 그림은 오른쪽을 본다
                    sr.sprite = frames[(int)(flap * 8f) % 2];
                    return;
                }
                sr.sprite = frames[0];   // 멈추면 가만히 선다
                if (rest > 0) { rest -= Time.deltaTime; return; }
                float dist = Vector2.Distance(pc.transform.position, me);
                if (dist > 3.2f) return;
                // 플레이어 반대쪽으로 2~3칸. 집(처음 자리)에서 7칸 넘게는 안 간다
                var cur = map.WorldToCell(me);
                Vector3Int best = cur; float bestScore = -999;
                foreach (var d in new[] { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0), new Vector3Int(1, 1, 0), new Vector3Int(-1, 1, 0), new Vector3Int(1, -1, 0), new Vector3Int(-1, -1, 0) })
                {
                    var c = cur;
                    for (int k = 0; k < 2; k++) { var n = c + d; if (!map.IsWalkable(n) || MapExit.At(n) != null || Mathf.Abs(n.x - home.x) + Mathf.Abs(n.y - home.y) > 7) break; c = n; }
                    if (c == cur) continue;
                    float score = Vector2.Distance(map.CellCenter(c), pc.transform.position);
                    if (score > bestScore) { bestScore = score; best = c; }
                }
                target = map.CellCenter(best);
                rest = 0.9f;   // 뛴 다음엔 잠깐 멈춘다
            }
        }
    }
}
