using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 베네치아(제안 초안): 수도 1 + 마을 4를 마을별 씬 다섯 개로 나눈다(합계 약 2만 칸). 씬을 넘을 때 공용 이동의 페이드가 일어난다.
    /// 맵은 Tools/MapGen/venice.py가 석호 원본 하나를 잘라 만든다(이전 한 맵 venice_lagoon은 등록하지 않는다).
    /// 지명·상품·수치는 팀 확정 전 임시값이다. 무라노·산테라스모·부라노는 제안 지명이다.
    /// 다른 지역 에셋·DB 항목은 지우거나 바꾸지 않는다. 공용 GameDatabase에는 추가만 한다(커밋 여부는 통합 담당과 정한다).
    /// </summary>
    public static class VeniceRegionSetup
    {
        const string Root = "Assets/PocketCaravan";
        const string Data = Root + "/Data/Venice";
        static readonly string[] MapIds = { "venice_rialto", "venice_santerasmo", "venice_murano", "venice_burano", "venice_chioggia" };
        // 본토 서부(중앙 대륙 서쪽 1/4, 제안): Tools/MapGen/venice_mainland.py, 등록은 World/Regions/center_venice.json
        static readonly string[] MainlandIds = { "venice_c_landing", "venice_c_vineyard", "venice_c_marsh", "venice_c_pass",
            "venice_c_bridge", "venice_c_post", "venice_c_gate" };

        [MenuItem("Pocket Caravan/베네치아/데이터·맵 만들기")]
        public static void Build()
        {
            ApplyData();
            var all = new List<string>(MapIds); all.AddRange(MainlandIds);
            MapJsonBuilder.BuildSelected(all.ToArray());
            foreach (var id in MapIds) AttachDistricts(id);
            foreach (var id in MainlandIds) UseVeniceCatIn(id);
            BuildTileAtlas();
            Debug.Log("[베네치아] 데이터와 맵 12개(베네치아 5 + 본토 7)를 만들었습니다. Scenes/Venice/venice_rialto에서 Play하세요.");
        }

        /// <summary>
        /// 석호 씬 5개만 다시 만든다 (2026-10-29). 통합 브랜치의 본토 씬에는 다른 문명 연결이 들어 있어 건드리지 않는다.
        /// 공용 데이터(GameDatabase)도 다시 쓰지 않는다.
        /// </summary>
        [MenuItem("Pocket Caravan/베네치아/석호 씬만 다시 만들기")]
        public static void BuildLagoonOnly()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play 종료 후 실행하세요.");
            MapJsonBuilder.BuildSelected(MapIds);
            foreach (var id in MapIds) AttachDistricts(id);
            BuildTileAtlas();
            Debug.Log("[베네치아] 석호 씬 5개를 다시 만들었습니다.");
        }

        /// <summary>
        /// 베네치아 씬 12개(석호 5 + 본토 7)를 맵 JSON에서 다시 만든다 (2026-10-29). 공용 데이터(GameDatabase)는 다시 쓰지 않는다.
        /// 본토 JSON에 들어 있는 통합 브랜치의 다른 문명 연결(link_ 출구)은 JSON 그대로 씬에 들어간다.
        /// </summary>
        [MenuItem("Pocket Caravan/베네치아/베네치아 씬 12개 다시 만들기 (데이터 제외)")]
        public static void BuildScenesOnly()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play 종료 후 실행하세요.");
            var all = new List<string>(MapIds); all.AddRange(MainlandIds);
            MapJsonBuilder.BuildSelected(all.ToArray());
            foreach (var id in MapIds) AttachDistricts(id);
            foreach (var id in MainlandIds) UseVeniceCatIn(id);
            BuildTileAtlas();
            Debug.Log("[베네치아] 씬 12개를 다시 만들었습니다 (데이터 제외).");
        }

        const string AtlasPath = Data + "/VeniceTiles.spriteatlasv2";

        /// <summary>
        /// 베네치아 전용 타일 그림(vgpt_·vq_·ven_·vhse_, NinjaAdventure/Tiles)을 아틀라스 한 장으로 묶는다 (2026-10-26).
        /// 씬마다 수백 장의 작은 그림을 따로 불러오던 것을 큰 그림 몇 장으로 줄여 씬 전환 시간을 줄이려는 것. 다른 지역 타일은 넣지 않는다.
        /// </summary>
        [MenuItem("Pocket Caravan/베네치아/타일 아틀라스 다시 만들기")]
        public static void BuildTileAtlas()
        {
            var sprites = new List<UnityEngine.Object>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { Root + "/Art/NinjaAdventure/Tiles" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileName(path);
                if (!(name.StartsWith("vgpt_") || name.StartsWith("vq_") || name.StartsWith("ven_") || name.StartsWith("vhse_"))) continue;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) sprites.Add(sprite);
            }
            var atlas = new UnityEditor.U2D.SpriteAtlasAsset();
            atlas.Add(sprites.ToArray());
            UnityEditor.U2D.SpriteAtlasAsset.Save(atlas, AtlasPath);
            AssetDatabase.ImportAsset(AtlasPath);
            if (AssetImporter.GetAtPath(AtlasPath) is UnityEditor.U2D.SpriteAtlasImporter importer)
            {
                importer.includeInBuild = true;
                importer.packingSettings = new UnityEditor.U2D.SpriteAtlasPackingSettings { enableRotation = false, enableTightPacking = false, padding = 2, enableAlphaDilation = false };
                importer.textureSettings = new UnityEditor.U2D.SpriteAtlasTextureSettings { filterMode = FilterMode.Point, generateMipMaps = false, sRGB = true, readable = false, anisoLevel = 0 };
                var platform = importer.GetPlatformSettings("DefaultTexturePlatform");
                platform.textureCompression = TextureImporterCompression.Uncompressed;
                platform.maxTextureSize = 4096;
                importer.SetPlatformSettings(platform);
                importer.SaveAndReimport();
            }
            UnityEditor.U2D.SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
            Debug.Log($"[베네치아] 타일 아틀라스: 그림 {sprites.Count}장 → {AtlasPath}");
        }

        public static void ApplyData()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play 종료 후 실행하세요.");
            Directory.CreateDirectory(Data); AssetDatabase.Refresh();
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(Root + "/Resources/GameDatabase.asset");
            if (db == null) throw new InvalidOperationException("GameDatabase가 없습니다.");
            T Get<T>(string file) where T : ScriptableObject
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(Data + "/" + file + ".asset");
                if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, Data + "/" + file + ".asset"); }
                return asset;
            }
            var region = Get<RegionData>("Region_Venice");
            region.id = "venice"; region.displayName = "베네치아";
            region.note = "제안 초안. 리알토·키오자·무라노·산테라스모·부라노는 실제 지명을 빌린 게임 각색이며 실제 배치와 다르다. 좌표 x10~19는 팀 합의안 구역의 임시값, y는 미정.";
            EditorUtility.SetDirty(region);

            // 시설 해금표 (제안): 쉼터는 누구나, 길 안내소는 거점 등급 2+·신용 1, 보관소는 등급 3(키오자는 항구라 2)+·신용 3/2.
            TownFacility Rest(string id) => new TownFacility { id = id + "_rest", displayName = "길손 쉼터", kind = FacilityKind.Rest, minTier = 1 };
            TownFacility Info(string id, string text) => new TownFacility { id = id + "_info", displayName = "교역 길 안내소", kind = FacilityKind.RouteInformation, minTier = 2, minCredit = 1, description = text };
            TownFacility Store(string id, string label, int tier, int credit) => new TownFacility { id = id + "_warehouse", displayName = label, kind = FacilityKind.Warehouse, minTier = tier, minCredit = credit, storageCapacity = 120 };
            TownData Town(string file, string id, string label, Vector2Int coord, int tier, string note, params TownFacility[] facilities)
            {
                var t = Get<TownData>(file); t.id = id; t.displayName = label; t.region = region; t.journeyNote = note;
                t.worldCoord = coord; t.startLevel = 1; t.settlementTier = tier; t.defaultOptimal = 3;
                t.facilities = new List<TownFacility>(facilities);
                if (!db.towns.Contains(t)) db.towns.Add(t);
                return t;
            }
            // 가격 거리용 좌표. 설계도의 상대 위치(가운데 수도, 네 모서리 마을)를 따른다. 수도~마을 2, 마주 보는 마을끼리 4.
            var rialto = Town("Town_Rialto", "venice_rialto", "리알토 장터", new Vector2Int(12, 1), 3,
                "수도. 네 마을의 물건이 모이는 큰 장터. 많이 팔아도 값이 덜 떨어진다. 손수레를 판다.",
                Rest("venice_rialto"), Info("venice_rialto", "네 마을이 섬의 네 모서리에 있습니다. 북서 과수원·북동 유리공방·남서 어촌·남동 교역항. 마주 보는 마을끼리는 값 차이가 더 크지만 길이 멉니다. 유리는 가벼워 등짐에, 소금·생선은 손수레에 유리합니다."),
                Store("venice_rialto", "상인회 보관소", 3, 3));
            var chioggia = Town("Town_Chioggia", "venice_chioggia", "키오자", new Vector2Int(13, 2), 2,
                "교역항. 석호 소금이 싸다. 소금은 부라노 어촌이 비싸게 사 준다.",
                Rest("venice_chioggia"), Info("venice_chioggia", "소금은 생선을 절이는 부라노에서 값이 좋습니다. 과수원은 길이 멀지만 소금·생선이 귀합니다."),
                Store("venice_chioggia", "항구 창고", 2, 2));
            var murano = Town("Town_Murano", "venice_murano", "무라노", new Vector2Int(13, 0), 2,
                "유리공방 마을. 유리그릇은 가볍고 비싸다. 한 곳에 몰아 팔면 값이 금방 떨어진다.",
                Rest("venice_murano"), Info("venice_murano", "유리는 과수원이 조금 더 쳐 주고, 멀리 부라노에서는 값이 큽니다. 여러 장터에 나눠 파세요."));
            var santerasmo = Town("Town_SantErasmo", "venice_santerasmo", "산테라스모", new Vector2Int(11, 0), 1,
                "과수원 마을. 과일이 싸다. 멀리 키오자 항구가 과일을 귀하게 친다.", Rest("venice_santerasmo"));
            var burano = Town("Town_Burano", "venice_burano", "부라노", new Vector2Int(11, 2), 1,
                "어촌. 절인 생선이 싸다. 소금을 비싸게 사 준다.", Rest("venice_burano"));

            ItemData Good(string file, string id, string label, float weight, int price, float perMap, TownData origin, string icon, bool quest = false)
            {
                var i = Get<ItemData>(file); i.id = id; i.displayName = label; i.weight = weight; i.basePrice = price;
                i.originTown = origin; i.pricePerMap = perMap; i.questItem = quest;
                i.icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Items/{icon}.png");
                if (!db.items.Contains(i)) db.items.Add(i);
                EditorUtility.SetDirty(i); return i;
            }
            // 기존 ID(venice_salt, venice_glass)는 유지. 수치는 Tools/Integration/venice_trade_sim.py와 같은 표.
            var salt = Good("Item_VeniceSalt", "venice_salt", "석호 소금", 2, 8, .5f, chioggia, "venice_salt");
            var glass = Good("Item_VeniceGlass", "venice_glass", "유리그릇", 1, 24, .35f, murano, "venice_glass");
            var fruit = Good("Item_VeniceFruit", "venice_fruit", "과일 상자", 2, 7, .5f, santerasmo, "venice_fruit");
            var fish = Good("Item_VeniceFish", "venice_fish", "절인 생선", 2, 9, .5f, burano, "venice_fish");
            // 퀘스트 물건 (장터에서 팔 수 없다)
            var parcel = Good("Item_VeniceParcel", "venice_parcel", "여관 꾸러미", 2, 1, 0f, null, "crate", true);
            var glassCrate = Good("Item_VeniceGlassCrate", "venice_glass_crate", "유리 상자", 3, 1, 0f, null, "venice_glass", true);
            var saltCargo = Good("Item_VeniceSaltCargo", "venice_salt_cargo", "소금 자루 짐", 4, 1, 0f, null, "venice_salt", true);

            void Stock(TownData town, params (ItemData item, float optimal, float local)[] entries)
            {
                town.stock.Clear();
                foreach (var e in entries)
                    if (e.item != null) town.stock.Add(new TownData.StockEntry { item = e.item, optimal = e.optimal, localPriceMultiplier = e.local });
                EditorUtility.SetDirty(town);
            }
            Stock(rialto, (salt, 30, 1), (glass, 20, 1), (fruit, 30, 1), (fish, 30, 1));
            Stock(chioggia, (salt, 80, 1), (fish, 14, 1.15f), (glass, 8, 1), (fruit, 14, 1.15f));
            Stock(murano, (glass, 40, 1), (salt, 14, 1), (fruit, 14, 1.1f), (fish, 14, 1.1f));
            Stock(santerasmo, (fruit, 80, 1), (glass, 8, 1.2f), (fish, 14, 1.15f), (salt, 14, 1));
            Stock(burano, (fish, 80, 1), (salt, 14, 1.3f), (fruit, 14, 1), (glass, 8, 1));

            // 본토 서부의 대상 숙소 (제안): 쉼터·길 안내소와 장터. 베네치아 물건을 멀리 가져와 파는 곳(좌표가 멀어 값이 높다).
            var mainland = Get<RegionData>("Region_CenterVenice");
            mainland.id = "center_venice"; mainland.displayName = "본토 서부";
            mainland.note = "제안 초안. 중앙 대륙 서쪽 1/4(베네치아 담당). 키오자에서 배로 건너 동문 큰길까지. 중앙 권역 연결은 통합 후.";
            EditorUtility.SetDirty(mainland);
            var caravanserai = Town("Town_Caravanserai", "venice_c_caravanserai", "대상 숙소", new Vector2Int(15, 2), 2,
                "본토 길이 모이는 숙소. 쉬고, 길 소식을 듣고, 석호 물건을 비싸게 판다.",
                Rest("venice_c_caravanserai"),
                Info("venice_c_caravanserai", "서쪽 직행로는 나루터로 바로 갑니다. 북쪽은 포도밭·고갯길, 남쪽은 습지·돌다리로 돌아가는 길입니다. 동문 너머 중앙 권역은 아직 길을 닦는 중입니다."));
            caravanserai.region = mainland;
            // 본토 장터 2곳 (2026-10-26, 큰 장이 돌아가며 서는 서쪽 조각 장터: 대상 숙소·나루터·포도밭 양조장)
            var landing = Town("Town_MainlandLanding", "venice_c_landing", "본토 나루터", new Vector2Int(14, 2), 1,
                "키오자 배가 닿는 본토 부두. 석호 물건이 본토로 들어오는 첫 장터.", Rest("venice_c_landing"));
            landing.region = mainland;
            var vineyard = Town("Town_Vineyard", "venice_c_vineyard", "포도밭 양조장", new Vector2Int(14, 1), 1,
                "언덕 양조장. 포도주가 싸다. 포도주는 석호의 도시에서 값이 좋다.", Rest("venice_c_vineyard"));
            vineyard.region = mainland;
            // 새 물건: 포도주(양조장 특산), 공방에서 만드는 유리 장식(무라노)·레이스 손수건(부라노)
            var wine = Good("Item_VeniceWine", "venice_wine", "포도주", 2, 12, .45f, vineyard, "venice_wine");
            var glassArt = Good("Item_VeniceGlassArt", "venice_glass_art", "유리 장식", 1, 70, .3f, murano, "venice_glass_art");
            var lace = Good("Item_VeniceLace", "venice_lace", "레이스 손수건", 0.5f, 40, .3f, burano, "venice_lace");
            Stock(caravanserai, (glass, 10, 1.1f), (salt, 16, 1.2f), (fruit, 16, 1.1f), (fish, 16, 1.2f), (wine, 16, 1f), (glassArt, 4, 1.2f), (lace, 4, 1.2f));
            Stock(landing, (salt, 20, 1.1f), (fish, 20, 1.1f), (fruit, 16, 1f), (glass, 8, 1.1f), (wine, 20, 1f));
            Stock(vineyard, (wine, 80, 1f), (salt, 14, 1.2f), (fish, 14, 1.2f), (fruit, 20, 1f), (glass, 6, 1.15f));
            // 석호 쪽도 포도주를 조금 산다 (멀리서 온 물건이라 값이 높다)
            rialto.stock.Add(new TownData.StockEntry { item = wine, optimal = 14, localPriceMultiplier = 1.15f });
            rialto.stock.Add(new TownData.StockEntry { item = glassArt, optimal = 6, localPriceMultiplier = 1f });
            rialto.stock.Add(new TownData.StockEntry { item = lace, optimal = 6, localPriceMultiplier = 1f });
            chioggia.stock.Add(new TownData.StockEntry { item = wine, optimal = 12, localPriceMultiplier = 1.1f });
            // 2026-11-05 통합(기우) 장터 규칙: 마을마다 생필품(땔감·곡식, 산지 없는 공용 물건) 2줄. 다른 지역 물건은 기준 공급품에 넣지 않는다
            var firewood = AssetDatabase.LoadAssetAtPath<ItemData>(Root + "/Data/Goryeo/Item_firewood.asset");
            var grainCommon = AssetDatabase.LoadAssetAtPath<ItemData>(Root + "/Data/Goryeo/Item_grain.asset");
            foreach (var t in new[] { rialto, chioggia, murano, santerasmo, burano, caravanserai, landing, vineyard })
            {
                if (firewood != null) t.stock.Add(new TownData.StockEntry { item = firewood, optimal = t == vineyard ? 24 : 8, localPriceMultiplier = 1f });   // 포도밭 양조장이 땔감 산지
                if (grainCommon != null) t.stock.Add(new TownData.StockEntry { item = grainCommon, optimal = t == landing ? 24 : 8, localPriceMultiplier = 1f });   // 본토 나루터가 곡식 집산
            }

            var handcart = db.vehicles.Find(v => v != null && v.id == "handcart");
            foreach (var t in new[] { rialto, chioggia, murano, santerasmo, burano, caravanserai, landing, vineyard }) t.vehiclesForSale.Clear();
            if (handcart != null) rialto.vehiclesForSale.Add(handcart);
            BuildQuests(db, Get<QuestData>, parcel, glassCrate, saltCargo, glass, fruit, handcart);
            EditorUtility.SetDirty(db); AssetDatabase.SaveAssets();
        }


        /// <summary>
        /// 베네치아판 1장 "빈 지게에서 손수레까지" (제안 초안). 공통 Q1~Q7 틀을 기존 목표 유형만으로 만든다.
        /// 퀘스트를 주는 NPC는 베네치아 맵에만 있으므로 다른 지역에서 시작한 플레이어에게 자동으로 나타나지 않는다.
        /// Q3의 정직 선택은 Scripts/Venice/VeniceHonesty(키오자 부두 검수원, 대화 선택지)가 맡는다(낙하 없음).
        /// </summary>
        static void BuildQuests(GameDatabase db, Func<string, QuestData> get, ItemData parcel, ItemData glassCrate, ItemData saltCargo,
            ItemData glass, ItemData fruit, VehicleData handcart)
        {
            Directory.CreateDirectory(Data + "/Quests"); AssetDatabase.Refresh();
            const string chain = "베네치아 1장: 빈 지게에서 손수레까지";
            QuestObjective O(ObjectiveType type, string text, string target = null, ItemData item = null, int count = 1, string line = null)
                => new QuestObjective { type = type, text = text, targetId = target, item = item, count = count, completeLine = line };
            QuestData Q(string file, string id, string title, string giver, QuestData req, string summary, string offer,
                int money, int credit, string finish, params QuestObjective[] steps)
            {
                var q = get("Quests/" + file);
                q.id = id; q.title = title; q.chain = chain; q.summary = summary; q.giverNpc = giver; q.offerLine = offer;
                q.requires = req != null ? new List<QuestData> { req } : new List<QuestData>();
                q.giveItem = null; q.giveCount = 0; q.offerItem = null; q.offerCount = 0; q.offerPrice = 0;
                q.objectives = new List<QuestObjective>(steps);
                q.rewardMoney = money; q.rewardCredit = credit; q.finishNote = finish;
                if (!db.quests.Contains(q)) db.quests.Add(q);
                EditorUtility.SetDirty(q);
                return q;
            }
            var q1 = Q("V1_FirstDelivery", "venice_q1_delivery", "첫 배달", "venice_innkeeper", null,
                "리알토 여관 주인의 꾸러미를 부라노 어부에게 전한다.",
                "자네가 새로 온 행상이로군.\n이 꾸러미를 부라노 어부에게 전해 주겠나?\n남서쪽 다리를 건너면 어촌이야.",
                25, 1, "고맙네. 첫 배달 삯이야.",
                O(ObjectiveType.Deliver, "부라노 어부에게 꾸러미 전하기", "venice_fisher", parcel, 1, "여관에서 보낸 그물실이군. 수고했네."));
            q1.giveItem = parcel; q1.giveCount = 1;
            var q2 = Q("V2_BuyGlass", "venice_q2_glass", "유리그릇 사 오기", "venice_clerk", q1,
                "무라노에서 유리그릇 2개를 사서 상인회 서기에게 납품한다.",
                "상인회에 유리그릇 두 개가 필요하네.\n무라노 공방 장터에서 사 오게.\n산지에서 싸게 사서 납품하면 남는 게 있지.",
                70, 1, "산지 값과 납품 값의 차이, 그게 장사야.",
                O(ObjectiveType.Buy, "무라노 장터에서 유리그릇 2개 사기", "venice_murano", glass, 2),
                O(ObjectiveType.Deliver, "상인회 서기에게 유리그릇 2개 납품", "venice_clerk", glass, 2, "흠집 하나 없군. 값을 쳐 주지."));
            var q3 = Q("V3_GlassCrate", "venice_q3_crate", "깨지기 쉬운 짐", "venice_glassmaker", q2,
                "유리 상자를 키오자 항구 창고지기에게 전한다. 거친 길에서는 천천히.",
                "이 유리 상자를 키오자 창고에 맡겨 주게.\n수레가 흔들리는 길은 천천히 지나가고.\n배에 실을 물건이야.",
                40, 2, "상자를 열어 보니 하나도 안 깨졌다더군. 믿을 만한 상인이야.",
                O(ObjectiveType.Deliver, "키오자 창고지기에게 유리 상자 전하기", "venice_warehouse", glassCrate, 1, "개수도 맞고 깨진 것도 없군. 장부에 적어 두지."));
            q3.giveItem = glassCrate; q3.giveCount = 1;
            var q4 = Q("V4_SellerPride", "venice_q4_sell", "파는 솜씨", "venice_clerk", q3,
                "물건을 팔아 이익 60냥을 남긴 뒤 상인회 서기에게 알린다.",
                "베네치아 상인은 파는 법을 안다네.\n물건을 팔아 이익 60냥을 남겨 보게.\n어디서 사서 어디에 팔지는 자네 몫이야.",
                40, 1, "값이 좋은 곳을 고르는 눈이 생겼군.",
                O(ObjectiveType.Profit, "판매 이익 60냥 남기기", null, null, 60),
                O(ObjectiveType.Talk, "상인회 서기에게 알리기", "venice_clerk", null, 1, "장부를 보니 제대로 남겼군."));
            var q5 = Q("V5_HeavySalt", "venice_q5_salt", "먼 길의 소금 짐", "venice_warehouse", q4,
                "무거운 소금 자루 짐을 산테라스모 과수원 일꾼에게 전한다. 수도를 지나는 길과 외곽 모래톱 길을 비교해 보자.",
                "과수원에서 소금이 급하다네.\n짐이 무거우니 길을 잘 고르게.\n수도를 지나는 길은 넓고, 바깥 모래톱 길은 조용하지.",
                60, 1, "먼 길이었지. 무거운 짐은 길을 고르는 게 반이야.",
                O(ObjectiveType.Deliver, "산테라스모 과수원 일꾼에게 소금 짐 전하기", "venice_gardener", saltCargo, 2, "이걸로 올해 과일을 절이겠네. 고맙네."));
            q5.giveItem = saltCargo; q5.giveCount = 2;
            Q("V6_SplitSales", "venice_q6_split", "나눠 팔기", "venice_gardener", q5,
                "리알토에 과일이 넘친다. 과일을 키오자와 무라노 두 곳에 나눠 판다.",
                "다른 상인이 리알토에 과일을 잔뜩 풀었대.\n한 곳에 몰아 팔면 값이 떨어지지.\n키오자와 무라노에 세 상자씩 나눠 팔아 보게.",
                30, 1, "나눠 팔면 값이 덜 떨어지지. 이제 알겠지?",
                O(ObjectiveType.Sell, "키오자 장터에 과일 3상자 팔기", "venice_chioggia", fruit, 3),
                O(ObjectiveType.Sell, "무라노 장터에 과일 3상자 팔기", "venice_murano", fruit, 3),
                O(ObjectiveType.Talk, "과수원 일꾼에게 알리기", "venice_gardener", null, 1, "두 곳 다 값을 잘 받았군."));
            var q6 = db.quests.Find(x => x != null && x.id == "venice_q6_split");
            var buyCart = new QuestObjective { type = ObjectiveType.BuyVehicle, text = "리알토 장터에서 손수레 사기", vehicle = handcart };
            Q("V7_Handcart", "venice_q7_handcart", "손수레", "venice_innkeeper", q6,
                "리알토 장터에서 손수레를 산다.",
                "이제 등짐으로는 모자라겠군.\n리알토 장터에서 손수레를 사 보게.\n소금이나 생선처럼 무거운 짐을 한 번에 나를 수 있지.",
                0, 1, "손수레라니, 이제 어엿한 상인이야.",
                buyCart, O(ObjectiveType.Talk, "여관 주인에게 보여 주기", "venice_innkeeper", null, 1, "바퀴 소리가 좋군. 축하하네."));

            // 2장 "석호를 넘어" (2026-10-27, 제안): 본토 서부의 포도주·대상 숙소 큰 장·특산 공방·동문으로 이어진다.
            const string chain2 = "베네치아 2장: 석호를 넘어";
            ItemData Item(string id) => db.items.Find(i => i != null && i.id == id);
            QuestData Q2(string file, string id, string title, string giver, QuestData req, string summary, string offer,
                int money, int credit, string finish, params QuestObjective[] steps)
            {
                var q = Q(file, id, title, giver, req, summary, offer, money, credit, finish, steps);
                q.chain = chain2;
                return q;
            }
            var q7 = db.quests.Find(x => x != null && x.id == "venice_q7_handcart");
            var q8 = Q2("V8_WineRoad", "venice_q8_wine", "포도주 길", "venice_warehouse", q7,
                "본토 포도밭 양조장에서 포도주 3병을 사서 리알토 장터에 판다.",
                "본토 배가 들어오면 포도주 얘기뿐이야.\n키오자 잔교에서 배를 타고 포도밭에 가 보게.\n세 병을 사서 리알토에 팔아 보게.",
                80, 1, "석호 밖에서 사 온 물건이 섬에서 값을 받는군.",
                O(ObjectiveType.VisitMap, "본토 나루터에 가기", "venice_c_landing"),
                O(ObjectiveType.Buy, "포도밭 양조장에서 포도주 3병 사기", "venice_c_vineyard", Item("venice_wine"), 3),
                O(ObjectiveType.Sell, "리알토 장터에 포도주 3병 팔기", "venice_rialto", Item("venice_wine"), 3),
                O(ObjectiveType.Talk, "창고지기에게 알리기", "venice_warehouse", null, 1, "포도주 배가 늘겠군. 장부에 적어 두지."));
            var q9 = Q2("V9_CaravanFair", "venice_q9_fair", "대상 숙소의 큰 장", "venice_clerk", q8,
                "본토 대상 숙소 장터에서 물건 4개를 판다. 큰 장이 서는 날이면 덤을 받는다.",
                "본토 서쪽에 대상 숙소가 있네.\n날마다 세 마을 중 한 곳에 큰 장이 서지.\n숙소 장터에서 물건 넷을 팔아 보게.",
                90, 2, "큰 장 날을 맞추면 덤이 붙지. 장사는 날짜도 보는 거야.",
                O(ObjectiveType.VisitMap, "대상 숙소에 가기", "venice_c_post"),
                O(ObjectiveType.Sell, "대상 숙소 장터에 물건 4개 팔기", "venice_c_caravanserai", null, 4),
                O(ObjectiveType.Talk, "상인회 서기에게 알리기", "venice_clerk", null, 1, "본토 상인들 사이에도 자네 이름이 돌겠군."));
            var q10 = Q2("V10_GlassOrder", "venice_q10_glassart", "유리 장식 주문", "venice_glassmaker", q9,
                "무라노 장식 공방에서 유리 장식을 만들어 상인회 서기에게 납품한다.",
                "동문 너머 손님이 유리 장식을 주문했어.\n장식 공방에 유리그릇 둘을 맡기면 만들어 주지.\n다 되면 상인회 서기에게 가져가.",
                110, 1, "이 장식이 대상 짐에 실려 멀리 가겠군.",
                O(ObjectiveType.Deliver, "상인회 서기에게 유리 장식 납품", "venice_clerk", Item("venice_glass_art"), 1, "빛이 곱군. 주문한 손님이 좋아하겠어."));
            Q2("V11_EastGate", "venice_q11_gate", "동문 너머", "venice_innkeeper", q10,
                "본토 동쪽 끝 동문 큰길까지 가 본 뒤 여관 주인에게 이야기한다.",
                "석호 상인이 본토 끝을 모르면 쓰나.\n동쪽 끝 동문 큰길까지 가 보게.\n중앙 대륙으로 가는 길이 거기서 시작되지.",
                50, 1, "중앙 대륙 길이 열리면 자네가 제일 먼저 가겠군.",
                O(ObjectiveType.VisitMap, "동문 큰길에 가기", "venice_c_gate"),
                O(ObjectiveType.Talk, "여관 주인에게 이야기하기", "venice_innkeeper", null, 1, "성벽에 사자 깃발이 걸렸다고? 우리 섬 상인들 솜씨지."));
        }

        [Serializable] class DistrictJson { public string name; public int x0, y0, x1, y1; }
        [Serializable] class MapJson { public string id; public int w, h; public DistrictJson[] districts; public string defaultDistrict; }

        /// <summary>맵 JSON의 구역 이름을 생성된 씬에 붙인다 (JSON은 위쪽이 0, Unity 셀은 아래쪽이 0).</summary>
        public static void AttachDistricts(string mapId)
        {
            var entry = WorldRegistry.Load().Map(mapId);
            var json = JsonUtility.FromJson<MapJson>(File.ReadAllText(entry.jsonPath));
            var scene = EditorSceneManager.OpenScene(entry.scenePath, OpenSceneMode.Single);
            var go = new GameObject("베네치아 구역 이름");
            var districts = go.AddComponent<VeniceDistricts>();
            districts.defaultName = string.IsNullOrEmpty(json.defaultDistrict) ? "석호 길" : json.defaultDistrict;
            foreach (var d in json.districts ?? Array.Empty<DistrictJson>())
            {
                int yMin = json.h - 1 - d.y1, yMax = json.h - 1 - d.y0;
                districts.districts.Add(new VeniceDistricts.District { name = d.name, cells = new RectInt(d.x0, yMin, d.x1 - d.x0, yMax - yMin) });
            }
            UseVeniceCat();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>본토 씬: 구역 이름 없이 플레이어 그림만 흰 고양이로 바꾼다.</summary>
        static void UseVeniceCatIn(string mapId)
        {
            var entry = WorldRegistry.Load().Map(mapId);
            var scene = EditorSceneManager.OpenScene(entry.scenePath, OpenSceneMode.Single);
            UseVeniceCat();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// 시험용: 이 씬의 플레이어 그림만 베네치아 흰 고양이(Art/Characters/venice_cat_tall_*)로 바꾼다.
        /// 출신·저장·공용 빌더는 바꾸지 않는다. 지역별 플레이어 동물은 팀 합의 전 제안(ART-01).
        /// 출신 선택이 생기면 출신에 따라 그림을 고르는 공용 방식으로 옮긴다.
        /// </summary>
        static void UseVeniceCat()
        {
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var anim = player != null ? player.anim : null;
            if (anim == null) { Debug.LogWarning("[베네치아] 플레이어를 찾지 못해 그림을 바꾸지 않았습니다."); return; }
            var frames = new Sprite[16];
            const string dirs = "dulr";
            for (int d = 0; d < 4; d++)
                for (int f = 0; f < 4; f++)
                    frames[d * 4 + f] = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Characters/venice_cat_tall_{dirs[d]}{f}.png");
            if (Array.Exists(frames, s => s == null)) { Debug.LogWarning("[베네치아] venice_cat 그림이 없어 너구리를 유지합니다."); return; }
            anim.frames = frames;
            var sr = anim.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = frames[0];
            EditorUtility.SetDirty(anim);
        }
    }
}
