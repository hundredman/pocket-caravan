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
    /// <summary>Editor-only text palette and real UI comparison. No server connection or user save writes.</summary>
    public sealed class GoryeoCommonTextReview : MonoBehaviour
    {
        public static string Pass = "after";
        static string Out => "ArtReview/Goryeo-UI1-Text-20261107/" + Pass + "/";
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
        bool palette;
        void OnGUI()
        {
            if (!palette) return;
            UIStyle.Begin(); GUI.depth = -2000; GUI.color = GUI.contentColor = Color.white;
            CuteUI.Fill(new Rect(0,0,UIStyle.VirtualWidth,UIStyle.VirtualHeight),CuteUI.Paper);
            var colors=new[]{CuteUI.Text,CuteUI.Sub,CuteUI.Interact,CuteUI.Alert,CuteUI.Up,CuteUI.Zero,CuteUI.Buy,CuteUI.Sell,CuteUI.White,CuteUI.SoftShadow};
            string[] names={"본문","보조","선택","알림","이익","중립","구매","판매","흰 글자","반투명"};
            for(int i=0;i<colors.Length;i++) {
                float y=14+i*24;
                if(i>=8)CuteUI.Fill(new Rect(0,y-10,UIStyle.VirtualWidth,23),CuteUI.Ink);
                CuteUI.Label(10,y,names[i]+" 한 줄",colors[i]);
                CuteUI.Wrap(160,y-7,140,names[i]+" 여러 줄",colors[i]);
                CuteUI.WrapCenter(310,y-7,150,names[i]+" 가운데",colors[i]);
            }
        }
        void VerifyStyles()
        {
            var flags=BindingFlags.Static|BindingFlags.NonPublic;
            var fields=new[]{"body","wrapBody","wrapCenter"};
            foreach(string f in fields) {
                var st=(GUIStyle)typeof(CuteUI).GetField(f,flags).GetValue(null);
                Color expected=Pass=="before"?(Color)CuteUI.SoftShadow:UIStyle.Lin(CuteUI.SoftShadow);
                Check(Vector4.Distance(st.normal.textColor,expected)<.00001f,"text color/alpha path "+f);
            }
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);var gm=GameManager.I;Check(gm!=null&&!gm.AutosaveEnabled,"saves OFF bootstrap");
            var login=FindFirstObjectByType<NetLogin>();if(login!=null)Call(login,"PlayAlone");
            Set(gm,"firstSceneLoaded",true);SaveSystem.UseSlot("codex_ui_text_"+Guid.NewGuid().ToString("N")+".json");slotPath=SaveSystem.CurrentPath;
            originalKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            yield return Size(480,270);yield return Load("Goryeo/Naru");
            gm.Player.money=520;gm.Player.credit=2;
            palette=true;yield return Photo("00-palette");VerifyStyles();palette=false;
            yield return Photo("01-hud");yield return Keypress(Key.Tab);yield return Photo("02-expanded");yield return Keypress(Key.Tab);
            var map=FindFirstObjectByType<WorldMapUI>();
            yield return Keypress(Key.M);yield return Photo("03-map");Set(map,"regional",true);yield return Photo("04-map-detail");yield return Keypress(Key.Escape);Set(map,"regional",false);
            if(login!=null) {
                ((NetTextBox)typeof(NetLogin).GetField("nameBox",Flags).GetValue(login)).Text="검토용 행상";
                ((NetTextBox)typeof(NetLogin).GetField("keyBox",Flags).GetValue(login)).Text="";
                Set(login,"remember",false);Set(login,"autoTried",true);Set(login,"submitting",false);
                yield return Keypress(Key.O);Check(UIState.Blocking,"offline form opens");yield return Photo("05-online-offline");
                var box=(NetTextBox)typeof(NetLogin).GetField("nameBox",Flags).GetValue(login);
                Check(Vector4.Distance(box.TextColor,UIStyle.Lin(CuteUI.Text))<.00001f,"offline name text color path");
                Check(box.Text=="검토용 행상","name text preserved");
                yield return Keypress(Key.Escape);Check(!UIState.Blocking,"offline form closes");
            }
            yield return Panel(Key.L,"06-ledger");
            yield return Size(960,540);yield return Panel(Key.M,"07-map-960");
            yield return Size(360,270);yield return Panel(Key.M,"08-map-narrow");
            yield return Size(480,270);
            foreach(string region in new[]{"Venice/venice_rialto","Arabia/arabia_ahsa","North/north_naru"}) {
                yield return Load(region);yield return Photo("09-hud-"+MapRoot.Current.mapId);
                yield return Panel(Key.M,"10-map-"+MapRoot.Current.mapId);
            }
            Check(!gm.AutosaveEnabled&&SaveSystem.CurrentPath==slotPath&&!File.Exists(slotPath),"saves OFF/no file throughout");
        }
    }
}
#endif
