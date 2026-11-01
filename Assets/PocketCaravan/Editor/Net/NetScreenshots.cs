using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 온라인 화면 캡처 (2026-10-31, feature/online). integration에 합치기 전에 온라인 창들이 어떻게 보이는지 사진으로 남긴다.
    /// 메뉴: Pocket Caravan/온라인/화면 캡처. 지금 열린 씬에서 Play에 들어가 차례로 찍고 Play를 멈춘다. 결과: 프로젝트 Logs/NetScreens/ (git 제외 폴더).
    ///  1 로그인 화면(게임 첫 화면, 미리보기로 띄움) → 2 온라인 창(O, 이 컴퓨터에 기억된 이름·접속키로 실제 팀 서버에 접속한 뒤) →
    ///  3 서버 저장 고르기 창(계정일 때. 서버 저장이 없거나 같으면 예시 값으로 띄운다) → 4 채팅 칸 → 5 내 리액션(/춤)
    /// 고르기 창은 고르지 않고 닫고, 서버에 돈·짐을 올리지도 않는다(NetSave가 고를 차례를 지운다). 채팅은 보내지 않는다. 끝나면 접속을 끊는다.
    /// 온라인 코드의 비공개 칸을 이름으로 건드리므로, 이름이 바뀌면 그 단계만 건너뛰고 report.txt에 적는다.
    /// </summary>
    [InitializeOnLoad]
    public static class NetScreenshots
    {
        const string OutDir = "Logs/NetScreens";
        const string KeyActive = "NetScreens.active", KeyEntered = "NetScreens.entered", KeyStep = "NetScreens.step", KeyAt = "NetScreens.at", KeyReport = "NetScreens.report";
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        static NetScreenshots() { EditorApplication.update += Tick; }

        [MenuItem("Pocket Caravan/온라인/화면 캡처 (로그인·온라인 창·채팅)")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[온라인 화면 캡처] Play를 멈춘 뒤 실행하세요."); return; }
            Directory.CreateDirectory(OutDir);
            foreach (var f in Directory.GetFiles(OutDir, "*.png")) File.Delete(f);
            SessionState.SetBool(KeyActive, true);
            SessionState.SetBool(KeyEntered, false);
            SessionState.SetInt(KeyStep, 0);
            SessionState.SetFloat(KeyAt, (float)EditorApplication.timeSinceStartup);
            SessionState.SetString(KeyReport, "");
            EditorApplication.EnterPlaymode();
        }

        [MenuItem("Pocket Caravan/온라인/로그인 화면 미리보기 (Play 중)")]
        public static void PreviewLogin()
        {
            if (!EditorApplication.isPlaying) { Debug.LogWarning("[온라인] Play 중에 누르세요. 게임 첫 씬에서 Play하면 미리보기 없이도 로그인 화면이 먼저 뜬다."); return; }
            NetLogin.Preview = true;
        }

        static int Step { get => SessionState.GetInt(KeyStep, 0); set { SessionState.SetInt(KeyStep, value); SessionState.SetFloat(KeyAt, (float)EditorApplication.timeSinceStartup); } }
        static float Since => (float)EditorApplication.timeSinceStartup - SessionState.GetFloat(KeyAt, 0f);
        static void Note(string line) { SessionState.SetString(KeyReport, SessionState.GetString(KeyReport, "") + line + "\n"); Debug.Log("[온라인 화면 캡처] " + line); }
        static void Shot(string name) { ScreenCapture.CaptureScreenshot($"{OutDir}/{name}.png"); Note("찍음: " + name); }

        static object Get(object o, string field) => o?.GetType().GetField(field, Any)?.GetValue(o);
        static bool Set(object o, string field, object value)
        {
            var f = o?.GetType().GetField(field, Any);
            if (f == null) { Note($"건너뜀: {o?.GetType().Name}.{field} 칸이 없음"); return false; }
            f.SetValue(o, value);
            return true;
        }
        static void Call(object o, string method)
        {
            var m = o?.GetType().GetMethod(method, Any, null, Type.EmptyTypes, null);
            if (m == null) Note($"건너뜀: {o?.GetType().Name}.{method}() 없음");
            else m.Invoke(o, null);
        }

        static void Tick()
        {
            if (!SessionState.GetBool(KeyActive, false)) return;
            if (!EditorApplication.isPlaying)
            {
                // Play를 나왔으면 끝. Play에 못 들어갔으면(컴파일 오류 등) 30초 뒤 그만둔다 — 다음 보통 Play를 가로채지 않게
                if (SessionState.GetBool(KeyEntered, false)) Finish();
                else if (Since > 30f) { SessionState.SetBool(KeyActive, false); Debug.LogWarning("[온라인 화면 캡처] Play에 들어가지 못해 그만둡니다."); }
                return;
            }
            if (!SessionState.GetBool(KeyEntered, false)) { SessionState.SetBool(KeyEntered, true); Step = 0; }
            var net = NetClient.I;
            switch (Step)
            {
                case 0:   // Play 시작: 게임이 다 뜰 때까지
                    if (Since < 3f) return;
                    if (net == null || GameManager.I == null) { Note("게임이 안 떴음 (NetClient · GameManager 없음). 게임 맵 씬에서 실행하세요."); Stop(); return; }
                    if (net.Status != NetClient.State.Offline) net.Disconnect();
                    Set(UnityEngine.Object.FindFirstObjectByType<NetLogin>(), "autoTried", true);   // 기억된 접속키로 저절로 접속하지 않게
                    NetLogin.Preview = true;
                    Step = 1;
                    return;
                case 1:
                    if (Since < 1f) return;
                    Shot("1_로그인화면");
                    Step = 2;
                    return;
                case 2:   // 접속하기 누르기 (기억된 이름·접속키)
                {
                    if (Since < .4f) return;
                    var login = UnityEngine.Object.FindFirstObjectByType<NetLogin>();
                    var submit = login != null ? login.GetType().GetMethod("Submit", Any, null, new[] { typeof(bool) }, null) : null;
                    if (submit == null) { Note("건너뜀: NetLogin.Submit 없음"); Stop(); return; }
                    submit.Invoke(login, new object[] { false });
                    Step = 3;
                    return;
                }
                case 3:   // 접속을 기다린다 (10초까지)
                    if (net.Status != NetClient.State.Online && Since < 10f) return;
                    if (net.Status != NetClient.State.Online)
                    {
                        Note("접속 실패: " + net.LastError + " (로그인 화면에 이름·접속키가 기억되어 있어야 한다)");
                        Shot("1b_로그인화면_오류");
                        Stop();
                        return;
                    }
                    Note($"접속됨: {net.MyName} ({net.MyId}) {(net.IsAccount ? "계정" : "손님")}");
                    Set(UnityEngine.Object.FindFirstObjectByType<NetSave>(), "waiting", null);   // 이 시험 Play의 돈·짐을 서버에 올리지 않게
                    Step = 4;
                    return;
                case 4:   // 온라인 창(O)
                {
                    if (Since < .8f) return;
                    var login = UnityEngine.Object.FindFirstObjectByType<NetLogin>();
                    var panel = login != null ? login.GetType().GetField("panel", Any) : null;
                    if (panel == null) { Note("건너뜀: NetLogin.panel 없음"); Step = 5; return; }
                    panel.SetValue(login, Enum.Parse(panel.FieldType, "Game"));
                    UIState.Blocking = true;
                    Step = 41;
                    return;
                }
                case 41:
                    if (Since < .8f) return;
                    Shot("2_온라인창");
                    Call(UnityEngine.Object.FindFirstObjectByType<NetLogin>(), "CloseGame");
                    Step = 5;
                    return;
                case 5:   // 서버 저장 고르기 창
                {
                    if (Since < .8f) return;
                    var save = UnityEngine.Object.FindFirstObjectByType<NetSave>();
                    if (save == null || !net.IsAccount) { Note("서버 저장 창: 손님(접속키 없음)이라 건너뜀"); Step = 7; return; }
                    if (Get(save, "offer") == null)
                    {
                        Set(save, "offer", new NetClient.SaveData
                        {
                            money = 1234,
                            items = new List<string> { "venice_glass", "venice_salt" },
                            counts = new List<int> { 3, 5 },
                            costs = new List<float> { 66f, 40f },
                            at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        });
                        Note("서버 저장 창: 실제로 고를 일이 없어서 예시 값(1234냥 · 짐 8개)으로 띄움");
                    }
                    Step = 6;
                    return;
                }
                case 6:
                {
                    if (Since < .8f) return;
                    var save = UnityEngine.Object.FindFirstObjectByType<NetSave>();
                    Shot("3_서버저장_고르기");
                    Call(save, "CloseOffer");   // 고르지 않고 닫는다: 돈·짐 그대로
                    Step = 7;
                    return;
                }
                case 7:   // 채팅 칸
                {
                    if (Since < .6f) return;
                    var chat = UnityEngine.Object.FindFirstObjectByType<NetChat>();
                    if (Get(chat, "box") is NetTextBox box && Set(chat, "typing", true))
                    {
                        box.Text = "안녕하세요! 리알토로 와요";
                        box.Begin();
                        Set(chat, "openedAt", Time.unscaledTime);
                        Set(chat, "sendFrame", -1);
                        UIState.Blocking = true;
                        NetChat.System("(예시) 시험봇 님이 리알토 장터에서 유리그릇을 3개 팔았어요 (값 ↓)");
                    }
                    Step = 8;
                    return;
                }
                case 8:
                {
                    if (Since < .8f) return;
                    Shot("4_채팅칸");
                    Call(UnityEngine.Object.FindFirstObjectByType<NetChat>(), "Close");
                    Step = 9;
                    return;
                }
                case 9:   // 내 리액션
                    if (Since < .5f) return;
                    RemotePlayers.PlaySelf("dance", "춤");
                    Step = 10;
                    return;
                case 10:
                    if (Since < .7f) return;
                    Shot("5_리액션_춤");
                    Step = 11;
                    return;
                case 11:
                    if (Since < .8f) return;
                    Stop();
                    return;
            }
        }

        static void Stop()
        {
            if (NetClient.I != null) NetClient.I.Disconnect();
            UIState.Release();
            if (Step < 12) Step = 12;
            EditorApplication.ExitPlaymode();
        }

        static void Finish()
        {
            SessionState.SetBool(KeyActive, false);
            File.WriteAllText($"{OutDir}/report.txt", SessionState.GetString(KeyReport, ""), new UTF8Encoding(false));
            Debug.Log($"[온라인 화면 캡처] 끝: {Path.GetFullPath(OutDir)}");
            EditorUtility.RevealInFinder(Path.GetFullPath($"{OutDir}/report.txt"));
        }
    }
}
