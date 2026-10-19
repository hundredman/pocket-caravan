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
                DialogueUI.Show(displayName, fail,
                    new DialogueUI.Choice("다른 의뢰", () => { if (!OfferNextIfAny()) DialogueUI.Show(displayName, greeting); }),
                    new DialogueUI.Choice("닫기", null));
                return;
            }

            // 2) 새로 줄 퀘스트
            if (!OfferNextIfAny()) DialogueUI.Show(displayName, greeting);
        }

        /// <summary>한 번에 한 의뢰를 읽고 다른 제안을 넘겨 볼 수 있다. Esc는 항상 닫기.</summary>
        bool OfferNextIfAny() => OfferAt(0);

        bool OfferAt(int index)
        {
            var qm = GameManager.I.Quests;
            var offers = qm.AvailableAllFrom(npcId);
            if (offers.Count == 0) return false;
            index = index % offers.Count;
            var q = offers[index];
            string cost = q.offerItem != null && q.offerCount > 0
                ? $"\n수락 비용: {q.offerPrice * q.offerCount}냥 ({q.offerItem.displayName} {q.offerCount}개)" : "";
            string reward = $"\n보상: {q.rewardMoney}냥 · 신용 {q.rewardCredit}";
            if (!string.IsNullOrEmpty(q.choiceGroup)) reward += "\n같은 운송 입문 의뢰와 둘 중 하나만 선택합니다.";
            var choices = new System.Collections.Generic.List<DialogueUI.Choice>();
            choices.Add(new DialogueUI.Choice("수락", () =>
            {
                string why = qm.AcceptBlocker(q);
                if (why != null) DialogueUI.Show(displayName, why,
                    new DialogueUI.Choice("의뢰 보기", () => OfferAt(index)),
                    new DialogueUI.Choice("닫기", null));
                else qm.Accept(q);
            }));
            if (offers.Count > 1) choices.Add(new DialogueUI.Choice("다른 의뢰", () => OfferAt(index + 1)));
            choices.Add(new DialogueUI.Choice("나중에", null));
            DialogueUI.Show(displayName, $"의뢰 {index + 1}/{offers.Count} · [{q.title}]\n{q.offerLine}{cost}{reward}", choices.ToArray());
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
            marker.transform.localPosition = new Vector3(0f, 1.6f + bob, 0f); // 이름표 위
        }
    }
}
