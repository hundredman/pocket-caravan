using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 베네치아 1장 Q3 "깨지기 쉬운 짐"의 정직 시험 (2026-10-26, 제안. 공용 퀘스트·저장 형식은 바꾸지 않는다).
    /// Q3(venice_q3_crate)를 하는 동안 키오자 창고지기 옆에 부두 검수원이 선다. 상자 하나에 금이 간 걸 보여 주며 묻는다:
    ///  - "옮기다 제가 그랬습니다" (정직): 변상 10냥, 신용 +2. "정직한 상인"으로 소문난다.
    ///  - "원래 금이 가 있었소" (거짓): 공방에 물린 값 중 15냥을 받지만 신용 −1.
    /// 한 번만(VeniceSave "honesty"). 주행 중 낙하는 쓰지 않는다(이전 Q3 낙하 연출은 팀 결정으로 제외).
    /// </summary>
    public class VeniceHonesty : Interactable
    {
        public const string QuestId = "venice_q3_crate";

        public override string DisplayName => "부두 검수원";
        public override Kind Category => Kind.Quest;

        public static bool Active
        {
            get
            {
                var gm = GameManager.I;
                if (gm == null || VeniceSave.Has("honesty")) return false;
                foreach (var p in gm.Quests.Active)
                    if (p.quest != null && p.quest.id == QuestId && !p.Done) return true;
                return false;
            }
        }

        void Start()
        {
            var r = gameObject.AddComponent<SpriteRenderer>();
            r.sprite = VeniceUtil.Sprite("inspector");
            r.sortingOrder = 10;
            transform.position += new Vector3(0f, -.5f, 0f);    // 발밑 기준 그림을 칸 아래쪽에 맞춘다
        }

        void Update()
        {
            if (!Active) gameObject.SetActive(false);
        }

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.I;
            DialogueUI.Show("부두 검수원", "잠깐, 그 유리 상자 좀 보세.\n하나에 금이 갔군. 공방에 '원래 금 간 물건'이라 적으면\n값을 물어 받을 수 있지. 어떻게 적을까?",
                new DialogueUI.Choice("옮기다 제가 그랬습니다 (변상 10냥)", () =>
                {
                    VeniceSave.Set("honesty", 1f);
                    gm.Player.money = Mathf.Max(0, gm.Player.money - 10);
                    gm.Player.credit += 2;
                    Sfx.Play("success");
                    DialogueUI.Show("부두 검수원", "허허, 요즘 보기 드문 상인이군.\n이 일은 장부에 좋게 적어 두지.");
                    Notify.Show("정직한 상인으로 소문이 났다. 신용 +2", 4f);
                }),
                new DialogueUI.Choice("원래 금이 가 있었소 (15냥 받기)", () =>
                {
                    VeniceSave.Set("honesty", -1f);
                    gm.Player.money += 15;
                    gm.Player.credit = Mathf.Max(0, gm.Player.credit - 1);
                    Sfx.Play("coin");
                    DialogueUI.Show("부두 검수원", "…그렇게 적지. 15냥은 자네 몫이야.");
                    Notify.Show("뒷맛이 개운치 않다. 신용 −1", 4f);
                }));
        }
    }
}
