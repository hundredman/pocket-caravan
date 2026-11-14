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
                Close();
                return;
            }
            if (!open && InputReader.MapPressed() && !UIState.Blocking && GameManager.I != null && !GameManager.I.IsTravelling
                && !RegionMapOverride.Claims(MapRoot.Current != null ? MapRoot.Current.mapId : null))   // 지역 지도가 맡은 맵에서는 열지 않는다
            {
                if (!UIState.TryOpen(this, Close)) return;
                open = true;
                Sfx.Play("open");
            }
        }

        void Close() { open = false; UIState.Release(this); }
        void OnDisable() { if (open) Close(); }

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
                    string name = info != null && !string.IsNullOrEmpty(info.displayName) ? info.displayName
                        : !string.IsNullOrEmpty(area.name) ? area.name : area.mapId;
                    bool destination = info != null && !string.IsNullOrEmpty(info.town) && gm.Player.contracts.Exists(c => c.toTown == info.town);
                    places.Add(new CuteMap.Place { name = ShortLabel(name), at = new Vector2(area.labelX, area.labelY), destination = destination, mapId = area.mapId });
                }
                if (activeAtlas.settlements != null)
                    foreach (var settlement in activeAtlas.settlements)
                        places.Add(new CuteMap.Place { name = ShortLabel(settlement.text), at = new Vector2(settlement.x, settlement.y), minor = true });
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
                $"북 ↑ · 지역 이름 {places.Count}곳", null, regionalAtlas != null ? (regional ? "전체 지도" : "고려 상세") : null, sideLabels: true);
            if (res.toggle) regional = !regional;
            if (res.close) Close();
        }

        // 지도 이름표만 줄인다. 등록 이름/현재 위치/저장 ID는 그대로다.
        static string ShortLabel(string name)
        {
            switch (name)
            {
                case "예성 나루 마을": return "예성 나루";
                case "장단 들마을": return "장단";
                case "송악 산록 마을": return "송악";
                case "닥나무 마을": return "닥나무";
                case "솔재 길목 마을": return "솔재";
                case "버들나루 마을": return "버들나루";
                case "내륙 교역 길목": return "교역 길목";
                case "버들 물가 길": return "버들 물가";
                case "북쪽 쉼터 마을": return "북쪽 쉼터";
                case "남쪽 호숫가 마을": return "남쪽 호숫가";
                default: return name;
            }
        }

        public static Rect PanelRect(float width,float height)=>new Rect(8,8,Mathf.Max(200,width-16),Mathf.Max(180,height-16));
    }
}
