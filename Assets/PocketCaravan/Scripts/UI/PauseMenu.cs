using UnityEngine;

namespace PocketCaravan
{
    /// <summary>Esc 메뉴: 계속, 저장, 소리, 처음부터.</summary>
    public class PauseMenu : MonoBehaviour
    {
        bool open, confirmNew;
        string message = "";

        void Update()
        {
            if (!InputReader.CancelPressed()) return;
            if (open) { Close(); return; }
            if (UIState.Blocking || UIState.JustClosed || GameManager.I == null || GameManager.I.IsTravelling) return;
            open = true; confirmNew = false; message = "";
            UIState.Blocking = true;
            Sfx.Play("open");
        }

        void Close()
        {
            open = false;
            UIState.Release();
        }

        void OnGUI()
        {
            if (!open) return;
            UIStyle.Begin();
            GUI.depth = -10;   // 다른 글자들 위에
            float w = 180, h = 150;
            var r = new Rect((UIStyle.VirtualWidth - w) / 2, (UIStyle.VirtualHeight - h) / 2, w, h);
            GUI.Box(r, "", UIStyle.Box);
            GUI.Label(new Rect(r.x, r.y + 8, w, 14), "Pocket Caravan", UIStyle.CenterRich);
            float y = r.y + 28;
            if (Button(ref y, r, "계속하기 (Esc)")) Close();
            if (Button(ref y, r, "저장하기")) { GameManager.I.SaveNow(); message = "저장했다."; Sfx.Play("accept"); }
            if (Button(ref y, r, AudioManager.Muted ? "소리 켜기" : "소리 끄기")) AudioManager.ToggleMute();
            if (Button(ref y, r, confirmNew ? "정말 처음부터? (한 번 더)" : "처음부터 시작"))
            {
                if (confirmNew) { open = false; UIState.Release(); GameManager.NewGame(); return; }
                confirmNew = true;
            }
            GUI.Label(new Rect(r.x, r.yMax - 18, w, 14), message, UIStyle.CenterRich);
        }

        static bool Button(ref float y, Rect r, string label)
        {
            bool b = GUI.Button(new Rect(r.x + 16, y, r.width - 32, 20), label, UIStyle.Button);
            y += 24;
            return b;
        }
    }
}
