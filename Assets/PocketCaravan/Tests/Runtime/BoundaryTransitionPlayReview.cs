#if UNITY_EDITOR
using System;using System.IO;using System.Collections;using System.Collections.Generic;using UnityEngine;using UnityEngine.SceneManagement;using UnityEditor;using UnityEditor.SceneManagement;
namespace PocketCaravan.EditorTools
{
    public sealed class BoundaryTransitionPlayReview:MonoBehaviour
    {
        public static string Output;public static Action<bool,string,string> Record;public static Action<Exception> Complete;
        PlayerMotor Motor=>PlayerController.Instance.Motor;
        static void Check(bool ok,string test,string detail=""){Record(ok,test,detail);}
        IEnumerator Start(){var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){object item=null;bool moved=false;Exception failure=null;try{moved=stack.Peek().MoveNext();if(moved)item=stack.Peek().Current;}catch(Exception e){failure=e;}if(failure!=null){Complete(failure);yield break;}if(!moved){stack.Pop();continue;}if(item is IEnumerator)stack.Push((IEnumerator)item);else yield return item;}Complete(null);}
        string ScenePath(string id){foreach(var s in EditorBuildSettings.scenes)if(Path.GetFileNameWithoutExtension(s.path)==id)return s.path;throw new InvalidOperationException(id);}
        IEnumerator Load(string id){yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath(id),new LoadSceneParameters(LoadSceneMode.Single));for(int i=0;i<4;i++)yield return null;Time.timeScale=0;PlayerController.Instance.enabled=false;Check(!GameManager.I.AutosaveEnabled,"original save protected",id);}
        Vector2 Direction(MapExit e){switch(e.edge){case "E":return Vector2.right;case "W":return Vector2.left;case "N":return Vector2.up;default:return Vector2.down;}}
        Vector2Int Facing(Vector2 d)=>new Vector2Int((int)d.x,(int)d.y);
        Vector2 Center(MapExit e){Rect r=e.WorldRect;return new Vector2(r.xMin+.5f,r.yMin+.5f);}
        Vector2 Contact(MapExit e){Rect r=e.WorldRect;Vector2 p=Center(e),h=Motor.feetSize*.5f;switch(e.edge){case "E":p.x=r.xMax-h.x-.00005f;break;case "W":p.x=r.xMin+h.x+.00005f;break;case "N":p.y=r.yMax-h.y-.00005f;break;case "S":p.y=r.yMin+h.y+.00005f;break;}return p;}
        void Pose(Vector2 feet,Vector2 direction){Motor.Teleport((Vector3)(feet-Motor.feetOffset),Facing(direction));FindFirstObjectByType<CameraFollow>().SnapNow();}
        IEnumerator Capture(string name){yield return null;yield return new WaitForEndOfFrame();var im=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Output+"/"+name+".png",im.EncodeToPNG());Destroy(im);}
        bool FreeFeet(Vector2 feet){var m=MapRoot.Current;var h=Motor.feetSize*.5f;return m.IsWalkable(m.WorldToCell(feet-h))&&m.IsWalkable(m.WorldToCell(feet+h))&&m.IsWalkable(m.WorldToCell(feet+new Vector2(h.x,-h.y)))&&m.IsWalkable(m.WorldToCell(feet+new Vector2(-h.x,h.y)));}
        bool Known(MapExit e)=>GoryeoTravelTransition.RegionOf(e.targetScene)!=null;
        void OldCamera(){var cam=Camera.main;var b=MapRoot.Current.WorldBounds;float h=cam.orthographicSize,w=h*cam.aspect;var p=Motor.transform.position;p.x=b.size.x>2*w?Mathf.Clamp(p.x,b.min.x+w,b.max.x-w):b.center.x;p.y=b.size.y>2*h?Mathf.Clamp(p.y,b.min.y+h,b.max.y-h):b.center.y;p.z=-10;cam.transform.position=p;}
        IEnumerator Movie(bool previous)
        {
            yield return Load("goryeo_songak");MapExit chosen=null;float length=5;
            foreach(float distance in new[]{5f,3f,1.5f})
            {
                foreach(var e in MapExit.All)
                {
                    if(e.targetScene!="transit_woodtown"||e.edge!="W")continue;
                    bool clear=true;for(float n=0;n<=distance;n+=.1f)clear&=FreeFeet(Contact(e)-Direction(e)*n);
                    if(clear){chosen=e;length=distance;break;}
                }
                if(chosen!=null)break;
            }
            Check(chosen!=null,"movie has a clear regional approach");
            var follow=FindFirstObjectByType<CameraFollow>();Vector2 direction=Direction(chosen);Pose(Contact(chosen)-direction*length,direction);
            follow.smoothTime=.12f;follow.enabled=!previous;Time.timeScale=1;Notify.Clear();int shot=0;string prefix=previous?"motion-before-":"motion-after-";
            bool used=false;
            for(int frame=0;frame<600;frame++)
            {
                Motor.Tick(direction,3.8f,1f/60);
                if(previous)OldCamera();
                if(frame%4==0)yield return Capture(prefix+(shot++).ToString("D3"));else yield return null;
                var exit=MapExit.At(Motor.Cell,Motor.Facing);
                if(exit==null)continue;
                if(previous){exit.Use(Motor);used=true;}else used=exit.TryUseAtBoundary(Motor,direction);
                if(used)break;
            }
            Check(used,"movie crossed the regional boundary",prefix);int frames=0;
            while(GameManager.I.IsTravelling)
            {
                if(frames++>600)throw new TimeoutException(prefix);
                if(frames%4==0)yield return Capture(prefix+(shot++).ToString("D3"));else yield return null;
            }
            yield return Capture(prefix+(shot++).ToString("D3"));Check(MapRoot.Current.mapId=="transit_woodtown","movie arrived in central continent",prefix);
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Output);for(int i=0;i<15;i++)yield return null;Check(MapRoot.Current.previewOnly&&!GameManager.I.AutosaveEnabled,"safe QA start");
            string[] maps={"Naru","Gaegyeong","Dakmok","goryeo_songak","goryeo_jangdan","transit_woodtown","transit_ferry","transit_crossroads","transit_southport","transit_wetroad","transit_pass","transit_central_approach","transit_south_inland"};
            int total=0;var filmed=new HashSet<string>();
            foreach(string id in maps)
            {
                yield return Load(id);var follow=FindFirstObjectByType<CameraFollow>();follow.smoothTime=0;
                var exits=new List<MapExit>(MapExit.All);
                foreach(var exit in exits)
                {
                    if(!Known(exit))continue;Vector2 direction=Direction(exit);Vector3 result;
                    Pose(Center(exit),direction);Check(!exit.TryGetBoundaryContact(Motor,Vector2.zero,out result),"standing never changes own map",id+" "+exit.edge);
                    Check(!exit.TryGetBoundaryContact(Motor,direction,out result),"exit cell center is not the edge",id+" "+exit.edge);
                    Pose(Contact(exit),direction);Check(!exit.TryGetBoundaryContact(Motor,-direction,out result),"turning inward does not travel",id);
                    Check(exit.TryGetBoundaryContact(Motor,direction,out result),"outer feet reach boundary",id+" "+exit.edge);
                    // The production contact path frames the exact exit before starting the fade.
                    Motor.Teleport(result,Facing(direction));follow.SnapNow();follow.FrameExit(exit);
                    Vector3 held=follow.transform.position;follow.SendMessage("LateUpdate");
                    Check(Vector3.Distance(held,follow.transform.position)<.0001f,"outgoing fade holds contact frame",id);
                    Rect r=exit.WorldRect;Vector3 edgePoint=Motor.Feet;
                    switch(exit.edge){case "E":edgePoint.x=r.xMax;break;case "W":edgePoint.x=r.xMin;break;case "N":edgePoint.y=r.yMax;break;case "S":edgePoint.y=r.yMin;break;}
                    Camera.main.ResetWorldToCameraMatrix();Vector3 v=Camera.main.WorldToViewportPoint(edgePoint);float screen=exit.edge=="E"?v.x:exit.edge=="W"?1-v.x:exit.edge=="N"?v.y:1-v.y;
                    Check(Mathf.Abs(screen-1)<.001f,"exit is the visible screen edge",id+" "+exit.edge+" v="+v+" camera="+Camera.main.transform.position+" h="+Camera.main.orthographicSize+" aspect="+Camera.main.aspect+" rect="+r);total++;
                    if(filmed.Contains(exit.edge)||!FreeFeet(Contact(exit)-direction*1.5f)||!FreeFeet(Contact(exit)-direction*.9f))continue;
                    bool onRoad=MapRoot.Current.GetComponent<RoadGrid>().GradeAt(MapRoot.Current.WorldToCell(Center(exit)))>0;
                    if(!onRoad)continue;filmed.Add(exit.edge);
                    foreach(float speed in new[]{3.8f,11.4f})foreach(int fps in new[]{30,60,144})
                    {
                        Pose(Contact(exit)-direction*1.5f,direction);bool reached=false;
                        for(int frame=0;frame<fps*3;frame++){Motor.Tick(direction,speed,1f/fps);if(exit.TryGetBoundaryContact(Motor,direction,out result)){reached=true;break;}}
                        Check(reached,"walk/run never stuck at the new edge",exit.edge+" "+speed+" "+fps);
                    }
                    // Reproduce the previous trigger and original rectangular camera without changing scene data.
                    Vector2 oldFeet=Center(exit);switch(exit.edge){case "E":oldFeet.x=r.xMin+.02f;break;case "W":oldFeet.x=r.xMax-.02f;break;case "N":oldFeet.y=r.yMin+.02f;break;case "S":oldFeet.y=r.yMax-.02f;break;}
                    Pose(oldFeet,direction);follow.enabled=false;OldCamera();yield return Capture(exit.edge+"-before");
                    follow.enabled=true;Pose(Contact(exit),direction);follow.FrameExit(exit);yield return Capture(exit.edge+"-after");
                }
            }
            Check(total>800,"own exit coverage",total.ToString());Check(filmed.Count==4,"all four directions filmed");
            // Actual scene transitions use the public controller path, not direct TravelTo.
            foreach(string id in new[]{"transit_woodtown","goryeo_songak","transit_pass","goryeo_jangdan"})
            {
                yield return Load(id);MapExit chosen=null;
                foreach(var e in MapExit.All)if(Known(e)&&GoryeoTravelTransition.IsRegionalCrossing(id,e.targetScene)&&FreeFeet(Contact(e)-Direction(e)*1.2f)&&FreeFeet(Contact(e)-Direction(e)*.6f)){chosen=e;break;}
                Check(chosen!=null,"cross-region walking exit exists",id);Vector2 d=Direction(chosen);string next=chosen.targetScene;Pose(Contact(chosen)-d*1.2f,d);bool used=false;
                for(int frame=0;frame<180;frame++){Motor.Tick(d,3.8f,1f/60);if(chosen.TryUseAtBoundary(Motor,d)){used=true;break;}}
                Check(used&&GameManager.I.IsTravelling,"real transition begins at outer edge",id);float started=Time.realtimeSinceStartup;
                while(GameManager.I.IsTravelling){if(Time.realtimeSinceStartup-started>30)throw new TimeoutException(next);yield return null;}
                Check(MapRoot.Current.mapId==next,"regional target reached",next);
            }
            yield return Movie(true);yield return Movie(false);
            yield return Load("venice_rialto");var foreign=MapExit.All[0];Pose(Center(foreign),Direction(foreign));Vector3 legacy;
            Check(foreign.TryGetBoundaryContact(Motor,Vector2.zero,out legacy),"foreign legacy exit behavior unchanged");
        }
    }
}
#endif
