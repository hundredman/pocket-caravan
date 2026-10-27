using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 에디터를 직접 클릭하지 않고 아라비아 도구를 실행하기 위한 파일 명령 감시 (원격 작업·자동 검증용).
    /// Library/ArabiaCommands/request.txt에 한 줄 명령을 쓰면 실행하고 result.txt에 결과를 남긴다.
    /// 명령: status, refresh, data, scenes, checks, playtest, walk, stop, regress, editorlog
    /// Library는 버전 관리 밖이며, 이 감시는 아라비아 메뉴와 같은 작업만 수행한다 (임의 코드 실행 없음).
    /// </summary>
    [InitializeOnLoad]
    public static class ArabiaCommandWatcher
    {
        const string Dir = "Library/ArabiaCommands";
        static double next;
        static readonly StringBuilder console = new StringBuilder();

        static ArabiaCommandWatcher()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
            Directory.CreateDirectory(Dir);
            File.WriteAllText(Path.Combine(Dir, "alive.txt"),
                $"{DateTime.Now:O} compiled={!EditorUtility.scriptCompilationFailed} unity={Application.unityVersion}\n");
        }

        static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Log && !message.StartsWith("[")) return;   // 게임 로그 중 태그 붙은 것과 경고·오류만
            lock (console)
            {
                console.AppendLine($"{DateTime.Now:HH:mm:ss} {type}: {message}");
                if (type == LogType.Exception || type == LogType.Error) console.AppendLine(stack);
                try { File.AppendAllText(Path.Combine(Dir, "console.log"), console.ToString()); } catch { }
                console.Clear();
            }
        }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + 1.0;
            string request = Path.Combine(Dir, "request.txt");
            if (!File.Exists(request)) return;
            string command = File.ReadAllText(request).Trim();
            File.Delete(request);
            string result;
            try { result = Execute(command); }
            catch (Exception e) { result = "ERROR " + e; }
            File.WriteAllText(Path.Combine(Dir, "result.txt"), $"{DateTime.Now:O} {command}\n{result}\n");
        }

        /// <summary>기존 공용·고려·중간권 편집 모드 검사를 그대로 실행한다 (Play·저장 없음). 아라비아 추가 후 회귀 확인용.</summary>
        static string Regression()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play 종료 후 실행하세요.");
            var sb = new StringBuilder($"# 기존 검사 회귀 {DateTime.Now:yyyy-MM-dd HH:mm:ss} · Unity {Application.unityVersion}\n");
            void Run(string name, Func<object> test)
            {
                try { var r = test(); sb.AppendLine($"- [PASS] {name}\n```\n{(r is string s ? s : JsonUtility.ToJson(r))}\n```"); }
                catch (Exception e) { sb.AppendLine($"- [FAIL] {name} — {e.GetType().Name}: {e.Message}"); }
            }
            Run("지역 통합 등록 검사", () => { MapJsonBuilder.ValidateWorld(); return "ok"; });
            Run("고려 거점·시설·저장 이주 검사", () => GoryeoRegionChecks.Run());
            Run("게시판 지역·경로 검사", () => RegionalContractChecks.Run());
            Run("여정 안내 데이터 검사", () => JourneyGuideChecks.RunData());
            Run("고려 체험 데이터 검사", () => GoryeoExperienceChecks.RunData());
            Run("중간권 왕복 시세 검사", () => TransitRegionChecks.Run());
            ArabiaRegionSetup.WriteReport("regression.md", sb.ToString());
            return sb.ToString();
        }

        static string Execute(string command)
        {
            switch (command)
            {
                case "status":
                    return $"phase={ArabiaPlaytest.Phase} playing={EditorApplication.isPlaying} compiling={EditorApplication.isCompiling} compileFailed={EditorUtility.scriptCompilationFailed} scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().path}";
                case "refresh":
                    AssetDatabase.Refresh();
                    return "refreshed";
                case "data":
                    ArabiaRegionSetup.ApplyData();
                    return "data ok";
                case "scenes":
                    ArabiaRegionSetup.BuildScenes();
                    return "scenes ok";
                case "checks":
                    ArabiaChecks.RunMenu();
                    return File.ReadAllText(Path.Combine(ArabiaRegionSetup.ReportDir, "static_checks.md"));
                case "playtest":
                    ArabiaPlaytest.Begin(false, false);
                    return "playtest started (Docs/Arabia/Reports/autoplay.md 확인)";
                case "walk":
                    ArabiaPlaytest.Begin(true, false);
                    return "walk test started (Docs/Arabia/Reports/walk.md 확인)";
                case "stop":
                    EditorApplication.isPlaying = false;
                    return "stopped";
                case "regress":
                    return Regression();
                case "editorlog":
                    string log = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library/Logs/Unity/Editor.log");
                    File.Copy(log, Path.Combine(Dir, "Editor.log"), true);
                    return "copied";
                default:
                    return "unknown command";
            }
        }
    }
}
