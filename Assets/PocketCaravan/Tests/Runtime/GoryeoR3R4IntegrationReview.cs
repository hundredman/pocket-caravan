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
    public sealed class GoryeoR3R4IntegrationReview : MonoBehaviour
    {
        const string Out = "ArtReview/Goryeo-R3-R4-Readable-20261106/";
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
        object Call(object o,string method,params object[] args) => o.GetType().GetMethod(method,Flags).Invoke(o,args);
        void Set(object o,string field,object value)=>o.GetType().GetField(field,Flags).SetValue(o,value);
        void Score(TownData t,int value) {
            var p=GameManager.I.Player;var scope=ReputationPricing.Scope(t);var target=ReputationPricing.Target(t);
            p.reputation.Add(scope,target,value-StandingRecovery.Score(p.reputation,scope,target));
        }
        string Economy(GameManager gm) {
            var d=new SaveData{money=gm.Player.money,credit=gm.Player.credit,day=gm.Clock.day,minute=gm.Clock.minuteOfDay,nextContractId=gm.Contracts.nextId};
            gm.Player.reputation.ExportTo(d);d.tradeActivity=gm.Player.tradeActivity.Export();d.contracts.AddRange(gm.Player.contracts);
            d.boards.AddRange(gm.Contracts.boards);foreach(var t in gm.Db.towns)if(t!=null)d.markets.Add(gm.GetMarket(t).Export());
            d.traders.AddRange(gm.Traders.traders);gm.Quests.Export(d);return JsonUtility.ToJson(d);
        }
        void FailAfter(GameManager gm,Action action,string label) {
            string before=Economy(gm);Check(!SavedEconomyChange.Try(gm,()=>{action();Directory.CreateDirectory(slotPath+".tmp");}),"write failure rejected "+label);
            Directory.Delete(slotPath+".tmp");Check(Economy(gm)==before,"all economic state restored "+label);
        }
        IEnumerator ContinueSaved(GameManager gm)
        {
            var data=SaveSystem.Read();Check(data!=null,"read real checkpoint for Continue");
            // Continue restores saves only at the real startup scene (build index 0).
            // Suppress writes while arranging that scene; keep the on-disk fixture intact.
            Set(gm,"autosave",false);
            yield return Load(SceneUtility.GetScenePathByBuildIndex(0));
            redirectedSession=RegionTestSession.For(MapRoot.Current.region);
            if(redirectedSession!=null) {
                originalSessionSlot=redirectedSession.saveFileName;
                redirectedSession.saveFileName=Path.GetFileName(slotPath);
            }
            Set(gm,"autosave",true);
            gm.Player.money=data.money+7;gm.Player.contracts.Clear(); // prove restoration, not unchanged memory
            Set(gm,"saveToLoad",data);StartScreen.Open(true);
            yield return Keypress(Key.Space);
            for(int i=0;i<240 && (gm.IsTravelling || SceneManager.GetActiveScene().name!=data.scene);i++)yield return null;
            for(int i=0;i<12;i++)yield return null;
            Check(!StartScreen.IsOpen&&SaveSystem.CurrentPath==slotPath&&gm.AutosaveEnabled,"normal Space Continue retains isolated slot and saves enabled");
            Check(gm.Player.money==data.money&&gm.Player.contracts.Count==data.contracts.Count,"Continue restores disk data over deliberately changed memory");
            if(redirectedSession!=null)redirectedSession.saveFileName=originalSessionSlot;
            redirectedSession=null;Quiet();
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);var gm=GameManager.I;Check(gm!=null&&!gm.AutosaveEnabled,"safe saves-OFF bootstrap");
            var login=FindFirstObjectByType<NetLogin>();if(login!=null)Call(login,"PlayAlone");
            typeof(GameManager).GetField("firstSceneLoaded",Flags).SetValue(gm,true);
            SaveSystem.UseSlot("codex_r3r4_"+Guid.NewGuid().ToString("N")+".json");slotPath=SaveSystem.CurrentPath;
            originalKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            yield return Size(480,270);yield return Load("Goryeo/Naru");
            typeof(GameManager).GetField("autosave",Flags).SetValue(gm,true);
            var p=gm.Player;var town=gm.Db.towns.Find(t=>t.id=="naru");var item=gm.ItemById("grain");
            p.money=10000;p.cargo.Clear();p.contracts.Clear();p.credit=-2;p.tradeActivity.Clear();
            gm.Clock.day=1;gm.Clock.minuteOfDay=480;Score(town,0);
            typeof(GameManager).GetProperty("Contracts").SetValue(gm,new ContractManager(gm.Db,gm.Clock,p,null,21));gm.Contracts.InitBoards();
            Check(gm.SaveNow(),"isolated initial checkpoint");
            TownMarket market=null;foreach(var m in FindObjectsByType<TownMarket>(FindObjectsSortMode.None))if(m.town==town)market=m;
            Check(market!=null,"real Naru market");Near(market);Set(market,"selected",item);Set(market,"quantityMode",2);
            // The previously demonstrated requested100/affordable1 exploit must now use the true quantity.
            p.money=gm.GetMarket(town).BuyPrice(item);int real=gm.GetMarket(town).Preview(item,p,100,false).count;
            Check(real==1,"fixture requested100 but affordable1");
            Call(market,"TryHaggle",HaggleStrength.Fair,ReputationPricing.MaxHaggle(ReputationStanding.Neutral),ReputationStanding.Neutral,100);
            Check((HaggleDeal)typeof(TownMarket).GetField("deal",Flags).GetValue(market)==null && p.tradeActivity.HaggleLocked(town.id,1),"no bulk bonus when only one executable item (known deterministic refusal)");
            p.money=100000;p.tradeActivity.Clear();
            var originalVehicle=p.vehicle;var largeVehicle=ScriptableObject.CreateInstance<VehicleData>();largeVehicle.capacity=10000;p.vehicle=largeVehicle;
            var originalMarket=gm.GetMarket(town).Export();var plentiful=gm.GetMarket(town).Export();
            int itemIndex=plentiful.items.IndexOf(item.id);
            if(itemIndex<0){plentiful.items.Add(item.id);plentiful.stocks.Add(1000);}else plentiful.stocks[itemIndex]=1000;
            gm.GetMarket(town).Import(plentiful,gm.ItemById);
            foreach(int requested in new[]{1,19,20,100}) {
                Set(market,"quantityMode",3);Set(market,"customQuantity",requested);
                int n=gm.GetMarket(town).Preview(item,p,requested,false).count;
                Check(n==requested,"real executable quote quantity "+requested);
                var d=new HaggleDeal(town.id,item.id,false,gm.Clock.day,.01f,HaggleStrength.Light,n);Set(market,"deal",d);
                Check((float)Call(market,"DealFor",false,item)>0,"deal valid for executable quote "+requested);
                Set(market,"customQuantity",n==1?2:n-1);
                Check((float)Call(market,"DealFor",false,item)==0,"changed quantity invalidates deal "+requested);
                Set(market,"customQuantity",requested);Set(market,"deal",null);
            }
            // Explicit stock and capacity shortfalls must remove the matching deal too.
            Set(market,"quantityMode",2);Set(market,"deal",new HaggleDeal(town.id,item.id,false,1,.01f,HaggleStrength.Light,100));
            itemIndex=plentiful.items.IndexOf(item.id);plentiful.stocks[itemIndex]=1;gm.GetMarket(town).Import(plentiful,gm.ItemById);
            Check((float)Call(market,"DealFor",false,item)==0,"stock shortfall invalidates larger deal");
            plentiful.stocks[itemIndex]=1000;gm.GetMarket(town).Import(plentiful,gm.ItemById);largeVehicle.capacity=0;
            Check((float)Call(market,"DealFor",false,item)==0,"capacity shortfall invalidates larger deal");
            p.vehicle=originalVehicle;Destroy(largeVehicle);gm.GetMarket(town).Import(originalMarket,gm.ItemById);
            // Insufficient stock/capacity and discounts cannot increase the committed quantity.
            Set(market,"quantityMode",2);p.money=gm.GetMarket(town).BuyPrice(item);
            int agreed=gm.GetMarket(town).Preview(item,p,100,false).count;
            Set(market,"deal",new HaggleDeal(town.id,item.id,false,1,.05f,HaggleStrength.Light,agreed));
            var quote=gm.GetMarket(town).Preview(item,p,agreed,false,.05f);int wallet=p.money;
            int bought=(int)Call(market,"TradeBatch",quote);
            Check(bought==agreed&&p.money==wallet-quote.total,"discounted batch executes exact agreed count and total");
            Check(typeof(TownMarket).GetField("deal",Flags).GetValue(market)==null,"successful batch consumes deal");
            int paid=quote.total;Set(market,"deal",new HaggleDeal(town.id,item.id,true,1,.05f,HaggleStrength.Light,bought));
            var resale=gm.GetMarket(town).Preview(item,p,bought,true,.05f);int before=p.money;
            int sold=(int)Call(market,"TradeBatch",resale);
            Check(sold==bought&&p.money-before<=paid,"recalculated same-market buy/sell profit <= 0 including haggling");
            p.money=5000;p.tradeActivity.Clear();p.cargo.Clear();Set(market,"deal",null);Score(town,3000);
            market.Interact(PlayerController.Instance);yield return Photo("01-market-haggle-top");
            Set(market,"haggleOpen",true);yield return Photo("02-market-haggle-controls");yield return Keypress(Key.Escape);
            // Actual negative save and recovery controls.
            Score(town,-150);p.money=0;Check(gm.SaveNow(),"negative money0 credit-2 fixture saved");
            var neg=SaveSystem.Read();Check(neg!=null&&neg.money==0&&neg.credit==-2,"negative fixture read from disk");
            var facilities=FindFirstObjectByType<TownFacilitiesUI>();Near(market);TownFacilitiesUI.Show(town);yield return Photo("03-recovery-negative");
            var scope=ReputationPricing.Scope(town);var target=ReputationPricing.Target(town);
            Call(facilities,"StartChore",gm,p,0,scope,target);
            Check(p.tradeActivity.ActiveChore?.town==town.id&&p.money==0,"money0 cargo0 credit-2 can take chore");
            Check(SaveSystem.Read().tradeActivity.chores.Count==1,"chore persisted immediately");yield return Photo("04a-recovery-chore-accepted");yield return Keypress(Key.Escape);
            var board=Board(town.id);Near(board);board.Interact(PlayerController.Instance);yield return Photo("04-board-chore-report");
            string state=Economy(gm);Directory.CreateDirectory(slotPath+".tmp");
            string denied=(string)Call(board,"ReportChore",gm,p);Directory.Delete(slotPath+".tmp");
            Check(denied==SavedEconomyChange.Failure&&Economy(gm)==state,"preflight failure changes nothing");
            FailAfter(gm,()=>{p.tradeActivity.FinishChore(town.id);gm.Clock.Advance(1);StandingRecovery.Raise(p.reputation,scope,target,75);},"chore including hourly market traders contracts");
            float hour=gm.Clock.TotalHours;string done=(string)Call(board,"ReportChore",gm,p);
            Check(p.tradeActivity.ActiveChore==null&&StandingRecovery.Score(p.reputation,scope,target)==-75&&Mathf.Abs(gm.Clock.TotalHours-hour-1)<.001,"chore once +75 and exactly1 gamehour");
            Check(((string)Call(board,"ReportChore",gm,p)).Contains("없습니다")&&StandingRecovery.Score(p.reputation,scope,target)==-75,"duplicate chore report cannot reward");
            yield return Keypress(Key.Escape);Near(market);TownFacilitiesUI.Show(town);p.money=500;yield return Photo("03b-recovery-funded");
            FailAfter(gm,()=>{var donation=StandingRecovery.Donate(p.reputation,scope,target,100,p.money);p.money-=donation.paid;},"donation money and reputation");
            Call(facilities,"Donate",gm,p,100,scope,target);
            Check(p.money==400&&StandingRecovery.Score(p.reputation,scope,target)==-25,"donation pays100 gains50");
            Call(facilities,"Donate",gm,p,200,scope,target);
            Check(p.money==350&&StandingRecovery.Score(p.reputation,scope,target)==0,"donation takes only50 and stops neutral");
            yield return Photo("05-recovery-complete");yield return Keypress(Key.Escape);
            // Atonement offer, deposit, rollback, delivery, cooldown and duplicate submission.
            Score(town,-150);p.money=5000;
            var c=gm.Contracts.Available(town.id).Find(x=>x.atonement);Check(c!=null,"atonement offered immediately without waiting an hour");
            Near(board);board.Interact(PlayerController.Instance);Set(board,"scroll",new Vector2(0,10000));yield return Photo("06-board-atonement-offer");
            int deposit=c.deposit;int id=c.id;
            int originalDeposit=c.deposit;c.deposit=-1;bool invalidRejected=false;
            try{c.ValidateRecovery();}catch(System.IO.InvalidDataException){invalidRejected=true;}
            c.deposit=originalDeposit;Check(invalidRejected,"negative deposit save rejected");

            FailAfter(gm,()=>Check(gm.Contracts.Accept(town.id,c),"failure fixture accepts atonement"),"accept deposit board active-contract and deadlines");
            Check(gm.Contracts.Available(town.id).Contains(c)&&!p.contracts.Contains(c),"original offer reference retained after rollback");
            string accepted=(string)Call(board,"AcceptSaved",c);Set(board,"message",accepted);
            yield return Photo("06a-board-atonement-accepted");
            Check(p.contracts.Contains(c)&&p.money==5000-deposit,"actual board accepts deposit exactly once");
            Check(SaveSystem.Read().contracts.Exists(x=>x.id==id&&x.atonement&&x.deposit==deposit),"accepted deposit contract on disk");
            Check(!gm.Contracts.Accept(town.id,c),"duplicate accept rejected");yield return Keypress(Key.Escape);
            yield return ContinueSaved(gm);c=p.contracts.Find(x=>x.id==id);
            Check(c!=null&&c.atonement&&p.money==5000-deposit,"normal Continue restores accepted deposit and contract exactly once");
            string destination=WorldIndex.Get().MapOf("market",c.toTown);yield return Load(destination);board=Board(c.toTown);Near(board);board.Interact(PlayerController.Instance);
            FailAfter(gm,()=>gm.Contracts.Deliver(c,c.toTown),"delivery deposit reputation credit cooldown quest");
            int beforeCap=p.money;p.money=int.MaxValue;
            var over=gm.Contracts.Deliver(c,c.toTown);
            Check(p.contracts.Contains(c)&&p.money==int.MaxValue,"deposit refund cannot overflow wallet or consume contract");p.money=beforeCap;
            int credit=p.credit;string delivered=(string)Call(board,"DeliverSaved",c);
            Set(board,"message",delivered);yield return Photo("06b-board-atonement-delivered");
            Check(!p.contracts.Contains(c)&&p.money==5000&&p.credit==credit+1,"atonement delivers returns exact deposit and credit");
            Check(StandingRecovery.Score(p.reputation,scope,target)==0,"atonement capped exactly neutral");
            float reopen=p.tradeActivity.AtonementReopensAt((int)scope,target);
            Check(reopen>=gm.Clock.TotalHours+23.99f,"atonement24h cooldown saved");
            int cash=p.money;gm.Contracts.Deliver(c,c.toTown);Check(p.money==cash,"duplicate delivery no money");
            var saved=SaveSystem.Read();Check(saved!=null&&saved.contracts.Count==0&&saved.tradeActivity.atonementReopen.Count>0,"delivery persisted completed state and reopen");
            yield return Keypress(Key.Escape);yield return ContinueSaved(gm);board=Board();Near(board);board.Interact(PlayerController.Instance);
            Check(p.money==cash&&p.tradeActivity.AtonementReopensAt((int)scope,target)==reopen,"normal Continue preserves cooldown without duplicate return");
            yield return Photo("07-board-atonement-complete");yield return Keypress(Key.Escape);
            // Loss/expiry/partial paths and independence are exercised on actual contracts, not rule-only simulations.
            var other=gm.Db.towns.Find(t=>t.id=="transit_woodtown");Score(other,-100);Score(town,-100);
            Check(StandingRecovery.Raise(p.reputation,scope,target,250)==100&&StandingRecovery.Score(p.reputation,ReputationPricing.Scope(other),ReputationPricing.Target(other))==-100,"civilization recovery leaves central town unchanged");
            var another=gm.Db.towns.Find(t=>t.id=="transit_ferry");Score(another,-120);
            StandingRecovery.Raise(p.reputation,ReputationPricing.Scope(other),ReputationPricing.Target(other),75);
            Check(StandingRecovery.Score(p.reputation,ReputationPricing.Scope(another),ReputationPricing.Target(another))==-120,"central A recovery leaves B unchanged");
            foreach(string failure in new[]{"lost","expired","partial"}) {
                Score(town,-100);p.tradeActivity.Clear();p.contracts.Clear();
                foreach(var b in gm.Contracts.boards)b.list.RemoveAll(x=>x.atonement);
                c=gm.Contracts.Available(town.id).Find(x=>x.atonement);Check(c!=null,"offer for "+failure);p.money=5000;
                Check(gm.Contracts.Accept(town.id,c),"accept for "+failure);int afterPay=p.money;credit=p.credit;
                if(failure=="lost")gm.Contracts.Lose(c);
                else if(failure=="expired") {gm.Clock.day+=(int)Math.Ceiling((c.finalDeadline-gm.Clock.TotalHours)/24)+1;gm.Contracts.OnHour();}
                else {c.condition=.6f;var result=gm.Contracts.Deliver(c,c.toTown);Check(result.success&&result.paid==c.deposit,"partial returns deposit without extra cash");}
                Check(!p.contracts.Contains(c)&&StandingRecovery.Score(p.reputation,scope,target)==(failure=="partial"?-25:-100),"reputation outcome "+failure);
                Check(p.money==(failure=="partial"?5000:afterPay)&&p.credit==(failure=="partial"?credit:credit-1),"money and credit outcome "+failure);
                Check(gm.SaveNow(),"checkpoint "+failure);
            }
            var old=new TradeActivity.Save{schemaVersion=1,haggleLocks=null,bans=null,chores=null,atonementReopen=null};var fromOld=new TradeActivity();fromOld.Import(old);
            Check(fromOld.ActiveChore==null&&!fromOld.HaggleLocked(town.id,1),"old save optional fields absent accepted");
            Check(File.Exists(slotPath),"real isolated save exercised");
        }
    }
}
#endif
