using UnityEditor;

namespace PocketCaravan.EditorTools
{
    /// <summary>Keep data regeneration consistent with Standing section 2 (R6).</summary>
    public static class R6RewardUpgrade
    {
        public static void Apply(GameDatabase db)
        {
            foreach (var q in db.quests)
            {
                if (q == null) continue;
                switch (q.id)
                {
                    case "tutorial_3": case "tutorial_5": case "naru_board":
                    case "goryeo_jangdan_trade": case "goryeo_songak_trade": case "goryeo_return_trade":
                        if (!q.hasLegacyReward)
                        {
                            q.hasLegacyReward = true;
                            q.legacyRewardMoney = q.rewardMoney;
                            q.legacyRewardCredit = q.id == "tutorial_5" ? 2 : 1;
                        }
                        q.rewardCredit = 0;
                        q.rewardReputationScope = ReputationScope.Civilization;
                        q.rewardReputationTarget = "goryeo";
                        q.rewardReputationSize = ReputationSize.Small;
                        EditorUtility.SetDirty(q);
                        break;
                }
            }
        }
    }
}
