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
    public sealed class GoryeoUILayoutReview : MonoBehaviour
    {
        const string Out = "ArtReview/Goryeo-UI1-Layout-20261106/";
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
        void PickBounds(StartScreen menu,int index)
        {
            var view=(Rect)Get(menu,"pickViewport");var footer=(Rect)Get(menu,"pickFooter");var scroll=(Vector2)Get(menu,"pickScroll");
            var cards=(List<Rect>)Get(menu,"pickRects");var c=cards[index];c.position+=view.position-scroll;
            Check(Inside(new Rect(0,0,UIStyle.VirtualWidth,UIStyle.VirtualHeight),view)&&Inside(new Rect(0,0,UIStyle.VirtualWidth,UIStyle.VirtualHeight),footer),"picker viewport/footer inside screen "+index);
            Check(view.yMax<=footer.yMin,"picker and footer never overlap "+index);
            Check(Inside(view,c),"selected original-sized card fully visible "+index);
            Check(cards.Count==4&&Mathf.Abs(c.width-190)<.1&&Mathf.Abs(c.height-107)<.1,"four unchanged card dimensions "+index);
        }
        void DialogueBounds(DialogueUI d,int expected,bool scrollExpected=false)
        {
            var window=(Rect)Get(d,"lastWindow");var view=(Rect)Get(d,"choiceViewport");var rows=(List<Rect>)Get(d,"choiceRects");
            var screen=new Rect(0,0,UIStyle.VirtualWidth,UIStyle.VirtualHeight);
            Check(Inside(screen,window)&&Inside(window,view),"dialogue window and choice view inside screen "+expected);
            Check(rows.Count==expected,"every choice has a rendered button "+expected);
            bool stacked=rows.Count>0&&rows[0].x<1;
            foreach(var row in rows)
                Check(stacked?row.xMin>=0&&row.xMax<=view.width:Inside(view,row),"choice button horizontal bounds "+expected);
            if(stacked&&!scrollExpected)Check(rows[rows.Count-1].yMax<=view.height+.6f,"all choices visible without scroll "+expected);
            if(stacked&&scrollExpected)Check(rows[rows.Count-1].yMax>view.height,"long choice list scrolls within bounded window");
        }
        IEnumerator Settle(){for(int i=0;i<8;i++)yield return null;yield return new WaitForEndOfFrame();}
        void CloseDialog(){Call(FindFirstObjectByType<DialogueUI>(),"Close");UIState.Release();}
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);var gm=GameManager.I;Check(gm!=null&&!gm.AutosaveEnabled,"saves-OFF bootstrap");
            var login=FindFirstObjectByType<NetLogin>();if(login!=null)Call(login,"PlayAlone");
            Set(gm,"firstSceneLoaded",true);SaveSystem.UseSlot("codex_ui1_"+Guid.NewGuid().ToString("N")+".json");slotPath=SaveSystem.CurrentPath;
            originalKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            yield return Size(480,270);yield return Load("Goryeo/Naru");
            var menu=FindFirstObjectByType<StartScreen>();var choices=StartChoices.Get();Check(choices.choices.Count==4,"four real origin choices");
            // Exercise the actual directional-input route in both rows, then confirm each real origin.
            for(int i=0;i<4;i++) {
                yield return Load("Goryeo/Naru");StartScreen.Open(false);yield return Settle();
                if(i%2==1)yield return Keypress(Key.D);if(i>=2)yield return Keypress(Key.S);yield return Settle();
                Check((int)Get(menu,"pick")==i,"ordinary directional selection "+i);PickBounds(menu,i);
                yield return Photo("01-origin-"+i);
                yield return Keypress(Key.Space);
                for(int frame=0;frame<300&&(gm.IsTravelling||SceneManager.GetActiveScene().name!=choices.choices[i].startScene);frame++)yield return null;
                Check(!StartScreen.IsOpen&&gm.Player.origin==choices.choices[i].origin&&SceneManager.GetActiveScene().name==choices.choices[i].startScene,"Space starts selected original scene/origin "+i);
                Check(!gm.AutosaveEnabled&&SaveSystem.CurrentPath==slotPath,"origin transition retains saves-OFF isolated slot "+i);Quiet();
            }
            yield return Load("Goryeo/Naru");StartScreen.Open(true);yield return Settle();
            int beforeMoney=gm.Player.money;var beforeOrigin=gm.Player.origin;
            yield return Keypress(Key.S);yield return Keypress(Key.Space);yield return Settle();
            Check(Get(menu,"mode").ToString()=="Pick","saved menu new-game entry opens picker without starting yet");
            yield return Keypress(Key.S);yield return Settle();PickBounds(menu,2);yield return Photo("01b-picker-back");
            yield return Keypress(Key.Escape);Check(Get(menu,"mode").ToString()=="Menu","Esc from picker returns to saved menu");
            Check(gm.Player.money==beforeMoney&&gm.Player.origin==beforeOrigin,"back navigation never starts a new game");
            typeof(StartScreen).GetField("mode",Flags).SetValue(menu,Enum.Parse(typeof(StartScreen).GetField("mode",Flags).FieldType,"Closed"));UIState.Release();
            yield return Load("Goryeo/Naru");yield return Size(960,540);StartScreen.Open(false);yield return Settle();yield return Keypress(Key.S);yield return Keypress(Key.D);yield return Settle();PickBounds(menu,3);yield return Photo("02-picker-960");
            yield return Keypress(Key.W);yield return Settle();Check((int)Get(menu,"pick")==1,"back to top row updates selection");PickBounds(menu,1);
            // A narrower supported viewport uses one column, keeping the same cards and up/down route.
            typeof(StartScreen).GetField("mode",Flags).SetValue(menu,Enum.Parse(typeof(StartScreen).GetField("mode",Flags).FieldType,"Closed"));UIState.Release();
            yield return Size(360,270);StartScreen.Open(false);yield return Settle();
            for(int i=0;i<4;i++){if(i>0)yield return Keypress(Key.S);yield return Settle();Check((int)Get(menu,"pick")==i,"one-column down selection "+i);PickBounds(menu,i);}
            yield return Photo("03-picker-narrow");
            typeof(StartScreen).GetField("mode",Flags).SetValue(menu,Enum.Parse(typeof(StartScreen).GetField("mode",Flags).FieldType,"Closed"));UIState.Release();yield return Size(480,270);
            // Real four-choice peddler content: observe only, never buy.
            int money=gm.Player.money;Peddler.Open();yield return Settle();var dialogue=FindFirstObjectByType<DialogueUI>();Set(dialogue,"pageIndex",Mathf.Max(0,dialogue.PageCount-1));yield return Settle();DialogueBounds(dialogue,4);yield return Photo("04-peddler-four-choices");CloseDialog();Check(gm.Player.money==money,"preview peddler never purchased");
            foreach(int count in new[]{1,2,4,8}) {
                int first=0,last=0;var list=new DialogueUI.Choice[count];
                for(int i=0;i<count;i++){int k=i;list[i]=new DialogueUI.Choice("선택 "+(i+1)+" · 검토용 안내",()=>{if(k==0)first++;if(k==count-1)last++;});}
                DialogueUI.Show("선택지 검토","선택지 내용과 단축키를 확인합니다.",list);yield return Settle();DialogueBounds(dialogue,count,count==8);
                if(count==8) {
                    yield return Photo("05-dialogue-scroll-eight");Set(dialogue,"choiceScroll",new Vector2(0,10000));yield return Settle();
                    var rows=(List<Rect>)Get(dialogue,"choiceRects");var view=(Rect)Get(dialogue,"choiceViewport");var scroll=(Vector2)Get(dialogue,"choiceScroll");var lastRow=rows[rows.Count-1];lastRow.position+=view.position-scroll;
                    Check(Inside(view,lastRow),"final choice can be revealed by actual GUI scroll view clamping");yield return Photo("05b-dialogue-scroll-bottom");
                }
                yield return Keypress(Key.Space);Check(first==1&&(count==1?last==1:last==0),"Space invokes original first callback once "+count);
                DialogueUI.Show("선택지 검토","취소는 마지막 선택지를 고릅니다.",list);yield return Settle();yield return Keypress(Key.Escape);
                Check(last==(count==1?2:1),"Esc invokes original last callback once "+count);
            }
            int picked=0;var longChoices=new[]{new DialogueUI.Choice("매우 긴 선택 문구도 보증금과 기한을 빠뜨리지 않고 끝까지 읽을 수 있는지 확인하는 검토 항목",()=>picked++),new DialogueUI.Choice("돌아가기",null)};
            DialogueUI.Show("긴 문구 검토","본문 첫 줄입니다.\n둘째 줄입니다.\n셋째 줄입니다.\n넷째 줄입니다.\n마지막 페이지에서만 선택합니다.",longChoices);
            yield return Settle();Check(dialogue.PageCount>1&&!dialogue.ChoicesVisible,"multipage dialogue hides choices before last page");int page=dialogue.PageIndex;yield return Keypress(Key.Escape);Check(dialogue.PageIndex==page+1&&picked==0,"Esc before last page advances without callback");
            while(!dialogue.ChoicesVisible)yield return Keypress(Key.Space);yield return Settle();DialogueBounds(dialogue,2);yield return Photo("06-long-multipage-choices");yield return Keypress(Key.Space);Check(picked==1,"long-label first callback stays intact");
            DialogueUI.Show("줄바꿈 선택지","두 줄 선택지 내용도 모두 보입니다.",new DialogueUI.Choice("첫 줄\n보증금 100냥",null),new DialogueUI.Choice("돌아가기",null));
            yield return Settle();DialogueBounds(dialogue,2);Check(((List<Rect>)Get(dialogue,"choiceRects"))[0].height>18,"explicit newline choice gets sufficient height");CloseDialog();
            DialogueUI.Show("일반 대화","선택지가 없는 대화입니다.");yield return Settle();yield return Keypress(Key.Space);Check(!UIState.Blocking,"plain zero-choice dialogue still closes with Space");
            yield return Size(960,540);Peddler.Open();yield return Settle();Set(dialogue,"pageIndex",Mathf.Max(0,dialogue.PageCount-1));yield return Settle();DialogueBounds(dialogue,4);yield return Photo("07-peddler-960");CloseDialog();
            Check(!gm.AutosaveEnabled&&SaveSystem.CurrentPath==slotPath&&!File.Exists(slotPath),"no player save written throughout layout QA");
        }
    }
}
#endif
