using UnityEngine;

namespace PocketCaravan
{
    /// <summary>Esc 메뉴: 계속, 저장, 소리, 처음부터.</summary>
    public class PauseMenu : MonoBehaviour
    {
        bool open, confirmNew;
        string message = "";
        Vector2 issueScroll;

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
            var issue = GameManager.I != null ? GameManager.I.LoadIssue : null;
            var statusStyle = new GUIStyle(UIStyle.Wrap) { richText = false };
            float w = Mathf.Min(360, UIStyle.VirtualWidth - 16);
            float issueHeight = string.IsNullOrEmpty(issue) ? 0 : statusStyle.CalcHeight(new GUIContent(issue), w-50)+8;
            float h = Mathf.Min(344, 192+issueHeight);
            var r = new Rect((UIStyle.VirtualWidth - w) / 2, (UIStyle.VirtualHeight - h) / 2, w, h);
            GUI.Box(r, "", UIStyle.Box);
            GUI.Label(new Rect(r.x, r.y + 8, w, 14), "Pocket Caravan", UIStyle.CenterRich);
            float y = r.y + 28;
            if (issueHeight > 0)
            {
                float shown = Mathf.Min(issueHeight,h-192);
                issueScroll = GUI.BeginScrollView(new Rect(r.x+16,y,w-32,shown),issueScroll,new Rect(0,0,w-50,issueHeight));
                GUI.Label(new Rect(0,0,w-50,issueHeight),issue,statusStyle);
                GUI.EndScrollView();
                y += shown;
            }
            if (Button(ref y, r, "계속하기 (Esc)")) Close();
            if (Button(ref y, r, "상업 백과"))
            {
                Close();
                GameManager.I.GetComponent<CommerceGuideUI>().Open();
                return;
            }
            if (Button(ref y, r, "저장하기")) { bool saved = GameManager.I.SaveNow(); message = saved ? "저장했다." : "저장하지 못했습니다. 기존 파일은 보존됩니다."; Sfx.Play(saved ? "accept" : "cancel"); }
            if (Button(ref y, r, AudioManager.Muted ? "소리 켜기" : "소리 끄기")) AudioManager.ToggleMute();
            if (Button(ref y, r, confirmNew ? "정말 처음부터? (한 번 더)" : "처음부터 시작"))
            {
                if (confirmNew) { open = false; UIState.Release(); GameManager.NewGame(); return; }
                confirmNew = true;
            }
            GUI.Label(new Rect(r.x+16, r.yMax-36, w-32, 30), message, statusStyle);
        }

        static bool Button(ref float y, Rect r, string label)
        {
            bool b = GUI.Button(new Rect(r.x + 16, y, r.width - 32, 20), label, UIStyle.Button);
            y += 24;
            return b;
        }
    }
}
