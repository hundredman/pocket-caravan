using System;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// M: 베네치아·본토 서부(venice_*) 맵에서 여는 교역도. 그림과 배치는 Tools/MapGen/venice_worldmap.py가 만든
    /// Resources/WorldMap/VeniceTerrain.png·VeniceLayout.json이다(실제 씬 미리보기를 축약, 선은 실제 출구끼리).
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
            foreach (var area in atlas.maps)
            {
                var map = index != null ? index.Map(area.mapId) : null;
                string label = map != null ? map.displayName : area.mapId;
                bool destination = map != null && !string.IsNullOrEmpty(map.town) && gm.Player.contracts.Exists(c => c.toTown == map.town);
                if (destination) label += " · 의뢰";
                var p = Project(area.labelX, area.labelY);
                float labelWidth = Mathf.Min(116, caption.CalcSize(new GUIContent(label)).x + 8);
                var labelRect = new Rect(Mathf.Clamp(p.x - labelWidth / 2, canvas.x, canvas.xMax - labelWidth), p.y - 8, labelWidth, 14);
                Paper(labelRect, destination ? new Color32(236, 200, 150, 240) : new Color32(224, 210, 167, 235));
                GUI.Label(labelRect, label, caption);
                if (area.mapId == here)
                {
                    var player = FindAnyObjectByType<PlayerMotor>();
                    if (player != null)
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
                "붉은 점: 내 위치    갈색 선: 걸어서 잇는 길    붉은 점선: 키오자↔본토 뱃길    의뢰: 배달 목적지", ink);
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
