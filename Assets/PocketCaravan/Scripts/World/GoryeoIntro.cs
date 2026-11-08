using System.Collections;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 2026-10-30 시작 이야기 + 조작 튜토리얼 (너구리 · 고려 개경상인). 북유럽·베네치아·아라비아 Intro와 같은 흐름,
    /// 대사는 「고려 한 바퀴」 초안(Docs/IntroStory-2026-10-30.md) 프롤로그·0장 「길 떠날 채비」를 따랐다(1~5장은 기존 Q1 그대로).
    /// 1) 나루에 처음 들어오면 프롤로그 자막("나루에는 아침마다 짐이 내린다…")
    /// 2) 바로 옆 선착장 일꾼에게 Space로 말 걸기 → 달리기 · Tab · Q · 보따리(I) · 지도(M) · 사기 · 팔기 · 장부(L)
    /// 3) 일꾼에게 돌아가면 "같은 물건도 필요한 곳에 가져가면 값이 달라진다" → 나루 촌장(1장 q1_naru)으로 보냄
    /// 진행은 GameManager 진행 메모 "goryeo:intro". 새 파일만 더했고 기존 고려 파일·씬·퀘스트는 그대로다.
    /// 그림은 같은 맵의 뱃사공 그림을 실행 중에 빌려 쓴다(새 아트 없음).
    /// 이미 고려 퀘스트를 시작했거나 다른 문명 캐릭터면 나오지 않는다.
    /// </summary>
    public class GoryeoIntro : MonoBehaviour
    {
        const string Home = "Naru";
        const int MapH = 84;   // Naru.json 높이 (맵 칸은 위가 0)
        const string FirstQuest = "q1_naru";
        static readonly Vector2Int GuideCell = new Vector2Int(40, 33);   // 시작 자리(S0 (37,32)) 오른쪽 아래
        const string Guide = "선착장 일꾼";

        static readonly string[] Tasks =
        {
            "Shift를 누른 채 걸어서 달려 보기",
            "Tab을 눌러 내 상태 보기",
            "Q를 눌러 할 일(퀘스트) 목록 보기",
            "I를 눌러 보따리(소지품) 열어 보기",
            "M을 눌러 지도 펼치고 장단 찾아보기",
            "나루 장터에서 아무 물건이나 하나 사기",
            "산 물건을 장터에 다시 팔아 보기",
            "L을 눌러 장부에서 방금 거래 기록 확인하기",
        };

        public static bool Active => Step >= 1 && Step <= 10;
        static int Step { get => GameManager.I != null ? Mathf.RoundToInt(GameManager.I.GetMemo("goryeo:intro")) : 0; set { if (GameManager.I != null) GameManager.I.SetMemo("goryeo:intro", value); } }
        static bool Started => GameManager.I != null && GameManager.I.HasMemo("goryeo:intro");
        static Vector3 Cell(int x, int y) => MapRoot.Current.CellCenter(new Vector3Int(x, MapH - 1 - y, 0));
        static bool InGoryeo => GoryeoTravelTransition.RegionOf(MapRoot.Current != null ? MapRoot.Current.mapId : null) != null;

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
            if (FindFirstObjectByType<GoryeoIntro>() != null) return;
            var go = new GameObject("GoryeoIntro");
            DontDestroyOnLoad(go);
            go.AddComponent<GoryeoIntro>();
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

        void Start() { if (MapRoot.Current != null && MapRoot.Current.mapId == Home) OnMap(Home); }

        static bool IsCat => GameManager.I != null && GameManager.I.Player.origin != null && GameManager.I.Player.origin.id == "goryeo";

        static bool FreshStart()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Quests == null || !IsCat || Started) return false;
            foreach (var p in gm.Quests.Active) if (p != null && p.quest != null && p.quest.id != null && (p.quest.id.StartsWith("q1_") || p.quest.id.StartsWith("tutorial_"))) return false;
            var q = gm.Db.quests.Find(x => x != null && x.id == FirstQuest);
            return q != null && gm.Quests.CanStart(q);
        }

        void OnMap(string map)
        {
            if (root != null) Destroy(root.gameObject);
            root = null;
            if (map != Home || !IsCat) return;
            root = new GameObject("Goryeo Intro").transform;
            var go = new GameObject("Intro Guide");
            go.transform.SetParent(root, false);
            go.transform.position = Cell(GuideCell.x, GuideCell.y);
            var p = go.AddComponent<Person>();
            p.blocksMovement = false;
            p.label = () => Step == 1 || Step == 10 ? Guide + " !" : Guide;
            p.act = TalkGuide;
            Sprite s = BorrowSprite("뱃사공");
            if (s != null) { var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = s; sr.sortingOrder = 10; }
            if (FreshStart()) StartCoroutine(Arrive());
        }

        IEnumerator Arrive()
        {
            Step = 1;
            yield return new WaitForSecondsRealtime(0.4f);
            while (GameManager.I != null && GameManager.I.IsTravelling) yield return null;
            DialogueUI.Show("나루의 아침",
                "나루에는 아침마다 짐이 내린다.\n어떤 것은 장터에 남고, 어떤 것은 먼 마을로 떠난다.\n\n" +
                "오늘, 새내기 행상 한 사람이 선착장에 섰다.",
                new DialogueUI.Choice("선착장으로", () => Notify.Show("바로 옆 선착장 일꾼에게 다가가 Space로 말을 걸어 보세요.", 5f)));
        }

        /// <summary>같은 맵에 서 있는 사람(이름으로 찾음)의 지금 그림을 빌린다. 없으면 null</summary>
        static Sprite BorrowSprite(string name)
        {
            foreach (var it in Interactable.All)
            {
                if (it == null || it.DisplayName != name) continue;
                var sr = it.GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.sprite != null) return sr.sprite;
            }
            return null;
        }

        void TalkGuide()
        {
            int s = Step;
            if (s == 1)
            {
                DialogueUI.Show(Guide,
                    "구경만 할 텐가, 아니면 자네도 한 짐 맡아 보겠나? 처음부터 큰 짐을 지는 사람은 없지.\n" +
                    "자, 짐을 챙겨 둘 보따리하고 마을 이름을 적어 둔 지도일세.\n\n" +
                    "길 떠나기 전에 채비부터 하세. 하나씩 해 보고 다시 오게.",
                    new DialogueUI.Choice("해 볼게요", () => { Step = 2; Notify.Show("보따리와 지도를 받았다!", 3f); Sfx.Play("coin"); }));
                return;
            }
            if (s >= 2 && s <= 9) { DialogueUI.Show(Guide, $"채비가 아직 덜 됐네.\n\n지금 할 일: {Tasks[s - 2]}"); return; }
            if (s == 10)
            {
                DialogueUI.Show(Guide,
                    "장부에 얼마를 주고 샀고 얼마를 받았는지 남아 있지? 그 차이를 봐야 해. 같은 장터에서 되팔면 손해가 나기도 하고.\n" +
                    "같은 물건도 필요한 곳에 가져가면 값이 달라진다네. 그걸 찾는 게 행상의 일이지.\n" +
                    "그리고 맡은 짐을 제때 전하면 신용이 쌓여. 신용 있는 사람에게 더 좋은 일감이 오는 법이야.",
                    new DialogueUI.Choice("첫 짐은 어디서 받아요?", () => DialogueUI.Show(Guide,
                        "나루 촌장님께 가 보게. 배로 들어온 소금이 있는데, 장단 농부가 기다린다더군.\n\n" +
                        "장단, 송악, 닥목을 거쳐 개경까지 한 바퀴 돌고 나면 자네도 어엿한 상인일세.",
                        new DialogueUI.Choice("다녀올게요!", Finish))));
                return;
            }
            DialogueUI.Show(Guide, "길이 헷갈리면 지도(M)를, 값이 헷갈리면 장부(L)를 보게. 좋은 장삿길 되게!");
        }

        void Finish()
        {
            Step = 11;
            var gm = GameManager.I;
            if (gm != null) gm.Player.money += 20;
            Sfx.Play("coin");
            Notify.Show("일꾼이 노잣돈 20냥을 건넸다 · 다음: 나루 촌장「소금 한 꾸러미」", 5f);
        }

        void Done(int task)
        {
            if (Step != task + 2) return;
            Step = task + 3;
            Sfx.Play("coin");
            Notify.Show(Step <= 9 ? $"잘했어요! 다음: {Tasks[Step - 2]}" : "다 했어요! 선착장 일꾼에게 돌아가 말을 걸어 보세요.", 4f);
        }

        void OnInventory() { if (IsCat) Done(3); }
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
            else if (s == 4 && InputReader.QuestLogPressed()) Done(2);
            else if (s == 6 && InputReader.MapPressed()) Done(4);
            else if (s == 9 && InputReader.LedgerPressed()) Done(7);
        }

        public Rect GuideRect { get; private set; }
        public string GuideText { get; private set; }
        void OnGUI()
        {
            int s = Step;
            if (s < 1 || s > 10 || UIState.Blocking || !IsCat || !InGoryeo) return;
            UIStyle.Begin();
            GUI.depth = -5;
            style ??= new GUIStyle() { font=UIStyle.SmallFont, fontSize=UIStyle.SmallSize,
                alignment=TextAnchor.UpperLeft,wordWrap=true,richText=false,
                clipping=TextClipping.Clip,padding=new RectOffset(0,0,0,0) };
            string title=s==1?"처음 배우기":s==10?"배우기 완료":$"배우기 {s-1}/{Tasks.Length}";
            string t = s == 1 ? "선착장 일꾼에게 말 걸기 (Space)"
                     : s == 10 ? "선착장 일꾼에게 돌아가 말 걸기 (Space)"
                     : Tasks[s - 2];
            const float w=168, padding=8;
            float textHeight=Mathf.Ceil(style.CalcHeight(new GUIContent(t),w-2*padding));
            // Compact quest tracker at the lower-right screen edge.
            float h=30+textHeight;
            // 11/08 아래 쌓는 줄(조작 안내·목표 길잡이) 위로 올리고, 차지한 자리를 알려 알림이 비키게 한다
            var r=new Rect(Mathf.Round(UIStyle.VirtualWidth-w),Mathf.Round(UIStyle.VirtualHeight-h-4-HUD.BottomReserved),w,h);
            GuideRect=r;GuideText=t;
            ScreenZones.Mark("goryeo.intro",r);
            CuteUI.Card(r);
            style.normal.textColor=style.hover.textColor=style.active.textColor=style.focused.textColor=UIStyle.Lin(CuteUI.Sub);
            GUI.Label(new Rect(r.x+padding,r.y+padding,w-2*padding,14),title,style);
            style.normal.textColor=style.hover.textColor=style.active.textColor=style.focused.textColor=UIStyle.Lin(CuteUI.Text);
            GUI.Label(new Rect(r.x+padding,r.y+22,w-2*padding,textHeight),t,style);
        }
    }
}
