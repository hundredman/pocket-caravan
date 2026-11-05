#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
namespace PocketCaravan.EditorTools
{
    public sealed class GoryeoGroundSpeedPlayReview:MonoBehaviour
    {
        [Serializable]class Report{public bool complete;public string error;public int comparisons;public List<string> checks=new List<string>();}
        readonly Report r=new Report();
        void Check(bool ok,string message){if(!ok)throw new Exception(message);r.checks.Add(message);}
        IEnumerator Start(){DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool next=false;object v=null;Exception error=null;try{next=stack.Peek().MoveNext();if(next)v=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){Finish(error);yield break;}if(!next){stack.Pop();continue;}if(v is IEnumerator)stack.Push((IEnumerator)v);else yield return v;}Finish(null);}
        void Finish(Exception e){r.complete=e==null;r.error=e==null?"":e.ToString();Directory.CreateDirectory("Docs/Validation/Goryeo-A6-2026-11-05");File.WriteAllText("Docs/Validation/Goryeo-A6-2026-11-05/play.json",JsonUtility.ToJson(r,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        IEnumerator Load(string path){yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Scenes/"+path+".unity",new LoadSceneParameters(LoadSceneMode.Single));for(int n=0;n<8;n++)yield return null;PlayerController.Instance.enabled=false;}
        float Baseline(PlayerController pc){var p=GameManager.I.Player;int grade=RoadGrid.Current.GradeAt(pc.Motor.Cell);float s=p.vehicle!=null?p.vehicle.SpeedOn(grade):1;if(p.origin!=null){s*=p.origin.speedMultiplier;if(grade==0)s*=p.origin.offroadSpeedMultiplier;}s*=Mathf.Lerp(1,pc.fullLoadSpeed,p.LoadRatio);if(pc.Running)s*=pc.runMultiplier;if(InputReader.DebugFast())s*=PlayerController.DebugFastMultiplier;var o=MapRoot.Current.GetComponent<ObstacleField>();if(o!=null){o.Effects(pc.Motor.Cell,out float m,out _,out _,out _,out _);s*=m;}return s*pc.Motor.walkSpeed;}
        bool Neutral(Vector3Int cell){var o=MapRoot.Current.GetComponent<ObstacleField>();if(o==null)return true;o.Effects(cell,out float m,out _,out _,out _,out _);return Mathf.Approximately(m,1);}
        Vector3Int Find(GoryeoStride stride,GoryeoStride.Surface surface){var map=MapRoot.Current;for(int y=0;y<stride.height;y++)for(int x=0;x<stride.width;x++){var c=new Vector3Int(x,y,0);if(stride.At(c)==surface&&map.IsWalkable(c)&&Neutral(c))return c;}throw new Exception("no safe "+surface+" "+map.mapId);}
        void At(PlayerController pc,Vector3Int cell)=>pc.Motor.Teleport(MapRoot.Current.CellCenter(cell)-(Vector3)pc.Motor.feetOffset,Vector2Int.right);
        IEnumerator Run()
        {
            Check(!GameManager.I.AutosaveEnabled,"bootstrap saves off");Time.timeScale=0;
            var origins=new[]{AssetDatabase.LoadAssetAtPath<OriginData>("Assets/PocketCaravan/Data/Goryeo/Origin_Goryeo.asset"),AssetDatabase.LoadAssetAtPath<OriginData>("Assets/PocketCaravan/Data/Origins/Origin_Viking.asset"),AssetDatabase.LoadAssetAtPath<OriginData>("Assets/PocketCaravan/Data/Origins/Origin_Venice.asset"),AssetDatabase.LoadAssetAtPath<OriginData>("Assets/PocketCaravan/Data/Origins/Origin_Arabia.asset")};
            var vehicles=new[]{AssetDatabase.LoadAssetAtPath<VehicleData>("Assets/PocketCaravan/Data/Goryeo/Vehicle_Walk.asset"),AssetDatabase.LoadAssetAtPath<VehicleData>("Assets/PocketCaravan/Data/Goryeo/Vehicle_Handcart.asset"),AssetDatabase.LoadAssetAtPath<VehicleData>("Assets/PocketCaravan/Data/North/Vehicle_NorthSled.asset")};
            foreach(var origin in origins)Check(origin!=null,"loaded origin");foreach(var vehicle in vehicles)Check(vehicle!=null,"loaded vehicle");
            int money=GameManager.I.Player.money;string cargo=JsonUtility.ToJson(GameManager.I.Player.cargo);var oldOrigin=GameManager.I.Player.origin;var oldVehicle=GameManager.I.Player.vehicle;
            foreach(var origin in origins)foreach(var vehicle in vehicles)foreach(var kind in new[]{GoryeoAtmosphere.Kind.Clear,GoryeoAtmosphere.Kind.Mist,GoryeoAtmosphere.Kind.Rain,GoryeoAtmosphere.Kind.Monsoon})for(int grade=1;grade<=3;grade++)foreach(float shared in new[]{.7f,1f,1.6f,1.12f})
            {
                float offroad=vehicle.SpeedOn(0)*origin.speedMultiplier*origin.offroadSpeedMultiplier;float road=vehicle.SpeedOn(grade)*origin.speedMultiplier;
                float grass=GoryeoGroundSpeed.Adjust(offroad,offroad,GoryeoStride.Surface.Grass,kind)*shared;float dirt=GoryeoGroundSpeed.Adjust(road,offroad,GoryeoStride.Surface.Dirt,kind)*shared;
                if(dirt<grass*1.04999f)throw new Exception("ratio reversed "+origin.id+" "+vehicle.id+" "+kind);r.comparisons++;
            }
            Check(r.comparisons==576,"all 4 origins x3 vehicles x4 weather x3 grades x4 shared load/run factors retain >=5% road advantage");
            foreach(var surface in new[]{GoryeoStride.Surface.Stone,GoryeoStride.Surface.Wood,GoryeoStride.Surface.Other})Check(GoryeoGroundSpeed.Adjust(1.2f,2,surface,GoryeoAtmosphere.Kind.Monsoon)==1.2f,"non dirt/grass unchanged "+surface);
            foreach(var id in new[]{"Naru","Gaegyeong","Dakmok","goryeo_songak","goryeo_jangdan","transit_woodtown","transit_ferry","transit_southport","transit_crossroads","transit_wetroad","transit_pass","transit_central_approach","transit_south_inland"})
            {
                yield return Load((id.StartsWith("transit_")?"Transit/":"Goryeo/")+id);var map=MapRoot.Current;var stride=map.GetComponent<GoryeoStride>();var weather=map.GetComponent<GoryeoAtmosphere>();var pc=PlayerController.Instance;
                Check(!map.previewOnly&&stride!=null&&weather!=null,"actual production map "+id);var dirt=Find(stride,GoryeoStride.Surface.Dirt);var grass=Find(stride,GoryeoStride.Surface.Grass);
                foreach(var kind in new[]{GoryeoAtmosphere.Kind.Clear,GoryeoAtmosphere.Kind.Rain,GoryeoAtmosphere.Kind.Monsoon})
                {
                    for(int d=1;d<1000;d++)if(GoryeoAtmosphere.DayWeather(d,weather.settings)==kind){GameManager.I.Clock.day=d;break;}GameManager.I.Clock.minuteOfDay=720;weather.Refresh();
                    foreach(var origin in origins)foreach(var vehicle in vehicles)
                    {
                        GameManager.I.Player.origin=origin;GameManager.I.Player.vehicle=vehicle;At(pc,grass);float g=pc.CurrentSpeed();At(pc,dirt);float road=pc.CurrentSpeed();
                        if(road<g*1.04999f)throw new Exception("actual ratio "+id+" "+origin.id+" "+vehicle.id+" "+kind);r.comparisons++;
                    }
                    Check(weather.Shown==kind,"actual weather/road advantage "+id+" "+kind);
                }
                GameManager.I.Player.origin=origins[0];GameManager.I.Player.vehicle=vehicles[0];
                if(id=="Naru")
                {
                    float[] moved=new float[2];int index=0;
                    foreach(var surface in new[]{GoryeoStride.Surface.Dirt,GoryeoStride.Surface.Grass})
                    {
                        Vector3Int start=Vector3Int.zero;bool found=false;
                        for(int y=1;y<stride.height-1&&!found;y++)for(int x=1;x<stride.width-6&&!found;x++)
                        {bool ok=true;for(int dx=0;dx<6;dx++)for(int dy=-1;dy<=1;dy++){var c=new Vector3Int(x+dx,y+dy,0);if(!map.IsWalkable(c)||stride.At(c)!=surface||!Neutral(c))ok=false;}if(ok){start=new Vector3Int(x,y,0);found=true;}}
                        Check(found,"safe real motor strip "+surface);At(pc,start);var from=pc.Motor.Feet;
                        for(int tick=0;tick<30;tick++)pc.Motor.Tick(Vector2.right,pc.CurrentSpeed(),1f/60);
                        moved[index++]=Vector2.Distance(from,pc.Motor.Feet);Check(moved[index-1]>.5f,"actual motor moved "+surface);
                    }
                    Check(moved[0]>moved[1],"same 0.5s real motor on dirt travels farther than grass in monsoon");
                }
                At(pc,dirt);
                weather.enabled=false;Check(Mathf.Approximately(pc.CurrentSpeed(),Baseline(pc)),"disabled weather restores original "+id);weather.enabled=true;
                stride.enabled=false;Check(Mathf.Approximately(pc.CurrentSpeed(),Baseline(pc)),"disabled surface restores original "+id);stride.enabled=true;
                Check(!GameManager.I.AutosaveEnabled,"saves off "+id);
            }
            foreach(var path in new[]{"Venice/venice_rialto","Arabia/arabia_ahsa","North/north_birka"}){yield return Load(path);foreach(var origin in origins)foreach(var vehicle in vehicles){GameManager.I.Player.origin=origin;GameManager.I.Player.vehicle=vehicle;if(!Mathf.Approximately(PlayerController.Instance.CurrentSpeed(),Baseline(PlayerController.Instance)))throw new Exception("foreign changed "+path);}Check(true,"foreign exact old speed "+path);}
            GameManager.I.Player.origin=oldOrigin;GameManager.I.Player.vehicle=oldVehicle;
            Check(GameManager.I.Player.money==money&&cargo==JsonUtility.ToJson(GameManager.I.Player.cargo),"money/cargo untouched");Check(!GameManager.I.AutosaveEnabled,"final saves off");
        }
    }
}
#endif
