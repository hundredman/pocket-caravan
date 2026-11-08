using System.Collections;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 2026-10-30 시작 이야기 + 조작 튜토리얼 (사막여우 · 아라비아 대상). 북유럽·베네치아 Intro와 같은 흐름, 이야기는 「향료길 장부」 초안의 0~1장만 따랐다(2~7장은 지역 담당이 이어서).
    /// 1) 알 아흐사에 처음 들어오면 프롤로그 자막(할아버지의 낡은 장부, 빈 도장 칸 셋)
    /// 2) 바로 옆 하산의 조카 아미르에게 Space로 말 걸기 → 지도·소지품 주머니를 받고 할 일을 차례로:
    ///    달리기(Shift) · 상태(Tab) · 소지품(I) · 지도(M) · 퀘스트 목록(Q) · 장터에서 사기 · 팔기 · 장부(L)
    /// 3) 아미르에게 돌아가면 장부 첫 줄·신용·도장, 아라비아 출신의 강점(멀리서 가져온 물건 +5%) → 짐꾼 힌드바드(Q1 arabia_q01_first_delivery)로 보냄
    /// 진행은 GameManager 진행 메모 "arabia:intro" (0 없음 · 1 말 걸기 · 2~9 할 일 · 10 돌아가기 · 11 끝). 새 파일만 더했고 기존 아라비아 파일·씬·퀘스트는 그대로다.
    /// 그림은 북유럽 대상 캠프에 옮겨 둔 아라비아 사람 그림(Resources/North/Minigame/caravan1_d0, 32×48 · 26PPU)을 빌려 쓴다.
    /// 이미 아라비아 퀘스트를 시작했거나 다른 문명 캐릭터면 나오지 않는다.
    /// </summary>
    public class ArabiaIntro : MonoBehaviour
    {
        const string Home = "arabia_ahsa";
        const int MapH = 44;   // arabia_ahsa.json 높이 (맵 칸은 위가 0)
        const string FirstQuest = "arabia_q01_first_delivery";
        static readonly Vector2Int GuideCell = new Vector2Int(35, 38);   // 시작 자리(남문 S0 (31,37)) 오른쪽 아래 (하산 (29,36) 이름표와 안 겹치게)
        const string Guide = "하산의 조카 아미르";

        static readonly string[] Tasks =
        {
            "Shift를 누른 채 걸어서 달려 보기",
            "Tab을 눌러 내 상태 보기",
            "I를 눌러 소지품 주머니 열어 보기",
            "M을 눌러 지도 펼쳐 보기",
            "Q를 눌러 할 일(퀘스트) 목록 보기",
            "알 아흐사 장터에서 아무 물건이나 하나 사기",
            "산 물건을 장터에 다시 팔아 보기",
            "L을 눌러 장부에서 방금 거래 기록 확인하기",
        };

        public static bool Active => Step >= 1 && Step <= 10;
        static int Step { get => GameManager.I != null ? Mathf.RoundToInt(GameManager.I.GetMemo("arabia:intro")) : 0; set { if (GameManager.I != null) GameManager.I.SetMemo("arabia:intro", value); } }
        static bool Started => GameManager.I != null && GameManager.I.HasMemo("arabia:intro");
        static Vector3 Cell(int x, int y) => MapRoot.Current.CellCenter(new Vector3Int(x, MapH - 1 - y, 0));
        static bool InArabia => MapRoot.Current != null && MapRoot.Current.mapId != null && MapRoot.Current.mapId.StartsWith("arabia_");

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
            if (FindFirstObjectByType<ArabiaIntro>() != null) return;
            var go = new GameObject("ArabiaIntro");
            DontDestroyOnLoad(go);
            go.AddComponent<ArabiaIntro>();
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

        static bool IsCat => GameManager.I != null && GameManager.I.Player.origin != null && GameManager.I.Player.origin.id == "arabia";

        static bool FreshStart()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Quests == null || !IsCat || Started) return false;
            foreach (var p in gm.Quests.Active) if (p != null && p.quest != null && p.quest.id != null && p.quest.id.StartsWith("arabia_")) return false;
            var q = gm.Db.quests.Find(x => x != null && x.id == FirstQuest);
            return q != null && gm.Quests.CanStart(q);
        }

        void OnMap(string map)
        {
            if (root != null) Destroy(root.gameObject);
            root = null;
            if (map != Home || !IsCat) return;
            root = new GameObject("Arabia Intro").transform;
            var go = new GameObject("Intro Guide");
            go.transform.SetParent(root, false);
            go.transform.position = Cell(GuideCell.x, GuideCell.y);
            var p = go.AddComponent<Person>();
            p.blocksMovement = false;
            p.label = () => Step == 1 || Step == 10 ? Guide + " !" : Guide;
            p.act = TalkGuide;
            var tex = Resources.Load<Texture2D>("North/Minigame/caravan1_d0");
            Sprite s = null;
            if (tex != null) { tex.filterMode = FilterMode.Point; s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.1f), 26f); }
            if (s != null) { var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = s; sr.sortingOrder = 10; }
            if (FreshStart()) StartCoroutine(Arrive());
        }

        IEnumerator Arrive()
        {
            Step = 1;
            yield return new WaitForSecondsRealtime(0.4f);
            while (GameManager.I != null && GameManager.I.IsTravelling) yield return null;
            DialogueUI.Show("낡은 장부",
                "할아버지는 평생 향료길을 오갔다. 남긴 것은 낡은 가방 하나와 장부 한 권.\n" +
                "장부 첫 줄: \"짐은 목적지까지, 약속은 사람에게까지.\"\n\n" +
                "그 아래 빈 칸 셋. 알 아흐사, 제다, 사나.",
                new DialogueUI.Choice("장부를 덮는다", () => Notify.Show("바로 옆의 아미르에게 다가가 Space로 말을 걸어 보세요.", 5f)));
        }

        void TalkGuide()
        {
            int s = Step;
            if (s == 1)
            {
                DialogueUI.Show(Guide,
                    "그 가방… 너희 할아버지 거지? 삼촌(길잡이 하산)이 그러는데, 매듭이 아직 튼튼하대.\n" +
                    "자, 향료길 지도랑 소지품 주머니야. 장사꾼한테 이 둘은 물보다 귀해.\n\n" +
                    "길 떠나기 전에 손에 익혀 둘 게 있어. 하나씩 해 보고 다시 와.",
                    new DialogueUI.Choice("해 볼게", () => { Step = 2; Notify.Show("지도와 소지품 주머니를 받았다!", 3f); Sfx.Play("coin"); }));
                return;
            }
            if (s >= 2 && s <= 9) { DialogueUI.Show(Guide, $"아직 남았어.\n\n지금 할 일: {Tasks[s - 2]}"); return; }
            if (s == 10)
            {
                DialogueUI.Show(Guide,
                    "다 했네! 같은 장터에서 사서 바로 되팔면 손해였지? 상인은 어디서 사고, 어느 길로 가고, 어디서 파는지만 알면 돼.\n" +
                    "그리고 장부 첫 줄 기억해. 짐은 목적지까지, 약속은 사람에게까지. 약속을 지키면 신용이 쌓이고, 신용이 쌓이면 마을 어른들이 장부에 도장을 찍어 줄 거야.",
                    new DialogueUI.Choice("사막여우 상인은 뭐가 좋아?", () => DialogueUI.Show(Guide,
                        "우리 아라비아 대상은 멀리서 가져온 물건을 팔면 값을 더 쳐 받아(+5%). 먼 길을 겁내지 않는 게 우리 장기지.\n\n" +
                        "첫 일거리는 짐꾼 힌드바드 아저씨가 찾고 있어. 광장 위쪽 여관 근처에 있을 거야. 하산 삼촌도 너 기다리셔!",
                        new DialogueUI.Choice("다녀올게!", Finish))));
                return;
            }
            DialogueUI.Show(Guide, "모래바람이 불면 이정석만 보고 가. 길이 헷갈리면 지도(M), 시세가 헷갈리면 장부(L)!");
        }

        void Finish()
        {
            Step = 11;
            var gm = GameManager.I;
            if (gm != null) gm.Player.money += 20;
            Sfx.Play("coin");
            Notify.Show("아미르가 노잣돈 20냥을 건넸다 · 다음: 짐꾼 힌드바드「반쪽 짐」", 5f);
        }

        void Done(int task)
        {
            if (Step != task + 2) return;
            Step = task + 3;
            Sfx.Play("coin");
            Notify.Show(Step <= 9 ? $"잘했어요! 다음: {Tasks[Step - 2]}" : "다 했어요! 아미르에게 돌아가 말을 걸어 보세요.", 4f);
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
            if (s < 1 || s > 10 || UIState.Blocking || HUD.Expanded || !IsCat || !InArabia) return;
            UIStyle.Begin();
            // 11/08 UI B안: 검은 줄(왼쪽 위 상태 카드·오른쪽 길 장애 알약을 덮었다) 대신 할 일 줄 아래 위 가운데 알약 (전구 = 배우기). Tab 펼친 동안은 숨긴다
            string main = s == 1 ? "아미르에게 말 걸기"
                        : s == 10 ? "아미르에게 돌아가 말 걸기"
                        : CuteUI.Plain(Tasks[s - 2]);
            string where = s == 1 ? "바로 옆 · Space" : s == 10 ? "다 했어요!" : $"배우기 {s - 1}/{Tasks.Length}";
            CuteUI.TopPill(CuteUI.TopStack(10), false, CuteUI.Pic.Bulb, main, where, false);
        }
    }
}
