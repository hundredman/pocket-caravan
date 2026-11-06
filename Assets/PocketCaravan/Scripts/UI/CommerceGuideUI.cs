using System;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>선택해서 읽는 상업 백과. 역사적 모티브와 게임 각색을 분리하며 경제 상태를 바꾸지 않는다.</summary>
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
        /// <summary>지금 맵의 지역 백과. 베네치아·본토 서부(venice_*) 맵이면 베네치아 백과, 그 밖은 고려 백과 (2026-10-26)</summary>
        public Book Content =>
            MapRoot.Current != null && MapRoot.Current.mapId != null && MapRoot.Current.mapId.StartsWith("venice_") && venice != null ? venice : goryeo;
        public bool IsOpen { get; private set; }
        int selected;
        Vector2 listScroll, pageScroll;
        Rect lastWindow, listViewport, pageViewport;
        GUIStyle listButton, listButtonOn;

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
            IsOpen = true;
            UIState.Blocking = true;
            Sfx.Play("open");
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            UIState.Release();
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
            string heading = Content != null && !string.IsNullOrEmpty(Content.heading) ? Content.heading : "고려의 모티브와 여행";
            float headingHeight = Mathf.Max(18, CuteUI.PaperWrap.CalcHeight(new GUIContent(heading), width - 28));
            GUI.Label(new Rect(r.x + 14, r.y + 14, width - 28, headingHeight), heading, CuteUI.PaperWrap);
            const string note = "여러 시대의 상인 문화가 만난 가상 세계입니다. 읽지 않아도 거래할 수 있어요.";
            float noteHeight = CuteUI.PaperSmall.CalcHeight(new GUIContent(note), width - 28);
            GUI.Label(new Rect(r.x + 14, r.y + 14 + headingHeight, width - 28, noteHeight), note, CuteUI.PaperSmall);
            if (GUI.Button(new Rect(r.xMax - 96, r.yMax - 28, 82, 20), "닫기 Esc", CuteUI.PaperButton)) Close();
            GUI.Label(new Rect(r.x + 14, r.yMax - 28, width - 124, 20), "목록에서 항목을 고르세요.", CuteUI.PaperSmall);
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
            float top = r.y + 14 + headingHeight + noteHeight + 6, bodyHeight = Mathf.Max(40, r.yMax - 38 - top);
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
