using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    public static class C6ProductionChecks
    {
        [Serializable] class Dependencies {public string[] pngs;}
        public static void ReadDependencies()
        {
            var paths=WorldRegistry.Load().maps.Where(e=>e.region.regionId=="goryeo"||e.region.regionId=="transit").Select(e=>e.scenePath).ToArray();
            var images=AssetDatabase.GetDependencies(paths,true).Where(p=>p.EndsWith(".png",StringComparison.OrdinalIgnoreCase)).Distinct().OrderBy(p=>p).ToArray();
            File.WriteAllText("Docs/Validation/C6-Production-2026-10-26/scene-image-dependencies.json",JsonUtility.ToJson(new Dependencies{pngs=images},true));
        }
        public static void Run()
        {
            const string output="Docs/Validation/C6-Production-2026-10-26/";
            C3ProductionChecks.RunTo(output+"unity-scenes.json");
            var retired=new[]{"Route01","goryeo_descent","goryeo_fieldroad","goryeo_ridge","goryeo_rivervalley","goryeo_southcoast","goryeo_tidalcoast","goryeo_westcoast"};
            EditorBuildSettings.scenes=EditorBuildSettings.scenes.Where(s=>!retired.Contains(Path.GetFileNameWithoutExtension(s.path))).ToArray();
            foreach(var id in retired)
            {
                string target=GoryeoLayout.Resolve(id);if(target==id)throw new Exception("Missing legacy alias: "+id);
                var entry=WorldRegistry.Load().Map(target);if(entry==null||!File.Exists(entry.scenePath))throw new Exception("Legacy alias target missing: "+id);
            }
            File.WriteAllText(output+"save.json",JsonUtility.ToJson(SaveCompatibilityChecks.RunData(),true));
            AssetDatabase.SaveAssets();Debug.Log("C6 legacy aliases: all 8 resolve to existing registered scenes");
        }
    }
}
