#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace PocketCaravan.EditorTools
{
    /// <summary>Editor-only map name/layout checks in actual GameViews. No server connection or user save writes.</summary>
    public sealed class GoryeoMapLabelsReview : MonoBehaviour
    {
        const string Out = "ArtReview/Goryeo-UI1-Map-Short-20261107/";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        [Serializable] class Report { public bool complete; public string error; public List<string> checks = new List<string>(); }
        readonly Report report = new Report();
        public static Action<int,int> Resize;
        Texture2D lastCapture;
        Keyboard keyboard, originalKeyboard;
        string slotPath;
        RegionTestSession redirectedSession;
        string originalSessionSlot;
        void Check(bool value, string text) { if (!value) throw new Exception(text); report.checks.Add(text); }
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject); var stack = new Stack<IEnumerator>(); stack.Push(Run());
            while (stack.Count > 0)
            {
                bool more = false; object value = null; Exception error = null;
                try { more = stack.Peek().MoveNext(); if (more) value = stack.Peek().Current; } catch (Exception e) { error = e; }
                if (error != null) { Finish(error); yield break; }
                if (!more) { stack.Pop(); continue; }
                if (value is IEnumerator) stack.Push((IEnumerator)value); else yield return value;
            }
            Finish(null);
        }
        void Finish(Exception error)
        {
            if(redirectedSession!=null)redirectedSession.saveFileName=originalSessionSlot;
            if (GameManager.I != null) typeof(GameManager).GetField("autosave", Flags).SetValue(GameManager.I, false);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (originalKeyboard != null && originalKeyboard.added) originalKeyboard.MakeCurrent();
            if(slotPath!=null && Directory.Exists(slotPath+".tmp"))Directory.Delete(slotPath+".tmp");
            if (slotPath != null) foreach (string suffix in new[] { "", ".bak", ".tmp" }) if (File.Exists(slotPath + suffix)) File.Delete(slotPath + suffix);
            report.complete = error == null; report.error = error == null ? "" : error.ToString();
            Directory.CreateDirectory(Out); File.WriteAllText(Out + "play.json", JsonUtility.ToJson(report, true));
            if(lastCapture!=null)Destroy(lastCapture);
            Time.timeScale = 1; EditorApplication.isPlaying = false;
        }
        IEnumerator Keypress(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null; yield return new WaitForEndOfFrame();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null; yield return new WaitForEndOfFrame();
        }
        void Quiet()
        {
            foreach (var s in FindObjectsByType<StoryGuide>(FindObjectsSortMode.None)) { s.StopAllCoroutines(); s.enabled = false; }
            foreach (var s in FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None)) { s.StopAllCoroutines(); s.enabled = false; }
            var d = FindFirstObjectByType<DialogueUI>(); typeof(DialogueUI).GetMethod("Close", Flags).Invoke(d, null);
            UIState.Release(); Notify.Clear(); Time.timeScale = 0;
        }
        IEnumerator Load(string path)
        {
            yield return SceneManager.LoadSceneAsync(Path.GetFileNameWithoutExtension(path), LoadSceneMode.Single);
            for (int i = 0; i < 12; i++) yield return null;
            Quiet(); Check(SaveSystem.CurrentPath == slotPath, "isolated slot retained " + path+" actual="+SaveSystem.CurrentPath+" expected="+slotPath);
            Check(SceneManager.GetActiveScene().buildIndex >= 0, "normal registered scene index " + path);
        }
        IEnumerator Photo(string name)
        {
            Notify.Clear(); yield return null; yield return new WaitForEndOfFrame();
            var t = ScreenCapture.CaptureScreenshotAsTexture(); Check(t.width == Screen.width && t.height == Screen.height, "native capture " + name+" "+t.width+"x"+t.height);
            File.WriteAllBytes(Out + name + ".png", t.EncodeToPNG()); lastCapture=t;
        }
        IEnumerator Size(int w,int h)
        {
            if(Resize==null)throw new Exception("Resize delegate missing");Resize(w,h);
            for(int i=0;i<10;i++)yield return null;
            Check(Screen.width==w&&Screen.height==h,"actual viewport "+w+"x"+h);
        }
        void Set(object obj,string field,object value)=>obj.GetType().GetField(field,Flags).SetValue(obj,value);
        object Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,Flags).Invoke(obj,args);
        IEnumerator Panel(Key key,string name) {
            UIState.Release();yield return Keypress(key);Check(UIState.Blocking,"modal opens "+name);
            yield return Photo(name);yield return Keypress(Key.Escape);Check(!UIState.Blocking,"modal closes "+name);
        }
        object Field(object obj,string field)=>obj.GetType().GetField(field,BindingFlags.Public|BindingFlags.Instance).GetValue(obj);
        object Layout()=>typeof(CuteMap).GetField("lastLabels",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
        bool Inside(Rect a,Rect b)=>b.x>=a.x-.6f&&b.y>=a.y-.6f&&b.xMax<=a.xMax+.6f&&b.yMax<=a.yMax+.6f;
        void CheckMap(string name,bool goryeo=false,bool regional=false)
        {
            var layout=Layout();Check(layout!=null,"layout exists "+name);
            var rects=(Rect[])Field(layout,"rects");var labels=(string[])Field(layout,"labels");var points=(Vector2[])Field(layout,"points");
            var places=(CuteMap.Place[])Field(layout,"places");var area=(Rect)Field(layout,"area");var pin=(Rect)Field(layout,"pin");
            Check(Inside(new Rect(0,0,UIStyle.VirtualWidth,UIStyle.VirtualHeight),area),"map panel inside viewport "+name);
            bool labelsFit=true,noOverlaps=true,markersClear=true,pinClear=true,terrainClear=true;
            var canvas=(Rect)Field(layout,"canvas");
            var style=(GUIStyle)typeof(CuteMap).GetProperty("TagStyle",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            for(int i=0;i<rects.Length;i++) {
                terrainClear &= !rects[i].Overlaps(canvas);
                labelsFit &= Inside(area,rects[i])&&style.CalcHeight(new GUIContent(labels[i]),rects[i].width)<=rects[i].height+.1f;
                for(int j=0;j<i;j++)noOverlaps &= !rects[i].Overlaps(rects[j]);
                for(int j=0;j<points.Length;j++)markersClear &= !rects[i].Overlaps(new Rect(points[j].x-4,points[j].y-4,8,8));
                if(pin.width>0)pinClear &= !rects[i].Overlaps(pin);
            }
            Check(labelsFit,"all label text fits inside map "+name);
            Check(noOverlaps,"no pair of name labels overlaps "+name);
            Check(markersClear,"map dots clear of labels "+name);
            Check(pinClear,"current pin clear of labels "+name);
            var foot=(List<Rect>)typeof(CuteMap).GetField("lastFooterRects",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            bool footerClear=true;
            for(int i=0;i<foot.Count;i++) {footerClear &= Inside(new Rect(0,0,UIStyle.VirtualWidth,UIStyle.VirtualHeight),foot[i]);for(int j=0;j<i;j++)footerClear &= !foot[i].Overlaps(foot[j]);}
            Check(footerClear,"footer information fits without overlap "+name);
            if(goryeo) {
                Check(terrainClear,"name tags do not cover terrain "+name);
                var map=FindFirstObjectByType<WorldMapUI>();var atlas=(WorldMapUI.Atlas)map.GetType().GetField(regional?"regionalAtlas":"atlas",Flags).GetValue(map);
                Check(places.Length==atlas.maps.Length+(atlas.settlements==null?0:atlas.settlements.Length),"all atlas entries retained "+name);
                for(int i=0;i<atlas.maps.Length;i++)Check(places[i].name==ExpectedLabel(WorldIndex.Get().MapName(atlas.maps[i].mapId)),"short registered map name "+atlas.maps[i].mapId+" "+name);
            }
            var cached=Layout();Check(ReferenceEquals(cached,layout),"cached layout inspected "+name);
        }
        static string ExpectedLabel(string name)
        {
            var labels=new Dictionary<string,string> {
                {"예성 나루 마을","예성 나루"},{"장단 들마을","장단"},{"송악 산록 마을","송악"},{"닥나무 마을","닥나무"},
                {"솔재 길목 마을","솔재"},{"버들나루 마을","버들나루"},{"내륙 교역 길목","교역 길목"},{"버들 물가 길","버들 물가"},
                {"북쪽 쉼터 마을","북쪽 쉼터"},{"남쪽 호숫가 마을","남쪽 호숫가"}};
            return labels.TryGetValue(name,out string label)?label:name;
        }
        IEnumerator MapPhoto(string name,bool goryeo=false,bool regional=false)
        {
            yield return Photo(name);CheckMap(name,goryeo,regional);
            var old=Layout();yield return null;yield return new WaitForEndOfFrame();Check(ReferenceEquals(old,Layout()),"unchanged map reuses layout "+name);
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);var gm=GameManager.I;Check(gm!=null&&!gm.AutosaveEnabled,"saves OFF bootstrap");
            var login=FindFirstObjectByType<NetLogin>();if(login!=null)Call(login,"PlayAlone");
            Set(gm,"firstSceneLoaded",true);SaveSystem.UseSlot("codex_ui_map_"+Guid.NewGuid().ToString("N")+".json");slotPath=SaveSystem.CurrentPath;
            originalKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            yield return Size(480,270);yield return Load("Goryeo/Naru");var map=FindFirstObjectByType<WorldMapUI>();
            yield return Keypress(Key.M);Check(UIState.Blocking,"M opens real map");yield return MapPhoto("01-map",true);
            Set(map,"regional",true);yield return MapPhoto("02-map-detail",true,true);Set(map,"regional",false);
            yield return Size(960,540);yield return MapPhoto("03-map-960",true);
            yield return Size(360,270);yield return MapPhoto("04-map-narrow",true);
            Set(map,"regional",true);yield return MapPhoto("05-detail-narrow",true,true);Set(map,"regional",false);
            // Review-only contracts: all current towns are marked as destinations; no acceptance or saves.
            gm.Player.contracts.Clear();foreach(var town in gm.Db.towns)if(town!=null)gm.Player.contracts.Add(new Contract{toTown=town.id});
            yield return MapPhoto("06-all-destinations-narrow",true);
            yield return Size(480,270);yield return MapPhoto("07-all-destinations",true);gm.Player.contracts.Clear();
            yield return Keypress(Key.Escape);Check(!UIState.Blocking,"Esc closes real map");
            foreach(string scene in new[]{"Goryeo/Dakmok","Transit/transit_crossroads"}) {
                yield return Load(scene);map=FindFirstObjectByType<WorldMapUI>();yield return Keypress(Key.M);yield return MapPhoto("08-map-pin-"+MapRoot.Current.mapId,true);yield return Keypress(Key.Escape);
            }
            foreach(string scene in new[]{"Venice/venice_rialto","Arabia/arabia_ahsa","North/north_naru"}) {
                yield return Load(scene);yield return Keypress(Key.M);yield return MapPhoto("09-map-"+MapRoot.Current.mapId);yield return Keypress(Key.Escape);
                yield return Size(360,270);yield return Keypress(Key.M);yield return MapPhoto("10-map-narrow-"+MapRoot.Current.mapId);yield return Keypress(Key.Escape);yield return Size(480,270);
            }
            Check(!gm.AutosaveEnabled&&SaveSystem.CurrentPath==slotPath&&!File.Exists(slotPath),"saves OFF/no file throughout");
        }
    }
}
#endif
