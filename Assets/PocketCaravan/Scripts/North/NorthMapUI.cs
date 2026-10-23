using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 북유럽 맵(north_)에 있을 때 M을 누르면 고려 교역도 대신 "북유럽 교역도"를 보여 준다 (공용 WorldMapUI는 고치지 않음).
    /// 북유럽에 있는 동안만 공용 지도 컴포넌트를 잠시 끄고, 다른 지역으로 나가면 원래대로 켠다.
    /// 지도 그림·배치: Resources/WorldMap/NorthTerrain.png, NorthLayout.json (Tools/MapGen/north_cartography.py가 만든다).
    /// </summary>
    public class NorthMapUI : MonoBehaviour
    {
        [Serializable] public class Area { public string mapId; public float x, y, w, h, labelX, labelY; }
        [Serializable] public class Atlas { public float width, height; public Area[] maps; }

        // WorldIndex에 아직 북유럽 맵이 없을 때(DB·목록을 다시 만들기 전) 쓰는 이름
        static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            { "north_birka", "비르카" }, { "north_fjord_road", "피오르 길" },
            { "north_hunter", "모피 사냥꾼 마을" }, { "north_naru", "피오르 나루 마을" },
            { "north_laugar", "라우가르 온천 마을" }, { "north_herd", "순록 고원 마을" },
            { "north_runehill", "룬돌 언덕" }, { "north_icecave", "얼음 동굴 골짜기" }, { "north_pass", "국경 고개" },
        };
        static readonly Dictionary<string, string> Towns = new Dictionary<string, string>
        {
            { "north_birka", "north_birka" }, { "north_hunter", "north_hunter" }, { "north_naru", "north_naru" },
            { "north_laugar", "north_laugar" }, { "north_herd", "north_herd" },
        };
        static readonly Color Ink = new Color32(68, 57, 41, 255);

        bool open;
        Texture2D terrain, paper;
        Atlas atlas;
        GUIStyle ink, caption, title;
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
            terrain = Resources.Load<Texture2D>("WorldMap/NorthTerrain");
            if (terrain != null && terrain.isReadable && QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                // 공용 지도와 같은 방식: OnGUI 색 공간에 맞춘 표시용 사본 (원본 PNG는 그대로)
                paper = new Texture2D(terrain.width, terrain.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                var c = terrain.GetPixels32();
                for (int i = 0; i < c.Length; i++) c[i] = UIStyle.Lin(c[i]);
                paper.SetPixels32(c);
                paper.Apply();
            }
            var json = Resources.Load<TextAsset>("WorldMap/NorthLayout");
            if (json != null) atlas = JsonUtility.FromJson<Atlas>(json.text);
        }

        void OnDestroy()
        {
            if (paper != null) Destroy(paper);
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
            if (ink == null)
            {
                ink = new GUIStyle(UIStyle.Rich) { fontSize = 10, wordWrap = false };
                foreach (var s in new[] { ink.normal, ink.hover, ink.active, ink.focused }) s.textColor = UIStyle.Lin((Color32)Ink);
                caption = new GUIStyle(ink) { alignment = TextAnchor.MiddleCenter, fontSize = 9 };
                title = new GUIStyle(ink) { fontSize = 13 };
            }

            float width = Mathf.Min(610, UIStyle.VirtualWidth - 16), height = 330;
            var panel = new Rect((UIStyle.VirtualWidth - width) / 2, (UIStyle.VirtualHeight - height) / 2, width, height);
            Fill(panel, new Color32(100, 76, 48, 255));
            Fill(new Rect(panel.x + 3, panel.y + 3, width - 6, height - 6), new Color32(225, 211, 171, 255));
            GUI.Label(new Rect(panel.x + 16, panel.y + 10, width - 32, 20), "북유럽 교역도 · 피오르와 고원의 마을들", title);
            GUI.Label(new Rect(panel.xMax - 76, panel.y + 13, 65, 14), "M / Esc 닫기", ink);

            if (terrain == null || atlas == null || atlas.maps == null || atlas.width <= 0 || atlas.height <= 0)
            {
                GUI.Label(new Rect(panel.x + 16, panel.y + 45, width - 32, 24), "북유럽 지도 데이터가 없습니다. Tools/MapGen/export_north.py를 실행해 주세요.", ink);
                return;
            }
            float scale = Mathf.Min((width - 24) / atlas.width, 246 / atlas.height);
            var canvas = new Rect(panel.center.x - atlas.width * scale / 2, panel.y + 36, atlas.width * scale, atlas.height * scale);
            GUI.DrawTexture(canvas, paper != null ? paper : terrain, ScaleMode.StretchToFill);

            Vector2 Project(float x, float y) => new Vector2(canvas.x + x * scale, canvas.y + y * scale);
            string here = MapRoot.Current != null ? MapRoot.Current.mapId : "";
            foreach (var area in atlas.maps)
            {
                var info = index != null ? index.Map(area.mapId) : null;
                string name = info != null ? info.displayName : (Names.TryGetValue(area.mapId, out var n) ? n : area.mapId);
                string town = info != null ? info.town : (Towns.TryGetValue(area.mapId, out var t) ? t : "");
                var p = Project(area.labelX, area.labelY);
                bool destination = !string.IsNullOrEmpty(town) && gm.Player.contracts.Exists(c => c.toTown == town);
                Dot(p, 3, destination ? new Color32(170, 95, 43, 255) : (Color32)Ink);
                string label = destination ? name + " · 의뢰" : name;
                float lw = Mathf.Min(116, caption.CalcSize(new GUIContent(label)).x + 8);
                var lr = new Rect(Mathf.Clamp(p.x - lw / 2, canvas.x, canvas.xMax - lw), p.y - 22, lw, 16);
                Fill(lr, new Color32(224, 210, 167, 235));
                GUI.Label(lr, label, caption);
                if (area.mapId == here)
                {
                    var player = FindAnyObjectByType<PlayerMotor>();
                    if (player != null)
                    {
                        var cell = player.Cell;
                        var at = Project(area.x + cell.x + .5f, area.y + area.h - cell.y - .5f);
                        Dot(at, 5, Color.white);
                        Dot(at, 3, new Color32(189, 55, 42, 255));
                    }
                }
            }
            float ly = panel.y + 286;
            GUI.Label(new Rect(panel.x + 16, ly, width - 32, 14),
                "붉은 점: 내 위치    갈색 길: 교역로    옅은 물: 얼어붙은 피오르    회녹색: 절벽    지붕: 마을    의뢰: 배달 목적지", ink);
            string current = Names.TryGetValue(here, out var cn) ? cn : (index != null ? index.MapName(here) : here);
            if (Towns.TryGetValue(here, out var townId))
            {
                var td = gm.Db.towns.Find(x => x != null && x.id == townId);
                if (td != null) current += " · " + gm.GetMarket(td).LevelName;
            }
            GUI.Label(new Rect(panel.x + 16, ly + 17, width - 32, 14),
                "현재: " + current + "    |    다리 밑 트롤과 의뢰 기한을 확인하며 이동하세요.", ink);
        }

        static void Dot(Vector2 p, float r, Color c) => Fill(new Rect(p.x - r, p.y - r, r * 2, r * 2), c);

        static void Fill(Rect rect, Color color)
        {
            var old = GUI.color;
            GUI.color = UIStyle.Lin((Color32)color);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
