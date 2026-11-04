#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace PocketCaravan.EditorTools
{
    public sealed class SpecialtyMarketProductionPlayReview:MonoBehaviour
    {
        const string Out="Docs/Validation/SpecialtyMarkets-Production-Task5-2026-11-04";
        [Serializable]class Result{public bool complete;public string error;public List<string> checks=new List<string>();}
        readonly Result report=new Result();void Check(bool b,string s){if(!b)throw new Exception(s);report.checks.Add(s);}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool n=false;object v=null;Exception error=null;try{n=stack.Peek().MoveNext();if(n)v=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){Finish(error);yield break;}if(!n){stack.Pop();continue;}if(v is IEnumerator)stack.Push((IEnumerator)v);else yield return v;}Finish(null);
        }
        void Finish(Exception e){report.complete=e==null;report.error=e==null?"":e.ToString();Directory.CreateDirectory(Out);File.WriteAllText(Out+"/play.json",JsonUtility.ToJson(report,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        IEnumerator Photo(string n){yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();Check(t.width==960&&t.height==540,"960x540 "+n);File.WriteAllBytes(Out+"/"+n+".png",t.EncodeToPNG());Destroy(t);}
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);for(int i=0;i<10;i++)yield return null;Time.timeScale=0;Check(!GameManager.I.AutosaveEnabled,"save isolation");
            foreach(var id in new[]{"transit_southport","transit_crossroads"})
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Scenes/Transit/"+id+".unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
                for(int i=0;i<10;i++)yield return null;var map=MapRoot.Current;var gm=GameManager.I;
                Check(!map.previewOnly&&!gm.AutosaveEnabled,"actual production with saves isolated "+id);Check(!CentralFair.IsFairMap(id),"not added to shared fair rotation "+id);
                foreach(var g in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None))g.enabled=false;
                foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);UIState.Blocking=false;Notify.Clear();
                Check(map.GetComponent<SpecialtyMarketTrialDressing>()==null,"no trial component in production");
                var targets=new List<SpriteRenderer>();
                foreach(var sr in map.GetComponentsInChildren<SpriteRenderer>())if(sr.name=="fish_rack"||sr.name=="salt_stall"||sr.name=="cloth_stall"||sr.name=="spice_stall")targets.Add(sr);
                Check(targets.Count==5,"five production specialty props "+id);
                foreach(var sr in targets)Check(sr.sprite.pixelsPerUnit==16&&sr.sprite.rect.width==48&&sr.sprite.rect.height==((sr.name=="fish_rack"||sr.name=="spice_stall")?32:48),"whole production native sprite "+sr.name);
                foreach(var space in new[]{"market","event-yard"})
                {
                    var centre=id=="transit_southport"?(space=="market"?new Vector3(33,24,-10):new Vector3(40,35,-10)):(space=="market"?new Vector3(44,41,-10):new Vector3(57,39,-10));
                    var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.orthographicSize=8.4375f;cam.transform.position=centre;
                    var desired=map.WorldToCell(centre+Vector3.down*5);desired.z=0;var cell=desired;bool found=map.IsWalkable(cell);
                    for(int r=1;r<=6&&!found;r++)for(int y=-r;y<=r&&!found;y++)for(int x=-r;x<=r&&!found;x++){var v=desired+new Vector3Int(x,y,0);if(map.IsWalkable(v)){cell=v;found=true;}}
                    Check(found,"safe review player position "+id+" "+space);var player=PlayerController.Instance;player.Motor.Teleport(map.CellCenter(cell)-(Vector3)player.Motor.feetOffset,Vector2Int.up);
                    yield return Photo(id+"-"+space);
                }

                Check(!gm.AutosaveEnabled,"no saves enabled "+id);
            }
        }
    }
}
#endif
