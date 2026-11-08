using System.Collections;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 2026-10-30 시작 이야기 + 조작 튜토리얼 (너구리 · 고려 개경상인). 북유럽·베네치아·아라비아 Intro와 같은 흐름,
    /// 대사는 「고려 한 바퀴」 초안(Docs/IntroStory-2026-10-30.md) 프롤로그·0장 「길 떠날 채비」를 따랐다(1~5장은 기존 Q1 그대로).
    /// 1) 나루에 처음 들어오면 선착장 일꾼이 부르는 하단 이야기
    /// 2) 바로 옆 선착장 일꾼에게 Space로 말 걸기 → 달리기 · Tab · 보따리(I) · 지도(M) · Q · 사기 · 팔기 · 장부(L)
    /// 3) 일꾼에게 돌아가면 "같은 물건도 필요한 곳에 가져가면 값이 달라진다" → 나루 촌장(1장 q1_naru)으로 보냄
    /// 진행은 GameManager 진행 메모 "goryeo:intro". 기존 퀘스트/ID/보상을 보존한다.
    /// 그림은 같은 맵의 뱃사공 그림을 실행 중에 빌려 쓴다(새 아트 없음).
    /// 이미 고려 퀘스트를 시작했거나 다른 문명 캐릭터면 나오지 않는다.
    /// </summary>
    public class GoryeoIntro : MonoBehaviour
    {
        const string Home = "Naru";
        const string FirstQuest = "q1_naru";
        const string Guide = "선착장 일꾼";

        static readonly string[] Tasks =
        {
            "Shift를 누른 채 걸어서 달려 보기",
            "Tab을 눌러 내 상태 보기",
            "I를 눌러 보따리(소지품) 열어 보기",
            "M을 눌러 지도 펼치고 장단 찾아보기",
            "Q를 눌러 할 일(퀘스트) 목록 보기",
            "상인에게 물건 하나 구매하기",
            "산 물건을 다른 마을 상인에게 판매하기",
            "L을 눌러 장부에서 방금 거래 기록 확인하기",
        };

        public static bool Active => Step >= 1 && Step <= 10;
        static int Step { get => GameManager.I != null ? Mathf.RoundToInt(GameManager.I.GetMemo("goryeo:intro")) : 0; set { if (GameManager.I != null) GameManager.I.SetMemo("goryeo:intro", value); } }
        static bool Started => GameManager.I != null && GameManager.I.HasMemo("goryeo:intro");
        [System.Serializable] class Anchor { public string mapId; public int width,height,x,y; }
        // Legacy saves completed Q before I/M. Keep that completion while teaching the new order.
        static void MigrateOrder()
        {
            var gm=GameManager.I;
            if(!IsCat||gm.HasMemo("goryeo:intro_order"))return;
            if(Started)
            {
                int old=Step;
                if(old==5||old==6){gm.SetMemo("goryeo:intro_q_done",1);Step=old-1;}
            }
            gm.SetMemo("goryeo:intro_order",2);
        }
        static bool GuidePosition(out Vector3 position)
        {
            position=default;
            var asset=Resources.Load<TextAsset>("Goryeo/Intro");var map=MapRoot.Current;
            if(asset==null||map==null)return false;
            var a=JsonUtility.FromJson<Anchor>(asset.text);var grid=map.GetComponent<RoadGrid>();
            if(a==null||a.mapId!=map.mapId||grid==null||grid.width!=a.width||grid.height!=a.height)return false;
            position=map.CellCenter(new Vector3Int(a.x,a.height-1-a.y,0));return true;
        }
        static bool InGoryeo => GoryeoTravelTransition.RegionOf(MapRoot.Current != null ? MapRoot.Current.mapId : null) != null;

        Transform root;
        Coroutine arrival;
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
            CancelArrival();
            GameEvents.MapEntered -= OnMap;
            GameEvents.ItemBought -= OnBought;
            GameEvents.ItemSold -= OnSold;
            InventoryUI.Opened -= OnInventory;
        }

        void Start() { if (root == null && MapRoot.Current != null && MapRoot.Current.mapId == Home) OnMap(Home); }

        static bool IsCat => GameManager.I != null && GameManager.I.Player.origin != null && GameManager.I.Player.origin.id == "goryeo";

        static bool FreshStart()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Quests == null || !IsCat) return false;
            // Recover saves affected by the old picker race, without replaying for
            // players who already started the lessons or acknowledged this opening.
            if (Started && (Step != 1 || gm.HasMemo("goryeo:intro_opening"))) return false;
            foreach (var p in gm.Quests.Active) if (p != null && p.quest != null && p.quest.id != null && (p.quest.id.StartsWith("q1_") || p.quest.id.StartsWith("tutorial_"))) return false;
            var q = gm.Db.quests.Find(x => x != null && x.id == FirstQuest);
            return q != null && gm.Quests.CanStart(q);
        }

        void CancelArrival()
        {
            if(arrival!=null)StopCoroutine(arrival);
            arrival=null;
        }

        void OnMap(string map)
        {
            CancelArrival();
            if (root != null) Destroy(root.gameObject);
            root = null;
            // Default origin is Goryeo even while the civilization picker is open.
            // Do not persist Step=1 until that choice has actually been committed.
            if (map != Home || !IsCat || StartScreen.IsOpen) return;
            MigrateOrder();
            if(!GuidePosition(out var position)){Debug.LogError("Goryeo tutorial guide anchor missing or stale");return;}
            root = new GameObject("Goryeo Intro").transform;
            var go = new GameObject("Intro Guide");
            go.transform.SetParent(root, false);
            go.transform.position = position;
            var p = go.AddComponent<Person>();
            p.blocksMovement = false;
            p.label = () => Step == 1 || Step == 10 ? Guide + " !" : Guide;
            p.act = TalkGuide;
            Sprite s = BorrowSprite("뱃사공");
            if (s != null) { var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = s; sr.sortingOrder = 10; }
            if (FreshStart()) arrival=StartCoroutine(Arrive());
        }

        IEnumerator Arrive()
        {
            Step = 1;
            yield return new WaitForSecondsRealtime(0.4f);
            while (GameManager.I != null && GameManager.I.IsTravelling) yield return null;
            arrival=null;
            if(!IsCat||MapRoot.Current==null||MapRoot.Current.mapId!=Home||StartScreen.IsOpen||Step!=1)yield break;
            DialogueUI.Show("나루의 아침",
                "아침 짐배가 닿자 나루가 분주해졌다.\n" +
                "“이보게, 새내기! 장삿길에 나서려면 채비부터 해야지.”\n" +
                "선착장 일꾼이 당신을 향해 손짓한다.",
                new DialogueUI.Choice("일꾼에게 가기", () =>
                {
                    if(GameManager.I!=null)GameManager.I.SetMemo("goryeo:intro_opening",1);
                    Notify.Show("선착장 일꾼에게 다가가 Space로 말을 걸어 보세요.",5f);
                }));
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
            Notify.Show("일꾼이 노잣돈 20냥을 건넸다", 5f);
        }

        void Done(int task)
        {
            if (Step != task + 2) return;
            Step = task + 3;
            if(Step==6 && GameManager.I.GetMemo("goryeo:intro_q_done")>0)Step=7;
            Sfx.Play("coin");
            Notify.Show(Step<=9?$"잘했어요! 다음: {Tasks[Step-2]}":"다 했어요! 선착장 일꾼에게 돌아가 말을 걸어 보세요.",4f);
        }

        void OnInventory() { if (IsCat) Done(2); }
        void OnBought(ItemData item, TownData town, int price) { if (IsCat) Done(5); }
        void OnSold(ItemData item, TownData town, int price, int profit) { if (IsCat) Done(6); }

        void Update()
        {
            if(IsCat)MigrateOrder();
            int s = Step;
            if (s < 2 || s > 9 || !IsCat || GameManager.I == null) return;
            if (s == 2 && !UIState.Blocking && InputReader.Run() && InputReader.MoveVector().sqrMagnitude > 0.1f)
            {
                runTime += Time.unscaledDeltaTime;
                if (runTime > 1f) { runTime = 0f; Done(0); }
            }
            else if (s == 3 && InputReader.StatusPressed()) Done(1);
            else if (s == 6 && InputReader.QuestLogPressed()) Done(4);
            else if (s == 5 && InputReader.MapPressed()) Done(3);
            else if (s == 9 && InputReader.LedgerPressed()) Done(7);
        }

        public Rect GuideRect { get; private set; }
        public string GuideText { get; private set; }
        public static bool Showing => Active && !UIState.Blocking && !HUD.Expanded && IsCat && InGoryeo;
        // Compatibility for older review tools and notification layout; the bottom card is retired.
        public static Rect ReservedGuideRect => default;
        void OnGUI()
        {
            GuideRect=default;GuideText=null;
            if(!Showing)return;
            UIStyle.Begin();
            int s=Step;
            string main=s==1?"선착장 일꾼에게 말 걸기":s==10?"선착장 일꾼에게 돌아가 말 걸기":CuteUI.Plain(Tasks[s-2]);
            string where=s==1?"바로 옆 · Space":s==10?"다 했어요!":$"배우기 {s-1}/{Tasks.Length}";
            GuideText=main;
            CuteUI.TopPill(CuteUI.TopStack(10),false,CuteUI.Pic.Bulb,main,where,false);
        }
    }
}
