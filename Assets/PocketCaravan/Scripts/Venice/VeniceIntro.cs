using System.Collections;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 2026-10-30 시작 이야기 + 조작 튜토리얼 (흰 고양이 · 베네치아 상인). 북유럽 NorthIntro와 같은 흐름, 이야기는 윤채 님 「베네치아 스토리」 정리를 따랐다.
    /// 1) 리알토에 처음 들어오면 날개 달린 사자 이야기 → 리알토 부두 광장에 도착한 새내기 행상
    /// 2) 바로 옆 상인회 안내원에게 Space로 말 걸기 → 지도·소지품 주머니를 받고 할 일을 차례로:
    ///    달리기(Shift) · 상태(Tab) · 소지품(I) · 지도(M) · 퀘스트 목록(Q) · 장터에서 사기 · 팔기 · 장부(L)
    /// 3) 안내원에게 돌아가면 돈·신용·부탁, 베네치아 출신의 강점, 숨은 사자 문장 이야기 → 여관 주인(1장 첫 배달 venice_q1_delivery)으로 보냄
    /// 진행은 VeniceSave "intro" (0 없음 · 1 말 걸기 · 2~9 할 일 · 10 돌아가기 · 11 끝). 새 파일만 더했고 기존 베네치아 파일·씬은 그대로다.
    /// 이미 베네치아 퀘스트를 시작했거나 다른 문명 캐릭터면 나오지 않는다.
    /// </summary>
    public class VeniceIntro : MonoBehaviour
    {
        const string Home = "venice_rialto";
        const string FirstQuest = "venice_q1_delivery";
        static readonly Vector2Int GuideCell = new Vector2Int(32, 39);   // 시작 자리(S0 (30,39)) 바로 오른쪽
        const string Guide = "상인회 안내원 조반나";

        static readonly string[] Tasks = ControlLesson.Tasks();   // 11/08 공통화 4단계: 조작 배우기 여덟 가지는 공용 ControlLesson

        public static bool Active => Step >= 1 && Step <= 10;
        static int Step { get => Mathf.RoundToInt(VeniceSave.Get("intro")); set => VeniceSave.Set("intro", value); }

        Transform root;
        float runTime;

        class Person : Interactable
        {
            public System.Func<string> label; public System.Action act;
            public override string DisplayName => label != null ? label() : "";
            public override Kind Category => Kind.Person;
            public override void Interact(PlayerController player) => act?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<VeniceIntro>() != null) return;
            var go = new GameObject("VeniceIntro");
            DontDestroyOnLoad(go);
            go.AddComponent<VeniceIntro>();
        }

        void OnEnable()
        {
            GameEvents.MapEntered += OnMap;
            GameEvents.ItemBought += OnBought;
            GameEvents.ItemSold += OnSold;
            InventoryUI.Opened += OnInventory;
            if (!Navigator.Tutorials.Contains(NavHint)) Navigator.Tutorials.Add(NavHint);
        }

        void OnDisable()
        {
            GameEvents.MapEntered -= OnMap;
            GameEvents.ItemBought -= OnBought;
            GameEvents.ItemSold -= OnSold;
            InventoryUI.Opened -= OnInventory;
            Navigator.Tutorials.Remove(NavHint);
        }

        void Start() { if (VeniceUtil.Map == Home) OnMap(Home); }

        // 11/11 은샘: 북유럽·베네치아 첫걸음 안내에는 길 안내 화살표 연결(Navigator.Tutorials)이 없어서,
        //   사기·팔기 단계에서 화살표가 뜨지 않았다 (기우·윤채 시연 확인). 고려·아라비아 첫걸음 안내(NavHint)와 같은 방식:
        //   1·10단계 조반나, 사기 단계는 이 맵 장터(없으면 가장 가까운 베네치아 장터), 팔기 단계는 산 마을이 아닌 가장 가까운 베네치아 장터.
        Navigator.Hint? NavHint()
        {
            if (!Active || !IsCat || StartScreen.IsOpen || MapRoot.Current == null) return null;
            int s = Step;
            if (s == 1 || s == 10)
            {
                var guide = root != null ? root.GetComponentInChildren<Person>() : null;
                if (guide != null) return new Navigator.Hint { target = guide, label = Guide };
                return new Navigator.Hint { kind = "map", id = Home, label = Guide };
            }
            if (s != 7 && s != 8) return null;
            string avoid = s == 8 ? BuyTownId() : null;
            foreach (var it in Interactable.All)
                if (it is TownMarket m && m.isActiveAndEnabled && m.town != null && m.town.id != avoid) return new Navigator.Hint { target = it, label = $"{TownName(m.town.id)} 장터 상인" };
            string best = NearestMarket(avoid);
            return best != null ? new Navigator.Hint { kind = "market", id = best } : (Navigator.Hint?)null;
        }

        /// <summary>사기 단계에서 산 마을 (모르면 null)</summary>
        static string BuyTownId()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Db == null || !gm.HasMemo(BuyTownMemo)) return null;
            int i = Mathf.RoundToInt(gm.GetMemo(BuyTownMemo));
            return i >= 0 && i < gm.Db.towns.Count && gm.Db.towns[i] != null ? gm.Db.towns[i].id : null;
        }

        static string TownName(string townId)
        {
            var index = WorldIndex.Get();
            string map = index != null ? index.MapOf("market", townId) : null;
            return map != null ? index.MapName(map) : townId;
        }

        static string cachedFrom, cachedAvoid, cachedBest; static float cachedAt = -9f;

        /// <summary>지금 맵에서 맵을 건너는 수가 가장 적은 베네치아 장터 (avoid 마을은 빼고). 1초마다 다시 잰다</summary>
        static string NearestMarket(string avoid)
        {
            var index = WorldIndex.Get(); var map = MapRoot.Current;
            if (index == null || map == null) return null;
            if (cachedFrom == map.mapId && cachedAvoid == avoid && Time.unscaledTime - cachedAt < 1f) return cachedBest;
            string best = null; int bestHops = int.MaxValue;
            foreach (var p in index.places)
            {
                if (p.kind != "market" || p.id == avoid || string.IsNullOrEmpty(p.mapId) || !p.mapId.StartsWith("venice_")) continue;
                int h = Hops(index, map.mapId, p.mapId);
                if (h >= 0 && h < bestHops) { bestHops = h; best = p.id; }
            }
            cachedFrom = map.mapId; cachedAvoid = avoid; cachedBest = best; cachedAt = Time.unscaledTime;
            return best;
        }

        static int Hops(WorldIndex index, string from, string to)
        {
            if (from == to) return 0;
            var seen = new System.Collections.Generic.Dictionary<string, int> { [from] = 0 };
            var q = new System.Collections.Generic.Queue<string>(); q.Enqueue(from);
            while (q.Count > 0)
            {
                var cur = q.Dequeue(); var info = index.Map(cur); if (info == null) continue;
                foreach (var n in info.neighbors)
                {
                    if (seen.ContainsKey(n)) continue;
                    seen[n] = seen[cur] + 1; if (n == to) return seen[n]; q.Enqueue(n);
                }
            }
            return -1;
        }

        static bool IsCat => GameManager.I != null && GameManager.I.Player.origin != null && GameManager.I.Player.origin.id == "venice";

        static bool FreshStart()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Quests == null || !IsCat || VeniceSave.Has("intro")) return false;
            foreach (var p in gm.Quests.Active) if (p != null && p.quest != null && p.quest.id != null && p.quest.id.StartsWith("venice_")) return false;
            var q = gm.Db.quests.Find(x => x != null && x.id == FirstQuest);
            return q != null && gm.Quests.CanStart(q);
        }

        void OnMap(string map)
        {
            if (root != null) Destroy(root.gameObject);
            root = null;
            if (map != Home || !IsCat) return;
            root = new GameObject("Venice Intro").transform;
            var go = new GameObject("Intro Guide");
            go.transform.SetParent(root, false);
            go.transform.position = VeniceUtil.Cell(GuideCell.x, GuideCell.y);
            var p = go.AddComponent<Person>();
            p.blocksMovement = false;
            p.label = () => Step == 1 || Step == 10 ? Guide + " !" : Guide;
            p.act = TalkGuide;
            var s = VeniceUtil.Sprite("pilgrim");
            if (s != null) { var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = s; sr.sortingOrder = 10; }
            if (FreshStart()) StartCoroutine(Arrive());
        }

        IEnumerator Arrive()
        {
            Step = 1;
            yield return new WaitForSecondsRealtime(0.4f);
            while (GameManager.I != null && GameManager.I.IsTravelling) yield return null;
            DialogueUI.Show("이야기",
                "석호의 안개가 걷히면, 리알토 곳곳에 새겨진 날개 달린 사자가 깨어난다는 옛말이 있다.\n" +
                "사자 문장 일곱 개를 모두 찾은 상인은 바다 건너 어디든 길이 열린다고.\n\n" +
                "빈 지게 하나 메고 리알토 부두에 내린 새내기 행상. 오늘이 장사 첫날이다.",
                new DialogueUI.Choice("광장으로", () => Notify.Show("바로 옆 상인회 안내원에게 다가가 Space로 말을 걸어 보세요.", 5f)));
        }

        void TalkGuide()
        {
            int s = Step;
            if (s == 1)
            {
                DialogueUI.Show(Guide,
                    "어서 와요, 새내기 행상님! 상인회에서 처음 오는 분들을 안내하고 있어요.\n" +
                    "자, 리알토 지도와 소지품 주머니예요. 빈 지게라도 이건 꼭 있어야죠.\n\n" +
                    "장사길에 오르기 전에 몸에 익혀 둘 것들이 있어요. 하나씩 해 보고 다시 오세요.",
                    new DialogueUI.Choice("해 볼게요", () => { Step = 2; Notify.Show("지도와 소지품 주머니를 받았다!", 3f); Sfx.Play("coin"); }));
                return;
            }
            if (s >= 2 && s <= 9) { DialogueUI.Show(Guide, $"아직 할 일이 남았어요.\n\n지금 할 일: {Tasks[s - 2]}"); return; }
            if (s == 10)
            {
                DialogueUI.Show(Guide,
                    "잘했어요! 산 곳과 판 곳의 값이 달랐죠? 산지 값과 납품 값의 차이, 그게 장사예요.\n" +
                    "석호의 다섯 섬은 저마다 특산품이 달라요. 무라노의 유리, 부라노의 생선, 키오자의 소금, 산테라스모의 과일.\n" +
                    "좋은 거래를 하려면 신용이 필요하고, 신용은 사람들의 부탁을 들어주며 쌓여요.",
                    new DialogueUI.Choice("베네치아 상인은 뭐가 좋아요?", () => DialogueUI.Show(Guide,
                        "우리 베네치아 상인은 다른 지역 물건을 팔아도 값이 덜 떨어져요. 멀리서 사 온 물건으로 크게 남기는 게 우리 장기죠.\n" +
                        "참, 섬 곳곳에 숨은 날개 달린 사자 문장도 찾아보세요. 일곱 개를 다 찾은 상인은 아직 없대요.\n\n" +
                        "첫 일거리는 광장 위쪽 여관 주인께 여쭤보세요. 빈 지게에서 손수레까지, 응원할게요!",
                        new DialogueUI.Choice("다녀올게요!", Finish))));
                return;
            }
            DialogueUI.Show(Guide, "길이 헷갈리면 지도(M)를, 시세가 헷갈리면 장부(L)를 보세요. 좋은 장사 하세요!");
        }

        void Finish()
        {
            Step = 11;
            var gm = GameManager.I;
            if (gm != null) gm.Player.money += 20;
            Sfx.Play("coin");
            Notify.Show("상인회 환영 선물 20냥을 받았다 · 다음: 여관 주인의 첫 배달", 5f);
        }

        void Done(int task)
        {
            if (Step != task + 2) return;
            Step = task + 3;
            Sfx.Play("coin");
            Notify.Show(Step <= 9 ? $"잘했어요! 다음: {Tasks[Step - 2]}" : "다 했어요! 안내원 조반나에게 돌아가 말을 걸어 보세요.", 4f);
        }

        void OnInventory() { if (IsCat) Done(2); }
        // 11/08 "산 물건을 다른 마을 상인에게 판매하기": 산 마을(Db.towns 번호)을 기억해 두고, 그 마을이 아닌 곳에서 팔아야 끝난다
        //   (예전엔 아무 판매로나 끝나, 산 곳에서 되팔아도 넘어갔다). 이 메모가 없는 예전 저장은 아무 판매로나 끝난다
        const string BuyTownMemo = "venice:intro:buytown";

        void OnBought(ItemData item, TownData town, int price)
        {
            if (!IsCat) return;
            var gm = GameManager.I;
            if (Step == 7 && gm != null && gm.Db != null) gm.SetMemo(BuyTownMemo, gm.Db.towns.IndexOf(town));
            Done(5);
        }

        void OnSold(ItemData item, TownData town, int price, int profit)
        {
            if (!IsCat || Step != 8) return;
            var gm = GameManager.I;
            if (gm != null && gm.Db != null && gm.HasMemo(BuyTownMemo) && gm.Db.towns.IndexOf(town) == Mathf.RoundToInt(gm.GetMemo(BuyTownMemo)))
            {
                Notify.Show("산 마을이 아닌 다른 마을 상인에게 팔아 보세요", 3f);
                return;
            }
            Done(6);
        }

        void Update()
        {
            int s = Step;
            if (s < 2 || s > 9 || !IsCat || GameManager.I == null) return;
            int task = ControlLesson.KeyTask(s, ref runTime);   // 11/08 공용 키 확인 (달리기·Tab·M·Q·L)
            if (task >= 0) Done(task);
        }

        /// <summary>위 가운데 배우기 알약이 떠 있다 (구역 이름 알약이 그 아래로 비킨다)</summary>
        /// <remarks>11/09 윤채: 베네치아 밖에서도 띄운다. "다른 마을에서 팔기"를 하러 나가면 알약이 사라져 튜토리얼이 끝난 줄 알았다</remarks>
        public static bool Showing { get { int s = Step; return s >= 1 && s <= 10 && !UIState.Blocking && !HUD.Expanded && IsCat && MapRoot.Current != null; } }   // 11/08 Tab 펼친 동안은 숨긴다 (상태 카드와 겹쳤다)

        void OnGUI()
        {
            if (!Showing) return;
            int s = Step;
            UIStyle.Begin();
            // 11/07 UI B안: 할 일 줄과 같은 위 가운데 알약 (전구 = 배우기, 오른쪽 작은 글 = 몇 번째). 위 알약들 아래 빈 자리에
            // 11/09 리알토를 떠나 있으면 앞에 "튜토리얼"을 붙이고, 안내원에게 가야 하는 단계는 어디로 가는지 알려 준다
            bool home = VeniceUtil.Map == Home;
            string main = s == 1 ? "상인회 안내원에게 말 걸기"
                        : s == 10 ? "안내원 조반나에게 돌아가기"
                        : CuteUI.Plain(Tasks[s - 2]);
            string where = s == 1 ? (home ? "바로 옆 · Space" : "리알토 부두 광장")
                         : s == 10 ? (home ? "다 했어요!" : "리알토 부두 광장 · 지도(M)")
                         : $"배우기 {s - 1}/{Tasks.Length}";
            // 11/10 길면 줄이지 않고 두 줄로 편다 (위 작은 줄 "튜토리얼 · 배우기 n/8", 아래 굵은 할 일)
            CuteUI.StackPill(10, false, CuteUI.Pic.Bulb, home ? null : "튜토리얼", main, where, !home && (s == 1 || s == 10));
        }
    }
}
