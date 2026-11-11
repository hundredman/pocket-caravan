using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/08 화면 가장자리에 붙는 UI가 이번 프레임 차지한 자리.
    /// 왼쪽 위 상태 카드 · 오른쪽 위 단추·온라인 알약·길 장애 알약 · 왼쪽 아래 채팅 · 오른쪽 아래 고려 배우기 카드가 OnGUI마다 Mark로 알리고,
    /// 가운데에 뜨는 것(위 가운데 알약 · 아래 알림)이 FreeRow로 그 사이 빈 너비를 재서 겹치지 않게 한다.
    /// 그리는 차례가 스크립트마다 달라 한 프레임 늦게 알 수 있다 (1프레임 지난 자리까지 살아 있는 것으로 본다).
    /// </summary>
    public static class ScreenZones
    {
        struct Zone { public Rect rect; public int frame; }
        static readonly Dictionary<string, Zone> zones = new Dictionary<string, Zone>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { zones.Clear(); centers.Clear(); }

        /// <summary>이번 프레임 id가 r을 차지한다 (OnGUI마다 부른다. 안 부르면 다음 프레임 뒤에 비워진다)</summary>
        public static void Mark(string id, Rect r) => zones[id] = new Zone { rect = r, frame = Time.frameCount };

        static bool Live(Zone z) => Time.frameCount - z.frame <= 1;

        // 11/11 은샘: 화면 가운데에 뜨는 것(위 가운데 알약 · 아래 목표 줄). 양옆 벽(FreeRow)에는 넣지 않고, 떠다니는 작은 표가 피할 때만 쓴다
        static readonly Dictionary<string, Zone> centers = new Dictionary<string, Zone>();
        public static void MarkCenter(string id, Rect r) => centers[id] = new Zone { rect = r, frame = Time.frameCount };
        /// <summary>가운데 UI 하나의 자리 (이번·지난 프레임에 그렸으면)</summary>
        public static Rect? Center(string id) => centers.TryGetValue(id, out var z) && Live(z) ? z.rect : (Rect?)null;

        static Rect? Hit(Rect r)
        {
            foreach (var z in zones.Values) if (Live(z) && z.rect.Overlaps(r)) return z.rect;
            foreach (var z in centers.Values) if (Live(z) && z.rect.Overlaps(r)) return z.rect;
            foreach (var n in Notify.DrawnRects) if (n.Overlaps(r)) return n;
            return null;
        }

        /// <summary>11/11 은샘: Dodge와 같은데 옆으로 비킨다 (화면 위·아래 가장자리 표). 화면 가운데보다 오른쪽이면 오른쪽으로</summary>
        public static Rect DodgeSideways(Rect r)
        {
            float vw = UIStyle.VirtualWidth;
            for (int pass = 0; pass < 8; pass++)
            {
                var hit = Hit(r);
                if (!hit.HasValue) return r;
                var h = hit.Value;
                if (r.center.x >= h.center.x) r.x = h.xMax + 4f; else r.x = h.x - r.width - 4f;
                if (r.x < 4f || r.xMax > vw - 4f) { r.x = Mathf.Clamp(r.x, 4f, vw - r.width - 4f); return Dodge(r); }
            }
            return r;
        }

        /// <summary>
        /// 11/11 은샘: 떠다니는 작은 표(놀거리 가장자리 화살표 등)를 다른 UI와 겹치지 않게 위아래로 비킨다.
        /// 가장자리 UI · 가운데 알약·목표 줄 · 아래 알림(Notify)과 겹치면 그 위(아래쪽 절반이면) 또는 아래(위쪽 절반이면)로 옮긴다
        /// </summary>
        public static Rect Dodge(Rect r)
        {
            float vh = UIStyle.VirtualHeight;
            for (int pass = 0; pass < 8; pass++)
            {
                var hit = Hit(r);
                if (!hit.HasValue) return r;
                var h = hit.Value;
                if (r.center.y > vh / 2f) r.y = h.y - r.height - 4f; else r.y = h.yMax + 4f;
                r.y = Mathf.Clamp(r.y, 4f, vh - r.height - 4f);
            }
            return r;
        }

        /// <summary>y0~y1 줄에서 양옆 가장자리 UI를 뺀 빈 구간. 화면 가운데보다 왼쪽에 놓인 것은 왼쪽 벽, 오른쪽은 오른쪽 벽이다</summary>
        public static (float left, float right) FreeRow(float y0, float y1, float gap = 6f)
        {
            float cx = UIStyle.VirtualWidth / 2f, left = gap, right = UIStyle.VirtualWidth - gap;
            foreach (var z in zones.Values)
            {
                if (!Live(z) || z.rect.yMax <= y0 || z.rect.y >= y1) continue;
                if (z.rect.center.x < cx) left = Mathf.Max(left, z.rect.xMax + gap);
                else right = Mathf.Min(right, z.rect.x - gap);
            }
            return (left, right);
        }

        /// <summary>y0~y1 줄을 가리는 가장자리 UI 가운데 가장 위쪽 y (없으면 y1). 알림이 그 위로 올라갈 때 쓴다</summary>
        public static float TopOf(float y0, float y1)
        {
            float top = y1;
            foreach (var z in zones.Values)
                if (Live(z) && z.rect.yMax > y0 && z.rect.y < y1) top = Mathf.Min(top, z.rect.y);
            return top;
        }
    }
}
