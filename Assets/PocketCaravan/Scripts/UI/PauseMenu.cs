using UnityEngine;

namespace PocketCaravan
{
    /// <summary>Esc 메뉴: 계속, 저장, 소리, 처음부터.</summary>
    public class PauseMenu : MonoBehaviour
    {
        bool open, confirmNew;
        string message = "";
        Vector2 issueScroll;
        Rect lastWindow, issueViewport;

        void Update()
        {
            if (!InputReader.CancelPressed()) return;
            if (open) { Close(); return; }
            if (UIState.Blocking || UIState.JustClosed || GameManager.I == null || GameManager.I.IsTravelling) return;
            if (!UIState.TryOpen(this, Close)) return;
            open = true; confirmNew = false; message = "";
            Sfx.Play("open");
        }

        void Close()
        {
            open = false;
            UIState.Release(this);
        }

        void OnDisable() { if (open) Close(); }

        // 11/08 은샘: 버튼만 쌓여 아래가 비던 메뉴를 정리. 위에 지금 어디·며칠째·돈, 단추마다 그림과 짧은 설명,
        // 처음부터는 빨간 단추로 한 번 더 묻는다.
        void OnGUI()
        {
            if (!open) return;
            UIStyle.Begin();
            GUI.depth = -10;   // 다른 글자들 위에
            var gm = GameManager.I;
            var issue = gm != null ? gm.LoadIssue : null;
            float w = Mathf.Min(340, UIStyle.VirtualWidth - 16);
            float issueHeight = string.IsNullOrEmpty(issue) ? 0 : CuteUI.WrapHeight(issue, w - 50, true) + 8;
            float shownIssue = Mathf.Min(issueHeight, 60);
            float h = Mathf.Min(UIStyle.VirtualHeight - 34, 42 + 5 * 30 + (string.IsNullOrEmpty(message) ? 4 : 20) + shownIssue + (shownIssue > 0 ? 8 : 0));
            var r = lastWindow = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), Mathf.Round((UIStyle.VirtualHeight - h) / 2 + 8), w, h);
            CuteUI.Dim(); CuteUI.Window(r); CuteUI.TitlePlate(r.center.x, r.y - 11, "게임 메뉴");
            CuteUI.Keycap(r.xMax - 32 - CuteUI.KeycapWidth("Esc") - 3, r.y - 3, "Esc");
            if (CuteUI.CloseButton(r.xMax - 32, r.y - 9)) { Close(); return; }

            // 지금 상태 한 줄
            float cy = r.y + 27, x = r.x + 16;
            if (gm != null)
            {
                var map = MapRoot.Current; var world = WorldIndex.Get();
                string place = map != null && world != null ? world.MapName(map.mapId) : "";
                CuteUI.Icon(CuteUI.Pic.Pin, x, cy - 5, 1, CuteUI.Sub);
                x = CuteUI.Label(x + 9, cy, TradeLedgerUI.Fit(place, 110, false, true), CuteUI.Text, false, true);
                CuteUI.Label(x + 8, cy, gm.Clock.Label, CuteUI.Sub, true);
                string money = gm.Player.money.ToString("N0") + "냥";
                CuteUI.LabelRight(r.xMax - 16, cy, money, CuteUI.Text, false, true);
                CuteUI.Icon(CuteUI.Pic.Coin7, r.xMax - 16 - CuteUI.Width(money, false, true) - 11, cy - 4);
            }
            float y = r.y + 42;
            if (issueHeight > 0)
            {
                issueViewport = new Rect(r.x + 16, y, w - 32, shownIssue);
                CuteUI.Box(new Rect(issueViewport.x - 2, y - 2, issueViewport.width + 4, shownIssue + 4), 6, CuteUI.AlertSoft, CuteUI.Alert);
                issueScroll.y = TradeLedgerUI.Scroll(issueViewport, issueScroll.y, issueHeight);
                GUI.BeginGroup(issueViewport);
                CuteUI.Wrap(4, 2 - issueScroll.y, w - 50, issue, CuteUI.Down, true);
                GUI.EndGroup();
                y += shownIssue + 8;
            }
            if (Row(ref y, r, null, "계속하기", "Esc", true)) Close();
            if (Row(ref y, r, CuteUI.Pic.OpenBook, "상업 백과", "고려와 교역 이야기"))
            {
                Close();
                gm.GetComponent<CommerceGuideUI>().Open();
                return;
            }
            if (Row(ref y, r, CuteUI.Pic.Scroll, "저장하기", "지금 진행을 남겨요")) { bool saved = gm.SaveNow(); message = saved ? "저장했어요." : "저장하지 못했어요. 기존 파일은 그대로예요."; Sfx.Play(saved ? "accept" : "cancel"); }
            if (Row(ref y, r, CuteUI.Pic.Speaker, AudioManager.Muted ? "소리 켜기" : "소리 끄기", AudioManager.Muted ? "지금 꺼져 있어요" : "지금 켜져 있어요")) AudioManager.ToggleMute();
            if (Row(ref y, r, CuteUI.Pic.XMark, confirmNew ? "정말 처음부터?" : "처음부터 시작", confirmNew ? "한 번 더 누르면 지워져요" : "지금 저장을 지워요", false, true))
            {
                if (confirmNew) { open = false; UIState.Release(this); GameManager.NewGame(); return; }
                confirmNew = true;
            }
            if (!string.IsNullOrEmpty(message)) CuteUI.LabelCenter(r.center.x, y + 2, message, CuteUI.Text, true);
        }

        /// <summary>메뉴 한 줄: 그림 + 이름 + 오른쪽 작은 설명. primary = 청록, danger = 빨강 테</summary>
        static bool Row(ref float y, Rect r, CuteUI.Pic? pic, string label, string note, bool primary = false, bool danger = false)
        {
            var b = new Rect(r.x + 16, y, r.width - 32, 24);
            y += 30;
            if (primary)
            {
                bool pressed = CuteUI.Button(b, label);
                CuteUI.Keycap(b.xMax - 8 - CuteUI.KeycapWidth(note), b.center.y - 6, note, true);
                return pressed;
            }
            bool hover = b.Contains(Event.current.mousePosition);
            CuteUI.Box(b, 8, danger ? (hover ? CuteUI.AlertSoft : CuteUI.Paper) : hover ? CuteUI.Paper2 : CuteUI.Paper, danger ? CuteUI.Alert : CuteUI.Ink, 1, 2, CuteUI.SoftShadow);
            if (pic.HasValue)
            {
                var ps = CuteUI.PicSize(pic.Value);
                CuteUI.Icon(pic.Value, b.x + 14 - ps.x / 2f, b.center.y - ps.y / 2f, 1, danger ? CuteUI.Down : (Color32?)null);
            }
            CuteUI.Label(b.x + 28, b.center.y, label, danger ? CuteUI.Down : CuteUI.Text, false, true);
            CuteUI.LabelRight(b.xMax - 10, b.center.y, note, CuteUI.Sub, true);
            return GUI.Button(b, GUIContent.none, GUIStyle.none);
        }
    }
}
