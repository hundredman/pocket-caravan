using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>지역 담당자는 World/Regions의 자기 JSON만 편집한다. 런타임 저장 ID는 변경하지 않는다.</summary>
    public static class WorldRegistry
    {
        public const string DirectoryPath = "Assets/PocketCaravan/World/Regions";
        const string SettingsPath = "Assets/PocketCaravan/World/settings.json";

        [Serializable] public class Settings { public int schemaVersion; public string startMapId; }
        [Serializable] public class MapEntry
        {
            public string mapId, jsonPath, scenePath, generator;
            public int worldX, worldY;
            [NonSerialized] public RegionEntry region;
        }
        [Serializable] public class NpcEntry { public string id, name, sprite, greeting; }
        [Serializable] public class VillagerEntry { public string name, sprite; public string[] lines; }
        [Serializable] public class RegionEntry
        {
            public int schemaVersion;
            public string regionId, regionAssetPath;
            public MapEntry[] maps;
            public NpcEntry[] npcs;
            public VillagerEntry[] villagers;
        }

        public sealed class Snapshot
        {
            public string startMapId;
            public List<RegionEntry> regions = new List<RegionEntry>();
            public List<MapEntry> maps = new List<MapEntry>();
            public MapEntry Map(string id) => maps.Find(m => m.mapId == id);
            public string[] ScenePaths => maps.Select(m => m.scenePath).ToArray();
        }

        public static Snapshot Load()
        {
            var settings = JsonUtility.FromJson<Settings>(File.ReadAllText(SettingsPath));
            if (settings == null || settings.schemaVersion != 1) throw new InvalidDataException("월드 설정 버전을 확인하세요.");
            var result = new Snapshot { startMapId = settings.startMapId };
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var regions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var coords = new HashSet<Vector2Int>();
            foreach (var file in Directory.GetFiles(DirectoryPath, "*.json").OrderBy(p => p, StringComparer.Ordinal))
            {
                var r = JsonUtility.FromJson<RegionEntry>(File.ReadAllText(file));
                if (r == null || r.schemaVersion != 1 || string.IsNullOrWhiteSpace(r.regionId) || !regions.Add(r.regionId))
                    throw new InvalidDataException($"지역 ID/버전 중복 또는 누락: {file}");
                CheckAssetPath(r.regionAssetPath, ".asset");
                var regionAsset = AssetDatabase.LoadAssetAtPath<RegionData>(r.regionAssetPath);
                if (regionAsset == null || regionAsset.id != r.regionId)
                    throw new InvalidDataException($"RegionData와 regionId가 다르거나 없습니다: {file}");
                if (r.maps == null || r.maps.Length == 0) throw new InvalidDataException($"지역에 맵이 없습니다: {file}");
                foreach (var m in r.maps)
                {
                    if (m == null || string.IsNullOrWhiteSpace(m.mapId) || !ids.Add(m.mapId))
                        throw new InvalidDataException($"맵 ID 중복/누락: {file}");
                    CheckAssetPath(m.jsonPath, ".json"); CheckAssetPath(m.scenePath, ".unity");
                    if (Path.GetFileNameWithoutExtension(m.scenePath) != m.mapId)
                        throw new InvalidDataException($"목적 씬 이름은 mapId와 같아야 합니다: {m.mapId}");
                    if (!paths.Add(m.scenePath) || !coords.Add(new Vector2Int(m.worldX, m.worldY)))
                        throw new InvalidDataException($"씬 경로/논리 좌표가 중복됩니다: {m.mapId}");
                    if (!File.Exists(m.jsonPath)) throw new FileNotFoundException("등록 맵 JSON이 없습니다.", m.jsonPath);
                    m.region = r; result.maps.Add(m);
                }
                result.regions.Add(r);
            }
            var start = result.Map(result.startMapId);
            if (start == null) throw new InvalidDataException("시작 맵이 등록되어 있지 않습니다.");
            result.maps.Remove(start); result.maps.Insert(0, start);
            return result;
        }

        static void CheckAssetPath(string path, string extension)
        {
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("Assets/PocketCaravan/", StringComparison.Ordinal)
                || path.Contains("..") || path.Contains('\\') || !path.EndsWith(extension, StringComparison.Ordinal))
                throw new InvalidDataException($"본편 에셋 경로가 올바르지 않습니다: {path}");
        }
    }
}
