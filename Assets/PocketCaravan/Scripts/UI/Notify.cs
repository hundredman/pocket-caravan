using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>화면 위쪽 가운데에 잠깐 뜨는 알림 (퀘스트 수락·완료, 마을 발전 등).</summary>
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

        static GUIStyle wrapCenter;
        static GUIStyle WrapCenter => wrapCenter ??= new GUIStyle(UIStyle.Center) { wordWrap = true, richText = true };

        /// <summary>OnGUI 안에서 부른다 (UIStyle.Begin 이후).</summary>
        public static void Draw()
        {
            if(wrapCenter!=null&&wrapCenter.font!=UIStyle.RegularFont)wrapCenter=null;
            entries.RemoveAll(e => Time.unscaledTime > e.until);
            float y = 8;
            foreach (var e in entries)
            {
                var content = new GUIContent(e.text);
                float w = Mathf.Min(320f, UIStyle.Center.CalcSize(content).x + 28f);
                float th = WrapCenter.CalcHeight(content, w - 16);
                var r = new Rect((UIStyle.VirtualWidth - w) / 2, y, w, th + 8);
                GUI.Box(r, "", UIStyle.Box);
                GUI.Label(new Rect(r.x + 8, r.y + 4, r.width - 16, th), e.text, WrapCenter);
                y += r.height + 2;
            }
        }
    }
}
