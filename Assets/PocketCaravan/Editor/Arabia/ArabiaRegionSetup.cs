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
        // 2026-10-27 오픈월드: 아라비아 지역 = 지형 하나를 7조각으로 자른 연속 지도 (마을 5 + 나즈드 대상로·룹알할리 빈 사막)
        //   + 중앙 대륙 남쪽 구역 6조각. 조각 사이는 걸을 수 있는 경계 전부가 출구(이음새)다.
        //   옛 길 맵(공방길·항구길·안전한 큰길)은 연속 지형에 흡수되어 목록에서 빠졌다 (상수는 옛 기록용으로 남김).
        public static readonly string[] MapIds = { AhsaMap, FarmroadMap, ShortcutMap, TaizMap, ZabidMap, JeddahMap, SanaaMap,
            CentralDunes, CentralCanyon, CentralWest, CentralBazaar, CentralEast, CentralSteppe };
        public const string AhsaTown = "arabia_ahsa", ZabidTown = "arabia_zabid";
        public const string TaizTown = "arabia_taiz", JeddahTown = "arabia_jeddah", SanaaTown = "arabia_sanaa";
        public static readonly string[] TownIds = { AhsaTown, ZabidTown, TaizTown, JeddahTown, SanaaTown };
        public const string ClothId = "arabia_cloth", SpiceId = "arabia_spice";
        public const string DatesId = "arabia_dates", GrainId = "arabia_grain", DeliveryDatesId = "arabia_delivery_dates";
        public const string Q1Id = "arabia_q01_first_delivery", Q2Id = "arabia_q02_supper";
        // 2026-11-05 (기우·통합): 장터 품목 최소 5개·탈것·퀘스트 흐름 맞추기
        public const string FrankId = "arabia_frankincense", CoffeeId = "arabia_coffee", PearlId = "arabia_pearl", SaltId = "arabia_salt", LeatherId = "arabia_leather", CarpetId = "arabia_carpet";
        public const string CamelId = "arabia_camel";
        public const string Q3Id = "arabia_q03_cloth_road", Q4Id = "arabia_q04_spice_capital", Q5Id = "arabia_q05_camel", Q6Id = "arabia_q06_frankincense", Q7Id = "arabia_q07_pearl";
        public const string Q8Id = "arabia_q08_carpet_order", Q9Id = "arabia_q09_dye_salt", Q10Id = "arabia_q10_shortcut", Q11Id = "arabia_q11_coffee", Q12Id = "arabia_q12_garden_dates";
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
            // 2026-11-04 통합(기우): 다른 지역과 같은 시설 규칙 — 2등급부터 길 안내, 수도(3등급)에 창고. 글은 아라비아 담당이 다듬어도 됨
            foreach (var (t, info) in new[] {
                (taiz, "남쪽 길로 자비드, 서쪽 해안길로 제다, 북쪽 고원길로 사나.\n염색 직물은 바다 건너 항구에서 값이 오릅니다."),
                (jeddah, "사나로 가는 두 길: 지름길(빠르지만 모래바람), 농로(느리지만 대상 숙소).\n배로 들어온 향신료는 내륙 수도에서 비쌉니다."),
                (sanaa, "남쪽으로 제다·타이즈, 북쪽 성문 밖은 중앙 대륙 사막(만국 장터 가는 길)입니다.\n수도는 먼 산지 물건을 가장 비싸게 삽니다.") })
                t.facilities.Add(new TownFacility { id = t.id + "_info", displayName = t == sanaa ? "궁전 길 안내소" : "대상 길 안내", kind = FacilityKind.RouteInformation, minTier = 2, minCredit = 1, description = info });
            sanaa.facilities.Add(new TownFacility { id = sanaa.id + "_warehouse", displayName = "수도 상인 창고", kind = FacilityKind.Warehouse, minTier = 3, minCredit = 3, storageCapacity = 120f });

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
            // 2026-11-05 새 품목 6개 (아이콘은 임시로 다른 지역 것 재사용 — 아라비아 담당이 바꿀 것)
            var frank = Item("Item_ArabiaFrankincense", FrankId, "유향 덩이", 1, 16, sanaa, false, Icon("transit_resin"));
            var coffee = Item("Item_ArabiaCoffee", CoffeeId, "커피 열매 자루", 1, 10, taiz, false, Icon("north_herb"));
            var pearl = Item("Item_ArabiaPearl", PearlId, "진주", 0.5f, 20, jeddah, false, Icon("north_amber"));
            var salt = Item("Item_ArabiaSalt", SaltId, "바닷소금 덩이", 1, 4, jeddah, false, Icon("salt_parcel"));
            var leather = Item("Item_ArabiaLeather", LeatherId, "낙타 가죽", 1, 7, ahsa, false, Icon("north_fur"));
            var carpet = Item("Item_ArabiaCarpet", CarpetId, "양탄자", 2, 18, sanaa, false, Icon("venice_lace"));

            // 장터 품목 최소 5개 (11/4 팀 결정). 산지 40·집산 20~30·소비 8~18
            Stock(ahsa, (dates, 40), (leather, 30), (grain, 12), (salt, 10), (coffee, 8));
            Stock(zabid, (grain, 40), (salt, 24), (dates, 12), (cloth, 10), (leather, 8));
            Stock(taiz, (cloth, 40), (coffee, 30), (grain, 14), (dates, 12), (salt, 10), (frank, 8));
            Stock(jeddah, (spice, 40), (pearl, 20), (salt, 30), (cloth, 16), (dates, 14), (grain, 14), (coffee, 10));
            Stock(sanaa, (frank, 30), (carpet, 20), (cloth, 14), (spice, 14), (grain, 18), (dates, 18), (coffee, 12), (pearl, 6));

            // 탈것: 낙타 (모래·험한 길에서 빠름). 제다·사나에서 판다
            var camel = Get<VehicleData>(DataDir + "/Vehicle_ArabiaCamel.asset");
            camel.id = CamelId; camel.displayName = "낙타"; camel.capacity = 45f; camel.price = 300; camel.minTownLevel = 2;
            camel.speedByGrade = new[] { 1.25f, 1.15f, 1.05f, 1f };
            if (!db.vehicles.Contains(camel)) db.vehicles.Add(camel);
            EditorUtility.SetDirty(camel);
            jeddah.vehiclesForSale = new List<VehicleData> { camel }; sanaa.vehiclesForSale = new List<VehicleData> { camel };

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

            // ---- 2026-11-05 Q3~Q7: 알 아흐사 세 사람(마리암·하산·힌드바드)이 이어서 준다. 다른 마을엔 아직 의뢰 NPC가 없어 사기·팔기·가기 목표로 짰다.
            QuestData Quest(string file, string id, string title, string giver, QuestData req, string summary, string offer, int money, int credit, string note, params QuestObjective[] objs)
            {
                var q = Get<QuestData>(QuestDir + "/" + file + ".asset");
                q.id = id; q.title = title; q.chain = "아라비아 1장"; q.summary = summary; q.giverNpc = giver;
                q.requires = req != null ? new List<QuestData> { req } : new List<QuestData>();
                q.offerLine = offer; q.giveItem = null; q.giveCount = 0; q.offerItem = null; q.offerCount = 0; q.offerPrice = 0;
                q.objectives = new List<QuestObjective>(objs);
                q.rewardMoney = money; q.rewardCredit = credit; q.finishNote = note;
                if (!db.quests.Contains(q)) db.quests.Add(q);
                EditorUtility.SetDirty(q);
                return q;
            }
            var q3 = Quest("Quest_ArabiaQ03ClothRoad", Q3Id, "타이즈의 염색 직물", "arabia_hasan", q2,
                "타이즈 염색 공방 마을에서 직물 꾸러미를 사서 항구 제다에 판다. 처음으로 두 마을을 잇는 길.",
                "자비드까지는 가 봤지? 그럼 다음은 타이즈야. 거기 염색 직물을 제다 항구에 가져가면 값이 올라.\n직물 2개면 돼. 길은 남쪽으로 쭉.",
                40, 1, "제다 항구에선 배로 들어온 향신료를 판다. 내륙으로 가져가면 더 비싸다.",
                new QuestObjective { type = ObjectiveType.VisitMap, text = "타이즈에 가기", targetId = TaizMap },
                new QuestObjective { type = ObjectiveType.Buy, text = "타이즈 장터에서 직물 꾸러미 사기", targetId = TaizTown, item = cloth, count = 2 },
                new QuestObjective { type = ObjectiveType.Sell, text = "제다 장터에 직물 꾸러미 팔기", targetId = JeddahTown, item = cloth, count = 2,
                    completeLine = "바다 건너 상인들이 타이즈 직물을 좋아하지. 잘 가져왔어." });
            var q4 = Quest("Quest_ArabiaQ04SpiceCapital", Q4Id, "향신료는 수도로", "arabia_maryam", q3,
                "제다 항구에서 향신료 자루를 사서 수도 사나에 판다. 아라비아에서 가장 긴 교역 길.",
                "제다에서 사나로 가는 길이 둘이야. 지름길은 빠르지만 모래바람이 불고, 농로는 느리지만 대상 숙소가 있어.\n향신료 3자루를 사나까지 가져가 보렴.",
                50, 1, "사나는 수도라 먼 산지 물건을 가장 비싸게 산다. 유향과 양탄자는 거꾸로 사나가 산지다.",
                new QuestObjective { type = ObjectiveType.Buy, text = "제다 장터에서 향신료 자루 사기", targetId = JeddahTown, item = spice, count = 3 },
                new QuestObjective { type = ObjectiveType.Sell, text = "사나 장터에 향신료 자루 팔기", targetId = SanaaTown, item = spice, count = 3,
                    completeLine = "수도 사람들은 향신료 없인 밥을 안 먹지. 좋은 값에 팔렸을 거야." });
            var q5 = Quest("Quest_ArabiaQ05Camel", Q5Id, "낙타 한 마리", "arabia_hasan", q3,
                "돈을 모아 제다나 사나에서 낙타를 산다. 모래와 험한 길에서 빠르고 짐도 더 싣는다.",
                "등짐으로 사막을 건너는 건 무리야. 300냥을 모으면 제다나 사나에서 낙타를 살 수 있어.\n낙타는 모래 위에서 사람보다 빨라.",
                60, 1, "낙타를 사면 지름길의 모래바람도 덜 무섭다.",
                new QuestObjective { type = ObjectiveType.HaveMoney, text = "300냥 모으기", count = 300 },
                new QuestObjective { type = ObjectiveType.BuyVehicle, text = "낙타 사기", vehicle = camel,
                    completeLine = "좋은 낙타군. 이제 자네도 대상이야." });
            var q6 = Quest("Quest_ArabiaQ06Frankincense", Q6Id, "사나의 유향", "arabia_maryam", q4,
                "수도 사나에서 유향 덩이를 사서 알 아흐사 여관의 마리암에게 가져온다. 거꾸로 가는 교역.",
                "여관에 피울 유향이 떨어졌어. 유향은 사나에서만 나와. 두 덩이만 가져다주면 넉넉히 쳐줄게.\n돌아오는 길에 사나 양탄자도 눈여겨보렴, 제다에서 비싸.",
                60, 2, "아라비아 1장이 끝났다. 중앙 대륙 사막 너머 만국 장터로 가 보자.",
                new QuestObjective { type = ObjectiveType.Buy, text = "사나 장터에서 유향 덩이 사기", targetId = SanaaTown, item = frank, count = 2 },
                new QuestObjective { type = ObjectiveType.Deliver, text = "알 아흐사 여관의 마리암에게 유향 덩이 전하기", targetId = "arabia_maryam", item = frank, count = 2,
                    completeLine = "이 향이야. 이제 여관에 손님이 끊이지 않겠구나. 넌 이제 이 땅의 상인이다." });
            var q7 = Quest("Quest_ArabiaQ07Pearl", Q7Id, "진주 한 알", "arabia_hindbad", q2,
                "제다 항구에서 진주를 한 알 사서 어디든 더 비싼 곳에 판다. 이익을 남기는 연습.",
                "제다 잠수부들이 진주를 캐 온대. 한 알 사서 다른 데 팔면 이익이 얼마나 남는지 볼래?\n산 값보다 40냥은 더 남겨 봐.",
                45, 1, "이익은 판 값에서 산 값을 뺀 것. 장부(J)에서 볼 수 있다.",
                new QuestObjective { type = ObjectiveType.Buy, text = "제다 장터에서 진주 사기", targetId = JeddahTown, item = pearl, count = 1 },
                new QuestObjective { type = ObjectiveType.Profit, text = "판매 이익 40냥 남기기", count = 40,
                    completeLine = "봐, 짐은 작아도 이익은 크지. 그게 장사야." });

            // ---- Q8~Q12: 다른 마을 사람들이 준다 (맵 JSON의 말 거는 사람 7명을 의뢰 NPC로 바꿈, World/Regions/arabia.json 등록)
            var q8 = Quest("Quest_ArabiaQ08CarpetOrder", Q8Id, "라시드의 양탄자 주문", "arabia_rashid", q3,
                "타이즈 직물 상인 라시드가 사나 양탄자를 주문한다. 수도에서 사서 거꾸로 가져오는 길.",
                "바다 건너 손님이 사나 양탄자를 찾아. 두 장만 가져다주면 값은 넉넉히 치르지.\n사나는 북쪽 고원길 끝이야.",
                55, 1, "양탄자는 무겁다. 낙타가 있으면 편하다.",
                new QuestObjective { type = ObjectiveType.Buy, text = "사나 장터에서 양탄자 사기", targetId = SanaaTown, item = carpet, count = 2 },
                new QuestObjective { type = ObjectiveType.Deliver, text = "타이즈 라시드에게 양탄자 전하기", targetId = "arabia_rashid", item = carpet, count = 2,
                    completeLine = "이 무늬야. 손님이 기뻐하겠군." });
            var q9 = Quest("Quest_ArabiaQ09DyeSalt", Q9Id, "염색 재료 소금", "arabia_dyer", q3,
                "타이즈 염색 장인에게 제다 바닷소금을 가져다준다. 소금이 있어야 색이 천에 붙는다.",
                "소금이 떨어졌어요. 제다 바닷가 소금 세 덩이만 구해 주세요. 소금 없인 색이 흐르거든요.",
                35, 1, "싼 물건도 필요한 곳에 가져가면 값이 된다.",
                new QuestObjective { type = ObjectiveType.Buy, text = "제다 장터에서 바닷소금 덩이 사기", targetId = JeddahTown, item = salt, count = 3 },
                new QuestObjective { type = ObjectiveType.Deliver, text = "타이즈 염색 장인에게 바닷소금 전하기", targetId = "arabia_dyer", item = salt, count = 3,
                    completeLine = "이제 쪽빛이 제대로 붙겠어요. 고마워요." });
            var q10 = Quest("Quest_ArabiaQ10Shortcut", Q10Id, "지름길을 건너다", "arabia_yusuf", q5,
                "낙타를 타고 제다-사나 지름길을 건너 본다. 모래바람이 부는 길.",
                "낙타를 샀군. 그럼 지름길을 한번 가 봐. 모래바람이 불 땐 멈추고, 그치면 달리는 거야.\n건너서 알 아흐사 가죽 두 장을 사나에 팔고 오면 안장값은 내가 깎아 주지.",
                45, 1, "지름길은 빠르지만 모래바람에 짐을 잃을 수 있다.",
                new QuestObjective { type = ObjectiveType.VisitMap, text = "지름길 건너기", targetId = ShortcutMap },
                new QuestObjective { type = ObjectiveType.Sell, text = "사나 장터에 낙타 가죽 팔기", targetId = SanaaTown, item = leather, count = 2,
                    completeLine = "지름길을 건넌 상인은 대상이 인정해 주지." });
            var q11 = Quest("Quest_ArabiaQ11Coffee", Q11Id, "나디아의 커피", "arabia_nadia", q4,
                "사나 시장 상인 나디아가 타이즈 커피를 찾는다. 고원 커피를 수도로.",
                "수도엔 없는 게 없는데 타이즈 커피만 없어. 세 자루만 가져다줘. 수도 사람들이 줄을 설 거야.",
                50, 1, "커피는 타이즈 고원에서만 난다. 수도에서 가장 비싸다.",
                new QuestObjective { type = ObjectiveType.Buy, text = "타이즈 장터에서 커피 열매 자루 사기", targetId = TaizTown, item = coffee, count = 3 },
                new QuestObjective { type = ObjectiveType.Deliver, text = "사나 나디아에게 커피 전하기", targetId = "arabia_nadia", item = coffee, count = 3,
                    completeLine = "이 향! 내일부터 내 가게 앞에 줄이 설 거야." });
            var q12 = Quest("Quest_ArabiaQ12GardenDates", Q12Id, "궁전 정원의 대추야자", "arabia_gardener", q6,
                "사나 궁전 정원사에게 알 아흐사 대추야자를 가져다준다. 아라비아 1장의 마무리.",
                "궁전 잔치에 대추야자가 모자라. 알 아흐사 것으로 네 묶음만. 제일 먼 산지에서 제일 단 열매가 나오지.",
                70, 2, "아라비아 땅끝에서 수도까지, 상인의 길이 이어졌다.",
                new QuestObjective { type = ObjectiveType.Buy, text = "알 아흐사 장터에서 대추야자 사기", targetId = AhsaTown, item = dates, count = 4 },
                new QuestObjective { type = ObjectiveType.Deliver, text = "사나 정원사에게 대추야자 전하기", targetId = "arabia_gardener", item = dates, count = 4,
                    completeLine = "잔치가 살았네. 궁전 사람들이 네 이름을 기억할 거야." });

            var session = Get<RegionTestSession>(SessionPath);
            session.regionId = RegionId; session.startScene = AhsaMap; session.saveFileName = SaveFileName;
            session.startMoney = startMoney;
            session.startOrigin = AssetDatabase.LoadAssetAtPath<OriginData>(Root + "/Data/Origins/Origin_Arabia.asset");
            session.startVehicle = startVehicle;
            session.note = "아라비아 씬에서 Play하면 본편 저장과 분리된 전용 파일로 진행한다. 시작 자금은 공용 GameDatabase.startMoney 값을 복사한 실험값.";
            EditorUtility.SetDirty(session);

            // 기존 목록·시작 설정이 그대로인지 확인 (추가만 허용)
            bool kept = keepItems.All(db.items.Contains) && keepTowns.All(db.towns.Contains) && keepQuests.All(db.quests.Contains)
                && db.vehicles.Take(vehicles0).SequenceEqual(keepVehicles) && db.startMoney == startMoney && db.startVehicle == startVehicle && db.startOrigin == startOrigin
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
