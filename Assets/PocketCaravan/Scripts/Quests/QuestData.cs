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
        [Tooltip("이 단계를 마칠 때 상대가 하는 말 (Talk, Deliver)")]
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
        public string title;
        [Tooltip("튜토리얼 등 연속 퀘스트 이름 (퀘스트 목록에 묶어 보임)")] public string chain;
        [TextArea] public string summary;

        [Header("받기")]
        [Tooltip("퀘스트를 주는 NPC id")] public string giverNpc;
        public List<QuestData> requires = new List<QuestData>();
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
        [TextArea] public string finishNote;
    }
}
