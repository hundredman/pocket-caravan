using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// M: 아라비아(arabia_*) 맵에서 여는 지도 (2026-11-05, UI B안 통일 때 윤채가 만듦 — 전에는 아라비아에서 고려 지도가 열렸다).
    /// 그림과 배치: Resources/WorldMap/Unified/ArabiaMap.png·ArabiaMapLayout.json (Tools/MapGen/unified_map.py: 모든 지역 같은 그림체).
    /// 그리기는 공용 CuteMap (고려·베네치아·북유럽 지도와 같은 모양). 표시만 하며 이동·시세·저장과 무관하다.
    /// </summary>
    public class ArabiaMapUI : MonoBehaviour
    {
        [Serializable] public class Area { public string mapId; public float x, y, w, h, labelX, labelY; }
        [Serializable] public class Note { public string text; public float x, y; }
        [Serializable] public class Layout { public float width, height; public Area[] maps; public Note[] annotations; }

        bool open;
        Texture2D terrain;
        Layout atlas;

        static bool Mine(string mapId) => mapId != null && mapId.StartsWith("arabia_");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Claim() => RegionMapOverride.Register(Mine);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (GameManager.I != null && GameManager.I.GetComponent<ArabiaMapUI>() == null)
                GameManager.I.gameObject.AddComponent<ArabiaMapUI>();
        }

        void Awake()
        {
            terrain = Resources.Load<Texture2D>("WorldMap/Unified/ArabiaMap");
            var json = Resources.Load<TextAsset>("WorldMap/Unified/ArabiaMapLayout");
            if (json != null) atlas = JsonUtility.FromJson<Layout>(json.text);
        }

        void Update()
        {
            string here = MapRoot.Current != null ? MapRoot.Current.mapId : null;
            if (open && (InputReader.MapPressed() || InputReader.CancelPressed() || !Mine(here))) { Close(); return; }
            if (!open && Mine(here) && InputReader.MapPressed() && !UIState.Blocking && GameManager.I != null && !GameManager.I.IsTravelling)
            {
                open = true;
                UIState.Blocking = true;
                Sfx.Play("open");
            }
        }

        void Close()
        {
            open = false;
            UIState.Release();
        }

        void OnGUI()
        {
            if (!open || GameManager.I == null) return;
            var gm = GameManager.I;
            var index = WorldIndex.Get();
            UIStyle.Begin();
            GUI.depth = -10;
            GUI.color = GUI.contentColor = Color.white;
            string here = MapRoot.Current != null ? MapRoot.Current.mapId : "";
            var places = new List<CuteMap.Place>();
            Vector2? me = null;
            bool ready = terrain != null && atlas != null && atlas.maps != null;
            if (ready)
            {
                foreach (var area in atlas.maps)
                {
                    var map = index != null ? index.Map(area.mapId) : null;
                    bool destination = map != null && !string.IsNullOrEmpty(map.town) && gm.Player.contracts.Exists(c => c.toTown == map.town);
                    places.Add(new CuteMap.Place { name = map != null ? map.displayName : area.mapId, at = new Vector2(area.labelX, area.labelY), destination = destination });
                }
                var areaHere = Array.Find(atlas.maps, a => a.mapId == here);
                var player = areaHere != null ? FindAnyObjectByType<PlayerMotor>() : null;
                if (player != null) me = new Vector2(areaHere.x + player.Cell.x + .5f, areaHere.y + areaHere.h - player.Cell.y - .5f);
            }
            string current = index != null ? index.MapName(here) : here;
            var info = index != null ? index.Map(here) : null;
            if (info != null && !string.IsNullOrEmpty(info.town))
            {
                var town = gm.Db.towns.Find(t => t != null && t.id == info.town);
                if (town != null) current += " · " + gm.GetMarket(town).LevelName;
            }
            var res = CuteMap.Draw("아라비아 교역도 · 사막과 홍해 길", ready ? terrain : null, ready ? atlas.width : 0, ready ? atlas.height : 0, places, me, current,
                "아래: 아라비아 남부 마을 · 위: 중앙 대륙 남쪽 조각");   // 지역 글씨(notes)는 다른 지역 지도와 똑같이 안 쓴다
            if (res.close) Close();
        }
    }
}
