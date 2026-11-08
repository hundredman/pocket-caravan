using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 북유럽 맵(north_)에 있을 때 M을 누르면 고려 교역도 대신 "북유럽 교역도"를 보여 준다 (공용 WorldMapUI는 고치지 않음).
    /// 북유럽에 있는 동안만 공용 지도 컴포넌트를 잠시 끄고, 다른 지역으로 나가면 원래대로 켠다.
    /// 지도 그림·배치: Resources/WorldMap/NorthTerrain.png, NorthLayout.json (Tools/MapGen/north_cartography.py가 만든다).
    /// 2026-11-05 UI B안 통일(윤채): 그리기만 공용 CuteMap으로 바꿈. 열고 닫기·이름·마을·내 위치(OwnCell) 계산은 그대로.
    /// </summary>
    public class NorthMapUI : MonoBehaviour
    {
        [Serializable] public class Area { public string mapId; public float x, y, w, h, labelX, labelY; public string[] own; public string[] @out; [NonSerialized] public Dictionary<int, Vector2Int> anchor; }
        [Serializable] public class Atlas { public float width, height; public Area[] maps; }

        // WorldIndex에 아직 북유럽 맵이 없을 때(DB·목록을 다시 만들기 전) 쓰는 이름
        static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            { "north_birka", "비르카" },
            { "north_hunter", "모피 사냥꾼 마을" }, { "north_naru", "피오르 나루 마을" },
            { "north_laugar", "라우가르 온천 마을" }, { "north_herd", "순록 고원 마을" },
            { "north_runehill", "룬돌 언덕" }, { "north_icecave", "얼음 동굴 골짜기" },
            // 10/26 중앙 대륙 북쪽 1/4 (피자 구성: 마을 4곳 + 길 5곳)
            { "north_c_gatetown", "북문 마을" }, { "north_c_road_nw", "눈숲 고갯길" }, { "north_c_road_ne", "개울 다리길" },
            { "north_c_snowwood", "눈숲 마을" }, { "north_c_creek", "개울 나루 마을" }, { "north_c_runeway", "룬돌 큰길" },
            { "north_c_road_sw", "서남 솔숲길" }, { "north_c_road_se", "동남 물가길" }, { "north_c_crossing", "북쪽 길목 마을" },
        };
        static readonly Dictionary<string, string> Towns = new Dictionary<string, string>
        {
            { "north_birka", "north_birka" }, { "north_hunter", "north_hunter" }, { "north_naru", "north_naru" },
            { "north_laugar", "north_laugar" }, { "north_herd", "north_herd" }, { "north_c_gatetown", "north_post" },
            { "north_c_snowwood", "north_cwood" }, { "north_c_creek", "north_ccreek" }, { "north_c_crossing", "north_cross" },
        };
        bool open;
        Texture2D terrain;
        Atlas atlas;
        WorldMapUI shared;
        GameManager sharedOwner;
        bool suppressed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthMapUI>() != null) return;
            var go = new GameObject("NorthMapUI");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthMapUI>();
        }

        static bool InNorth => MapRoot.Current != null && MapRoot.Current.mapId != null && MapRoot.Current.mapId.StartsWith("north_");

        void Awake()
        {
            terrain = Resources.Load<Texture2D>("WorldMap/Unified/NorthMap");   // 11/05 모든 지역 같은 그림체 (unified_map.py, 배치·own·out은 NorthLayout 그대로)
            var json = Resources.Load<TextAsset>("WorldMap/Unified/NorthMapLayout");
            if (json != null) atlas = JsonUtility.FromJson<Atlas>(json.text);
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm != sharedOwner) { sharedOwner = gm; shared = gm != null ? gm.GetComponent<WorldMapUI>() : null; suppressed = false; }

            if (!InNorth)
            {
                if (open) Close();
                if (suppressed && shared != null) shared.enabled = true;   // 내가 끈 것만 되돌린다
                suppressed = false;
                return;
            }
            if (shared != null && shared.enabled) { shared.enabled = false; suppressed = true; }

            if (open && (InputReader.MapPressed() || InputReader.CancelPressed())) { Close(); return; }
            if (!open && InputReader.MapPressed() && !UIState.Blocking && gm != null && !gm.IsTravelling)
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
            bool ready = terrain != null && atlas != null && atlas.maps != null && atlas.width > 0 && atlas.height > 0;
            if (ready)
                foreach (var area in atlas.maps)
                {
                    var info = index != null ? index.Map(area.mapId) : null;
                    string name = info != null ? info.displayName : (Names.TryGetValue(area.mapId, out var n) ? n : area.mapId);
                    string town = info != null ? info.town : (Towns.TryGetValue(area.mapId, out var t) ? t : "");
                    bool destination = !string.IsNullOrEmpty(town) && gm.Player.contracts.Exists(c => c.toTown == town);
                    places.Add(new CuteMap.Place { name = name, at = new Vector2(area.labelX, area.labelY), destination = destination });
                    if (area.mapId == here)
                    {
                        var player = FindAnyObjectByType<PlayerMotor>();
                        if (player != null)
                        {
                            var cell = OwnCell(area, player.Cell.x, (int)area.h - 1 - player.Cell.y);
                            me = new Vector2(area.x + cell.x + .5f, area.y + cell.y + .5f);
                        }
                    }
                }
            string current = Names.TryGetValue(here, out var cn) ? cn : (index != null ? index.MapName(here) : here);
            if (Towns.TryGetValue(here, out var townId))
            {
                var td = gm.Db.towns.Find(x => x != null && x.id == townId);
                if (td != null) current += " · " + gm.GetMarket(td).LevelName;
            }
            var res = CuteMap.Draw("북유럽 교역도 · 피오르와 고원의 마을들", ready ? terrain : null, ready ? atlas.width : 0, ready ? atlas.height : 0, places, me, current,
                "다리 밑 트롤과 의뢰 기한을 확인하며 이동하세요", sideLabels: true);   // 2026-11-08 고려 지도와 같은 양옆 이름표·꺾은 선 (은샘, 기우님 확인 요청)
            if (res.close) Close();
        }

        // 2026-10-30: 맵 가장자리 출구 복도는 옆 맵 땅 위에 있어서, 그대로 찍으면 붉은 점이 옆 마을에 가 있다.
        // 이 맵 땅(own, 위에서부터 줄) 밖이면 가장 가까운 내 땅 칸에 찍는다. (x, 위에서부터 y)
        static Vector2Int OwnCell(Area area, int x, int y)
        {
            var own = area.own;
            if (own == null || own.Length == 0) return new Vector2Int(x, y);
            bool Mine(int cx, int cy) => cy >= 0 && cy < own.Length && cx >= 0 && cx < own[cy].Length && own[cy][cx] == '1';
            if (Mine(x, y)) return new Vector2Int(x, y);
            if (area.anchor == null)   // 출구 복도 칸 → 걸어서 가장 가까운 내 땅 칸(생성기가 미리 계산)
            {
                area.anchor = new Dictionary<int, Vector2Int>();
                if (area.@out != null)
                    foreach (var s in area.@out)
                    {
                        var p = s.Split(',');
                        if (p.Length == 4 && int.TryParse(p[0], out int ox) && int.TryParse(p[1], out int oy) && int.TryParse(p[2], out int ax) && int.TryParse(p[3], out int ay))
                            area.anchor[oy * 10000 + ox] = new Vector2Int(ax, ay);
                    }
            }
            if (area.anchor.TryGetValue(y * 10000 + x, out var a)) return a;
            int max = own.Length + own[0].Length;
            for (int r = 1; r < max; r++)
            {
                Vector2Int best = default; int bd = int.MaxValue;
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r || !Mine(x + dx, y + dy)) continue;
                        int d = dx * dx + dy * dy;
                        if (d < bd) { bd = d; best = new Vector2Int(x + dx, y + dy); }
                    }
                if (bd != int.MaxValue) return best;
            }
            return new Vector2Int(x, y);
        }
    }
}
