using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 2026-11-05 지역 지도(M) 공용 그리기: UI B안 창(CuteUI) + 지형 그림 + 마을 이름표 + 내 위치 + 범례.
    /// 고려(WorldMapUI)·베네치아·북유럽·아라비아 지도가 같은 모양으로 보이게, 각 지도는 자리만 계산해서 Draw에 넘긴다.
    /// 지형 그림은 그대로 그린다 (예전처럼 UIStyle.Lin 사본을 만들면 두 번 어두워진다).
    /// </summary>
    public static class CuteMap
    {
        /// <summary>지도 위 한 곳. at = 지도 배치 좌표 (지형 그림 칸 단위)</summary>
        public struct Place
        {
            public string name;
            public Vector2 at;
            public bool destination;   // 맡은 배달의 목적지
            public bool minor;         // 마을이 아닌 이름 (고려 정착지 등): 점 없이 작게
        }

        public struct Result { public bool close, toggle; }

        static readonly Color32 NoteFill = new Color32(0xFF, 0xFA, 0xF2, 200), LabelFill = new Color32(0xFF, 0xFA, 0xF2, 240);
        static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        static readonly Color32 DestFill = new Color32(0xFF, 0xF0, 0xBE, 245);
        static readonly (string name, Color32 color)[] Swatches =
        {
            ("건물", new Color32(232, 160, 126, 255)), ("숲", new Color32(147, 194, 122, 255)),
            ("물", new Color32(166, 212, 230, 255)), ("길", new Color32(215, 172, 116, 255)),
        };

        /// <summary>
        /// 지도 창을 그린다. 모든 지역이 같은 모양: 이름표는 점 바로 옆 (선 없음, 지도 옆 빈 곳까지 써서 겹치지 않게).
        /// toggleLabel이 있으면 왼쪽 위에 보조 단추 (고려 전체/상세).
        /// </summary>
        public static Result Draw(string title, Texture2D terrain, float atlasW, float atlasH, List<Place> places, Vector2? me,
            string current, string legend, List<(string text, Vector2 at)> notes = null, string toggleLabel = null)
        {
            var result = new Result();
            CuteUI.Dim();
            float vw = UIStyle.VirtualWidth, vh = UIStyle.VirtualHeight;
            float w = Mathf.Min(620, vw - 16), h = Mathf.Min(344, vh - 26);
            var r = new Rect(Mathf.Round((vw - w) / 2), Mathf.Round((vh - h) / 2 + 7), w, h);
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11, title);
            CuteUI.Keycap(r.xMax - 32 - CuteUI.KeycapWidth("M") - 3, r.y - 3, "M");
            if (CuteUI.CloseButton(r.xMax - 32, r.y - 9)) result.close = true;
            if (!string.IsNullOrEmpty(toggleLabel) && CuteUI.SoftButton(new Rect(r.x + 14, r.y + 8, CuteUI.Width(toggleLabel) + 20, 18), toggleLabel)) result.toggle = true;

            var area = new Rect(r.x + 14, r.y + 30, r.width - 28, r.height - 74);
            CuteUI.Box(area, 6, CuteUI.Paper2, CuteUI.Line);
            if (terrain == null || atlasW <= 0 || atlasH <= 0)
            {
                CuteUI.LabelCenter(area.center.x, area.center.y, "지도 그림이 아직 없어요", CuteUI.Sub, true);
                Footer(r, area, current, legend);
                return result;
            }
            float scale = Mathf.Min((area.width - 10) / atlasW, (area.height - 10) / atlasH);
            var canvas = new Rect(Mathf.Round(area.center.x - atlasW * scale / 2), Mathf.Round(area.center.y - atlasH * scale / 2),
                Mathf.Round(atlasW * scale), Mathf.Round(atlasH * scale));
            if (Event.current.type == EventType.Repaint) GUI.DrawTexture(canvas, terrain, ScaleMode.StretchToFill);
            CuteUI.Box(new Rect(canvas.x - 1, canvas.y - 1, canvas.width + 2, canvas.height + 2), 2, Clear, CuteUI.Ink);
            Vector2 Project(Vector2 a) => new Vector2(canvas.x + a.x * scale, canvas.y + a.y * scale);

            if (notes != null)
                foreach (var (text, at) in notes)
                {
                    var p = Project(at);
                    float nw = CuteUI.Width(text, true) + 8;
                    var nr = new Rect(Mathf.Clamp(p.x, canvas.x + 2, canvas.xMax - nw - 2), Mathf.Clamp(p.y, canvas.y + 2, canvas.yMax - 14), nw, 12);
                    CuteUI.Box(nr, 4, NoteFill, NoteFill, 0);
                    CuteUI.Label(nr.x + 4, nr.center.y, text, CuteUI.Sub, true);
                }

            NearLabels(places, Project, canvas, area);

            if (me.HasValue) Pin(Project(me.Value));
            Footer(r, area, current, legend);
            return result;
        }

        /// <summary>점 옆 이름표 (위쪽 이름부터 자리를 잡고, 겹치지 않게 위·아래·옆을 고른다. 지도 옆 빈 곳까지 쓴다)</summary>
        static void NearLabels(List<Place> places, System.Func<Vector2, Vector2> project, Rect canvas, Rect area)
        {
            var items = new List<(Place pl, Vector2 p, string label, float w)>();
            foreach (var pl in places)
            {
                string label = pl.destination ? pl.name + " · 의뢰" : pl.name;
                items.Add((pl, project(pl.at), label, Mathf.Min(130, CuteUI.Width(label, true) + 10)));
            }
            items.Sort((a, b) => a.p.y.CompareTo(b.p.y));
            var markers = new List<Rect>();
            foreach (var it in items) markers.Add(new Rect(it.p.x - 4, it.p.y - 4, 8, 8));
            var placed = new List<Rect>();
            var rects = new List<Rect>();
            foreach (var it in items)
            {
                var lr = PlaceLabel(it.p, it.w, canvas, area, placed, markers);
                placed.Add(lr);
                rects.Add(lr);
            }
            foreach (var it in items) Dot(it.p, it.pl);
            for (int i = 0; i < items.Count; i++) Tag(rects[i], items[i].label, items[i].pl);
        }

        static void Dot(Vector2 p, Place pl) => CuteUI.Circle(p.x - 3, p.y - 3, 7, pl.destination ? CuteUI.Deco : CuteUI.Paper, CuteUI.Ink);

        static void Tag(Rect lr, string label, Place pl)
        {
            CuteUI.Box(lr, 5, pl.destination ? DestFill : LabelFill, CuteUI.Ink);
            CuteUI.LabelCenter(lr.center.x, lr.center.y, label, CuteUI.Text, true);
        }

        /// <summary>내 위치: 주황 핀 (흰 가운데 + 아래 뾰족)</summary>
        static void Pin(Vector2 p)
        {
            float x = Mathf.Round(p.x), y = Mathf.Round(p.y);
            CuteUI.Circle(x - 2, y - 2, 5, CuteUI.Ink, CuteUI.Ink);
            for (int i = 0; i < 4; i++) CuteUI.Fill(new Rect(x - 3 + i, y - 6 + i, 7 - 2 * i, 1), CuteUI.Ink);
            for (int i = 0; i < 3; i++) CuteUI.Fill(new Rect(x - 2 + i, y - 6 + i, 5 - 2 * i, 1), CuteUI.Frame);
            CuteUI.Circle(x - 7, y - 19, 15, CuteUI.Frame, CuteUI.Ink, 1, 1);
            CuteUI.Circle(x - 2, y - 14, 5, CuteUI.White, CuteUI.White, 0);
        }

        static void Footer(Rect r, Rect area, string current, string legend)
        {
            float y = area.yMax + 12;
            CuteUI.Icon(CuteUI.Pic.Pin, r.x + 16, y - 3, 1, CuteUI.Frame);
            CuteUI.Label(r.x + 25, y, "지금 " + current, CuteUI.Text, true, true);
            // 범례: 내 위치 · 마을 · 의뢰 목적지
            float x = r.xMax - 16;
            x = CuteUI.LabelRight(x, y, "의뢰 목적지", CuteUI.Sub, true) - CuteUI.Width("의뢰 목적지", true) - 11;
            CuteUI.Circle(x, y - 3, 7, CuteUI.Deco, CuteUI.Ink);
            x -= 12;
            x = CuteUI.LabelRight(x, y, "마을", CuteUI.Sub, true) - CuteUI.Width("마을", true) - 11;
            CuteUI.Circle(x, y - 3, 7, CuteUI.Paper, CuteUI.Ink);
            x -= 12;
            x = CuteUI.LabelRight(x, y, "내 위치", CuteUI.Sub, true) - CuteUI.Width("내 위치", true) - 12;
            CuteUI.Circle(x, y - 4, 9, CuteUI.Frame, CuteUI.Ink);
            if (!string.IsNullOrEmpty(legend)) CuteUI.Label(r.x + 16, y + 15, legend, CuteUI.Sub, true);
            // 모든 지역 같은 그림체라 색 뜻도 하나 (Tools/MapGen/unified_map.py 팔레트)
            float lx = r.xMax - 16;
            foreach (var (name, col) in Swatches)
            {
                lx = CuteUI.LabelRight(lx, y + 15, name, CuteUI.Sub, true) - CuteUI.Width(name, true) - 10;
                CuteUI.Box(new Rect(lx, y + 12, 7, 7), 2, col, CuteUI.Ink);
                lx -= 10;
            }
        }

        /// <summary>
        /// 이름표 자리 고르기: 점 위·아래·오른쪽·왼쪽·대각선을 가까운 것부터 해 보고,
        /// 다른 이름표와 겹치면 크게, 다른 점을 가리면 조금, 지도 밖(옆 빈 곳)이면 아주 조금 벌점. 가장 벌점이 적은 곳
        /// </summary>
        static Rect PlaceLabel(Vector2 p, float w, Rect canvas, Rect area, List<Rect> placed, List<Rect> markers)
        {
            float hw = w / 2f;
            var offsets = new[]
            {
                new Vector2(0, -17), new Vector2(0, 5), new Vector2(hw + 6, -6), new Vector2(-hw - 6, -6),
                new Vector2(hw + 2, -16), new Vector2(-hw - 2, -16), new Vector2(hw + 2, 4), new Vector2(-hw - 2, 4),
                new Vector2(0, -31), new Vector2(0, 19), new Vector2(hw + 6, -21), new Vector2(-hw - 6, -21),
                new Vector2(hw + 6, 9), new Vector2(-hw - 6, 9), new Vector2(0, -45), new Vector2(0, 33),
            };
            Rect best = default; float bestPenalty = float.MaxValue;
            foreach (var o in offsets)
            {
                var lr = new Rect(Mathf.Round(Mathf.Clamp(p.x + o.x - hw, area.x + 3, area.xMax - w - 3)),
                    Mathf.Round(Mathf.Clamp(p.y + o.y, area.y + 3, area.yMax - 16)), w, 13);
                var padded = new Rect(lr.x - 2, lr.y - 1, lr.width + 4, lr.height + 2);
                float penalty = Vector2.Distance(lr.center, p) * .2f;
                foreach (var other in placed) if (padded.Overlaps(other)) penalty += 1000;
                foreach (var other in markers) if (padded.Overlaps(other)) penalty += 60;
                if (!canvas.Overlaps(lr) || lr.x < canvas.x || lr.xMax > canvas.xMax) penalty += 3;
                if (penalty < bestPenalty) { best = lr; bestPenalty = penalty; }
            }
            return best;
        }
    }
}
