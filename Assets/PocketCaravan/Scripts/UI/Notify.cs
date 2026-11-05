using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>화면 아래쪽 가운데에 잠깐 뜨는 알림 (퀘스트 수락·완료, 마을 발전 등). 11/05 전엔 위쪽 가운데.</summary>
    public static class Notify
    {
        struct Entry { public string text, key; public float until; }
        static readonly List<Entry> entries = new List<Entry>();

        public static void Show(string text, float seconds = 3.5f)
        {
            entries.Add(new Entry { text = text, until = Time.unscaledTime + seconds });
            if (entries.Count > 4) entries.RemoveAt(0);
        }

        public static void Clear() => entries.Clear();
        public static void Location(string text,float seconds)
        {
            entries.RemoveAll(e=>e.key=="location");
            entries.Add(new Entry{text=text,key="location",until=Time.unscaledTime+seconds});
            if(entries.Count>4)entries.RemoveAt(0);
        }

        /// <summary>
        /// OnGUI 안에서 부른다 (UIStyle.Begin 이후).
        /// 11/05 B안: 종이 상자로, 화면 아래 가운데에서 위로 쌓는다 (위쪽 카드·할 일 줄을 덮지 않게).
        /// 어두운 바탕용 색 태그는 종이 위에서 안 읽혀서 뺀다.
        /// </summary>
        public static void Draw()
        {
            entries.RemoveAll(e => Time.unscaledTime > e.until);
            float vw = UIStyle.VirtualWidth;
            float bottom = UIStyle.VirtualHeight - 8 - HUD.BottomReserved;
            for (int i = entries.Count - 1; i >= 0; i--)   // 새 알림이 맨 아래
            {
                string text = CuteUI.Plain(entries[i].text);
                float w = Mathf.Min(340f, vw - 40f, CuteUI.Width(text) + 28f);
                float th = CuteUI.WrapHeight(text, w - 24f);
                var r = new Rect(Mathf.Round((vw - w) / 2f), Mathf.Round(bottom - th - 12f), w, th + 12f);
                CuteUI.Box(r, 8, CuteUI.Paper, CuteUI.Ink, 1, 2);
                CuteUI.WrapCenter(r.x + 12f, r.y + 6f, w - 24f, text, CuteUI.Text);
                bottom = r.y - 5f;
            }
        }
    }
}
