using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// M: 실제 맵 지형을 축약한 지도. 출구 정렬로 배치하며 가격용 좌표와는 독립적이다.
    /// 11/05 UI B안: 그리기는 공용 CuteMap (베네치아·북유럽·아라비아 지도와 같은 모양). 이름이 많아 양옆 줄 이름표.
    /// </summary>
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
        Texture2D regionalTerrain;
        Atlas regionalAtlas;
        Texture2D terrain;
        Atlas atlas;

        void Awake()
        {
            // 11/05 모든 지역 같은 그림체 (Tools/MapGen/unified_map.py). 예전 GoryeoTerrain 그림체는 쓰지 않는다
            terrain = Resources.Load<Texture2D>("WorldMap/Unified/GoryeoMap");
            regionalTerrain = Resources.Load<Texture2D>("WorldMap/Unified/GoryeoRegionalMap");
            var detail = Resources.Load<TextAsset>("WorldMap/Unified/GoryeoRegionalMapLayout");
            if(detail != null) regionalAtlas = JsonUtility.FromJson<Atlas>(detail.text);
            var json = Resources.Load<TextAsset>("WorldMap/Unified/GoryeoMapLayout");
            if (json != null) atlas = JsonUtility.FromJson<Atlas>(json.text);
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
            var activeAtlas = regional ? regionalAtlas : atlas;
            var activeTerrain = regional ? regionalTerrain : terrain;
            var places = new List<CuteMap.Place>();
            Vector2? me = null;
            if (activeAtlas != null)
            {
                foreach (var area in activeAtlas.maps)
                {
                    var info = index != null ? index.Map(area.mapId) : null;
                    string name = !string.IsNullOrEmpty(area.name) ? area.name : info != null ? info.displayName : area.mapId;
                    bool destination = info != null && !string.IsNullOrEmpty(info.town) && gm.Player.contracts.Exists(c => c.toTown == info.town);
                    places.Add(new CuteMap.Place { name = name, at = new Vector2(area.labelX, area.labelY), destination = destination });
                }
                if (activeAtlas.settlements != null)
                    foreach (var settlement in activeAtlas.settlements)
                        places.Add(new CuteMap.Place { name = settlement.text, at = new Vector2(settlement.x, settlement.y), minor = true });
                string here = MapRoot.Current != null ? MapRoot.Current.mapId : "";
                var areaHere = Array.Find(activeAtlas.maps, a => a.mapId == here);
                var player = PlayerController.Instance;
                if (areaHere != null && player != null)
                {
                    var c = player.Motor.Cell;
                    me = new Vector2(areaHere.x + c.x + .5f, areaHere.y + areaHere.h - c.y - .5f);
                }
            }
            string current = index != null && MapRoot.Current != null ? index.MapName(MapRoot.Current.mapId) : "";
            var res = CuteMap.Draw(regional ? "고려 상세 지도" : "고려 · 중앙 대륙 동쪽 교역도", activeTerrain,
                activeAtlas != null ? activeAtlas.width : 0, activeAtlas != null ? activeAtlas.height : 0, places, me, current,
                $"북 ↑ · 지역 이름 {places.Count}곳", null, regionalAtlas != null ? (regional ? "전체 지도" : "고려 상세") : null);
            if (res.toggle) regional = !regional;
            if (res.close) { open = false; UIState.Release(); }
        }

        public static Rect PanelRect(float width,float height)=>new Rect(8,8,Mathf.Max(200,width-16),Mathf.Max(180,height-16));
    }
}
