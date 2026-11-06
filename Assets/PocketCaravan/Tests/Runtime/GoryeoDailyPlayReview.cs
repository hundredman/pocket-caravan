#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

namespace PocketCaravan.EditorTools
{
    public sealed class GoryeoDailyPlayReview : MonoBehaviour
    {
        const string Out = "ArtReview/Goryeo-Daily-20261106/";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        [Serializable] class Report { public bool complete; public string error; public List<string> checks = new List<string>(); }
        readonly Report report = new Report();
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
        void Finish(Exception e)
        {
            if (GameManager.I != null) typeof(GameManager).GetField("autosave", Flags).SetValue(GameManager.I, false);
            if (slotPath != null) foreach (string suffix in new[] { "", ".bak", ".tmp" }) if (File.Exists(slotPath + suffix)) File.Delete(slotPath + suffix);
            report.complete = e == null; report.error = e == null ? "" : e.ToString();
            Directory.CreateDirectory(Out); File.WriteAllText(Out + "play.json", JsonUtility.ToJson(report, true));
            Time.timeScale = 1; EditorApplication.isPlaying = false;
        }
        void Close()
        {
            var d = FindFirstObjectByType<DialogueUI>(); typeof(DialogueUI).GetMethod("Close", Flags).Invoke(d, null);
            Notify.Clear();
        }
        IEnumerator Load(string path)
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Scenes/" + path + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 12; i++) yield return null;
            foreach (var s in FindObjectsByType<StoryGuide>(FindObjectsSortMode.None)) { s.StopAllCoroutines(); s.enabled = false; }
            foreach (var s in FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None)) { s.StopAllCoroutines(); s.enabled = false; }
            Close(); Time.timeScale = 0;
        }
        IEnumerator Photo(string name)
        {
            Notify.Clear(); yield return null; yield return new WaitForEndOfFrame();
            var t = ScreenCapture.CaptureScreenshotAsTexture();
            Check(t.width == 480 && t.height == 270, "actual 480x270 capture " + name);
            File.WriteAllBytes(Out + name + ".png", t.EncodeToPNG()); Destroy(t);
        }
        void Near(GoryeoDailyActivity activity)
        {
            var pc = PlayerController.Instance;
            pc.Motor.Teleport(activity.transform.position + Vector3.down - (Vector3)pc.Motor.feetOffset, Vector2Int.up);
            var cam = Camera.main; cam.GetComponent<CameraFollow>().enabled = false;
            cam.orthographicSize = 8.4375f; cam.transform.position = activity.transform.position + Vector3.down * 3 + new Vector3(0, 0, -10);
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out); var gm = GameManager.I;
            Check(!gm.AutosaveEnabled, "bootstrap saves OFF");
            SaveSystem.UseSlot("codex_daily_qa_" + Guid.NewGuid().ToString("N") + ".json"); slotPath = SaveSystem.CurrentPath;
            yield return Load("Goryeo/Naru");
            Check(FindObjectsByType<GoryeoDailyActivity>(FindObjectsSortMode.None).Length == 1, "production scene has approved daily well");
            foreach(var existing in FindObjectsByType<GoryeoDailyActivity>(FindObjectsSortMode.None)) DestroyImmediate(existing.gameObject);
            var cfg = JsonUtility.FromJson<GoryeoDailyActivity.Site>(File.ReadAllText(Out + "source.json"));
            bool refused = false; try { GoryeoDailyActivity.InstallReview(cfg); } catch (InvalidOperationException) { refused = true; }
            Check(refused, "production map rejected by review installer");
            MapRoot.Current.previewOnly = true;
            var activity = GoryeoDailyActivity.InstallReview(cfg); var pc = PlayerController.Instance;
            Check(GoryeoDailyActivity.InstallReview(cfg) == activity, "duplicate installation returns same activity");
            Check(!activity.blocksMovement, "existing collision unchanged");
            gm.Clock.day = 20; gm.Clock.minuteOfDay = 12 * 60; Near(activity);
            Check(!gm.HasMemo(cfg.memoKey) && !activity.DoneToday, "old save without daily memo starts available");
            int money = gm.Player.money, credit = gm.Player.credit; var vehicle = gm.Player.vehicle;
            string cargo = JsonUtility.ToJson(new CargoSnapshot(gm));
            float clock = gm.Clock.TotalHours;
            activity.Interact(pc); yield return Photo("01-offer");
            var dialogue = FindFirstObjectByType<DialogueUI>(); Check(dialogue.ChoicesVisible && dialogue.PageCount == 1, "offer and both choices fit one page");
            Close(); Check(activity.Begin(pc), "nearby local player can begin");
            yield return new WaitForSecondsRealtime(.9f); Check(activity.Working && activity.Progress > 0, "real unscaled progress while UI pauses movement and clock");
            yield return Photo("02-working"); activity.Cancel();
            Check(!activity.Working && !UIState.Blocking && !gm.HasMemo(cfg.memoKey), "cancel releases UI and does not consume day");
            Check(activity.Begin(pc), "cancelled action can retry"); activity.enabled = false; yield return null;
            Check(!activity.Working && !UIState.Blocking && !gm.HasMemo(cfg.memoKey), "disabled activity cancels without daily record"); activity.enabled = true;
            pc.Motor.Teleport(pc.transform.position + Vector3.right * 8, Vector2Int.left);
            Check(!activity.Begin(pc), "distant player rejected"); Near(activity);
            UIState.Blocking = true; Check(!activity.Begin(pc), "existing modal UI blocks action"); UIState.Release();
            Check(activity.Begin(pc), "begin before slot switch"); SaveSystem.UseSlot("codex_daily_other_qa.json"); yield return null;
            Check(!activity.Working && !UIState.Blocking && !gm.HasMemo(cfg.memoKey), "slot change cancels and releases owned lock");
            SaveSystem.UseSlot(Path.GetFileName(slotPath));
            Check(activity.Begin(pc), "begin before actor disabled"); pc.enabled = false; yield return null;
            Check(!activity.Working && !UIState.Blocking && !gm.HasMemo(cfg.memoKey), "disabled actor does not consume day"); pc.enabled = true;
            Check(activity.Begin(pc), "begin before day change"); gm.Clock.day++; yield return null;
            Check(!activity.Working && !UIState.Blocking && !gm.HasMemo(cfg.memoKey), "unexpected date change cancels without consumption"); gm.Clock.day = 20;
            Check(activity.Begin(pc), "complete trial starts");
            yield return new WaitForSecondsRealtime(cfg.seconds + .2f);
            Check(!activity.Working && activity.DoneToday && gm.GetMemo(cfg.memoKey, -1) == 20, "completion records only existing public memo");
            yield return Photo("03-complete"); Close();
            Check(!activity.Begin(pc), "same day repeat blocked"); activity.Interact(pc); yield return Photo("04-done-today"); Close();
            Check(gm.Player.money == money && gm.Player.credit == credit && gm.Player.vehicle == vehicle && JsonUtility.ToJson(new CargoSnapshot(gm)) == cargo, "money credit vehicle cargo unchanged");
            Check(Mathf.Approximately(gm.Clock.TotalHours, clock), "activity does not advance game time");
            typeof(GameManager).GetField("autosave", Flags).SetValue(gm, true);
            Check(gm.SaveNow(), "actual public SaveNow writes isolated slot");
            var saved = SaveSystem.Read(); Check(saved != null, "actual SaveSystem.Read validates checkpoint");
            int index = saved.cooldownKeys.IndexOf(cfg.memoKey);
            Check(index >= 0 && saved.cooldownUntil[index] == 20, "existing cooldown arrays round trip daily date");
            Check(saved.money == money && saved.credit == credit, "serialized balance unchanged");
            gm.SetMemo(cfg.memoKey, -1); typeof(GameManager).GetMethod("Apply", Flags).Invoke(gm, new object[] { saved });
            Check(activity.DoneToday, "actual GameManager.Apply restores daily record");
            typeof(GameManager).GetField("autosave", Flags).SetValue(gm, false);
            Near(activity); gm.Clock.day = 19; Check(activity.DoneToday, "earlier game date cannot bypass completed later date");
            gm.Clock.day = 21; Check(!activity.DoneToday && activity.Begin(pc), "following game day available"); activity.Cancel();
            yield return Load("Goryeo/Naru"); Check(FindObjectsByType<GoryeoDailyActivity>(FindObjectsSortMode.None).Length == 1, "reenter production scene installs approved well only");
            foreach(var existing in FindObjectsByType<GoryeoDailyActivity>(FindObjectsSortMode.None)) DestroyImmediate(existing.gameObject);
            MapRoot.Current.previewOnly = true; activity = GoryeoDailyActivity.InstallReview(cfg); Near(activity); gm.Clock.day = 20;
            Check(activity.DoneToday, "same game manager memo survives scene reload");
            gm.Clock.day = 21; Check(activity.Begin(PlayerController.Instance), "start before leaving map");
            yield return Load("Transit/transit_ferry");
            Check(!UIState.Blocking && FindObjectsByType<GoryeoDailyActivity>(FindObjectsSortMode.None).Length == 0, "map unload clears trial and modal lock");
            Check(gm.GetMemo(cfg.memoKey, -1) == 20, "unfinished departing action keeps previous date");
            Check(SaveSystem.CurrentPath == slotPath && !gm.AutosaveEnabled, "isolated slot and saves OFF at teardown");
        }
        [Serializable] class CargoSnapshot
        {
            public List<string> ids = new List<string>(); public List<int> counts = new List<int>(); public List<float> costs = new List<float>();
            public CargoSnapshot(GameManager gm) { foreach (var p in gm.Player.cargo.Items) { ids.Add(p.Key.id); counts.Add(p.Value); costs.Add(gm.Player.cargo.TotalCost(p.Key)); } }
        }
    }
}
#endif
