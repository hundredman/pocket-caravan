using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 상인 칭호 (2026-11-25 은샘 "성장 체감"): 성장이 돈과 신용 숫자로만 보이고, 좌판을 연 뒤에는 더 노릴 것이 없었다.
    /// - 지금까지 장사로 번 돈(판 이익 + 배달 보수)을 따로 쌓아 칭호가 오른다. 돈을 써도 줄지 않는다.
    /// - 칭호는 보이는 것만 바꾼다 (상태 창 막대 · 여정 수첩 성장 탭 · 오를 때 알림). 값·한도 같은 규칙은 건드리지 않는다.
    ///   그래서 사고팔기를 되풀이해 억지로 올려도 얻는 것이 없고, 못 올려도 손해가 없다.
    /// - 저장 형식은 그대로: 공용 메모 "rank:earned"(번 돈) · "rank:seeded"(옛 저장 한 번 맞춤) · "rank:told"(알린 칭호).
    /// - 이 기능이 없던 저장은 처음 한 번, 가진 돈과 산 운반 수단·좌판 값으로 번 돈을 어림해 맞춘다 (그동안 번 것을 잃은 느낌이 없게).
    /// </summary>
    public static class MerchantRank
    {
        public struct Title { public string name; public int need; }
        /// <summary>칭호와 그 칭호가 되는 번 돈. 처음 한 시간에 서너 번 오르고(성장 시뮬레이션: 손수레 약 15분 · 마차 약 62분 · 좌판 약 105분),
        /// 좌판 뒤에도 '거상', '전설의 거상'이 남는다. 네 지역이 함께 쓰는 이름이라 어느 한 문명 말은 피했다</summary>
        public static readonly Title[] Titles =
        {
            new Title { name = "새내기 상인", need = 0 },
            new Title { name = "떠돌이 상인", need = 60 },
            new Title { name = "단골 상인", need = 250 },
            new Title { name = "이름난 상인", need = 600 },
            new Title { name = "큰손 상인", need = 1500 },
            new Title { name = "거상", need = 4000 },
            new Title { name = "전설의 거상", need = 10000 },
        };
        const string EarnedKey = "rank:earned", SeededKey = "rank:seeded", ToldKey = "rank:told";

        /// <summary>오른 직후 상태 창 막대가 반짝이는 시각</summary>
        public static float RaisedAt { get; private set; } = -99f;

        public static int IndexFor(int earned)
        {
            int i = 0;
            while (i + 1 < Titles.Length && earned >= Titles[i + 1].need) i++;
            return i;
        }

        /// <summary>지금까지 장사로 번 돈</summary>
        public static int Earned(GameManager gm)
        {
            if (gm == null) return 0;
            if (gm.GetMemo(SeededKey) < 1f) Seed(gm);
            return Mathf.RoundToInt(gm.GetMemo(EarnedKey));
        }
        public static int Index(GameManager gm) => IndexFor(Earned(gm));
        public static string Name(GameManager gm) => Titles[Index(gm)].name;

        /// <summary>다음 칭호까지: 이번 칭호 안에서 번 돈 / 필요한 돈 (마지막 칭호면 false)</summary>
        public static bool Progress(GameManager gm, out int have, out int need, out string next)
        {
            int earned = Earned(gm), i = IndexFor(earned);
            if (i + 1 >= Titles.Length) { have = need = 0; next = null; return false; }
            have = earned - Titles[i].need; need = Titles[i + 1].need - Titles[i].need; next = Titles[i + 1].name;
            return true;
        }

        static void Seed(GameManager gm)
        {
            var p = gm.Player; int guess = 0;
            if (p != null)
            {
                guess = Mathf.Max(0, p.money - (gm.Db != null ? gm.Db.startMoney : 0));   // 밑천은 번 돈이 아니다
                if (p.vehicle != null) guess += Mathf.Max(0, p.vehicle.price);
                if (p.stowedVehicle != null) guess += Mathf.Max(0, p.stowedVehicle.price);
                if (PlayerStall.Has(p)) guess += VehicleGoal.StallPrice;
            }
            gm.SetMemo(EarnedKey, guess);
            gm.SetMemo(SeededKey, 1f);
            gm.SetMemo(ToldKey, IndexFor(guess));   // 어림으로 맞춘 칭호는 알리지 않는다
        }

        /// <summary>번 돈을 더하고, 칭호가 올랐으면 알린다</summary>
        public static void Add(int amount)
        {
            var gm = GameManager.I;
            if (gm == null || amount <= 0) return;
            int before = Earned(gm);
            gm.SetMemo(EarnedKey, Mathf.Min(before + amount, 9999999));
            int now = IndexFor(before + amount), told = Mathf.RoundToInt(gm.GetMemo(ToldKey));
            if (now <= told) return;
            gm.SetMemo(ToldKey, now);
            RaisedAt = Time.unscaledTime;
            string more = now + 1 < Titles.Length ? $" 다음은 '{Titles[now + 1].name}'" : "";
            Notify.Event($"칭호가 올랐다! 이제 '{Titles[now].name}'.{more}", 5f);
            Sfx.Play("levelup", .7f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Hook()
        {
            Rehook(default, default);
            // "처음부터 시작"은 정적 구독을 지운다(GameEvents.ClearForNewGame). 씬이 열릴 때마다 다시 건다
            SceneManager.sceneLoaded -= Rehook; SceneManager.sceneLoaded += Rehook;
        }
        static void Rehook(Scene s, LoadSceneMode m)
        {
            GameEvents.ItemSold -= OnSold; GameEvents.ItemSold += OnSold;
            GameEvents.ContractFinished -= OnContract; GameEvents.ContractFinished += OnContract;
        }
        static void OnSold(ItemData item, TownData town, int price, int profit) => Add(profit);
        static void OnContract(Contract c, bool ok) { if (ok && c != null) Add(c.reward); }
    }
}
