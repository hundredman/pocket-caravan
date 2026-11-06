using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// UI B안 화면 캡처 (2026-11-05, feature/ui-b). 메뉴: Pocket Caravan/UI/B안 화면 캡처.
    /// Play가 아니면 지금 열린 씬으로 Play에 들어가 찍고 멈춘다. 이미 Play 중이면 그 자리에서 찍고 Play는 그대로 둔다.
    ///  1 평소 화면 → 2 Tab 펼침 → 3 교역 장부 · 내 짐 판매처 → 4 교역 장부 · 전체 시세 → 5 온라인 창(O) → 6 M 지도. 결과: Logs/UIB/ (git 제외 폴더)
    /// 장부가 비어 있으면 모든 장터 값을 잠깐 적어서 찍고(본 날짜는 오늘·1일 전·2일 전으로 섞음), 끝나면 원래 장부로 되돌린다.
    /// </summary>
    [InitializeOnLoad]
    public static class UIBScreenshots
    {
        const string OutDir = "Logs/UIB";
        const string KeyActive = "UIB.active", KeyOwnPlay = "UIB.ownPlay", KeyEntered = "UIB.entered", KeyStep = "UIB.step", KeyAt = "UIB.at", KeyReport = "UIB.report";
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        static List<TradeLedger.Quote> backup;
        static bool filled;

        static UIBScreenshots() { EditorApplication.update += Tick; }

        [MenuItem("Pocket Caravan/UI/B안 화면 캡처 (상태 줄·Tab·장부·온라인 창·지도)")]
        public static void Run()
        {
            Directory.CreateDirectory(OutDir);
            foreach (var f in Directory.GetFiles(OutDir, "*.png")) File.Delete(f);
            SessionState.SetBool(KeyActive, true);
            SessionState.SetBool(KeyOwnPlay, !EditorApplication.isPlaying);
            SessionState.SetBool(KeyEntered, false);
            SessionState.SetString(KeyReport, "");
            Step = 0;
            backup = null; filled = false;
            if (!EditorApplication.isPlaying) EditorApplication.EnterPlaymode();
        }

        static int Step { get => SessionState.GetInt(KeyStep, 0); set { SessionState.SetInt(KeyStep, value); SessionState.SetFloat(KeyAt, (float)EditorApplication.timeSinceStartup); } }
        static float Since => (float)EditorApplication.timeSinceStartup - SessionState.GetFloat(KeyAt, 0f);
        static bool OwnPlay => SessionState.GetBool(KeyOwnPlay, true);
        static void Note(string line) { SessionState.SetString(KeyReport, SessionState.GetString(KeyReport, "") + line + "\n"); Debug.Log("[UI B안 캡처] " + line); }
        static void Shot(string name) { ScreenCapture.CaptureScreenshot($"{OutDir}/{name}.png"); Note("찍음: " + name); }
        static FieldInfo Field(Type t, string name) => t.GetField(name, Any);

        static void Tick()
        {
            if (!SessionState.GetBool(KeyActive, false)) return;
            if (!EditorApplication.isPlaying)
            {
                // Play를 나왔으면 끝. Play에 못 들어갔으면(컴파일 오류 등) 30초 뒤 그만둔다 — 다음 보통 Play를 가로채지 않게
                if (SessionState.GetBool(KeyEntered, false)) Finish();
                else if (Since > 30f) { SessionState.SetBool(KeyActive, false); Debug.LogWarning("[UI B안 캡처] Play에 들어가지 못해 그만둡니다."); }
                return;
            }
            if (!SessionState.GetBool(KeyEntered, false)) { SessionState.SetBool(KeyEntered, true); Step = 0; }
            var gm = GameManager.I;
            var ledgerUI = gm != null ? gm.GetComponent<TradeLedgerUI>() : null;
            switch (Step)
            {
                case 0:   // 게임이 다 뜰 때까지
                    if (Since < (OwnPlay ? 3f : .3f)) return;
                    if (gm == null || MapRoot.Current == null) { Note("게임이 안 떴음 (GameManager·맵 없음). 게임 맵 씬에서 실행하세요."); Stop(); return; }
                    if (StartScreen.IsOpen && !SkipStartScreen()) { Stop(); return; }
                    if (!string.IsNullOrEmpty(gm.LoadIssue)) Note("저장 문제 (캡처와 무관): " + gm.LoadIssue);
                    FillLedger(gm);
                    Step = 1;
                    return;
                // 11/05 사진은 그 틱이 아니라 다음 그림 때 찍힌다: 찍은 뒤 잠깐 기다렸다가 화면을 바꾼다
                case 1:
                    if (Since < 1.2f) return;
                    Shot("1_평소");
                    Step = 2;
                    return;
                case 2:
                    if (Since < .6f) return;
                    Field(typeof(HUD), "expandedUntil")?.SetValue(null, Time.unscaledTime + 30f);
                    Step = 3;
                    return;
                case 3:
                    if (Since < .8f) return;
                    Shot("2_Tab_펼침");
                    Step = 4;
                    return;
                case 4:   // 장부 · 내 짐 판매처
                    if (Since < .6f) return;
                    Field(typeof(HUD), "expandedUntil")?.SetValue(null, -1f);
                    if (ledgerUI == null) { Note("교역 장부(TradeLedgerUI)가 없음"); Stop(); return; }
                    ledgerUI.Open();
                    if (!ledgerUI.IsOpen) { Note("장부가 안 열림 (다른 창이 떠 있음?)"); Stop(); return; }
                    Field(typeof(TradeLedgerUI), "showCargo")?.SetValue(ledgerUI, true);
                    Step = 5;
                    return;
                case 5:
                    if (Since < .8f) return;
                    Shot("3_장부_내짐");
                    Step = 6;
                    return;
                case 6:
                    if (Since < .6f) return;
                    Field(typeof(TradeLedgerUI), "showCargo")?.SetValue(ledgerUI, false);
                    Field(typeof(TradeLedgerUI), "selected")?.SetValue(ledgerUI, BestItem(gm));
                    Step = 7;
                    return;
                case 7:
                    if (Since < .8f) return;
                    Shot("4_장부_전체시세");
                    Step = 8;
                    return;
                case 8:   // 온라인 창 (O)
                {
                    if (Since < .6f) return;
                    ledgerUI.Close();
                    var login = UnityEngine.Object.FindFirstObjectByType<NetLogin>();
                    var panel = login != null ? Field(typeof(NetLogin), "panel") : null;
                    if (panel == null) { Note("건너뜀: NetLogin.panel 없음"); Step = 10; return; }
                    panel.SetValue(login, Enum.Parse(panel.FieldType, "Game"));
                    UIState.Blocking = true;
                    Step = 9;
                    return;
                }
                case 9:
                    if (Since < .8f) return;
                    Shot("5_온라인창");
                    Step = 10;
                    return;
                case 10:
                {
                    if (Since < .6f) return;
                    var login = UnityEngine.Object.FindFirstObjectByType<NetLogin>();
                    var close = login != null ? typeof(NetLogin).GetMethod("CloseGame", Any) : null;
                    if (close != null && NetLogin.IsOpen) close.Invoke(login, null);
                    Step = 11;
                    return;
                }
                case 11:   // M 지도 (지역마다 다른 지도가 열린다)
                    if (Since < .5f) return;
                    InputReader.Press('m');
                    Step = 12;
                    return;
                case 12:
                    if (Since < 1f) return;
                    Shot("6_지도");
                    Step = 13;
                    return;
                case 13:
                    if (Since < .6f) return;
                    if (UIState.Blocking) InputReader.Press('m');
                    Step = 14;
                    return;
                case 14:
                    if (Since < .4f) return;
                    Stop();
                    return;
            }
        }

        /// <summary>시작 화면이 떠 있으면 "이어 하기"로 넘긴다 (저장이 없으면 못 넘긴다)</summary>
        static bool SkipStartScreen()
        {
            var inst = Field(typeof(StartScreen), "inst")?.GetValue(null) as StartScreen;
            if (inst == null || !(Field(typeof(StartScreen), "hasSave")?.GetValue(inst) is bool hasSave) || !hasSave)
            {
                Note("시작 화면에 저장이 없어 넘기지 못함: 게임을 시작한 뒤 Play 중에 메뉴를 다시 누르세요.");
                return false;
            }
            var mode = Field(typeof(StartScreen), "mode");
            mode.SetValue(inst, Enum.Parse(mode.FieldType, "Closed"));
            StartScreen.Held = false;
            GameManager.I.ContinueFromStartScreen();
            Note("시작 화면: 이어 하기로 넘김");
            return true;
        }

        /// <summary>장부가 비어 있으면 모든 장터 값을 잠깐 적는다 (Stop에서 되돌린다)</summary>
        static void FillLedger(GameManager gm)
        {
            backup = gm.Player.ledger.Export();
            if (backup.Count > 0) { Note($"장부: 지금 기록 {backup.Count}줄 그대로 찍음"); return; }
            int day = gm.Clock != null ? gm.Clock.day : 1;
            foreach (var town in gm.Db.towns)
                if (town != null) gm.Player.ledger.Observe(town, gm.GetMarket(town), gm.Db.items, day);
            var list = gm.Player.ledger.Export();
            for (int i = 0; i < list.Count; i++) list[i].seenDay = Mathf.Max(1, day - i % 3);
            gm.Player.ledger.Import(list);
            filled = true;
            Note($"장부가 비어 있어서 모든 장터 값을 잠깐 적음 ({list.Count}줄, 끝나면 되돌림)");
        }

        /// <summary>전체 시세에서 고를 물건: 본 장터가 둘 이상이고 차익이 가장 큰 것</summary>
        static string BestItem(GameManager gm)
        {
            string best = null; int bestGain = int.MinValue;
            foreach (var item in gm.Db.items)
            {
                if (item == null || item.questItem) continue;
                int n = 0, buy = int.MaxValue, sell = int.MinValue;
                foreach (var town in gm.Db.towns)
                {
                    var q = town != null ? gm.Player.ledger.Find(town.id, item.id) : null;
                    if (q == null) continue;
                    n++; buy = Mathf.Min(buy, q.buy); sell = Mathf.Max(sell, q.sell);
                }
                if (n >= 2 && sell - buy > bestGain) { bestGain = sell - buy; best = item.id; }
            }
            return best;
        }

        static void Stop()
        {
            var gm = GameManager.I;
            if (filled && gm != null && backup != null) { gm.Player.ledger.Import(backup); Note("장부를 원래대로 되돌림"); }
            filled = false;
            if (OwnPlay && EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            else Finish();
        }

        static void Finish()
        {
            if (!SessionState.GetBool(KeyActive, false)) return;
            SessionState.SetBool(KeyActive, false);
            File.WriteAllText($"{OutDir}/report.txt", SessionState.GetString(KeyReport, ""));
            Debug.Log($"[UI B안 캡처] 끝. {Path.GetFullPath(OutDir)}");
        }
    }
}
