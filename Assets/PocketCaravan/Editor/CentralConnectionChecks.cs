using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan.EditorTools
{
    public static class CentralConnectionChecks
    {
        [Serializable] public class Report { public List<string> passed=new List<string>(); public string error; public bool complete; }
        public static Report Current { get; private set; }
        static bool previousBackground;
        [MenuItem("Pocket Caravan/중앙 연결/내륙 교역 길목 열기")]
        public static void OpenCentral()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Play 종료 후 수정 중인 씬을 저장하세요.");
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/PocketCaravan/Scenes/Transit/transit_crossroads.unity");
        }
        [MenuItem("Pocket Caravan/Tests/고려~중앙 출구 왕복 검사")]
        public static void RunMenu() => Begin();
        static void Check(string label,bool ok){if(!ok)throw new Exception(label);Current.passed.Add(label);}
        public static void Begin()
        {
            var gm=GameManager.I;
            if(!EditorApplication.isPlaying || gm==null || (bool)typeof(GameManager).GetField("autosave",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(gm))throw new InvalidOperationException("Initialized non-start Play required");
            if(Current!=null && !Current.complete)throw new InvalidOperationException("Check already running");
            foreach(var progress in gm.Quests.Active)throw new InvalidOperationException("Use a fresh non-start session without active quests");
            previousBackground=Application.runInBackground;Application.runInBackground=true;
            Current=new Report();Directory.CreateDirectory("output/continuous-terrain");gm.StartCoroutine(Drive(Run(gm)));
        }
        static IEnumerator Drive(IEnumerator run)
        {
            var stack=new Stack<IEnumerator>();stack.Push(run);
            while(stack.Count>0 && Current.error==null)
            {
                bool more=false;object next=null;
                try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}
                catch(Exception e){Current.error=e.ToString();}
                if(Current.error!=null)break;
                if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(next is IEnumerator nested){stack.Push(nested);continue;}
                yield return next;
            }
            while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Current.complete=true;Application.runInBackground=previousBackground;
            File.WriteAllText("output/continuous-terrain/play-checks.json",JsonUtility.ToJson(Current,true));
        }
        static IEnumerator Run(GameManager gm)
        {
            var p=gm.Player;int money=p.money,credit=p.credit;var vehicle=p.vehicle;var origin=p.origin;
            var cargo=new List<Tuple<ItemData,int,float>>();foreach(var row in p.cargo.Items)cargo.Add(Tuple.Create(row.Key,row.Value,p.cargo.AverageCost(row.Key)));
            var contracts=new List<Contract>(p.contracts);var paper=gm.ItemById("paper");
            void Freeze(Scene scene,LoadSceneMode mode){if(PlayerController.Instance!=null)PlayerController.Instance.enabled=false;UIState.Blocking=true;}
            IEnumerator Arrive(string target)
            {
                float limit=Time.realtimeSinceStartup+10;
                while(gm.IsTravelling && Time.realtimeSinceStartup<limit)yield return null;
                Check(target+" actual scene loaded",!gm.IsTravelling && MapRoot.Current!=null && MapRoot.Current.mapId==target);
            }
            IEnumerator Cross(string target,int offset,bool grass=false)
            {
                string from=MapRoot.Current.mapId;var exit=MapExit.All.Find(e=>e.targetScene==target && (grass ? e.targetSpawnId.StartsWith("seam_") : !e.targetSpawnId.StartsWith("seam_")));
                Check(from+" exit registered to "+target,exit!=null);
                string spawnId=exit.targetSpawnId;var motor=PlayerController.Instance.Motor;
                var originCell=MapRoot.Current.WorldToCell(exit.transform.position);
                bool verticalEdge=exit.edge=="E" || exit.edge=="W";
                Vector3 direction=exit.edge=="W"?Vector3.left:exit.edge=="E"?Vector3.right:exit.edge=="S"?Vector3.down:Vector3.up;
                var approach=MapRoot.Current.CellCenter(originCell)+(exit.size.x>1?Vector3.right:Vector3.up)*offset-direction*1.1f;
                if(!verticalEdge)approach.y-=motor.feetOffset.y;
                motor.Teleport(approach,Vector2Int.up);
                for(int step=0;step<45 && !exit.Contains(motor.Cell);step++){motor.Tick((Vector2)direction,3.8f,1f/30);yield return null;}
                Check(from+" -> "+target+" physically walked into "+(grass?"grass":"road")+" seam",exit.Contains(motor.Cell));
                exit.Use(motor);yield return Arrive(target);
                var spawn=SpawnPoint.Find(spawnId);motor=PlayerController.Instance.Motor;
                Check(from+" -> "+target+" offset "+offset+" preserved",spawn!=null && Vector3.Distance(motor.transform.position,spawn.PositionFor(offset))<.01f);
                Check(target+" arrival feet walkable",MapRoot.Current.IsWalkable(motor.Cell));
                Check(target+" trade assets/cargo preserved",p.money==777 && p.credit==2 && p.vehicle.id=="handcart" && p.cargo.Count(paper)==3 && Mathf.Abs(p.cargo.TotalCost(paper)-33)<.01f && p.contracts.Count==1 && p.contracts[0].id==988888);
            }
            try
            {
                p.money=777;p.credit=2;p.vehicle=gm.Db.vehicles.Find(v=>v.id=="handcart");p.origin=null;p.cargo.Clear();p.cargo.Add(paper,3,11);p.contracts.Clear();
                p.contracts.Add(new Contract{id=988888,fromTown="transit_woodtown",toTown="transit_ferry",weight=4,reward=12,deadline=gm.Clock.TotalHours+100000,finalDeadline=gm.Clock.TotalHours+150000});
                SceneManager.sceneLoaded+=Freeze;Freeze(default,LoadSceneMode.Single);
                gm.TravelTo("transit_crossroads","E");yield return Arrive("transit_crossroads");
                Check("central hub stays within the central continent",MapExit.All.TrueForAll(e=>e.targetScene.StartsWith("transit_")));
                var route=new[]{"transit_crossroads","transit_central_approach","transit_wetroad","transit_ferry","transit_crossroads","transit_ferry","transit_wetroad","transit_woodtown","transit_wetroad","transit_pass","goryeo_songak","Gaegyeong","Dakmok","goryeo_jangdan","Naru","goryeo_songak","Gaegyeong","goryeo_jangdan"};
                for(int leg=1;leg<route.Length;leg++)
                {
                    for(int offset=0;offset<3;offset++)
                    {yield return Cross(route[leg],offset);yield return Cross(route[leg-1],offset);}
                    if(MapExit.All.Exists(e=>e.targetScene==route[leg] && e.targetSpawnId.StartsWith("seam_")))
                    {yield return Cross(route[leg],0,true);yield return Cross(route[leg-1],0,true);}
                    yield return Cross(route[leg],0);
                }
            }
            finally
            {
                SceneManager.sceneLoaded-=Freeze;if(PlayerController.Instance!=null)PlayerController.Instance.enabled=true;UIState.Release();
                p.money=money;p.credit=credit;p.vehicle=vehicle;p.origin=origin;p.cargo.Clear();foreach(var row in cargo)p.cargo.Add(row.Item1,row.Item2,row.Item3);
                p.contracts.Clear();p.contracts.AddRange(contracts);Notify.Clear();
            }
        }
    }
}
