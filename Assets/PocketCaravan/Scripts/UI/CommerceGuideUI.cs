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
        [Serializable] public class Book { public Card[] cards; }

        public Book Content { get; private set; }
        public bool IsOpen { get; private set; }
        int selected;
        Vector2 listScroll, pageScroll;

        void Awake()
        {
            var text = Resources.Load<TextAsset>("CommerceGuide/Goryeo");
            if (text != null) Content = JsonUtility.FromJson<Book>(text.text);
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
            float width = Mathf.Min(560, UIStyle.VirtualWidth - 16), height = 320;
            var r = new Rect((UIStyle.VirtualWidth - width) / 2, (UIStyle.VirtualHeight - height) / 2, width, height);
            GUI.Box(r, "", UIStyle.Box);
            GUI.Label(new Rect(r.x + 12, r.y + 10, width - 112, 20), "상업 백과 · 고려의 모티브와 여행", UIStyle.Rich);
            if (GUI.Button(new Rect(r.xMax - 92, r.y + 8, 80, 20), "닫기 Esc", UIStyle.Button)) Close();
            GUI.Label(new Rect(r.x + 12, r.y + 32, width - 24, 16), "여러 시대의 상인 문화가 만난 가상 세계입니다. 읽지 않아도 거래할 수 있어요.", UIStyle.Small);
            if (Content == null || Content.cards == null || Content.cards.Length == 0)
            {
                GUI.Label(new Rect(r.x + 12, r.y + 60, width - 24, 28), "백과를 아직 준비하지 못했습니다.", UIStyle.Wrap);
                return;
            }
            selected = Mathf.Clamp(selected, 0, Content.cards.Length - 1);
            GUILayout.BeginArea(new Rect(r.x + 12, r.y + 56, 122, height - 76));
            listScroll = GUILayout.BeginScrollView(listScroll);
            for (int i = 0; i < Content.cards.Length; i++)
                if (GUILayout.Button((selected == i ? "▸ " : "") + Content.cards[i].title, UIStyle.Button, GUILayout.MinHeight(27)))
                { selected = i; pageScroll = Vector2.zero; }
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            var card = Content.cards[selected];
            GUILayout.BeginArea(new Rect(r.x + 146, r.y + 56, width - 158, height - 76));
            pageScroll = GUILayout.BeginScrollView(pageScroll);
            GUILayout.Label(card.title, UIStyle.Rich);
            Section("역사적 모티브", card.history);
            Section("게임 속 각색", card.adaptation);
            Section("여행에 써 보기", card.travel);
            if (card.sources != null && card.sources.Length > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label("참고 자료 · 누르면 외부 브라우저로 열립니다", UIStyle.Small);
                foreach (var source in card.sources)
                    if (!string.IsNullOrEmpty(source.url) && source.url.StartsWith("https://", StringComparison.Ordinal))
                        if (GUILayout.Button(source.title, UIStyle.Button, GUILayout.MinHeight(22))) Application.OpenURL(source.url);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        static void Section(string title, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            GUILayout.Space(6);
            GUILayout.Label(title, UIStyle.Rich);
            GUILayout.Label(text, UIStyle.Wrap);
        }
    }
}
