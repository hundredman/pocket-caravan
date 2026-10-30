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

        static readonly string[] Tasks =
        {
            "Shift를 누른 채 걸어서 달려 보기",
            "Tab을 눌러 내 상태 보기",
            "I를 눌러 소지품 주머니 열어 보기",
            "M을 눌러 지도 펼쳐 보기",
            "Q를 눌러 할 일(퀘스트) 목록 보기",
            "리알토 장터에서 아무 물건이나 하나 사기",
            "산 물건을 장터에 다시 팔아 보기",
            "L을 눌러 장부에서 방금 거래 기록 확인하기",
        };

        public static bool Active => Step >= 1 && Step <= 10;
        static int Step { get => Mathf.RoundToInt(VeniceSave.Get("intro")); set => VeniceSave.Set("intro", value); }

        Transform root;
        float runTime;
        GUIStyle style;

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
        }

        void OnDisable()
        {
            GameEvents.MapEntered -= OnMap;
            GameEvents.ItemBought -= OnBought;
            GameEvents.ItemSold -= OnSold;
            InventoryUI.Opened -= OnInventory;
        }

        void Start() { if (VeniceUtil.Map == Home) OnMap(Home); }

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
                    "잘했어요! 같은 장터에서 사서 바로 되팔면 손해였죠? 산지 값과 납품 값의 차이, 그게 장사예요.\n" +
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
        void OnBought(ItemData item, TownData town, int price) { if (IsCat) Done(5); }
        void OnSold(ItemData item, TownData town, int price, int profit) { if (IsCat) Done(6); }

        void Update()
        {
            int s = Step;
            if (s < 2 || s > 9 || !IsCat || GameManager.I == null) return;
            if (s == 2 && !UIState.Blocking && InputReader.Run() && InputReader.MoveVector().sqrMagnitude > 0.1f)
            {
                runTime += Time.unscaledDeltaTime;
                if (runTime > 1f) { runTime = 0f; Done(0); }
            }
            else if (s == 3 && InputReader.StatusPressed()) Done(1);
            else if (s == 5 && InputReader.MapPressed()) Done(3);
            else if (s == 6 && InputReader.QuestLogPressed()) Done(4);
            else if (s == 9 && InputReader.LedgerPressed()) Done(7);
        }

        void OnGUI()
        {
            int s = Step;
            if (s < 1 || s > 10 || UIState.Blocking || !IsCat || !VeniceUtil.InVenice) return;
            UIStyle.Begin();
            style ??= new GUIStyle(UIStyle.Rich) { alignment = TextAnchor.MiddleCenter };
            string t = s == 1 ? "바로 옆 <b>상인회 안내원</b>에게 Space로 말을 걸어 보세요"
                     : s == 10 ? "할 일을 다 했어요! <b>안내원 조반나</b>에게 돌아가 말을 걸어 보세요"
                     : $"<color=#ffde59>배우기 {s - 1}/{Tasks.Length}</color>  {Tasks[s - 2]}";
            float w = Mathf.Min(420, UIStyle.VirtualWidth - 20);
            var r = new Rect((UIStyle.VirtualWidth - w) / 2, 30, w, 20);
            GUI.Box(r, "", UIStyle.Box);
            GUI.Label(r, t, style);
        }
    }
}
