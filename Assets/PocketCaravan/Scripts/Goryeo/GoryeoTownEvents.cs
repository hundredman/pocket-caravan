using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 11/08 은샘: 고려 다섯 마을 장터의 작은 사건. 날마다 마을마다 많아야 하나, 낮(8~17시)에 장터에 들어서면 생긴다.
    /// - 급한 손님: 다른 마을 물건 두 개를 장터 판매가의 1.5배에 사 간다.
    /// - 달아난 닭: 장터 둘레를 도망 다니는 닭을 잡아 주인에게 데려다준다.
    /// - 흩어진 짐: 짐꾼이 떨어뜨린 꾸러미 세 개를 주워 준다.
    /// 어느 사건인지·생기는지는 날짜와 맵으로 정한다(Random 안 씀). 끝낸 날은 공용 메모 "gevent:맵"(값 = 날짜)에 남는다. 장면·저장 형식은 바꾸지 않는다.
    /// 사람 그림은 승인된 사람 NPC 정면 그림, 닭은 고려 생활 닭 그림, 꾸러미는 Tools/ArtGen/goryeo_life_props.py로 그린 그림.
    /// </summary>
    public sealed class GoryeoTownEvents : MonoBehaviour
    {
        static readonly string[] Towns = { "Gaegyeong", "Naru", "Dakmok", "goryeo_songak", "goryeo_jangdan" };
        // 다른 마을에서 가져와야 하는 물건 (이 마을 특산이면 건너뜀)
        static readonly string[] RushWants = { "salt", "paper", "ginseng", "grain", "transit_driedfish", "transit_resin" };
        const int RushCount = 2, ChickenReward = 12, SackReward = 15, SackCount = 3;

        enum EventKind { None, Rush, Chicken, Sacks }

        static GoryeoTownEvents instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() { instance = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (instance != null || FindFirstObjectByType<GoryeoTownEvents>() != null) return;
            var go = new GameObject("Goryeo town events");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GoryeoTownEvents>();
        }

        MapRoot checkedMap;
        int checkedDay = -1;
        Transform root;
        EventKind kind;
        Host host;
        Chicken chicken;
        readonly List<Sack> sacks = new List<Sack>();
        bool carryingChicken;
        int picked;
        ItemData rushItem;
        TownData town;

        void OnEnable() => SceneManager.sceneLoaded += OnLoaded;
        void OnDisable() => SceneManager.sceneLoaded -= OnLoaded;
        void OnLoaded(Scene s, LoadSceneMode m) { checkedMap = null; Clear(); }

        static GameManager GM => GameManager.I;
        static string DoneKey(string map) => "gevent:" + map;
        static int Hash(int a, int b) { unchecked { uint h = (uint)(a * 73856093) ^ (uint)(b * 19349663); h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15; return (int)(h & 0x7fffffff); } }

        void Update()
        {
            var map = MapRoot.Current;
            if (map == null || GM == null || GM.IsTravelling || PlayerController.Instance == null) return;
            int day = GM.Clock.day;
            if (map == checkedMap && day == checkedDay) return;
            if (UIState.Blocking || GoryeoIntro.Active) return;
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
            root = null; host = null; chicken = null; sacks.Clear(); kind = EventKind.None; carryingChicken = false; picked = 0; rushItem = null;
        }

        // ---------------- 세우기 ----------------

        void Begin(MapRoot map, int h)
        {
            root = new GameObject("Goryeo town event").transform;
            root.SetParent(map.transform, false);
            var market = FindFirstObjectByType<TownMarket>();
            var mc = map.WorldToCell(market.transform.position);
            if (kind == EventKind.Rush)
            {
                var wants = new List<ItemData>();
                foreach (var id in RushWants) { var it = GM.ItemById(id); if (it != null && it.originTown != town) wants.Add(it); }
                if (wants.Count == 0) { kind = EventKind.None; return; }
                rushItem = wants[h % wants.Count];
                if (!MakeHost(map, mc, "급한 손님", "npc_capital_peddler")) return;
                Notify.Show($"{town.displayName} 장터에 {Josa.Eul(rushItem.displayName)} 급히 찾는 손님이 있다!", 4f);
            }
            else if (kind == EventKind.Chicken)
            {
                if (!MakeHost(map, mc, "닭 주인 아낙", "npc_resident_ricecake")) return;
                var cell = FreeCell(map, mc, 4, 9, h);
                if (cell == null) { Clear(); return; }
                chicken = new GameObject("달아난 닭").AddComponent<Chicken>();
                chicken.transform.SetParent(root, false);
                chicken.transform.position = map.CellCenter(cell.Value);
                chicken.owner = this;
                Notify.Show($"{town.displayName} 장터에서 닭이 달아났다!", 4f);
            }
            else
            {
                if (!MakeHost(map, mc, "짐꾼 돌이", "npc_wood_seller")) return;
                var sprite = Resources.Load<Sprite>("Goryeo/Events/sack");
                for (int i = 0; i < SackCount; i++)
                {
                    var cell = FreeCell(map, mc, 3 + i * 2, 7 + i * 2, h + i * 31);
                    if (cell == null) continue;
                    var s = new GameObject("흩어진 꾸러미").AddComponent<Sack>();
                    s.transform.SetParent(root, false);
                    s.transform.position = map.CellCenter(cell.Value);
                    s.owner = this;
                    var sr = s.gameObject.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = 9;
                    sacks.Add(s);
                }
                if (sacks.Count == 0) { Clear(); return; }
                Notify.Show($"{town.displayName} 장터에 짐꾼이 꾸러미를 쏟았다!", 4f);
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
            sr.sprite = Resources.Load<Sprite>("Goryeo/Stalls/Keepers/" + sprite); sr.sortingOrder = 10;
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
                    bool near = false;
                    foreach (var it in Interactable.All) if (it != null && Mathf.Abs(it.Cell.x - c.x) <= 1 && Mathf.Abs(it.Cell.y - c.y) <= 1) { near = true; break; }
                    var pc = PlayerController.Instance;
                    if (!near && pc != null && map.WorldToCell(pc.transform.position) == c) near = true;
                    if (!near) list.Add(c);
                }
            if (list.Count == 0) return null;
            return list[Mathf.Abs(seed) % list.Count];
        }

        void Finish(string line)
        {
            GM.SetMemo(DoneKey(MapRoot.Current.mapId), GM.Clock.day);
            var label = host != null ? host.label : "";
            DialogueUI.Show(label, line);
            var r = root; root = null; kind = EventKind.None;
            if (r != null) Destroy(r.gameObject, 0.05f);
            host = null; chicken = null; sacks.Clear();
        }

        // ---------------- 말 걸기 ----------------

        void TalkHost()
        {
            var p = GM.Player;
            if (kind == EventKind.Rush)
            {
                int each = Mathf.Max(1, Mathf.RoundToInt(GM.GetMarket(town).SellPrice(rushItem) * 1.5f));
                int have = p.cargo.Count(rushItem);
                string ask = $"{rushItem.displayName} {RushCount}개만 구해 주시오! 오늘 안에 꼭 있어야 하오.\n하나에 {each}냥씩 쳐 드리리다. (장터 판매가의 1.5배 · 내 짐 {have}개)";
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
                    Finish($"살았소! 덕분에 일을 그르치지 않았구려. ({total}냥 · 손익 {(profitSum >= 0 ? "+" : "")}{Mathf.RoundToInt(profitSum)}냥)");
                }), new DialogueUI.Choice("다음에요", null));
            }
            else if (kind == EventKind.Chicken)
            {
                if (!carryingChicken) { DialogueUI.Show(host.label, "아이고, 우리 꼬꼬가 장터로 달아났어요! 잡아다 주면 사례할게요.\n(닭 가까이 가서 Space)"); return; }
                Reward(ChickenReward, "우리 꼬꼬! 고마워요. 오늘 낳은 알값이라도 받아 줘요.");
            }
            else
            {
                if (picked < SackCount && sacks.Count > 0) { DialogueUI.Show(host.label, $"꾸러미를 쏟아 버렸소… 장터 둘레에 흩어진 걸 주워 주면 고맙겠소. ({picked}/{sacks.Count + picked})"); return; }
                Reward(SackReward, "다 찾았구려! 이거 얼마 안 되지만 받으시오.");
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

        void CatchChicken()
        {
            if (chicken == null) return;
            Destroy(chicken.gameObject); chicken = null; carryingChicken = true;
            Sfx.Play("accept");
            Notify.Show("닭을 잡았다! 주인에게 데려다주자.", 3f);
        }

        void PickSack(Sack s)
        {
            sacks.Remove(s); Destroy(s.gameObject); picked++;
            Sfx.Play("accept");
            Notify.Show(sacks.Count > 0 ? $"꾸러미를 주웠다 ({picked}/{picked + sacks.Count})" : "꾸러미를 다 주웠다! 짐꾼에게 돌려주자.", 2.5f);
        }

        // ---------------- 대상 ----------------

        public sealed class Host : Interactable
        {
            public string label; public GoryeoTownEvents owner;
            public override string DisplayName => label;
            public override Kind Category => Kind.Quest;
            public override void Interact(PlayerController player) { if (owner != null) owner.TalkHost(); }
        }

        public sealed class Sack : Interactable
        {
            public GoryeoTownEvents owner;
            void Awake() => blocksMovement = false;
            public override string DisplayName => "꾸러미";
            public override string ActionVerb => "줍기";
            public override void Interact(PlayerController player) { if (owner != null) owner.PickSack(this); }
        }

        /// <summary>장터 둘레를 도망 다니는 닭. 가까이 오면 반대로 몇 칸 뛰고, 가끔 멈춰 모이를 쫀다(그때 잡기 쉽다)</summary>
        public sealed class Chicken : Interactable
        {
            public GoryeoTownEvents owner;
            SpriteRenderer sr; Sprite[] frames;
            Vector3 target; float rest, flap; Vector3Int home;
            void Awake() => blocksMovement = false;
            public override string DisplayName => "달아난 닭";
            public override string ActionVerb => "닭 잡기";
            public override Kind Category => Kind.Person;
            public override void Interact(PlayerController player) { if (owner != null) owner.CatchChicken(); }
            void Start()
            {
                sr = gameObject.AddComponent<SpriteRenderer>(); sr.sortingOrder = 10;
                frames = new[] { Resources.Load<Sprite>("Goryeo/VillageLife/Art/chicken_0"), Resources.Load<Sprite>("Goryeo/VillageLife/Art/chicken_1") };
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
                    sr.flipX = delta.x > 0;
                    sr.sprite = frames[(int)(flap * 8f) % 2];
                    return;
                }
                sr.sprite = frames[(int)(flap * 1.5f) % 2];
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
