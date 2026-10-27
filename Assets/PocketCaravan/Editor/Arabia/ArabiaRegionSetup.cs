using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 아라비아 지역(수도1+마을4+길5 = 10개 씬, Q1·Q2) 데이터와 씬을 만든다.
    /// - 기존 GameDatabase 항목·GUID·값은 유지하고 아라비아 항목만 추가한다 ('시작 세트 만들기'를 쓰지 않음).
    /// - 씬은 아라비아 3개만 선택 생성한다 (전체 재생성 금지). 공용 부수 효과는 보고서에 기록한다.
    /// 수치(가격·보수·무게)는 실험값이며 팀 확정값이 아니다.
    /// </summary>
    public static class ArabiaRegionSetup
    {
        public const string Root = "Assets/PocketCaravan";
        public const string DataDir = Root + "/Data/Arabia";
        public const string QuestDir = DataDir + "/Quests";
        public const string SessionPath = Root + "/Resources/RegionSessions/arabia.asset";
        public const string DbPath = Root + "/Resources/GameDatabase.asset";
        public const string ReportDir = "Docs/Arabia/Reports";

        public const string RegionId = "arabia";
        public const string AhsaMap = "arabia_ahsa", FarmroadMap = "arabia_farmroad", ZabidMap = "arabia_zabid";
        public const string WorkshopMap = "arabia_workshoproad", TaizMap = "arabia_taiz", PortroadMap = "arabia_portroad", JeddahMap = "arabia_jeddah";
        public const string ShortcutMap = "arabia_shortcut", SanaaMap = "arabia_sanaa", HighroadMap = "arabia_highroad";
        // 중앙 대륙 남쪽 구역 (2026-10-26 회의: 아라비아 담당) — 같은 틀 320×160을 6조각으로 나눈 탐험형 씬
        public const string CentralDunes = "arabia_central_dunes", CentralCanyon = "arabia_central_canyon";
        public const string CentralWest = "arabia_central_westvale", CentralBazaar = "arabia_central_bazaar";
        public const string CentralEast = "arabia_central_eastvale", CentralSteppe = "arabia_central_steppe";
        // 수도1(사나)+마을4(알 아흐사·자비드·타이즈·제다)+길5 = 10개 씬 (Docs/Arabia/02 §1) + 중앙 대륙 남쪽 구역 6개
        public static readonly string[] MapIds = { AhsaMap, FarmroadMap, ZabidMap, WorkshopMap, TaizMap, PortroadMap, JeddahMap, ShortcutMap, SanaaMap, HighroadMap,
            CentralDunes, CentralCanyon, CentralWest, CentralBazaar, CentralEast, CentralSteppe };
        public const string AhsaTown = "arabia_ahsa", ZabidTown = "arabia_zabid";
        public const string TaizTown = "arabia_taiz", JeddahTown = "arabia_jeddah", SanaaTown = "arabia_sanaa";
        public static readonly string[] TownIds = { AhsaTown, ZabidTown, TaizTown, JeddahTown, SanaaTown };
        public const string ClothId = "arabia_cloth", SpiceId = "arabia_spice";
        public const string DatesId = "arabia_dates", GrainId = "arabia_grain", DeliveryDatesId = "arabia_delivery_dates";
        public const string Q1Id = "arabia_q01_first_delivery", Q2Id = "arabia_q02_supper";
        public const string SaveFileName = "pocket_caravan_arabia_test_save.json";

        // 실험값 (Docs/Arabia/02 §5). 플레이 평가 후 조정한다.
        public const int Q1RewardMoney = 20, Q1RewardCredit = 1, Q2RewardMoney = 30, Q2RewardCredit = 1;
        public const int Q1Count = 2, Q2Count = 2;

        [MenuItem("Pocket Caravan/아라비아/1. 첫 구간 데이터 갱신 (DB 보존·추가)", priority = 200)]
        public static void ApplyData()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play 종료 후 실행하세요.");
            EnsureFolder(DataDir); EnsureFolder(QuestDir); EnsureFolder(Path.GetDirectoryName(SessionPath).Replace('\\', '/'));
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DbPath);
            if (db == null) throw new InvalidOperationException("GameDatabase가 없습니다. 시작 세트를 새로 만들지 말고 원본 프로젝트의 DB를 확인하세요.");
            int items0 = db.items.Count, towns0 = db.towns.Count, quests0 = db.quests.Count, vehicles0 = db.vehicles.Count;
            var keepItems = new List<ItemData>(db.items); var keepTowns = new List<TownData>(db.towns);
            var keepQuests = new List<QuestData>(db.quests); var keepVehicles = new List<VehicleData>(db.vehicles);
            int startMoney = db.startMoney; var startVehicle = db.startVehicle; var startOrigin = db.startOrigin;

            var region = Get<RegionData>(DataDir + "/Region_Arabia.asset");
            region.id = RegionId; region.displayName = "아라비아";
            region.note = "아라비아 첫 구간(시험). 실제 지리·도시 규모·위계의 재현이 아닌 게임용 압축 배치. 논리 좌표 x20~22는 팀 합의안의 아라비아 구역 제안을 로컬 시험에 쓴 값이며 확정이 아님.";
            EditorUtility.SetDirty(region);

            TownData Town(string file, string id, string label, int x, string note)
            {
                var t = Get<TownData>(DataDir + "/" + file + ".asset");
                t.id = id; t.displayName = label; t.region = region; t.journeyNote = note;
                t.worldCoord = new Vector2Int(x, 0); t.startLevel = 1; t.settlementTier = 1; t.defaultOptimal = 3;
                t.facilities = new List<TownFacility> {
                    new TownFacility { id = id + "_rest", displayName = "여관 쉼터", kind = FacilityKind.Rest, minTier = 1, minCredit = 0 }
                };
                t.vehiclesForSale = new List<VehicleData>();
                if (!db.towns.Contains(t)) db.towns.Add(t);
                EditorUtility.SetDirty(t);
                return t;
            }
            var ahsa = Town("Town_ArabiaAhsa", AhsaTown, "알 아흐사", 20, "대추야자 공급지, 곡물 소비지. 첫 배달과 여관 의뢰의 출발점.");
            var zabid = Town("Town_ArabiaZabid", ZabidTown, "자비드", 22, "곡물 공급지, 대추야자 소비지. 공동 부엌과 곡물 장터가 입구 가까이에 있다.");
            var taiz = Town("Town_ArabiaTaiz", TaizTown, "타이즈", 24, "염색 공방 마을. 직물 공급지, 생활 식량 소비지.");
            var jeddah = Town("Town_ArabiaJeddah", JeddahTown, "제다", 26, "교역 중계 항구. 배로 들어온 향신료 공급지, 사나로 가는 두 길의 출발점.");
            var sanaa = Town("Town_ArabiaSanaa", SanaaTown, "사나", 28, "수도. 여러 산지 상품의 큰 소비처.");
            taiz.settlementTier = 2; jeddah.settlementTier = 2; sanaa.settlementTier = 3;   // 02 §2 제안 등급 (실험값)

            Sprite Icon(string itemId) { var i = keepItems.Find(x => x != null && x.id == itemId); return i != null ? i.icon : null; }
            ItemData Item(string file, string id, string label, float weight, float price, TownData origin, bool quest, Sprite icon)
            {
                var i = Get<ItemData>(DataDir + "/" + file + ".asset");
                i.id = id; i.displayName = label; i.weight = weight; i.basePrice = price; i.originTown = origin;
                i.pricePerMap = 0.35f; i.questItem = quest; i.icon = icon;
                if (!db.items.Contains(i)) db.items.Add(i);
                EditorUtility.SetDirty(i);
                return i;
            }
            // 임시 아이콘: 공용 아이콘 재사용 (P2 전용 아이콘 전까지)
            var dates = Item("Item_ArabiaDates", DatesId, "대추야자", 1, 6, ahsa, false, Icon("herbs_parcel"));
            var grain = Item("Item_ArabiaGrain", GrainId, "곡물 꾸러미", 1, 5, zabid, false, Icon("grain"));
            var delivery = Item("Item_ArabiaDeliveryDates", DeliveryDatesId, "맡긴 대추야자", 2, 6, null, true, db.crateIcon);
            var cloth = Item("Item_ArabiaCloth", ClothId, "직물 꾸러미", 1, 9, taiz, false, Icon("hanji_parcel"));
            var spice = Item("Item_ArabiaSpice", SpiceId, "향신료 자루", 1, 12, jeddah, false, Icon("salt_parcel"));

            void Stock(TownData town, params (ItemData item, float optimal)[] entries)
            {
                town.stock = entries.Select(e => new TownData.StockEntry { item = e.item, optimal = e.optimal, localPriceMultiplier = 1f }).ToList();
                EditorUtility.SetDirty(town);
            }
            Stock(ahsa, (dates, 40), (grain, 12));
            Stock(zabid, (grain, 40), (dates, 12));
            Stock(taiz, (cloth, 40), (grain, 14), (dates, 12));
            Stock(jeddah, (spice, 40), (cloth, 16), (dates, 14), (grain, 14));
            Stock(sanaa, (cloth, 14), (spice, 14), (grain, 18), (dates, 18));

            var q1 = Get<QuestData>(QuestDir + "/Quest_ArabiaQ01FirstDelivery.asset");
            q1.id = Q1Id; q1.title = "힌드바드의 반쪽 짐"; q1.chain = "아라비아 1장";
            q1.summary = "초보 짐꾼 힌드바드에게서 대추야자 꾸러미 일부를 맡아 자비드 공동 부엌 담당에게 전한다.";
            q1.giverNpc = "arabia_hindbad"; q1.requires = new List<QuestData>();
            q1.offerLine = "짐을 잘 나른다는 건 혼자 다 든다는 뜻은 아니더라. 이 묶음을 함께 맡아 줄래?\n자비드의 공동 부엌으로 가면 돼. 무거우면 천천히 걸어도 괜찮아.";
            q1.giveItem = delivery; q1.giveCount = Q1Count;
            q1.offerItem = null; q1.offerCount = 0; q1.offerPrice = 0;
            q1.objectives = new List<QuestObjective> {
                new QuestObjective { type = ObjectiveType.Deliver, text = "자비드 공동 부엌 담당에게 맡긴 대추야자 전하기",
                    targetId = "arabia_kitchen_keeper", item = delivery, count = Q1Count,
                    completeLine = "기다리던 짐이구나. 네 이름도 장부에 적어 둘게." }
            };
            q1.rewardMoney = Q1RewardMoney; q1.rewardCredit = Q1RewardCredit;
            q1.finishNote = "알 아흐사 여관의 마리암이 다음 일을 부탁하려 한다.";
            if (!db.quests.Contains(q1)) db.quests.Add(q1);
            EditorUtility.SetDirty(q1);

            var q2 = Get<QuestData>(QuestDir + "/Quest_ArabiaQ02Supper.asset");
            q2.id = Q2Id; q2.title = "여관의 저녁 준비"; q2.chain = "아라비아 1장";
            q2.summary = "자비드 장터에서 곡물 꾸러미 2개를 직접 사서 알 아흐사 여관의 마리암에게 납품한다. 보수에서 산 값을 뺀 것이 실제 이익이다.";
            q2.giverNpc = "arabia_maryam"; q2.requires = new List<QuestData> { q1 };
            q2.offerLine = "곡물은 자비드에서 사 오렴. 물건값을 빼도 네 몫이 남도록 줄게.\n수락한 뒤 자비드 장터에서 산 것만 장부에 남아. 곡물 꾸러미는 보통 개당 5냥 안팎이야.";
            q2.giveItem = null; q2.giveCount = 0; q2.offerItem = null; q2.offerCount = 0; q2.offerPrice = 0;
            q2.objectives = new List<QuestObjective> {
                new QuestObjective { type = ObjectiveType.Buy, text = "자비드 장터에서 곡물 꾸러미 사기", targetId = ZabidTown, item = grain, count = Q2Count,
                    completeLine = "" },
                new QuestObjective { type = ObjectiveType.Deliver, text = "알 아흐사 여관의 마리암에게 곡물 꾸러미 전하기",
                    targetId = "arabia_maryam", item = grain, count = Q2Count,
                    completeLine = "이제 한 줄을 네 힘으로 채웠구나. 다음에 무엇을 나를지는 네가 골라 보렴." }
            };
            q2.rewardMoney = Q2RewardMoney; q2.rewardCredit = Q2RewardCredit;
            q2.finishNote = "장부에 산 값을 적어 두렴. 받은 돈이 전부 이익은 아니니까.";
            if (!db.quests.Contains(q2)) db.quests.Add(q2);
            EditorUtility.SetDirty(q2);

            var session = Get<RegionTestSession>(SessionPath);
            session.regionId = RegionId; session.startScene = AhsaMap; session.saveFileName = SaveFileName;
            session.startMoney = startMoney;
            session.startOrigin = AssetDatabase.LoadAssetAtPath<OriginData>(Root + "/Data/Origins/Origin_Arabia.asset");
            session.startVehicle = startVehicle;
            session.note = "아라비아 씬에서 Play하면 본편 저장과 분리된 전용 파일로 진행한다. 시작 자금은 공용 GameDatabase.startMoney 값을 복사한 실험값.";
            EditorUtility.SetDirty(session);

            // 기존 목록·시작 설정이 그대로인지 확인 (추가만 허용)
            bool kept = keepItems.All(db.items.Contains) && keepTowns.All(db.towns.Contains) && keepQuests.All(db.quests.Contains)
                && keepVehicles.SequenceEqual(db.vehicles) && db.startMoney == startMoney && db.startVehicle == startVehicle && db.startOrigin == startOrigin
                && db.items.Take(items0).SequenceEqual(keepItems) && db.towns.Take(towns0).SequenceEqual(keepTowns) && db.quests.Take(quests0).SequenceEqual(keepQuests);
            if (!kept) throw new InvalidOperationException("기존 GameDatabase 항목이 바뀌었습니다. 저장하지 않습니다.");
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log($"[아라비아] 데이터 갱신: 상품 {items0}→{db.items.Count}, 마을 {towns0}→{db.towns.Count}, 퀘스트 {quests0}→{db.quests.Count}, 운반 수단 {vehicles0}→{db.vehicles.Count} (기존 순서·참조 유지)");
        }

        static readonly string[] SharedWatch =
        {
            Root + "/Resources/WorldIndex.asset", Root + "/Resources/GameDatabase.asset", Root + "/Settings/PostFX.asset",
            Root + "/Resources/WorldMap/GoryeoTerrain.png.meta", "ProjectSettings/EditorBuildSettings.asset",
        };

        [MenuItem("Pocket Caravan/아라비아/2. 아라비아 씬만 생성 (선택 생성)", priority = 201)]
        public static void BuildScenes()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play 종료 후 실행하세요.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var others = Directory.GetFiles(Root + "/Scenes", "*.unity", SearchOption.AllDirectories)
                .Select(p => p.Replace('\\', '/')).Where(p => !p.Contains("/Scenes/Arabia/")).OrderBy(p => p).ToArray();
            var before = Hashes(SharedWatch.Concat(others));
            var buildBefore = EditorBuildSettings.scenes.Select(s => new EditorBuildSettingsScene(s.path, s.enabled)).ToList();

            ArabiaSceneDressing.EnsureCharacterImport();
            MapJsonBuilder.BuildSelected(MapIds);
            // 아라비아 전용 덧입힘 (플레이어·상인 그림, 카메라). 아라비아 씬만 연다.
            foreach (var id in MapIds) ArabiaSceneDressing.Apply(WorldRegistry.Load().Map(id).scenePath);

            // 빌드 목록: 기존 순서를 보존하고 아라비아 씬만 뒤에 붙인다 (빌드 0번 Naru 유지).
            var arabiaScenes = WorldRegistry.Load().maps.Where(m => m.region.regionId == RegionId).Select(m => m.scenePath).ToList();
            var list = new List<EditorBuildSettingsScene>(buildBefore.Where(s => !arabiaScenes.Contains(s.path)));
            foreach (var path in arabiaScenes) list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();

            var after = Hashes(SharedWatch.Concat(others));
            var changed = before.Where(kv => !after.TryGetValue(kv.Key, out var h) || h != kv.Value).Select(kv => kv.Key).ToList();
            var sb = new StringBuilder();
            sb.AppendLine($"# 아라비아 씬 선택 생성 {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("생성: " + string.Join(", ", arabiaScenes));
            sb.AppendLine("빌드 0번: " + (EditorBuildSettings.scenes.Length > 0 ? EditorBuildSettings.scenes[0].path : "(없음)"));
            sb.AppendLine("다른 지역 씬 변경: " + (changed.Any(p => p.EndsWith(".unity")) ? string.Join(", ", changed.Where(p => p.EndsWith(".unity"))) : "없음"));
            sb.AppendLine("공용 파일 변경 (부수 효과):");
            foreach (var p in changed.Where(p => !p.EndsWith(".unity"))) sb.AppendLine("- " + p);
            WriteReport("scene_build.md", sb.ToString());
            Debug.Log("[아라비아] " + sb.ToString().Replace("\n", " | "));
            if (changed.Any(p => p.EndsWith(".unity"))) Debug.LogError("[아라비아] 다른 지역 씬이 바뀌었습니다. 보고서를 확인하세요.");
            EditorSceneManager.OpenScene(arabiaScenes[0]);
        }

        [MenuItem("Pocket Caravan/아라비아/3. 시험 플레이 (알 아흐사에서 시작)", priority = 202)]
        public static void PlayFromAhsa()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var entry = WorldRegistry.Load().Map(AhsaMap);
            EditorSceneManager.OpenScene(entry.scenePath);
            EditorApplication.isPlaying = true;
        }

        // ---------- 공용 도우미 ----------

        public static T Get<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
            return asset;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static Dictionary<string, string> Hashes(IEnumerable<string> paths)
        {
            var result = new Dictionary<string, string>();
            using (var sha = SHA256.Create())
                foreach (var p in paths.Distinct())
                    result[p] = File.Exists(p) ? BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-", "") : "(없음)";
            return result;
        }

        public static string FileHash(string path)
        {
            if (!File.Exists(path)) return "(없음)";
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "");
        }

        public static void WriteReport(string name, string text)
        {
            Directory.CreateDirectory(ReportDir);
            File.WriteAllText(Path.Combine(ReportDir, name), text, new UTF8Encoding(false));
        }
    }
}
