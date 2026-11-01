using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/01 북유럽 퀘스트 동시 진행 제한 (기우: ! 퀘스트를 다 끝내지 않고 계속 받을 수 있어 꼬임).
    /// 북유럽 퀘스트는 한 번에 3개까지, 그중 마을 곁가지("북쪽 마을 이야기")는 2개까지.
    /// 사람이 주지 않는 자동 퀘스트(눈보라 N4)는 세지만 막지 않는다. 다른 지역 퀘스트는 건드리지 않는다.
    /// 공용 QuestManager.ExtraAcceptBlocker 자리에 조건만 더한다.
    /// </summary>
    public static class NorthQuestLimit
    {
        public const int MaxNorth = 3, MaxSide = 2;
        const string SideChain = "북쪽 마을 이야기";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() { QuestManager.ExtraAcceptBlocker = Check; }

        static bool IsNorth(QuestData q) => q != null && q.id != null && q.id.StartsWith("north_");

        static string Check(QuestData q)
        {
            if (!IsNorth(q) || string.IsNullOrEmpty(q.giverNpc) || GameManager.I == null) return null;
            int north = 0, side = 0;
            foreach (var p in GameManager.I.Quests.Active)
            {
                if (p == null || !IsNorth(p.quest)) continue;
                north++;
                if (p.quest.chain == SideChain) side++;
            }
            if (north >= MaxNorth)
                return $"자네 지금 맡은 일이 {north}개나 되는구먼. 하던 일부터 끝내고 오게.\n(북유럽 의뢰는 한 번에 {MaxNorth}개까지)";
            if (q.chain == SideChain && side >= MaxSide)
                return $"마을 부탁을 벌써 {side}개나 들고 다니는군. 하나 끝내고 다시 들르게.\n(마을 곁가지는 한 번에 {MaxSide}개까지)";
            return null;
        }
    }
}
