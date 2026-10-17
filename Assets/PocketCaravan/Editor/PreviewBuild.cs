using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>등록된 본편 맵의 두 플랫폼 검토 빌드. 별도 저장 파일로 기존 플레이 진행을 보호한다.</summary>
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
        public static void Mac() => Build(BuildTarget.StandaloneOSX, "Builds/macOS", "PocketCaravan.app");

        [MenuItem("Pocket Caravan/Build/Windows 고려 프리뷰")]
        public static void Windows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows", "PocketCaravan.exe");

        [MenuItem("Pocket Caravan/Build/환경 원형 시험장 Windows")]
        public static void ModulesWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/ModuleLibrary/Windows", "PocketCaravanModules.exe", new[]{MapModuleWorkbench.ScenePath,"Assets/PocketCaravan/Scenes/ModuleLab/ModuleComposition.unity"});
        [MenuItem("Pocket Caravan/Build/환경 원형 시험장 macOS")]
        public static void ModulesMac() => Build(BuildTarget.StandaloneOSX, "Builds/ModuleLibrary/macOS", "PocketCaravanModules.app", new[]{MapModuleWorkbench.ScenePath,"Assets/PocketCaravan/Scenes/ModuleLab/ModuleComposition.unity"});

        static void Build(BuildTarget target, string folder, string file, string[] selectedScenes = null)
        {
            if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Play를 종료하고 현재 씬을 저장한 뒤 빌드하세요.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
                throw new InvalidOperationException("Unity Hub에서 해당 플랫폼 빌드 지원 모듈을 설치하세요: " + target);
            Directory.CreateDirectory(folder);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = selectedScenes ?? WorldRegistry.Load().ScenePaths,
                locationPathName = folder + "/" + file,
                target = target,
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
                throw new InvalidOperationException("프리뷰 빌드 실패: " + folder + "/build-report.json을 확인하세요.");
            Debug.Log("고려 " + target + " 프리뷰 빌드 완료. pocket_caravan_preview_save.json을 사용합니다.");
        }
    }
}
