using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/11 은샘: 길 안내 대상 하나 (위 알약 · 노란 화살표 · 아래 목표 줄이 이것만 따라간다).
    /// - Quest: 받은 퀘스트 중 수첩에서 고른 것 (QuestManager.Tracked)
    /// - Lead: 아직 받지 않은 다음 이야기 (튜토리얼 뒤 "첫 일거리" 등, StoryGuide) — 그 사람에게 가면 퀘스트가 이어진다
    /// - Event: 이벤트 퀘스트 (만국 상인회, NetGuild)
    /// - None: 수첩에서 길 안내를 끈 상태
    /// 가장 최근에 받은 것이 대상이 된다 (퀘스트 수락 · 상인회 초대장/답장 · 튜토리얼이 끝나 첫 일거리가 생김). Q 수첩에서 바꿀 수 있다.
    /// 고른 대상이 끝나서 없어지면 남은 것 중에서 고른다 (퀘스트 → 이벤트 → 다음 이야기). 저장: 공용 메모 "focus:kind".
    /// </summary>
    public static class QuestFocus
    {
        public enum Kind { None = 0, Quest = 1, Lead = 2, Event = 3 }
        const string Key = "focus:kind";

        /// <summary>고른 것 (메모가 없는 예전 저장은 퀘스트)</summary>
        public static Kind Chosen
        {
            get
            {
                var gm = GameManager.I;
                if (gm == null || !gm.HasMemo(Key)) return Kind.Quest;
                return (Kind)Mathf.Clamp(Mathf.RoundToInt(gm.GetMemo(Key)), 0, 3);
            }
        }

        public static void Set(Kind k) { if (GameManager.I != null) GameManager.I.SetMemo(Key, (int)k); }

        static bool Has(Kind k)
        {
            var gm = GameManager.I;
            switch (k)
            {
                case Kind.Quest: return gm != null && gm.Quests != null && gm.Quests.Tracked != null;
                case Kind.Lead: return StoryGuide.LeadAvailable;
                case Kind.Event: return NetGuild.Active;
                default: return false;
            }
        }

        /// <summary>지금 길 안내 대상. 고른 것이 없어졌으면 남은 것 중에서 (끈 상태면 None 그대로)</summary>
        public static Kind Current
        {
            get
            {
                var k = Chosen;
                if (k == Kind.None || Has(k)) return k;
                if (Has(Kind.Quest)) return Kind.Quest;
                if (Has(Kind.Event)) return Kind.Event;
                if (Has(Kind.Lead)) return Kind.Lead;
                return Kind.None;
            }
        }

        /// <summary>수첩 단추: 이미 안내 중이면 끄고, 아니면 이것으로</summary>
        public static void Toggle(Kind k) => Set(Current == k ? Kind.None : k);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            QuestManager.Accepted -= OnAccepted;
            QuestManager.Accepted += OnAccepted;
        }
        static void OnAccepted(QuestManager.Progress p) => Set(Kind.Quest);
    }
}
