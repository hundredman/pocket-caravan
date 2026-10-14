using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>화면 위쪽 가운데에 잠깐 뜨는 알림 (퀘스트 수락·완료, 마을 발전 등).</summary>
    public static class Notify
    {
        struct Entry { public string text; public float until; }
        static readonly List<Entry> entries = new List<Entry>();

        public static void Show(string text, float seconds = 3.5f)
        {
            entries.Add(new Entry { text = text, until = Time.unscaledTime + seconds });
            if (entries.Count > 4) entries.RemoveAt(0);
        }

        public static void Clear() => entries.Clear();

        /// <summary>OnGUI 안에서 부른다 (UIStyle.Begin 이후).</summary>
        public static void Draw()
        {
            entries.RemoveAll(e => Time.unscaledTime > e.until);
            float y = 8;
            foreach (var e in entries)
            {
                float w = Mathf.Min(300f, UIStyle.Center.CalcSize(new GUIContent(e.text)).x + 28f);
                var r = new Rect((UIStyle.VirtualWidth - w) / 2, y, w, 20);
                GUI.Box(r, "", UIStyle.Box);
                GUI.Label(new Rect(r.x + 8, r.y + 2, r.width - 16, r.height), e.text, UIStyle.Center);
                y += 22;
            }
        }
    }
}
