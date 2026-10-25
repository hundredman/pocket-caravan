using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace PocketCaravan.EditorTools
{
    public static class C3ProductionChecks
    {
        [Serializable] class Data {public string id;public int w,h;public string[] palette;public int[] ground,deco,obj,high,blocked;}
        [Serializable] class Result {public int maps,cells,checks;public List<string> ids=new List<string>();}
        public static void Run()
        {
            var result=new Result();var plan=JsonUtility.FromJson<C3OcclusionReview.Plan>(File.ReadAllText("ArtReview/C3a-2026-10-26/plan.json"));
            foreach(var area in plan.maps)
            {
                var e=WorldRegistry.Load().Map(area.map);string json=File.ReadAllText(e.jsonPath);var data=JsonUtility.FromJson<Data>(json);
                MapJsonBuilder.BuildSingleMap(json,e.scenePath);
                var map=UnityEngine.Object.FindAnyObjectByType<MapRoot>();if(map.previewOnly)throw new Exception("Production preview flag");
                foreach(var pair in new[]{("Ground",data.ground,0),("Deco",data.deco,1),("Objects",data.obj,5),("Above",data.high,20)})
                {
                    var tilemap=map.transform.Find(pair.Item1).GetComponent<Tilemap>();
                    if(tilemap.GetComponent<TilemapRenderer>().sortingOrder!=pair.Item3)throw new Exception("Layer order");result.checks++;
                    for(int i=0;i<pair.Item2.Length;i++)
                    {
                        var c=new Vector3Int(i%data.w,data.h-1-i/data.w,0);var tile=tilemap.GetTile<Tile>(c);var expected=data.palette[pair.Item2[i]];
                        if((tile==null?"":tile.sprite.name)!=expected)throw new Exception(data.id+" tile mismatch "+pair.Item1+" "+i);result.checks++;
                    }
                }
                for(int i=0;i<data.blocked.Length;i++)
                {if(map.blocked.HasTile(new Vector3Int(i%data.w,data.h-1-i/data.w,0))!=(data.blocked[i]!=0))throw new Exception("Collision mismatch");result.checks++;}
                var player=UnityEngine.Object.FindAnyObjectByType<PlayerMotor>();if(player.GetComponentInChildren<SpriteRenderer>().sortingOrder!=11)throw new Exception("Player layer");
                result.maps++;result.cells+=data.w*data.h;result.ids.Add(data.id);
            }
            AssetDatabase.SaveAssets();File.WriteAllText("Docs/Validation/C3a-Occlusion-2026-10-26/unity-production.json",JsonUtility.ToJson(result,true));Debug.Log("C3 production checks "+result.checks);
        }
    }
}
