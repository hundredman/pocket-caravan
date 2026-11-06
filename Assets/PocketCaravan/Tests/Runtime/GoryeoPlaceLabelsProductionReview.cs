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
    public sealed class GoryeoPlaceLabelsProductionReview:MonoBehaviour
    {
        const string Out="ArtReview/Goryeo-Atmosphere-A5-20261105/";
        [Serializable]class Map{public string id;public GoryeoPlaceLabels.Place[] places;}
        [Serializable]class Config{public string status;public Map[] maps;}
        [Serializable]class Report{public bool complete;public string error;public List<string> checks=new List<string>();}
        readonly Report r=new Report();
        void Check(bool b,string s){if(!b)throw new Exception(s);r.checks.Add(s);}
        IEnumerator Start(){DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool n=false;object v=null;Exception e=null;try{n=stack.Peek().MoveNext();if(n)v=stack.Peek().Current;}catch(Exception x){e=x;}if(e!=null){Finish(e);yield break;}if(!n){stack.Pop();continue;}if(v is IEnumerator)stack.Push((IEnumerator)v);else yield return v;}Finish(null);}
        void Finish(Exception e){r.complete=e==null;r.error=e==null?"":e.ToString();Directory.CreateDirectory(Out+"Unity");File.WriteAllText(Out+"Unity/production.json",JsonUtility.ToJson(r,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        IEnumerator Photo(string name){yield return null;yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();Check(t.width==480&&t.height==270,"480x270 "+name);File.WriteAllBytes(Out+"Unity/production-"+name+".png",t.EncodeToPNG());Destroy(t);}
        void Pose(Vector3 at){var p=PlayerController.Instance;p.Motor.Teleport(at-(Vector3)p.Motor.feetOffset,Vector2Int.down);}
        void View(Vector3 at){var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.orthographicSize=8.4375f;cam.transform.position=at+new Vector3(0,1,-10);}
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out+"Unity");Check(!GameManager.I.AutosaveEnabled,"bootstrap saves off");Time.timeScale=0;
            var config=JsonUtility.FromJson<Config>(File.ReadAllText(Out+"source.json"));Check(config.status=="review","unapproved A5 stays in review");
            Check(GoryeoPlaceLabels.Nearby(Vector2.zero,new Vector2(4,0))&&!GoryeoPlaceLabels.Nearby(Vector2.zero,new Vector2(4.01f,0)),"four-cell threshold inclusive");
            foreach(var row in config.maps)
            {
                var id=row.id;string folder=id.StartsWith("transit_",StringComparison.Ordinal)?"Transit":"Goryeo";yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Scenes/"+folder+"/"+id+".unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));for(int i=0;i<6;i++)yield return null;
                var map=MapRoot.Current;Check(map.mapId==id&&!map.previewOnly&&map.GetComponent<GoryeoPlaceLabels>()!=null,"approved A5 production install "+id);
                foreach(var g in UnityEngine.Object.FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None)){g.StopAllCoroutines();g.enabled=false;}foreach(var g in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None)){g.StopAllCoroutines();g.enabled=false;}
                foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);UIState.Blocking=false;Notify.Clear();var player=PlayerController.Instance;player.enabled=false;
                GameManager.I.Clock.minuteOfDay=720;for(int d=1;d<1000;d++)if(GoryeoAtmosphere.DayWeather(d)==GoryeoAtmosphere.Kind.Clear){GameManager.I.Clock.day=d;break;}map.GetComponent<GoryeoAtmosphere>().Refresh();
                var common=GameManager.I.GetComponent<WorldLabels>();bool oldEnabled=map.GetComponent<GoryeoPlaceLabels>().OriginalEnabled;int money=GameManager.I.Player.money;string cargo=JsonUtility.ToJson(GameManager.I.Player.cargo);int count=Interactable.All.Count;var names=new Dictionary<Interactable,string>();foreach(var it in Interactable.All)names[it]=it.DisplayName;
                var market=UnityEngine.Object.FindFirstObjectByType<TownMarket>();var board=UnityEngine.Object.FindFirstObjectByType<ContractBoard>();var workshop=UnityEngine.Object.FindFirstObjectByType<GoryeoWorkshopSpot>();
                var spot=market!=null?(Interactable)market:board!=null?board:null;Vector3 focus=spot!=null?spot.transform.position:player.transform.position;
                if(id=="Dakmok"&&workshop!=null)focus=workshop.transform.position;
                if(id=="Gaegyeong")foreach(var it in Interactable.All)if(Inn.IsHost(it)){focus=it.transform.position;break;}
                Pose(focus+Vector3.down*2);View(focus);
                bool capture=id=="Naru"||id=="Dakmok"||id=="transit_woodtown"||id=="Gaegyeong";

                var labels=map.GetComponent<GoryeoPlaceLabels>();Check(labels.approvedForProduction,"approval gate "+id);GoryeoPlaceLabelsDirector.Apply();Check(map.GetComponents<GoryeoPlaceLabels>().Length==1,"no duplicate install "+id);Check(labels.OwnsOriginal&&!common.enabled,"single scoped label renderer "+id);
                foreach(var it in Interactable.All)
                {
                    if(GoryeoPlaceLabels.IsPlace(it)){Check(!string.IsNullOrEmpty(GoryeoPlaceLabels.LabelFor(it,it.transform.position))&&GoryeoPlaceLabels.LabelFor(it,(Vector2)it.transform.position+Vector2.right*4.1f)=="","place near/far "+id+" "+it.GetType().Name);}
                    else Check(GoryeoPlaceLabels.LabelFor(it,(Vector2)it.transform.position+Vector2.right*100)==it.DisplayName,"person or gameplay label unchanged "+id+" "+it.GetType().Name);
                }
                if(capture){yield return Photo(id+"-near");if(id=="Gaegyeong"){int inns=0;foreach(var text in labels.DrawnNames)if(text.Contains("객주"))inns++;Check(inns<=1,"inn host and building share a single label");}for(int n=0;n<labels.DrawnRects.Count;n++)for(int k=0;k<n;k++)Check(!labels.DrawnRects[n].Overlaps(labels.DrawnRects[k]),"label rectangles do not overlap "+id+" "+n+" "+k);Pose(focus+Vector3.down*6);yield return Photo(id+"-far");if(market!=null&&!GoryeoPlaceLabels.Nearby(player.Motor.Feet,market.transform.position))Check(!labels.DrawnNames.Contains(market.town.displayName+" 장터"),"far market hidden "+id);}
                if(id=="Naru")foreach(var place in row.places)if(place.text=="우물")
                {
                    var at=map.CellCenter(Vector3Int.zero)+new Vector3(place.x-.5f,place.y-.5f,0);Pose(at+Vector3.down);View(at);yield return Photo("Naru-well");Check(labels.DrawnNames.Contains("우물"),"existing well gets nearby label");break;
                }
                UIState.Blocking=true;yield return null;yield return null;Check(labels.DrawnNames.Count==0,"modal hides labels "+id);UIState.Blocking=false;
                labels.enabled=false;Check(common.enabled==oldEnabled&&!labels.OwnsOriginal,"common labels restored on disable "+id);labels.enabled=true;yield return null;Check(!common.enabled&&labels.OwnsOriginal,"re-enable without duplicate common labels "+id);
                map.previewOnly=false;yield return null;Check(!common.enabled,"approved UI active without preview permission "+id);labels.approvedForProduction=false;labels.Refresh();Check(common.enabled==oldEnabled,"loss of approval restores common UI "+id);labels.approvedForProduction=true;labels.Refresh();
                Check(count==Interactable.All.Count&&money==GameManager.I.Player.money&&cargo==JsonUtility.ToJson(GameManager.I.Player.cargo),"interactions money cargo unchanged "+id);foreach(var pair in names)Check(pair.Key.DisplayName==pair.Value,"actual display name/ID untouched "+id);Check(!GameManager.I.AutosaveEnabled,"saves off "+id);
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
