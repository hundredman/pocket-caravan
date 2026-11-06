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
    public sealed class GoryeoWholeUIAuditReview : MonoBehaviour
    {
        const string Out = "ArtReview/Goryeo-Whole-UI-Audit-20261106/";
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
        object Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,Flags).Invoke(obj,args);
        void Set(object obj,string field,object value)=>obj.GetType().GetField(field,Flags).SetValue(obj,value);
        void EnumField(object obj,string field,string value) {
            var f=obj.GetType().GetField(field,Flags);f.SetValue(obj,Enum.Parse(f.FieldType,value));
        }
        IEnumerator Panel(Key key,string name) {
            UIState.Release();yield return Keypress(key);Check(UIState.Blocking,"modal opens "+name);
            yield return Photo(name);yield return Keypress(Key.Escape);Check(!UIState.Blocking,"modal closes "+name);
        }
        void CloseDialogue() {Call(FindFirstObjectByType<DialogueUI>(),"Close");UIState.Release();}
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);var gm=GameManager.I;Check(gm!=null&&!gm.AutosaveEnabled,"saves OFF bootstrap");
            var login=FindFirstObjectByType<NetLogin>();if(login!=null)Call(login,"PlayAlone");
            Set(gm,"firstSceneLoaded",true);SaveSystem.UseSlot("codex_ui_audit_"+Guid.NewGuid().ToString("N")+".json");slotPath=SaveSystem.CurrentPath;
            originalKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            yield return Size(480,270);yield return Load("Goryeo/Naru");
            gm.Player.money=520;gm.Player.credit=2;gm.Player.cargo.Clear();
            foreach(string id in new[]{"grain","salt","paper"}) {var item=gm.ItemById(id);if(item!=null)gm.Player.cargo.Add(item,1,10);}
            var q=gm.Db.quests.Find(x=>x!=null&&x.id=="q1_naru");
            if(q!=null) {
                var data=new SaveData{q1MigrationVersion=1};data.quests.Add(new QuestManager.Save{id=q.id,step=0,amount=0,
                    rewardCaptured=true,acceptedRewardMoney=q.rewardMoney,acceptedRewardCredit=q.rewardCredit});gm.Quests.Import(data);
                bool active=false;foreach(var progress in gm.Quests.Active)if(progress.quest==q)active=true;
                Check(active,"canonical active quest displayed through review-only imported progress");
            }
            TownMarket market=null;foreach(var m in FindObjectsByType<TownMarket>(FindObjectsSortMode.None))if(m.town.id=="naru")market=m;
            Check(market!=null,"market fixture");Near(market);Call(market,"Observe");
            yield return Photo("01-hud");yield return Keypress(Key.Tab);yield return Photo("02-status-expanded");yield return Keypress(Key.Tab);
            yield return Panel(Key.I,"03-inventory");yield return Panel(Key.M,"04-map");
            yield return Panel(Key.Q,"05-quests");yield return Panel(Key.L,"06-ledger");yield return Panel(Key.Escape,"07-pause");
            var guide=gm.GetComponent<CommerceGuideUI>();guide.Open();Check(guide.IsOpen,"guide opens");yield return Photo("08-commerce-guide");guide.Close();
            DialogueUI.Show("나루의 상인","물건을 사기 전에 장부를 살펴보세요. 같은 물건이라도 마을마다 값이 다릅니다.\n짐이 무거우면 이동이 느려집니다.",
                new DialogueUI.Choice("장터로 가 볼게요",null),new DialogueUI.Choice("조금 더 둘러볼게요",null));
            yield return Photo("09-dialogue");var dialogue=FindFirstObjectByType<DialogueUI>();Set(dialogue,"pageIndex",Mathf.Max(0,dialogue.PageCount-1));yield return Photo("10-dialogue-choices");CloseDialogue();
            Inn.Offer("나루 주막");yield return Photo("11-inn");Call(FindFirstObjectByType<Inn>(),"CloseAsk");
            Peddler.Open();yield return Photo("12-peddler");dialogue=FindFirstObjectByType<DialogueUI>();Set(dialogue,"pageIndex",Mathf.Max(0,dialogue.PageCount-1));yield return Photo("13-peddler-choices");CloseDialogue();
            Museum.Open(Reactions.Region.Goryeo);yield return Photo("14-museum");CloseDialogue();
            if(login!=null) {
                ((NetTextBox)typeof(NetLogin).GetField("nameBox",Flags).GetValue(login)).Text="검토용 행상";
                ((NetTextBox)typeof(NetLogin).GetField("keyBox",Flags).GetValue(login)).Text="";
                Set(login,"remember",false);Set(login,"autoTried",true);Set(login,"submitting",false);
                yield return Panel(Key.O,"15-online-offline");
            }
            StartScreen.Open(true);yield return Photo("16-start-menu");var start=FindFirstObjectByType<StartScreen>();EnumField(start,"mode","Pick");yield return Photo("17-start-origins");EnumField(start,"mode","Closed");UIState.Release();
            yield return Size(960,540);yield return Panel(Key.M,"18-map-960");yield return Panel(Key.I,"19-inventory-960");
            StartScreen.Open(false);yield return Photo("20-start-origins-960");EnumField(start,"mode","Closed");UIState.Release();
            yield return Size(480,270);
            foreach(string region in new[]{"Venice/venice_rialto","Arabia/arabia_ahsa","North/north_naru"}) {
                yield return Load(region);yield return Photo("21-hud-"+MapRoot.Current.mapId);
                yield return Keypress(Key.M);yield return Photo("22-map-"+MapRoot.Current.mapId);yield return Keypress(Key.Escape);UIState.Release();
            }
            Check(!gm.AutosaveEnabled&&SaveSystem.CurrentPath==slotPath&&!File.Exists(slotPath),"saves OFF/no file throughout");
        }
    }
}
#endif
