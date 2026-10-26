using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 아라비아 첫 구간 정적 검사 (Play 없이). 데이터·DB 등록·좌표·가격 손익·씬 객체·출구 쌍·기존 지역 연결 회귀.
    /// 씬은 추가로 열었다가 저장하지 않고 닫는다. 실제 플레이 검증은 ArabiaPlaytest가 맡는다.
    /// </summary>
    public static class ArabiaChecks
    {
        [MenuItem("Pocket Caravan/아라비아/4. 정적 검사 (데이터·씬)", priority = 220)]
        public static void RunMenu()
        {
            var (ok, text) = Run();
            ArabiaRegionSetup.WriteReport("static_checks.md", text);
            if (ok) Debug.Log("[아라비아] 정적 검사 통과\n" + text); else Debug.LogError("[아라비아] 정적 검사 실패\n" + text);
        }

        public static (bool ok, string text) Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play 종료 후 실행하세요.");
            var lines = new List<string>();
            bool all = true;
            void Check(string name, bool ok, string detail = "")
            {
                lines.Add($"- [{(ok ? "PASS" : "FAIL")}] {name}{(string.IsNullOrEmpty(detail) ? "" : " — " + detail)}");
                all &= ok;
            }

            try { MapJsonBuilder.ValidateWorld(); Check("공용 등록 검사 (MapJsonBuilder.ValidateWorld)", true); }
            catch (Exception e) { Check("공용 등록 검사 (MapJsonBuilder.ValidateWorld)", false, e.Message); }

            var registry = WorldRegistry.Load();
            Check("시작 맵은 그대로 Naru", registry.startMapId == "Naru", registry.startMapId);
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(ArabiaRegionSetup.DbPath);
            T Find<T>(List<T> list, string id) where T : ScriptableObject =>
                list.Find(x => x != null && (string)x.GetType().GetField("id").GetValue(x) == id);
            var ahsa = Find(db.towns, ArabiaRegionSetup.AhsaTown); var zabid = Find(db.towns, ArabiaRegionSetup.ZabidTown);
            var dates = Find(db.items, ArabiaRegionSetup.DatesId); var grain = Find(db.items, ArabiaRegionSetup.GrainId);
            var delivery = Find(db.items, ArabiaRegionSetup.DeliveryDatesId);
            var q1 = Find(db.quests, ArabiaRegionSetup.Q1Id); var q2 = Find(db.quests, ArabiaRegionSetup.Q2Id);
            Check("DB에 아라비아 마을·상품·퀘스트 등록", ahsa && zabid && dates && grain && delivery && q1 && q2);
            if (!(ahsa && zabid && dates && grain && delivery && q1 && q2)) return (false, string.Join("\n", lines));

            var towns = ArabiaRegionSetup.TownIds.Select(id => Find(db.towns, id)).ToList();
            Check("DB에 아라비아 5개 거점(수도1+마을4)·직물·향신료 등록", towns.All(t => t != null) && Find(db.items, ArabiaRegionSetup.ClothId) && Find(db.items, ArabiaRegionSetup.SpiceId),
                string.Join(", ", ArabiaRegionSetup.TownIds));
            if (towns.Any(t => t == null)) return (false, string.Join("\n", lines));
            foreach (var t in towns)
            {
                var entry = registry.Map(t.id);
                Check($"{t.id} 마을 좌표 = 등록 맵 좌표", entry != null && t.worldCoord == new Vector2Int(entry.worldX, entry.worldY), $"{t.worldCoord}");
            }
            var otherCoords = db.towns.Where(t => t != null && (t.region == null || t.region.id != "arabia")).Select(t => t.worldCoord).ToList();
            Check("아라비아 마을 좌표가 기존 마을과 겹치지 않음", towns.All(t => !otherCoords.Contains(t.worldCoord)));
            Check("의뢰 전용품은 판매 불가(questItem)", delivery.questItem && !dates.questItem && !grain.questItem);

            // 빈 등짐 적재량으로 Q1 수락 가능
            float capacity = db.startVehicle != null ? db.startVehicle.capacity : 10f;
            Check("Q1 맡긴 짐 무게 ≤ 빈 등짐 적재량", delivery.weight * q1.giveCount <= capacity, $"{delivery.weight * q1.giveCount}/{capacity}");
            Check("Q2 선행 조건 = Q1", q2.requires.Count == 1 && q2.requires[0] == q1);
            Check("Q2 구매 단계 = 자비드 장터·곡물·2개", q2.objectives.Count == 2 && q2.objectives[0].type == ObjectiveType.Buy
                && q2.objectives[0].targetId == zabid.id && q2.objectives[0].item == grain && q2.objectives[0].count == 2);

            // 실제 시세 공식으로 구매비 계산 (평일/장날, 첫 방문 재고)
            int Cost(TownData town, ItemData item, int n, float demand)
            {
                float opt = town.OptimalFor(item) * 1f, stock = opt; int sum = 0;
                for (int i = 0; i < n; i++) { sum += MarketMath.Price(town.BasePriceFor(item), opt, stock, demand); stock -= 1f; }
                return sum;
            }
            int cost = Cost(zabid, grain, 2, 1f), costMarketDay = Cost(zabid, grain, 2, db.marketDayDemand);
            Check("Q2 보수 > 자비드 곡물 2개 실제 구매비 (평일)", q2.rewardMoney > cost, $"보수 {q2.rewardMoney} − 구매비 {cost} = {q2.rewardMoney - cost}");
            Check("Q2 보수 > 구매비 (장날)", q2.rewardMoney > costMarketDay, $"장날 구매비 {costMarketDay}");
            var session = AssetDatabase.LoadAssetAtPath<RegionTestSession>(ArabiaRegionSetup.SessionPath);
            Check("시험 세션 설정 (전용 저장 파일)", session != null && session.regionId == "arabia" && SaveSystem.IsValidSlotName(session.saveFileName)
                && session.saveFileName == ArabiaRegionSetup.SaveFileName, session != null ? session.saveFileName : "없음");
            if (session != null)
                Check("Q2 진입 자금: 시작 자금 + Q1 보수 ≥ 구매비", session.startMoney + q1.rewardMoney >= costMarketDay, $"{session.startMoney}+{q1.rewardMoney} ≥ {costMarketDay}");
            Check("교역 차익 참고: 자비드→알 아흐사 곡물 1개 판매가 − 구매가",
                true, $"{MarketMath.Buyback(MarketMath.Price(ahsa.BasePriceFor(grain), ahsa.OptimalFor(grain), ahsa.OptimalFor(grain) + 1))} − {Cost(zabid, grain, 1, 1f)}");

            // 씬 검사: 객체 배치·출구·스폰 쌍
            var scenes = new Dictionary<string, Scene>();
            var opened = new List<Scene>();
            var active = SceneManager.GetActiveScene();
            try
            {
                foreach (var id in ArabiaRegionSetup.MapIds)
                {
                    var entry = registry.Map(id);
                    bool inBuild = EditorBuildSettings.scenes.Any(s => s.path == entry.scenePath && s.enabled);
                    Check($"{id} 씬 존재·빌드 목록 포함", System.IO.File.Exists(entry.scenePath) && inBuild);
                    if (!System.IO.File.Exists(entry.scenePath)) continue;
                    var s = EditorSceneManager.OpenScene(entry.scenePath, OpenSceneMode.Additive);
                    scenes[id] = s; opened.Add(s);
                }
                List<T> In<T>(string id) where T : Component =>
                    scenes.TryGetValue(id, out var s) ? s.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToList() : new List<T>();
                foreach (var id in scenes.Keys)
                {
                    var root = In<MapRoot>(id).FirstOrDefault();
                    Check($"{id} MapRoot·지역·좌표", root != null && root.mapId == id && root.region != null && root.region.id == "arabia"
                        && root.worldCoord == new Vector2Int(registry.Map(id).worldX, registry.Map(id).worldY));
                    Check($"{id} GameManager 중복 배치 없음", In<GameManager>(id).Count == 0);
                }
                var npcs = scenes.Keys.SelectMany(id => In<QuestNpc>(id).Select(n => (id, n.npcId))).ToList();
                Check("NPC 배치: 하산·마리암·힌드바드 = 알 아흐사, 부엌 담당 = 자비드",
                    npcs.Contains((ArabiaRegionSetup.AhsaMap, "arabia_hasan")) && npcs.Contains((ArabiaRegionSetup.AhsaMap, "arabia_maryam"))
                    && npcs.Contains((ArabiaRegionSetup.AhsaMap, "arabia_hindbad")) && npcs.Contains((ArabiaRegionSetup.ZabidMap, "arabia_kitchen_keeper")),
                    string.Join(", ", npcs.Select(n => n.Item1 + ":" + n.Item2)));
                foreach (var t in towns)
                {
                    Check($"장터·게시판: {t.displayName} (장터 1곳, 마을 데이터 연결)",
                        In<TownMarket>(t.id).Count(m => m.town == t) == 1 && In<ContractBoard>(t.id).Any(b => b.town == t.id));
                }
                Check("알 아흐사 S0 시작 스폰", In<SpawnPoint>(ArabiaRegionSetup.AhsaMap).Any(s => s.id == SpawnPoint.StartId));
                int pairs = 0;
                foreach (var id in scenes.Keys)
                    foreach (var exit in In<MapExit>(id))
                    {
                        bool known = scenes.ContainsKey(exit.targetScene);
                        var spawn = known ? In<SpawnPoint>(exit.targetScene).FirstOrDefault(s => s.id == exit.targetSpawnId) : null;
                        bool back = known && In<MapExit>(exit.targetScene).Any(e => e.targetScene == id);
                        Check($"출구 {id} → {exit.targetScene}/{exit.targetSpawnId}", spawn != null && spawn.size == exit.size && back,
                            spawn == null ? "도착 스폰 없음" : $"폭 {exit.size}↔{spawn.size}, 역방향 {(back ? "있음" : "없음")}");
                        if (!exit.targetScene.StartsWith("arabia_")) Check($"출구 {id}가 아라비아 밖으로 연결되지 않음", false, exit.targetScene);
                        pairs++;
                    }
                Check("출구 수 = 22 (양방향 11쌍: 길 5개·사나로 가는 두 길·사나 북문↔중앙 대륙 남부, 미완성 외부 출구 없음)", pairs == 22, pairs.ToString());
            }
            finally
            {
                foreach (var s in opened) if (s.IsValid() && s != active) EditorSceneManager.CloseScene(s, true);
            }

            // 기존 지역 회귀: NPC 상인 거래 대상 필터가 기존 마을끼리의 연결을 끊지 않는지
            var world = AssetDatabase.LoadAssetAtPath<WorldIndex>(ArabiaRegionSetup.Root + "/Resources/WorldIndex.asset");
            var legacy = db.towns.Where(t => t != null && (t.region == null || t.region.id != "arabia")).ToList();
            var cut = new List<string>();
            foreach (var a in legacy) foreach (var b in legacy) if (a != b && !world.MarketsConnected(a.id, b.id)) cut.Add(a.id + "↔" + b.id);
            Check("기존 고려·중간권 마을끼리 장터 연결 유지 (NPC 상인 필터 회귀)", cut.Count == 0, cut.Count == 0 ? $"{legacy.Count}개 마을" : string.Join(", ", cut));
            Check("아라비아 ↔ 고려 장터는 미연결 (원격 거래 차단)", !world.MarketsConnected(ahsa.id, legacy[0].id), legacy[0].id);
            var unlinked = new List<string>();
            foreach (var a in towns) foreach (var b in towns) if (a != b && !world.MarketsConnected(a.id, b.id)) unlinked.Add(a.id + "↔" + b.id);
            Check("아라비아 5개 거점 장터 서로 연결", unlinked.Count == 0, unlinked.Count == 0 ? "모두 연결" : string.Join(", ", unlinked));

            var sb = new StringBuilder();
            sb.AppendLine($"# 아라비아 정적 검사 {DateTime.Now:yyyy-MM-dd HH:mm:ss} · Unity {Application.unityVersion}");
            sb.AppendLine(all ? "결과: 통과" : "결과: 실패 있음");
            foreach (var l in lines) sb.AppendLine(l);
            return (all, sb.ToString());
        }
    }
}
