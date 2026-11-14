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

        /// <param name="bag">소지품 이름 (고려 "보따리(소지품)")</param>
        /// <param name="mapGoal">지도에서 해 볼 것 (고려 "장단 찾아보기"). 없으면 펼쳐 보기만</param>
        public static string[] Tasks(string bag = "소지품 주머니", string mapGoal = null) => new[]
        {
            "Shift를 누른 채 걸어서 달려 보기",
            "Tab을 눌러 내 상태 보기",
            $"I를 눌러 {bag} 열어 보기",
            string.IsNullOrEmpty(mapGoal) ? "M을 눌러 지도 펼쳐 보기" : $"M을 눌러 지도 펼치고 {mapGoal}",
            "왼쪽 할 일 카드를 눌러 여정 수첩 열어 보기",   // 11/13 Q 키 대신 할 일 카드
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
                case Quests: return (null, "이 카드 누르기");
                case Bought: return (null, "장터에서 하나 사기");
                case Sold: return (null, "다른 마을에서 팔기");
                case Ledger: return ("L", "장부 보기");
            }
            return (null, full);
        }

        /// <summary>
        /// 이번 프레임에 키로 끝낸 배우기 번호(없으면 -1). step은 Intro 순서 번호(2 = 첫 배우기).
        /// 소지품(I)·사기·팔기는 키가 아니라 사건(InventoryUI.Opened, GameEvents)으로 끝낸다.
        /// </summary>
        public static int KeyTask(int step, ref float runTime)
        {
            int task = step - 2;
            if (task == Run)
            {
                if (!UIState.Blocking && InputReader.Run() && InputReader.MoveVector().sqrMagnitude > 0.1f)
                {
                    runTime += Time.unscaledDeltaTime;
                    if (runTime > 1f) { runTime = 0f; return Run; }
                }
                return -1;
            }
            if (task == Status && InputReader.StatusPressed()) return Status;
            if (task == Map && InputReader.MapPressed()) return Map;
            if (task == Quests && InputReader.QuestLogPressed()) return Quests;
            if (task == Ledger && InputReader.LedgerPressed()) return Ledger;
            return -1;
        }
    }
}
