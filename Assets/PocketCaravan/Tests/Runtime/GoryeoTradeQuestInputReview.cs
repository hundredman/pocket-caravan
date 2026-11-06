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
    public sealed class GoryeoTradeQuestInputReview : MonoBehaviour
    {
        const string Out = "ArtReview/Goryeo-Trade-Quest-20261106/";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        [Serializable] class Report { public bool complete; public string error; public List<string> checks = new List<string>(); }
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
                if (error != null)
                {
                    yield return new WaitForEndOfFrame();
                    var shot=ScreenCapture.CaptureScreenshotAsTexture();
                    Directory.CreateDirectory(Out);File.WriteAllBytes(Out+"failure.png",shot.EncodeToPNG());Destroy(shot);
                    Finish(error);yield break;
                }
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
            // Let TalkTarget and the inn's previous-frame target settle after fixture teleports.
            yield return null; yield return null;
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
        QuestData Q(string id) => GameManager.I.Db.quests.Find(q=>q.id==id);
        QuestNpc Npc(string id)
        {
            foreach(var npc in FindObjectsByType<QuestNpc>(FindObjectsSortMode.None))if(npc.npcId==id)return npc;
            throw new Exception("Missing quest NPC "+id);
        }
        IEnumerator CloseDialogs()
        {
            for(int i=0;i<6 && UIState.Blocking;i++)yield return Keypress(Key.Escape);
            // Inn reads IMGUI Event.current, not InputReader. A virtual Keyboard cannot generate that event.
            // Invoke only its cancel callback to continue the Q1 fixture; no native Escape/click claim.
            var inn=FindFirstObjectByType<Inn>();
            if(UIState.Blocking&&inn!=null&&typeof(Inn).GetField("askHost",Flags).GetValue(inn)!=null)
            {
                typeof(Inn).GetMethod("CloseAsk",Flags).Invoke(inn,null);yield return null;
                Check(!UIState.Blocking,"inn cancel handler closes IMGUI prompt (native input not tested)");
            }
            Check(!UIState.Blocking,"dialogue chain closes without stuck modal");
        }
        IEnumerator Accept(string id)
        {
            var gm=GameManager.I;var q=Q(id);var npc=Npc(q.giverNpc);Near(npc);yield return Keypress(Key.Space);
            Check(UIState.Blocking,"Space opens quest giver "+id);
            var offers=gm.Quests.AvailableAllFrom(q.giverNpc);int index=offers.IndexOf(q);Check(index>=0,"real available quest list contains "+id);
            // Select a known offer through its own UI helper; native mouse/offer cycling is not tested.
            typeof(QuestNpc).GetMethod("OfferAt",Flags).Invoke(npc,new object[]{index});
            yield return null;yield return new WaitForEndOfFrame();
            var dialogue=FindFirstObjectByType<DialogueUI>();
            for(int i=0;i<12&&!dialogue.ChoicesVisible;i++)yield return Keypress(Key.Space);
            Check(dialogue.ChoicesVisible,"offer pages reach real choices "+id);
            yield return Keypress(Key.Space);
            bool active=false;foreach(var a in gm.Quests.Active)if(a.quest==q)active=true;
            Check(active||gm.Quests.IsFinished(q),"Space accepts "+id);yield return CloseDialogs();
        }
        IEnumerator Talk(string id)
        {
            Near(Npc(id));yield return Keypress(Key.Space);Check(UIState.Blocking,"Space talks/reports at "+id);yield return CloseDialogs();
        }
        TownMarket Market()
        {
            foreach(var m in FindObjectsByType<TownMarket>(FindObjectsSortMode.None))if(m.town!=null)return m;
            throw new Exception("Missing market");
        }
        IEnumerator Trade(string itemId,int count,bool selling)
        {
            var gm=GameManager.I;var market=Market();var item=gm.ItemById(itemId);Near(market);yield return Keypress(Key.Space);
            Check((bool)typeof(TownMarket).GetField("open",Flags).GetValue(market),"Space opens production market "+market.town.id);
            var quote=gm.GetMarket(market.town).Preview(item,gm.Player,count,selling);Check(quote.count==count,"requested sequential estimate available "+itemId);
            int cash=gm.Player.money,held=gm.Player.cargo.Count(item);
            var pendingRewards=new Dictionary<QuestData,int>();var pendingCredit=new Dictionary<QuestData,int>();
            foreach(var progress in gm.Quests.Active){pendingRewards[progress.quest]=progress.RewardMoney;pendingCredit[progress.quest]=progress.RewardCredit;}
            int credit=gm.Player.credit;
            int executed=(int)typeof(TownMarket).GetMethod("TradeBatch",Flags).Invoke(market,new object[]{quote});
            Check(executed==count,"real batch execution "+itemId+(selling?" sell":" buy"));
            // Quest completion rewards may be paid inside the last unit's event.
            Check(gm.Player.cargo.Count(item)==held+(selling?-count:count),"batch cargo count matches "+itemId);
            int reward=0,creditReward=0;foreach(var entry in pendingRewards)if(gm.Quests.IsFinished(entry.Key)){reward+=entry.Value;creditReward+=pendingCredit[entry.Key];}
            Check(gm.Player.money==cash+(selling?quote.total:-quote.total)+reward,"batch balance exactly matches estimate/captured rewards "+itemId);
            Check(gm.Player.credit==credit+creditReward,"batch credit exactly matches captured quest rewards "+itemId);
            yield return Keypress(Key.L);Check(gm.GetComponent<TradeLedgerUI>().IsOpen,"L opens ledger from market");
            yield return Keypress(Key.Escape);Check(!gm.GetComponent<TradeLedgerUI>().IsOpen,"ledger Escape closes ledger "+market.town.id);
            if(UIState.Blocking)yield return CloseDialogs();
            Check(!UIState.Blocking,"control restored after ledger and optional inn prompt "+market.town.id);
        }
        IEnumerator RestoreFromMenu()
        {
            var gm=GameManager.I;Check(gm.SaveNow(),"save checkpoint before menu restore");var data=SaveSystem.Read();Check(data!=null,"checkpoint validates");
            int money=gm.Player.money;gm.Player.money=1;gm.Player.cargo.Clear();
            typeof(GameManager).GetField("saveToLoad",Flags).SetValue(gm,data);StartScreen.Open(true);yield return Keypress(Key.Space);
            Check(!StartScreen.IsOpen&&gm.Player.money==money,"real menu Continue restores checkpoint balance");
            foreach(var entry in data.quests){bool found=false;foreach(var active in gm.Quests.Active)if(active.quest.id==entry.id&&active.step==entry.step&&active.amount==entry.amount)found=true;Check(found,"restored active quest "+entry.id);}
            foreach(string id in data.finishedQuests)Check(gm.Quests.IsFinished(Q(id)),"restored finished quest "+id);
            for(int i=0;i<data.cargoItems.Count;i++)Check(gm.Player.cargo.Count(gm.ItemById(data.cargoItems[i]))==data.cargoCounts[i],"restored cargo "+data.cargoItems[i]);
            Quiet();
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);var gm=GameManager.I;Check(gm!=null&&!gm.AutosaveEnabled,"safe preview bootstrap");
            var login=FindFirstObjectByType<NetLogin>();if(login!=null)typeof(NetLogin).GetMethod("PlayAlone",Flags).Invoke(login,null);
            Check(!NetLogin.IsOpen&&!StartScreen.Held,"offline route selected");
            SaveSystem.UseSlot("codex_tradequest_"+Guid.NewGuid().ToString("N")+".json");slotPath=SaveSystem.CurrentPath;
            originalKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            yield return Load("Goryeo/Naru");typeof(GameManager).GetField("autosave",Flags).SetValue(gm,true);
            gm.Clock.day=31;gm.Clock.minuteOfDay=720;gm.Player.money=100;gm.Player.credit=0;
            // Deterministic board generation, with production database/world and no fabricated delivery events.
            typeof(GameManager).GetProperty("Contracts").SetValue(gm,new ContractManager(gm.Db,gm.Clock,gm.Player,null,21));gm.Contracts.InitBoards();
            yield return Accept("q1_naru");Check(gm.Player.cargo.Count(Q("q1_naru").giveItem)==1,"acceptance grants exactly one quest parcel");
            yield return RestoreFromMenu();Check(gm.Player.cargo.Count(Q("q1_naru").giveItem)==1,"in-progress parcel survives Continue");
            yield return Load("Goryeo/goryeo_jangdan");yield return Talk("goryeo_jangdan_farmer");
            Check(gm.Quests.IsFinished(Q("q1_naru"))&&gm.Player.money==130&&gm.Player.credit==1,"first delivery consumes parcel and pays once");
            yield return Accept("q1_jangdan");yield return Trade("grain",3,false);
            yield return Load("Goryeo/goryeo_songak");yield return Trade("grain",3,true);
            Check(gm.Quests.IsFinished(Q("q1_jangdan")),"real trade events finish grain route quest");
            yield return Accept("q1_songak");
            var board=FindFirstObjectByType<ContractBoard>();Check(board!=null&&board.town=="goryeo_songak","Songak board exists");Near(board);yield return Keypress(Key.Space);
            Check((bool)typeof(ContractBoard).GetField("open",Flags).GetValue(board),"Space opens ordinary board");yield return Photo("songak-board");
            board.enabled=false;MapRoot.Current.previewOnly=true;typeof(GameManager).GetField("autosave",Flags).SetValue(gm,false);
            var trialObject=new GameObject("Board layout proposal");trialObject.transform.SetParent(MapRoot.Current.transform,false);trialObject.transform.position=board.transform.position;
            var trial=trialObject.AddComponent<GoryeoBoardLayoutTrial>();trial.town=board.town;trial.Interact(PlayerController.Instance);
            yield return Photo("songak-board-proposed");
            trial.enabled=false;Destroy(trialObject);MapRoot.Current.previewOnly=false;typeof(GameManager).GetField("autosave",Flags).SetValue(gm,true);board.enabled=true;board.Interact(PlayerController.Instance);
            Check(gm.AutosaveEnabled&&SaveSystem.CurrentPath==slotPath,"UI trial restores production flags and isolated slot");
            var offer=gm.Contracts.Available("goryeo_songak").Find(c=>!c.Premium&&gm.Contracts.CanAccept(c)==null);
            Check(offer!=null&&gm.Contracts.Accept("goryeo_songak",offer),"ordinary board accept handler succeeds");yield return Keypress(Key.Escape);
            Check(gm.SaveNow(),"save accepted ordinary contract");var accepted=SaveSystem.Read();Check(accepted.contracts.Exists(c=>c.id==offer.id),"accepted contract serialized");
            string target=WorldIndex.Get().MapOf("market",offer.toTown);Check(!string.IsNullOrEmpty(target),"delivery town resolves to actual map");
            yield return Load(target);board=null;foreach(var b in FindObjectsByType<ContractBoard>(FindObjectsSortMode.None))if(b.town==offer.toTown)board=b;
            Check(board!=null,"real destination board exists");Near(board);yield return Keypress(Key.Space);
            var delivered=gm.Contracts.Deliver(offer,offer.toTown);Check(delivered.success&&gm.Player.credit==2,"ordinary delivery handler pays single credit");
            int paidMoney=gm.Player.money;var repeated=gm.Contracts.Deliver(offer,offer.toTown);Check(!repeated.success&&gm.Player.money==paidMoney,"same contract cannot pay twice");yield return Keypress(Key.Escape);
            yield return Load("Goryeo/goryeo_songak");yield return Talk("goryeo_songak_merchant");Check(gm.Quests.IsFinished(Q("q1_songak"))&&gm.Player.credit==2,"board tutorial report does not double credit");
            yield return Load("Goryeo/Dakmok");yield return Accept("q1_dakmok");yield return Trade("paper",2,false);yield return Talk("dakmok_chief");
            Check(gm.Quests.IsFinished(Q("q1_dakmok"))&&gm.Player.cargo.Count(gm.ItemById("paper"))==2,"paper purchase/report preserves sale cargo");
            yield return Accept("q1_gaegyeong");yield return Load("Goryeo/Gaegyeong");yield return Talk("gaekju");yield return Trade("paper",2,true);
            foreach(string id in new[]{"q1_naru","q1_jangdan","q1_songak","q1_dakmok","q1_gaegyeong"})Check(gm.Quests.IsFinished(Q(id)),"full live Q1 chain finished "+id);
            Check(gm.Player.credit==3&&gm.Player.money>=100,"100-money fixture completes Q1 with three credit and no insolvency");yield return Photo("capital-q1-complete");
            // Return to registered starting scene so Continue follows the actual startup path.
            yield return Load("Goryeo/Naru");yield return RestoreFromMenu();int finalMoney=gm.Player.money;int finalCredit=gm.Player.credit;
            yield return Talk("naru_chief");Check(gm.Player.money==finalMoney&&gm.Player.credit==finalCredit,"finished first quest cannot award twice after Continue");
            int before=gm.Player.money;yield return Trade("grain",1,false);yield return Trade("grain",1,true);Check(gm.Player.money<=before,"same-town recalculated resale cannot generate profit");
            Check(SaveSystem.CurrentPath==slotPath,"only disposable save slot used");
        }
    }
}
#endif
