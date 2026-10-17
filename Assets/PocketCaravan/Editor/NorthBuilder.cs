using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 북유럽 지역 데이터와 맵 씬을 만든다. 고려용 MapJsonBuilder·StarterBuilder는 건드리지 않는다.
    /// 맵: Tools/MapGen/export_north.py → Maps/North/*.json + Art/North/Tiles/*.png → 이 메뉴.
    /// 데이터(마을·물건)는 GameDatabase 목록 끝에 덧붙인다. 메뉴 1(시작 세트)을 다시 돌리면 지워지니 이 메뉴도 다시 돌린다.
    /// 지역을 합칠 때 StarterBuilder·MapJsonBuilder로 옮기고 이 파일은 지운다.
    /// </summary>
    public static class NorthBuilder
    {
        const string Root = "Assets/PocketCaravan";
        const string MapDir = Root + "/Maps/North";
        const string TilePngDir = Root + "/Art/North/Tiles";
        const string TileAssetDir = Root + "/Art/North/TileAssets";
        const string SceneDir = Root + "/Scenes/North";
        const string DataDir = Root + "/Data/North";
        const string DbPath = Root + "/Resources/GameDatabase.asset";
        const string Prefix = "North_";

        /// <summary>북유럽 맵. 시작 맵이 맨 앞</summary>
        static readonly string[] MapOrder = { "North_Birka" };

        /// <summary>북유럽 안에서의 좌표 (맵 단위). 지역을 합칠 때 다 같이 옮긴다.</summary>
        static readonly Dictionary<string, Vector2Int> MapCoords = new Dictionary<string, Vector2Int>
        {
            ["North_Birka"] = new Vector2Int(0, 0),
        };

        class NpcDef { public string name, sprite, greeting; }

        static readonly Dictionary<string, NpcDef> QuestNpcs = new Dictionary<string, NpcDef>
        {
            ["north_innkeeper"] = new NpcDef { name = "여관 할머니", sprite = "elder_tall", greeting = "어서 오렴. 못 하나로도 수프를 끓이는 집이 여기란다. 일거리는 저 게시판에 붙어 있지." },
        };

        [Serializable] class MapObject
        {
            public string kind, id, npc, town, target, spawn, edge, text, name, character, sheet, type;
            public string[] lines;
            public int x, y, w = 1, h = 1, variant, idx, frames;
            public float fps, radius;
            public int[] path;
        }

        [Serializable] class MapFile
        {
            public string id, name;
            public int w, h, hours;
            public int[] ground, deco, obj, high, road, blocked;
            public string[] palette;
            public MapObject[] objects;
        }

        [MenuItem("Pocket Caravan/북유럽 만들기 (데이터 + 맵 씬)", priority = 20)]
        public static void BuildAll()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DbPath);
            if (db == null) { Debug.LogError("GameDatabase가 없습니다. 메뉴 1(시작 세트 만들기)을 먼저 실행하세요."); return; }
            EnsureFolder(DataDir); EnsureFolder(TileAssetDir); EnsureFolder(SceneDir);

            BuildData(db);
            AssetDatabase.SaveAssets();
            BuildWorldIndex();

            var paths = new List<string>();
            foreach (var id in MapOrder)
            {
                string json = $"{MapDir}/{id}.json";
                if (!File.Exists(json)) { Debug.LogWarning($"{json} 없음 (Tools/MapGen/export_north.py 실행)"); continue; }
                string scenePath = $"{SceneDir}/{id}.unity";
                BuildScene(JsonUtility.FromJson<MapFile>(File.ReadAllText(json)), scenePath);
                paths.Add(scenePath);
            }

            // 빌드 목록: 기존 순서(0번 = 고려 시작 맵)는 그대로 두고 끝에 덧붙인다
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var p in paths)
                if (!list.Exists(s => s.path == p)) list.Add(new EditorBuildSettingsScene(p, true));
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
            if (paths.Count > 0) EditorSceneManager.OpenScene(paths[0]);
            Debug.Log($"[북유럽] 맵 {paths.Count}개를 만들었습니다. 이 씬에서 Play하면 바이킹 출신으로 시작합니다.");
        }

        // ---------- 데이터 ----------

        static RegionData BuildData(GameDatabase db)
        {
            var north = Upsert<RegionData>($"{DataDir}/Region_North.asset", r => { r.id = "north"; r.displayName = "북유럽"; r.note = "D구역. 한랭 지형·피오르·숲. 특산: 모피, 호박"; });

            TownData Town(string file, string id, string name, Vector2Int coord, int level) =>
                Upsert<TownData>($"{DataDir}/{file}.asset", t =>
                {
                    t.id = id; t.displayName = name; t.region = north; t.worldCoord = coord; t.startLevel = level;
                    t.stock.Clear(); t.vehiclesForSale.Clear(); t.defaultOptimal = 4f;
                });
            var birka = Town("Town_Birka", "north_birka", "비르카", new Vector2Int(0, 0), 1);

            ItemData Item(string id, string name, float weight, float price, TownData origin) =>
                Upsert<ItemData>($"{DataDir}/Item_{id}.asset", i =>
                {
                    i.id = id; i.displayName = name; i.weight = weight; i.basePrice = price; i.originTown = origin;
                    i.pricePerMap = 0.5f; i.questItem = false;
                    i.icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Items/{id}.png");
                });
            // 산지 마을은 그 맵이 생기면 바꾼다 (모피: 사냥꾼 마을, 호박: 호박 해안)
            var mead = Item("north_mead", "꿀술", 2f, 12f, birka);
            var fur = Item("north_fur", "모피", 2f, 30f, null);
            var amber = Item("north_amber", "호박", 1f, 50f, null);
            var meat = Item("north_dried_meat", "말린 고기", 2f, 8f, null);

            birka.stock.Add(S(mead, 25)); birka.stock.Add(S(fur, 6)); birka.stock.Add(S(amber, 4)); birka.stock.Add(S(meat, 20));
            EditorUtility.SetDirty(birka);

            // GameDatabase에 덧붙이기 (북유럽 것은 지우고 다시 넣는다)
            db.towns.RemoveAll(t => t == null || t.id.StartsWith("north_"));
            db.items.RemoveAll(i => i == null || i.id.StartsWith("north_"));
            db.towns.Add(birka);
            db.items.AddRange(new[] { mead, fur, amber, meat });
            EditorUtility.SetDirty(db);
            return north;
        }

        static TownData.StockEntry S(ItemData item, float optimal) => new TownData.StockEntry { item = item, optimal = optimal };

        // ---------- 세계 지도 색인 ----------

        /// <summary>WorldIndex에서 북유럽 맵만 지우고 다시 적는다 (고려 맵은 그대로).</summary>
        static void BuildWorldIndex()
        {
            const string indexPath = Root + "/Resources/WorldIndex.asset";
            var index = AssetDatabase.LoadAssetAtPath<WorldIndex>(indexPath);
            if (index == null) { index = ScriptableObject.CreateInstance<WorldIndex>(); AssetDatabase.CreateAsset(index, indexPath); }
            index.maps.RemoveAll(m => m.mapId.StartsWith(Prefix));
            index.places.RemoveAll(p => p.mapId.StartsWith(Prefix));
            foreach (var id in MapOrder)
            {
                string json = $"{MapDir}/{id}.json";
                if (!File.Exists(json)) continue;
                var f = JsonUtility.FromJson<MapFile>(File.ReadAllText(json));
                var info = new WorldIndex.MapInfo { mapId = f.id, displayName = f.name, coord = MapCoords.TryGetValue(f.id, out var mc) ? mc : Vector2Int.zero };
                foreach (var o in f.objects)
                {
                    if (o.kind == "exit" && !string.IsNullOrEmpty(o.target) && !info.neighbors.Contains(o.target)) info.neighbors.Add(o.target);
                    if (o.kind == "npc") index.places.Add(new WorldIndex.Place { kind = "npc", id = o.npc, mapId = f.id });
                    if (o.kind == "market") { index.places.Add(new WorldIndex.Place { kind = "market", id = o.town, mapId = f.id }); info.town = o.town; }
                    if (o.kind == "board") index.places.Add(new WorldIndex.Place { kind = "board", id = o.town, mapId = f.id });
                }
                index.maps.Add(info);
            }
            EditorUtility.SetDirty(index);
            AssetDatabase.SaveAssets();
        }

        // ---------- 씬 ----------

        static void BuildScene(MapFile f, string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // 새 씬을 만든 뒤에 에셋을 불러온다 (앞서 불러온 에셋은 내려감)
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DbPath);
            var region = AssetDatabase.LoadAssetAtPath<RegionData>($"{DataDir}/Region_North.asset");
            var tiles = new Tile[f.palette.Length];
            for (int i = 1; i < f.palette.Length; i++) tiles[i] = TileFor(f.palette[i]);
            var blocker = BlockerTile();

            var gridGo = new GameObject("Map");
            var grid = gridGo.AddComponent<Grid>();
            var ground = Layer(gridGo, "Ground", 0);
            var deco = Layer(gridGo, "Deco", 1);
            var obj = Layer(gridGo, "Objects", 5);
            var high = Layer(gridGo, "Above", 20);
            var blocked = Layer(gridGo, "Blocked", 0, renderer: false);

            Paint(ground, f.ground, f, tiles);
            Paint(deco, f.deco, f, tiles);
            Paint(obj, f.obj, f, tiles);
            Paint(high, f.high, f, tiles);
            for (int i = 0; i < f.blocked.Length; i++)
                if (f.blocked[i] != 0) blocked.SetTile(Cell(f, i), blocker);

            var map = gridGo.AddComponent<MapRoot>();
            map.mapId = f.id;
            map.travelHours = f.hours;
            map.displayName = f.name;
            map.ground = ground; map.road = null; map.blocked = blocked;
            map.region = region;
            map.worldCoord = MapCoords.TryGetValue(f.id, out var wc) ? wc : Vector2Int.zero;

            var roads = gridGo.AddComponent<RoadGrid>();
            roads.width = f.w; roads.height = f.h;
            roads.grades = new int[f.w * f.h];
            for (int i = 0; i < f.road.Length; i++) { var c = Cell(f, i); roads.grades[c.y * f.w + c.x] = f.road[i]; }

            var objects = new GameObject("Objects").transform;
            RoadEventDirector director = null;
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
                        var ll = new Vector3Int(o.x, f.h - 1 - (o.y + o.h - 1), 0);
                        ex.transform.position = grid.CellToWorld(ll) + new Vector3(0.01f, 0.01f, 0);
                        ex.size = new Vector2Int(o.w, o.h);
                        ex.targetScene = o.target; ex.targetSpawnId = o.spawn;
                        break;
                    case "npc":
                        if (!QuestNpcs.TryGetValue(o.npc, out var nd)) { Debug.LogWarning($"[북유럽] NPC 정의 없음: {o.npc}"); break; }
                        var qn = Character(nd.name, nd.sprite, center, objects).AddComponent<QuestNpc>();
                        qn.npcId = o.npc; qn.displayName = nd.name; qn.greeting = nd.greeting;
                        var mk = new GameObject("Quest Marker").AddComponent<SpriteRenderer>();
                        mk.transform.SetParent(qn.transform, false);
                        mk.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                        mk.sortingOrder = 25;
                        qn.marker = mk;
                        qn.availableSprite = LoadSprite(PlaceholderArt.MarkerAvailable);
                        qn.turnInSprite = LoadSprite(PlaceholderArt.MarkerTurnIn);
                        qn.inProgressSprite = LoadSprite(PlaceholderArt.MarkerProgress);
                        break;
                    case "market":
                        var t = db.towns.Find(x => x != null && x.id == o.town);
                        var npc = Character($"장터 상인 ({(t != null ? t.displayName : o.town)})", "merchant_tall", center, objects);
                        npc.AddComponent<TownMarket>().town = t;
                        break;
                    case "talker":
                        var tk = Character(o.name, o.character, center, objects).AddComponent<TalkNpc>();
                        tk.displayName = o.name; tk.lines = o.lines;
                        break;
                    case "sign":
                        var sg = new GameObject("이정표").AddComponent<TalkNpc>();
                        sg.transform.SetParent(objects); sg.transform.position = center;
                        sg.displayName = "이정표"; sg.lines = new[] { o.text }; sg.blocksMovement = false;
                        break;
                    case "board":
                        var bd = new GameObject("의뢰 게시판").AddComponent<ContractBoard>();
                        bd.transform.SetParent(objects); bd.transform.position = center;
                        bd.town = o.town; bd.blocksMovement = false;
                        break;
                    case "anim":
                        var an = new GameObject($"Anim {o.sheet}");
                        an.transform.SetParent(objects); an.transform.position = center;
                        var asr = an.AddComponent<SpriteRenderer>();
                        asr.sortingOrder = o.sheet == "ripples" ? 1 : 4;
                        var sa = an.AddComponent<SpriteAnim>();
                        sa.frames = SheetFrames(o.sheet, o.frames > 0 ? o.frames : 4);
                        sa.fps = o.fps > 0 ? o.fps : 4f;
                        if (sa.frames.Length > 0) asr.sprite = sa.frames[0];
                        break;
                    case "bandit":
                        var spot = new GameObject("도적 출몰 자리").AddComponent<BanditSpot>();
                        spot.transform.SetParent(objects); spot.transform.position = center;
                        spot.sightRange = o.radius > 0 ? o.radius : 6f;
                        var bgo = Character("도적", "bandit_tall", center, objects);
                        bgo.AddComponent<PlayerMotor>().walkSpeed = 3.4f;
                        var bandit = bgo.AddComponent<Bandit>();
                        bandit.anim = bgo.GetComponent<CharacterAnimator>();
                        var alert = new GameObject("Alert").AddComponent<SpriteRenderer>();
                        alert.transform.SetParent(bgo.transform, false);
                        alert.transform.localPosition = new Vector3(0f, 1.3f, 0f);
                        alert.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/NinjaAdventure/Ui/emote_alert.png");
                        alert.sortingOrder = 25; alert.enabled = false;
                        bandit.alert = alert;
                        spot.prefabAnimator = bandit.anim;
                        bgo.SetActive(false);
                        break;
                    case "event":
                        if (director == null)
                        {
                            director = new GameObject("돌발 사건").AddComponent<RoadEventDirector>();
                            director.transform.SetParent(objects);
                        }
                        foreach (var (etype, ech) in new[] { (RoadEvent.Type.FallenTraveler, "villager_rabbit_tall"), (RoadEvent.Type.Peddler, "trader_tall") })
                        {
                            var ego = Character(etype == RoadEvent.Type.Peddler ? "떠돌이 상인" : "쓰러진 행인", ech, center, objects);
                            var ev = ego.AddComponent<RoadEvent>();
                            ev.type = etype;
                            var body = new GameObject("Body");
                            body.transform.SetParent(ego.transform, false);
                            var esr = ego.GetComponent<SpriteRenderer>();
                            var bsr = body.AddComponent<SpriteRenderer>();
                            bsr.sprite = esr.sprite; bsr.sortingOrder = esr.sortingOrder;
                            var eanim = ego.GetComponent<CharacterAnimator>();
                            var banim = body.AddComponent<CharacterAnimator>();
                            banim.frames = eanim.frames;
                            UnityEngine.Object.DestroyImmediate(eanim);
                            UnityEngine.Object.DestroyImmediate(esr);
                            ev.body = body.transform;
                            director.events.Add(ev);
                            ego.SetActive(false);
                        }
                        break;
                    case "walker":
                        var pts = new List<Vector3>();
                        if (o.path != null)
                            for (int k = 0; k + 1 < o.path.Length; k += 2)
                                pts.Add(grid.GetCellCenterWorld(new Vector3Int(o.path[k], f.h - 1 - o.path[k + 1], 0)));
                        var wgo = Character("NPC 상인", "trader_tall", pts.Count > 0 ? pts[0] : center, objects);
                        var walker = wgo.AddComponent<Walker>();
                        walker.path = pts;
                        walker.traderIndex = o.idx;
                        walker.anim = wgo.GetComponent<CharacterAnimator>();
                        break;
                }
            }

            // 개발용: 이 씬에서 바로 Play하면 바이킹 출신으로
            var dev = new GameObject("북유럽 테스트 시작").AddComponent<NorthDevStart>();
            dev.origin = AssetDatabase.LoadAssetAtPath<OriginData>($"{Root}/Data/Origins/Origin_Viking.asset");

            // 플레이어
            var player = new GameObject("Player");
            player.transform.position = startPos;
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(player.transform, false);
            var sr = bodyGo.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 11;
            var anim = bodyGo.AddComponent<CharacterAnimator>();
            anim.frames = LoadFrames("tanuki_tall");
            sr.sprite = anim.frames[0];
            Shadow(player.transform);
            player.AddComponent<PlayerMotor>();
            player.AddComponent<PlayerController>().anim = anim;

            // 카메라: 고려 맵과 같은 설정
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.75f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x2A, 0x2E, 0x38, 0xFF);
            camGo.transform.position = new Vector3(startPos.x, startPos.y, -10f);
            var ppc = camGo.AddComponent<PixelPerfectCamera>();
            SetInt(ppc, "m_AssetsPPU", PixelArtImporter.PPU);
            SetInt(ppc, "m_RefResolutionX", 384);
            SetInt(ppc, "m_RefResolutionY", 216);
            SetInt(ppc, "m_GridSnapping", 1);
            camGo.AddComponent<CameraFollow>().target = player.transform;

            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            SetInt(light, "m_LightType", 4);

            // 후처리: 고려 맵과 같은 PostFX.asset을 그대로 쓴다 (다시 쓰지 않음)
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>($"{Root}/Settings/PostFX.asset");
            if (profile != null)
            {
                var vol = new GameObject("Post FX").AddComponent<Volume>();
                vol.isGlobal = true;
                vol.sharedProfile = profile;
            }

            EditorSceneManager.SaveScene(scene, path);
        }

        // ---------- 도우미 (MapJsonBuilder와 같은 동작, 북유럽 폴더를 씀) ----------

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

        static GameObject Character(string name, string character, Vector3 pos, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;
            var anim = go.AddComponent<CharacterAnimator>();
            anim.frames = LoadFrames(character);
            sr.sprite = anim.frames[0];
            Shadow(go.transform);
            return go;
        }

        static void Shadow(Transform parent)
        {
            var go = new GameObject("Shadow");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, -0.36f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Characters/shadow.png");
            sr.sortingOrder = 9;
        }

        static Sprite[] LoadFrames(string character)
        {
            var frames = new Sprite[16];
            string dirs = "dulr";
            for (int d = 0; d < 4; d++)
                for (int k = 0; k < 4; k++)
                    frames[d * 4 + k] = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Characters/{character}_{dirs[d]}{k}.png");
            if (frames[0] == null) Debug.LogWarning($"캐릭터 그림 없음: {character}");
            return frames;
        }

        static Sprite[] SheetFrames(string sheet, int count)
        {
            var list = new List<Sprite>();
            for (int i = 0; i < count; i++)
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/NinjaAdventure/Animated/{sheet}_{i}.png");
                if (sp != null) list.Add(sp);
            }
            if (list.Count == 0) Debug.LogWarning($"움직이는 그림 없음: {sheet}");
            return list.ToArray();
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

        static T Upsert<T>(string path, Action<T> apply) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            bool create = a == null;
            if (create) a = ScriptableObject.CreateInstance<T>();
            apply(a);
            if (create) AssetDatabase.CreateAsset(a, path);
            EditorUtility.SetDirty(a);
            return a;
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
