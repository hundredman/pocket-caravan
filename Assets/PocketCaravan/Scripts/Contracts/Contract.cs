using System;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 배달 의뢰 하나. 마을 게시판에서 맡아 기한 안에 다른 마을 게시판에 납품한다.
    /// 기한(acceptedAt·deadline·finalDeadline)은 내 의뢰 시간 GameClock.PlayHours 기준 (2026-11-08: 게임을 끄면 멈추고, 온라인 세상 시간과 따로 흐른다).
    /// 2026-11-08 이전 저장은 세상 시간 기준이었으며, 불러올 때 PlayHours를 그 값에서 이어받아 그대로 맞는다.
    /// </summary>
    [Serializable]
    public class Contract
    {
        public int id;
        public string fromTown, toTown;
        public string cargo;
        public float weight;
        public int reward;
        // 구 저장은0. 수락한 계약은 이 값과 보수/기한을 변경하지 않는다.
        public int rewardRuleVersion;
        public int quotedRouteSteps;
        [Tooltip("이 신용 이상이어야 맡을 수 있음")] public int minCredit;
        [Tooltip("맡은 뒤 기한까지 걸리는 시간")] public float hours;

        public float acceptedAt;
        public float deadline;        // 이때까지 오면 보수 전액
        public float finalDeadline;   // 이때가 지나면 실패
        public float condition = 1f;  // 짐 상태 0~1
        public bool dropped;          // 지금 길에 떨어져 있음
        // R4 속죄 의뢰: 평판이 마이너스인 단위만 연다. 보증금은 수락 때 내고 성공하면 돌려받는다.
        public bool atonement;
        public int deposit;
        public int atonementScope;    // ReputationScope 값
        public string atonementTarget;
        /// <summary>11/19 의뢰 종류 (ContractKind). 옛 저장은 0 = 보통</summary>
        public int kind;

        public void ValidateRecovery()
        {
            if (!atonement) return; // Old ordinary contracts have none of the additive R4 fields.
            if (deposit <= 0 || reward != 0 || minCredit != 0 || (atonementScope != 0 && atonementScope != 1)
                || string.IsNullOrWhiteSpace(atonementTarget))
                throw new System.IO.InvalidDataException("속죄 의뢰의 보증금/평판 대상이 올바르지 않습니다.");
        }

        public float Window => Mathf.Max(1f, deadline - acceptedAt);
        public bool Premium => minCredit > 0;
        public bool Express => kind == ContractKind.Express;
        public bool Fragile => kind == ContractKind.Fragile;
        public bool Bulk => kind == ContractKind.Bulk;
    }

    /// <summary>
    /// 11/19 은샘 레벨 디자인: 신용 단계가 오르면 게시판에 새 종류의 의뢰가 나온다 (Core/Credit.cs 단계표).
    /// 짐마다 다루는 법이 달라 같은 배달도 다르게 논다 (Sea of Thieves 화물 운송, Oolite 계약 참고).
    ///  1단계 급한 짐: 기한이 보통의 6할, 보수 1.35배. 길 고르기·달리기 체력
    ///  2단계 깨지기 쉬운 짐: 달리거나 길 밖을 걸으면 짐 상태가 깎인다 (90% 아래면 덜 받음), 보수 1.25배. 조심히 걷기
    ///  3단계 귀한 짐: 예전부터 있던 것 (minCredit = 3). 도적이 노린다
    ///  4단계 큰 주문: 무게 10~16, 기한 넉넉, 보수 1.6배, 큰 판 짐 싣기 퍼즐 (9×4)
    /// 종류마다 신용 +1점 (ContractManager.CreditPoints).
    /// </summary>
    public static class ContractKind
    {
        public const int Normal = 0, Express = 1, Fragile = 2, Bulk = 3;
        public static int LevelOf(int kind) => kind == Express ? 1 : kind == Fragile ? 2 : kind == Bulk ? 4 : 0;
        public static string Name(int kind) => kind == Express ? "급한 짐" : kind == Fragile ? "깨지기 쉬움" : kind == Bulk ? "큰 주문" : null;

        // 11/19 은샘 "새 시스템은 튜토리얼처럼 설명, 마우스를 올리면 자세히": 게시판 카드에 마우스를 올리면 Help, 처음 볼 때 스포트라이트 말풍선은 Intro
        public const string PremiumKey = "premium";
        public static string Key(Contract c) => c == null || c.atonement ? null : c.Premium ? PremiumKey : c.kind == Express ? "express" : c.kind == Fragile ? "fragile" : c.kind == Bulk ? "bulk" : null;

        public static string Help(string key)
        {
            switch (key)
            {
                case "express": return "급한 짐\n기한이 보통의 6할로 짧아요\n대신 보수 1.35배 · 신용 +1\n가까운 길로 서둘러 가요";
                case "fragile": return "깨지기 쉬운 짐\n달리면 1초에 1%씩 상해요 (걸으면 괜찮아요)\n90% 아래로 상하면 덜 받고 신용도 못 받아요\n온전히 나르면 보수 1.25배 · 신용 +1";
                case "bulk": return "큰 주문\n무게 10~16의 큰 짐이라 수레·짐말이 있어야 해요\n기한은 넉넉하고, 맡을 때 큰 짐 싣기 퍼즐\n보수 1.6배 · 신용 +1";
                case PremiumKey: return "귀한 짐\n신용 3단계부터 맡을 수 있어요\n도적이 다니는 길에서는 노리니 조심해요\n보수 1.8배 · 신용 +1";
                default: return null;
            }
        }

        public static string Intro(string key)
        {
            switch (key)
            {
                case "express": return "새 의뢰: 급한 짐! 기한이 짧은 대신 보수가 많아요\n카드에 마우스를 올리면 자세히 볼 수 있어요";
                case "fragile": return "새 의뢰: 깨지기 쉬운 짐! 달리면 상하니 걸어서 날라요\n카드에 마우스를 올리면 자세히 볼 수 있어요";
                case "bulk": return "새 의뢰: 큰 주문! 무거운 짐이라 수레·짐말이 필요해요\n카드에 마우스를 올리면 자세히 볼 수 있어요";
                case PremiumKey: return "이제 귀한 짐을 맡을 수 있어요! 보수가 크지만 도적이 노려요\n카드에 마우스를 올리면 자세히 볼 수 있어요";
                default: return null;
            }
        }

        /// <summary>맡을 때 알림 끝에 붙이는 한마디</summary>
        public static string Reminder(Contract c) => c == null ? null : c.Fragile ? "달리면 상해요, 걸어서" : c.Express ? "기한이 짧아요" : c.Bulk ? "기한은 넉넉해요" : null;
    }

    /// <summary>납품 결과</summary>
    public struct ContractResult
    {
        public int paid;
        public int credit;
        public bool success;
        public string message;
    }
}
