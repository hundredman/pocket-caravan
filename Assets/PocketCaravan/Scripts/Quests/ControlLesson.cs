using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 2026-11-08 공통화 4단계: 시작 튜토리얼의 조작 배우기 여덟 가지 (네 지역 Intro가 똑같이 쓰던 목록과 키 확인).
    /// 이야기·안내 인물·순서 번호(Step 2~9)는 각 지역 Intro가 그대로 갖고, 조작 설명만 여기서 한 번에 고친다.
    /// </summary>
    public static class ControlLesson
    {
        public const int Count = 8;
        public const int Run = 0, Status = 1, Inventory = 2, Map = 3, Quests = 4, Bought = 5, Sold = 6, Ledger = 7;

        /// <summary>
        /// 11/18 은샘: 배우는 순서. 할 일 카드(여정 수첩)를 맨 앞으로 (처음 하는 사람이 튜토리얼이 어디 보이는지 몰랐다).
        /// 단계 번호 2~9 = 이 순서의 1~8번째. 번호(Run=0 …)는 할 일 아이디라 그대로 두고, 순서만 여기서 정한다.
        /// 예전 순서는 아이디 순서(달리기·Tab·I·M·카드·사기·팔기·장부)였다 → MigrateFromOld로 옮긴다.
        /// </summary>
        public static readonly int[] Order = { Quests, Run, Status, Inventory, Map, Bought, Sold, Ledger };
        /// <summary>단계 번호(2~9)의 할 일 아이디 (아니면 -1)</summary>
        public static int TaskAt(int step) => step >= 2 && step < 2 + Count ? Order[step - 2] : -1;
        static bool Has(int mask, int task) => task >= 0 && (mask & (1 << task)) != 0;
        /// <summary>step부터 아직 안 한 첫 단계 (다 했으면 10 = 안내 인물에게 돌아가기)</summary>
        static int FirstOpen(int step, int mask) { while (step <= 9 && Has(mask, TaskAt(step))) step++; return step; }
        /// <summary>할 일 하나를 끝냈다: 배우는 중(2~9단계)이면 mask에 표시한다(미리 한 것은 그 단계에서 건너뛴다).
        /// 지금 단계의 일이면 다음 단계 번호, 아니면 -1 (이때도 mask는 바뀌었을 수 있으니 저장한다)</summary>
        public static int NextStep(int step, int task, ref int mask)
        {
            if (step < 2 || step > 9 || task < 0 || task >= Count) return -1;
            mask |= 1 << task;
            if (TaskAt(step) != task) return -1;
            return FirstOpen(step + 1, mask);
        }
        /// <summary>예전 순서로 저장된 단계(2~9)를 새 순서로 옮긴다. 예전에 끝낸 것은 mask에 표시. questsDone = 카드 배우기를 이미 했다</summary>
        public static int MigrateFromOld(int oldStep, ref int mask, bool questsDone)
        {
            if (oldStep < 2 || oldStep > 9) return oldStep;
            for (int t = 0; t < oldStep - 2; t++) mask |= 1 << t;   // 예전 순서 = 아이디 순서
            if (questsDone) mask |= 1 << Quests;
            return FirstOpen(2, mask);
        }

        // 11/18 지금 배우는 것 (지역 Intro가 매 프레임 KeyTask를 부를 때 적는다). 튜토리얼 스포트라이트(TutorialSpot)가 본다
        static int current = -1, currentFrame = -9;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { current = -1; currentFrame = -9; openedTask = -1; openedSeen = false; }
        static int openedTask = -1; static bool openedSeen;   // Tab·수첩: 이 단계에서 연 적이 있다 (닫으면 끝)
        /// <summary>지금 이 배우기 단계인가 (이번·지난 프레임에 Intro가 알렸으면)</summary>
        public static bool IsActive(int task) => current == task && Time.frameCount - currentFrame <= 2;

        /// <param name="bag">소지품 이름 (고려 "보따리(소지품)")</param>
        /// <param name="mapGoal">지도에서 해 볼 것 (고려 "장단 찾아보기"). 없으면 펼쳐 보기만</param>
        public static string[] Tasks(string bag = "소지품 주머니", string mapGoal = null) => new[]
        {
            "Shift를 누른 채 걸어서 달려 보기",
            "Tab을 눌러 내 상태 보고 다시 닫기",
            $"I를 눌러 {bag} 열어 보기",
            string.IsNullOrEmpty(mapGoal) ? "M을 눌러 지도 펼쳐 보기" : $"M을 눌러 지도 펼치고 {mapGoal}",
            "Q나 왼쪽 할 일 카드로 여정 수첩을 열고, 목록을 본 뒤 닫기",   // 11/13 Q 키 대신 할 일 카드
            "상인에게 물건 하나 구매하기",
            "산 물건을 다른 마을 상인에게 판매하기",
            "L을 눌러 장부에서 방금 거래 기록 확인하기",
        };

        /// <summary>
        /// 11/13 기우(시연 피드백 "튜토리얼 카드가 너무 크다"): 할 일 카드에 띄우는 짧은 이름 (키캡 + 두세 낱말).
        /// 긴 문장(Tasks)은 안내 인물 대사와 "잘했어요! 다음: …" 알림에 그대로 쓴다.
        /// full = 그 단계의 긴 문장 (지도에서 해 볼 것이 있으면 "M을 눌러 지도 펼치고 ○○" → [M] ○○)
        /// </summary>
        public static (string key, string text) Short(int task, string full = null)
        {
            switch (task)
            {
                case Run: return ("Shift", "달리기");
                case Status: return ("Tab", "상태 보기");
                case Inventory: return ("I", "소지품 열기");
                case Map:
                    int at = full != null ? full.IndexOf("펼치고 ", System.StringComparison.Ordinal) : -1;
                    return ("M", at >= 0 ? full.Substring(at + 4) : "지도 펴기");
                case Quests: return ("Q", "여정 수첩 보기");   // 11/18 Q 키로도
                case Bought: return (null, "장터에서 하나 사기");
                case Sold: return (null, "다른 마을에서 팔기");
                case Ledger: return ("L", "장부 보기");
            }
            return (null, full);
        }

        /// <summary>
        /// 이번 프레임에 키로 끝낸 배우기 번호(없으면 -1). step은 Intro 순서 번호(2 = 첫 배우기, 할 일은 TaskAt).
        /// 소지품(I)·사기·팔기는 키가 아니라 사건(InventoryUI.Opened, GameEvents)으로 끝낸다.
        /// </summary>
        const float RunNeed = .4f;

        public static int KeyTask(int step, ref float runTime)
        {
            int task = TaskAt(step);
            current = task; currentFrame = Time.frameCount;
            if (task == Run)
            {
                if (!UIState.Blocking && InputReader.Run() && InputReader.MoveVector().sqrMagnitude > 0.1f)
                {
                    runTime += Time.unscaledDeltaTime;
                    // 11/18 은샘 "두 번 해야 통과": 1초를 모아야 해서 짧게 두 번 달려야 넘어갔다 → 0.4초 (Shift를 누르고 몇 걸음)
                    if (runTime > RunNeed) { runTime = 0f; return Run; }
                }
                return -1;
            }
            // 11/18 은샘: Tab·수첩은 열자마자 다음으로 넘어가지 않고, 연 뒤 닫았을 때 끝난다 (연 채로 다음 단계 안내가 겹쳤다)
            if (task == Status || task == Quests)
            {
                bool isOpen = task == Status ? HUD.Expanded : QuestTracker.JournalOpen;
                if (openedTask != task) { openedTask = task; openedSeen = false; }
                if (isOpen) openedSeen = true;
                else if (openedSeen) { openedSeen = false; openedTask = -1; return task; }
                return -1;
            }
            if (task == Map && InputReader.MapPressed()) return Map;
            if (task == Ledger && InputReader.LedgerPressed()) return Ledger;
            return -1;
        }
    }
}
