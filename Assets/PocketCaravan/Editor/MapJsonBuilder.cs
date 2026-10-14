using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// Assets/PocketCaravan/Maps/*.json (Tools/MapGen에서 만든 맵)을 씬으로 만든다.
    /// 타일 그림은 Art/NinjaAdventure/Tiles의 PNG. 씬을 손으로 고친 뒤 다시 돌리면 그 내용은 사라진다.
    /// </summary>
    public static class MapJsonBuilder
    {
        const string Root = "Assets/PocketCaravan";
        const string MapDir = Root + "/Maps";
        const string TilePngDir = Root + "/Art/NinjaAdventure/Tiles";
        const string TileAssetDir = Root + "/Art/NinjaAdventure/TileAssets";
        const string SceneDir = Root + "/Scenes/Goryeo";
        const string DbPath = Root + "/Resources/GameDatabase.asset";

        /// <summary>시작 맵이 맨 앞</summary>
        public static readonly string[] MapOrder = { "Naru", "Route01", "Gaegyeong", "Dakmok" };

        /// <summary>월드 지도 좌표 (맵 단위). 마을 TownData.worldCoord와 맞춘다.</summary>
        public static readonly Dictionary<string, Vector2Int> MapCoords = new Dictionary<string, Vector2Int>
        {
            ["Naru"] = new Vector2Int(0, 0), ["Route01"] = new Vector2Int(1, 0),
            ["Gaegyeong"] = new Vector2Int(2, 0), ["Dakmok"] = new Vector2Int(3, 0),
        };

        [Serializable]
        class MapObject
        {
            public string kind, id, npc, town, target, spawn, edge;
            public int x, y, w = 1, h = 1, variant, idx;
        }

        [Serializable]
        class MapFile
        {
            public string id, name;
            public int w, h;
            public int[] ground, deco, obj, high, road, blocked;
            public string[] palette;
            public MapObject[] objects;
        }

        class NpcDef
        {
            public string name, sprite, greeting;
        }

        static readonly Dictionary<string, NpcDef> QuestNpcs = new Dictionary<string, NpcDef>
        {
            ["naru_chief"] = new NpcDef { name = "나루 마을 촌장", sprite = PlaceholderArt.ChiefSprite, greeting = "예성강 물길이 개경까지 닿으니, 이 작은 나루에도 온갖 짐이 오간다네." },
            ["gaekju"] = new NpcDef { name = "개경 객주 곰돌", sprite = PlaceholderArt.GaekjuSprite, greeting = "객주는 상인들이 묵고 짐을 맡기는 곳이지. 일거리가 생기면 부르겠네." },
            ["dakmok_chief"] = new NpcDef { name = "닥나무 마을 촌장", sprite = PlaceholderArt.ChiefSprite, greeting = "우리 마을 종이는 질기고 하얗기로 이름났다네." },
        };

        static readonly (string name, string sprite, string[] lines)[] Villagers =
        {
            ("비단 장수", PlaceholderArt.MerchantSprite, new[] { "시전 거리에는 송나라 비단도, 아라비아 향료도 다 들어온다네.", "벽란도에 큰 배가 들어오는 날이면 값이 출렁이지." }),
            ("짐꾼", PlaceholderArt.GaekjuSprite, new[] { "등짐으로는 하루에 몇 번 못 오가. 수레가 있어야 돈이 되지.", "남문 밖 길은 평평해서 수레가 잘 굴러가." }),
            ("선비", PlaceholderArt.ChiefSprite, new[] { "저 위가 만월대일세. 송악산 아래 임금님이 계시지.", "개경은 고려의 서울이라 사람도 물건도 다 모인다네." }),
        };

        [MenuItem("Pocket Caravan/2. 맵 씬 다시 만들기 (JSON → 씬)", priority = 2)]
        public static void BuildAll()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DbPath);
            if (db == null) { Debug.LogError("GameDatabase가 없습니다. 메뉴 1을 먼저 실행하세요."); return; }
            EnsureFolder(TileAssetDir);
            EnsureFolder(SceneDir);

            var paths = new List<string>();
            foreach (var id in MapOrder)
            {
                string json = $"{MapDir}/{id}.json";
                if (!File.Exists(json)) { Debug.LogWarning($"{json} 없음"); continue; }
                string scenePath = $"{SceneDir}/{id}.unity";
                BuildScene(JsonUtility.FromJson<MapFile>(File.ReadAllText(json)), scenePath);
                paths.Add(scenePath);
            }

            var list = new List<EditorBuildSettingsScene>();
            foreach (var p in paths) list.Add(new EditorBuildSettingsScene(p, true));
            foreach (var s in EditorBuildSettings.scenes)
                if (!paths.Contains(s.path) && File.Exists(s.path)) list.Add(s);
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
            if (paths.Count > 0) EditorSceneManager.OpenScene(paths[0]);
            Debug.Log($"[Pocket Caravan] 맵 {paths.Count}개를 만들었습니다. 시작: {MapOrder[0]}");
        }

        static void BuildScene(MapFile f, string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // 새 씬을 만든 뒤에 에셋을 불러온다 (앞서 불러온 에셋은 내려감)
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DbPath);
            var tiles = new Tile[f.palette.Length];
            for (int i = 1; i < f.palette.Length; i++) tiles[i] = TileFor(f.palette[i]);
            var blocker = BlockerTile();

            var gridGo = new GameObject("Map");
            var grid = gridGo.AddComponent<Grid>();
            var ground = Layer(gridGo, "Ground", 0);
            var deco = Layer(gridGo, "Deco", 1);
            var obj = Layer(gridGo, "Objects", 5);
            var high = Layer(gridGo, "Above", 20);   // 나무 윗부분: 플레이어 위로 그림
            var blocked = Layer(gridGo, "Blocked", 0, renderer: false);

            Paint(ground, f.ground, f, tiles);
            Paint(deco, f.deco, f, tiles);
            Paint(obj, f.obj, f, tiles);
            Paint(high, f.high, f, tiles);
            for (int i = 0; i < f.blocked.Length; i++)
                if (f.blocked[i] != 0) blocked.SetTile(Cell(f, i), blocker);

            var map = gridGo.AddComponent<MapRoot>();
            map.mapId = f.id;
            map.displayName = f.name;
            map.ground = ground; map.road = null; map.blocked = blocked;
            map.region = db.towns.Count > 0 && db.towns[0] != null ? db.towns[0].region : null;
            map.worldCoord = MapCoords.TryGetValue(f.id, out var wc) ? wc : Vector2Int.zero;

            var roads = gridGo.AddComponent<RoadGrid>();
            roads.width = f.w; roads.height = f.h;
            roads.grades = new int[f.w * f.h];
            for (int i = 0; i < f.road.Length; i++) { var c = Cell(f, i); roads.grades[c.y * f.w + c.x] = f.road[i]; }

            var objects = new GameObject("Objects").transform;
            Vector3 startPos = Vector3.zero;
            bool hasStart = false;
            foreach (var o in f.objects)
            {
                var cell = new Vector3Int(o.x, f.h - 1 - o.y, 0);
                var center = grid.GetCellCenterWorld(cell);
                switch (o.kind)
                {
                    case "spawn":
                        var sp = new GameObject($"Spawn {o.id}").AddComponent<SpawnPoint>();
                        // 자리의 첫 칸 = 왼쪽 아래 칸 (JSON은 위쪽 기준이라 세로로 긴 자리는 아래 끝 칸)
                        var first = new Vector3Int(o.x, f.h - 1 - (o.y + o.h - 1), 0);
                        sp.transform.SetParent(objects); sp.transform.position = grid.GetCellCenterWorld(first);
                        sp.id = o.id; sp.size = new Vector2Int(o.w, o.h);
                        center = sp.transform.position;
                        if (o.id == SpawnPoint.StartId) { startPos = center; hasStart = true; }
                        else if (!hasStart && startPos == Vector3.zero) startPos = center;
                        break;
                    case "exit":
                        var ex = new GameObject($"Exit → {o.target}").AddComponent<MapExit>();
                        ex.transform.SetParent(objects);
                        // 출구 사각형의 왼쪽 아래 칸 (JSON은 위쪽 기준)
                        var ll = new Vector3Int(o.x, f.h - 1 - (o.y + o.h - 1), 0);
                        ex.transform.position = grid.CellToWorld(ll) + new Vector3(0.01f, 0.01f, 0);
                        ex.size = new Vector2Int(o.w, o.h);
                        ex.targetScene = o.target; ex.targetSpawnId = o.spawn;
                        break;
                    case "npc":
                        if (!QuestNpcs.TryGetValue(o.npc, out var nd)) break;
                        var qn = Character(nd.name, nd.sprite, center, objects).AddComponent<QuestNpc>();
                        qn.npcId = o.npc; qn.displayName = nd.name; qn.greeting = nd.greeting;
                        var mk = new GameObject("Quest Marker").AddComponent<SpriteRenderer>();
                        mk.transform.SetParent(qn.transform, false);
                        mk.transform.localPosition = new Vector3(0f, 1.75f, 0f);
                        mk.sortingOrder = 25;
                        qn.marker = mk;
                        qn.availableSprite = LoadSprite(PlaceholderArt.MarkerAvailable);
                        qn.turnInSprite = LoadSprite(PlaceholderArt.MarkerTurnIn);
                        qn.inProgressSprite = LoadSprite(PlaceholderArt.MarkerProgress);
                        break;
                    case "market":
                        var t = db.towns.Find(x => x != null && x.id == o.town);
                        var npc = Character($"장터 상인 ({(t != null ? t.displayName : o.town)})", PlaceholderArt.MerchantSprite, center, objects);
                        npc.AddComponent<TownMarket>().town = t;
                        break;
                    case "villager":
                        var v = Villagers[o.variant % Villagers.Length];
                        var vn = Character(v.name, v.sprite, center, objects).AddComponent<TalkNpc>();
                        vn.displayName = v.name;
                        vn.lines = v.lines;
                        break;
                }
            }

            // 플레이어
            var player = new GameObject("Player");
            player.transform.position = startPos;
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(player.transform, false);
            var sr = bodyGo.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(PlaceholderArt.PlayerSprite);
            sr.sortingOrder = 11;
            player.AddComponent<PlayerMotor>();
            player.AddComponent<PlayerController>().body = sr;

            // 카메라: 320×180 기준, 16 PPU 픽셀 퍼펙트
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.625f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x22, 0x2B, 0x22, 0xFF);
            camGo.transform.position = new Vector3(startPos.x, startPos.y, -10f);
            var ppc = camGo.AddComponent<PixelPerfectCamera>();
            SetInt(ppc, "m_AssetsPPU", PixelArtImporter.PPU);
            SetInt(ppc, "m_RefResolutionX", 320);
            SetInt(ppc, "m_RefResolutionY", 180);
            SetInt(ppc, "m_GridSnapping", 1);
            camGo.AddComponent<CameraFollow>().target = player.transform;

            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            SetInt(light, "m_LightType", 4);

            EditorSceneManager.SaveScene(scene, path);
        }

        static Vector3Int Cell(MapFile f, int i) => new Vector3Int(i % f.w, f.h - 1 - i / f.w, 0);

        static void Paint(Tilemap tm, int[] layer, MapFile f, Tile[] tiles)
        {
            if (layer == null) return;
            for (int i = 0; i < layer.Length; i++)
                if (layer[i] > 0 && tiles[layer[i]] != null) tm.SetTile(Cell(f, i), tiles[layer[i]]);
        }

        static Tilemap Layer(GameObject grid, string name, int order, bool renderer = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(grid.transform, false);
            var tm = go.AddComponent<Tilemap>();
            if (renderer) go.AddComponent<TilemapRenderer>().sortingOrder = order;
            return tm;
        }

        static GameObject Character(string name, string sprite, Vector3 pos, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(sprite);
            sr.sortingOrder = 10;
            return go;
        }

        static Sprite LoadSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Placeholder/{name}.png");

        static Tile TileFor(string name)
        {
            string assetPath = $"{TileAssetDir}/{name}.asset";
            var t = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (t != null) return t;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TilePngDir}/{name}.png");
            if (sprite == null) { Debug.LogWarning($"타일 그림 없음: {name}"); return null; }
            t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = sprite;
            t.colliderType = Tile.ColliderType.None;
            AssetDatabase.CreateAsset(t, assetPath);
            return t;
        }

        static Tile BlockerTile()
        {
            string p = $"{TileAssetDir}/_blocker.asset";
            var t = AssetDatabase.LoadAssetAtPath<Tile>(p);
            if (t != null) return t;
            t = ScriptableObject.CreateInstance<Tile>();
            t.colliderType = Tile.ColliderType.None;
            AssetDatabase.CreateAsset(t, p);
            return t;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void SetInt(UnityEngine.Object o, string prop, int value)
        {
            var so = new SerializedObject(o);
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogWarning($"{o.GetType().Name}.{prop} 없음"); return; }
            p.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
