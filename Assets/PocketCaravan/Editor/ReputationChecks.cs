using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>Real Unity JsonUtility and temporary-file checks. Never reads/writes the player's save path.</summary>
    public static class ReputationChecks
    {
        [Serializable] public class Report
        {
            public string unityVersion;
            public string mode = "Unity editor data/JsonUtility/temp-file; no gameplay";
            public List<string> passed = new List<string>();
            public List<string> existingSaveChecks = new List<string>();
            public string error;
        }

        static void Check(Report report, string name, bool condition)
        {
            if (!condition) throw new Exception(name);
            report.passed.Add(name);
        }

        static SaveData Legacy(int version = 2) => new SaveData { version = version, money = 77, credit = -2, stamina = 1, day = 3, minute = 480 };

        [MenuItem("Pocket Caravan/Tests/Reputation data and save")]
        public static void RunMenu() => Debug.Log(JsonUtility.ToJson(RunData(), true));

        public static Report RunData()
        {
            var report = new Report { unityVersion = Application.unityVersion };
            var region = ScriptableObject.CreateInstance<RegionData>(); region.id = "goryeo";
            var origin = ScriptableObject.CreateInstance<OriginData>(); origin.id = "goryeo"; origin.homeRegion = region;
            var changedOrigin = ScriptableObject.CreateInstance<OriginData>(); changedOrigin.id = "arabia";
            var db = ScriptableObject.CreateInstance<GameDatabase>();
            string file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pocket-caravan-r1-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var state = new ReputationState(); state.Initialize(ReputationState.HomeOf(origin));
                Check(report, "home civilization starts Friendly", state.Get(ReputationScope.Civilization, "goryeo").standing == ReputationStanding.Friendly);
                foreach (string id in new[] { "arabia", "venice", "viking" })
                    Check(report, id + " starts Neutral", state.Get(ReputationScope.Civilization, id).standing == ReputationStanding.Neutral);
                Check(report, "central village starts Neutral", state.Get(ReputationScope.Town, "east_gate").standing == ReputationStanding.Neutral);
                state.Set(ReputationScope.Town, "east_gate", ReputationStanding.Trusted, 7);
                Check(report, "central villages are independent", state.Get(ReputationScope.Town, "other_port").standing == ReputationStanding.Neutral);
                state.Set(ReputationScope.Town, "goryeo", ReputationStanding.Wary, 3);
                Check(report, "civilization and town key namespaces do not collide", state.Get(ReputationScope.Civilization, "goryeo").standing == ReputationStanding.Friendly);
                var save = Legacy(); state.ExportTo(save);
                SaveCompatibility.Validate(save, db, null);
                var loaded = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save));
                var restored = new ReputationState(); restored.ImportFrom(loaded, changedOrigin);
                Check(report, "Unity JSON preserves town standing and progress", restored.Get(ReputationScope.Town, "east_gate").standing == ReputationStanding.Trusted && restored.Get(ReputationScope.Town, "east_gate").progress == 7);
                Check(report, "saved home survives configured origin change", restored.HomeCivilizationId == "goryeo");
                Check(report, "cash and credit remain unchanged", loaded.money == 77 && loaded.credit == -2);
                state.Set(ReputationScope.Civilization, "goryeo", ReputationStanding.Hostile, 12); state.ExportTo(save);
                restored.ImportFrom(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save)), origin);
                Check(report, "reloading does not regrant Friendly", restored.Get(ReputationScope.Civilization, "goryeo").standing == ReputationStanding.Hostile);
                foreach (int version in new[] { 1, 2 })
                {
                    var legacy = Legacy(version); string before = JsonUtility.ToJson(legacy);
                    restored.ImportFrom(legacy, origin);
                    Check(report, "legacy v" + version + " initializes without payload mutation", restored.Get(ReputationScope.Civilization, "goryeo").standing == ReputationStanding.Friendly && before == JsonUtility.ToJson(legacy));
                    restored.Set(ReputationScope.Town, "east_gate", ReputationStanding.Wary, 4); restored.ExportTo(legacy);
                    var next = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(legacy)); restored.ImportFrom(next, changedOrigin);
                    Check(report, "legacy v" + version + " second load keeps migrated state", next.version == 3 && restored.Get(ReputationScope.Town, "east_gate").progress == 4 && restored.HomeCivilizationId == "goryeo");
                }
                var noVersion = JsonUtility.FromJson<SaveData>("{\"money\":77,\"credit\":-2,\"stamina\":1,\"day\":3,\"minute\":480}");
                SaveSystem.Validate(noVersion); restored.ImportFrom(noVersion, origin);
                Check(report, "original missing-version legacy JSON remains loadable", restored.HomeCivilizationId == "goryeo");
                foreach (ReputationStanding standing in Enum.GetValues(typeof(ReputationStanding)))
                {
                    state.Set(ReputationScope.Town, "stage_check", standing, 11); state.ExportTo(save);
                    restored.ImportFrom(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save)), origin);
                    Check(report, "Unity JSON standing roundtrip " + standing, restored.Get(ReputationScope.Town, "stage_check").standing == standing);
                }
                state.Set(ReputationScope.Town, "future_team_town", ReputationStanding.Respected, int.MaxValue);
                state.ExportTo(save); restored.ImportFrom(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save)), origin);
                Check(report, "unknown valid future town is retained", restored.Get(ReputationScope.Town, "future_team_town").progress == int.MaxValue);
                void Reject(string name, Action<ReputationState.Save> mutate)
                {
                    var invalid = state.Export(); mutate(invalid); string before = JsonUtility.ToJson(restored.Export()); bool refused = false;
                    try { restored.Import(invalid); } catch (InvalidDataException) { refused = true; }
                    Check(report, name, refused && before == JsonUtility.ToJson(restored.Export()));
                }
                Reject("future schema rejected atomically", r => r.schemaVersion = 9);
                Reject("duplicate entries rejected atomically", r => r.entries.Add(r.entries[0]));
                Reject("negative progress rejected atomically", r => r.entries[0].progress = -1);
                Reject("invalid stage rejected atomically", r => r.entries[0].standing = (ReputationStanding)99);
                Reject("invalid scope rejected atomically", r => r.entries[0].scope = (ReputationScope)99);
                Reject("missing home rejected atomically", r => r.entries.RemoveAll(e => e.scope == ReputationScope.Civilization && e.targetId == r.homeCivilizationId));
                bool missing = false; try { SaveSystem.Validate(new SaveData { version = 3 }); } catch (InvalidDataException) { missing = true; }
                Check(report, "v3 missing reputation cannot reset silently", missing);
                bool wireMissing = false;
                try { SaveSystem.Validate(JsonUtility.FromJson<SaveData>("{\"version\":3,\"money\":77,\"stamina\":1,\"day\":3,\"minute\":480}")); }
                catch (InvalidDataException) { wireMissing = true; }
                Check(report, "v3 absent inline block in real JSON is rejected", wireMissing);
                Check(report, "real temporary save write", SaveSystem.WriteAt(file, save));
                loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(file)); SaveCompatibility.Validate(loaded, db, null); restored.ImportFrom(loaded, origin);
                Check(report, "temporary file roundtrip keeps values", restored.Get(ReputationScope.Town, "future_team_town").progress == int.MaxValue);
                save.money = 88; Check(report, "replacement creates backup", SaveSystem.WriteAt(file, save) && File.Exists(file + ".bak"));
                byte[] original = File.ReadAllBytes(file); var bad = Legacy(); bad.version = 3;
                Check(report, "bad reputation cannot overwrite valid save", !SaveSystem.WriteAt(file, bad) && Convert.ToBase64String(original) == Convert.ToBase64String(File.ReadAllBytes(file)));
                state.Initialize(null);
                Check(report, "isolated session reset is neutral", state.Get(ReputationScope.Civilization, "goryeo").standing == ReputationStanding.Neutral);
                return report;
            }
            finally
            {
                foreach (string suffix in new[] { "", ".bak", ".tmp" }) if (File.Exists(file + suffix)) File.Delete(file + suffix);
                UnityEngine.Object.DestroyImmediate(db); UnityEngine.Object.DestroyImmediate(changedOrigin);
                UnityEngine.Object.DestroyImmediate(origin); UnityEngine.Object.DestroyImmediate(region);
            }
        }

        public static void RunBatch()
        {
            Report report = null;
            try
            {
                report = RunData();
                report.existingSaveChecks = SaveCompatibilityChecks.RunData().passed;
            }
            catch (Exception e)
            {
                if (report == null) report = new Report { unityVersion = Application.unityVersion };
                report.error = e.ToString();
            }
            string output = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Docs/Validation/R1-Unity-2026-10-24.json"));
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            EditorApplication.Exit(report.error == null ? 0 : 1);
        }
    }
}
