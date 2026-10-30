using System;
using UnityEditor;

namespace PocketCaravan.EditorTools
{
    public static class Q1QuestUpgrade
    {
        public static void Apply(GameDatabase db)
        {
            foreach (string name in new[] { "naru", "jangdan", "songak", "dakmok", "gaegyeong" })
            {
                var q = AssetDatabase.LoadAssetAtPath<QuestData>("Assets/PocketCaravan/Data/Goryeo/Quests/Q1_" + name + ".asset");
                if (q == null) throw new InvalidOperationException("Q1 원본을 먼저 생성해야 합니다: " + name);
                if (!db.quests.Contains(q)) db.quests.Add(q);
            }
            foreach (var q in db.quests)
            {
                if (q == null) continue;
                if (q.id == "tutorial_1" || q.id == "tutorial_2" || q.id == "naru_board" || q.id == "dakmok_hanji") q.retired = true;
                string prerequisite = q.id == "tutorial_3" ? "q1_gaegyeong" : q.id == "tutorial_4" || q.id == "goryeo_songak_trade" ? "q1_songak"
                    : q.id == "tutorial_5" ? "q1_dakmok" : q.id == "goryeo_jangdan_trade" ? "q1_jangdan" : null;
                if (prerequisite != null) { q.requires.Clear(); q.requires.Add(db.quests.Find(value => value.id == prerequisite)); }
                EditorUtility.SetDirty(q);
            }
            EditorUtility.SetDirty(db);
        }
    }
}
