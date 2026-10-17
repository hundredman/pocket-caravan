using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 고려 시작 지역의 데이터(물건, 마을, 이동 수단, 출신, 퀘스트)와 임시 캐릭터 그림을 만들고,
    /// 맵 JSON으로 씬을 만든다. 데이터는 이 코드가 기준이라 실행할 때마다 값을 다시 쓴다.
    /// (밸런스를 Inspector에서 고쳤다면 여기 숫자도 같이 고칠 것)
    /// </summary>
    public static class StarterBuilder
    {
        const string Root = "Assets/PocketCaravan";
        const string ArtDir = Root + "/Art/Placeholder";
        const string DataDir = Root + "/Data/Goryeo";
        const string QuestDir = DataDir + "/Quests";
        const string OriginDir = Root + "/Data/Origins";
        const string DbPath = Root + "/Resources/GameDatabase.asset";

        [MenuItem("Pocket Caravan/1. 시작 세트 만들기 (데이터 + 맵)", priority = 1)]
        public static void BuildAll()
        {
            if (EditorApplication.isPlaying || !EditorUtility.DisplayDialog("고려 초기 데이터 재생성",
                "기존 고려 데이터와 공용 GameDatabase 목록을 초기값으로 덮어씁니다.\n다른 지역을 연결한 팀 작업에서는 실행하지 마세요. 맵만 갱신하려면 메뉴 2를 사용하세요.", "초기값으로 재생성", "취소")) return;
            foreach (var d in new[] { ArtDir, DataDir, QuestDir, OriginDir, Root + "/Resources" }) EnsureFolder(d);
            PlaceholderArt.WriteAll(ArtDir);
            AssetDatabase.Refresh();
            BuildData();
            AssetDatabase.SaveAssets();
            MapJsonBuilder.BuildAll();
        }

        [MenuItem("Pocket Caravan/3. 시작 맵 열기", priority = 3)]
        public static void OpenStart()
        {
            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(WorldRegistry.Load().ScenePaths[0]);
        }

        // ---------- 데이터 ----------

        static void BuildData()
        {
            var goryeo = Upsert<RegionData>($"{DataDir}/Region_Goryeo.asset", r => { r.id = "goryeo"; r.displayName = "고려"; });

            TownData Town(string file, string id, string name, Vector2Int coord, int level) =>
                Upsert<TownData>($"{DataDir}/{file}.asset", t =>
                {
                    t.id = id; t.displayName = name; t.region = goryeo; t.worldCoord = coord; t.startLevel = level;
                    t.stock.Clear(); t.vehiclesForSale.Clear(); t.defaultOptimal = 4f;
                });
            var naru = Town("Town_Naru", "naru", "예성 나루 마을", new Vector2Int(0, 0), 1);
            var gaegyeong = Town("Town_Gaegyeong", "gaegyeong", "개경", new Vector2Int(2, 0), 3);   // 수도: 처음부터 큼
            var dakmok = Town("Town_Dakmok", "dakmok", "닥나무 마을", new Vector2Int(3, 0), 1);

            ItemData Item(string id, string name, float weight, float price, TownData origin, bool quest = false) =>
                Upsert<ItemData>($"{DataDir}/Item_{id}.asset", i =>
                {
                    i.id = id; i.displayName = name; i.weight = weight; i.basePrice = price; i.originTown = origin;
                    i.pricePerMap = 0.5f; i.questItem = quest;
                    i.icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Items/{id}.png");
                });
            var ginseng = Item("ginseng", "인삼", 1f, 40f, gaegyeong);
            var najeon = Item("najeon", "나전칠기", 3f, 120f, gaegyeong);
            var paper = Item("paper", "종이", 2f, 15f, dakmok);
            var salt = Item("salt", "소금", 2f, 10f, naru);
            var grain = Item("grain", "곡식", 3f, 8f, null);
            var wood = Item("firewood", "땔감", 4f, 4f, null);
            var saltParcel = Item("salt_parcel", "소금 꾸러미", 2f, 1f, null, quest: true);

            var walk = Upsert<VehicleData>($"{DataDir}/Vehicle_Walk.asset", v =>
            {
                v.id = "walk"; v.displayName = "등짐"; v.capacity = 10f; v.price = 0; v.minTownLevel = 1;
                v.speedByGrade = new[] { 1f, 1.05f, 1.1f, 1.1f };
            });
            var cart = Upsert<VehicleData>($"{DataDir}/Vehicle_Handcart.asset", v =>
            {
                v.id = "handcart"; v.displayName = "손수레"; v.capacity = 30f; v.price = 150; v.minTownLevel = 1;
                v.speedByGrade = new[] { 0.45f, 0.85f, 1.15f, 1.2f };
            });

            // 재고: 산지는 크게, 소비지는 물건이 깊게 팔리도록 넉넉히 (손수레가 의미 있게)
            naru.stock.AddRange(new[] { S(salt, 30), S(grain, 20), S(wood, 20), S(ginseng, 6), S(paper, 6) });
            gaegyeong.stock.AddRange(new[] { S(ginseng, 20), S(najeon, 6), S(paper, 10), S(salt, 10), S(grain, 25), S(wood, 25) });
            gaegyeong.vehiclesForSale.Add(cart);
            dakmok.stock.AddRange(new[] { S(paper, 30), S(ginseng, 8), S(salt, 8), S(najeon, 3), S(grain, 15), S(wood, 25) });
            foreach (var t in new[] { naru, gaegyeong, dakmok }) EditorUtility.SetDirty(t);

            var origin = Upsert<OriginData>($"{DataDir}/Origin_Goryeo.asset", o =>
            {
                o.id = "goryeo"; o.displayName = "고려 개경상인"; o.homeRegion = goryeo; o.ledgerStaleMultiplier = 0.5f;
            });
            Upsert<OriginData>($"{OriginDir}/Origin_Arabia.asset", o => { o.id = "arabia"; o.displayName = "아라비아 대상"; o.longHaulMinMaps = 3; o.longHaulBonus = 0.05f; });
            Upsert<OriginData>($"{OriginDir}/Origin_Venice.asset", o => { o.id = "venice"; o.displayName = "베네치아 상인"; o.resaleDropMultiplier = 0.8f; });
            Upsert<OriginData>($"{OriginDir}/Origin_Viking.asset", o => { o.id = "viking"; o.displayName = "바이킹 교역상"; o.speedMultiplier = 1.05f; o.offroadSpeedMultiplier = 1.3f; o.startingCredit = -2; });

            var herbs = Item("herbs_parcel", "약재 꾸러미", 2f, 1f, null, quest: true);
            var hanji = Item("hanji_parcel", "한지 꾸러미", 2f, 1f, null, quest: true);
            var quests = BuildTutorialQuests(saltParcel, paper, wood, cart, ginseng, herbs, hanji);

            var db = Upsert<GameDatabase>(DbPath, d =>
            {
                d.startMoney = 100; d.startVehicle = walk; d.startOrigin = origin;
                d.crateIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Items/crate.png");
                d.townLevelVolume = new[] { 0, 500, 1800, 8000 };   // 시연: 몇 번의 교역으로 등급이 오르게
            });
            db.items = new List<ItemData> { ginseng, najeon, paper, salt, grain, wood, saltParcel, herbs, hanji };
            db.towns = new List<TownData> { naru, gaegyeong, dakmok };
            db.vehicles = new List<VehicleData> { walk, cart };
            db.quests = quests;
            EditorUtility.SetDirty(db);
        }

        static TownData.StockEntry S(ItemData item, float optimal) => new TownData.StockEntry { item = item, optimal = optimal };

        // ---------- 퀘스트 ----------

        const string NaruChief = "naru_chief", Gaekju = "gaekju", DakmokChief = "dakmok_chief";

        static QuestObjective O(ObjectiveType type, string text, string target = null, ItemData item = null, int count = 1, string line = null, VehicleData vehicle = null) =>
            new QuestObjective { type = type, text = text, targetId = target, item = item, count = count, completeLine = line, vehicle = vehicle };

        static List<QuestData> BuildTutorialQuests(ItemData parcel, ItemData paper, ItemData wood, VehicleData cart,
            ItemData ginsengItem, ItemData herbsItem, ItemData hanjiItem)
        {
            const string chain = "행상의 첫걸음";
            var list = new List<QuestData>();
            QuestData Q(string file, System.Action<QuestData> init)
            {
                var q = Upsert<QuestData>($"{QuestDir}/{file}.asset", x =>
                {
                    x.requires.Clear(); x.objectives.Clear(); x.giveItem = null; x.giveCount = 0;
                    x.offerItem = null; x.offerCount = 0; x.offerPrice = 0;
                    x.rewardMoney = 0; x.rewardCredit = 0; x.finishNote = ""; x.chain = "";
                    init(x);
                });
                list.Add(q);
                return q;
            }

            var q1 = Q("T1_FirstDelivery", q =>
            {
                q.id = "tutorial_1"; q.title = "개경으로 가는 첫 짐"; q.chain = chain; q.giverNpc = NaruChief;
                q.summary = "나루에 들어온 소금 꾸러미를 개경 객주에게 전한다. 동쪽 예성 길을 따라가면 개경 남문이 나온다.";
                q.offerLine = "자네가 행상이 되겠다는 젊은이로군. 마침 배로 들어온 소금 꾸러미를 개경 객주에게 보내야 하네. 동쪽 길을 따라가면 고려의 서울, 개경 남문이 나오지. 처음 보면 입이 떡 벌어질 걸세.";
                q.giveItem = parcel; q.giveCount = 1;
                q.objectives.Add(O(ObjectiveType.VisitMap, "개경에 가기", "Gaegyeong"));
                q.objectives.Add(O(ObjectiveType.Deliver, "개경 객주에게 소금 꾸러미 전하기", Gaekju, parcel, 1,
                    "나루 촌장이 보낸 소금이군. 개경은 처음인가? 남문에서 곧장 올라가면 만월대, 가로지르는 큰길이 시전 거리일세. 고려의 물건이란 물건은 다 여기 모이지."));
                q.rewardMoney = 30; q.rewardCredit = 1;
            });

            var q2 = Q("T2_BuyPaper", q =>
            {
                q.id = "tutorial_2"; q.title = "종이 사 오기"; q.chain = chain; q.giverNpc = Gaekju; q.requires.Add(q1);
                q.summary = "동문 밖 닥나무 마을에서 종이를 사서 개경 객주에게 가져간다.";
                q.offerLine = "객주에 장부 쓸 종이가 떨어졌네. 동문 밖 닥나무 마을 종이가 제일이지. 그 마을 장터 상인에게 말을 걸면 사고팔 수 있네.";
                q.objectives.Add(O(ObjectiveType.Buy, "닥나무 마을 장터에서 종이 사기", null, paper, 2));
                q.objectives.Add(O(ObjectiveType.Deliver, "개경 객주에게 종이 가져가기", Gaekju, paper, 2,
                    "질기고 하얀 닥나무 종이로군! 산 값보다 넉넉히 쳐 주겠네."));
                q.rewardMoney = 45; q.rewardCredit = 1;
            });

            var q3 = Q("T3_Profit", q =>
            {
                q.id = "tutorial_3"; q.title = "객주가 넘겨준 인삼"; q.chain = chain; q.giverNpc = Gaekju; q.requires.Add(q2);
                q.summary = "객주가 초보 상인에게만 싸게 넘겨준 인삼 3뿌리를 예성 나루 마을 장터에 판다.\n" +
                            "팁: 개경에는 인삼과 나전칠기가 모이고, 닥나무 마을은 종이, 나루는 소금을 공급한다. " +
                            "먼 곳의 기준가는 높아도 실제 값은 재고에 따라 달라진다. 장부와 원가를 비교하고, 한 곳에 몰아 팔면 값이 떨어지는 것도 살펴보자.";
                q.offerLine = "종이 고맙네. 인삼은 여러 곳에서 이 개경 장터로 모인다네. 초보 상인에게 내 인삼 3뿌리를 하나에 22냥, 모두 66냥에 넘겨주겠네. " +
                              "나루에서 팔아 보되 먼저 원가와 판매가를 비교하게. 가는 동안 시세가 달라질 수도 있다네.";
                q.offerItem = ginsengItem; q.offerCount = 3; q.offerPrice = 22;
                q.objectives.Add(O(ObjectiveType.Sell, "예성 나루 마을 장터에 인삼 3뿌리 팔기", "naru", ginsengItem, 3));
                q.objectives.Add(O(ObjectiveType.Talk, "나루 마을 촌장에게 이야기하기", NaruChief, null, 1,
                    "개경에서 가져온 인삼을 팔았군. 받은 돈에서 매입 원가를 빼면 거래 손익이 나오네. 이익이었는지 손해였는지 살펴보고 다음 짐을 골라 보게."));
                q.rewardMoney = 20; q.rewardCredit = 1;
            });

            var q4 = Q("T4_Herbs", q =>
            {
                q.id = "tutorial_4"; q.title = "닥나무 마을로 가는 약재"; q.chain = chain; q.giverNpc = NaruChief; q.requires.Add(q3);
                q.summary = "나루에 들어온 약재 꾸러미를 닥나무 마을 촌장에게 전한다. 개경을 지나 동문 밖으로 가면 된다.\n" +
                            "팁: 가는 길에 마을 게시판의 배달 의뢰도 함께 맡으면 한 번에 두 몫을 번다.";
                q.offerLine = "배로 약재가 들어왔는데 닥나무 마을에서 급히 찾는다네. 종이 뜨다 다친 사람이 많아서 말이야. " +
                              "개경을 지나 동문 밖이니 조금 멀지만, 수고비는 넉넉히 주겠네. 장터 옆 게시판에 같은 쪽 의뢰가 있으면 같이 맡아 가게.";
                q.giveItem = herbsItem; q.giveCount = 1;
                q.objectives.Add(O(ObjectiveType.Deliver, "닥나무 마을 촌장에게 약재 꾸러미 전하기", DakmokChief, herbsItem, 1,
                    "약재로군! 살았네. 자네처럼 짐을 날라 주는 사람이 있어야 마을이 돌아가지. 마을마다 게시판 의뢰를 맡으면 꾸준히 벌 수 있다네."));
                q.rewardMoney = 50; q.rewardCredit = 1;
            });

            var q5 = Q("T4_Handcart", q =>
            {
                q.id = "tutorial_5"; q.title = "손수레"; q.chain = chain; q.giverNpc = DakmokChief; q.requires.Add(q4);
                q.summary = "개경 시전에서 손수레(150냥)를 산다. 등짐보다 세 배를 싣고, 길 위에서 더 빠르다.";
                q.offerLine = "등짐으로는 한 번에 몇 냥 못 버네. 개경 시전 상인에게 손수레를 사 보게. 길 위에서는 빠르지만 풀밭에서는 느리니, 길을 잘 골라 다니게.";
                q.objectives.Add(O(ObjectiveType.BuyVehicle, "개경 시전에서 손수레 사기", null, null, 1, null, cart));
                q.objectives.Add(O(ObjectiveType.Talk, "개경 객주에게 보이기", Gaekju, null, 1, "어엿한 수레상이 되었구먼! 이제 자네도 한 사람 몫의 상인일세."));
                q.rewardCredit = 2;
                q.finishNote = "튜토리얼 완료! 게시판 의뢰와 마을 사람들의 부탁으로 벌고, 산지에서 사서 먼 곳에 팔아 보자.";
            });

            Q("S3_Hanji", q =>
            {
                q.id = "dakmok_hanji"; q.title = "개경으로 가는 한지"; q.giverNpc = DakmokChief; q.requires.Add(q4);
                q.summary = "닥나무 마을 한지 꾸러미를 개경 객주에게 전한다.";
                q.offerLine = "객주가 장부 쓸 한지를 또 주문했다네. 가는 길이면 이 꾸러미 좀 전해 주게. 수고비는 객주가 줄 걸세.";
                q.giveItem = hanjiItem; q.giveCount = 1;
                q.objectives.Add(O(ObjectiveType.Deliver, "개경 객주에게 한지 꾸러미 전하기", Gaekju, hanjiItem, 1, "닥나무 마을 한지로군. 역시 빛깔이 좋아. 수고비 받게."));
                q.rewardMoney = 45; q.rewardCredit = 1;
            });

            Q("S2_Board", q =>
            {
                q.id = "naru_board"; q.title = "의뢰 게시판"; q.giverNpc = NaruChief; q.requires.Add(q1);
                q.summary = "마을 의뢰 게시판에서 배달 의뢰를 맡아 기한 안에 다른 마을 게시판에 납품한다.";
                q.offerLine = "개경에 다녀왔다니 이제 길은 알겠구먼. 장터 옆 게시판에는 짐을 맡기려는 사람들 쪽지가 붙어 있다네. 하나 맡아 기한 안에 날라 보게. 늦거나 짐이 상하면 값을 덜 받고, 신용도 깎이니 조심하고.";
                q.objectives.Add(O(ObjectiveType.CompleteContract, "게시판 배달 의뢰 하나 마치기", "naru", null, 1));
                q.objectives.Add(O(ObjectiveType.Talk, "나루 마을 촌장에게 알리기", NaruChief, null, 1, "벌써 해냈나! 의뢰를 잘 해내면 신용이 쌓이고, 신용이 높으면 귀한 짐도 맡길 걸세."));
                q.rewardMoney = 25; q.rewardCredit = 1;
            });

            Q("S1_Firewood", q =>
            {
                q.id = "dakmok_firewood"; q.title = "땔감이 모자라요"; q.giverNpc = DakmokChief; q.requires.Add(q5);
                q.summary = "닥나무 마을에 땔감 4단을 가져간다. 무거워서 손수레가 필요하다.";
                q.offerLine = "종이를 뜨려면 닥나무를 삶아야 하는데 땔감이 모자라네. 땔감 4단만 구해 주게. 무거우니 수레가 있어야 할 걸세.";
                q.objectives.Add(O(ObjectiveType.Deliver, "닥나무 마을 촌장에게 땔감 4단 가져가기", DakmokChief, wood, 4, "살았네! 이걸로 한동안 종이를 뜰 수 있겠어."));
                q.rewardMoney = 60; q.rewardCredit = 1;
            });

            foreach (var q in list) EditorUtility.SetDirty(q);
            return list;
        }

        // ---------- 도우미 ----------

        /// <summary>있으면 불러와 값을 다시 쓰고, 없으면 만든다.</summary>
        static T Upsert<T>(string path, System.Action<T> apply) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            bool create = a == null;
            if (create) a = ScriptableObject.CreateInstance<T>();
            apply(a);
            if (create) AssetDatabase.CreateAsset(a, path);
            EditorUtility.SetDirty(a);
            return a;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
