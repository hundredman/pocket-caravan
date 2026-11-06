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
    public sealed class GoryeoFinalInputReview : MonoBehaviour
    {
        const string Out = "ArtReview/Goryeo-Final-Input-20261106/";
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
        GoryeoDailyActivity Well() => FindFirstObjectByType<GoryeoDailyActivity>();
        TalkNpc Boatman()
        {
            var p = GoryeoFerry.Find(MapRoot.Current.mapId);
            foreach (var npc in FindObjectsByType<TalkNpc>(FindObjectsSortMode.None))
                if (MapRoot.Current.WorldToCell(npc.transform.position) == new Vector3Int(p.npcX, p.height-p.npcY-1, 0)) return npc;
            throw new Exception("Boatman missing");
        }
        IEnumerator Voyage(string destination)
        {
            var gm = GameManager.I; Near(Boatman()); int money = gm.Player.money; float hours = gm.Clock.TotalHours;
            yield return Keypress(Key.Space);
            Check(UIState.Blocking && FindFirstObjectByType<DialogueUI>().ChoicesVisible, "Space opens boatman choices");
            yield return Keypress(Key.Space);
            for (int i = 0; i < 600 && (gm.IsTravelling || MapRoot.Current == null || MapRoot.Current.mapId != destination); i++) yield return null;
            Check(!gm.IsTravelling && MapRoot.Current.mapId == destination, "Space selects first route and arrives " + destination);
            // Directors install after the travel coroutine releases its lock on the next Update.
            yield return null; yield return null;
            Check(gm.Player.money == money-8 && Mathf.Abs(gm.Clock.TotalHours-hours-1)<.001f, "one fare and one game hour " + destination);
            Check(MapRoot.Current.IsWalkable(PlayerController.Instance.Motor.Cell), "boat arrival walkable " + destination);
            var saved = SaveSystem.Read(); Check(saved != null && saved.scene == destination && saved.money == gm.Player.money, "arrival checkpoint reflects fare " + destination);
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out); var gm = GameManager.I;
            Check(gm != null && !gm.AutosaveEnabled, "safe preview bootstrap");
            // Select the existing offline route before opening StartScreen, avoiding saved auto-login.
            var login = FindFirstObjectByType<NetLogin>();
            if (login != null) typeof(NetLogin).GetMethod("PlayAlone", Flags).Invoke(login, null);
            Check(!StartScreen.Held && !NetLogin.IsOpen, "offline prerequisite selected before start menu");
            SaveSystem.UseSlot("codex_final_input_"+Guid.NewGuid().ToString("N")+".json"); slotPath = SaveSystem.CurrentPath;
            originalKeyboard = Keyboard.current; keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            Check(Keyboard.current == keyboard, "virtual keyboard owns ordinary InputReader");
            yield return Load("Goryeo/Naru"); typeof(GameManager).GetField("autosave", Flags).SetValue(gm, true);
            gm.Clock.day = 31; gm.Clock.minuteOfDay = 720; gm.Player.money = 100;
            Check(!gm.HasMemo("goryeo:daily:water_day"), "old save fixture has no daily memo");
            var well = Well(); Check(well != null, "approved well auto installed"); Near(well);
            yield return Keypress(Key.Space); Check(UIState.Blocking && PlayerController.Instance.TalkTarget == null, "Space opens well dialogue and blocks ordinary interaction");
            Check(FindFirstObjectByType<DialogueUI>().CurrentPage.Contains("사례비 3냥"), "offer displays approved reward");
            yield return Keypress(Key.Space); Check(well.Working, "second Space invokes normal dialogue choice and begins");
            yield return Keypress(Key.Escape); Check(!well.Working && !UIState.Blocking && gm.Player.money == 100 && !well.DoneToday, "Escape cancels without opening pause menu or consuming reward");
            yield return Keypress(Key.Space); yield return Keypress(Key.Space);
            Check(well.Working, "Space can retry after cancel");
            yield return new WaitForSecondsRealtime(3.3f);
            Check(!well.Working && well.DoneToday && gm.Player.money == 103, "ordinary input flow pays exactly three coins");
            yield return Photo("well-complete"); yield return Keypress(Key.Space);
            Check(!UIState.Blocking && !well.Working, "completion Space closes without reopening activity");
            yield return Keypress(Key.Space); Check(UIState.Blocking && !well.Working && gm.Player.money == 103, "same-day Space shows done message without payment");
            yield return Keypress(Key.Escape); Check(!UIState.Blocking, "Escape closes done dialogue without pause menu");
            Check(gm.SaveNow(), "checkpoint for start-screen Continue"); var data = SaveSystem.Read();
            gm.Player.money = 7; gm.SetMemo("goryeo:daily:water_day", -1);
            // Fixture supplies the record normally read during Awake; menu input and Continue path are real.
            typeof(GameManager).GetField("saveToLoad", Flags).SetValue(gm, data); StartScreen.Open(true);
            yield return Photo("continue-menu"); yield return Keypress(Key.Space);
            Check(!StartScreen.IsOpen && gm.Player.money == 103 && well.DoneToday, "start-screen Space Continue restores balance and daily record: open="+StartScreen.IsOpen+", held="+StartScreen.Held+", money="+gm.Player.money+", memo="+gm.GetMemo("goryeo:daily:water_day",-1)+", day="+gm.Clock.day+", issue="+gm.LoadIssue);
            Check(!gm.IsTravelling && MapRoot.Current.mapId == "Naru", "Continue retains saved map");
            Quiet(); Near(well); yield return Keypress(Key.Space); Check(gm.Player.money == 103 && !well.Working, "post-Continue Space cannot duplicate reward"); yield return Keypress(Key.Escape);
            yield return Voyage("transit_ferry"); yield return Voyage("Naru");
            Check(gm.Player.money == 87 && Well().DoneToday, "round-trip fares and daily history preserved");
            yield return Load("Transit/transit_central_approach");
            var seats = FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None); Check(seats.Length == 3, "approved three seats auto installed");
            foreach (var seat in seats)
            {
                Near(seat); yield return Keypress(Key.Space); Check(GoryeoSeat.Sitting(PlayerController.Instance), "Space sits " + seat.site.id);
                yield return Keypress(Key.Space); Check(!GoryeoSeat.Sitting(PlayerController.Instance), "Space stands without another interaction " + seat.site.id);
                Near(seat); yield return Keypress(Key.Space); Check(GoryeoSeat.Sitting(PlayerController.Instance), "Space sits again " + seat.site.id);
                yield return Keypress(Key.W); Check(!GoryeoSeat.Sitting(PlayerController.Instance), "W stands through real input path " + seat.site.id);
                Check(!UIState.Blocking, "seat input leaves UI unlocked " + seat.site.id);
            }
            Check(gm.Player.money == 87 && SaveSystem.CurrentPath == slotPath, "seating never changes money or save slot");
        }
    }
}
#endif
