using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 10/27 통합: 메뉴 하나로 네 지역 데이터 등록 + 씬 생성 + 문명 연결 맵 반영.
    /// 각 지역 담당 메뉴를 그대로 불러서 지역별 덧입힘(베네치아 구역·고양이, 아라비아 캐릭터·카메라 등)을 지킨다.
    /// 고려 데이터는 합친 GameDatabase에 이미 들어 있다. 고려 씬은 연결이 바뀐 맵만 다시 만든다.
    /// "1. 시작 세트 만들기"는 GameDatabase를 처음부터 다시 쓰므로 통합 후에는 쓰지 않는다.
    /// </summary>
    public static class RegionLinksBuilder
    {
        const string LinksPath = "Assets/PocketCaravan/World/Connections/region_links.json";

        [Serializable] class Side { public string map; }
        [Serializable] class Link { public Side a; public Side b; }
        [Serializable] class Links { public List<Link> links = new List<Link>(); }

        [MenuItem("Pocket Caravan/통합/전체 준비 (네 지역 데이터 + 씬 + 연결)", priority = 59)]
        public static void BuildAll()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[통합] Play를 멈춘 뒤 실행하세요."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            if (!Step("아라비아 데이터", ArabiaRegionSetup.ApplyData)) return;
            if (!Step("베네치아 데이터", VeniceRegionSetup.ApplyData)) return;
            if (!Step("북유럽 데이터", () => { if (!NorthBuilder.BuildData()) throw new Exception("북유럽 데이터 등록 실패 (위 로그 참고)"); })) return;
            // 데이터가 다 들어간 뒤에 씬 (공용 검사가 등록부 전체 마을을 GameDatabase에서 찾기 때문)
            if (!Step("베네치아 씬", VeniceRegionSetup.Build)) return;
            if (!Step("아라비아 씬", ArabiaRegionSetup.BuildScenes)) return;
            if (!Step("북유럽 씬", () => MapJsonBuilder.BuildSelected(
                WorldRegistry.Load().maps.FindAll(m => m.mapId.StartsWith("north_")).ConvertAll(m => m.mapId).ToArray()))) return;
            if (!Step("연결된 고려 맵 씬", BuildLinkedOthers)) return;
            AssetDatabase.SaveAssets();
            Debug.Log("[통합] 전체 준비 끝. Tests > 지역 통합 등록 검사 → north_c_crossing에서 Play로 연결을 걸어서 확인하세요.");
        }

        /// <summary>연결 목록에 든 맵 중 지역 메뉴가 만들지 않는 맵(고려 쪽)만 다시 만든다.</summary>
        [MenuItem("Pocket Caravan/통합/연결된 고려 맵 씬만 다시 만들기", priority = 60)]
        public static void BuildLinkedOthers()
        {
            var spec = JsonUtility.FromJson<Links>(File.ReadAllText(LinksPath));
            var ids = new List<string>();
            foreach (var l in spec.links)
                foreach (var s in new[] { l.a, l.b })
                    if (s != null && !string.IsNullOrEmpty(s.map) && !ids.Contains(s.map)
                        && !s.map.StartsWith("north_") && !s.map.StartsWith("venice_") && !s.map.StartsWith("arabia_"))
                        ids.Add(s.map);
            if (ids.Count == 0) return;
            MapJsonBuilder.BuildSelected(ids.ToArray());
            Debug.Log($"[통합] 연결된 맵 {ids.Count}개를 다시 만들었습니다: {string.Join(", ", ids)}");
        }

        static bool Step(string name, Action act)
        {
            try { act(); Debug.Log($"[통합] {name} 완료"); return true; }
            catch (Exception e) { Debug.LogError($"[통합] {name} 실패: {e.Message}\n{e}"); return false; }
        }
    }
}
