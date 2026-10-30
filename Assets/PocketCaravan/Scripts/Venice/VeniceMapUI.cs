using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// M: 베네치아·본토 서부(venice_*) 맵에서 여는 지도. 그림과 배치는 Tools/MapGen/venice_tilemap.py가 만든
    /// Resources/WorldMap/VeniceTerrain.png·VeniceLayout.json이다(실제 맵 타일을 칸당 4px로 줄여 붙임, 고려 지도와 같은 그림체).
    /// 다른 지역에서는 기존 지도(WorldMapUI)가 그대로 열린다. 표시만 하며 이동·시세·저장과 무관하다.
    /// </summary>
    public class VeniceMapUI : MonoBehaviour
    {
        // 배치 JSON 형식 (공용 WorldMapUI.Atlas와 같은 필드 + 지도 위 글씨). 공용 클래스 버전 차이에 묶이지 않게 따로 둔다.
        [Serializable] public class Area { public string mapId; public float x, y, w, h, labelX, labelY; }
        [Serializable] public class Note { public string text; public float x, y; }
        [Serializable] public class Layout { public float width, height; public Area[] maps; public Note[] annotations; }

        bool open;
        Texture2D terrain, paper;
        Layout atlas;
        GUIStyle ink, caption, title;
        static readonly Color Ink = new Color32(68, 57, 41, 255);

        static bool Mine(string mapId) => mapId != null && mapId.StartsWith("venice_");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Claim() => RegionMapOverride.Register(Mine);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (GameManager.I != null && GameManager.I.GetComponent<VeniceMapUI>() == null)
                GameManager.I.gameObject.AddComponent<VeniceMapUI>();
        }

        void Awake()
        {
            terrain = Resources.Load<Texture2D>("WorldMap/VeniceTerrain");
            if (terrain != null && terrain.isReadable && QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                // OnGUI 색 공간에 맞춘 표시용 사본 (원본 PNG는 그대로)
                paper = new Texture2D(terrain.width, terrain.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                var colors = terrain.GetPixels32();
                for (int i = 0; i < colors.Length; i++) colors[i] = UIStyle.Lin(colors[i]);
                paper.SetPixels32(colors);
                paper.Apply();
            }
            var json = Resources.Load<TextAsset>("WorldMap/VeniceLayout");
            if (json != null) atlas = JsonUtility.FromJson<Layout>(json.text);
        }

        void OnDestroy()
        {
            if (paper != null) Destroy(paper);
        }

        void Update()
        {
            string here = MapRoot.Current != null ? MapRoot.Current.mapId : null;
            if (open && (InputReader.MapPressed() || InputReader.CancelPressed() || !Mine(here)))
            {
                open = false;
                UIState.Release();
                return;
            }
            if (!open && Mine(here) && InputReader.MapPressed() && !UIState.Blocking && GameManager.I != null && !GameManager.I.IsTravelling)
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
            GUI.Label(new Rect(panel.x + 16, panel.y + 10, width - 32, 20), "베네치아·본토 서부 교역도", title);
            GUI.Label(new Rect(panel.xMax - 76, panel.y + 13, 65, 14), "M / Esc 닫기", ink);

            if (terrain == null || atlas == null || atlas.maps == null || atlas.width <= 0 || atlas.height <= 0)
            {
                GUI.Label(new Rect(panel.x + 16, panel.y + 45, width - 32, 24), "지도 데이터가 없습니다. MapGen venice_worldmap.py를 실행해 주세요.", ink);
                return;
            }
            float scale = Mathf.Min((width - 24) / atlas.width, 246 / atlas.height);
            var canvas = new Rect(panel.center.x - atlas.width * scale / 2, panel.y + 36, atlas.width * scale, atlas.height * scale);
            GUI.color = Color.white;
            GUI.DrawTexture(canvas, paper != null ? paper : terrain, ScaleMode.StretchToFill);

            Vector2 Project(float x, float y) => new Vector2(canvas.x + x * scale, canvas.y + y * scale);
            string here = MapRoot.Current != null ? MapRoot.Current.mapId : "";
            foreach (var note in atlas.annotations ?? Array.Empty<Note>())
            {
                var p = Project(note.x, note.y);
                GUI.Label(new Rect(p.x, p.y, 160, 14), note.text, ink);
            }
            var markers = new List<Rect>();
            foreach (var area in atlas.maps)
            {
                var p = Project(area.labelX, area.labelY);
                markers.Add(new Rect(p.x - 4, p.y - 4, 8, 8));
            }
            var placed = new List<Rect>();
            foreach (var area in atlas.maps)
            {
                var map = index != null ? index.Map(area.mapId) : null;
                string label = map != null ? map.displayName : area.mapId;
                bool destination = map != null && !string.IsNullOrEmpty(map.town) && gm.Player.contracts.Exists(c => c.toTown == map.town);
                if (destination) label += " · 의뢰";
                var p = Project(area.labelX, area.labelY);
                Dot(p, 3, destination ? new Color32(170, 95, 43, 255) : Ink);
                // 고려 지도와 같이: 마을 자리 네모 + 겹치지 않게 놓은 이름표 + 가는 연결선
                float labelWidth = Mathf.Min(116, caption.CalcSize(new GUIContent(label)).x + 8);
                var labelRect = PlaceLabel(p, labelWidth, new Rect(panel.x + 12, canvas.y, width - 24, canvas.height), placed, markers);
                placed.Add(labelRect);
                float endY = labelRect.center.y < p.y ? labelRect.yMax : labelRect.y;
                Paper(new Rect(p.x, Mathf.Min(p.y, endY), .5f, Mathf.Abs(endY - p.y)), new Color32(105, 92, 68, 130));
                Paper(new Rect(Mathf.Min(p.x, labelRect.center.x), endY, Mathf.Abs(p.x - labelRect.center.x), .5f), new Color32(105, 92, 68, 130));
                Paper(labelRect, destination ? new Color32(236, 200, 150, 240) : new Color32(224, 210, 167, 235));
                GUI.Label(labelRect, label, caption);
            }
            var here_ = Array.Find(atlas.maps, a => a.mapId == here);
            var player = here_ != null ? FindAnyObjectByType<PlayerMotor>() : null;
            if (player != null)
            {
                Vector3Int cell = player.Cell;
                var location = Project(here_.x + cell.x + .5f, here_.y + here_.h - cell.y - .5f);
                Dot(location, 5, Color.white);
                Dot(location, 3, new Color32(189, 55, 42, 255));
            }
            var legendY = panel.y + 286;
            GUI.Label(new Rect(panel.x + 16, legendY, width - 32, 14),
                "붉은 점: 내 위치   갈색 길: 교역로   푸른 물: 석호·강   네모: 마을   의뢰: 배달 목적지", ink);
            string current = index != null ? index.MapName(here) : here;
            var info = index != null ? index.Map(here) : null;
            if (info != null && !string.IsNullOrEmpty(info.town))
            {
                var town = gm.Db.towns.Find(t => t != null && t.id == info.town);
                if (town != null) current += " · " + gm.GetMarket(town).LevelName;
            }
            GUI.Label(new Rect(panel.x + 16, legendY + 17, width - 32, 14),
                "현재: " + current + "    |    본토 길은 대상 숙소에서 모이고, 동문 너머 중앙 권역은 아직 닦는 중입니다.", ink);
        }

        // 고려 지도(WorldMapUI)와 같은 이름표 자리 고르기. 공용 코드를 고치지 않으려고 베네치아 쪽에 같은 방식으로 둔다.
        static Rect PlaceLabel(Vector2 point, float width, Rect canvas, List<Rect> placed, List<Rect> markers)
        {
            var offsets = new[] { new Vector2(0, -22), new Vector2(0, 4), new Vector2(-24, -22), new Vector2(24, -22), new Vector2(-80, -8), new Vector2(80, -8), new Vector2(-120, -8), new Vector2(120, -8), new Vector2(0, -40), new Vector2(0, 22), new Vector2(-48, -22), new Vector2(48, -22), new Vector2(0, -58), new Vector2(0, 40) };
            Rect best = default; float bestPenalty = float.MaxValue;
            foreach (var offset in offsets)
            {
                var r = new Rect(Mathf.Clamp(point.x - width / 2 + offset.x, canvas.x, canvas.xMax - width), Mathf.Clamp(point.y + offset.y, canvas.y, canvas.yMax - 16), width, 16);
                var padded = new Rect(r.x - 2, r.y - 1, r.width + 4, r.height + 2); float penalty = 0;
                foreach (var other in placed) if (padded.Overlaps(other)) penalty += 1000;
                foreach (var other in markers) if (padded.Overlaps(other)) penalty += 100;
                if (penalty < bestPenalty) { best = r; bestPenalty = penalty; }
                if (penalty == 0) return r;
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
