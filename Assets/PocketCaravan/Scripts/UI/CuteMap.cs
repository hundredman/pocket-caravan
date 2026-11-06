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
        /// 지도 창을 그린다. 모든 지역이 같은 모양: 이름표는 점 가까이 두고, 밀집한 곳은 빈 자리로 옮겨 연결선으로 위치를 알린다.
        /// toggleLabel이 있으면 왼쪽 위에 보조 단추 (고려 전체/상세).
        /// </summary>
        public static Result Draw(string title, Texture2D terrain, float atlasW, float atlasH, List<Place> places, Vector2? me,
            string current, string legend, List<(string text, Vector2 at)> notes = null, string toggleLabel = null, bool sideLabels = false)
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

            bool stackKeys = CuteUI.Width("지금 " + current, true, true) + KeyLegendWidth() + 30 > r.width - 32;
            bool stackSwatches = CuteUI.Width(legend ?? "", true) + SwatchWidth() + 16 > r.width - 32;
            var area = new Rect(r.x + 14, r.y + 30, r.width - 28, r.height - 74 - (stackKeys ? 15 : 0) - (stackSwatches ? 15 : 0));
            CuteUI.Box(area, 6, CuteUI.Paper2, CuteUI.Line);
            if (terrain == null || atlasW <= 0 || atlasH <= 0)
            {
                CuteUI.LabelCenter(area.center.x, area.center.y, "지도 그림이 아직 없어요", CuteUI.Sub, true);
                Footer(r, area, current, legend, stackKeys, stackSwatches);
                return result;
            }
            float sideSpace = sideLabels ? Mathf.Clamp(area.width * .22f, 60, 105) : 0;
            float scale = Mathf.Min((area.width - 10 - sideSpace * 2) / atlasW, (area.height - 10) / atlasH);
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

            NearLabels(places, Project, canvas, area, me.HasValue ? (Vector2?)Project(me.Value) : null, sideLabels);

            if (me.HasValue) Pin(Project(me.Value));
            Footer(r, area, current, legend, stackKeys, stackSwatches);
            return result;
        }

        sealed class LabelLayout
        {
            public Rect canvas, area, pin;
            public Font font;
            public int size;
            public bool sideLabels;
            public Place[] places;
            public Vector2[] points;
            public string[] labels;
            public Rect[] rects;
        }
        static LabelLayout lastLabels;
        static GUIStyle tagStyle;
        static GUIStyle TagStyle
        {
            get
            {
                if (tagStyle == null || tagStyle.font != UIStyle.SmallFont || tagStyle.fontSize != UIStyle.SmallSize)
                    tagStyle = new GUIStyle(CuteUI.PaperSmall) { font = UIStyle.SmallFont, fontSize = UIStyle.SmallSize,
                        alignment = TextAnchor.MiddleCenter, wordWrap = true, richText = false, clipping = TextClipping.Clip,
                        padding = new RectOffset(5,5,0,0), margin = new RectOffset(0,0,0,0) };
                tagStyle.normal.textColor = UIStyle.Lin(CuteUI.Text);
                return tagStyle;
            }
        }

        static bool SamePlaces(LabelLayout layout, List<Place> places)
        {
            if (layout.places.Length != places.Count) return false;
            for (int i = 0; i < places.Count; i++)
            {
                var a = layout.places[i]; var b = places[i];
                if (a.name != b.name || a.at != b.at || a.destination != b.destination || a.minor != b.minor) return false;
            }
            return true;
        }

        /// <summary>밀집한 곳만 빈 공간을 탐색한다. 결과를 캐시해 매 OnGUI마다 후보를 다시 찾지 않는다.</summary>
        static void NearLabels(List<Place> places, System.Func<Vector2, Vector2> project, Rect canvas, Rect area, Vector2? me, bool sideLabels)
        {
            var pin = me.HasValue ? new Rect(Mathf.Round(me.Value.x)-8, Mathf.Round(me.Value.y)-20, 17, 24) : default;
            var layout = lastLabels;
            if (layout == null || layout.canvas != canvas || layout.area != area || layout.pin != pin
                || layout.font != UIStyle.SmallFont || layout.size != UIStyle.SmallSize || layout.sideLabels != sideLabels || !SamePlaces(layout, places))
            {
                layout = new LabelLayout { canvas=canvas, area=area, pin=pin, font=UIStyle.SmallFont, size=UIStyle.SmallSize, sideLabels=sideLabels,
                    places=places.ToArray(), points=new Vector2[places.Count], labels=new string[places.Count], rects=new Rect[places.Count] };
                var order = new List<int>(); var markers = new List<Rect>();
                for (int i=0;i<places.Count;i++)
                {
                    layout.points[i]=project(places[i].at);
                    layout.labels[i]=(places[i].name ?? "")+(places[i].destination && !sideLabels ? " · 의뢰" : "");
                    markers.Add(new Rect(layout.points[i].x-4,layout.points[i].y-4,8,8)); order.Add(i);
                }
                if (me.HasValue) markers.Add(pin);
                order.Sort((a,b)=> {int cmp=layout.points[a].y.CompareTo(layout.points[b].y); return cmp!=0?cmp:a.CompareTo(b);});
                if (!sideLabels || !SideLabels(layout,order))
                {
                var placed = new List<Rect>();
                foreach (int i in order)
                {
                    float maxWidth = sideLabels ? Mathf.Max(20, Mathf.Min(canvas.x-area.x,area.xMax-canvas.xMax)-8) : 160;
                    float width=Mathf.Min(Mathf.Max(20,area.width-8), Mathf.Min(maxWidth,CuteUI.Width(layout.labels[i],true)+10));
                    float height=Mathf.Max(13,Mathf.Ceil(TagStyle.CalcHeight(new GUIContent(layout.labels[i]),width)));
                    layout.rects[i]=PlaceLabel(layout.points[i],width,height,canvas,area,placed,markers,sideLabels);
                    placed.Add(layout.rects[i]);
                }
                }
                lastLabels=layout;
            }
            for(int i=0;i<places.Count;i++) Leader(layout.points[i],layout.rects[i]);
            for(int i=0;i<places.Count;i++) Dot(layout.points[i],places[i]);
            for(int i=0;i<places.Count;i++) Tag(layout.rects[i],layout.labels[i],places[i]);
        }

        // 두 열의 총 높이를 제한하면서 각 점에 가까운 쪽을 고른다.
        // 위에서부터 차례로 놓은 뒤 아래쪽 경계를 넘는 행만 위로 당긴다.
        static bool SideLabels(LabelLayout layout,List<int> order)
        {
            int n=order.Count, capacity=Mathf.FloorToInt(layout.area.height-4);
            var widths=new float[n];var heights=new float[n];var weights=new int[n];
            float maxWidth=Mathf.Min(layout.canvas.x-layout.area.x,layout.area.xMax-layout.canvas.xMax)-8;
            var cost=new float[n+1,capacity+1];var previous=new int[n+1,capacity+1];var choseLeft=new bool[n+1,capacity+1];
            for(int i=0;i<=n;i++)for(int h=0;h<=capacity;h++)cost[i,h]=float.PositiveInfinity;
            cost[0,0]=0;int used=0;
            for(int row=0;row<n;row++)
            {
                int i=order[row];widths[i]=Mathf.Min(maxWidth,CuteUI.Width(layout.labels[i],true)+10);
                heights[i]=Mathf.Max(13,Mathf.Ceil(TagStyle.CalcHeight(new GUIContent(layout.labels[i]),widths[i])));
                int weight=weights[i]=Mathf.CeilToInt(heights[i])+2;
                float leftDistance=Mathf.Abs(layout.points[i].x-layout.canvas.x),rightDistance=Mathf.Abs(layout.canvas.xMax-layout.points[i].x);
                for(int h=0;h<=capacity;h++)
                {
                    if(float.IsPositiveInfinity(cost[row,h]))continue;
                    if(h+weight<=capacity && cost[row,h]+leftDistance<cost[row+1,h+weight])
                    {cost[row+1,h+weight]=cost[row,h]+leftDistance;previous[row+1,h+weight]=h;choseLeft[row+1,h+weight]=true;}
                    if(used-h+weight<=capacity && cost[row,h]+rightDistance<cost[row+1,h])
                    {cost[row+1,h]=cost[row,h]+rightDistance;previous[row+1,h]=h;choseLeft[row+1,h]=false;}
                }
                used+=weight;
            }
            int best=-1;float bestCost=float.PositiveInfinity;
            for(int h=0;h<=capacity;h++)if(cost[n,h]<bestCost){best=h;bestCost=cost[n,h];}
            if(best<0)return false;
            var sides=new bool[n];
            for(int row=n;row>0;row--){sides[order[row-1]]=choseLeft[row,best];best=previous[row,best];}
            foreach(bool left in new[]{true,false})
            {
                var column=new List<int>();float next=layout.area.y+3;
                foreach(int i in order)if(sides[i]==left)
                {
                    float top=Mathf.Max(next,layout.points[i].y-heights[i]/2);
                    float x=left?layout.canvas.x-5-widths[i]:layout.canvas.xMax+5;
                    layout.rects[i]=new Rect(Mathf.Round(x),Mathf.Round(top),widths[i],heights[i]);
                    next=layout.rects[i].yMax+2;column.Add(i);
                }
                float bottom=layout.area.yMax-3;
                for(int row=column.Count-1;row>=0;row--)
                {
                    int i=column[row];var r=layout.rects[i];r.y=Mathf.Min(r.y,bottom-r.height);layout.rects[i]=r;bottom=r.y-2;
                }
            }
            return true;
        }

        static void Leader(Vector2 p, Rect label)
        {
            var end = new Vector2(Mathf.Clamp(p.x,label.x,label.xMax),Mathf.Clamp(p.y,label.y,label.yMax));
            if (Vector2.Distance(p,end)<7) return;
            var col = new Color32(CuteUI.Ink.r,CuteUI.Ink.g,CuteUI.Ink.b,130);
            float x=Mathf.Round(p.x), y=Mathf.Round(p.y), ex=Mathf.Round(end.x), ey=Mathf.Round(end.y);
            CuteUI.Fill(new Rect(Mathf.Min(x,ex),y,Mathf.Abs(ex-x)+1,1),col);
            CuteUI.Fill(new Rect(ex,Mathf.Min(y,ey),1,Mathf.Abs(ey-y)+1),col);
        }

        static void Dot(Vector2 p, Place pl) => CuteUI.Circle(p.x - 3, p.y - 3, 7, pl.destination ? CuteUI.Deco : CuteUI.Paper, CuteUI.Ink);

        static void Tag(Rect lr, string label, Place pl)
        {
            CuteUI.Box(lr, 5, pl.destination ? DestFill : LabelFill, CuteUI.Ink);
            GUI.Label(lr, label, TagStyle);
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

        static float KeyLegendWidth() => CuteUI.Width("의뢰 목적지",true)+CuteUI.Width("마을",true)+CuteUI.Width("내 위치",true)+70;
        static float SwatchWidth()
        {
            float width=0; foreach(var (name,col) in Swatches) width+=CuteUI.Width(name,true)+20; return width;
        }
        static readonly List<Rect> lastFooterRects = new List<Rect>();
        static void Footer(Rect r, Rect area, string current, string legend, bool stackKeys, bool stackSwatches)
        {
            lastFooterRects.Clear();
            float y = area.yMax + 12;
            CuteUI.Icon(CuteUI.Pic.Pin, r.x + 16, y - 3, 1, CuteUI.Frame);
            CuteUI.Label(r.x + 25, y, "지금 " + current, CuteUI.Text, true, true);
            lastFooterRects.Add(new Rect(r.x+16,y-6,CuteUI.Width("지금 "+current,true,true)+9,12));
            float keyY=y+(stackKeys?15:0);
            float x = r.xMax - 16;
            lastFooterRects.Add(new Rect(x-KeyLegendWidth(),keyY-6,KeyLegendWidth(),12));
            x = CuteUI.LabelRight(x, keyY, "의뢰 목적지", CuteUI.Sub, true) - CuteUI.Width("의뢰 목적지", true) - 11;
            CuteUI.Circle(x, keyY - 3, 7, CuteUI.Deco, CuteUI.Ink); x -= 12;
            x = CuteUI.LabelRight(x, keyY, "마을", CuteUI.Sub, true) - CuteUI.Width("마을", true) - 11;
            CuteUI.Circle(x, keyY - 3, 7, CuteUI.Paper, CuteUI.Ink); x -= 12;
            x = CuteUI.LabelRight(x, keyY, "내 위치", CuteUI.Sub, true) - CuteUI.Width("내 위치", true) - 12;
            CuteUI.Circle(x, keyY - 4, 9, CuteUI.Frame, CuteUI.Ink);
            float noteY=keyY+15, swatchY=noteY+(stackSwatches?15:0);
            if (!string.IsNullOrEmpty(legend))
            {
                CuteUI.Label(r.x + 16,noteY,legend,CuteUI.Sub,true);
                lastFooterRects.Add(new Rect(r.x+16,noteY-6,CuteUI.Width(legend,true),12));
            }
            float lx=r.xMax-16;
            lastFooterRects.Add(new Rect(lx-SwatchWidth(),swatchY-6,SwatchWidth(),12));
            foreach(var (name,col) in Swatches)
            {
                lx=CuteUI.LabelRight(lx,swatchY,name,CuteUI.Sub,true)-CuteUI.Width(name,true)-10;
                CuteUI.Box(new Rect(lx,swatchY-3,7,7),2,col,CuteUI.Ink); lx-=10;
            }
        }

        /// <summary>가까운 후보가 모두 막힌 경우에만 지도 틀 안 빈 곳도 탐색한다.</summary>
        static Rect PlaceLabel(Vector2 p, float w, float h, Rect canvas, Rect area, List<Rect> placed, List<Rect> markers, bool sideLabels)
        {
            float hw=w/2;
            var offsets=new[]{new Vector2(0,-h-4),new Vector2(0,5),new Vector2(hw+6,-h/2),new Vector2(-hw-6,-h/2),
                new Vector2(hw+2,-h-3),new Vector2(-hw-2,-h-3),new Vector2(hw+2,4),new Vector2(-hw-2,4),
                new Vector2(0,-h-18),new Vector2(0,19),new Vector2(hw+6,-h-8),new Vector2(-hw-6,-h-8),
                new Vector2(hw+6,9),new Vector2(-hw-6,9),new Vector2(0,-h-32),new Vector2(0,33)};
            Rect best=default; float bestPenalty=float.MaxValue;
            void Consider(float x,float y)
            {
                var lr=new Rect(Mathf.Round(Mathf.Clamp(x,area.x+3,area.xMax-w-3)),Mathf.Round(Mathf.Clamp(y,area.y+3,area.yMax-h-3)),w,h);
                var padded=new Rect(lr.x-2,lr.y-1,lr.width+4,lr.height+2);
                float penalty=Vector2.Distance(lr.center,p)*.2f;
                if(sideLabels && padded.Overlaps(canvas)) penalty+=100000;
                foreach(var other in placed) if(padded.Overlaps(other)) penalty+=10000;
                foreach(var other in markers) if(padded.Overlaps(other)) penalty+=1000;
                if(lr.x<canvas.x||lr.xMax>canvas.xMax) penalty+=3;
                if(penalty<bestPenalty){best=lr;bestPenalty=penalty;}
            }
            foreach(var o in offsets)Consider(p.x+o.x-hw,p.y+o.y);
            if(bestPenalty>=1000)
            {
                for(float y=area.y+3;y<=area.yMax-h-3;y+=4)
                    for(float x=area.x+3;x<=area.xMax-w-3;x+=6)Consider(x,y);
                Consider(area.xMax-w-3,area.yMax-h-3);
            }
            return best;
        }
    }
}
