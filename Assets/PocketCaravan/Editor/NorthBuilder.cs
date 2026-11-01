using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 북유럽 데이터(지역·마을·물건)를 만들고, 테스트용으로 내 컴퓨터의 GameDatabase 목록 끝에 덧붙인다.
    /// 맵 씬은 공용 메뉴 "2. 맵 씬 다시 만들기"가 World/Regions/north.json을 읽어 만든다.
    /// 팀 규칙: GameDatabase.asset 변경은 커밋하지 않는다 (통합 담당자가 모아서 반영). Docs/TeamDevelopment.md
    /// </summary>
    public static class NorthBuilder
    {
        const string Root = "Assets/PocketCaravan";
        const string DataDir = Root + "/Data/North";
        const string DbPath = Root + "/Resources/GameDatabase.asset";

        /// <summary>
        /// 테스트 준비: 북유럽 데이터를 만들어 GameDatabase 목록 끝에 덧붙이고(다른 지역은 그대로 둠),
        /// 공용 MapJsonBuilder.BuildSelected로 북유럽 맵만 다시 만든다 (다른 담당 씬은 건드리지 않음).
        /// GameDatabase·WorldIndex·빌드 목록 변경은 커밋하지 않는다 (통합 담당자가 반영).
        /// </summary>
        [MenuItem("Pocket Caravan/북유럽/테스트 준비 (데이터 등록 + 북유럽 맵만 생성)", priority = 30)]
        public static void PrepareTest()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[북유럽] Play를 멈춘 뒤 실행하세요."); return; }
            // 10/27 맵 생성기가 바꾼 NorthLife.json·그림을 유니티가 아직 안 읽었을 수 있어 먼저 새로 읽게 한다
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset($"{Root}/Resources/North/NorthLife.json", ImportAssetOptions.ForceUpdate);
            if (!BuildData()) return;
            var ids = WorldRegistry.Load().maps.FindAll(m => m.mapId.StartsWith("north_")).ConvertAll(m => m.mapId).ToArray();
            try { MapJsonBuilder.BuildSelected(ids); }
            catch (Exception e) { Debug.LogError($"[북유럽] 맵 생성 실패: {e.Message}"); return; }
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene($"{Root}/Scenes/North/north_birka.unity");
            Debug.Log($"[북유럽] 맵 {ids.Length}개를 다시 만들었습니다. north_birka에서 Play 하세요.");
        }

        [MenuItem("Pocket Caravan/북유럽/데이터만 등록", priority = 31)]
        public static void BuildDataMenu() => BuildData();

        public static bool BuildData()   // 10/27 통합 메뉴(RegionLinksBuilder)에서도 부름
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DbPath);
            if (db == null) { Debug.LogError("GameDatabase가 없습니다."); return false; }

            var north = Upsert<RegionData>($"{DataDir}/Region_North.asset", r => { r.id = "north"; r.displayName = "북유럽"; r.note = "D구역. 한랭 지형·피오르·숲. 특산: 모피, 호박"; });

            TownData Town(string file, string id, string name, Vector2Int coord, int level) =>
                Upsert<TownData>($"{DataDir}/{file}.asset", t =>
                {
                    t.id = id; t.displayName = name; t.region = north; t.worldCoord = coord; t.startLevel = level;
                    t.stock.Clear(); t.vehiclesForSale.Clear(); t.defaultOptimal = 4f;
                });
            // 좌표는 north.json의 worldX, worldY와 같게 (경제 거리 계산에 쓰임)
            // 10/23: 비르카 = 북유럽 수도 (고려 개경처럼 3등급, 등급·신용별 시설)
            var birka = Town("Town_Birka", "north_birka", "비르카", new Vector2Int(29, 1), 3);
            birka.settlementTier = 3;
            birka.facilities = new System.Collections.Generic.List<TownFacility>
            {
                new TownFacility { id = "north_capital_rest", displayName = "회관 손님방", kind = FacilityKind.Rest, minTier = 1, minCredit = 0, storageCapacity = 120f },
                new TownFacility { id = "north_capital_info", displayName = "룬돌 길 안내소", kind = FacilityKind.RouteInformation, minTier = 2, minCredit = 1, storageCapacity = 120f,
                    description = "북쪽 산길로 온천 마을(그 위 룬돌 언덕), 서쪽으로 모피 사냥꾼 마을과 그 위 순록 고원, 동쪽으로 피오르 나루(그 위 얼음 동굴 골짜기)로 갈라집니다.\n남쪽 출구로 내려가면 바로 중앙 대륙의 북문 마을입니다.\n모피는 숲에서 싸고 바닷가에서 비쌉니다." },
                new TownFacility { id = "north_capital_warehouse", displayName = "상인 길드 창고", kind = FacilityKind.Warehouse, minTier = 3, minCredit = 3, storageCapacity = 120f },
            };
            var hunter = Town("Town_Hunter", "north_hunter", "모피 사냥꾼 마을", new Vector2Int(28, 0), 1);
            var naru = Town("Town_Naru", "north_naru", "피오르 나루 마을", new Vector2Int(30, 0), 1);
            // 10/23 추가: 온천 마을, 순록 고원 마을 (10/28 배치 바꿈: 온천은 비르카 북쪽, 순록 고원은 비르카 서쪽)
            var laugar = Town("Town_Laugar", "north_laugar", "라우가르 온천 마을", new Vector2Int(29, 2), 1);
            var herd = Town("Town_Herd", "north_herd", "순록 고원 마을", new Vector2Int(28, 1), 1);
            // 10/26 중앙 대륙 북쪽 1/4 (피자 구성): 장터 마을 4곳. 북유럽 지역(2만 칸)이 아니라 중앙 북쪽 구역 소속 (World/Regions/center_north.json)
            // 좌표는 center_north.json의 worldX, worldY와 같게 (화요일 중앙 합칠 때 다시 정함)
            var centerNorth = Upsert<RegionData>($"{Root}/Data/Center/Region_CenterNorth.asset", r => { r.id = "center_north"; r.displayName = "중앙 북쪽"; r.note = "중앙 대륙 10만 칸 중 북쪽 1/4 (기우 담당, 2만5천 칸). 마을 4곳 + 길 5곳"; });
            TownData CenterTown(string file, string id, string name, Vector2Int coord, string rest, string info, string infoText)
            {
                var t = Town(file, id, name, coord, 1);
                t.region = centerNorth;
                t.facilities = new System.Collections.Generic.List<TownFacility>
                {
                    new TownFacility { id = id + "_rest", displayName = rest, kind = FacilityKind.Rest, minTier = 1, minCredit = 0, storageCapacity = 120f },
                    new TownFacility { id = id + "_info", displayName = info, kind = FacilityKind.RouteInformation, minTier = 1, minCredit = 0, storageCapacity = 120f, description = infoText },
                };
                return t;
            }
            // 북문 마을 (id는 예전 교역 초소 north_post 그대로: 저장·소금 산지 유지)
            var post = CenterTown("Town_NorthPost", "north_post", "북문 마을", new Vector2Int(29, -1), "북문 여관", "북문 길 안내",
                "북쪽 길로 올라가면 바로 북유럽 수도 비르카입니다.\n서쪽 눈숲 고갯길은 눈숲 마을(썰매), 동쪽 개울 다리길은 개울 나루 마을(낚시·나룻배).\n남쪽 룬돌 큰길이 북쪽 길목 마을까지 가장 빠르지만 눈늑대가 나옵니다.");
            var cwood = CenterTown("Town_NorthCWood", "north_cwood", "눈숲 마을", new Vector2Int(28, -2), "숲 마을 쉼터", "썰매꾼 길 안내",
                "북쪽 눈숲 고갯길로 북문 마을, 남쪽 서남 솔숲길로 북쪽 길목 마을.\n솔숲길엔 산적이 숨어 있고, 바람 부는 날엔 통나무가 길을 막습니다.\n서쪽은 북서 포구(베네치아 쪽) 길을 닦는 중입니다.");
            var ccreek = CenterTown("Town_NorthCCreek", "north_ccreek", "개울 나루 마을", new Vector2Int(30, -2), "물방앗간 쉼터", "나루 길 안내",
                "나룻배를 타면 피오르 나루 마을까지 바로 갑니다 (12냥).\n남쪽 동남 물가길은 눈 녹은 물에 강이 넘칠 때가 있습니다.\n동쪽 다리 너머는 북동 포구(고려 쪽) 길을 닦는 중입니다.");
            var cross = CenterTown("Town_NorthCross", "north_cross", "북쪽 길목 마을", new Vector2Int(29, -3), "길목 여관", "길목 길 안내",
                "세 길이 모이는 곳: 룬돌 큰길(가장 빠름, 눈늑대), 서남 솔숲길(산적), 동남 물가길(강 범람).\n남쪽은 대륙 중심으로 이어집니다 (합칠 때 열림).");

            ItemData Item(string id, string name, float weight, float price, TownData origin) =>
                Upsert<ItemData>($"{DataDir}/Item_{id}.asset", i =>
                {
                    i.id = id; i.displayName = name; i.weight = weight; i.basePrice = price; i.originTown = origin;
                    i.pricePerMap = 0.5f; i.questItem = false;
                    i.icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Items/{id}.png");
                });
            // 산지: 모피·말린 고기 = 사냥꾼 마을, 꿀술 = 비르카, 호박 = 피오르 나루 (호박 해안이 생기면 옮김)
            var mead = Item("north_mead", "꿀술", 2f, 12f, birka);
            var fur = Item("north_fur", "모피", 2f, 30f, hunter);
            var amber = Item("north_amber", "호박", 1f, 50f, naru);
            var meat = Item("north_dried_meat", "말린 고기", 2f, 8f, hunter);
            var herb = Item("north_herb", "온천 약초", 1f, 18f, laugar);
            var cheese = Item("north_cheese", "순록 치즈", 2f, 14f, herd);
            // 10/24 교역 초소 특산: 남쪽에서 올라온 소금. 고기·생선 절이는 사냥꾼 마을·나루에서 귀함
            var salt = Item("north_salt", "남쪽 소금", 2f, 16f, post);
            // 10/24 얼음 낚시 미니게임으로 잡는 물고기 (NorthMinigames.cs). 나루가 생선 마을이라 산지, 내륙·초소는 비싸게 삼
            var cod = Item("north_cod", "얼음 대구", 1f, 9f, naru);
            var charr = Item("north_char", "북극 곤들매기", 1f, 24f, naru);
            var salmon = Item("north_salmon", "황금 연어", 2f, 70f, naru);
            // 10/26 특산 공방 가공품 (NorthCraft.cs): 재료보다 가볍고 비싸다. 북쪽 산지는 안 팔고, 중앙·비르카에서 비싸게 산다
            var stockfish = Item("north_stockfish", "말린 대구", 1f, 42f, naru);      // 얼음 대구 3 (9×3)
            var furcoat = Item("north_furcoat", "털옷", 2f, 88f, hunter);             // 모피 2 (30×2) + 수공 5
            var herbmead = Item("north_herbmead", "온천 약초 꿀술", 2f, 62f, laugar);  // 꿀술 2 (12×2) + 약초 1 (18)
            // 10/30 눈숲 마을 눈싸움 보상 (NorthSnowball.cs): 아이들이 만든 눈사람 장식. 장터에서 팔 수 있는 가벼운 기념품
            var snowman = Item("north_snowman", "눈사람 장식", 1f, 20f, cwood);
            // 10/30 라우가르 꿀벌 오두막 보상 (NorthBees.cs): 하루 한 단지. 양조장에서 꿀술 2 대신 쓰고, 장터에서도 판다
            var honey = Item("north_honey", "벌꿀 단지", 1f, 15f, laugar);

            // 재고: 산지는 크게(싸게), 소비지는 작게(비싸게 팔림). 나루는 배가 모피를 실어 가서 모피 수요가 큼
            birka.stock.AddRange(new[] { S(mead, 30), S(fur, 8), S(amber, 4), S(meat, 15), S(herb, 4), S(cheese, 5) });
            hunter.stock.AddRange(new[] { S(fur, 30), S(meat, 30), S(mead, 6), S(amber, 2), S(cheese, 6) });
            naru.stock.AddRange(new[] { S(amber, 20), S(mead, 12), S(fur, 4), S(meat, 10), S(herb, 3), S(cheese, 6) });
            // 온천 마을: 약초 산지, 지친 여행자가 고기·꿀술을 찾음. 순록 마을: 치즈 산지, 모피·고기도 넉넉
            laugar.stock.AddRange(new[] { S(herb, 30), S(mead, 8), S(meat, 8), S(fur, 4), S(cheese, 5) });
            herd.stock.AddRange(new[] { S(cheese, 30), S(fur, 14), S(meat, 20), S(herb, 4), S(mead, 5) });
            // 교역 초소 = 북쪽 물건을 남쪽으로 내보내는 수출항: 모피·호박 재고가 바닥이라 가장 비싸게 사 준다.
            // 대신 남쪽 소금이 넘친다 (초소에서만 싸게 삼). 사냥꾼 마을·나루는 소금이 귀함 → 소금↓ 모피↑ 왕복 장사
            post.stock.AddRange(new[] { S(salt, 32), S(fur, 2), S(amber, 1), S(mead, 6), S(meat, 6), S(herb, 4), S(cheese, 4) });
            // 10/26 중앙 북쪽 마을 셋: 눈숲 = 고기·모피가 싸고 소금·꿀술이 귀함 / 개울 나루 = 생선·호박이 싸고 치즈·약초가 귀함
            // 길목 = 대륙 중심으로 넘기는 곳이라 북쪽 물건(모피·호박·치즈·꿀술)을 비싸게 삼, 소금은 넉넉
            cwood.stock.AddRange(new[] { S(meat, 26), S(fur, 12), S(cheese, 4), S(salt, 3), S(mead, 3), S(herb, 4), S(amber, 2) });
            ccreek.stock.AddRange(new[] { S(amber, 8), S(salt, 4), S(mead, 6), S(cheese, 2), S(herb, 2), S(fur, 4), S(meat, 6) });
            cross.stock.AddRange(new[] { S(salt, 14), S(fur, 2), S(amber, 1), S(cheese, 2), S(mead, 3), S(meat, 6), S(herb, 4) });
            hunter.stock.Add(S(salt, 2)); naru.stock.Add(S(salt, 3)); birka.stock.Add(S(salt, 6)); herd.stock.Add(S(salt, 3)); laugar.stock.Add(S(salt, 4));
            naru.stock.AddRange(new[] { S(cod, 14), S(charr, 4), S(salmon, 1) });
            foreach (var t in new[] { birka, hunter, laugar, herd, post, cwood, cross })
                t.stock.AddRange(new[] { S(cod, 3), S(charr, 1), S(salmon, 1) });
            ccreek.stock.AddRange(new[] { S(cod, 16), S(charr, 6), S(salmon, 1) });
            // 가공품: 중앙 마을·비르카 장터 목록에 올린다 (값은 산지에서 멀수록 비쌈). 산지 마을엔 두지 않는다
            foreach (var t in new[] { birka, post, cwood, ccreek, cross })
                t.stock.AddRange(new[] { S(stockfish, 4), S(furcoat, 4), S(herbmead, 4) });
            // 10/30 눈사람 장식: 눈숲 마을 밖 장터에서 기념품으로 사 준다 (재고가 적어 값이 좋음)
            foreach (var t in new[] { birka, post, ccreek, cross }) t.stock.Add(S(snowman, 1));
            foreach (var t in new[] { birka, hunter, naru, cwood, cross }) t.stock.Add(S(honey, 2));
            // 손수레는 공용 수단(고려 데이터)을 그대로 판다 (팀 규칙: 공통 수단을 지역마다 복제하지 않음)
            var cart = AssetDatabase.LoadAssetAtPath<VehicleData>($"{Root}/Data/Goryeo/Vehicle_Handcart.asset");
            if (cart != null) birka.vehiclesForSale.Add(cart);
            // 10/24 짐 싣는 순록 썰매: 순록 고원 마을에서만 판다 (Q8). 길 없는 눈밭(등급 0)에서 가장 빠르고, 다져진 큰길일수록 덜 빠름.
            // 눈 없는 땅·날씨에 따른 속도는 NorthTravel.cs가 맵별로 조정한다.
            var sled = Upsert<VehicleData>($"{DataDir}/Vehicle_NorthSled.asset", v =>
            {
                v.id = "north_sled"; v.displayName = "순록 썰매"; v.capacity = 40f; v.price = 320; v.minTownLevel = 1;
                v.speedByGrade = new[] { 1.45f, 1.3f, 1.2f, 1.12f };
                v.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Resources/North/Minigame/cargo_v.png");
            });
            herd.vehiclesForSale.Add(sled);
            // 10/27 여정 수첩 한 줄 (고려 main의 TownData.journeyNote). 필드가 아직 없는 브랜치에서도 오류 없이 넘어가도록 이름으로 찾는다.
            // 공용 규칙: 거래 역할·귀환 힌트만, 시세나 이익 보장 문구는 쓰지 않는다.
            void Note(TownData t, string note)
            {
                var so = new SerializedObject(t);
                var p = so.FindProperty("journeyNote");
                if (p == null) return;
                p.stringValue = note;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            Note(birka, "북유럽 수도. 꿀술이 흔하고 남쪽 소금은 귀해요. 공방에서 만든 말린 대구·털옷·약초 꿀술도 받아 줘요.");
            Note(hunter, "모피와 말린 고기의 마을. 소금을 찾아요. 털옷 공방에 모피 2장을 맡기면 털옷이 돼요.");
            Note(naru, "얼음 대구와 호박의 바닷가 마을. 대구 덕장에 대구 3마리를 맡기면 말린 대구가 돼요. 나룻배로 개울 나루 마을까지 가요.");
            Note(laugar, "온천 약초의 마을. 양조장에 꿀술 2 + 약초 1을 맡기면 약초 꿀술이 돼요. 온천에 몸을 담그면 눈길이 덜 지쳐요.");
            Note(herd, "순록 치즈의 고원 마을. 순록 썰매를 팔아요. 길 없는 눈밭에서 썰매가 가장 빨라요.");
            Note(post, "북쪽 물건이 남쪽으로 나가는 문. 남쪽 소금이 넘치고 모피·호박을 찾아요. 큰 장이 돌아오는 마을 중 하나예요.");
            Note(cwood, "고기와 모피가 흔한 숲 마을. 소금·꿀술은 귀해요. 서남 솔숲길엔 산적이 숨어 있어요.");
            Note(ccreek, "생선과 호박이 흔한 물가 마을. 치즈·약초를 찾아요. 나룻배로 피오르 나루까지 바로 가요.");
            Note(cross, "세 길이 모이는 길목. 북쪽 물건을 대륙 중심으로 넘기는 곳이라 모피·호박·치즈를 찾아요.");
            foreach (var t in new[] { birka, hunter, naru, laugar, herd, post, cwood, ccreek, cross }) EditorUtility.SetDirty(t);

            // 퀘스트용 물건 (장터에 팔 수 없음)
            ItemData QuestItem(string id, string name, float weight) =>
                Upsert<ItemData>($"{DataDir}/Item_{id}.asset", i =>
                {
                    i.id = id; i.displayName = name; i.weight = weight; i.basePrice = 1f; i.originTown = null;
                    i.pricePerMap = 0f; i.questItem = true;
                    i.icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Items/{id}.png");
                });
            var butter = QuestItem("north_butter_jar", "버터 단지", 1f);
            var hay = QuestItem("north_hay", "건초 한 짐", 6f);          // 무거워서 등짐 한도를 느끼게
            var saltLick = QuestItem("north_salt_lick", "소금 덩이", 3f);
            var meadJar = QuestItem("north_mead_jar", "꿀술 단지", 2f);
            var quests = BuildQuests(butter, hay, saltLick, meat, cart, meadJar, mead, fur, sled, salt, stockfish, herb, cheese, amber);

            // 테스트용 등록 (북유럽 것만 지우고 다시 넣음, 고려 목록은 그대로)
            db.towns.RemoveAll(t => t == null || t.id.StartsWith("north_"));
            db.items.RemoveAll(i => i == null || i.id.StartsWith("north_"));
            db.quests.RemoveAll(q => q == null || q.id.StartsWith("north_"));
            // 북유럽 마을을 앞에 둔다: 손수레 길 안내가 목록에서 처음 찾은 마을(원래는 개경)을 가리키기 때문
            db.towns.InsertRange(0, new[] { birka, hunter, naru, laugar, herd, post, cwood, ccreek, cross });
            db.items.AddRange(new[] { mead, fur, amber, meat, herb, cheese, salt, cod, charr, salmon, stockfish, furcoat, herbmead, butter, hay, saltLick, meadJar, snowman, honey });
            db.vehicles.RemoveAll(v => v == null || v.id.StartsWith("north_"));
            db.vehicles.Add(sled);
            db.quests.AddRange(quests);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log("[북유럽] 데이터를 만들고 GameDatabase에 덧붙였습니다. GameDatabase.asset 변경은 커밋하지 마세요.");
            return true;
        }

        // ---------- 퀘스트 (초반 공통 시나리오 북유럽판: Q1, Q2, Q5, Q7) ----------

        const string Innkeeper = "north_innkeeper", Husband = "north_husband", Wife = "north_wife", Goats = "north_goats", Har = "north_har",
            Warehouse = "north_warehouse", Tomte = "north_tomte", Bold = "north_bold",
            // 10/31 마을 곁가지·주는 사람 옮기기 (맵의 talker를 npc로 바꿈, World/Regions/north.json npcs)
            Tora = "north_tora", Captain = "north_captain", Herbalist = "north_herbalist", Bathkeeper = "north_bathkeeper",
            HerdGranny = "north_herd_granny", Sledmaker = "north_sledmaker", Runekeeper = "north_runekeeper", Miner = "north_miner";

        static QuestObjective O(ObjectiveType type, string text, string target = null, ItemData item = null, int count = 1, string line = null, VehicleData vehicle = null) =>
            new QuestObjective { type = type, text = text, targetId = target, item = item, count = count, completeLine = line, vehicle = vehicle };

        static List<QuestData> BuildQuests(ItemData butter, ItemData hay, ItemData saltLick, ItemData meat, VehicleData cart,
            ItemData meadJar, ItemData mead, ItemData fur, VehicleData sled,
            ItemData salt, ItemData stockfish, ItemData herb, ItemData cheese, ItemData amber)
        {
            const string chain = "빈 등짐에서 손수레까지";
            string dir = $"{DataDir}/Quests";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(DataDir, "Quests");
            var list = new List<QuestData>();
            QuestData Q(string file, Action<QuestData> init)
            {
                var q = Upsert<QuestData>($"{dir}/{file}.asset", x =>
                {
                    x.requires.Clear(); x.objectives.Clear(); x.giveItem = null; x.giveCount = 0;
                    x.offerItem = null; x.offerCount = 0; x.offerPrice = 0;
                    x.rewardMoney = 0; x.rewardCredit = 0; x.finishNote = ""; x.chain = "";
                    init(x);
                });
                list.Add(q);
                return q;
            }

            // Q1-1 (필수): 마을 사이 배달 — 집 보는 남편의 버터 단지
            var q1 = Q("N1_Butter", q =>
            {
                q.id = "north_q1_butter"; q.title = "집 보는 남편의 버터 단지"; q.chain = chain; q.giverNpc = Husband;
                q.summary = "비르카의 남편이 만든 버터 단지를 모피 사냥꾼 마을 들판의 아내에게 전한다. 비르카 서쪽 출구로 나가면 바로 사냥꾼 마을이다.";
                q.offerLine = "오늘은 아내랑 일을 바꿨거든. 아내는 들일, 나는 집안일. 그런데 버터를 저어 놨더니 아내가 점심에 먹고 싶다지 뭐야. 사냥꾼 마을 들판에 있으니 이 단지 좀 갖다 주게.";
                q.giveItem = butter; q.giveCount = 1;
                q.objectives.Add(O(ObjectiveType.VisitMap, "모피 사냥꾼 마을에 가기", "north_hunter"));
                q.objectives.Add(O(ObjectiveType.Deliver, "들판의 아내에게 버터 단지 전하기", Wife, butter, 1,
                    "어머, 버터가 제대로 굳었네? 남편이 웬일이람. 고마워."));
                q.rewardMoney = 20; q.rewardCredit = 1;
            });

            // Q1-2 (필수): 무거운 짐 — 아내가 보내는 건초
            var q1b = Q("N1b_Hay", q =>
            {
                q.id = "north_q1_hay"; q.title = "소가 굶고 있어요"; q.chain = chain; q.giverNpc = Wife; q.requires.Add(q1);
                q.summary = "아내가 보내는 건초 한 짐을 비르카의 남편에게 가져간다. 무거워서 등짐이 거의 찬다.";
                q.offerLine = "그 사람, 분명 소 먹이는 걸 잊었을 거야. 이 건초 한 짐 좀 가져다줘. 무거우니까 달리면 숨이 찰 거야. Shift로 달리다 지치면 잠깐 걸어.";
                q.giveItem = hay; q.giveCount = 1;
                q.objectives.Add(O(ObjectiveType.Deliver, "비르카의 남편에게 건초 전하기", Husband, hay, 1,
                    "살았다! 소가 지붕에 올라가서 풀 뜯을 뻔했어. 내일은 원래대로 내가 들일 할래."));
                q.rewardMoney = 25; q.rewardCredit = 1;
                q.finishNote = "부부가 웃으며 고맙다고 한다. 짐 무게와 등짐 한도를 익혔다.";
            });

            // Q2 (필수): 산지에서 사 와서 납품 — 여관 할머니의 못 수프
            var q2 = Q("N2_NailSoup", q =>
            {
                q.id = "north_q2_soup"; q.title = "못 하나로 끓이는 수프"; q.chain = chain; q.giverNpc = Innkeeper; q.requires.Add(q1b);
                q.summary = "모피 사냥꾼 마을 장터에서 말린 고기 2개를 사서 여관 할머니에게 가져간다.";
                q.offerLine = "못 하나로도 수프는 끓일 수 있지. 그래도 고기가 조금 들어가면 손님들이 좋아한단다. 사냥꾼 마을에서 말린 고기 두 덩이만 사다 주렴. 거기가 산지라 싸게 판단다. 값은 넉넉히 쳐 주마.";
                q.objectives.Add(O(ObjectiveType.Buy, "사냥꾼 마을 장터에서 말린 고기 2개 사기", "north_hunter", meat, 2));
                q.objectives.Add(O(ObjectiveType.Deliver, "여관 할머니에게 말린 고기 전하기", Innkeeper, meat, 2,
                    "옳지, 좋은 고기로구나. 봐라, 산지에서 사면 이렇게 싸고, 여기까지 오면 값이 오르지. 그게 장사란다."));
                q.rewardMoney = 45; q.rewardCredit = 1;
                q.finishNote = "산지에서 싸게 사서 먼 곳에 가져가면 비싸게 받는다. 이제 게시판 의뢰와 교역으로 손수레 값을 모아 보자.";
            });

            // Q5 (자유): 위험한 길 너머 배달 — 다리 밑 트롤
            Q("N5_Troll", q =>
            {
                q.id = "north_q5_troll"; q.title = "다리 밑 트롤"; q.chain = chain; q.giverNpc = Warehouse; q.requires.Add(q2);
                q.summary = "피오르 나루 창고지기가 맡긴 소금 덩이를 사냥꾼 마을의 순록지기 삼형제에게 배달한다. 나루 북쪽 갈림길(라우가르로 가는 지름길)엔 트롤 패거리가 산다. 돌아가려면 서쪽 비르카 길로.";
                q.offerLine = "배로 들어온 소금 덩이야. 사냥꾼 마을 순록지기 삼형제가 순록한테 핥게 한다고 기다리고 있지. 북쪽 갈림길로 라우가르를 거쳐 가면 빠른데, 거기 트롤 패거리가 살아. 통행료를 내든, 냅다 달리든, 서쪽 비르카로 돌아가든 자네가 정하게.";
                q.giveItem = saltLick; q.giveCount = 1;
                q.objectives.Add(O(ObjectiveType.Deliver, "순록지기 맏형에게 소금 덩이 전하기", Goats, saltLick, 1,
                    "오, 소금이다! 순록들이 신나겠군. 트롤한테 \"뒤에 더 큰 짐꾼이 온다\"고 하면 한 번은 속는다니까. 고마워."));
                q.rewardMoney = 50; q.rewardCredit = 1;
            });

            // Q3 (자유): 배달 + 정직 시험 — 톰테의 꿀술 단지 (선택지는 Scripts/North/NorthTomte). 주행 중 낙하·파손은 팀 결정으로 쓰지 않음
            Q("N3_Tomte", q =>
            {
                q.id = "north_q3_tomte"; q.title = "톰테의 꿀술 단지"; q.chain = chain; q.giverNpc = Innkeeper; q.requires.Add(q2);
                q.summary = "꿀술 단지를 피오르 나루 창고지기에게 배달한다. 창고 문 앞을 지키는 톰테 할아버지는 정직한 상인을 좋아한다.";
                q.offerLine = "나루 창고에서 우리 꿀술을 기다린단다. 창고 문 앞에는 톰테 할아버지가 지키고 계시지. 예의 바르고 정직하게 굴렴. 그분 눈에 들면 장사가 술술 풀린단다.";
                q.giveItem = meadJar; q.giveCount = 1;
                q.objectives.Add(O(ObjectiveType.VisitMap, "피오르 나루 마을에 가기", "north_naru"));
                q.objectives.Add(O(ObjectiveType.Talk, "창고 문 앞의 톰테 할아버지와 이야기하기", Tomte));
                q.objectives.Add(O(ObjectiveType.Deliver, "창고지기에게 꿀술 단지 전하기", Warehouse, meadJar, 1,
                    "주문한 꿀술 맞네! 톰테 할아버지를 만났다고? 그분이 들여보냈다면 믿을 만한 상인이지."));
                q.rewardMoney = 40; q.rewardCredit = 1;
            });

            // Q4 (이벤트): 북유럽 이동 강점(기동·회전율) — 무니가 물어온 눈보라.
            // NPC가 주지 않고, Q2를 마친 뒤 자유 구간 퀘스트(Q3·Q5·Q6)를 처음 받을 때 NorthQuestDirector가 자동 수락시킴 (저장된 퀘스트 상태만으로 판단)
            Q("N4_Storm", q =>
            {
                q.id = "north_q4_storm"; q.title = "무니가 물어온 눈보라"; q.chain = chain; q.giverNpc = ""; q.requires.Add(q2);
                q.summary = "눈보라로 사냥꾼 마을에 꿀술이 끊겼다. 재고가 다시 차기 전에 비르카에서 꿀술을 사서 사냥꾼 마을에 3개 팔자. 빨리 도착할수록 값을 더 받는다.";
                q.objectives.Add(O(ObjectiveType.Buy, "비르카 장터에서 꿀술 사기", "north_birka", mead, 3));
                q.objectives.Add(O(ObjectiveType.Sell, "사냥꾼 마을 장터에 꿀술 3개 팔기", "north_hunter", mead, 3));
                q.rewardMoney = 20; q.rewardCredit = 1;
                q.finishNote = "소식을 듣자마자 움직여 남보다 먼저 닿았다. 항해와 원정에서 익힌 북유럽 상인의 이동 요령이 빛났다.";
            });

            // Q6 (자유): 라이벌과 시세 경쟁 — 멈추지 않는 볼드. 모피 5장 사기를 끝낸 순간 NorthQuestDirector가 나루에 투매
            // 판정은 공용 Sell 목표(모피 판매 수량). 이익 금액은 판정하지 않으므로 설명에도 쓰지 않는다.
            Q("N6_Bold", q =>
            {
                q.id = "north_q6_bold"; q.title = "멈추지 않는 볼드"; q.chain = chain; q.giverNpc = Bold; q.requires.Add(q2);
                q.summary = "사냥꾼 마을에서 모피 5장을 사서 5장을 판다. 모피를 다 사면 볼드가 먼저 나루 장에 모피를 쏟아붓는다. 값이 떨어진 장을 피해 다른 마을에 팔거나 값이 회복되길 기다리자.";
                q.offerLine = "흥, 네가 그 새로 온 행상이냐? 모피 장사로 나를 이길 수 있을 것 같아? 사냥꾼 마을 모피 다섯 장을 사서 팔아 보시지. 난 더, 더 많이 팔 거니까!";
                q.objectives.Add(O(ObjectiveType.Buy, "사냥꾼 마을에서 모피 5장 사기", "north_hunter", fur, 5));
                q.objectives.Add(O(ObjectiveType.Sell, "모피 5장 팔기 (어느 마을이든)", null, fur, 5));
                q.objectives.Add(O(ObjectiveType.Talk, "볼드에게 이익을 보여주기", Bold, null, 1,
                    "뭐, 뭐야? 내가 판 장을 피해서 팔았다고? …다음엔 안 진다!"));
                q.rewardMoney = 30; q.rewardCredit = 1;
                q.finishNote = "볼드는 이제 라이벌로 남는다. 한 장에 몰아 팔면 값이 떨어진다는 걸 배웠다.";
            });

            // Q7 (마무리): 이동 수단 구입 — 하르의 손수레
            var q7 = Q("N7_Handcart", q =>
            {
                q.id = "north_q7_cart"; q.title = "하르의 손수레"; q.chain = chain; q.giverNpc = Har; q.requires.Add(q2);
                q.summary = "돈을 모아 비르카 장터에서 손수레를 사고 목수 하르에게 보여준다. 한 번에 세 배를 실을 수 있다.";
                q.offerLine = "등짐으로는 하루에 몇 번 못 오가지. 내가 만든 손수레는 비르카 장터에서 판다네. 의뢰와 장사로 돈을 모아서 사 가게. 같은 일을 해도 버는 돈이 확 달라질 걸세.";
                if (cart != null)
                {
                    q.objectives.Add(O(ObjectiveType.HaveMoney, $"손수레 값 {cart.price}냥 모으기", null, null, cart.price));
                    q.objectives.Add(O(ObjectiveType.BuyVehicle, "비르카 장터에서 손수레 사기", null, null, 1, null, cart));
                }
                q.objectives.Add(O(ObjectiveType.Talk, "목수 하르에게 보여주기", Har, null, 1,
                    "잘 굴러가지? 이제 고개 너머 다른 나라 상인들도 만나 보게. 눈이 녹는 남쪽에 큰 장이 선다더군."));
                q.rewardMoney = 0; q.rewardCredit = 2;
                q.finishNote = "손수레를 샀다! 1장 끝. (관문과 다른 지역 연결은 지역을 합칠 때 열린다)";
            });

            // Q8 (10/24): 눈보라를 달리는 썰매 — 하르와 순록 고원 썰매 장인이 함께 만든 짐 썰매. 대표 장면 "눈보라 속 순록 썰매"
            Q("N8_Sled", q =>
            {
                q.id = "north_q8_sled"; q.title = "눈보라를 달리는 썰매"; q.chain = "북쪽의 길"; q.giverNpc = Sledmaker; q.requires.Add(q7);
                q.summary = "순록 고원의 썰매 장인이 하르의 손수레에 지지 않을 짐 썰매를 만든다. 깔개로 쓸 모피를 가져다주고, 고원 장터에서 썰매를 사서 비르카 남쪽 북문 마을까지 달려 본다.";
                q.offerLine = "하르 녀석 손수레를 샀다고? 눈길에선 바퀴가 빠진다네. 내가 제대로 된 짐 썰매를 만들어 주지. 깔개로 쓸 모피 세 장만 가져다주겠나? 다 되면 고원 장터에 내놓겠네.";
                q.objectives.Add(O(ObjectiveType.Deliver, "썰매 장인에게 모피 3장 가져다주기", Sledmaker, fur, 3,
                    "좋은 모피로군! 바로 썰매에 깔겠네. 값이 좀 나가니 돈을 모아 두게."));
                if (sled != null)
                {
                    q.objectives.Add(O(ObjectiveType.HaveMoney, $"썰매 값 {sled.price}냥 모으기", null, null, sled.price));
                    q.objectives.Add(O(ObjectiveType.BuyVehicle, "순록 고원 장터에서 순록 썰매 사기", null, null, 1, null, sled));
                }
                q.objectives.Add(O(ObjectiveType.VisitMap, "썰매를 타고 비르카 남쪽 북문 마을까지 달리기", "north_c_gatetown"));
                q.objectives.Add(O(ObjectiveType.Talk, "썰매 장인에게 썰매 자랑하기", Sledmaker, null, 1,
                    "눈보라 속에서도 순록이 길을 아는 법이지. 하르 녀석한테도 꼭 보여 주게!"));
                q.rewardMoney = 60; q.rewardCredit = 2;
                q.finishNote = "순록 썰매를 얻었다! 눈밭에서 빠르고 짐을 40까지 싣는다. 눈보라에도 걷기보다 덜 느려진다.";
            });

            // ---------- 10/31 마을 곁가지 (큰 줄기 Q2를 마치면 열림, 그 마을 사람에게 말 걸면 시작) ----------
            const string side = "북쪽 마을 이야기";
            Q("N9_TanSalt", q =>
            {
                q.id = "north_q9_tansalt"; q.title = "가죽 절일 소금"; q.chain = side; q.giverNpc = Tora; q.requires.Add(q2);
                q.summary = "모피 사냥꾼 마을의 무두장이 토라에게 소금 3자루를 가져다준다. 남쪽 소금은 북문 마을 장터에서 가장 싸다.";
                q.offerLine = "가죽 절일 소금이 다 떨어졌어. 여기선 소금이 귀해서 부르는 게 값이야. 북문 마을엔 남쪽 소금이 넘친다던데, 세 자루만 사다 줄래? 수고비는 두둑이 쳐 줄게.";
                q.objectives.Add(O(ObjectiveType.Buy, "북문 마을 장터에서 남쪽 소금 3개 사기", "north_post", salt, 3));
                q.objectives.Add(O(ObjectiveType.Deliver, "무두장이 토라에게 소금 전하기", Tora, salt, 3,
                    "이거면 한 달은 버티겠다! 봐, 거기선 싸고 여기선 귀하지. 모피를 남쪽에 가져가면 반대로 되는 거고."));
                q.rewardMoney = 70; q.rewardCredit = 1;
                q.finishNote = "싼 곳에서 사서 귀한 곳에 가져가면 돈이 된다. 소금은 내려갈 때, 모피는 올라올 때가 아니라 내려갈 때 싣자.";
            });
            Q("N10_Stockfish", q =>
            {
                q.id = "north_q10_stockfish"; q.title = "온천으로 가는 말린 대구"; q.chain = side; q.giverNpc = Captain; q.requires.Add(q2);
                q.summary = "피오르 나루 선장의 부탁으로 말린 대구 2개를 라우가르 목욕 회관 주인에게 배달한다. 대구 덕장에 얼음 대구 3마리를 맡기면 말린 대구가 된다.";
                q.offerLine = "라우가르 목욕 회관에서 손님상에 낼 말린 대구를 주문했는데, 배가 묶여서 못 보내고 있네. 덕장에서 만들든 장터에서 사든 두 개만 갖다 주겠나?";
                q.objectives.Add(O(ObjectiveType.Deliver, "라우가르 목욕 회관 주인에게 말린 대구 2개 전하기", Bathkeeper, stockfish, 2,
                    "오, 나루 선장이 보낸 거구먼! 탕에서 나와 먹는 말린 대구가 최고지. 몸 좀 녹이고 가게."));
                q.rewardMoney = 110; q.rewardCredit = 1;
            });
            Q("N11_Herb", q =>
            {
                q.id = "north_q11_herb"; q.title = "고원 할머니께 약초"; q.chain = side; q.giverNpc = Herbalist; q.requires.Add(q2);
                q.summary = "라우가르 약초꾼이 맡긴 온천 약초 2묶음을 순록 고원의 목동 할머니에게 배달한다.";
                q.offerLine = "고원 목동 할머니가 겨울마다 무릎이 쑤신다고 이 약초를 찾으셔. 온천물에 담가 둬서 오래 가니까 서두르지 않아도 돼. 두 묶음만 전해 줄래?";
                q.giveItem = herb; q.giveCount = 2;
                q.objectives.Add(O(ObjectiveType.Deliver, "순록 고원 목동 할머니에게 약초 전하기", HerdGranny, herb, 2,
                    "아이고, 라우가르 약초구나. 이거 달여 먹으면 겨울이 거뜬하지. 고맙다, 얘야."));
                q.rewardMoney = 35; q.rewardCredit = 1;
            });
            Q("N12_Cheese", q =>
            {
                q.id = "north_q12_cheese"; q.title = "고원 치즈 내려보내기"; q.chain = side; q.giverNpc = HerdGranny; q.requires.Add(q2);
                q.summary = "순록 고원 장터에서 순록 치즈 4개를 사서 북문 마을 장터에 판다. 산지에서 멀어질수록 값을 더 받는다.";
                q.offerLine = "올해는 치즈가 너무 많이 나와서 여기선 헐값이란다. 북문 마을 사람들은 고원 치즈를 귀하게 여긴다더라. 네 개만 가져가서 팔아 보렴. 남는 돈은 네 몫이야.";
                q.objectives.Add(O(ObjectiveType.Buy, "순록 고원 장터에서 치즈 4개 사기", "north_herd", cheese, 4));
                q.objectives.Add(O(ObjectiveType.Sell, "북문 마을 장터에 치즈 4개 팔기", "north_post", cheese, 4));
                q.rewardMoney = 30; q.rewardCredit = 1;
                q.finishNote = "산지에서 싸게 사서 먼 장터에 팔았다. 북쪽 물건은 남쪽으로 내려갈수록 값이 오른다.";
            });
            Q("N13_RiteMead", q =>
            {
                q.id = "north_q13_ritemead"; q.title = "룬돌 고리에 올릴 꿀술"; q.chain = side; q.giverNpc = Runekeeper; q.requires.Add(q2);
                q.summary = "룬 지기 노인에게 룬돌 고리 의식에 올릴 꿀술 2병을 가져다준다. 꿀술은 비르카 장터에서 가장 싸다.";
                q.offerLine = "고리 한가운데 불을 지필 때 조상님께 꿀술을 올리는 게 법도라네. 이 늙은이는 언덕을 내려가기가 힘들어서 말이야. 꿀술 두 병만 구해다 주겠나?";
                q.objectives.Add(O(ObjectiveType.Deliver, "룬 지기 노인에게 꿀술 2병 전하기", Runekeeper, mead, 2,
                    "고맙네. 오늘 밤 고리에 불을 지피면 룬돌이 더 밝게 빛날 걸세. 숨은 룬돌을 찾거든 언제든 다시 오게."));
                q.rewardMoney = 40; q.rewardCredit = 1;
            });
            Q("N14_Amber", q =>
            {
                q.id = "north_q14_amber"; q.title = "얼음 속 호박, 남쪽으로"; q.chain = side; q.giverNpc = Miner; q.requires.Add(q2);
                q.summary = "피오르 나루 장터에서 호박 2개를 사서 북쪽 길목 마을 장터에 판다. 길목은 북쪽 물건을 대륙 중심으로 넘기는 곳이라 호박을 비싸게 산다.";
                q.offerLine = "동굴에서 캔 호박은 다 나루 장터로 넘기지. 그런데 남쪽 끝 길목 마을에선 몇 배를 쳐 준다더군. 먼 길이지만 한번 해 보겠나? 나루에서 두 개만 사서 가져가 보게.";
                q.objectives.Add(O(ObjectiveType.Buy, "피오르 나루 장터에서 호박 2개 사기", "north_naru", amber, 2));
                q.objectives.Add(O(ObjectiveType.Sell, "북쪽 길목 마을 장터에 호박 2개 팔기", "north_cross", amber, 2));
                q.rewardMoney = 50; q.rewardCredit = 1;
                q.finishNote = "가볍고 비싼 물건은 멀리 가져갈수록 이익이 크다.";
            });

            foreach (var q in list) EditorUtility.SetDirty(q);
            return list;
        }

        static TownData.StockEntry S(ItemData item, float optimal) => new TownData.StockEntry { item = item, optimal = optimal };

        static T Upsert<T>(string path, Action<T> apply) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            bool create = a == null;
            if (create) a = ScriptableObject.CreateInstance<T>();
            apply(a);
            if (create) AssetDatabase.CreateAsset(a, path);
            EditorUtility.SetDirty(a);
            return a;
        }
    }
}
