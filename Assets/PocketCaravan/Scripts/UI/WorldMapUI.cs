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
            public string mapId, name;
            public float x, y, w, h, labelX, labelY;
        }
        [Serializable] public class MapAnnotation { public string text; public float x,y; }
        [Serializable] public class Atlas
        {
            public float width, height;
            public MapArea[] maps;
            public MapAnnotation[] annotations;
            public MapAnnotation[] settlements;
        }

        bool open, regional;
        Texture2D regionalTerrain, regionalPaper;
        Atlas regionalAtlas;
        Texture2D terrain;
        Texture2D paperTerrain;
        Atlas atlas;
        GUIStyle ink, caption, title;
        static readonly Color Ink = new Color32(68, 57, 41, 255);

        void Awake()
        {
            terrain = Resources.Load<Texture2D>("WorldMap/GoryeoTerrain");
            paperTerrain = PaperCopy(terrain);
            regionalTerrain = Resources.Load<Texture2D>("WorldMap/GoryeoRegionalTerrain");
            regionalPaper = PaperCopy(regionalTerrain);
            var detail = Resources.Load<TextAsset>("WorldMap/GoryeoRegionalLayout");
            if(detail != null) regionalAtlas = JsonUtility.FromJson<Atlas>(detail.text);
            var json = Resources.Load<TextAsset>("WorldMap/GoryeoLayout");
            if (json != null) atlas = JsonUtility.FromJson<Atlas>(json.text);
        }

        static Texture2D PaperCopy(Texture2D source)
        {
            if(source==null || !source.isReadable || QualitySettings.activeColorSpace != ColorSpace.Linear) return null;
            var copy=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
            var colors=source.GetPixels32();
            for(int i=0;i<colors.Length;i++) colors[i]=UIStyle.Lin(colors[i]);
            copy.SetPixels32(colors);copy.Apply();return copy;
        }

        void OnDestroy()
        {
            if (paperTerrain != null) Destroy(paperTerrain);
            if (regionalPaper != null) Destroy(regionalPaper);
        }

        void Update()
        {
            if (open && (InputReader.MapPressed() || InputReader.CancelPressed()))
            {
                open = false;
                UIState.Release();
                return;
            }
            if (!open && InputReader.MapPressed() && !UIState.Blocking && GameManager.I != null && !GameManager.I.IsTravelling
                && !RegionMapOverride.Claims(MapRoot.Current != null ? MapRoot.Current.mapId : null))   // 지역 지도가 맡은 맵에서는 열지 않는다
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
            if (ink == null || ink.font != UIStyle.RegularFont)
            {
                ink = new GUIStyle(UIStyle.Rich) { font = UIStyle.RegularFont, fontSize = UIStyle.BodySize, wordWrap = false };
                foreach (var state in new[] { ink.normal, ink.hover, ink.active, ink.focused })
                    state.textColor = Color.black;
                caption = new GUIStyle(ink) { alignment = TextAnchor.MiddleCenter, fontSize = UIStyle.BodySize };
                title = new GUIStyle(ink) { fontSize = UIStyle.BodySize };
            }

            var panel=PanelRect(UIStyle.VirtualWidth,UIStyle.VirtualHeight);
            Paper(panel,new Color32(67,57,43,255));
            Paper(new Rect(panel.x+2,panel.y+2,panel.width-4,panel.height-4),new Color32(232,223,195,255));
            GUI.Label(new Rect(panel.x+12,panel.y+9,panel.width-130,20),regional?"고려 상세 지도":"고려 · 중앙 대륙 동쪽",title);
            if(GUI.Button(new Rect(panel.xMax-108,panel.y+8,96,20),regional?"전체 지도":"고려 상세",UIStyle.Button)&&regionalAtlas!=null)regional=!regional;
            var activeAtlas=regional?regionalAtlas:atlas;
            var activeTerrain=regional?regionalTerrain:terrain;
            var activePaper=regional?regionalPaper:paperTerrain;
            if(activeAtlas==null||activeTerrain==null)return;
            float bodyY=panel.y+43,bodyHeight=panel.height-80;
            float side=Mathf.Min(110,(panel.width-120)/2);
            float scale=Mathf.Min((panel.width-side*2-28)/activeAtlas.width,bodyHeight/activeAtlas.height);
            var canvas=new Rect(panel.center.x-activeAtlas.width*scale/2,bodyY+(bodyHeight-activeAtlas.height*scale)/2,activeAtlas.width*scale,activeAtlas.height*scale);
            GUI.color=Color.white;GUI.DrawTexture(canvas,activePaper!=null?activePaper:activeTerrain,ScaleMode.StretchToFill);
            Vector2 Project(float x,float y)=>new Vector2(canvas.x+x*scale,canvas.y+y*scale);
            var labels=new List<LabelPoint>();
            foreach(var area in activeAtlas.maps)
            {
                var info=index!=null?index.Map(area.mapId):null;
                string name=!string.IsNullOrEmpty(area.name)?area.name:info!=null?info.displayName:area.mapId;
                bool destination=info!=null&&!string.IsNullOrEmpty(info.town)&&gm.Player.contracts.Exists(c=>c.toTown==info.town);
                labels.Add(new LabelPoint{name=name,point=Project(area.labelX,area.labelY),destination=destination});
            }
            if(activeAtlas.settlements!=null)foreach(var settlement in activeAtlas.settlements)
                labels.Add(new LabelPoint{name=settlement.text,point=Project(settlement.x,settlement.y)});
            labels.Sort((a,b)=>a.point.x.CompareTo(b.point.x));int split=labels.Count/2;
            for(int sideIndex=0;sideIndex<2;sideIndex++)
            {
                var group=labels.GetRange(sideIndex==0?0:split,sideIndex==0?split:labels.Count-split);
                group.Sort((a,b)=>a.point.y.CompareTo(b.point.y));
                for(int i=0;i<group.Count;i++)
                {
                    var item=group[i];float row=bodyHeight/Mathf.Max(1,group.Count);
                    var rect=new Rect(sideIndex==0?panel.x+8:panel.xMax-side-8,bodyY+row*(i+.5f)-7,side,16);
                    rect.x=Mathf.Round(rect.x);rect.y=Mathf.Round(rect.y);
                    float edge=sideIndex==0?rect.xMax:rect.x;
                    Paper(new Rect(Mathf.Min(edge,item.point.x),rect.center.y,Mathf.Abs(edge-item.point.x),1),new Color32(156,145,118,255));
                    Paper(new Rect(item.point.x,Mathf.Min(rect.center.y,item.point.y),1,Mathf.Abs(rect.center.y-item.point.y)),new Color32(156,145,118,255));
                    Dot(item.point,2,item.destination?new Color32(159,88,38,255):Ink);
                    GUI.Label(rect,item.name,caption);
                }
            }
            string here=MapRoot.Current!=null?MapRoot.Current.mapId:"";var areaHere=Array.Find(activeAtlas.maps,a=>a.mapId==here);
            var player=PlayerController.Instance;
            if(areaHere!=null&&player!=null)
            {
                var c=player.Motor.Cell;var point=Project(areaHere.x+c.x+.5f,areaHere.y+areaHere.h-c.y-.5f);
                Dot(point,4,Color.white);Dot(point,2,new Color32(183,55,42,255));
            }
            GUI.Label(new Rect(panel.x+12,panel.yMax-32,panel.width-24,14),"붉은 점: 내 위치  ·  갈색: 의뢰 목적지  ·  북 ↑",ink);
            GUI.Label(new Rect(panel.x+12,panel.yMax-18,panel.width-24,14),"지역 이름 "+labels.Count+"곳 표시  ·  M / Esc 닫기",ink);
        }

        public static Rect PanelRect(float width,float height)=>new Rect(8,8,Mathf.Max(200,width-16),Mathf.Max(180,height-16));
        class LabelPoint { public string name; public Vector2 point; public bool destination; }

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
