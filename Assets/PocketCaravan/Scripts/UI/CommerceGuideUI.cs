using System;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 선택해서 읽는 상업 백과. 역사적 모티브와 게임 각색을 분리하며 경제 상태를 바꾸지 않는다.
    /// 11/18 윤채 (UT "글씨가 너무 많고 눈에 안 들어온다"): 한 번에 한 가지만 보이게 바꿨다.
    /// - 왼쪽 항목 목록, 오른쪽에 그 항목 제목 + 탭 셋(역사 속에서 · 게임에서는 · 여행 팁)과 탭 하나의 글만.
    /// - 위 머리말·안내 줄, 아래 "목록에서 항목을 고르세요"·닫기 단추는 뺐다 (오른쪽 위 X · Esc).
    /// - 글은 띄어쓰기에서만 줄을 바꾸고(CuteUI.WrapWords) 줄 사이를 띄운다. 참고 자료는 "역사 속에서" 탭 아래 작은 링크로.
    /// - ↑↓ 항목 · ←→ 탭. 마우스로도 누른다. 내용(JSON)은 그대로 쓴다.
    /// </summary>
    public sealed class CommerceGuideUI : MonoBehaviour
    {
        [Serializable] public class Source { public string title, url; }
        [Serializable] public class Card
        {
            public string id, title, history, adaptation, travel;
            public Source[] sources;
        }
        [Serializable] public class Book { public string heading; public Card[] cards; }

        Book goryeo, venice;
        /// <summary>
        /// 보고 있는 지역 백과. 11/18 윤채 ("베네치아에 있는데 왜 고려가 뜨니"): 예전엔 맵 이름이 venice_로 시작할 때만 베네치아,
        /// 그 밖(북유럽·아라비아·가운데 대륙 포함)은 모두 고려였다 → 열 때 맵의 지역(MapRoot.region)으로 고르고,
        /// 그 지역 백과가 아직 없으면 내 출신 지역, 그것도 없으면 고려. 위 지역 단추로 직접 바꿔 볼 수도 있다.
        /// </summary>
        public Book Content => book == "venice" && venice != null ? venice : goryeo != null ? goryeo : venice;
        string RegionName => Content == venice && venice != null ? "베네치아" : "고려";
        string book = "goryeo", missingRegion;

        Book BookOf(string civ) => civ == "venice" ? venice : civ == "goryeo" ? goryeo : null;

        /// <summary>열 때 고르는 백과: 지금 맵의 지역 → 내 출신 → 고려. 지역 백과가 없으면 그 지역 이름을 남겨 "준비 중"으로 알려 준다</summary>
        void PickBook()
        {
            var map = MapRoot.Current;
            string here = map != null ? OriginPerks.Civ(map.region) : "";
            missingRegion = null;
            if (BookOf(here) != null) { book = here; return; }
            if (!string.IsNullOrEmpty(here)) missingRegion = OriginPerks.CivName(here);
            var origin = GameManager.I != null ? GameManager.I.Player.origin : null;
            string home = origin == null ? "" : origin.id == "viking" ? "north" : origin.id;
            book = BookOf(home) != null ? home : "goryeo";
        }
        public bool IsOpen { get; private set; }
        int selected, tab;
        float listScroll, pageScroll;
        Vector2 lastMove;

        static readonly string[] TabNames = { "역사 속에서", "게임에서는", "여행 팁" };
        static readonly CuteUI.Pic[] TabPics = { CuteUI.Pic.Scroll, CuteUI.Pic.Star4, CuteUI.Pic.Pin };

        void Awake()
        {
            var text = Resources.Load<TextAsset>("CommerceGuide/Goryeo");
            if (text != null) goryeo = JsonUtility.FromJson<Book>(text.text);
            var v = Resources.Load<TextAsset>("CommerceGuide/Venice");
            if (v != null) venice = JsonUtility.FromJson<Book>(v.text);
        }

        public void Open()
        {
            var gm = GameManager.I;
            if (IsOpen || UIState.Blocking || gm == null || gm.IsTravelling) return;
            if (!UIState.TryOpen(this, Close)) return;
            IsOpen = true;
            PickBook();
            selected = 0; tab = 0; listScroll = pageScroll = 0f;
            lastMove = Vector2.one;   // 열 때 눌려 있던 방향키는 무시
            Sfx.Play("open");
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            UIState.Release(this);
        }

        void Update()
        {
            if (!IsOpen) return;
            if (InputReader.CancelPressed()) { Close(); return; }
            // ↑↓ 항목 · ←→ 탭 (누른 순간 한 번)
            Vector2 mv = InputReader.MoveVector();
            bool fresh = mv.sqrMagnitude > .25f && lastMove.sqrMagnitude <= .25f;
            lastMove = mv;
            if (!fresh || Content == null || Content.cards == null || Content.cards.Length == 0) return;
            if (Mathf.Abs(mv.y) > Mathf.Abs(mv.x)) Select(selected - (int)Mathf.Sign(mv.y), tab);
            else Select(selected, (tab + (mv.x > 0 ? 1 : 2)) % 3);
        }

        void Select(int card, int newTab)
        {
            card = Mathf.Clamp(card, 0, Content.cards.Length - 1);
            if (card == selected && newTab == tab) return;
            if (card != selected) newTab = 0;
            selected = card; tab = newTab; pageScroll = 0f;
            Sfx.Play("open", .4f);
        }

        void OnDisable() => Close();

        void OnGUI()
        {
            if (!IsOpen) return;
            UIStyle.Begin();
            GUI.depth = -10;
            float width = Mathf.Min(560, UIStyle.VirtualWidth - 16), height = UIStyle.WindowHeight(300) - 6;
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - width) / 2), Mathf.Round((UIStyle.VirtualHeight - height) / 2 + 6), width, height);
            CuteUI.Dim(); CuteUI.Window(r); CuteUI.TitlePlate(r.center.x, r.y - 11, $"상업 백과 · {RegionName}");
            CuteUI.Keycap(r.xMax - 32 - CuteUI.KeycapWidth("Esc") - 3, r.y - 3, "Esc");
            if (CuteUI.CloseButton(r.xMax - 32f, r.y - 9f)) { Close(); return; }
            if (Content == null || Content.cards == null || Content.cards.Length == 0)
            {
                CuteUI.LabelCenter(r.center.x, r.center.y, "백과를 아직 준비하지 못했어요", CuteUI.Sub);
                return;
            }
            selected = Mathf.Clamp(selected, 0, Content.cards.Length - 1);

            // ---- 왼쪽: 항목 목록 ----
            float listW = Mathf.Round(Mathf.Clamp(width * .27f, 112, 140));
            // 11/18 지역 단추 (백과가 있는 지역만): 누르면 그 지역 백과
            float bx = r.x + 16;
            foreach (var (civ, label) in new[] { ("goryeo", "고려"), ("venice", "베네치아") })
            {
                if (BookOf(civ) == null) continue;
                bool on = BookOf(civ) == Content;
                float bw = CuteUI.Width(label, true, on) + 16;
                var br = new Rect(bx, r.y + 20, bw, 16);
                bool hv = !on && br.Contains(Event.current.mousePosition);
                CuteUI.Box(br, 7, on ? CuteUI.Frame : hv ? CuteUI.Paper2 : CuteUI.Paper, on ? CuteUI.Ink : CuteUI.Line, 1);
                CuteUI.LabelCenter(br.center.x, br.center.y, label, on ? CuteUI.White : CuteUI.Sub, true, on);
                if (CuteUI.Hit(br, GUIContent.none, GUIStyle.none) && !on) { book = civ; selected = 0; tab = 0; listScroll = pageScroll = 0f; Sfx.Play("open", .4f); }
                bx = br.xMax + 4;
            }
            float listBottom = r.yMax - 16 - (missingRegion != null ? 18 : 0);
            if (missingRegion != null) CuteUI.Label(r.x + 16, listBottom + 9, $"{missingRegion} 백과는 준비 중이에요", CuteUI.Sub, true);
            var list = new Rect(r.x + 16, r.y + 44, listW, listBottom - (r.y + 44));
            const float rowH = 24, gap = 5;
            float listH = Content.cards.Length * (rowH + gap) - gap;
            listScroll = TradeLedgerUI.Scroll(list, listScroll, listH);
            GUI.BeginGroup(new Rect(list.x - 2, list.y - 2, list.width + 4, list.height + 4));
            for (int i = 0; i < Content.cards.Length; i++)
            {
                var row = new Rect(2, 2 + i * (rowH + gap) - listScroll, listW, rowH);
                if (row.yMax < 0 || row.y > list.height + 4) continue;
                bool on = i == selected, hover = !on && row.Contains(Event.current.mousePosition);
                if (on) CuteUI.Box(row, 7, CuteUI.Interact, CuteUI.Ink, 1, 2, CuteUI.InteractLo);
                else CuteUI.Box(row, 7, hover ? CuteUI.Paper2 : CuteUI.Paper, on ? CuteUI.Ink : CuteUI.Line, 1);
                string title = TradeLedgerUI.Fit(Content.cards[i].title, listW - 20, false, on);
                CuteUI.Label(row.x + 10, row.center.y, title, on ? CuteUI.White : CuteUI.Text, false, on);
                if (CuteUI.Hit(row, GUIContent.none, GUIStyle.none)) Select(i, i == selected ? tab : 0);
            }
            GUI.EndGroup();

            // ---- 오른쪽: 제목 + 탭 + 글 하나 ----
            var card = Content.cards[selected];
            float px = list.xMax + 16, pw = r.xMax - 16 - px, py = r.y + 22;
            CuteUI.Label(px, py + 8, card.title, CuteUI.Text, false, true);
            py += 22;
            // 탭: 비어 있는 탭은 건너뛴다
            string[] bodies = { card.history, card.adaptation, card.travel };
            if (string.IsNullOrEmpty(bodies[tab])) for (int k = 0; k < 3; k++) if (!string.IsNullOrEmpty(bodies[k])) { tab = k; break; }
            float tx = px;
            for (int k = 0; k < 3; k++)
            {
                if (string.IsNullOrEmpty(bodies[k])) continue;
                bool on = k == tab;
                float tw = CuteUI.Width(TabNames[k], true, on) + 26;
                var tr = new Rect(tx, py, tw, 18);
                bool hover = !on && tr.Contains(Event.current.mousePosition);
                CuteUI.Box(tr, 8, on ? CuteUI.Interact : hover ? CuteUI.Paper2 : CuteUI.Paper, on ? CuteUI.Ink : CuteUI.Line, 1, on ? 1 : 0, CuteUI.InteractLo);
                var ps = CuteUI.PicSize(TabPics[k]);
                CuteUI.Icon(TabPics[k], tr.x + 7, tr.center.y - ps.y / 2f, 1, on ? CuteUI.White : CuteUI.Sub);
                CuteUI.Label(tr.x + 9 + ps.x, tr.center.y, TabNames[k], on ? CuteUI.White : CuteUI.Sub, true, on);
                if (CuteUI.Hit(tr, GUIContent.none, GUIStyle.none) && !on) { tab = k; pageScroll = 0f; Sfx.Play("open", .4f); }
                tx = tr.xMax + 5;
            }
            py += 26;

            // 글 상자
            var box = new Rect(px, py, pw, r.yMax - 16 - py);
            CuteUI.Box(box, 8, CuteUI.Paper2, CuteUI.Line, 1);
            var inner = new Rect(box.x + 12, box.y + 8, box.width - 24, box.height - 16);
            string body = bodies[tab] ?? "";
            float textH = CuteUI.WrapWords(0, 0, inner.width, body, CuteUI.Text, false, false);
            bool showSources = tab == 0 && card.sources != null && card.sources.Length > 0;
            float sourcesH = showSources ? 18 + card.sources.Length * 18 : 0;
            pageScroll = TradeLedgerUI.Scroll(inner, pageScroll, textH + sourcesH);
            GUI.BeginGroup(inner);
            float y = -pageScroll;
            CuteUI.WrapWords(0, y, inner.width, body, CuteUI.Text);
            y += textH;
            if (showSources)
            {
                y += 8;
                CuteUI.Label(0, y + 5, "참고 자료 (누르면 브라우저로 열려요)", CuteUI.Sub, true);
                y += 14;
                foreach (var source in card.sources)
                {
                    if (string.IsNullOrEmpty(source.url) || !source.url.StartsWith("https://", StringComparison.Ordinal)) continue;
                    string t = TradeLedgerUI.Fit(source.title, inner.width - 16, true, false);
                    var lr = new Rect(0, y, CuteUI.Width(t, true) + 14, 15);
                    bool hover = lr.Contains(Event.current.mousePosition);
                    CuteUI.Box(lr, 6, hover ? CuteUI.Paper : CuteUI.Paper2, CuteUI.Line, 1);
                    CuteUI.Label(lr.x + 7, lr.center.y, t, CuteUI.Interact, true);
                    if (CuteUI.Hit(lr, GUIContent.none, GUIStyle.none)) Application.OpenURL(source.url);
                    y += 18;
                }
            }
            GUI.EndGroup();
        }
    }
}
