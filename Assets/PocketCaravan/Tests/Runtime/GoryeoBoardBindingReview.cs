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
    /// <summary>Visual board fixtures open through Interact; Escape uses virtual Keyboard/InputReader.</summary>
    public sealed class GoryeoBoardBindingReview : MonoBehaviour
    {
        const string Out = "ArtReview/Goryeo-Board-Binding-20261106/";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        [Serializable] class Report { public bool complete; public string error; public List<string> checks = new List<string>(); }
        readonly Report report = new Report();
        public static Action<int,int> Resize;
        Texture2D lastCapture;
        Keyboard keyboard, originalKeyboard;
        string slotPath;
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
            if (GameManager.I != null) typeof(GameManager).GetField("autosave", Flags).SetValue(GameManager.I, false);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (originalKeyboard != null && originalKeyboard.added) originalKeyboard.MakeCurrent();
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
            Quiet(); Check(SaveSystem.CurrentPath == slotPath, "isolated slot retained " + path);
            Check(SceneManager.GetActiveScene().buildIndex >= 0, "normal registered scene index " + path);
        }
        IEnumerator Photo(string name)
        {
            Notify.Clear(); yield return null; yield return new WaitForEndOfFrame();
            var t = ScreenCapture.CaptureScreenshotAsTexture(); Check(t.width == Screen.width && t.height == Screen.height, "native capture " + name+" "+t.width+"x"+t.height);
            File.WriteAllBytes(Out + name + ".png", t.EncodeToPNG()); lastCapture=t;
        }
        void Near(Interactable target)
        {
            var pc = PlayerController.Instance;
            pc.Motor.Teleport(target.transform.position + Vector3.down - (Vector3)pc.Motor.feetOffset, Vector2Int.up);
            var cam = Camera.main; cam.GetComponent<CameraFollow>().enabled = false;
            cam.orthographicSize = 8.4375f; cam.transform.position = target.transform.position + Vector3.down * 3 + new Vector3(0, 0, -10);
        }
        ContractBoard Board(string town=null)
        {
            foreach(var b in FindObjectsByType<ContractBoard>(FindObjectsSortMode.None))if(town==null||b.town==town)return b;
            throw new Exception("Missing board "+town);
        }
        IEnumerator Size(int w,int h)
        {
            if(Resize==null)throw new Exception("Resize delegate missing");Resize(w,h);
            for(int i=0;i<10;i++)yield return null;
            Check(Screen.width==w&&Screen.height==h,"actual viewport "+w+"x"+h);
        }
        void LayoutBounds(string name)
        {
            float h=UIStyle.WindowHeight(310);float y=(UIStyle.VirtualHeight-h)/2;
            Check(y>=0&&y+h<=UIStyle.VirtualHeight,"panel inside virtual viewport "+name);
            Check(Mathf.Max(72,h-108)<h-16,"list smaller than panel area "+name);
        }
        IEnumerator CaptureBoard(ContractBoard board,string name)
        {
            Near(board);board.Interact(PlayerController.Instance);LayoutBounds(name);yield return Photo(name);
            Destroy(lastCapture);lastCapture=null;
            yield return Keypress(Key.Escape);Check(!UIState.Blocking,"Escape closes production board "+name);
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);var gm=GameManager.I;Check(gm!=null&&!gm.AutosaveEnabled,"safe saves-OFF bootstrap");
            var login=FindFirstObjectByType<NetLogin>();if(login!=null)typeof(NetLogin).GetMethod("PlayAlone",Flags).Invoke(login,null);
            SaveSystem.UseSlot("codex_board_layout_"+Guid.NewGuid().ToString("N")+".json");slotPath=SaveSystem.CurrentPath;
            originalKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            yield return Size(480,270);yield return Load("Goryeo/goryeo_songak");
            typeof(GameManager).GetProperty("Contracts").SetValue(gm,new ContractManager(gm.Db,gm.Clock,gm.Player,null,21));gm.Contracts.InitBoards();
            gm.Clock.day=31;gm.Clock.minuteOfDay=720;
            var board=Board("goryeo_songak");Near(board);board.Interact(PlayerController.Instance);LayoutBounds("goryeo-empty");
            yield return Photo("production-empty");var production=lastCapture;lastCapture=null;
            board.enabled=false;MapRoot.Current.previewOnly=true;
            var obj=new GameObject("Approved board reference");obj.transform.SetParent(MapRoot.Current.transform,false);obj.transform.position=board.transform.position;
            var trial=obj.AddComponent<GoryeoBoardLayoutTrial>();trial.town=board.town;trial.Interact(PlayerController.Instance);yield return Photo("approved-reference");
            var a=production.GetPixels32();var b=lastCapture.GetPixels32();int differences=0;
            for(int y=12;y<258;y++)for(int x=12;x<468;x++)if(!a[y*480+x].Equals(b[y*480+x]))differences++;
            Check(differences==0,"approved reference/production opaque panel RGBA differences 0 (456x246)");Destroy(production);Destroy(lastCapture);lastCapture=null;
            trial.enabled=false;Destroy(obj);MapRoot.Current.previewOnly=false;board.enabled=true;
            var offer=gm.Contracts.Available(board.town).Find(c=>!c.Premium&&gm.Contracts.CanAccept(c)==null);
            Check(offer!=null&&gm.Contracts.Accept(board.town,offer),"ordinary contract for accepted-message fixture");
            board.Interact(PlayerController.Instance);typeof(ContractBoard).GetField("message",Flags).SetValue(board,$"{Josa.Eul(offer.cargo)} 맡았다. {gm.Contracts.TownName(offer.toTown)} 게시판에 가져가자.");
            LayoutBounds("accepted");yield return Photo("production-accepted");Destroy(lastCapture);lastCapture=null;yield return Keypress(Key.Escape);
            yield return Size(960,540);yield return CaptureBoard(board,"production-960x540");
            string destination=WorldIndex.Get().MapOf("market",offer.toTown);yield return Size(480,270);yield return Load(destination);board=Board(offer.toTown);
            yield return CaptureBoard(board,"production-incoming");
            var result=gm.Contracts.Deliver(offer,offer.toTown);Check(result.success,"delivery completion message fixture");board.Interact(PlayerController.Instance);typeof(ContractBoard).GetField("message",Flags).SetValue(board,result.message);
            LayoutBounds("delivery-complete");yield return Photo("production-delivered");Destroy(lastCapture);lastCapture=null;yield return Keypress(Key.Escape);
            foreach(string path in new[]{"Venice/venice_rialto","Arabia/arabia_ahsa","North/north_naru"})
            {
                yield return Size(640,360);yield return Load(path);Check(!UIStyle.CrispRegion,"legacy360 UI mode "+path);yield return CaptureBoard(Board(),"production-"+Path.GetFileName(path));
            }
            Check(!gm.AutosaveEnabled&&SaveSystem.CurrentPath==slotPath,"no production save enabled during visual fixture");
        }
    }
}
#endif
