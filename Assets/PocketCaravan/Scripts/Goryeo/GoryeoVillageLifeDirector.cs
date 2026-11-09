using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace PocketCaravan
{
    public static class GoryeoVillageLifeDirector
    {
        [Serializable]public class Map{public string id;public int w,h;public GoryeoVillageLife.Point[] points;public Nudge[] nudge;}
        [Serializable]public class Nudge{public string kind;public float x,y,dy;}   // 11/08 Extra.json: 좌판을 한 칸 올린 자리와 겹치는 원본 점을 dy만큼 옮긴다(원본 파일은 그대로)
        [Serializable]public class Asset{public string key,path;}
        [Serializable]public class Config{public string status;public Map[] maps;public Asset[] assets;}
        static Config config,extra;static GoryeoVillageLife.Art[] art;static Sprite[] trader;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){config=null;extra=null;art=null;trader=null;SceneManager.sceneLoaded-=OnLoaded;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){SceneManager.sceneLoaded-=OnLoaded;SceneManager.sceneLoaded+=OnLoaded;Apply();}
        static void OnLoaded(Scene scene,LoadSceneMode mode)=>Apply();
        public static void Apply()
        {
            var map=MapRoot.Current;if(map==null||map.previewOnly||GoryeoTravelTransition.RegionOf(map.mapId)==null)return;
            if(config==null){var text=Resources.Load<TextAsset>("Goryeo/VillageLife/Anchors");if(text==null)return;config=JsonUtility.FromJson<Config>(text.text);}
            if(config==null||config.status!="approved"||config.maps==null||config.assets==null)return;
            var row=Array.Find(config.maps,v=>v.id==map.mapId);var road=map.GetComponent<RoadGrid>();
            if(row==null||row.points==null||road==null||row.w!=road.width||row.h!=road.height)return;
            if(art==null){var list=new List<GoryeoVillageLife.Art>();foreach(var a in config.assets){var sprite=Resources.Load<Sprite>("Goryeo/VillageLife/Art/"+a.key);if(sprite==null)return;list.Add(new GoryeoVillageLife.Art{key=a.key,sprite=sprite});}
                LoadExtra();if(extra.assets!=null)foreach(var a in extra.assets){var sprite=Resources.Load<Sprite>("Goryeo/VillageLife/Art/"+a.key);if(sprite!=null)list.Add(new GoryeoVillageLife.Art{key=a.key,sprite=sprite});}   // 11/08 장독대·빨래줄
                art=list.ToArray();}
            if(trader==null){var frames=new Sprite[16];for(int i=0;i<16;i++){frames[i]=Resources.Load<Sprite>("Goryeo/VillageLife/Trader/"+i);if(frames[i]==null)return;}trader=frames;}
            var life=map.GetComponent<GoryeoVillageLife>();if(life!=null)return;
            life=map.gameObject.AddComponent<GoryeoVillageLife>();life.approvedForProduction=true;life.Configure(Merge(row),art,trader);
        }
        // 11/08 은샘: 빈 마을에 오가는 사람을 더한다. 잠긴 T3 원본(Anchors.json)은 그대로 두고 따로 둔 Extra.json을 같은 맵 크기일 때만 붙인다.
        static void LoadExtra(){if(extra==null){var text=Resources.Load<TextAsset>("Goryeo/VillageLife/Extra");extra=text!=null?JsonUtility.FromJson<Config>(text.text):new Config();}}
        static GoryeoVillageLife.Point[] Merge(Map row)
        {
            LoadExtra();
            var add=extra!=null&&extra.maps!=null?Array.Find(extra.maps,v=>v.id==row.id):null;
            if(add==null||add.points==null||add.w!=row.w||add.h!=row.h)return row.points;
            var list=new List<GoryeoVillageLife.Point>();
            foreach(var p in row.points)
            {
                var n=add.nudge!=null?Array.Find(add.nudge,v=>v.kind==p.kind&&Mathf.Approximately(v.x,p.x)&&Mathf.Approximately(v.y,p.y)):null;
                list.Add(n==null?p:new GoryeoVillageLife.Point{kind=p.kind,reason=p.reason,x=p.x,y=p.y+n.dy,bx=p.bx,by=p.by+n.dy,via=p.via});
            }
            list.AddRange(add.points);
            while(list.Count>GoryeoVillageLife.RendererLimit)list.RemoveAt(list.Count-1);
            return list.ToArray();
        }
    }
}
