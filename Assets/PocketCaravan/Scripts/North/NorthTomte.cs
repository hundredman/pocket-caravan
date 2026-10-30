using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// Q3 톰테의 정직 시험. 피오르 나루 창고 문 앞의 톰테 할아버지 (주행 중 낙하는 쓰지 않음).
    /// 맵에는 일반 마을 사람(talker "톰테 할아버지")으로 놓고, NorthQuestDirector가 이 컴포넌트로 바꿔 끼운다.
    /// 금도끼 은도끼의 정직 시험을 북유럽 톰테(집 정령) 이야기로 바꿨다. 정직하면 신용이 크게 오른다.
    /// </summary>
    public class NorthTomte : Interactable
    {
        public const string NpcId = "north_tomte";
        public const string DisplayLabel = "톰테 할아버지";
        public string[] idleLines = { "흠흠. 예의 바른 손님은 언제나 환영이지.", "창고를 지키는 건 내 일이지. 정직한 상인에겐 문이 활짝 열린단다." };
        int next;

        public override string DisplayName => DisplayLabel;
        public override Kind Category => Kind.Quest;

        public override void Interact(PlayerController player)
        {
            var qm = GameManager.I != null ? GameManager.I.Quests : null;
            if (qm == null || qm.WaitingAt(NpcId) == null)
            {
                DialogueUI.Show(DisplayLabel, idleLines[next++ % idleLines.Length]);
                return;
            }
            DialogueUI.Show(DisplayLabel,
                "나는 이 창고를 지키는 톰테다. 들어가기 전에 하나 묻지. 여기 금단지, 은단지, 흙단지가 있다. 네가 가져온 단지는 어느 것이냐?",
                new DialogueUI.Choice("금단지입니다", () => Answer(false)),
                new DialogueUI.Choice("은단지입니다", () => Answer(false)),
                new DialogueUI.Choice("제 것은 흙단지입니다", () => Answer(true)));
        }

        void Answer(bool honest)
        {
            var gm = GameManager.I;
            gm.Quests.TryCompleteAt(NpcId, out _);
            if (honest)
            {
                gm.Player.credit += 2;
                Sfx.Play("success");
                DialogueUI.Show(DisplayLabel, "허허, 정직하구나! 예의 바른 상인은 어디서나 믿음을 얻지. 어서 들어가 창고지기에게 단지를 건네렴. (신용 +2 덤)");
            }
            else
            {
                gm.Player.credit -= 2;
                gm.Player.money = Mathf.Max(0, gm.Player.money - 10);
                DialogueUI.Show(DisplayLabel, "쯧쯧, 욕심이 앞서는구나. 금단지도 은단지도 네 것이 아니지. 들어가도 좋다만, 소문은 빨리 돈단다. (신용 −2, 10냥 사례비)");
            }
        }
    }
}
