using UnityEngine;

namespace PocketCaravan
{
    /// <summary>말을 걸면 한마디 하는 마을 사람. 여러 줄이면 말을 걸 때마다 다음 줄.</summary>
    public class TalkNpc : Interactable
    {
        public string displayName = "마을 사람";
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
