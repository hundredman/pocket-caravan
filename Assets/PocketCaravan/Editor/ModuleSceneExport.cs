using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PocketCaravan.EditorTools
{
    public static class ModuleSceneExport
    {
        [Serializable] class Spawn { public string kind="spawn",id="S0",edge="W";public int x,y,w=1,h=1; }
        [Serializable] class Data
        {
            public string id,name;public int w,h,hours;public string[] palette;
            public int[] ground,deco,obj,high,road,blocked;public Spawn[] objects;
        }
        [MenuItem("Pocket Caravan/맵 제작/편집한 구역을 JSON으로 내보내기…")]
        public static void ExportDialog()
        {
            string p=EditorUtility.SaveFilePanelInProject("구역 JSON","CustomModule","json","원형의 generated 파일 밖에 저장하세요.","Assets/PocketCaravan/MapModules");
            if(!string.IsNullOrEmpty(p))Export(p,Path.GetFileNameWithoutExtension(p));
        }
        public static void Export(string path,string id)
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Play 종료 후 원본 타일로 내보내세요.");
            var map=UnityEngine.Object.FindFirstObjectByType<MapRoot>();
            if(map==null || !map.previewOnly)throw new InvalidOperationException("독립 제작 시험장/복제 씬에서 실행하세요.");
            if(UnityEngine.Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None).Length>0 || UnityEngine.Object.FindObjectsByType<MapExit>(FindObjectsSortMode.None).Length>0)
                throw new InvalidOperationException("이 도구는 구역 지형만 내보냅니다. NPC/출구가 있는 본편 씬에서는 사용할 수 없습니다.");
            var road=map.GetComponent<RoadGrid>();var appearance=map.GetComponent<MapAppearance>();
            int w=road.width,h=road.height;var palette=new List<string>{""};var indices=new Dictionary<string,int>{{"",0}};
            int[] Layer(Tilemap layer)
            {
                var result=new int[w*h];
                for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                {
                    var tile=layer.GetTile(new Vector3Int(x,h-1-y,0));if(tile==null)continue;
                    string expected="Assets/PocketCaravan/Art/NinjaAdventure/TileAssets/"+tile.name+".asset";
                    if(AssetDatabase.GetAssetPath(tile)!=expected)throw new InvalidOperationException("라이브러리 기본 타일만 내보낼 수 있습니다. 새 그림은 EnvironmentSkin Overrides로 연결하세요: "+tile.name);
                    if(!indices.TryGetValue(tile.name,out int value)){value=palette.Count;indices[tile.name]=value;palette.Add(tile.name);}result[y*w+x]=value;
                }
                return result;
            }
            var f=new Data{id=id,name=map.displayName,w=w,h=h,ground=Layer(appearance.visualLayers[0]),deco=Layer(appearance.visualLayers[1]),obj=Layer(appearance.visualLayers[2]),high=Layer(appearance.visualLayers[3]),road=new int[w*h],blocked=new int[w*h]};
            f.palette=palette.ToArray();
            for(int y=0;y<h;y++)for(int x=0;x<w;x++){var cell=new Vector3Int(x,h-1-y,0);f.road[y*w+x]=road.GradeAt(cell);f.blocked[y*w+x]=map.blocked.HasTile(cell)?1:0;}
            int spawn=-1;for(int i=0;i<w*h;i++)if(f.ground[i]>0&&f.blocked[i]==0&&f.road[i]>0){spawn=i;break;}
            if(spawn<0)throw new InvalidOperationException("통행 가능한 길이 없습니다.");
            f.objects=new[]{new Spawn{x=spawn%w,y=spawn/w}};
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(f));AssetDatabase.Refresh();
        }
    }
}
