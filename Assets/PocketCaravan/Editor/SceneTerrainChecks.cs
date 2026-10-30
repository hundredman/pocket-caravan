using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan.EditorTools
{
    public static class SceneTerrainChecks
    {
        [Serializable] class MapFile { public string id; public int w,h,terrainX,terrainY; public string[] palette; public int[] ground,blocked,road; }
        [Serializable] public class Report { public int maps,cells; public List<string> passed=new List<string>(); }
        public static int CheckSavePositions()
        {
            int count=0;
            foreach(var map in WorldIndex.Get().maps)
            {
                var data=new SaveData{scene=map.mapId,px=4.25f,py=8.5f,money=777,layoutVersion=1};
                data.cargoItems.Add("paper");data.cargoCounts.Add(3);data.cargoCosts.Add(33);
                string before=JsonUtility.ToJson(data);
                var legacy=SaveSystem.PositionFor(data);
                int previous=map.legacyHeight>0?map.legacyHeight:map.height;
                var moved=GoryeoLayout.Find(map.mapId);   // M4 새 맵: 옛 배치 저장은 그 맵 도착 자리에서 다시 시작
                bool ok=moved!=null ? legacy==new Vector3(moved.spawnX,moved.spawnY,0) : Mathf.Abs(legacy.y-(8.5f+map.height-previous))<=.001f;
                if(!ok || before!=JsonUtility.ToJson(data))throw new Exception(map.mapId+" legacy save position/payload mismatch");
                data.mapHeight=map.height;data.layoutVersion=GoryeoLayout.Current;
                if(SaveSystem.PositionFor(data).y!=8.5f)throw new Exception(map.mapId+" modern save position mismatch");
                count++;
            }
            return count;
        }

        public static Report CheckGeneratedScenes()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop Play and save edits first");
            string original=SceneManager.GetActiveScene().path;var report=new Report();
            try
            {
                foreach(var e in WorldRegistry.Load().maps)
                {
                    var f=JsonUtility.FromJson<MapFile>(File.ReadAllText(e.jsonPath));
                    EditorSceneManager.OpenScene(e.scenePath);var root=UnityEngine.Object.FindAnyObjectByType<MapRoot>();
                    if(root==null || root.mapId!=f.id || root.terrainOrigin!=new Vector2Int(f.terrainX,f.terrainY))throw new Exception(f.id+" origin mismatch");
                    var road=root.GetComponent<RoadGrid>();
                    for(int y=0;y<f.h;y++)for(int x=0;x<f.w;x++)
                    {
                        int i=y*f.w+x;var cell=new Vector3Int(x,f.h-1-y,0);var tile=root.ground.GetTile(cell);
                        string want=f.palette[f.ground[i]];   // M4: 담당 밖 칸은 바닥 그림 없음('')
                        if((want==""?tile!=null:(tile==null || tile.name!=want)) || root.blocked.HasTile(cell)!=(f.blocked[i]!=0) || road.GradeAt(cell)!=f.road[i])throw new Exception(f.id+" scene/terrain mismatch at "+x+","+y);
                        report.cells++;
                    }
                    foreach(var sp in UnityEngine.Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
                    {
                        var cell=root.WorldToCell(sp.transform.position);
                        for(int yy=0;yy<sp.size.y;yy++)for(int xx=0;xx<sp.size.x;xx++)
                            if(!root.ground.HasTile(cell+new Vector3Int(xx,yy,0)) || root.blocked.HasTile(cell+new Vector3Int(xx,yy,0)))throw new Exception(f.id+" blocked scene spawn "+sp.id);
                    }
                    report.maps++;report.passed.Add(f.id+" all ground/blocked/road cells and spawn strips match JSON");
                }
            }
            finally{if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);}
            Directory.CreateDirectory("output/continuous-terrain");File.WriteAllText("output/continuous-terrain/scene-checks.json",JsonUtility.ToJson(report,true));return report;
        }
    }
}
