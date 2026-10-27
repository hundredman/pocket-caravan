using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 베네치아 이동 효과 플레이 테스트 (개발용). Play에 들어가 실제 맵 이동을 차례로 실행하고, 이동하는 동안
    /// 프레임 시간을 기록하고 화면을 캡처한다. 결과: 프로젝트 Logs/VenicePlaytest/ (report.txt, *.png). 저장 파일은 건드리지 않는다
    /// (시작 맵이 아닌 씬에서 Play하므로 GameManager가 자동 저장을 끈다).
    /// 메뉴: Pocket Caravan/베네치아/이동 효과 플레이 테스트. 명령줄: -executeMethod PocketCaravan.EditorTools.VenicePlaytest.RunAndQuit
    /// </summary>
    [InitializeOnLoad]
    public static class VenicePlaytest
    {
        const string StartScene = "Assets/PocketCaravan/Scenes/Venice/venice_chioggia.unity";
        const string OutDir = "Logs/VenicePlaytest";       // Temp는 Unity를 닫으면 지워진다. Logs는 git에서 제외된 폴더
        const string KeyActive = "VenicePlaytest.active", KeyQuit = "VenicePlaytest.quit", KeyDone = "VenicePlaytest.done", KeyShots = "VenicePlaytest.shots";
        // (도착 씬, 도착 자리, 설명)
        static readonly string[][] Trips =
        {
            new[] { "venice_c_landing", "from_venice_chioggia", "1_ferry_to_landing(water)" },
            new[] { "venice_c_vineyard", "from_venice_c_landing", "2_landing_to_vineyard(land)" },
            new[] { "venice_c_landing", "from_venice_c_vineyard", "3_vineyard_to_landing(land)" },
            new[] { "venice_chioggia", "from_venice_c_landing", "4_landing_to_chioggia(water)" },
            new[] { "venice_rialto", "from_venice_chioggia_49", "5_chioggia_to_rialto(lagoon)" },
        };
        static readonly float[] ShotTimes = { 0.35f, 0.7f, 1.1f, 1.6f, 2.1f };

        static int trip = -1;
        static float tripStart, waitUntil;
        static bool travelling;
        static int shot, arrivedFrame = -1;
        static readonly List<float> frames = new List<float>();
        static readonly List<float> moving = new List<float>();   // 도착 씬을 다 불러온 뒤(고양이가 움직이는 구간)
        static readonly StringBuilder report = new StringBuilder();

        static VenicePlaytest()
        {
            EditorApplication.update += Tick;
        }

        [MenuItem("Pocket Caravan/베네치아/이동 효과 플레이 테스트")]
        public static void Run() => Begin(false);

        /// <summary>명령줄용: 베네치아 데이터·맵을 다시 만든 뒤 테스트하고 Unity를 닫는다.</summary>
        public static void RunAndQuit()
        {
            VeniceRegionSetup.Build();
            Begin(true);
        }

        /// <summary>명령줄용: 화면 캡처 없이 프레임 시간만 잰다 (캡처 PNG 저장이 한 프레임을 길게 만들어 측정을 흐리므로).</summary>
        public static void MeasureAndQuit()
        {
            Begin(true);
            SessionState.SetBool(KeyShots, false);
        }

        static void Begin(bool quit)
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[플레이 테스트] Play를 멈춘 뒤 실행하세요."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(OutDir);
            foreach (var f in Directory.GetFiles(OutDir)) File.Delete(f);
            SessionState.SetBool(KeyActive, true);
            SessionState.SetBool(KeyQuit, quit);
            SessionState.SetBool(KeyDone, false);
            SessionState.SetBool(KeyShots, true);
            EditorSceneManager.OpenScene(StartScene, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        static void Tick()
        {
            if (!SessionState.GetBool(KeyActive, false)) return;
            if (!EditorApplication.isPlaying)
            {
                if (SessionState.GetBool(KeyDone, false)) Finish();   // Play를 나오면 도메인이 다시 올라와 정적 값이 지워지므로 SessionState로 확인
                return;
            }
            Application.runInBackground = true;   // Unity 창이 뒤로 가도 테스트가 멈추지 않게 (Play 중에만, 프로젝트 설정은 그대로)
            var gm = GameManager.I;
            if (gm == null || MapRoot.Current == null) return;
            float now = Time.realtimeSinceStartup;
            if (trip == -1) { trip = 0; waitUntil = now + 2f; report.AppendLine("[베네치아 이동 효과 플레이 테스트] Game 화면 " + Screen.width + "x" + Screen.height); return; }
            if (travelling)
            {
                frames.Add(Time.unscaledDeltaTime * 1000f);
                if (MapRoot.Current.mapId == Trips[trip][0])
                {
                    if (arrivedFrame < 0) arrivedFrame = Time.frameCount;
                    if (Time.frameCount >= arrivedFrame + 4) moving.Add(Time.unscaledDeltaTime * 1000f);   // 효과와 같이 4프레임 뒤부터 움직임
                }
                float t = now - tripStart;
                if (SessionState.GetBool(KeyShots, true) && shot < ShotTimes.Length && t >= ShotTimes[shot])
                {
                    ScreenCapture.CaptureScreenshot($"{OutDir}/{Trips[trip][2]}_{shot}.png");
                    shot++;
                }
                if (!gm.IsTravelling && t > 0.2f)
                {
                    travelling = false;
                    Summarize(Trips[trip][2], t, MapRoot.Current.mapId);
                    trip++;
                    waitUntil = now + 1.5f;
                }
                return;
            }
            if (now < waitUntil) return;
            if (trip >= Trips.Length)
            {
                File.WriteAllText($"{OutDir}/report.txt", report.ToString());
                Debug.Log(report.ToString());
                SessionState.SetBool(KeyDone, true);
                trip = -2;
                EditorApplication.ExitPlaymode();
                return;
            }
            frames.Clear(); moving.Clear(); shot = 0; arrivedFrame = -1; travelling = true; tripStart = now;
            gm.TravelTo(Trips[trip][0], Trips[trip][1]);
        }

        static void Summarize(string name, float seconds, string arrived)
        {
            frames.Sort();
            float max = frames.Count > 0 ? frames[frames.Count - 1] : 0f;
            float median = frames.Count > 0 ? frames[frames.Count / 2] : 0f;
            int over33 = frames.FindAll(f => f > 33.4f).Count, over100 = frames.FindAll(f => f > 100f).Count;
            report.AppendLine($"{name}: 도착 {arrived}, {seconds:0.00}초, 프레임 {frames.Count}개, 중앙값 {median:0.0}ms, 최대 {max:0.0}ms, 33ms 초과 {over33}, 100ms 초과 {over100}");
            var top = frames.GetRange(Mathf.Max(0, frames.Count - 5), Mathf.Min(5, frames.Count));
            report.AppendLine("   가장 긴 프레임(ms): " + string.Join(", ", top.ConvertAll(f => f.ToString("0"))));
            moving.Sort();
            report.AppendLine($"   움직이는 구간: 프레임 {moving.Count}개, 중앙값 {(moving.Count > 0 ? moving[moving.Count / 2] : 0):0.0}ms, 최대 {(moving.Count > 0 ? moving[moving.Count - 1] : 0):0.0}ms, 50ms 초과 {moving.FindAll(f => f > 50f).Count}");
        }

        static void Finish()
        {
            bool quit = SessionState.GetBool(KeyQuit, false);
            SessionState.SetBool(KeyActive, false);
            SessionState.SetBool(KeyDone, false);
            trip = -1;
            Debug.Log("[플레이 테스트] 끝. 결과: " + Path.GetFullPath(OutDir));
            if (quit) EditorApplication.Exit(0);
        }
    }
}
