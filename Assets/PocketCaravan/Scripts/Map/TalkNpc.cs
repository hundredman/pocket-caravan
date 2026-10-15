using UnityEngine;

namespace PocketCaravan
{
    /// <summary>말을 걸면 한마디 하는 마을 사람. 여러 줄이면 말을 걸 때마다 다음 줄.</summary>
    public class TalkNpc : Interactable
    {
        public string displayName = "마을 사람";
        [Tooltip("배열 항목은 말을 다시 걸 때 바뀜. 한 항목 안의 긴 대사는 Space로 넘기는 여러 페이지로 자동 분할")]
        [TextArea] public string[] lines = { "좋은 날이오." };
        int next;

        public override string DisplayName => displayName;

        public override void Interact(PlayerController player)
        {
            if (lines == null || lines.Length == 0) return;
            DialogueUI.Show(displayName, lines[next % lines.Length]);
            next++;
        }
    }
}
