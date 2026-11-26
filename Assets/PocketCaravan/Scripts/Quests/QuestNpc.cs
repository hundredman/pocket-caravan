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

        protected override void OnEnable()
        {
            base.OnEnable();
            // 11/19 은샘 "npc나 유저가 순록의 발보다 아래에 있으면 순록이 가려진다": 이야기 NPC도 짐승·좌판과 발 높이로 앞뒤를 맞춘다 (TalkNpc와 같다)
            if (GetComponent<DepthActor>() == null) gameObject.AddComponent<DepthActor>();
        }

        /// <summary>11/08 마지막으로 퀘스트 대화(받기·완료·조건 부족)가 열린 프레임. 여관 주인이 퀘스트 대화 뒤에는 "쉬어 갈래요?"를 묻지 않게 한다</summary>
        public static int QuestTalkFrame { get; private set; } = -1;

        public override void Interact(PlayerController player)
        {
            var qm = GameManager.I.Quests;

            // 1) 이 NPC에게서 끝나는 단계
            string line = qm.TryCompleteAt(npcId, out string fail);
            if (line != null)
            {
                QuestTalkFrame = Time.frameCount;
                DialogueUI.Show(displayName, line, new DialogueUI.Choice("확인", () => OfferNextIfAny()));
                return;
            }
            if (fail != null)
            {
                QuestTalkFrame = Time.frameCount;
                DialogueUI.Show(displayName, fail,
                    new DialogueUI.Choice("다른 의뢰", () => { if (!OfferNextIfAny()) Greet(); }),
                    new DialogueUI.Choice("닫기", null));
                return;
            }

            // 2) 새로 줄 퀘스트
            if (OfferNextIfAny()) QuestTalkFrame = Time.frameCount;
            else Greet();
        }

        const string DropLabel = "맡은 일 물리기";

        /// <summary>11/26 인사. 이 사람에게 받은 의뢰를 하고 있으면 [맡은 일 물리기]를 고를 수 있다 (의뢰 포기는 준 사람에게 말해서 한다)</summary>
        void Greet()
        {
            var drop = GameManager.I.Quests.DroppableFrom(npcId);
            if (drop == null) { DialogueUI.Show(displayName, greeting); return; }
            QuestTalkFrame = Time.frameCount;
            DialogueUI.Show(displayName, greeting, new DialogueUI.Choice(DropLabel, () => AskDrop(drop)), new DialogueUI.Choice("닫기", null));
        }

        void AskDrop(QuestManager.Progress p)
        {
            var qm = GameManager.I.Quests;
            string why = qm.AbandonBlocker(p);
            if (why != null) { DialogueUI.Show(displayName, why); return; }
            DialogueUI.Show(displayName, qm.DropAskLine(p),
                new DialogueUI.Choice("계속 할게요", null),
                new DialogueUI.Choice("의뢰 포기", () => { if (qm.Abandon(p)) GameManager.I.SaveNow(); }));
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
            if (q.rewardReputationSize != ReputationSize.None) reward += " · " + ReputationAwards.TargetName(GameManager.I.Db, q.rewardReputationScope, q.rewardReputationTarget) + " 평판 기여 " + ReputationAwards.SizeName(q.rewardReputationSize);
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
            var drop = qm.DroppableFrom(npcId);
            if (drop != null) choices.Add(new DialogueUI.Choice(DropLabel, () => AskDrop(drop)));
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
