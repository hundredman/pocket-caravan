using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    public enum ObjectiveType
    {
        Talk,        // targetNpc에게 말 걸기
        Deliver,     // item을 count만큼 들고 targetNpc에게 말 걸기 (건네줌)
        Buy,         // item을 count만큼 사기 (item이 비면 아무 물건)
        Sell,        // item을 count만큼 팔기 (item이 비면 아무 물건)
        Profit,      // 판매 이익을 count만큼 남기기
        VisitMap,    // targetId 맵에 가기
        BuyVehicle,  // vehicle 사기
        HaveMoney,   // 돈을 count 이상 모으기
        CompleteContract, // 배달 의뢰를 count번 성공하기 (targetId: 처음 맡을 마을 게시판)
    }

    [Serializable]
    public class QuestObjective
    {
        public ObjectiveType type;
        [Tooltip("화면에 보이는 할 일")] public string text;
        [Tooltip("NPC id 또는 맵 id")] public string targetId;
        public ItemData item;
        public VehicleData vehicle;
        public int count = 1;
        [Tooltip("단계 완료 대사 (Talk, Deliver). 짧은 문장 1~2개 권장. 창 높이를 넘으면 자동으로 다음 페이지로 나뉨")]
        [TextArea] public string completeLine;
    }

    /// <summary>
    /// 퀘스트 하나. 단계(objectives)를 순서대로 진행한다.
    /// 우클릭 > Create > Pocket Caravan > Quest 로 만들고 GameDatabase.quests에 넣는다.
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Caravan/Quest", fileName = "Quest")]
    public class QuestData : ScriptableObject
    {
        public string id;
        public bool retired;
        public string title;
        [Tooltip("튜토리얼 등 연속 퀘스트 이름 (퀘스트 목록에 묶어 보임)")] public string chain;
        [TextArea] public string summary;

        [Header("받기")]
        [Tooltip("퀘스트를 주는 NPC id")] public string giverNpc;
        public List<QuestData> requires = new List<QuestData>();
        [Tooltip("비어 있지 않으면 이 중 하나도 완료해야 수락 가능. requires는 여전히 모두 필요")]
        public List<QuestData> requiresAny = new List<QuestData>();
        [Tooltip("동일 그룹의 다른 의뢰를 진행/완료했다면 신규 수락 제외. 이미 수락한 구 저장은 보존")]
        public string choiceGroup;
        [Tooltip("퀘스트 제안 대사. 짧은 문장 1~2개씩 작성. 제목도 표시 공간을 쓰며, 긴 본문은 자동 분할하고 마지막 페이지에서 수락/거절")]
        [TextArea(3, 6)] public string offerLine;
        [Tooltip("수락할 때 받는 짐 (의뢰 물건)")] public ItemData giveItem;
        public int giveCount;
        [Tooltip("수락할 때 이 값에 넘겨받는 물건 (초보자용 싼 물건). 돈이 모자라면 수락할 수 없다")] public ItemData offerItem;
        public int offerCount;
        public int offerPrice;

        [Header("진행")]
        public List<QuestObjective> objectives = new List<QuestObjective>();

        [Header("보상")]
        public int rewardMoney;
        public int rewardCredit;
        public ReputationScope rewardReputationScope;
        public string rewardReputationTarget;
        public ReputationSize rewardReputationSize;
        [Tooltip("보상 기록 필드가 없던 구 저장의 활성 의뢰에만 아래 이전 약속 적용")]
        public bool hasLegacyReward;
        public int legacyRewardMoney, legacyRewardCredit;
        [Tooltip("완료 대사. 일반 텍스트로 작성하며 긴 내용은 대화창 크기에 맞춰 자동 분할")]
        [TextArea] public string finishNote;
    }
}
