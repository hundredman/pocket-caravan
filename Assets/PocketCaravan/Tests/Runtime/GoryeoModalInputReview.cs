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
    /// <summary>Virtual keyboard through ordinary Update/InputReader. No direct activity/choice invocation.</summary>
    public sealed class GoryeoModalInputReview : MonoBehaviour
    {
        const string Out = "ArtReview/Goryeo-Modal-Input-20261106/";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        [Serializable] class Report { public bool complete; public string error; public float movementDuringModal, clockDuringModal; public Vector3 atOpen, atRest; public List<MotionResult> motions = new List<MotionResult>(); public List<string> checks = new List<string>(); }
        [Serializable] class MotionResult{public string map,key;public float distance,hours;}
        readonly Report report = new Report();
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
            var t = ScreenCapture.CaptureScreenshotAsTexture(); Check(t.width == 480 && t.height == 270, "actual480x270 " + name);
            File.WriteAllBytes(Out + name + ".png", t.EncodeToPNG()); Destroy(t);
        }
        void Near(Interactable target)
        {
            var pc = PlayerController.Instance;
            pc.Motor.Teleport(target.transform.position + Vector3.down - (Vector3)pc.Motor.feetOffset, Vector2Int.up);
            var cam = Camera.main; cam.GetComponent<CameraFollow>().enabled = false;
            cam.orthographicSize = 8.4375f; cam.transform.position = target.transform.position + Vector3.down * 3 + new Vector3(0, 0, -10);
        }
        bool PrivateOpen(Component obj,string field) => (bool)obj.GetType().GetField(field,Flags).GetValue(obj);
        int ModalCount()
        {
            var gm=GameManager.I;
            return (gm.GetComponent<InventoryUI>().IsOpen?1:0)+(gm.GetComponent<TradeLedgerUI>().IsOpen?1:0)
                +(PrivateOpen(gm.GetComponent<WorldMapUI>(),"open")?1:0)+(PrivateOpen(gm.GetComponent<QuestTracker>(),"logOpen")?1:0)
                +(PrivateOpen(gm.GetComponent<PauseMenu>(),"open")?1:0);
        }
        void PutOnOpenGround()
        {
            var map=MapRoot.Current;var grid=map.GetComponent<RoadGrid>();
            for(int y=8;y<grid.height-8;y++)for(int x=8;x<grid.width-8;x++)
            {
                bool free=true;
                for(int dy=-2;dy<=2&&free;dy++)for(int dx=-2;dx<=2;dx++)
                {var c=new Vector3Int(x+dx,y+dy,0);if(!map.IsWalkable(c)||MapExit.At(c)!=null){free=false;break;}}
                if(!free)continue;
                var pc=PlayerController.Instance;pc.Motor.Teleport(map.CellCenter(new Vector3Int(x,y,0))-(Vector3)pc.Motor.feetOffset,Vector2Int.up);return;
            }
            throw new Exception("No 5x5 walkable fixture");
        }
        IEnumerator Motion(Key key)
        {
            var gm=GameManager.I;PutOnOpenGround();Time.timeScale=1;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));yield return new WaitForSecondsRealtime(.2f);
            Check(PlayerController.Instance.Motor.Velocity.magnitude>.5f,"W movement before "+key+" in "+MapRoot.Current.mapId);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,key));
            for(int i=0;i<30&&!UIState.Blocking;i++)yield return null;
            Check(UIState.Blocking&&ModalCount()==1,"one modal opens while W is held "+key+" in "+MapRoot.Current.mapId);
            report.atOpen=PlayerController.Instance.transform.position;float clock=gm.Clock.TotalHours;
            yield return new WaitForSecondsRealtime(.3f);
            report.atRest=PlayerController.Instance.transform.position;
            report.movementDuringModal=Vector3.Distance(report.atOpen,report.atRest);report.clockDuringModal=gm.Clock.TotalHours-clock;
            report.motions.Add(new MotionResult{map=MapRoot.Current.mapId,key=key.ToString(),distance=report.movementDuringModal,hours=report.clockDuringModal});
            Check(Mathf.Abs(report.clockDuringModal)<.0001f,"clock stops under "+key+" in "+MapRoot.Current.mapId);
            Check(report.movementDuringModal<.001f,"player stays still under "+key+" in "+MapRoot.Current.mapId+" distance="+report.movementDuringModal);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());Time.timeScale=0;yield return Keypress(Key.Escape);
            Check(!UIState.Blocking&&ModalCount()==0,"modal releases cleanly "+key);
            var pos=PlayerController.Instance.transform.position;Time.timeScale=1;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));yield return new WaitForSecondsRealtime(.15f);
            Check(Vector3.Distance(pos,PlayerController.Instance.transform.position)>.02f,"normal movement resumes after "+key+" in "+MapRoot.Current.mapId);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());Time.timeScale=0;
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);var gm=GameManager.I;Check(gm!=null&&!gm.AutosaveEnabled,"safe saves-OFF preview");
            var login=FindFirstObjectByType<NetLogin>();if(login!=null)typeof(NetLogin).GetMethod("PlayAlone",Flags).Invoke(login,null);
            SaveSystem.UseSlot("codex_modal_input_"+Guid.NewGuid().ToString("N")+".json");slotPath=SaveSystem.CurrentPath;
            originalKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            yield return Load("Goryeo/Naru");PutOnOpenGround();
            int money=gm.Player.money,credit=gm.Player.credit;float weight=gm.Player.TotalWeight;
            foreach(Key open in new[]{Key.I,Key.M,Key.Q,Key.L})
            {
                yield return Keypress(open);Check(UIState.Blocking&&ModalCount()==1,"one modal opens "+open);
                yield return Photo("modal-"+open);
                foreach(Key other in new[]{Key.I,Key.M,Key.Q,Key.L})if(other!=open)
                {yield return Keypress(other);Check(UIState.Blocking&&ModalCount()==1,"no second modal "+open+" then "+other);}
                yield return Keypress(Key.Space);Check(UIState.Blocking&&ModalCount()==1,"Space does not interact with world under "+open);
                yield return Keypress(open);Check(!UIState.Blocking&&ModalCount()==0,"same key closes without reopening "+open);
                yield return Keypress(open);yield return Keypress(Key.Escape);Check(!UIState.Blocking&&ModalCount()==0,"Escape closes without opening pause "+open);
            }
            yield return Keypress(Key.Escape);Check(UIState.Blocking&&ModalCount()==1,"Escape opens pause menu");
            foreach(Key other in new[]{Key.I,Key.M,Key.Q,Key.L}){yield return Keypress(other);Check(ModalCount()==1,"pause blocks "+other);}
            yield return Keypress(Key.Escape);Check(!UIState.Blocking&&ModalCount()==0,"Escape closes pause");
            yield return Keypress(Key.Tab);Check(HUD.Expanded,"Tab expands nonmodal HUD");
            yield return Keypress(Key.I);yield return Keypress(Key.Tab);Check(HUD.Expanded&&ModalCount()==1,"Tab cannot change HUD behind modal");
            yield return Keypress(Key.Escape);yield return Keypress(Key.Tab);Check(!HUD.Expanded&&!UIState.Blocking,"Tab collapses HUD after closing modal");
            Check(gm.Player.money==money&&gm.Player.credit==credit&&gm.Player.TotalWeight==weight,"modal keys preserve money credit cargo");
            foreach(Key key in new[]{Key.I,Key.M,Key.Q,Key.L,Key.Escape})yield return Motion(key);
            foreach(string region in new[]{"Venice/venice_rialto","Arabia/arabia_ahsa","North/north_naru"})
            {yield return Load(region);yield return Motion(Key.I);}
            Check(!gm.AutosaveEnabled&&SaveSystem.CurrentPath==slotPath,"saves OFF and isolated slot throughout");
        }
    }
}
#endif
