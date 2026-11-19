using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/19 은샘 "신용 1을 올리는 데 채워야 할 수치가 있는 것": 신용 점수와 신용 단계.
    /// - PlayerState.credit은 점수다. 맡은 일(배달·납품·게시판 의뢰)과 맡은 짐 속 정직 선택에서만 오르내린다 (Docs/Standing.md 원칙).
    /// - 단계 n이 되려면 누적 점수: 1단계 1 · 2단계 6 · 3단계 12 · 4단계 20 · 5단계 30 · 6단계 42 (2단계부터 n×(n+1)).
    /// - 한 번 오른 단계는 점수가 깎여도 내려가지 않는다 (PlayerState.creditFloor). 열렸던 시설이 다시 닫히지 않게.
    /// - 시설·귀한 짐 의뢰·좌판 같은 조건(minCredit 등)은 모두 단계로 따진다.
    /// - 옛 저장(creditVersion 0)의 신용 c는 그때 단계였으니 c×(c+1)점으로 옮겨 단계를 그대로 둔다 (GameManager.Apply).
    /// </summary>
    public static class Credit
    {
        public const int Version = 1;

        /// <summary>n단계가 되는 누적 점수: 1단계 1점(첫 맡은 일 하나 · 5분 시연 안), 2단계부터 n×(n+1) (6·12·20·30·42). 11/19 성장 속도 측정으로 1단계만 앞당김</summary>
        public static int PointsFor(int level) => level <= 0 ? 0 : level == 1 ? 1 : level * (level + 1);

        /// <summary>점수만으로 닿는 단계</summary>
        public static int LevelOfPoints(int points)
        {
            int n = 0;
            while (n < 999 && PointsFor(n + 1) <= points) n++;
            return n;
        }

        /// <summary>지금 신용 단계 (깎여도 한 번 오른 단계 밑으로는 안 내려감)</summary>
        public static int Level(PlayerState p) => p == null ? 0 : Mathf.Max(LevelOfPoints(p.credit), p.creditFloor);

        /// <summary>다음 단계까지: 이번 단계 안에서 모은 점수 / 필요한 점수</summary>
        public static void Progress(PlayerState p, out int have, out int need)
        {
            int lv = Level(p);
            int from = PointsFor(lv), to = PointsFor(lv + 1);
            need = to - from;
            // 깎여서 이번 단계 시작점보다 낮으면 음수로 (몇 점을 되찾아야 하는지 그대로 보이게)
            have = Mathf.Min((p != null ? p.credit : 0) - from, need);
        }

        /// <summary>옛 저장의 신용(단계였던 값)을 점수로</summary>
        public static int MigratePoints(int oldCredit) => oldCredit > 0 ? PointsFor(oldCredit) : oldCredit;

        /// <summary>점수가 새 단계에 닿았으면 바닥을 올리고 그 단계를 돌려준다 (아니면 0). GameManager가 매 프레임 부른다</summary>
        public static int CheckLevelUp(PlayerState p)
        {
            if (p == null) return 0;
            int lv = LevelOfPoints(p.credit);
            if (lv <= p.creditFloor) return 0;
            p.creditFloor = lv;
            return lv;
        }

        /// <summary>단계별 혜택 (여정 수첩·알림에 쓰는 글). 시설 조건은 TownData에 따로 있다</summary>
        public static string PerkText(int level)
        {
            switch (level)
            {
                case 1: return "게시판에 급한 짐 의뢰";
                case 2: return "깨지기 쉬운 짐 의뢰 · 한 번에 4개";
                case 3: return "게시판 귀한 짐 의뢰";
                case 4: return "큰 주문 의뢰 · 장터 외상";
                case 5: return "내 좌판 (장터 창에서 열기) · 외상 한도 500냥";
                case 6: return "외상 한도 800냥";
                // 중앙 대륙 장거리 의뢰(6단계)는 만들면 case 6에 더한다 (아직 없는 것은 열린다고 알리지 않는다)
                default: return null;
            }
        }
    }
}
