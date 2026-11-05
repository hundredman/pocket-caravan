#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
namespace PocketCaravan.EditorTools
{
    public sealed class GoryeoVillageLifeProductionReview:MonoBehaviour
    {
        const string Out="ArtReview/Goryeo-Atmosphere-A7-20261105/";
        [Serializable]class Map{public string id;public GoryeoVillageLife.Point[] points;}
        [Serializable]class Asset{public string key,path;}
        [Serializable]class Config{public string status;public Map[] maps;public Asset[] assets;}
        [Serializable]class Report{public bool complete;public string error;public List<string> checks=new List<string>();}
        readonly Report r=new Report();
        void Check(bool b,string s){if(!b)throw new Exception(s);r.checks.Add(s);}
        IEnumerator Start(){DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool n=false;object v=null;Exception e=null;try{n=stack.Peek().MoveNext();if(n)v=stack.Peek().Current;}catch(Exception x){e=x;}if(e!=null){Finish(e);yield break;}if(!n){stack.Pop();continue;}if(v is IEnumerator)stack.Push((IEnumerator)v);else yield return v;}Finish(null);}
        void Finish(Exception e){r.complete=e==null;r.error=e==null?"":e.ToString();Directory.CreateDirectory(Out+"Unity");File.WriteAllText(Out+"Unity/production.json",JsonUtility.ToJson(r,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        IEnumerator Photo(string name){yield return null;yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();Check(t.width==480&&t.height==270,"480x270 "+name);File.WriteAllBytes(Out+"Unity/production-"+name+".png",t.EncodeToPNG());Destroy(t);}
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out+"Unity");Check(!GameManager.I.AutosaveEnabled,"saves off bootstrap");Time.timeScale=0;
            var config=JsonUtility.FromJson<Config>(File.ReadAllText(Out+"source.json"));var arts=new List<GoryeoVillageLife.Art>();foreach(var a in config.assets){var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PocketCaravan/Editor/B2Preview/AtmosphereA7/"+a.key+".png");Check(sprite!=null&&sprite.pixelsPerUnit==16&&sprite.texture.filterMode==FilterMode.Point,"native whole sprite "+a.key);arts.Add(new GoryeoVillageLife.Art{key=a.key,sprite=sprite});}
            var trader=new Sprite[16];string dirs="dulr";for(int d=0;d<4;d++)for(int f=0;f<4;f++)trader[d*4+f]=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PocketCaravan/Art/Characters/NPC/npc_goryeo_songak_merchant/npc_goryeo_songak_merchant_tall_"+dirs[d]+f+".png");Check(Array.TrueForAll(trader,s=>s!=null),"approved human walker16frames");
            foreach(var row in config.maps)
            {
                var id=row.id;string folder=id.StartsWith("transit_",StringComparison.Ordinal)?"Transit":"Goryeo";yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Scenes/"+folder+"/"+id+".unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));for(int i=0;i<6;i++)yield return null;
                var map=MapRoot.Current;Check(map.mapId==id&&!map.previewOnly&&map.GetComponent<GoryeoVillageLife>()!=null,"approved production A7 install "+id);
                foreach(var g in UnityEngine.Object.FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None)){g.StopAllCoroutines();g.enabled=false;}foreach(var g in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None)){g.StopAllCoroutines();g.enabled=false;}
                foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);UIState.Blocking=false;Notify.Clear();var player=PlayerController.Instance;player.enabled=false;
                int money=GameManager.I.Player.money;string cargo=JsonUtility.ToJson(GameManager.I.Player.cargo);var road=map.GetComponent<RoadGrid>();int roadHash=0;foreach(int g in road.grades)roadHash=unchecked(roadHash*31+g);int interactables=Interactable.All.Count;
                GameManager.I.Clock.minuteOfDay=450;for(int d=1;d<1000;d++)if(GoryeoAtmosphere.DayWeather(d)==GoryeoAtmosphere.Kind.Clear){GameManager.I.Clock.day=d;break;}map.GetComponent<GoryeoAtmosphere>().Refresh();
                bool capture=id=="Naru"||id=="Dakmok"||id=="goryeo_jangdan"||id=="transit_woodtown";
                GoryeoVillageLife.Point focus=Array.Find(row.points,p=>p.kind=="chicken"||p.kind=="dog")??row.points[0];
                if(id=="transit_woodtown")focus=Array.Find(row.points,p=>p.kind=="walker");
                var pos=map.CellCenter(Vector3Int.zero)+new Vector3(focus.x-.5f,focus.y-.5f,0);player.Motor.Teleport(pos+Vector3.up*2-(Vector3)player.Motor.feetOffset,Vector2Int.down);
                var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;camera.orthographicSize=8.4375f;camera.transform.position=pos+new Vector3(0,1,-10);

                var extra=Array.Find(row.points,p=>p.kind==(id=="Naru"?"boat":"bird"));
                var mainView=camera.transform.position;var extraView=extra==null?mainView:map.CellCenter(Vector3Int.zero)+new Vector3(extra.x-.5f,extra.y+.5f,-10);

                var life=map.GetComponent<GoryeoVillageLife>();Check(life.approvedForProduction,"approval gate "+id);GoryeoVillageLifeDirector.Apply();Check(map.GetComponents<GoryeoVillageLife>().Length==1,"no duplicate install "+id);life.Tick(2);
                foreach(var asset in life.art){var expected=arts.Find(a=>a.key==asset.key).sprite;Check(asset.sprite.rect.size==expected.rect.size&&asset.sprite.pivot==expected.pivot&&asset.sprite.pixelsPerUnit==expected.pixelsPerUnit,"approved art import geometry "+id+" "+asset.key);}
                for(int n=0;n<16;n++)Check(life.traderFrames[n].pivot==trader[n].pivot&&life.traderFrames[n].pixelsPerUnit==trader[n].pixelsPerUnit,"approved trader foot pivot "+id+" "+n);
                Check(life.RendererCount>0&&life.RendererCount<=64,"bounded renderer count "+id+" "+life.RendererCount);Check(life.Root.GetComponentsInChildren<Collider2D>(true).Length==0&&life.Root.GetComponentsInChildren<Collider>(true).Length==0,"no added collision "+id);Check(interactables==Interactable.All.Count,"visual Walker not registered as gameplay interaction "+id);
                foreach(var walker in life.Root.GetComponentsInChildren<GoryeoVisualWalker>())Check(!walker.blocksMovement&&walker.path.Count==2,"reused Walker data, cosmetic only "+id);
                if(capture){yield return Photo(id+"-after");
                    if(id=="Naru"||id=="goryeo_jangdan"){camera.transform.position=extraView;life.Tick(2);yield return Photo(id+"-detail-after");camera.transform.position=mainView;}
GameManager.I.Clock.minuteOfDay=1260;life.Tick(3);yield return Photo(id+"-night");foreach(var lamp in life.Root.GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>())Check(Array.IndexOf(lamp.targetSortingLayers,SortingLayer.NameToID("Default"))>=0&&lamp.intensity>.1f,"night lamp lights actual ground layer "+id);GameManager.I.Clock.minuteOfDay=450;}
                var walkers=life.Root.GetComponentsInChildren<GoryeoVisualWalker>();if(walkers.Length>0&&capture){life.Tick(4);var before=walkers[0].transform.position;Time.timeScale=1;yield return new WaitForSeconds(.5f);Time.timeScale=0;Check(Vector3.Distance(before,walkers[0].transform.position)>.05f,"actual cosmetic walker moves "+id);}
                var sprites=life.Root.GetComponentsInChildren<SpriteRenderer>(true);life.Tick(10);var first=new Vector3[sprites.Length];for(int n=0;n<sprites.Length;n++)first[n]=sprites[n].transform.position;life.Tick(12);int moving=0;for(int n=0;n<sprites.Length;n++){if(first[n]!=sprites[n].transform.position)moving++;Check(sprites[n].transform.localScale==Vector3.one,"native unscaled sprite "+id+" "+n);}Check(moving>0,"life motion changes over time "+id);
                string random=JsonUtility.ToJson(UnityEngine.Random.state);for(int n=0;n<60;n++)life.Tick(n*.1f);Check(random==JsonUtility.ToJson(UnityEngine.Random.state)&&life.Root.GetComponentsInChildren<SpriteRenderer>(true).Length==sprites.Length,"steady pool and global random unchanged "+id);
                var oldCamera=camera.transform.position;camera.transform.position=new Vector3(-1000,-1000,-10);yield return new WaitForEndOfFrame();life.Tick(5);Check(life.VisibleCount==0,"offscreen visuals culled "+id);camera.transform.position=oldCamera;
                life.enabled=false;yield return null;yield return null;Check(life.Root==null,"owned visuals cleaned on disable "+id);life.enabled=true;life.Tick(6);Check(life.RendererCount<=64&&life.Root!=null,"bounded rebuild on enable "+id);
                int afterHash=0;foreach(int g in road.grades)afterHash=unchecked(afterHash*31+g);Check(roadHash==afterHash&&money==GameManager.I.Player.money&&cargo==JsonUtility.ToJson(GameManager.I.Player.cargo),"road money/cargo unchanged "+id);Check(!GameManager.I.AutosaveEnabled,"saves off "+id);
            }
            foreach(var path in new[]{"Venice/venice_rialto","Arabia/arabia_ahsa","North/north_birka"})
            {
                yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Scenes/"+path+".unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));for(int n=0;n<8;n++)yield return null;
                Check(GoryeoTravelTransition.RegionOf(MapRoot.Current.mapId)==null&&MapRoot.Current.GetComponent<GoryeoVillageLife>()==null&&MapRoot.Current.GetComponent<GoryeoPlaceLabels>()==null,"foreign map untouched "+path);
                Check(GameManager.I.GetComponent<WorldLabels>().enabled,"common foreign labels enabled "+path);Check(!GameManager.I.AutosaveEnabled,"foreign scene saves off "+path);
            }
        }
    }
}
#endif
