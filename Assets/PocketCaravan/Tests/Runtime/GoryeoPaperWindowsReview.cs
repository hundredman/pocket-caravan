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
    public sealed class GoryeoPaperWindowsReview : MonoBehaviour
    {
        const string Out = "ArtReview/Goryeo-UI1-Windows-20261106/";
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
        object Get(object obj,string field)=>obj.GetType().GetField(field,Flags).GetValue(obj);
        void Set(object obj,string field,object value)=>obj.GetType().GetField(field,Flags).SetValue(obj,value);
        object Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,Flags).Invoke(obj,args);
        bool Inside(Rect outer,Rect inner)=>inner.xMin>=outer.xMin-.6f&&inner.yMin>=outer.yMin-.6f&&inner.xMax<=outer.xMax+.6f&&inner.yMax<=outer.yMax+.6f;
        void Window(Component obj,string field) {Check(Inside(new Rect(0,0,UIStyle.VirtualWidth,UIStyle.VirtualHeight),(Rect)Get(obj,field)),"window inside viewport "+obj.GetType().Name+" "+Screen.width);}
        IEnumerator Settle(){for(int i=0;i<8;i++)yield return null;yield return new WaitForEndOfFrame();}
        string Cargo(PlayerState p) {var list=new List<string>();foreach(var kv in p.cargo.Items)list.Add(kv.Key.id+":"+kv.Value+":"+p.cargo.TotalCost(kv.Key));list.Sort();return string.Join("|",list);}
        void InventoryBounds(InventoryUI bag)
        {
            Window(bag,"lastWindow");var window=(Rect)Get(bag,"lastWindow");
            foreach(string name in new[]{"itemViewport","detailViewport","contractViewport"})Check(Inside(window,(Rect)Get(bag,name)),"inventory region inside window "+name);
            var items=(List<KeyValuePair<ItemData,int>>)Get(bag,"rows");
            if(items.Count>0){int selected=(int)Get(bag,"selected");var row=((List<Rect>)Get(bag,"itemRects"))[selected];var view=(Rect)Get(bag,"itemViewport");row.position+=view.position-(Vector2)Get(bag,"itemScroll");Check(Inside(view,row),"selected inventory item visible "+selected);}
            Check(((Rect)Get(bag,"itemViewport")).xMax<((Rect)Get(bag,"detailViewport")).xMin,"item grid and details do not overlap");
        }
        void GuideBounds(CommerceGuideUI guide) {Window(guide,"lastWindow");var w=(Rect)Get(guide,"lastWindow");Check(Inside(w,(Rect)Get(guide,"listViewport"))&&Inside(w,(Rect)Get(guide,"pageViewport")),"guide list/article inside window");Check(((Rect)Get(guide,"listViewport")).xMax<((Rect)Get(guide,"pageViewport")).xMin,"guide columns separated");}
        void InnBounds(Inn inn) {Window(inn,"askWindow");var w=(Rect)Get(inn,"askWindow");foreach(string f in new[]{"askTextRect","yesRect","noRect"})Check(Inside(w,(Rect)Get(inn,f)),"inn content inside "+f);Check(((Rect)Get(inn,"askTextRect")).yMax<((Rect)Get(inn,"yesRect")).yMin,"inn copy clear of action buttons");Check(((Rect)Get(inn,"yesRect")).xMax<((Rect)Get(inn,"noRect")).xMin,"inn action buttons separated");}
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);var gm=GameManager.I;Check(gm!=null&&!gm.AutosaveEnabled,"saves-OFF bootstrap");
            var login=FindFirstObjectByType<NetLogin>();if(login!=null)Call(login,"PlayAlone");Set(gm,"firstSceneLoaded",true);
            SaveSystem.UseSlot("codex_paper_windows_"+Guid.NewGuid().ToString("N")+".json");slotPath=SaveSystem.CurrentPath;
            originalKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            yield return Size(480,270);yield return Load("Goryeo/Naru");var p=gm.Player;
            p.money=520;p.credit=2;p.cargo.Clear();p.contracts.Clear();foreach(string id in new[]{"grain","salt","paper"}){var item=gm.ItemById(id);if(item!=null)p.cargo.Add(item,1,10);}
            var bag=gm.GetComponent<InventoryUI>();var pause=gm.GetComponent<PauseMenu>();var guide=gm.GetComponent<CommerceGuideUI>();var inn=FindFirstObjectByType<Inn>();
            string cargo=Cargo(p);int money=p.money;float clock=gm.Clock.TotalHours;
            yield return Keypress(Key.I);yield return Settle();Check(bag.IsOpen&&UIState.Blocking,"I opens inventory");InventoryBounds(bag);yield return Photo("01-inventory");
            yield return Keypress(Key.D);yield return Settle();Check((int)Get(bag,"selected")==1,"inventory real directional selection");InventoryBounds(bag);
            yield return Keypress(Key.I);Check(!bag.IsOpen&&!UIState.Blocking,"I closes inventory");Check(p.money==money&&Cargo(p)==cargo&&gm.Clock.TotalHours==clock,"inventory read-only money/cargo/time");
            p.cargo.Clear();yield return Keypress(Key.I);yield return Settle();InventoryBounds(bag);yield return Photo("02-inventory-empty");yield return Keypress(Key.Escape);
            var originalVehicle=p.vehicle;foreach(var vehicle in gm.Db.vehicles)if(vehicle!=null&&(p.vehicle==null||vehicle.capacity>p.vehicle.capacity))p.vehicle=vehicle;
            var all=new List<ItemData>();foreach(var item in gm.Db.items)if(item!=null&&!item.questItem&&item.icon!=null&&all.Count<18){all.Add(item);p.cargo.Add(item,1,10);}
            Check(all.Count>=15,"many real catalogue items fixture");
            for(int i=0;i<3;i++)p.contracts.Add(new Contract{id=9000+i,fromTown="naru",toTown=i==0?"dakmok":"goryeo_jangdan",cargo=i==0?"긴 배달 짐 이름과 도착지 표시 검토":"검토용 배달 짐",weight=2,reward=i==2?0:35,deadline=gm.Clock.TotalHours+24,finalDeadline=gm.Clock.TotalHours+36,hours=24,condition=1,atonement=i==2,deposit=i==2?100:0,atonementScope=0,atonementTarget=i==2?"goryeo":null});
            cargo=Cargo(p);yield return Keypress(Key.I);yield return Settle();
            for(int i=0;i<all.Count-1;i++){yield return Keypress(Key.D);yield return Settle();InventoryBounds(bag);}
            Check((int)Get(bag,"selected")==all.Count-1,"all catalogue items accessible with ordinary input");Set(bag,"contractScroll",new Vector2(0,10000));yield return Settle();yield return Photo("03-inventory-many-and-contracts");
            yield return Size(360,270);yield return Settle();InventoryBounds(bag);Check((int)Get(bag,"columns")<6,"narrow inventory adapts columns");yield return Photo("04-inventory-narrow");yield return Keypress(Key.Escape);
            Check(Cargo(p)==cargo&&p.money==money,"scroll and resize never mutate cargo or money");p.cargo.Clear();p.contracts.Clear();p.vehicle=originalVehicle;yield return Size(480,270);
            yield return Keypress(Key.Escape);yield return Settle();Check((bool)Get(pause,"open")&&UIState.Blocking,"Escape opens game menu");Window(pause,"lastWindow");yield return Photo("05-game-menu");
            yield return Keypress(Key.I);Check(!bag.IsOpen&&(bool)Get(pause,"open"),"menu blocks inventory opening behind it");yield return Keypress(Key.Escape);Check(!(bool)Get(pause,"open")&&!UIState.Blocking,"Escape closes game menu");
            var loadIssue=typeof(GameManager).GetProperty("LoadIssue");var oldIssue=loadIssue.GetValue(gm);
            loadIssue.SetValue(gm,"검토용 긴 저장 알림: 파일을 복원하지 못했을 때에도 안내문과 게임 메뉴를 화면 안에서 읽을 수 있는지 확인합니다. 기존 파일은 보존됩니다. 이 문구는 실제 오류가 아닌 화면 검토용입니다.");
            yield return Keypress(Key.Escape);yield return Settle();Window(pause,"lastWindow");Check(Inside((Rect)Get(pause,"lastWindow"),(Rect)Get(pause,"issueViewport")),"long load notice scroll region bounded");yield return Photo("06-game-menu-long-notice");yield return Keypress(Key.Escape);loadIssue.SetValue(gm,oldIssue);
            guide.Open();yield return Settle();Check(guide.IsOpen&&UIState.Blocking,"guide opens");GuideBounds(guide);yield return Photo("07-guide");
            int cardCount=guide.Content.cards.Length;for(int i=0;i<cardCount;i++){Set(guide,"selected",i);Set(guide,"pageScroll",new Vector2(0,10000));yield return Settle();GuideBounds(guide);}
            Check(cardCount>0,"all real Goryeo guide entries rendered");yield return Photo("08-guide-bottom");yield return Keypress(Key.Escape);Check(!guide.IsOpen&&!UIState.Blocking,"guide Escape close");
            yield return Size(360,270);guide.Open();yield return Settle();GuideBounds(guide);yield return Photo("09-guide-narrow");guide.Close();yield return Size(480,270);
            p.money=100;Inn.Offer("나루 주막");yield return Settle();Check(UIState.Blocking&&Get(inn,"askHost")!=null,"inn existing Offer opens");InnBounds(inn);yield return Photo("10-inn");Call(inn,"CloseAsk");Check(!UIState.Blocking&&p.money==100,"inn existing cancel callback preserves money");
            p.money=0;Inn.Offer("나루 주막");yield return Settle();Call(inn,"Rest");yield return Settle();Check(p.money==0&&Get(inn,"askNote")!=null&&(float)Get(inn,"fade")<0,"insufficient money retains existing guard");InnBounds(inn);yield return Photo("11-inn-insufficient");Call(inn,"CloseAsk");
            p.money=100;p.stamina=.2f;float before=gm.Clock.TotalHours;Inn.Offer("나루 주막");yield return Settle();Call(inn,"Rest");
            float until=Time.realtimeSinceStartup+6;while((float)Get(inn,"fade")>=0&&Time.realtimeSinceStartup<until)yield return null;
            Check((float)Get(inn,"fade")<0&&!UIState.Blocking,"existing sleep fade finishes");Check(p.money==100-Inn.Price&&Mathf.Abs(gm.Clock.TotalHours-before-Inn.SleepHours)<.01&&p.stamina==1,"existing inn price/time/stamina unchanged");Check(RestBuff.Rested&&Mathf.Abs(RestBuff.Until-gm.Clock.TotalHours-Inn.BuffHours)<.01,"existing rest buff duration unchanged");
            yield return Size(960,540);yield return Keypress(Key.I);yield return Settle();InventoryBounds(bag);yield return Photo("12-inventory-960");yield return Keypress(Key.Escape);
            yield return Load("Venice/venice_rialto");guide.Open();yield return Settle();Check(object.ReferenceEquals(guide.Content,Get(guide,"venice")),"Venice guide still selects existing Venice content");GuideBounds(guide);yield return Photo("13-venice-guide");guide.Close();
            Check(!gm.AutosaveEnabled&&SaveSystem.CurrentPath==slotPath&&!File.Exists(slotPath),"all four windows retained saves-OFF isolated slot");
        }
    }
}
#endif
