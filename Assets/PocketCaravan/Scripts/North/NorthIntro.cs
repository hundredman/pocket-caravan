using System.Collections;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 2026-10-30 유저테스트 반영: 순록(바이킹 교역상)으로 새로 시작하면 바로 광장에 떨어지지 않고 시작 이야기 + 조작 튜토리얼.
    /// 1) 비르카에 처음 들어오면 은빛 순록 꿈 이야기 → 우리 집 문 앞에서 깨어남
    /// 2) 옆의 어머니에게 Space로 말 걸기 → 지도·소지품 주머니를 받고 할 일을 차례로:
    ///    달리기(Shift) · 상태(Tab) · 소지품(I) · 지도(M) · 퀘스트 목록(Q) · 장터에서 사기 · 팔기 · 장부(L)
    /// 3) 어머니에게 돌아가면 돈·신용·부탁 이야기와 은빛 순록 이야기 → 집 보는 남편(첫 퀘스트 north_q1_butter)으로 보냄
    /// 진행은 NorthSave "intro" (0 없음 · 1 어머니에게 말 걸기 · 2~9 할 일 · 10 어머니에게 돌아가기 · 11 끝).
    /// 이미 북유럽 퀘스트를 시작했거나 다른 문명 캐릭터면 나오지 않는다. 공용 코드는 읽기만 한다(소지품 창 Opened 이벤트 구독).
    /// </summary>
    public class NorthIntro : MonoBehaviour
    {
        const string Home = "north_birka";
        const string FirstQuest = "north_q1_butter";
        // 우리 집: 광장 바로 위 집 (문 (41,37)). 깨어나는 자리와 어머니 자리 (맵 칸, 위에서부터 셈)
        static readonly Vector2Int WakeCell = new Vector2Int(41, 39), MomCell = new Vector2Int(43, 39);
        const string Mom = "어머니";

        static readonly string[] Tasks = ControlLesson.Tasks();   // 11/08 공통화 4단계: 조작 배우기 여덟 가지는 공용 ControlLesson

        public static bool Active => Step >= 1 && Step <= 10;
        static int Step { get => Mathf.RoundToInt(NorthSave.Get("intro")); set => NorthSave.Set("intro", value); }

        Transform root;
        float runTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthIntro>() != null) return;
            var go = new GameObject("NorthIntro");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthIntro>();
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

        void Start() { if (NorthUtil.Map == Home) OnMap(Home); }

        static bool IsReindeer => GameManager.I != null && GameManager.I.Player.origin != null && GameManager.I.Player.origin.id == "viking";

        /// <summary>새로 시작한 순록 상인인가: 북유럽 퀘스트를 아직 하나도 시작하지 않았고 첫 퀘스트를 받을 수 있음</summary>
        static bool FreshStart()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Quests == null || !IsReindeer || NorthSave.Has("intro")) return false;
            foreach (var p in gm.Quests.Active) if (p != null && p.quest != null && p.quest.id != null && p.quest.id.StartsWith("north_")) return false;
            var q = gm.Db.quests.Find(x => x != null && x.id == FirstQuest);
            return q != null && gm.Quests.CanStart(q);
        }

        void OnMap(string map)
        {
            if (root != null) Destroy(root.gameObject);
            root = null;
            if (map != Home || !IsReindeer) return;
            root = new GameObject("North Intro").transform;
            var spot = NorthSpot.Make(root, NorthUtil.Cell(Home, MomCell.x, MomCell.y), () => Step == 1 || Step == 10 ? Mom + " !" : Mom, TalkMom, Interactable.Kind.Person);
            var f = NorthUtil.Load("listener1_d0", new Vector2(0.5f, 0.1f));
            if (f != null) { var sr = spot.gameObject.AddComponent<SpriteRenderer>(); sr.sprite = f; sr.sortingOrder = 10; }
            if (FreshStart()) StartCoroutine(Wake());
        }

        IEnumerator Wake()
        {
            Step = 1;
            yield return new WaitForSecondsRealtime(0.4f);
            while (GameManager.I != null && GameManager.I.IsTravelling) yield return null;
            var motor = FindAnyObjectByType<PlayerMotor>();
            if (motor != null) motor.Teleport(NorthUtil.Cell(Home, WakeCell.x, WakeCell.y), new Vector2Int(0, -1));
            var cam = FindAnyObjectByType<CameraFollow>(); if (cam != null) cam.SnapNow();
            DialogueUI.Show("꿈",
                "하얀 눈보라 속, 얼어붙은 피오르 위로 은빛 순록 한 마리가 나타났다.\n" +
                "순록은 아무 말 없이 눈 위에 길을 밟아 보이고는, 오로라 속으로 사라졌다…\n\n" +
                "…눈을 떠 보니 우리 집 문 앞이다. 옆에서 어머니가 부르신다.",
                new DialogueUI.Choice("일어나기", () => Notify.Show("옆에 계신 어머니께 다가가 Space로 말을 걸어 보세요.", 5f)));
        }

        void TalkMom()
        {
            int s = Step;
            if (s == 1)
            {
                DialogueUI.Show(Mom,
                    "또 은빛 순록 꿈을 꿨니? 너희 할아버지도 그 꿈을 꾸고 처음 장사길에 나섰단다.\n" +
                    "오늘부터 너도 어엿한 상인이야. 자, 이 지도와 소지품 주머니를 챙기렴.\n\n" +
                    "떠나기 전에 몸에 익혀 둘 것들이 있어. 하나씩 해 보고 다 되면 다시 오렴.",
                    new DialogueUI.Choice("네, 해 볼게요", () => { Step = 2; Notify.Show("지도와 소지품 주머니를 받았다!", 3f); Sfx.Play("coin"); }));
                return;
            }
            if (s >= 2 && s <= 9) { DialogueUI.Show(Mom, $"아직 할 일이 남았구나.\n\n지금 할 일: {Tasks[s - 2]}"); return; }
            if (s == 10)
            {
                DialogueUI.Show(Mom,
                    "잘했다! 이제 상인의 기본은 다 익혔구나.\n\n" +
                    "같은 장터에서 사서 바로 되팔면 손해를 봤지? 장사는 싼 곳에서 사서 그 물건이 귀한 곳에 가져가 파는 거란다.\n" +
                    "좋은 물건을 좋은 값에 사려면 사람들의 믿음, 신용이 필요해. 신용은 사람들의 부탁을 들어주며 쌓이지.",
                    new DialogueUI.Choice("은빛 순록은요?", () => DialogueUI.Show(Mom,
                        "옛 노래에 은빛 순록은 길을 잃은 상인에게 길을 밟아 보여 준대. 북유럽 곳곳의 룬돌에 그 이야기가 새겨져 있다더구나.\n" +
                        "꿈에 나왔다면 너에게도 보여 줄 길이 있나 보지.\n\n" +
                        "우선 마을 사람들부터 도와 보렴. 서쪽 첫 집의 집 보는 남편이 버터 때문에 쩔쩔매고 있던데.",
                        new DialogueUI.Choice("다녀올게요!", Finish))));
                return;
            }
            DialogueUI.Show(Mom, "몸조심하고, 길이 막히면 지도(M)를 보렴. 은빛 순록이 너를 지켜 줄 거야.");
        }

        void Finish()
        {
            Step = 11;
            var gm = GameManager.I;
            if (gm != null) gm.Player.money += 20;
            Sfx.Play("coin");
            Notify.Show("어머니가 노잣돈 20냥을 쥐여 주셨다 · 다음 이야기: 집 보는 남편", 5f);
        }

        void Done(int task)
        {
            if (Step != task + 2) return;
            Step = task + 3;
            Sfx.Play("coin");
            Notify.Show(Step <= 9 ? $"잘했어요! 다음: {Tasks[Step - 2]}" : "다 했어요! 어머니께 돌아가 말을 걸어 보세요.", 4f);
        }

        void OnInventory() { if (IsReindeer) Done(2); }
        void OnBought(ItemData item, TownData town, int price) { if (IsReindeer) Done(5); }
        void OnSold(ItemData item, TownData town, int price, int profit) { if (IsReindeer) Done(6); }

        void Update()
        {
            int s = Step;
            if (s < 2 || s > 9 || !IsReindeer || GameManager.I == null) return;
            int task = ControlLesson.KeyTask(s, ref runTime);   // 11/08 공용 키 확인 (달리기·Tab·M·Q·L)
            if (task >= 0) Done(task);
        }

        void OnGUI()
        {
            int s = Step;
            if (s < 1 || s > 10 || UIState.Blocking || HUD.Expanded || !IsReindeer || NorthUtil.Map == null || !NorthUtil.InNorth) return;
            UIStyle.Begin();
            // 11/08 UI B안: 검은 줄(왼쪽 위 상태 카드·오른쪽 길 장애 알약을 덮었다) 대신 할 일 줄 아래 위 가운데 알약 (전구 = 배우기). Tab 펼친 동안은 숨긴다
            string main = s == 1 ? "어머니께 말 걸기"
                        : s == 10 ? "어머니께 돌아가 말 걸기"
                        : CuteUI.Plain(Tasks[s - 2]);
            string where = s == 1 ? "바로 옆 · Space" : s == 10 ? "다 했어요!" : $"배우기 {s - 1}/{Tasks.Length}";
            CuteUI.TopPill(CuteUI.TopStack(10), false, CuteUI.Pic.Bulb, main, where, false);
        }
    }
}
