using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>M: 실제 맵 지형을 축약한 지도. 출구 정렬로 배치하며 가격용 좌표와는 독립적이다.</summary>
    public class WorldMapUI : MonoBehaviour
    {
        [Serializable] public class MapArea
        {
            public string mapId;
            public float x, y, w, h, labelX, labelY;
        }
        [Serializable] public class Atlas
        {
            public float width, height;
            public MapArea[] maps;
        }

        bool open;
        Texture2D terrain;
        Texture2D paperTerrain;
        Atlas atlas;
        GUIStyle ink, caption, title;
        static readonly Color Ink = new Color32(68, 57, 41, 255);

        void Awake()
        {
            terrain = Resources.Load<Texture2D>("WorldMap/GoryeoTerrain");
            if (terrain != null && terrain.isReadable && QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                // OnGUI의 색 공간에 맞춘 표시용 사본. 원본 지도 PNG는 그대로 보존한다.
                paperTerrain = new Texture2D(terrain.width, terrain.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                var colors = terrain.GetPixels32();
                for (int i = 0; i < colors.Length; i++) colors[i] = UIStyle.Lin(colors[i]);
                paperTerrain.SetPixels32(colors);
                paperTerrain.Apply();
            }
            var json = Resources.Load<TextAsset>("WorldMap/GoryeoLayout");
            if (json != null) atlas = JsonUtility.FromJson<Atlas>(json.text);
        }

        void OnDestroy()
        {
            if (paperTerrain != null) Destroy(paperTerrain);
        }

        void Update()
        {
            if (open && (InputReader.MapPressed() || InputReader.CancelPressed()))
            {
                open = false;
                UIState.Release();
                return;
            }
            if (!open && InputReader.MapPressed() && !UIState.Blocking && GameManager.I != null && !GameManager.I.IsTravelling)
            {
                open = true;
                UIState.Blocking = true;
                Sfx.Play("open");
            }
        }

        void OnGUI()
        {
            if (!open || GameManager.I == null) return;
            var gm = GameManager.I;
            var index = WorldIndex.Get();
            UIStyle.Begin();
            GUI.depth = -10;
            GUI.color = GUI.contentColor = Color.white;
            if (ink == null)
            {
                ink = new GUIStyle(UIStyle.Rich) { fontSize = 10, wordWrap = false };
                foreach (var state in new[] { ink.normal, ink.hover, ink.active, ink.focused })
                    state.textColor = UIStyle.Lin((Color32)Ink);
                caption = new GUIStyle(ink) { alignment = TextAnchor.MiddleCenter, fontSize = 9 };
                title = new GUIStyle(ink) { fontSize = 13 };
            }

            float width = Mathf.Min(610, UIStyle.VirtualWidth - 16), height = 330;
            var panel = new Rect((UIStyle.VirtualWidth - width) / 2, (UIStyle.VirtualHeight - height) / 2, width, height);
            Paper(panel, new Color32(100, 76, 48, 255));
            Paper(new Rect(panel.x + 3, panel.y + 3, width - 6, height - 6), new Color32(225, 211, 171, 255));
            GUI.Label(new Rect(panel.x + 16, panel.y + 10, width - 32, 20), "고려·중간권 교역도", title);
            GUI.Label(new Rect(panel.xMax - 76, panel.y + 13, 65, 14), "M / Esc 닫기", ink);

            if (terrain == null || atlas == null || atlas.maps == null || atlas.width <= 0 || atlas.height <= 0)
            {
                GUI.Label(new Rect(panel.x + 16, panel.y + 45, width - 32, 24), "지형 지도 데이터가 없습니다. MapGen export.py를 실행해 주세요.", ink);
                return;
            }
            float scale = Mathf.Min((width - 24) / atlas.width, 246 / atlas.height);
            var canvas = new Rect(panel.center.x - atlas.width * scale / 2, panel.y + 36, atlas.width * scale, atlas.height * scale);
            var color = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(canvas, paperTerrain != null ? paperTerrain : terrain, ScaleMode.StretchToFill);
            GUI.color = color;

            Vector2 Project(float x, float y) => new Vector2(canvas.x + x * scale, canvas.y + y * scale);
            string here = MapRoot.Current != null ? MapRoot.Current.mapId : "";
            var placedLabels = new List<Rect>();
            var markers = new List<Rect>();
            foreach(var area in atlas.maps)
                if(index != null && index.Map(area.mapId) != null)
                { var point=Project(area.labelX,area.labelY);markers.Add(new Rect(point.x-4,point.y-4,8,8)); }
            foreach (var area in atlas.maps)
            {
                var map = index != null ? index.Map(area.mapId) : null;
                if (map == null) continue;
                var p = Project(area.labelX, area.labelY);
                bool destination = gm.Player.contracts.Exists(c => c.toTown == map.town && !string.IsNullOrEmpty(map.town));
                Dot(p, 3, destination ? new Color32(170, 95, 43, 255) : Ink);
                string label = map.displayName;
                if (destination) label += " · 의뢰";
                // 글자 밑만 종이색으로 밝힌다. 구역별 네모 박스는 그리지 않는다.
                float labelWidth = Mathf.Min(116, caption.CalcSize(new GUIContent(label)).x + 8);
                var labelRect = PlaceLabel(p,labelWidth,canvas,placedLabels,markers);
                placedLabels.Add(labelRect);
                float endY=labelRect.center.y<p.y?labelRect.yMax:labelRect.y;
                Paper(new Rect(p.x,Mathf.Min(p.y,endY),.5f,Mathf.Abs(endY-p.y)),new Color32(105,92,68,130));
                Paper(new Rect(Mathf.Min(p.x,labelRect.center.x),endY,Mathf.Abs(p.x-labelRect.center.x),.5f),new Color32(105,92,68,130));
                Paper(labelRect, new Color32(224, 210, 167, 235));
                GUI.Label(labelRect, label, caption);
                if (area.mapId == here)
                {
                    var player = FindAnyObjectByType<PlayerMotor>();
                    if (player != null && MapRoot.Current != null)
                    {
                        Vector3Int cell = player.Cell;
                        var location = Project(area.x + cell.x + .5f, area.y + area.h - cell.y - .5f);
                        Dot(location, 5, Color.white);
                        Dot(location, 3, new Color32(189, 55, 42, 255));
                    }
                }
            }
            var legendY = panel.y + 286;
            GUI.Label(new Rect(panel.x + 16, legendY, width - 32, 14),
                "붉은 점: 내 위치    갈색 길: 교역로    푸른 물: 강·연못    나무: 숲    지붕: 마을    의뢰: 배달 목적지", ink);
            string current = index != null ? index.MapName(here) : here;
            var currentInfo = index != null ? index.Map(here) : null;
            if (currentInfo != null && !string.IsNullOrEmpty(currentInfo.town))
            {
                var town = gm.Db.towns.Find(t => t != null && t.id == currentInfo.town);
                if (town != null) current += " · " + gm.GetMarket(town).LevelName;
            }
            GUI.Label(new Rect(panel.x + 16, legendY + 17, width - 32, 14),
                "현재: " + current + "    |    길과 거래처를 살펴보고 돌아올 때 실을 화물도 계획해 보세요.", ink);
        }

        // Greedy placement keeps nearby map names legible as the registered world grows.
        static Rect PlaceLabel(Vector2 point,float width,Rect canvas,List<Rect> placed,List<Rect> markers)
        {
            var offsets=new[]{new Vector2(0,-22),new Vector2(0,4),new Vector2(-24,-22),new Vector2(24,-22),new Vector2(0,-40),new Vector2(0,22),new Vector2(-48,-22),new Vector2(48,-22),new Vector2(0,-58),new Vector2(0,40),new Vector2(0,-76),new Vector2(0,58)};
            Rect best=default;float bestPenalty=float.MaxValue;
            foreach(var offset in offsets)
            {
                var r=new Rect(Mathf.Clamp(point.x-width/2+offset.x,canvas.x,canvas.xMax-width),Mathf.Clamp(point.y+offset.y,canvas.y,canvas.yMax-16),width,16);
                var padded=new Rect(r.x-2,r.y-1,r.width+4,r.height+2);float penalty=0;
                foreach(var other in placed)if(padded.Overlaps(other))penalty+=1000;
                foreach(var other in markers)if(padded.Overlaps(other))penalty+=100;
                if(penalty<bestPenalty){best=r;bestPenalty=penalty;}
                if(penalty==0)return r;
            }
            return best;
        }

        static void Dot(Vector2 point, float radius, Color color)
        {
            Paper(new Rect(point.x - radius, point.y - radius, radius * 2, radius * 2), color);
        }

        static void Paper(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = UIStyle.Lin((Color32)color);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
