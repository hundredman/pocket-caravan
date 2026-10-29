#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan.EditorTools
{
    public sealed class NaturePlayReviewRunner : MonoBehaviour
    {
        public static Action<bool,string,string> Record;
        public static Action<Exception> Complete;
        public static string ReviewRoot, Output, ChangesPath;
        public static bool ProductionOnly;
        [Serializable] class ArtPoint { public string asset,map; public float x,y; }
        [Serializable] class ArtPoints { public ArtPoint[] points; }
        [Serializable] class Map { public string id; public int w,h; public int[] blocked,ground; }
        [Serializable] class Endpoint { public int[] exit, arrival; }
        [Serializable] class Relocation { public string map,target,spawnId; public Endpoint @new; }
        [Serializable] class Changes { public Relocation[] relocations; }
        static void Check(bool value, string name, string detail = "") => Record(value, name, detail);
        IEnumerator Start()
        {
            var stack = new System.Collections.Generic.Stack<IEnumerator>(); stack.Push(Run());
            while (stack.Count > 0)
            {
                var run = stack.Peek();
                bool more = false; Exception error = null;
                try { more = run.MoveNext(); } catch (Exception e) { error = e; }
                if (error != null) { Complete(error); yield break; }
                if (!more) { stack.Pop(); continue; }
                var nested = run.Current as IEnumerator;
                if (nested != null && !(run.Current is CustomYieldInstruction)) { stack.Push(nested); continue; }
                yield return run.Current;
            }
            Complete(null);
        }
        PlayerMotor Motor => PlayerController.Instance.Motor;
        void Freeze()
        {
            Time.timeScale = 0;
            PlayerController.Instance.enabled = false;
            Check(!GameManager.I.AutosaveEnabled, "autosave remains disabled", MapRoot.Current.mapId);
        }
        Vector3 Position(float worldX, float worldY)
        {
            var map = MapRoot.Current; var grid = map.GetComponent<RoadGrid>();
            return new Vector3(worldX - map.terrainOrigin.x, grid.height - (worldY - map.terrainOrigin.y), 0);
        }
        void Pose(float worldX, float worldY)
        {
            var map = MapRoot.Current; Vector3 desired = Position(worldX, worldY); Vector3 best = desired; float distance = float.PositiveInfinity;
            var bounds = map.ground.cellBounds;
            foreach (var c in bounds.allPositionsWithin)
            {
                if (!map.IsWalkable(c) || MapExit.At(c) != null) continue;
                var center = map.CellCenter(c); float d = (center - desired).sqrMagnitude;
                if (d < distance) { distance = d; best = center - (Vector3)Motor.feetOffset; }
            }
            Check(!float.IsPositiveInfinity(distance), "reachable pose", worldX + "," + worldY);
            Motor.Teleport(best, Vector2Int.down); FindFirstObjectByType<CameraFollow>().SnapNow();
        }
        IEnumerator Capture(string name)
        {
            yield return null; yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            Check(image.width == 480 && image.height == 270, "native capture", name + " " + image.width + "x" + image.height);
            File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG()); Destroy(image);
        }
        IEnumerator VisibleCapture(string name)
        {
            yield return Capture(name);
            var body=PlayerController.Instance.anim.GetComponent<SpriteRenderer>();
            var point=Camera.main.WorldToScreenPoint(body.bounds.center);
            body.enabled=false;yield return Capture(name+"-without-player");body.enabled=true;
            var shown=new Texture2D(2,2);var hidden=new Texture2D(2,2);
            ImageConversion.LoadImage(shown,File.ReadAllBytes(Output+"/"+name+".png"));
            ImageConversion.LoadImage(hidden,File.ReadAllBytes(Output+"/"+name+"-without-player.png"));
            int visible=0;
            for(int py=Mathf.Max(0,(int)point.y-18);py<Mathf.Min(270,(int)point.y+18);py++)
                for(int px=Mathf.Max(0,(int)point.x-12);px<Mathf.Min(480,(int)point.x+12);px++)
                    if(shown.GetPixel(px,py)!=hidden.GetPixel(px,py))visible++;
            Destroy(shown);Destroy(hidden);Check(visible>=20,"player remains visible",name+": "+visible);
        }
        IEnumerator Run()
        {
            for (int i = 0; i < 20; i++) yield return null;
            Check(MapRoot.Current.previewOnly, "start in previewOnly"); Freeze();
            if(ProductionOnly){yield return ProductionRun();yield break;}
            string[] ids = { "goryeo_songak", "goryeo_jangdan", "Naru", "Gaegyeong", "Dakmok", "transit_woodtown", "transit_ferry", "transit_crossroads", "transit_southport", "transit_wetroad", "transit_pass", "transit_central_approach", "transit_south_inland" };
            foreach (string id in ids)
            {
                if (MapRoot.Current.mapId != id)
                {
                    yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ReviewRoot + "/Scenes/" + id + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
                    yield return null;
                }
                Freeze(); Check(MapRoot.Current.mapId == id && MapRoot.Current.previewOnly, "preview map loaded", id);
                var data = JsonUtility.FromJson<Map>(File.ReadAllText(ReviewRoot + "/Maps/" + id + ".json"));
                int differences = 0;
                for (int y = 0; y < data.h; y++) for (int x = 0; x < data.w; x++)
                    if (MapRoot.Current.blocked.HasTile(new Vector3Int(x, data.h - y - 1, 0)) != (data.blocked[y * data.w + x] != 0)) differences++;
                Check(differences == 0, "serialized/runtime barriers match", id);
                if (id == "goryeo_songak")
                {
                    Pose(182, -30); yield return VisibleCapture("songak-ridge");
                    int probes = 0;
                    var map = MapRoot.Current;
                    foreach (var target in map.blocked.cellBounds.allPositionsWithin)
                    {
                        if (probes >= 6) break;
                        Vector3 top = Position(172, -43), bottom = Position(192, -31);
                        if (target.x < top.x || target.x >= bottom.x || target.y > top.y || target.y < bottom.y || !map.blocked.HasTile(target)) continue;
                        var start = target + Vector3Int.down;
                        if (!map.IsWalkable(start) || !map.blocked.HasTile(target + Vector3Int.left) || !map.blocked.HasTile(target + Vector3Int.right)) continue;
                        Motor.Teleport(map.CellCenter(start) - (Vector3)Motor.feetOffset, Vector2Int.up);
                        int stopped = 0; bool stayedFree = true;
                        for (int tick = 0; tick < 90; tick++)
                        {
                            var before = Motor.transform.position; Motor.Tick(Vector2.up, Motor.walkSpeed, 1f / 60);
                            if (before == Motor.transform.position) stopped++;
                            stayedFree &= map.IsWalkable(Motor.Cell);
                        }
                        Check(stayedFree, "motor never enters barrier", id);
                        Check(stopped > 0, "ridge stops motor", stopped.ToString()); probes++;
                        Vector3 repaired;
                        Check(map.TryResolveSavedPosition(map.CellCenter(target) - (Vector3)Motor.feetOffset, Motor.feetOffset, out repaired) && map.IsWalkable(map.WorldToCell(repaired + (Vector3)Motor.feetOffset)), "blocked saved position repaired");
                    }
                    Check(probes > 0, "ridge collision probes ran", probes.ToString());
                }
                if (id == "goryeo_jangdan")
                {
                    Pose(216, 20); yield return null;
                    var depth = FindFirstObjectByType<CropDepth>();
                    Check(depth != null && depth.patches.Length == 2, "crop depth connected");
                    depth.Apply(Motor.Cell);
                    yield return VisibleCapture("rice-inside");
                    int ticks = 0; bool passage = true;
                    for (int i = 0; i < 180; i++) { Motor.Tick(Vector2.right, Motor.walkSpeed, 1f / 60); passage &= MapRoot.Current.IsWalkable(Motor.Cell); ticks++; }
                    Check(passage, "rice preserves passage");
                    Check(ticks == 180, "rice walk completed");
                    Pose(221, 26); yield return Capture("rice-path");
                }
                if (id == "transit_woodtown")
                {
                    Pose(116,-28);yield return Capture("source-upper");
                    Pose(121,-13);yield return Capture("source-lower");
                }
            }
            string artPoints=Output+"/../asset-views.json";
            if(File.Exists(artPoints))foreach(var point in JsonUtility.FromJson<ArtPoints>(File.ReadAllText(artPoints)).points)
            {
                if(MapRoot.Current.mapId!=point.map)
                {
                    yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ReviewRoot+"/Scenes/"+point.map+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;
                }
                Freeze();Pose(point.x,point.y);yield return Capture("asset-"+point.asset);
            }
            var changes = JsonUtility.FromJson<Changes>(File.ReadAllText(ChangesPath));
            foreach (var change in changes.relocations)
            {
                if (MapRoot.Current.mapId != change.map)
                {
                    yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ReviewRoot + "/Scenes/" + change.map + ".unity", new LoadSceneParameters(LoadSceneMode.Single));yield return null;
                }
                Freeze();MapExit entrance = null;
                foreach (var exit in MapExit.All) if (exit.targetScene == change.target && exit.targetSpawnId == change.spawnId) { entrance = exit;break; }
                Check(entrance != null,"relocated exit ID preserved",change.map+" / "+change.spawnId);
                var cell=MapRoot.Current.WorldToCell(entrance.transform.position);
                Vector2Int facing=entrance.edge=="N"?Vector2Int.up:entrance.edge=="S"?Vector2Int.down:entrance.edge=="E"?Vector2Int.right:Vector2Int.left;
                Motor.Teleport(MapRoot.Current.CellCenter(cell)-(Vector3)Motor.feetOffset,facing);entrance.Use(Motor);
                float start=Time.realtimeSinceStartup;
                while(GameManager.I.IsTravelling){if(Time.realtimeSinceStartup-start>60)throw new TimeoutException("Relocated boundary travel");yield return null;}
                Check(MapRoot.Current.mapId==change.target && MapRoot.Current.previewOnly,"relocated boundary reaches preview destination",change.map+" -> "+change.target);
                Check(MapRoot.Current.IsWalkable(Motor.Cell) && MapExit.At(Motor.Cell)==null,"relocated arrival is safe",change.spawnId);
                Freeze();
                MapExit reverse=null;foreach(var exit in MapExit.All)if(exit.targetScene==change.map){reverse=exit;break;}
                Check(reverse!=null,"reverse boundary exists");cell=MapRoot.Current.WorldToCell(reverse.transform.position);
                facing=reverse.edge=="N"?Vector2Int.up:reverse.edge=="S"?Vector2Int.down:reverse.edge=="E"?Vector2Int.right:Vector2Int.left;
                Motor.Teleport(MapRoot.Current.CellCenter(cell)-(Vector3)Motor.feetOffset,facing);reverse.Use(Motor);start=Time.realtimeSinceStartup;
                while(GameManager.I.IsTravelling){if(Time.realtimeSinceStartup-start>60)throw new TimeoutException("Reverse travel");yield return null;}
                Check(MapRoot.Current.mapId==change.map && MapRoot.Current.IsWalkable(Motor.Cell) && MapExit.At(Motor.Cell)==null,"relocated boundary round trip",change.spawnId);Freeze();
            }
            // Existing production travel coroutine, entered from a preview session with autosave disabled.
            foreach (string destination in new[] { "goryeo_songak", "transit_woodtown", "transit_ferry", "Naru" })
            {
                string source = MapRoot.Current.mapId; bool crossing = GoryeoTravelTransition.IsRegionalCrossing(source, destination);
                var gm = GameManager.I; float began = Time.realtimeSinceStartup; bool captured = false, middle = false;
                gm.TravelTo(destination, "S0");
                while (gm.IsTravelling)
                {
                    float fade = (float)typeof(GameManager).GetField("fade", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(gm);
                    if (!middle && fade >= 0.45f && fade < 0.999f)
                    {
                        yield return Capture("cover-" + destination); middle = true;
                    }
                    if (crossing && fade >= 0.999f && !captured)
                    {
                        yield return Capture("arrival-" + destination); captured = true;
                    }
                    if (Time.realtimeSinceStartup - began >= 60) throw new TimeoutException("Travel: " + destination);
                    yield return null;
                }
                Check(MapRoot.Current.mapId == destination, "real travel destination", source + " -> " + destination);
                Check(MapRoot.Current.previewOnly,"travel uses integrated candidate scene",destination);
                Check(!gm.AutosaveEnabled, "travel never enables production save");
                if (crossing) Check(captured, "regional arrival captured", GoryeoTravelTransition.RegionOf(destination));
                Freeze();
            }
            var state=new SaveData{scene=MapRoot.Current.mapId,layoutVersion=GoryeoLayout.Current,mapHeight=MapRoot.Current.GetComponent<RoadGrid>().height,
                px=Motor.transform.position.x,py=Motor.transform.position.y,money=1234,credit=21,stamina=.8f,
                origin=GameManager.I.Player.origin.id,vehicle=GameManager.I.Player.vehicle.id};
            GameManager.I.Player.reputation.ExportTo(state);GameManager.I.Quests.Export(state);
            string temporary=Output+"/roundtrip-save.json";
            Check(SaveSystem.WriteAt(temporary,state),"isolated save write");
            var loaded=JsonUtility.FromJson<SaveData>(File.ReadAllText(temporary));SaveSystem.Validate(loaded);
            bool applied=(bool)typeof(GameManager).GetMethod("TryApplySave",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(GameManager.I,new object[]{loaded});
            Check(applied && GameManager.I.Player.money==1234 && GameManager.I.Player.credit==21,"isolated save read/apply preserves money and credit");
            Check(loaded.scene==state.scene && loaded.layoutVersion==GoryeoLayout.Current && loaded.quests.Count==state.quests.Count && loaded.finishedQuests.Count==state.finishedQuests.Count,"isolated save preserves map and quest data");
            Check(!GameManager.I.AutosaveEnabled,"isolated save test cannot overwrite production save");
        }

        string ProductionScene(string id)
        {
            foreach(var entry in UnityEditor.EditorBuildSettings.scenes)
                if(Path.GetFileNameWithoutExtension(entry.path)==id)return entry.path;
            throw new InvalidOperationException("No production scene: "+id);
        }
        IEnumerator LoadProduction(string id)
        {
            if(SceneManager.GetActiveScene().path!=ProductionScene(id))
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ProductionScene(id),new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            }
            Freeze();
            Check(MapRoot.Current.mapId==id&&!MapRoot.Current.previewOnly,"production map loaded",id);
        }
        IEnumerator ProductionRun()
        {
            foreach(string id in new[]{"Naru","Gaegyeong","Dakmok","goryeo_songak","goryeo_jangdan","transit_woodtown","transit_ferry","transit_crossroads","transit_southport","transit_wetroad","transit_pass","transit_central_approach","transit_south_inland"})
            {
                yield return LoadProduction(id);
                var data=JsonUtility.FromJson<Map>(File.ReadAllText("Assets/PocketCaravan/Maps/"+id+".json"));int mismatch=0,missing=0;
                for(int y=0;y<data.h;y++)for(int x=0;x<data.w;x++)
                {
                    var p=new Vector3Int(x,data.h-y-1,0);int i=y*data.w+x;
                    if(MapRoot.Current.blocked.HasTile(p)!=(data.blocked[i]!=0))mismatch++;
                    if(MapRoot.Current.ground.HasTile(p)!=(data.ground[i]!=0))missing++;
                }
                Check(mismatch==0&&missing==0,"production ground/barriers match generated map",id);
            }
            var views=JsonUtility.FromJson<ArtPoints>(File.ReadAllText(Output+"/../asset-views.json"));
            foreach(var point in views.points)
            {
                yield return LoadProduction(point.map);Pose(point.x,point.y);yield return Capture("asset-"+point.asset);
            }
            foreach(var id in new[]{"Naru","goryeo_songak","transit_woodtown"})
            {
                GameManager.I.TravelTo(id,"S0");float start=Time.realtimeSinceStartup;
                while(GameManager.I.IsTravelling){if(Time.realtimeSinceStartup-start>60)throw new TimeoutException(id);yield return null;}
                Check(MapRoot.Current.mapId==id&&!MapRoot.Current.previewOnly,"production real travel",id);Freeze();
            }
        }
    }
}
#endif
