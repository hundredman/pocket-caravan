#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
namespace PocketCaravan.EditorTools
{
    public sealed class E3FloodTerrainPlayReview:MonoBehaviour
    {
        [Serializable] public class Result{public bool complete;public string error;public List<string> checks=new List<string>();}
        const string Out="ArtReview/E3-FloodTerrain-20261103/Unity";
        readonly Result result=new Result();
        void Check(bool value,string text){if(!value)throw new Exception(text);result.checks.Add(text);}
        void Save(){Directory.CreateDirectory(Out);File.WriteAllText(Out+"/checks.json",JsonUtility.ToJson(result,true));}
        IEnumerator Start()
        {
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0){bool next=false;object item=null;Exception error=null;try{next=stack.Peek().MoveNext();if(next)item=stack.Peek().Current;}catch(Exception e){error=e;}
                if(error!=null){Finish(error);yield break;}if(!next){stack.Pop();continue;}if(item is IEnumerator)stack.Push((IEnumerator)item);else yield return item;}
            Finish(null);
        }
        void Finish(Exception error){result.complete=error==null;result.error=error==null?"":error.ToString();Save();Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        IEnumerator Photo(string name)
        {
            Texture2D im=null;for(int n=0;n<20;n++){yield return null;yield return new WaitForEndOfFrame();im=ScreenCapture.CaptureScreenshotAsTexture();if(new HashSet<Color32>(im.GetPixels32()).Count>128)break;Destroy(im);im=null;}
            Check(im!=null,"render "+name);Check(im.width==480&&im.height==270,"480x270 "+name);File.WriteAllBytes(Out+"/"+name+".png",im.EncodeToPNG());Destroy(im);
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);
            for(int i=0;i<10;i++)yield return null;
            var map=MapRoot.Current;var view=map.GetComponent<E3FloodTerrainPreview>();var player=PlayerController.Instance;var motor=player.Motor;var road=map.GetComponent<RoadGrid>();var deco=map.transform.Find("Deco").GetComponent<Tilemap>();
            Check(map.previewOnly,"previewOnly");Check(!GameManager.I.AutosaveEnabled,"autosave off");Check(view!=null,"terrain state component");
            player.enabled=false;Time.timeScale=0;GameManager.I.Clock.minuteOfDay=720;GameManager.I.Clock.day=1;view.showControls=false;
            foreach(var i in UnityEngine.Object.FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None)){i.StopAllCoroutines();i.enabled=false;}
            foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None)){typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);d.enabled=false;}
            foreach(var h in UnityEngine.Object.FindObjectsByType<HUD>(FindObjectsSortMode.None))h.enabled=false;foreach(var q in UnityEngine.Object.FindObjectsByType<QuestTracker>(FindObjectsSortMode.None))q.enabled=false;foreach(var l in UnityEngine.Object.FindObjectsByType<WorldLabels>(FindObjectsSortMode.None))l.enabled=false;Notify.Clear();
            var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;camera.orthographicSize=8.4375f;camera.transform.position=new Vector3(47.5f,45.5f,-10);
            var safe=map.CellCenter(new Vector3Int(44,51,0))-(Vector3)motor.feetOffset;motor.Teleport(safe,Vector2Int.down);
            int terrainTiles=0;foreach(var c in map.ground.cellBounds.allPositionsWithin){var t=map.ground.GetTile<Tile>(c);if(t==null)continue;terrainTiles++;if(t.sprite==null||t.sprite.pixelsPerUnit!=16)throw new Exception("Invalid terrain sprite at "+c);}
            Check(terrainTiles>0,"ground sprites exist at PPU16");
            Check(!view.Flooded,"starts dry");yield return Photo("normal");
            Check(view.SetFlooded(true),"activate from dry bank");Check(view.Flooded,"flood state active");int changedRoad=0;
            foreach(var c in view.cells)
            {
                Check(map.ground.GetTile(c.position)==c.wetGround,"wet tile "+c.position);
                Check(map.blocked.GetTile(c.position)==c.wetBlocked,"wet collision "+c.position);
                if(c.dryGrade>0&&c.wetGrade==0){changedRoad++;Check(!map.IsWalkable(c.position),"submerged road blocked "+c.position);}
            }
            Check(changedRoad==22,"22 road cells submerged");yield return Photo("flood");
            motor.Teleport(map.CellCenter(new Vector3Int(44,49,0))-(Vector3)motor.feetOffset,Vector2Int.down);
            for(int i=0;i<60;i++)motor.Tick(Vector2.down,3.8f,1f/60);
            Check(motor.Cell.y>=49,"PlayerMotor cannot cross flooded road");
            Check(view.SetFlooded(false),"recede");
            foreach(var c in view.cells)
            {
                Check(map.ground.GetTile(c.position)==c.dryGround,"restore ground "+c.position);Check(deco.GetTile(c.position)==c.dryDeco,"restore deco "+c.position);
                Check(map.blocked.GetTile(c.position)==c.dryBlocked,"restore collision "+c.position);Check(road.GradeAt(c.position)==c.dryGrade,"restore road grade "+c.position);
            }
            motor.Teleport(safe,Vector2Int.down);yield return Photo("restored");
            motor.Teleport(map.CellCenter(new Vector3Int(44,49,0))-(Vector3)motor.feetOffset,Vector2Int.down);
            for(int i=0;i<60;i++)motor.Tick(Vector2.down,3.8f,1f/60);
            Check(motor.Cell.y<=47,"PlayerMotor crosses restored road");
            motor.Teleport(map.CellCenter(new Vector3Int(44,48,0))-(Vector3)motor.feetOffset,Vector2Int.down);var position=motor.transform.position;
            Check(!view.SetFlooded(true),"defer flood while player on affected road");Check(!view.Flooded&&position==motor.transform.position,"no trapping or teleport");
            motor.Teleport(safe,Vector2Int.down);Check(view.SetFlooded(true)&&view.SetFlooded(false),"repeat flood and recede");
            Check(!GameManager.I.AutosaveEnabled,"autosave stays off");
        }
    }
}
#endif
