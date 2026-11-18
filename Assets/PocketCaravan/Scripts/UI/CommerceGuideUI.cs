using System;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 선택해서 읽는 상업 백과. 역사적 모티브와 게임 각색을 분리하며 경제 상태를 바꾸지 않는다.
    /// 11/18 은샘 "고려만 있고 퀄리티가 낮다": 기본 · 고려 · 베네치아 · 아라비아 · 북유럽 · 중앙 대륙 여섯 권을 위 단추로 고른다.
    /// 열면 지금 있는 지역 책부터. 글은 Resources/CommerceGuide/*.json
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

        static readonly string[] BookFiles = { "Common", "Goryeo", "Venice", "Arabia", "North", "Center" };
        static readonly string[] BookNames = { "기본", "고려", "베네치아", "아라비아", "북유럽", "중앙 대륙" };
        readonly Book[] books = new Book[BookFiles.Length];
        int bookIndex = 1;
        /// <summary>지금 고른 책</summary>
        public Book Content => books[bookIndex];

        /// <summary>지금 맵의 지역 책 번호 (중앙 대륙 길목 transit_ 맵은 중앙 대륙)</summary>
        static int HereBook()
        {
            string map = MapRoot.Current != null ? MapRoot.Current.mapId : null;
            if (!string.IsNullOrEmpty(map) && map.StartsWith("transit_")) return 5;
            switch (Reactions.RegionOfMap(map))
            {
                case Reactions.Region.Venice: return 2;
                case Reactions.Region.Arabia: return 3;
                case Reactions.Region.North: return 4;
                default: return 1;
            }
        }
        public bool IsOpen { get; private set; }
        int selected;
        Vector2 listScroll, pageScroll;
        Rect lastWindow, listViewport, pageViewport;
        GUIStyle listButton, listButtonOn;

        void Awake()
        {
            for (int i = 0; i < BookFiles.Length; i++)
            {
                var text = Resources.Load<TextAsset>("CommerceGuide/" + BookFiles[i]);
                if (text != null) books[i] = JsonUtility.FromJson<Book>(text.text);
            }
        }

        public void Open()
        {
            var gm = GameManager.I;
            if (IsOpen || UIState.Blocking || gm == null || gm.IsTravelling) return;
            if (!UIState.TryOpen(this, Close)) return;
            IsOpen = true;
            bookIndex = HereBook(); selected = 0; listScroll = pageScroll = Vector2.zero;
            Sfx.Play("open");
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            UIState.Release(this);
        }

        void Update() { if (IsOpen && InputReader.CancelPressed()) Close(); }
        void OnDisable() => Close();

        void OnGUI()
        {
            if (!IsOpen) return;
            UIStyle.Begin();
            GUI.depth = -10;
            float width = Mathf.Min(560, UIStyle.VirtualWidth - 16), height = UIStyle.WindowHeight(320) - 6;
            var r = lastWindow = new Rect(Mathf.Round((UIStyle.VirtualWidth - width) / 2), Mathf.Round((UIStyle.VirtualHeight - height) / 2 + 6), width, height);
            CuteUI.Dim(); CuteUI.Window(r); CuteUI.TitlePlate(r.center.x, r.y - 11, "상업 백과");
            if (CuteUI.CloseButton(r.xMax - 16f, r.y - 8f)) { Close(); return; }   // 11/08 오른쪽 위 X
            // 책 고르기 (11/18)
            float bx = r.x + 14, by = r.y + 14;
            for (int i = 0; i < BookNames.Length; i++)
            {
                float bw = Mathf.Round(CuteUI.Width(BookNames[i], true, true) + 16);
                var b = new Rect(bx, by, bw, 18);
                bool pressed = i == bookIndex ? CuteUI.Button(b, BookNames[i]) : CuteUI.SoftButton(b, BookNames[i]);
                if (pressed && i != bookIndex) { bookIndex = i; selected = 0; listScroll = pageScroll = Vector2.zero; Sfx.Play("move", .5f); }
                bx += bw + 4;
            }
            float head0 = r.y + 14 + 24;
            string heading = Content != null && !string.IsNullOrEmpty(Content.heading) ? Content.heading : BookNames[bookIndex];
            float headingHeight = Mathf.Max(18, CuteUI.PaperWrap.CalcHeight(new GUIContent(heading), width - 28));
            GUI.Label(new Rect(r.x + 14, head0, width - 28, headingHeight), heading, CuteUI.PaperWrap);
            const string note = "여러 시대의 상인 문화가 만난 가상 세계입니다. 읽지 않아도 거래할 수 있어요.";
            float noteHeight = CuteUI.PaperSmall.CalcHeight(new GUIContent(note), width - 28);
            GUI.Label(new Rect(r.x + 14, head0 + headingHeight, width - 28, noteHeight), note, CuteUI.PaperSmall);
            if (GUI.Button(new Rect(r.xMax - 96, r.yMax - 31, 82, 20), "닫기 Esc", CuteUI.PaperButton)) Close();
            GUI.Label(new Rect(r.x + 14, r.yMax - 31, width - 124, 20), "위에서 지역을, 왼쪽에서 항목을 고르세요.", CuteUI.PaperSmall);
            if (Content == null || Content.cards == null || Content.cards.Length == 0)
            {
                GUI.Label(new Rect(r.x + 14, r.y + 64, width - 28, 28), "백과를 아직 준비하지 못했습니다.", CuteUI.PaperWrap); return;
            }
            selected = Mathf.Clamp(selected, 0, Content.cards.Length - 1);
            if (listButton == null || listButton.font != UIStyle.RegularFont)
            {
                listButton = new GUIStyle(CuteUI.PaperButton) { wordWrap = true };
                listButtonOn = new GUIStyle(CuteUI.PaperButtonOn) { wordWrap = true };
            }
            float top = head0 + headingHeight + noteHeight + 6, bodyHeight = Mathf.Max(40, r.yMax - 41 - top);
            float listWidth = Mathf.Round(Mathf.Clamp(width * .28f, 96, 132));
            listViewport = new Rect(r.x + 14, top, listWidth, bodyHeight);
            pageViewport = new Rect(listViewport.xMax + 12, top, width - 28 - listWidth - 12, bodyHeight);
            GUILayout.BeginArea(listViewport); listScroll = GUILayout.BeginScrollView(listScroll);
            float buttonWidth = listWidth - 24;
            for (int i = 0; i < Content.cards.Length; i++)
            {
                string title = Content.cards[i].title;
                float rowHeight = Mathf.Max(27, listButton.CalcHeight(new GUIContent(title), buttonWidth));
                if (GUILayout.Button(title, selected == i ? listButtonOn : listButton, GUILayout.Width(buttonWidth), GUILayout.Height(rowHeight)))
                { selected = i; pageScroll = Vector2.zero; }
            }
            GUILayout.EndScrollView(); GUILayout.EndArea();

            var card = Content.cards[selected];
            GUILayout.BeginArea(pageViewport); pageScroll = GUILayout.BeginScrollView(pageScroll);
            GUILayout.Label(card.title, CuteUI.PaperRich);
            Section("역사적 모티브", card.history); Section("게임 속 각색", card.adaptation); Section("여행에 써 보기", card.travel);
            if (card.sources != null && card.sources.Length > 0)
            {
                GUILayout.Space(5); GUILayout.Label("참고 자료 · 누르면 외부 브라우저로 열립니다", CuteUI.PaperSmall);
                foreach (var source in card.sources)
                    if (!string.IsNullOrEmpty(source.url) && source.url.StartsWith("https://", StringComparison.Ordinal))
                    {
                        float sourceWidth = pageViewport.width - 24;
                        if (GUILayout.Button(source.title, listButton, GUILayout.Width(sourceWidth), GUILayout.MinHeight(22))) Application.OpenURL(source.url);
                    }
            }
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }

        static void Section(string title, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            GUILayout.Space(6);
            GUILayout.Label(title, CuteUI.PaperRich);
            GUILayout.Label(text, CuteUI.PaperWrap);
        }
    }
}
