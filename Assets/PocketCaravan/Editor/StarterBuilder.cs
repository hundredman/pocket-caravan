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
    /// 고려 시작 세트(임시 아트, 타일, 데이터, 맵 씬 3개)를 코드로 만든다.
    /// 이미 있는 에셋은 건드리지 않으므로 여러 번 실행해도 안전하다.
    /// 씬을 처음 상태로 되돌리고 싶을 때만 "맵 씬 다시 만들기"를 쓴다 (씬에서 고친 내용이 사라짐).
    /// </summary>
    public static class StarterBuilder
    {
        const string Root = "Assets/PocketCaravan";
        const string ArtDir = Root + "/Art/Placeholder";
        const string TileDir = Root + "/Art/Tiles";
        const string DataDir = Root + "/Data/Goryeo";
        const string DbPath = Root + "/Resources/GameDatabase.asset";
        const string SceneDir = Root + "/Scenes/Goryeo";

        // ---------- 맵 배치 ----------
        // T 나무  ~ 물  H 집(위는 지붕, 아래는 벽)  . 풀밭
        // - 오솔길  = 길  M 장터 상인
        // S 시작 자리  W 서쪽에서 들어온 자리  E 동쪽에서 들어온 자리
        // > 동쪽 출구  < 서쪽 출구

        class MapDef
        {
            public string id, displayName;
            public Vector2Int coord;
            public string townId;      // 장터가 있으면 마을 id
            public string east, west;  // 이웃 맵 id
            public string[] rows;
        }

        static readonly string[] TileNames = { "grass", "tree", "water", "roof", "wall", "road1", "road2", "road3" };

        static readonly MapDef[] Maps =
        {
            new MapDef
            {
                id = "Gaegyeong", displayName = "개경", coord = new Vector2Int(0, 0), townId = "gaegyeong", east = "Route01",
                rows = new[]
                {
                    "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                    "T..T.......................T...T",
                    "T...HHHH...HHHH......HHHH......T",
                    "T...HHHH...HHHH......HHHH......T",
                    "T.....-......-..........-......T",
                    "T.....--------------------.....T",
                    "T.............-.........-......T",
                    "T..HHHH.......-...M.....-..HHH.T",
                    "T..HHHH.......-.........-..HHH.T",
                    "T.............-...S.....-......>",
                    "T.............----------------E>",
                    "T.............-.........-......>",
                    "T..HHHH.......-.........-..HHH.T",
                    "T..HHHH.......-.........-..HHH.T",
                    "T.....~~~~~...-................T",
                    "T....~~~~~~~...................T",
                    "T..T.....................T.....T",
                    "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                },
            },
            new MapDef
            {
                id = "Route01", displayName = "1번 길 (개경 - 닥나무 마을)", coord = new Vector2Int(1, 0), west = "Gaegyeong", east = "Dakmok",
                rows = new[]
                {
                    "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                    "T....T.........T......~~..T....T",
                    "T..T......T...........~~.......T",
                    "T.......T.......T.....~~...T...T",
                    "T...T..........T......~~.......T",
                    "T..........T..........~~...T...T",
                    "T..T...T..............~~.......T",
                    "T.........T.....T.....~~...T...T",
                    "<.....T...............~~.......>",
                    "<W----..........T.....==......E>",
                    "<.......T.............~~.......>",
                    "T...T.........T.......~~...T...T",
                    "T.........T...........~~.......T",
                    "T..T.............T....~~..T....T",
                    "T......T..............~~.......T",
                    "T...........T.........~~...T...T",
                    "T....T..........T.....~~.......T",
                    "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                },
            },
            new MapDef
            {
                id = "Dakmok", displayName = "닥나무 마을", coord = new Vector2Int(2, 0), townId = "dakmok", west = "Route01",
                rows = new[]
                {
                    "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                    "T...T.........T.........T......T",
                    "T.........HHHH.....HHHH........T",
                    "T..T......HHHH.....HHHH....T...T",
                    "T.............-......-.........T",
                    "T.............--------.........T",
                    "T...HHHH......-...M............T",
                    "T...HHHH......-.........HHHH...T",
                    "<.............-.........HHHH...T",
                    "<W-------------................T",
                    "<..............................T",
                    "T...T......HHHH........T.......T",
                    "T..........HHHH................T",
                    "T....TT.................TT.....T",
                    "T....TT.....~~~~.......TT......T",
                    "T..........~~~~~~..............T",
                    "T...T..............T.......T...T",
                    "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                },
            },
        };

        // ---------- 메뉴 ----------

        [MenuItem("Pocket Caravan/1. 시작 세트 만들기 (없는 것만)", priority = 1)]
        public static void BuildMissing() => Build(false);

        [MenuItem("Pocket Caravan/2. 맵 씬 다시 만들기 (덮어쓰기)", priority = 2)]
        public static void RebuildScenes() => Build(true);

        [MenuItem("Pocket Caravan/3. 시작 맵 열기 (개경)", priority = 3)]
        public static void OpenStart()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene($"{SceneDir}/Gaegyeong.unity");
        }

        static void Build(bool overwriteScenes)
        {
            foreach (var d in new[] { ArtDir, TileDir, DataDir, Root + "/Resources", SceneDir }) EnsureFolder(d);

            PlaceholderArt.WriteAll(ArtDir);
            AssetDatabase.Refresh();

            var tiles = new Dictionary<string, Tile>();
            foreach (var n in TileNames) tiles[n] = MakeTile(n);

            CreateOrLoad<RoadTileSet>($"{TileDir}/RoadTiles.asset", r =>
                r.byGrade = new TileBase[] { null, tiles["road1"], tiles["road2"], tiles["road3"] });

            BuildData();

            var scenePaths = new List<string>();
            foreach (var def in Maps)
            {
                string path = $"{SceneDir}/{def.id}.unity";
                scenePaths.Add(path);
                if (overwriteScenes || !File.Exists(path)) BuildScene(def, path);
            }

            // Build Settings: 개경이 첫 씬
            var list = new List<EditorBuildSettingsScene>();
            foreach (var p in scenePaths) list.Add(new EditorBuildSettingsScene(p, true));
            foreach (var s in EditorBuildSettings.scenes)
                if (!scenePaths.Contains(s.path)) list.Add(s);
            EditorBuildSettings.scenes = list.ToArray();

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(scenePaths[0]);
            Debug.Log("[Pocket Caravan] 시작 세트 준비 완료. Play를 누르면 개경에서 시작합니다.");
        }

        // ---------- 데이터 ----------

        static GameDatabase BuildData()
        {
            var goryeo = CreateOrLoad<RegionData>($"{DataDir}/Region_Goryeo.asset", r => { r.id = "goryeo"; r.displayName = "고려"; });

            var gaegyeong = CreateOrLoad<TownData>($"{DataDir}/Town_Gaegyeong.asset", t =>
            {
                t.id = "gaegyeong"; t.displayName = "개경"; t.region = goryeo; t.worldCoord = new Vector2Int(0, 0); t.startLevel = 1;
            });
            var dakmok = CreateOrLoad<TownData>($"{DataDir}/Town_Dakmok.asset", t =>
            {
                t.id = "dakmok"; t.displayName = "닥나무 마을"; t.region = goryeo; t.worldCoord = new Vector2Int(2, 0); t.startLevel = 1;
            });

            ItemData Item(string id, string name, float weight, float price, TownData origin) =>
                CreateOrLoad<ItemData>($"{DataDir}/Item_{id}.asset", i =>
                {
                    i.id = id; i.displayName = name; i.weight = weight; i.basePrice = price; i.originTown = origin; i.pricePerMap = 0.5f;
                });

            var ginseng = Item("ginseng", "인삼", 1f, 40f, gaegyeong);
            var najeon = Item("najeon", "나전칠기", 3f, 120f, gaegyeong);
            var paper = Item("paper", "종이", 2f, 15f, dakmok);
            var grain = Item("grain", "곡식", 3f, 8f, null);
            var wood = Item("firewood", "땔감", 4f, 4f, null);

            var walk = CreateOrLoad<VehicleData>($"{DataDir}/Vehicle_Walk.asset", v =>
            {
                v.id = "walk"; v.displayName = "등짐"; v.capacity = 10f; v.price = 0; v.minTownLevel = 1;
                v.speedByGrade = new[] { 1f, 1.05f, 1.1f, 1.1f };
            });
            var cart = CreateOrLoad<VehicleData>($"{DataDir}/Vehicle_Handcart.asset", v =>
            {
                v.id = "handcart"; v.displayName = "손수레"; v.capacity = 30f; v.price = 150; v.minTownLevel = 1;
                v.speedByGrade = new[] { 0.45f, 0.85f, 1.15f, 1.2f };
            });

            var origin = CreateOrLoad<OriginData>($"{DataDir}/Origin_Goryeo.asset", o =>
            {
                o.id = "goryeo"; o.displayName = "고려 개경상인"; o.homeRegion = goryeo; o.ledgerStaleMultiplier = 0.5f;
            });

            // 마을 재고는 처음 만들 때만 채운다
            if (gaegyeong.stock.Count == 0)
            {
                gaegyeong.stock.AddRange(new[]
                {
                    S(ginseng, 20), S(najeon, 6), S(paper, 4), S(grain, 30), S(wood, 30),
                });
                gaegyeong.vehiclesForSale.Add(cart);
                EditorUtility.SetDirty(gaegyeong);
            }
            if (dakmok.stock.Count == 0)
            {
                dakmok.stock.AddRange(new[]
                {
                    S(paper, 30), S(ginseng, 3), S(najeon, 2), S(grain, 20), S(wood, 25),
                });
                EditorUtility.SetDirty(dakmok);
            }

            var db = CreateOrLoad<GameDatabase>(DbPath, d =>
            {
                d.startMoney = 100; d.startVehicle = walk; d.startOrigin = origin;
            });
            AddUnique(db.items, ginseng, najeon, paper, grain, wood);
            AddUnique(db.towns, gaegyeong, dakmok);
            AddUnique(db.vehicles, walk, cart);
            EditorUtility.SetDirty(db);
            return db;
        }

        static TownData.StockEntry S(ItemData item, float optimal) => new TownData.StockEntry { item = item, optimal = optimal };

        static void AddUnique<T>(List<T> list, params T[] items)
        {
            foreach (var i in items) if (i != null && !list.Contains(i)) list.Add(i);
        }

        // ---------- 씬 ----------

        static void BuildScene(MapDef def, string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 새 씬을 만들면 앞서 불러온 에셋이 내려가므로, 씬을 만든 뒤에 다시 불러온다.
            var tiles = new Dictionary<string, Tile>();
            foreach (var n in TileNames) tiles[n] = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/{n}.asset");
            var roadSet = AssetDatabase.LoadAssetAtPath<RoadTileSet>($"{TileDir}/RoadTiles.asset");
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DbPath);

            var gridGo = new GameObject("Map");
            var grid = gridGo.AddComponent<Grid>();
            grid.cellSize = Vector3.one;
            var ground = Layer(gridGo, "Ground", 0);
            var road = Layer(gridGo, "Road", 1);
            var blocked = Layer(gridGo, "Blocked", 2);

            var map = gridGo.AddComponent<MapRoot>();
            map.mapId = def.id;
            map.displayName = def.displayName;
            map.worldCoord = def.coord;
            map.region = db.towns.Count > 0 ? db.towns[0].region : null;
            map.ground = ground; map.road = road; map.blocked = blocked;

            var roads = gridGo.AddComponent<RoadGrid>();
            roads.roadTilemap = road;
            roads.tiles = roadSet;

            var objects = new GameObject("Objects").transform;
            int h = def.rows.Length, w = 32;
            var exitCells = new Dictionary<char, List<Vector3Int>>();
            Vector3 startPos = Vector3.zero;

            for (int r = 0; r < h; r++)
            {
                string row = def.rows[r].PadRight(w, '.');
                for (int x = 0; x < w; x++)
                {
                    char ch = row[x];
                    var c = new Vector3Int(x, h - 1 - r, 0);
                    ground.SetTile(c, tiles["grass"]);
                    Vector3 center = grid.GetCellCenterWorld(c);
                    switch (ch)
                    {
                        case 'T': blocked.SetTile(c, tiles["tree"]); break;
                        case '~': blocked.SetTile(c, tiles["water"]); break;
                        case 'H':
                            bool roofAbove = r > 0 && def.rows[r - 1].PadRight(w, '.')[x] == 'H';
                            blocked.SetTile(c, roofAbove ? tiles["wall"] : tiles["roof"]);
                            break;
                        case '-': road.SetTile(c, tiles["road1"]); break;
                        case '=': road.SetTile(c, tiles["road2"]); break;
                        case 'S':
                        case 'W':
                        case 'E':
                            road.SetTile(c, tiles["road1"]);
                            var sp = new GameObject($"Spawn {ch}").AddComponent<SpawnPoint>();
                            sp.id = ch.ToString();
                            sp.transform.SetParent(objects);
                            sp.transform.position = center;
                            if (ch == 'S' || startPos == Vector3.zero) startPos = center;
                            break;
                        case '>':
                        case '<':
                            road.SetTile(c, tiles["road1"]);
                            if (!exitCells.ContainsKey(ch)) exitCells[ch] = new List<Vector3Int>();
                            exitCells[ch].Add(c);
                            break;
                        case 'M':
                            var town = db.towns.Find(t => t.id == def.townId);
                            var npc = Character($"장터 상인 ({(town != null ? town.displayName : "?")})", PlaceholderArt.MerchantSprite, center, objects);
                            npc.AddComponent<TownMarket>().town = town;
                            break;
                    }
                }
            }

            foreach (var kv in exitCells)
            {
                bool east = kv.Key == '>';
                var cells = kv.Value;
                int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
                foreach (var c in cells) { minX = Mathf.Min(minX, c.x); minY = Mathf.Min(minY, c.y); maxX = Mathf.Max(maxX, c.x); maxY = Mathf.Max(maxY, c.y); }
                var exit = new GameObject(east ? "Exit East" : "Exit West").AddComponent<MapExit>();
                exit.transform.SetParent(objects);
                exit.transform.position = grid.CellToWorld(new Vector3Int(minX, minY, 0)) + new Vector3(0.01f, 0.01f, 0);
                exit.size = new Vector2Int(maxX - minX + 1, maxY - minY + 1);
                exit.targetScene = east ? def.east : def.west;
                exit.targetSpawnId = east ? "W" : "E"; // 동쪽으로 나가면 다음 맵의 서쪽에 선다
            }

            // 플레이어
            var player = Character("Player", PlaceholderArt.PlayerSprite, startPos, null);
            var body = player.GetComponent<SpriteRenderer>();
            Object.DestroyImmediate(body);
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(player.transform, false);
            var sr = bodyGo.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(PlaceholderArt.PlayerSprite);
            sr.sortingOrder = 11;
            player.AddComponent<GridMover>();
            player.AddComponent<PlayerController>().body = sr;

            // 카메라: 320×180 기준, 16 PPU 픽셀 퍼펙트
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.625f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x2E, 0x3B, 0x2C, 0xFF);
            camGo.transform.position = new Vector3(startPos.x, startPos.y, -10f);
            var ppc = camGo.AddComponent<PixelPerfectCamera>();
            SetInt(ppc, "m_AssetsPPU", PixelArtImporter.PPU);
            SetInt(ppc, "m_RefResolutionX", 320);
            SetInt(ppc, "m_RefResolutionY", 180);
            SetInt(ppc, "m_GridSnapping", 1); // Pixel Snapping
            camGo.AddComponent<CameraFollow>().target = player.transform;

            // 2D 조명: 전역 빛 하나 (나중에 낮·밤 색을 여기서 바꾼다)
            var lightGo = new GameObject("Global Light 2D");
            var light = lightGo.AddComponent<Light2D>();
            SetInt(light, "m_LightType", 4); // Global

            EditorSceneManager.SaveScene(scene, path);
        }

        static Tilemap Layer(GameObject grid, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(grid.transform, false);
            var tm = go.AddComponent<Tilemap>();
            var r = go.AddComponent<TilemapRenderer>();
            r.sortingOrder = order;
            return tm;
        }

        static GameObject Character(string name, string sprite, Vector3 pos, Transform parent)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(sprite);
            sr.sortingOrder = 10;
            return go;
        }

        // ---------- 에셋 도우미 ----------

        static Sprite LoadSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/{name}.png");

        static Tile MakeTile(string spriteName) =>
            CreateOrLoad<Tile>($"{TileDir}/{spriteName}.asset", t =>
            {
                t.sprite = LoadSprite(spriteName);
                t.colliderType = Tile.ColliderType.None;
            });

        static T CreateOrLoad<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var a = ScriptableObject.CreateInstance<T>();
            init(a);
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void SetInt(Object o, string prop, int value)
        {
            var so = new SerializedObject(o);
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogWarning($"[Pocket Caravan] {o.GetType().Name}.{prop} 속성을 찾지 못했습니다. Inspector에서 직접 설정하세요."); return; }
            p.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
