using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PocketCaravan.EditorTools
{
    public static class LayoutDTerrainChecks
    {
        const string Review="ArtReview/B2-LayoutD/", Preview="Assets/PocketCaravan/Editor/B2Preview/World";
        [Serializable] class Segment {public int x,y,nx,ny;}
        [Serializable] class Map {public string id;public int w,h;public int[] owned,blocked,road;public string[] palette;public Segment[] walkSegments;}
        [Serializable] class Report {public string unity;public int scenes,groundCells,walkable,checks,motorTicks,walkedSegments;public bool productionRegistered=false;public List<string> screenshots=new List<string>();}
        public static void RunBatch()
        {
            if(Application.isPlaying || GameManager.I!=null)throw new InvalidOperationException("Use isolated Edit mode");
            var report=new Report{unity=Application.unityVersion};
            void Check(bool ok,string message){if(!ok)throw new Exception(message);report.checks++;}
            Directory.CreateDirectory(Preview+"/Tiles");Directory.CreateDirectory(Preview+"/Scenes");
            foreach(string file in Directory.GetFiles(Review+"Tiles","*.png"))File.Copy(file,Preview+"/Tiles/"+Path.GetFileName(file),true);
            AssetDatabase.Refresh();
            foreach(string file in Directory.GetFiles(Preview+"/Tiles","*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(file);importer.textureType=TextureImporterType.Sprite;
                importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=16;importer.filterMode=FilterMode.Point;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.SaveAndReimport();
            }
            foreach(string file in Directory.GetFiles(Review+"Maps","*.json").OrderBy(p=>p))
            {
                string json=File.ReadAllText(file);var source=JsonUtility.FromJson<Map>(json);
                string scenePath=Preview+"/Scenes/"+source.id+".unity";
                MapJsonBuilder.BuildSingleMap(json,scenePath,Preview);
                var map=UnityEngine.Object.FindAnyObjectByType<MapRoot>();
                Check(map.previewOnly,"Preview save protection");
                for(int i=0;i<source.owned.Length;i++)
                {
                    var cell=new Vector3Int(i%source.w,source.h-1-i/source.w,0);
                    Check(map.ground.HasTile(cell)==(source.owned[i]!=0),"Owned tile mismatch");
                    Check(map.blocked.HasTile(cell)==(source.blocked[i]!=0),"Collision mismatch");
                    if(source.owned[i]!=0)report.groundCells++;
                    if(map.IsWalkable(cell))report.walkable++;
                }
                var motor=UnityEngine.Object.FindAnyObjectByType<PlayerMotor>();
                typeof(MapRoot).GetProperty("Current").SetValue(null,map);
                foreach(var segment in source.walkSegments)
                {
                    var from=map.CellCenter(new Vector3Int(segment.x,source.h-1-segment.y,0));
                    var to=map.CellCenter(new Vector3Int(segment.nx,source.h-1-segment.ny,0));
                    motor.Teleport(from-(Vector3)motor.feetOffset,Vector2Int.down);
                    for(int tick=0;tick<45 && Vector2.Distance(motor.Feet,to)>.075f;tick++)
                    {
                        motor.Tick(((Vector2)to-motor.Feet).normalized,motor.walkSpeed,1f/60f);report.motorTicks++;
                    }
                    Check(Vector2.Distance(motor.Feet,to)<.09f,"Actual motor route blocked");report.walkedSegments++;
                }
                typeof(MapRoot).GetProperty("Current").SetValue(null,null);
                foreach(var tile in map.ground.GetTilesBlock(map.ground.cellBounds).Where(t=>t!=null).Distinct())
                {
                    var sprite=((UnityEngine.Tilemaps.Tile)tile).sprite;
                    Check(sprite!=null && sprite.rect.size==new Vector2(16,16),"Native tile size");
                    Check(sprite.pixelsPerUnit==16 && sprite.texture.filterMode==FilterMode.Point,"Native import scale/filter");
                }
                var spawn=UnityEngine.Object.FindAnyObjectByType<SpawnPoint>();
                Check(map.IsWalkable(map.WorldToCell(spawn.transform.position)),"Spawn on blocked ground");
                motor.Teleport(spawn.transform.position,Vector2Int.down);
                var camera=Camera.main;camera.transform.position=new Vector3(spawn.transform.position.x,spawn.transform.position.y,-10);
                // Native camera pictures are captured from generated terrain, with actual player art.
                if(source.id=="layoutD_goryeo_나루" || source.id=="layoutD_goryeo_닥목" || source.id=="layoutD_east_북동 길")
                {
                    string screen=Review+source.id+"-camera.png";Capture(Camera.main,screen);report.screenshots.Add(screen);
                }
                report.scenes++;
            }
            // Static unregistered mosaic only: this avoids missing-neighbor void in
            // the review camera and does not change the one-map-per-scene D5 runtime.
            MapJsonBuilder.BuildSingleMap(File.ReadAllText(Review+"review-mosaic.json"),Preview+"/Scenes/road-review-mosaic.unity",Preview);
            var mosaic=UnityEngine.Object.FindAnyObjectByType<MapRoot>();
            Check(mosaic.previewOnly,"Mosaic preview protection");
            Capture(Camera.main,Review+"natural-road-camera.png");report.screenshots.Add(Review+"natural-road-camera.png");
            var routing=JsonUtility.FromJson<TownDistance.Settings>(File.ReadAllText(Review+"layoutD-routes.json"));TownDistance.Validate(routing);
            foreach(var a in routing.towns)foreach(var b in routing.towns)Check(a.id==b.id?TownDistance.WalkingSeconds(routing,a.id,b.id)==0:TownDistance.WalkingSeconds(routing,a.id,b.id)>0,"Actual route distance table");
            EditorSceneManager.OpenScene("Assets/PocketCaravan/Scenes/Goryeo/Naru.unity");
            Capture(Camera.main,Review+"current-naru-camera.png");report.screenshots.Add(Review+"current-naru-camera.png");
            Check(report.scenes==13&&report.groundCells==45000,"Scene total");
            // Build settings and runtime registry are untouched by BuildSingleMap.
            Check(!EditorBuildSettings.scenes.Any(s=>s.path.Contains("B2Preview")),"Preview scene in build settings");
            File.WriteAllText(Review+"unity.json",JsonUtility.ToJson(report,true));
            Debug.Log("B2 terrain checks completed: "+report.checks);
        }

        static void Capture(Camera camera,string path)
        {
            var texture=new RenderTexture(480,270,24,RenderTextureFormat.ARGB32);texture.Create();
            var previous=RenderTexture.active;
            try
            {
                camera.aspect=480f/270f;
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=texture});
                RenderTexture.active=texture;
                var pixels=new Texture2D(480,270,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,480,270),0,0);pixels.Apply();
                File.WriteAllBytes(path,pixels.EncodeToPNG());UnityEngine.Object.DestroyImmediate(pixels);
            }
            finally {RenderTexture.active=previous;texture.Release();UnityEngine.Object.DestroyImmediate(texture);}
        }
    }
}
