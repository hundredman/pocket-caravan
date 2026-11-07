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
    public sealed class GoryeoModalReview : MonoBehaviour
    {
        public static bool ExpectExclusive;
        static string Out => "Docs/Validation/Goryeo-Modal-Exclusive-2026-11-07/"+(ExpectExclusive?"after/":"before/");
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
        bool Open(object obj,string field)=>(bool)obj.GetType().GetField(field,Flags).GetValue(obj);
        IEnumerator Frames(){for(int i=0;i<5;i++)yield return null;yield return new WaitForEndOfFrame();}
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);var gm=GameManager.I;Check(gm!=null&&!gm.AutosaveEnabled,"saves OFF bootstrap");
            var login=FindFirstObjectByType<NetLogin>();if(login!=null)Call(login,"PlayAlone");Set(gm,"firstSceneLoaded",true);
            SaveSystem.UseSlot("codex_modal_"+Guid.NewGuid().ToString("N")+".json");slotPath=SaveSystem.CurrentPath;
            originalKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            yield return Size(480,270);yield return Load("Goryeo/Naru");
            var inv=FindFirstObjectByType<InventoryUI>();var market=FindFirstObjectByType<TownMarket>();var board=FindFirstObjectByType<ContractBoard>();var dialogue=FindFirstObjectByType<DialogueUI>();
            inv.Open();market.Interact(PlayerController.Instance);yield return Frames();yield return Photo("01-market-inventory");
            Check(inv.IsOpen,"inventory remains open");Check(Open(market,"open")==!ExpectExclusive,ExpectExclusive?"market entry cannot stack over inventory":"baseline defect: market stacks over inventory");
            if(Open(market,"open"))Call(market,"Close");inv.Close();yield return Frames();
            inv.Open();DialogueUI.Show("검토","다른 창이 열려 있을 때 도착한 대화입니다.");yield return Frames();yield return Photo("02-dialogue-inventory");
            if(!ExpectExclusive)
            {
                Check(Open(dialogue,"open")&&inv.IsOpen,"baseline defect: dialogue stacks over inventory");
                yield return Keypress(Key.Space);yield return Frames();
                Check(inv.IsOpen&&!UIState.Blocking,"baseline defect: closing dialogue unlocks a still-open inventory");inv.Close();
            }
            else
            {
                Check(!Open(dialogue,"open")&&inv.IsOpen&&UIState.Blocking,"busy dialogue waits behind inventory");
                yield return Keypress(Key.Escape);yield return Frames();Check(!inv.IsOpen&&Open(dialogue,"open")&&UIState.Blocking,"queued dialogue opens only after inventory closes");
                yield return Keypress(Key.Space);yield return Frames();Check(!Open(dialogue,"open")&&!UIState.Blocking,"dialogue closes and releases only its lock");
                market.Interact(PlayerController.Instance);board.Interact(PlayerController.Instance);inv.Open();yield return Frames();
                Check(Open(market,"open")&&!Open(board,"open")&&!inv.IsOpen,"market excludes board and inventory");
                UIState.Release();Check(UIState.Blocking,"unrelated legacy release does not unlock market");
                UIState.Blocking=false;Check(UIState.Blocking,"legacy false flag does not unlock owned window");
                yield return Keypress(Key.L);yield return Frames();var ledger=FindFirstObjectByType<TradeLedgerUI>();Check(!Open(market,"open")&&ledger.IsOpen&&UIState.Blocking,"market L transfers to ledger without stacking");
                yield return Keypress(Key.Escape);yield return Frames();Check(!ledger.IsOpen&&!UIState.Blocking,"ledger Escape closes cleanly");
                foreach(var key in new[]{Key.I,Key.Q,Key.M,Key.Escape})
                {
                    yield return Keypress(key);yield return Frames();Check(UIState.Blocking,"main window opens "+key);
                    inv.Open();market.Interact(PlayerController.Instance);board.Interact(PlayerController.Instance);yield return Frames();
                    int count=(inv.IsOpen?1:0)+(Open(market,"open")?1:0)+(Open(board,"open")?1:0)+(Open(FindFirstObjectByType<QuestTracker>(),"logOpen")?1:0)+(Open(FindFirstObjectByType<WorldMapUI>(),"open")?1:0)+(Open(FindFirstObjectByType<PauseMenu>(),"open")?1:0);
                    Check(count==1,"one main window under repeated entry "+key);
                    yield return Keypress(Key.Escape);yield return Frames();Check(!UIState.Blocking,"Escape leaves no stale lock "+key);
                }
            }
            Check(!gm.AutosaveEnabled&&!File.Exists(slotPath),"no save file throughout modal review");
        }
    }
}
#endif
