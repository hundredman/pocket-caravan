using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 퀘스트를 주고받는 NPC. 머리 위에 ! (받을 퀘스트), ? (완료할 퀘스트) 표시가 뜬다.
    /// npcId는 QuestData의 giverNpc, QuestObjective의 targetId와 같아야 한다.
    /// </summary>
    public class QuestNpc : Interactable
    {
        public string npcId;
        public string displayName;
        [TextArea] public string greeting = "좋은 날일세.";

        [Header("머리 위 표시")]
        public SpriteRenderer marker;
        public Sprite availableSprite, turnInSprite, inProgressSprite;

        float bobTime;

        public override string DisplayName => displayName;
        public override Kind Category => Kind.Quest;

        public override void Interact(PlayerController player)
        {
            var qm = GameManager.I.Quests;

            // 1) 이 NPC에게서 끝나는 단계
            string line = qm.TryCompleteAt(npcId, out string fail);
            if (line != null)
            {
                DialogueUI.Show(displayName, line, new DialogueUI.Choice("확인", () => OfferNextIfAny()));
                return;
            }
            if (fail != null)
            {
                DialogueUI.Show(displayName, fail);
                return;
            }

            // 2) 새로 줄 퀘스트
            if (!OfferNextIfAny()) DialogueUI.Show(displayName, greeting);
        }

        /// <summary>줄 퀘스트가 있으면 제안 대화를 연다.</summary>
        bool OfferNextIfAny()
        {
            var qm = GameManager.I.Quests;
            var q = qm.AvailableFrom(npcId);
            if (q == null) return false;
            DialogueUI.Show(displayName, $"<b>[{q.title}]</b>\n{q.offerLine}",
                new DialogueUI.Choice("수락", () => qm.Accept(q)),
                new DialogueUI.Choice("나중에", null));
            return true;
        }

        void Update()
        {
            if (marker == null || GameManager.I == null) return;
            var m = GameManager.I.Quests.MarkerFor(npcId);
            marker.sprite = m == QuestMarker.Available ? availableSprite
                : m == QuestMarker.TurnIn ? turnInSprite
                : m == QuestMarker.InProgress ? inProgressSprite : null;
            bobTime += Time.deltaTime;
            float bob = Mathf.Floor(Mathf.Sin(bobTime * 4f) * 1.5f + 1.5f) / 16f; // 픽셀 단위로 통통
            marker.transform.localPosition = new Vector3(0f, 1.45f + bob, 0f); // 이름표 위
        }
    }
}
