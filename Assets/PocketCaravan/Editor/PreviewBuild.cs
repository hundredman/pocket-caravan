using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>본편 네 맵의 Mac 검토 빌드. 별도 저장 파일로 기존 플레이 진행을 보호한다.</summary>
    public static class PreviewBuild
    {
        [Serializable]
        class Result
        {
            public string result, output, builtAtUtc;
            public int errors, warnings;
            public ulong bytes;
            public double seconds;
        }

        [MenuItem("Pocket Caravan/Build/macOS 고려 프리뷰")]
        public static void Mac()
        {
            if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Play를 종료하고 현재 씬을 저장한 뒤 빌드하세요.");
            const string folder = "Builds/macOS";
            Directory.CreateDirectory(folder);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] {
                    "Assets/PocketCaravan/Scenes/Goryeo/Naru.unity",
                    "Assets/PocketCaravan/Scenes/Goryeo/Route01.unity",
                    "Assets/PocketCaravan/Scenes/Goryeo/Gaegyeong.unity",
                    "Assets/PocketCaravan/Scenes/Goryeo/Dakmok.unity"
                },
                locationPathName = folder + "/PocketCaravan.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None,
                extraScriptingDefines = new[] { "CARAVAN_PREVIEW" }
            });
            var summary = report.summary;
            File.WriteAllText(folder + "/build-report.json", JsonUtility.ToJson(new Result {
                result = summary.result.ToString(), output = summary.outputPath,
                errors = (int)summary.totalErrors, warnings = (int)summary.totalWarnings,
                bytes = summary.totalSize, seconds = summary.totalTime.TotalSeconds,
                builtAtUtc = DateTime.UtcNow.ToString("o")
            }, true));
            if (summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("프리뷰 빌드 실패: Builds/macOS/build-report.json을 확인하세요.");
            Debug.Log("고려 macOS 프리뷰 빌드 완료. pocket_caravan_preview_save.json을 사용합니다.");
        }
    }
}
