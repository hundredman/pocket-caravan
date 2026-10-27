using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 10/27 통합: 문명 사이 연결(World/Connections/region_links.json)에 든 맵의 씬만 다시 만든다.
    /// 출구·스폰은 Tools/Integration/connect_regions.py(다른 지역 쪽)와 각 생성기(북유럽 쪽)가 맵 JSON에 넣는다.
    /// 공용 MapJsonBuilder.BuildSelected를 써서 연결과 상관없는 씬은 건드리지 않고, WorldIndex·빌드 목록은 등록부 전체로 다시 쓴다.
    /// </summary>
    public static class RegionLinksBuilder
    {
        const string LinksPath = "Assets/PocketCaravan/World/Connections/region_links.json";

        [Serializable] class Side { public string map; }
        [Serializable] class Link { public Side a; public Side b; }
        [Serializable] class Links { public List<Link> links = new List<Link>(); }

        [MenuItem("Pocket Caravan/통합/지역 연결 맵 씬 다시 만들기", priority = 60)]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[통합] Play를 멈춘 뒤 실행하세요."); return; }
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            var spec = JsonUtility.FromJson<Links>(File.ReadAllText(LinksPath));
            var ids = new List<string>();
            foreach (var l in spec.links)
                foreach (var s in new[] { l.a, l.b })
                    if (s != null && !string.IsNullOrEmpty(s.map) && !ids.Contains(s.map)) ids.Add(s.map);
            try { MapJsonBuilder.BuildSelected(ids.ToArray()); }
            catch (Exception e) { Debug.LogError($"[통합] 맵 생성 실패: {e.Message}"); return; }
            Debug.Log($"[통합] 연결 맵 {ids.Count}개를 다시 만들었습니다: {string.Join(", ", ids)}");
        }
    }
}
