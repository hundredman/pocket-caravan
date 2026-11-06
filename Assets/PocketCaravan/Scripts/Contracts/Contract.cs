using System;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 배달 의뢰 하나. 마을 게시판에서 맡아 기한 안에 다른 마을 게시판에 납품한다.
    /// 시간은 모두 GameClock.TotalHours 기준.
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

        public void ValidateRecovery()
        {
            if (!atonement) return; // Old ordinary contracts have none of the additive R4 fields.
            if (deposit <= 0 || reward != 0 || minCredit != 0 || (atonementScope != 0 && atonementScope != 1)
                || string.IsNullOrWhiteSpace(atonementTarget))
                throw new System.IO.InvalidDataException("속죄 의뢰의 보증금/평판 대상이 올바르지 않습니다.");
        }

        public float Window => Mathf.Max(1f, deadline - acceptedAt);
        public bool Premium => minCredit > 0;
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
