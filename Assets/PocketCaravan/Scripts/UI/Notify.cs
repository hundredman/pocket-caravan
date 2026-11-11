using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>화면 아래쪽 가운데에 잠깐 뜨는 알림 (퀘스트 수락·완료, 마을 발전 등). 11/05 전엔 위쪽 가운데.</summary>
    public static class Notify
    {
        struct Entry { public string text, key; public float until; public bool special; }
        static readonly List<Entry> entries = new List<Entry>();

        public static void Show(string text, float seconds = 3.5f)
        {
            entries.Add(new Entry { text = text, until = Time.unscaledTime + seconds });
            if (entries.Count > 4) entries.RemoveAt(0);
        }

        /// <summary>11/11 은샘: 이벤트 퀘스트 알림 (만국 상인회 등). 금빛 종이·주황 테로 다른 알림과 구분한다</summary>
        public static void Event(string text, float seconds = 4f)
        {
            entries.Add(new Entry { text = text, until = Time.unscaledTime + seconds, special = true });
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
        public static readonly List<Rect> DrawnRects=new List<Rect>();
        public static void Draw(Rect reserved=default)
        {
            bool record=!Application.isEditor||Camera.main==null||(Camera.main.pixelWidth==Screen.width&&Camera.main.pixelHeight==Screen.height);
            if(record)DrawnRects.Clear();
            entries.RemoveAll(e => Time.unscaledTime > e.until);
            float vw = UIStyle.VirtualWidth;
            float bottom = UIStyle.VirtualHeight - 8 - HUD.BottomReserved;
            var talk = DialogueUI.ShownRect;   // 11/08 대화창이 떠 있으면 그 위로 (대화창이 알림을 덮었다)
            if (talk.HasValue) bottom = Mathf.Min(bottom, talk.Value.y - 14f);   // 왼쪽 위에 걸친 이름표까지 비킨다
            for (int i = entries.Count - 1; i >= 0; i--)   // 새 알림이 맨 아래
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
