#if UNITY_EDITOR
using System;using System.Collections;using System.Collections.Generic;using System.IO;using System.Reflection;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;
namespace PocketCaravan.EditorTools
{
    /// <summary>Temporary art capture only. No schedules, markets, save changes or production registration.</summary>
    public sealed class E3ArtPlayReview:MonoBehaviour
    {
        [Serializable] public class Row{public string test,detail;public bool passed;}
        [Serializable] public class Report{public bool complete;public string error;public List<Row> checks=new List<Row>();}
        const string Root="Assets/PocketCaravan/Editor/B2Preview/E3Art";const string Output="ArtReview/E3-E2-Art-20261103";readonly Report report=new Report();string Out=Output+"/Unity";bool icons;
        Texture2D spice,wool;Texture2D oldGrain,oldSalt;
        void Check(bool yes,string test,string detail=""){report.checks.Add(new Row{test=test,detail=detail,passed=yes});if(!yes)throw new Exception(test+" "+detail);}
        void Save(){Directory.CreateDirectory(Out);File.WriteAllText(Out+"/checks.json",JsonUtility.ToJson(report,true));}
        void OnEnable(){SceneManager.sceneLoaded+=Isolate;}
        void OnDisable(){SceneManager.sceneLoaded-=Isolate;}
        static void Isolate(Scene s,LoadSceneMode m){foreach(var p in UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))p.enabled=false;}
        IEnumerator Start(){var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool next=false;object item=null;Exception error=null;try{next=stack.Peek().MoveNext();if(next)item=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){Finish(error);yield break;}if(!next){stack.Pop();continue;}if(item is IEnumerator)stack.Push((IEnumerator)item);else yield return item;}Finish(null);}
        void Finish(Exception e){report.complete=e==null;report.error=e==null?null:e.ToString();Save();Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        IEnumerator Load(string name){yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Root+"/Scenes/"+name+".unity",new LoadSceneParameters(LoadSceneMode.Single));for(int i=0;i<6;i++)yield return null;Time.timeScale=0;foreach(var pos in MapRoot.Current.ground.cellBounds.allPositionsWithin){var tile=MapRoot.Current.ground.GetTile<UnityEngine.Tilemaps.Tile>(pos);if(tile!=null){Check(tile.sprite!=null,"ground sprite exists",name);Check(tile.sprite.pixelsPerUnit==16,"ground sprite PPU16",name);break;}}Check(MapRoot.Current.previewOnly,"preview only",name);Check(!GameManager.I.AutosaveEnabled,"save disabled",name);}
        void Pose(float x,float y)
        {
            GameManager.I.Clock.day=1;GameManager.I.Clock.minuteOfDay=720;var p=PlayerController.Instance;p.enabled=false;var map=MapRoot.Current;Vector3 safe;var desired=map.CellCenter(new Vector3Int(Mathf.RoundToInt(x)-3,Mathf.RoundToInt(y),0))-(Vector3)p.Motor.feetOffset;
            if(map.TryResolveSavedPosition(desired,p.Motor.feetOffset,out safe))p.Motor.Teleport(safe,Vector2Int.down);
            var c=Camera.main;c.GetComponent<CameraFollow>().enabled=false;c.orthographicSize=8.4375f;c.transform.position=new Vector3(x,y,-10);
            foreach(var i in UnityEngine.Object.FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None)){i.StopAllCoroutines();i.enabled=false;}
            foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None)){typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);d.enabled=false;}
            foreach(var h in UnityEngine.Object.FindObjectsByType<HUD>(FindObjectsSortMode.None))h.enabled=false;foreach(var q in UnityEngine.Object.FindObjectsByType<QuestTracker>(FindObjectsSortMode.None))q.enabled=false;foreach(var l in UnityEngine.Object.FindObjectsByType<WorldLabels>(FindObjectsSortMode.None))l.enabled=false;Notify.Clear();
        }
        IEnumerator Photo(string name)
        {
            Texture2D im=null;for(int n=0;n<20;n++){yield return null;yield return new WaitForEndOfFrame();im=ScreenCapture.CaptureScreenshotAsTexture();if(new HashSet<Color32>(im.GetPixels32()).Count>128)break;Destroy(im);im=null;}
            Check(im!=null,"render complete",name);Check(im.width==480&&im.height==270,"native480x270",name);File.WriteAllBytes(Out+"/"+name+".png",im.EncodeToPNG());Destroy(im);Save();
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);for(int i=0;i<8;i++)yield return null;Check(!GameManager.I.AutosaveEnabled,"safe bootstrap");
            yield return Load("trial");Pose(15,8.5f);
            var map=MapRoot.Current;var log=map.transform.Find("Log candidate").gameObject;var high=map.transform.Find("Flood high candidate").gameObject;var low=map.transform.Find("Flood low candidate").gameObject;
            foreach(var sr in map.GetComponentsInChildren<SpriteRenderer>(true)){Check(sr.sprite!=null,"sprite exists",sr.name);Check(sr.sprite.pixelsPerUnit==16&&sr.transform.lossyScale==Vector3.one,"native scale",sr.name);}
            yield return Photo("log");log.SetActive(false);yield return Photo("river-normal");high.SetActive(true);yield return Photo("flood-high");high.SetActive(false);low.SetActive(true);yield return Photo("flood-low");low.SetActive(false);
            spice=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Whole/transit_spice.png");wool=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Whole/transit_wool.png");oldGrain=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PocketCaravan/Art/Items/grain.png");oldSalt=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PocketCaravan/Art/Items/salt.png");icons=true;yield return Photo("item-ui");icons=false;
            foreach(var version in new[]{"before","after"})
            {
                yield return Load("southport-"+version);Pose(45,12);yield return Photo("southport-"+version);
                yield return Load("ferry-"+version);Pose(64,26);foreach(var anim in UnityEngine.Object.FindObjectsByType<SpriteAnim>(FindObjectsSortMode.None)){anim.enabled=false;if(anim.frames.Length>0)anim.GetComponent<SpriteRenderer>().sprite=anim.frames[0];}yield return Photo("ferry-"+version);
            }
        }
        void OnGUI()
        {
            if(!icons)return;GUI.depth=-100;GUI.Box(new Rect(30,40,420,130),"상품 아이콘 비교 · 기존 / 새 시안");var images=new[]{oldGrain,oldSalt,spice,wool};var labels=new[]{"곡식","소금","향신료","양모"};
            for(int i=0;i<4;i++){GUI.DrawTexture(new Rect(60+i*95,75,32,32),images[i],ScaleMode.ScaleToFit,true);GUI.Label(new Rect(48+i*95,112,85,24),labels[i]);}
        }
    }
}
#endif
