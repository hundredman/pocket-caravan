using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    [InitializeOnLoad]
    public static class NatureIntegrationReview
    {
        public const string Root = "Assets/PocketCaravan/Editor/B2Preview/A7";
        public const string Output = "ArtReview/NatureBinding-20261029/Unity";
        const string Active = "Codex.NatureIntegrationReview";
        const string BuildBackupKey = "Codex.NatureIntegrationReview.BuildBackup";
        [Serializable] class SceneEntry { public string path; public bool enabled; }
        [Serializable] class SceneBackup { public SceneEntry[] scenes; }
        [Serializable] public class Record { public string test, detail; public bool passed; }
        [Serializable] public class Report { public bool playCompleted; public string error; public List<Record> checks = new List<Record>(); }
        public static Report Result = new Report();

        static NatureIntegrationReview()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    string backup = SessionState.GetString(BuildBackupKey, "");
                    if (!string.IsNullOrEmpty(backup))
                    {
                        var original = JsonUtility.FromJson<SceneBackup>(backup);
                        EditorBuildSettings.scenes = Array.ConvertAll(original.scenes, s => new EditorBuildSettingsScene(s.path, s.enabled));
                        File.Copy(Output + "/EditorBuildSettings.before", "ProjectSettings/EditorBuildSettings.asset", true);
                        SessionState.EraseString(BuildBackupKey);
                    }
                }
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Active, false)) return;
                Result = new Report();
                NaturePlayReviewRunner.Record = Check; NaturePlayReviewRunner.Complete = Finish;
                NaturePlayReviewRunner.ReviewRoot = Root; NaturePlayReviewRunner.Output = Output;
                var go = new GameObject("Nature integration review (temporary)");
                UnityEngine.Object.DontDestroyOnLoad(go); go.AddComponent<NaturePlayReviewRunner>();
            };
        }

        public static void Check(bool passed, string test, string detail = "")
        {
            Result.checks.Add(new Record { test = test, passed = passed, detail = detail });
            Save(); if (!passed) throw new InvalidOperationException(test + ": " + detail);
        }
        public static void Save()
        {
            Directory.CreateDirectory(Output); File.WriteAllText(Output + "/play-checks.json", JsonUtility.ToJson(Result, true));
        }
        public static void Finish(Exception error = null)
        {
            Result.error = error?.ToString(); Result.playCompleted = error == null; Save();
            SessionState.SetBool(Active, false); Time.timeScale = 1; EditorApplication.isPlaying = false;
            if (error != null) Debug.LogException(error);
        }

        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Unsaved scene");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            Directory.CreateDirectory(Root + "/Scenes"); Directory.CreateDirectory(Output);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string path in Directory.GetFiles(Root + "/Tiles", "*.png"))
                {
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) throw new InvalidOperationException("No texture importer: " + path);
                    if (importer.textureType == TextureImporterType.Sprite && importer.spritePixelsPerUnit == 16
                        && importer.filterMode == FilterMode.Point && !importer.mipmapEnabled
                        && importer.textureCompression == TextureImporterCompression.Uncompressed) continue;
                    importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 16; importer.filterMode = FilterMode.Point;
                    importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.alphaIsTransparency = true; importer.isReadable = false; importer.SaveAndReimport();
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            foreach (string path in Directory.GetFiles(Root + "/Maps", "*.json"))
                MapJsonBuilder.BuildSingleMap(File.ReadAllText(path), Root + "/Scenes/" + Path.GetFileNameWithoutExtension(path) + ".unity", Root);
            if (setup.Length > 0 && !string.IsNullOrEmpty(setup[0].path)) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        public static void Run() => BeginReview(true);

        [MenuItem("Pocket Caravan/Review/자연 배치 수동 검토 (저장 안 함)")]
        public static void OpenManual() => BeginReview(false);

        static void BeginReview(bool automatic)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first");
            SetGameSize();
            if (automatic) { Result = new Report(); Save(); }
            var scenes = EditorBuildSettings.scenes;
            var backup = new SceneBackup { scenes = Array.ConvertAll(scenes, s => new SceneEntry { path = s.path, enabled = s.enabled }) };
            SessionState.SetString(BuildBackupKey, JsonUtility.ToJson(backup));
            File.Copy("ProjectSettings/EditorBuildSettings.asset", Output + "/EditorBuildSettings.before", true);
            EditorBuildSettings.scenes = Array.ConvertAll(scenes, s =>
            {
                string candidate = Root + "/Scenes/" + Path.GetFileName(s.path);
                return new EditorBuildSettingsScene(File.Exists(candidate) ? candidate : s.path, s.enabled);
            });
            SessionState.SetBool(Active, automatic);
            EditorSceneManager.OpenScene(Root + "/Scenes/goryeo_songak.unity");
            EditorApplication.isPlaying = true;
        }

        static void SetGameSize()
        {
            // Existing PondProductionChecks uses this verified editor reflection path.
            var assembly = typeof(Editor).Assembly; var type = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(type);
            var sizes = singleton.GetProperty("instance").GetValue(null, null);
            var group = type.GetMethod("GetGroup").Invoke(sizes, new object[] { 0 });
            var kind = assembly.GetType("UnityEditor.GameViewSizeType");
            var size = Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"), new object[] { Enum.ToObject(kind, 1), 480, 270, "Nature review 480x270" });
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            var view = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
            view.GetType().GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(view, count - 1, null);
            view.Focus(); view.Repaint();
        }
    }
}
