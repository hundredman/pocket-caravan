using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>화면 아래쪽 가운데에 잠깐 뜨는 알림 (퀘스트 수락·완료, 마을 발전 등). 11/05 전엔 위쪽 가운데.</summary>
    public static class Notify
    {
        struct Entry { public string text, key; public float until, seconds; public bool special; }
        static readonly List<Entry> entries = new List<Entry>();
        /// <summary>11/11 은샘: 한 번에 보이는 알림 수. 나머지는 앞 알림이 사라지면 이어서 뜬다 (화면이 알림으로 가득 차지 않게)</summary>
        const int MaxVisible = 2, MaxQueued = 6;

        /// <summary>
        /// 11/11 은샘: 알림에서 띄어 쓴 괄호 안 덧붙임은 뺀다 ("(★ 표시를 찾아가 보자)", "(팔면 20% 더)" 등). 짧게 한 줄로.
        /// 낱말에 붙은 괄호("을(를)")는 그대로 둔다. 빈 줄이 되면 줄째 뺀다
        /// </summary>
        public static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string t = System.Text.RegularExpressions.Regex.Replace(text, @"(?m)(^|[ \t])\([^()\n]*\)", "$1");
            var lines = new List<string>();
            foreach (var line in t.Split('\n'))
            {
                string l = System.Text.RegularExpressions.Regex.Replace(line, @"[ \t]{2,}", " ").Trim();
                if (l.Length > 0) lines.Add(l);
            }
            return lines.Count > 0 ? string.Join("\n", lines) : text;
        }

        static void Add(Entry e)
        {
            e.text = Clean(e.text);
            foreach (var x in entries) if (x.text == e.text) return;   // 같은 알림이 떠 있거나 기다리면 또 넣지 않는다
            e.until = -1f;   // 자리가 나면 그때부터 시간이 간다
            entries.Add(e);
            while (entries.Count > MaxQueued) entries.RemoveAt(0);
        }

        public static void Show(string text, float seconds = 3.5f) => Add(new Entry { text = text, seconds = seconds });

        /// <summary>11/11 은샘: 이벤트 퀘스트 알림 (만국 상인회 등). 금빛 종이·주황 테로 다른 알림과 구분한다</summary>
        public static void Event(string text, float seconds = 4f) => Add(new Entry { text = text, seconds = seconds, special = true });

        public static void Clear() => entries.Clear();
        public static void Location(string text,float seconds)
        {
            entries.RemoveAll(e=>e.key=="location");
            Add(new Entry{text=text,key="location",seconds=seconds});
            // 11/11 마을 이름은 도착하자마자 보여야 하니 맨 앞으로 (기다리는 알림 뒤로 밀리지 않게)
            int i=entries.FindIndex(e=>e.key=="location");
            if(i>0){var e=entries[i];entries.RemoveAt(i);entries.Insert(0,e);}
        }

        /// <summary>
        /// OnGUI 안에서 부른다 (UIStyle.Begin 이후).
        /// 11/05 B안: 종이 상자로, 화면 아래 가운데에서 위로 쌓는다 (위쪽 카드·할 일 줄을 덮지 않게).
        /// 어두운 바탕용 색 태그는 종이 위에서 안 읽혀서 뺀다.
        /// </summary>
        public static readonly List<Rect> DrawnRects=new List<Rect>();
        public static void Draw(Rect reserved=default)
        {
            bool record=!Application.isEditor||Camera.main==null||(Camera.main.pixelWidth==Screen.width&&Camera.main.pixelHeight==Screen.height);
            if(record)DrawnRects.Clear();
            entries.RemoveAll(e => e.until > 0f && Time.unscaledTime > e.until);
            // 11/11 앞에서부터 MaxVisible개만 보인다. 기다리던 알림은 보이기 시작할 때부터 시간을 잰다
            int shown = 0;
            for (int i = 0; i < entries.Count && shown < MaxVisible; i++)
            {
                var e = entries[i];
                if (e.until < 0f) { e.until = Time.unscaledTime + e.seconds; entries[i] = e; }
                shown++;
            }
            float vw = UIStyle.VirtualWidth;
            float bottom = UIStyle.VirtualHeight - 8 - HUD.BottomReserved;
            var talk = DialogueUI.ShownRect;   // 11/08 대화창이 떠 있으면 그 위로 (대화창이 알림을 덮었다)
            if (talk.HasValue) bottom = Mathf.Min(bottom, talk.Value.y - 14f);   // 왼쪽 위에 걸친 이름표까지 비킨다
            for (int i = Mathf.Min(entries.Count, MaxVisible) - 1; i >= 0; i--)   // 새 알림이 맨 아래 (보이는 것만)
            {
                string text = CuteUI.Plain(entries[i].text);
                float want = Mathf.Min(340f, vw - 40f, CuteUI.Width(text) + 28f);
                // 11/08 아래 양옆 카드(왼쪽 채팅 · 오른쪽 고려 배우기 카드) 사이 가운데로. 너무 좁으면 그 카드들 위로 올린다
                var (left, right) = ScreenZones.FreeRow(bottom - CuteUI.WrapHeight(text, want - 24f) - 12f, bottom);
                if (right - left < Mathf.Min(want, 180f))
                {
                    bottom = ScreenZones.TopOf(bottom - CuteUI.WrapHeight(text, want - 24f) - 12f, bottom) - 5f;
                    (left, right) = ScreenZones.FreeRow(bottom - CuteUI.WrapHeight(text, want - 24f) - 12f, bottom);
                }
                float w = Mathf.Max(80f, Mathf.Min(want, right - left));
                float th = CuteUI.WrapHeight(text, w - 24f);
                var r = new Rect(Mathf.Round(left + (right - left - w) / 2f), Mathf.Round(bottom - th - 12f), w, th + 12f);
                if(reserved.width>0&&r.Overlaps(reserved))   // 고려 배우기 카드 (ScreenZones로도 비키지만 처음 한 프레임을 위해 그대로 둔다)
                {
                    if(reserved.xMin-w-8>=8)r.x=Mathf.Floor(reserved.xMin-w-8);
                    else r.y=Mathf.Min(r.y,reserved.yMin-r.height-6);
                }
                if(record)DrawnRects.Add(r);
                if (entries[i].special) CuteUI.Box(r, 8, CuteUI.EventPaper, CuteUI.EventLine, 2, 2);
                else CuteUI.Box(r, 8, CuteUI.Paper, CuteUI.Ink, 1, 2);
                CuteUI.WrapCenter(r.x + 12f, r.y + 6f, w - 24f, text, CuteUI.Text);
                bottom = r.y - 5f;
            }
        }
    }
}
