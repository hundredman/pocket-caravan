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
    public sealed class GoryeoHudCartLabelsReview : MonoBehaviour
    {
        const string Out="ArtReview/Goryeo-Nameplate-Outline-20261107/";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        [Serializable] class LayoutSnapshot { public string[] names; public Rect[] rectangles, protectedArt; public Vector2 origin; }
        [Serializable] class Scope { public ScopeMap[] maps; }
        [Serializable] class ScopeMap { public string id; }
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
        bool legacyPortrait;
        void OnGUI()
        {
            if(!legacyPortrait)return;UIStyle.Begin();GUI.depth=-2000;CuteUI.FramedCircle(4,5,36);
            var pc=PlayerController.Instance;var sr=pc.anim.GetComponent<SpriteRenderer>();var tr=sr.sprite.rect;
            float w=Mathf.Min(tr.width,28),h=Mathf.Min(tr.height*.6f,28);
            CuteUI.Sprite(sr.sprite,new Rect(4+(36-w)/2,9,w,h),new Rect((1-w/tr.width)/2,1-h/tr.height,w/tr.width,h/tr.height),sr.flipX);
        }
        IEnumerator Frames(){for(int i=0;i<4;i++)yield return null;yield return new WaitForEndOfFrame();}
        Color32[] PortraitPixels(){var all=lastCapture.GetPixels32();var result=new Color32[36*36];for(int y=0;y<36;y++)for(int x=0;x<36;x++)result[y*36+x]=all[(Screen.height-41+y)*Screen.width+4+x];return result;}
        bool Equal(Color32[] a,Color32[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(!a[i].Equals(b[i]))return false;return true;}
        bool EqualFace(Color32[] a,Color32[] b)
        {
            // 기존 코드 오버레이는 반투명 원형 테두리를 두 번 그린다. 얼굴 영역만 원본과 비교한다.
            for(int y=4;y<19;y++)for(int x=10;x<26;x++)if(!a[(35-y)*36+x].Equals(b[(35-y)*36+x]))return false;
            return true;
        }
        void NearBoard(ContractBoard board)
        {
            var pc=PlayerController.Instance;pc.enabled=true;pc.Motor.Teleport(board.transform.position+Vector3.down*2-(Vector3)pc.Motor.feetOffset,Vector2Int.up);
            var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.orthographicSize=8.4375f;cam.transform.position=board.transform.position+Vector3.down*2+new Vector3(0,0,-10);
        }
        void CheckBoard(GoryeoPlaceLabels labels)
        {
            Check(labels.BoardRects.Count>0,"real board art protection exists");
            if(PlayerController.Instance.TalkTarget is ContractBoard)
            {
                var prompt=(Rect)typeof(GoryeoPlaceLabels).GetField("compactPrompt",Flags).GetValue(labels);
                Check(prompt.height==12 && prompt.width<40,"Space-only prompt has no verb or box padding");
            }
            var style=(GUIStyle)typeof(GoryeoPlaceLabels).GetField("nameStyle",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            Check(style.font==UIStyle.SmallFont&&style.fontSize==UIStyle.SmallSize,"nameplates retain original compact font size with white outline");
            Check(Vector4.Distance(style.normal.textColor,UIStyle.Lin(CuteUI.Text))<.00001f,"nameplates use dark text core");
            bool pairs=true;for(int i=0;i<labels.DrawnRects.Count;i++)for(int j=0;j<i;j++)pairs &= !labels.DrawnRects[i].Overlaps(labels.DrawnRects[j]);
            Check(pairs,"compact text labels do not overlap each other");
            bool artwork=true;foreach(var name in labels.DrawnRects)foreach(var art in labels.ProtectedAssetRects)artwork &= !name.Overlaps(art);
            Check(artwork,"name extents avoid foreground sprites and object/above tile cells");
            foreach(var rect in labels.DrawnRects)Check(rect.height<=UIStyle.SmallSize+5,"text-only label has minimal vertical padding");
            bool clear=true;foreach(var a in labels.DrawnRects)foreach(var b in labels.BoardRects)clear &= !a.Overlaps(b);
            Check(clear,"world names do not cover board artwork");Check(labels.DrawnNames.Contains("의뢰 게시판"),"board name still visible");
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);var gm=GameManager.I;Check(gm!=null&&!gm.AutosaveEnabled,"saves OFF bootstrap");
            var login=FindFirstObjectByType<NetLogin>();if(login!=null)Call(login,"PlayAlone");
            Set(gm,"firstSceneLoaded",true);SaveSystem.UseSlot("codex_hud_cart_"+Guid.NewGuid().ToString("N")+".json");slotPath=SaveSystem.CurrentPath;
            originalKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            yield return Size(480,270);yield return Load("Goryeo/Naru");gm.Clock.minuteOfDay=720;
            var pc=PlayerController.Instance;pc.enabled=false;var originalOrigin=gm.Player.origin;var originalFrames=pc.anim.frames;
            var source=typeof(CuteUI).GetMethod("PortraitSource",BindingFlags.NonPublic|BindingFlags.Static);
            foreach(var guid in UnityEditor.AssetDatabase.FindAssets("t:OriginData"))
            { var origin=UnityEditor.AssetDatabase.LoadAssetAtPath<OriginData>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            if(origin==null)continue;
            /* origin iteration */
            if(origin.playerFrames!=null&&origin.playerFrames.Length>=16)
            {
                gm.Player.origin=origin;pc.anim.frames=origin.playerFrames;
                pc.anim.facing=Vector2Int.down;pc.anim.walking=false;yield return Frames();legacyPortrait=true;yield return Photo("portrait-before-"+origin.id);var legacyPixels=PortraitPixels();legacyPortrait=false;
                Color32[] first=null;int index=0;
                foreach(var facing in new[]{Vector2Int.down,Vector2Int.up,Vector2Int.left,Vector2Int.right})
                {
                    pc.anim.facing=facing;yield return Frames();yield return Photo("portrait-after-"+origin.id+"-"+index);
                    Check(source.Invoke(null,null)==origin.playerFrames[0],"portrait stays front for "+origin.id+" "+facing);
                    var pixels=PortraitPixels();if(origin.id=="arabia")Check(EqualFace(legacyPixels,pixels),"fennec uses original face RGBA framing (frame shadow excluded) "+facing);if(first==null)first=pixels;else Check(Equal(first,pixels),"portrait RGBA unchanged by facing "+origin.id+" "+facing);index++;
                }
            }
            }
            gm.Player.origin=originalOrigin;pc.anim.frames=originalFrames;gm.Player.vehicle=gm.Db.vehicles.Find(v=>v.id=="handcart");Check(gm.Player.vehicle!=null,"existing handcart vehicle");
            var body=pc.anim.GetComponent<SpriteRenderer>();var carry=pc.GetComponent<CarryVisual>();var cart=(Transform)carry.GetType().GetField("cart",Flags).GetValue(carry);
            foreach(var facing in new[]{Vector2Int.down,Vector2Int.up,Vector2Int.left,Vector2Int.right})
            {
                pc.Motor.Teleport(pc.transform.position,facing);pc.anim.facing=facing;yield return Frames();
                var sr=cart.GetComponent<SpriteRenderer>();Check(sr.sortingOrder<body.sortingOrder,"player drawn above own cart "+facing);
                foreach(var child in cart.GetComponentsInChildren<SpriteRenderer>(true))Check(child.sortingOrder<body.sortingOrder,"player above cart/cargo "+child.name+" "+facing);
                var at=cart.position;yield return Frames();Check(cart.position==at,"idle player cart stationary "+facing);
            }
            pc.Motor.Teleport(pc.transform.position,Vector2Int.up);pc.anim.facing=Vector2Int.up;yield return Frames();yield return Photo("01-player-cart-north");
            var board=FindFirstObjectByType<ContractBoard>();NearBoard(board);yield return Frames();yield return Photo("02-board");var labels=MapRoot.Current.GetComponent<GoryeoPlaceLabels>();CheckBoard(labels);
            var baseline=labels.DrawnRects.ToArray();var names=labels.DrawnNames.ToArray();var oldOrigin=(Vector2)typeof(GoryeoPlaceLabels).GetField("guiOrigin",Flags).GetValue(labels);
            File.WriteAllText(Out+"pan-baseline.json",JsonUtility.ToJson(new LayoutSnapshot{names=names,rectangles=baseline,protectedArt=labels.ProtectedAssetRects.ToArray(),origin=oldOrigin},true));
            for(int step=0;step<24;step++)
            {
                Camera.main.transform.position+=new Vector3(.017f,.013f,0);yield return Frames();
                var origin=(Vector2)typeof(GoryeoPlaceLabels).GetField("guiOrigin",Flags).GetValue(labels);int expectedCount=0;
                File.WriteAllText(Out+"pan-layout-"+step.ToString("00")+".json",JsonUtility.ToJson(new LayoutSnapshot{names=labels.DrawnNames.ToArray(),rectangles=labels.DrawnRects.ToArray(),protectedArt=labels.ProtectedAssetRects.ToArray(),origin=origin},true));
                for(int i=0;i<baseline.Length;i++)
                {
                    var expected=baseline[i];expected.position+=origin-oldOrigin;
                    bool visible=expected.x>=4 && expected.xMax<=UIStyle.VirtualWidth-4 && expected.y>=25 && expected.yMax<=UIStyle.VirtualHeight-44;
                    bool present=false;for(int j=0;j<labels.DrawnNames.Count;j++)if(labels.DrawnNames[j]==names[i]&&Vector2.Distance(labels.DrawnRects[j].position,expected.position)<.1f)present=true;
                    if(visible){expectedCount++;Check(present,"camera-independent label placement "+step+" "+names[i]);}
                    else Check(!present,"only viewport-clipped label hides "+step+" "+names[i]);
                }
                for(int j=0;j<labels.DrawnNames.Count;j++)
                {
                    var previous=labels.DrawnRects[j];previous.position-=origin-oldOrigin;
                    bool known=false;for(int i=0;i<names.Length;i++)if(names[i]==labels.DrawnNames[j]&&Vector2.Distance(baseline[i].position,previous.position)<.1f)known=true;
                    if(known)continue;
                    Check(previous.x<4||previous.xMax>UIStyle.VirtualWidth-4||previous.y<25||previous.yMax>UIStyle.VirtualHeight-44,"new label enters only from viewport edge "+step+" "+labels.DrawnNames[j]);
                }
                CheckBoard(labels);yield return Photo("pan-"+step.ToString("00"));
            }
            yield return Load("Transit/transit_woodtown");pc=PlayerController.Instance;pc.enabled=false;var life=MapRoot.Current.GetComponent<GoryeoVillageLife>();Check(life!=null&&life.Root!=null,"production village life installed");
            int carts=0;foreach(var sr in life.Root.GetComponentsInChildren<SpriteRenderer>(true))if(sr.gameObject.name=="cart")
            {
                carts++;life.Tick(2);var at=sr.transform.position;life.Tick(150);Check(sr.transform.position==at,"unattended village cart does not move with time");
                Check(sr.sortingOrder<pc.anim.GetComponent<SpriteRenderer>().sortingOrder,"village cart below player");
                foreach(var npc in life.Root.GetComponentsInChildren<GoryeoVisualWalker>())Check(sr.sortingOrder<npc.GetComponent<SpriteRenderer>().sortingOrder,"village cart below walking NPC");
                var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.orthographicSize=8.4375f;cam.transform.position=at+new Vector3(0,0,-10);pc.Motor.Teleport(at-(Vector3)pc.Motor.feetOffset,Vector2Int.down);
            }
            Check(carts>0,"real unattended cart covered");yield return Photo("03-village-cart");
            board=FindFirstObjectByType<ContractBoard>();NearBoard(board);yield return Frames();yield return Photo("04-central-board");CheckBoard(MapRoot.Current.GetComponent<GoryeoPlaceLabels>());
            var scope=JsonUtility.FromJson<Scope>(Resources.Load<TextAsset>("Goryeo/PlaceLabels").text);Check(scope.maps.Length==13,"all thirteen approved label maps covered");
            foreach(var row in scope.maps)
            {
                yield return Load(row.id);gm.Clock.minuteOfDay=720;pc=PlayerController.Instance;
                board=FindFirstObjectByType<ContractBoard>();
                if(board!=null)NearBoard(board);
                else
                {
                    pc.enabled=false;var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.orthographicSize=8.4375f;cam.transform.position=pc.transform.position+new Vector3(0,0,-10);
                }
                yield return Frames();yield return Photo("map-"+row.id);
                labels=MapRoot.Current.GetComponent<GoryeoPlaceLabels>();Check(labels!=null&&labels.OwnsOriginal,"scoped labels own map "+row.id);
                bool clear=true;foreach(var name in labels.DrawnRects)foreach(var art in labels.ProtectedAssetRects)clear &= !name.Overlaps(art);
                Check(clear,"labels avoid protected foreground art "+row.id);
                foreach(var rect in labels.DrawnRects)Check(rect.height<=UIStyle.SmallSize+5,"minimal label padding "+row.id);
                if(board!=null)CheckBoard(labels);
            }
            Check(!gm.AutosaveEnabled&&!File.Exists(slotPath),"preview never writes save");
        }
    }
}
#endif
