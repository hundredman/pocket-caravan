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
    /// Assets/PocketCaravan/Maps/*.json (Tools/MapGen에서 만든 맵)을 씬으로 만든다.
    /// 타일 그림은 Art/NinjaAdventure/Tiles의 PNG. 씬을 손으로 고친 뒤 다시 돌리면 그 내용은 사라진다.
    /// </summary>
    public static class MapJsonBuilder
    {
        const string Root = "Assets/PocketCaravan";
        const string TilePngDir = Root + "/Art/NinjaAdventure/Tiles";
        const string TileAssetDir = Root + "/Art/NinjaAdventure/TileAssets";
        const string DbPath = Root + "/Resources/GameDatabase.asset";

        // Compatibility for existing tools; the registration files are the source of truth.
        public static string[] MapOrder => WorldRegistry.Load().maps.ConvertAll(m => m.mapId).ToArray();
        public static Dictionary<string, Vector2Int> MapCoords
        {
            get
            {
                var coords = new Dictionary<string, Vector2Int>();
                foreach (var m in WorldRegistry.Load().maps) coords.Add(m.mapId, new Vector2Int(m.worldX, m.worldY));
                return coords;
            }
        }

        [Serializable]
        class MapObject
        {
            public string kind, id, npc, town, target, spawn, edge, text, name, character, sheet, type;
            public string[] lines;
            public int x, y, w = 1, h = 1, variant, idx, frames;
            public float fps, radius;
            public int[] path;
        }

        [Serializable]
        class MapFile
        {
            public string id, name;
            public int w, h, hours;
            public int[] ground, deco, obj, high, road, blocked;
            public string[] palette;
            public MapObject[] objects;
        }

        [MenuItem("Pocket Caravan/Tests/지역 통합 등록 검사")]
        public static void ValidateWorld()
        {
            var registry = WorldRegistry.Load();
            ValidateRegisteredMaps(registry);
            Debug.Log($"지역 통합 검사 통과: {registry.regions.Count}개 지역, {registry.maps.Count}개 맵. 씬·저장은 변경하지 않음.");
        }

        static void ValidateRegisteredMaps(WorldRegistry.Snapshot registry)
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DbPath);
            if (db == null) throw new InvalidDataException("GameDatabase가 없습니다.");
            UniqueIds(db.items.ConvertAll(v => v == null ? null : v.id), "상품");
            UniqueIds(db.towns.ConvertAll(v => v == null ? null : v.id), "마을");
            UniqueIds(db.vehicles.ConvertAll(v => v == null ? null : v.id), "운반 수단");
            UniqueIds(db.quests.ConvertAll(v => v == null ? null : v.id), "퀘스트");
            var npcIds = new List<string>();
            foreach (var r in registry.regions)
                foreach (var n in r.npcs ?? Array.Empty<WorldRegistry.NpcEntry>()) npcIds.Add(n.id);
            UniqueIds(npcIds, "NPC");
            var files = new Dictionary<string, MapFile>();
            foreach (var entry in registry.maps)
            {
                var f = JsonUtility.FromJson<MapFile>(File.ReadAllText(entry.jsonPath));
                if (f == null || f.id != entry.mapId || f.w <= 0 || f.h <= 0 || f.objects == null)
                    throw new InvalidDataException($"맵 ID/크기/객체 목록 오류: {entry.mapId}");
                foreach (var layer in new[] { f.ground, f.deco, f.obj, f.high, f.road, f.blocked })
                    if (layer == null || layer.Length != f.w * f.h) throw new InvalidDataException($"타일 층 길이 오류: {f.id}");
                var spawns = new List<string>();
                foreach (var o in f.objects)
                {
                    if (o == null) throw new InvalidDataException($"빈 맵 객체: {f.id}");
                    if (o.kind == "spawn") spawns.Add(o.id);
                    if (o.kind == "npc" && !Array.Exists(entry.region.npcs ?? Array.Empty<WorldRegistry.NpcEntry>(), n => n.id == o.npc))
                        throw new InvalidDataException($"지역 NPC 정의 누락: {f.id}/{o.npc}");
                    if ((o.kind == "market" || o.kind == "board") && !db.towns.Exists(t => t != null && t.id == o.town))
                        throw new InvalidDataException($"GameDatabase에 마을 미등록: {f.id}/{o.town}");
                    if (o.kind == "villager" && (entry.region.villagers == null || entry.region.villagers.Length == 0))
                        throw new InvalidDataException($"지역 주민 정의 누락: {f.id}");
                }
                UniqueIds(spawns, f.id + " 스폰");
                if (entry.mapId == registry.startMapId && !spawns.Contains(SpawnPoint.StartId))
                    throw new InvalidDataException("시작 맵에 S0 스폰이 없습니다.");
                files.Add(f.id, f);
            }
            foreach (var f in files.Values)
                foreach (var o in f.objects)
                    if (o.kind == "exit" && (!files.TryGetValue(o.target ?? "", out var target)
                        || !Array.Exists(target.objects, s => s.kind == "spawn" && s.id == o.spawn)))
                        throw new InvalidDataException($"출구 목표 맵/스폰 없음: {f.id} → {o.target}/{o.spawn}");
        }

        static void UniqueIds(IEnumerable<string> values, string kind)
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in values)
                if (string.IsNullOrWhiteSpace(id) || !used.Add(id)) throw new InvalidDataException($"{kind} ID 누락/중복: {id}");
        }

        [MenuItem("Pocket Caravan/2. 맵 씬 다시 만들기 (JSON → 씬)", priority = 2)]
        public static void BuildAll()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DbPath);
            if (db == null) { Debug.LogError("GameDatabase가 없습니다. 메뉴 1을 먼저 실행하세요."); return; }
            EnsureFolder(TileAssetDir);
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var registry = WorldRegistry.Load();
            ValidateRegisteredMaps(registry);

            BuildWorldIndex();

            var paths = new List<string>();
            foreach (var entry in registry.maps)
            {
                EnsureFolder(Path.GetDirectoryName(entry.scenePath).Replace('\\', '/'));
                BuildScene(JsonUtility.FromJson<MapFile>(File.ReadAllText(entry.jsonPath)), entry.scenePath, entry);
                paths.Add(entry.scenePath);
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

        /// <summary>선택한 맵만 재생성하고 공통 색인·빌드 목록을 갱신한다. 타 담당 씬의 재직렬화 방지.</summary>
        public static void BuildSelected(params string[] mapIds)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play 종료 후 실행하세요.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("수정 중인 씬을 먼저 저장하세요.");
            var registry = WorldRegistry.Load();
            ValidateRegisteredMaps(registry);
            var selected = new List<WorldRegistry.MapEntry>();
            foreach (var id in mapIds)
            {
                var entry = registry.Map(id);
                if (entry == null) throw new InvalidDataException("미등록 맵: " + id);
                if (!selected.Contains(entry)) selected.Add(entry);
            }
            foreach (var entry in selected)
            {
                EnsureFolder(Path.GetDirectoryName(entry.scenePath).Replace('\\', '/'));
                BuildScene(JsonUtility.FromJson<MapFile>(File.ReadAllText(entry.jsonPath)), entry.scenePath, entry);
            }
            BuildWorldIndex();
            var list = new List<EditorBuildSettingsScene>();
            var paths = new List<string>(registry.ScenePaths);
            foreach (var path in paths) list.Add(new EditorBuildSettingsScene(path, true));
            foreach (var scene in EditorBuildSettings.scenes)
                if (!paths.Contains(scene.path) && File.Exists(scene.path)) list.Add(scene);
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
        }

        /// <summary>맵 사이 연결과 NPC·장터 위치를 Resources/WorldIndex에 적는다 (길 안내용).</summary>
        static void BuildWorldIndex()
        {
            const string terrainPath = Root + "/Resources/WorldMap/GoryeoTerrain.png";
            var terrainImporter = AssetImporter.GetAtPath(terrainPath) as TextureImporter;
            if (terrainImporter != null)
            {
                terrainImporter.textureType = TextureImporterType.Default;
                terrainImporter.filterMode = FilterMode.Point;
                terrainImporter.mipmapEnabled = false;
                terrainImporter.isReadable = true;
                terrainImporter.textureCompression = TextureImporterCompression.Uncompressed;
                terrainImporter.maxTextureSize = 2048;
                terrainImporter.SaveAndReimport();
            }
            const string indexPath = Root + "/Resources/WorldIndex.asset";
            var index = AssetDatabase.LoadAssetAtPath<WorldIndex>(indexPath);
            if (index == null) { index = ScriptableObject.CreateInstance<WorldIndex>(); AssetDatabase.CreateAsset(index, indexPath); }
            index.maps.Clear(); index.places.Clear();
            foreach (var entry in WorldRegistry.Load().maps)
            {
                var f = JsonUtility.FromJson<MapFile>(File.ReadAllText(entry.jsonPath));
                var info = new WorldIndex.MapInfo { mapId = f.id, displayName = f.name, coord = new Vector2Int(entry.worldX, entry.worldY) };
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

        static void BuildScene(MapFile f, string path, WorldRegistry.MapEntry entry = null)
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
            map.travelHours = f.hours;
            map.displayName = f.name;
            map.ground = ground; map.road = null; map.blocked = blocked;
            // Independent test maps may omit registration and do not inherit a civilization.
            map.region = entry == null ? null : AssetDatabase.LoadAssetAtPath<RegionData>(entry.region.regionAssetPath);
            map.worldCoord = entry == null ? Vector2Int.zero : new Vector2Int(entry.worldX, entry.worldY);

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
                        var nd = Array.Find(entry?.region.npcs ?? Array.Empty<WorldRegistry.NpcEntry>(), n => n.id == o.npc);
                        if (nd == null) throw new InvalidDataException($"NPC 미등록: {f.id}/{o.npc}");
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
                        sg.displayName = "이정표"; sg.lines = new[] { o.text }; sg.blocksMovement = false; // 칸은 소품 타일이 막음
                        break;
                    case "board":
                        var bd = new GameObject("의뢰 게시판").AddComponent<ContractBoard>();
                        bd.transform.SetParent(objects); bd.transform.position = center;
                        bd.town = o.town; bd.blocksMovement = false;   // 칸은 게시판 타일이 막음
                        break;
                    case "anim":
                        var an = new GameObject($"Anim {o.sheet}");
                        an.transform.SetParent(objects); an.transform.position = center;
                        var asr = an.AddComponent<SpriteRenderer>();
                        asr.sortingOrder = o.sheet == "ripples" ? 1 : 4;   // 물결은 바닥 바로 위, 꽃은 소품과 같은 높이
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
                            // 눕힐 때 그림만 돌리도록 몸을 자식으로 옮긴다
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
                    case "growth":
                        var gr = new GameObject($"마을 발전 {o.town} {o.variant}등급");
                        gr.transform.SetParent(objects); gr.transform.position = center;
                        var gsr = gr.AddComponent<SpriteRenderer>();
                        gsr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TilePngDir}/{o.id}.png");
                        gsr.sortingOrder = 5;
                        var tg = gr.AddComponent<TownGrowthProp>();
                        tg.town = o.town; tg.minLevel = Mathf.Max(2, o.variant);
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
                    case "worker":
                        var workerGo = Character(o.name, o.character, center, objects);
                        var worker = workerGo.AddComponent<WorkingNpc>();
                        worker.displayName = o.name;
                        worker.lines = o.lines;
                        worker.anim = workerGo.GetComponent<CharacterAnimator>();
                        if (o.path != null)
                            for (int k = 0; k + 1 < o.path.Length; k += 2)
                                worker.path.Add(grid.GetCellCenterWorld(new Vector3Int(o.path[k], f.h - 1 - o.path[k + 1], 0)));
                        break;
                    case "ambience":
                        var sound = new GameObject("물가 주변음");
                        sound.transform.SetParent(objects);
                        sound.transform.position = center;
                        var ambience = sound.AddComponent<MapAmbience>();
                        ambience.clipName = o.id;
                        ambience.radius = o.radius > 0 ? o.radius : 10f;
                        break;
                    case "villager":
                        var villagers = entry?.region.villagers;
                        if (villagers == null || villagers.Length == 0) throw new InvalidDataException($"주민 정의 없음: {f.id}");
                        var v = villagers[(o.variant + o.idx) % villagers.Length];
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
            sr.sortingOrder = 11;
            var anim = bodyGo.AddComponent<CharacterAnimator>();
            anim.frames = LoadFrames("tanuki_tall");
            sr.sprite = anim.frames[0];
            Shadow(player.transform);
            player.AddComponent<PlayerMotor>();
            player.AddComponent<PlayerController>().anim = anim;

            // 카메라: 320×180 기준, 16 PPU 픽셀 퍼펙트
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.75f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x22, 0x2B, 0x22, 0xFF);
            camGo.transform.position = new Vector3(startPos.x, startPos.y, -10f);
            var ppc = camGo.AddComponent<PixelPerfectCamera>();
            SetInt(ppc, "m_AssetsPPU", PixelArtImporter.PPU);
            SetInt(ppc, "m_RefResolutionX", 384);
            SetInt(ppc, "m_RefResolutionY", 216);
            SetInt(ppc, "m_GridSnapping", 1);
            camGo.AddComponent<CameraFollow>().target = player.transform;

            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            SetInt(light, "m_LightType", 4);

            // 후처리: 은은한 빛번짐, 채도·대비 살짝, 가장자리 어둡게
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var vol = new GameObject("Post FX").AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = PostFxProfile();

            EditorSceneManager.SaveScene(scene, path);
        }

        /// <summary>별도 도구에서 한 장만 생성. 본편 맵 색인·빌드 씬 목록을 다시 쓰지 않는다.</summary>
        public static void BuildSingleMap(string json, string scenePath)
        {
            var map = JsonUtility.FromJson<MapFile>(json);
            BuildScene(map, scenePath, WorldRegistry.Load().Map(map.id));
        }

        /// <summary>모든 맵이 함께 쓰는 후처리 설정. 코드가 기준이라 매번 값을 다시 쓴다.</summary>
        static VolumeProfile PostFxProfile()
        {
            string path = $"{Root}/Settings/PostFX.asset";
            Directory.CreateDirectory($"{Root}/Settings");
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            T Get<T>() where T : VolumeComponent
            {
                if (!profile.TryGet<T>(out var c)) { c = profile.Add<T>(true); c.name = typeof(T).Name; AssetDatabase.AddObjectToAsset(c, profile); }
                c.active = true;
                return c;
            }
            var bloom = Get<Bloom>();
            bloom.threshold.Override(0.95f); bloom.intensity.Override(0.2f); bloom.scatter.Override(0.6f);
            var color = Get<ColorAdjustments>();
            color.saturation.Override(-6f); color.contrast.Override(6f); color.postExposure.Override(0f);
            var vig = Get<Vignette>();
            vig.intensity.Override(0.2f); vig.smoothness.Override(0.45f); vig.color.Override(new Color(0.12f, 0.08f, 0.16f));
            var smh = Get<WhiteBalance>();
            smh.temperature.Override(-4f); smh.tint.Override(0f);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
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

        /// <summary>16×24 캐릭터 (Art/Characters/이름_방향프레임.png). 아래를 보고 서 있다.</summary>
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

        /// <summary>발밑 그림자. 캐릭터가 땅에 서 있는 느낌을 준다.</summary>
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
                for (int f = 0; f < 4; f++)
                    frames[d * 4 + f] = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Characters/{character}_{dirs[d]}{f}.png");
            if (frames[0] == null) Debug.LogWarning($"캐릭터 그림 없음: {character} (Tools/ArtGen/chars.py 실행)");
            return frames;
        }

        /// <summary>움직이는 그림 프레임 (Art/NinjaAdventure/Animated/이름_0.png, _1.png ...)</summary>
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
