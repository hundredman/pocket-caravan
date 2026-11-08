using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>고려 경험 패치. 기존 ID/목표 순서·진행·타 지역 데이터는 보존한다.</summary>
    public static class GoryeoExperienceUpgrade
    {
        const string Data = "Assets/PocketCaravan/Data/Goryeo/";
        [MenuItem("Pocket Caravan/고려/교역 역할·선택 의뢰 갱신")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play 종료 후 실행하세요.");
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/PocketCaravan/Resources/GameDatabase.asset");
            if (db == null) throw new InvalidOperationException("GameDatabase 누락");
            ItemData Item(string id) => db.items.Find(i => i != null && i.id == id);
            TownData Town(string id) => db.towns.Find(t => t != null && t.id == id);
            void Price(string townId, string itemId, float factor)
            {
                var town = Town(townId); var item = Item(itemId);
                if (town == null || item == null) throw new InvalidOperationException(townId + "/" + itemId);
                var stock = town.stock.Find(e => e.item == item);
                if (stock == null) { stock = new TownData.StockEntry { item = item, optimal = 12 }; town.stock.Add(stock); }
                stock.localPriceMultiplier = factor;
                EditorUtility.SetDirty(town);
            }
            Price("goryeo_jangdan", "grain", .6f);
            Price("goryeo_jangdan", "firewood", .55f);
            Price("goryeo_jangdan", "paper", 1.05f);
            Price("goryeo_songak", "ginseng", .26f);
            // NPC 상인의 한 번 매입4개로 초보자2개 거래까지 사라지지 않게 공급 폭을 둔다.
            Town("goryeo_songak").stock.Find(e => e.item == Item("ginseng")).optimal = 24;
            Price("goryeo_songak", "salt", 1.15f);
            Price("gaegyeong", "grain", 1.6f);
            Price("dakmok", "paper", .85f);
            Price("dakmok", "firewood", 1.8f);

            void Note(string id, string text) { var t = Town(id); t.journeyNote = text; EditorUtility.SetDirty(t); }
            Note("naru", "소금과 하역의 출발점. 개경으로 오갈 때 게시판 배달과 매매를 함께 비교해 보세요.");
            Note("gaegyeong", "주변 생산품이 모이는 수도. 곡식 판매처를 확인하고, 신용 3부터 객주에 재고를 나누어 맡길 수 있습니다.");
            Note("dakmok", "종이 공급·땔감 수요의 마을. 장단에서 땔감을 들여오고, 돌아갈 때 종이의 판매가를 비교해 보세요.");
            Note("goryeo_songak", "적은 짐으로 약재를 거래할 수 있는 산록 집산지. 닥목까지 산록 길과 개경 큰길을 수단에 맞게 비교하세요.");
            Note("goryeo_jangdan", "곡식·땔감 공급의 들마을. 적재 여유를 남겨 배달을 겸하거나, 닥목에서 귀환품을 골라 보세요.");

            var first = db.quests.Find(q => q != null && q.id == "tutorial_2");
            var cart = db.quests.Find(q => q != null && q.id == "tutorial_5");
            if (first == null || cart == null) throw new InvalidOperationException("기존 도입 퀘스트 누락");
            foreach (var o in first.objectives)
                if (o.type == ObjectiveType.Buy && o.item == Item("paper")) o.targetId = "dakmok";
            first.finishNote = "이제 다음 장사는 직접 골라 보세요. 북쪽 송악은 적은 짐의 약재 거래, 남쪽 장단은 곡식과 땔감 거래처입니다. 손수레를 먼저 사도 됩니다.";
            // 기존 목표 수와 순서는 유지해 활성 저장의 step/amount를 보존한다.
            cart.requires = new List<QuestData> { first };
            cart.finishNote = "적재 공간이 세 배가 됐습니다. 장단의 땔감을 닥목으로 옮기거나 귀환품을 더 실어 보세요. 수레값 외에 다음 매입 자금도 남겨 두세요.";
            EditorUtility.SetDirty(first); EditorUtility.SetDirty(cart);
            foreach (var oldId in new[] { "tutorial_3", "tutorial_4" })
            {
                var optional = db.quests.Find(q => q != null && q.id == oldId);
                if (optional != null) { optional.chain = "행상의 선택 심부름"; EditorUtility.SetDirty(optional); }
            }
            QuestObjective Trade(ObjectiveType type, string text, string town, string item, int count) =>
                new QuestObjective { type = type, text = text, targetId = town, item = Item(item), count = count };
            QuestData Quest(string file, string id, string title, string giver, string offer, QuestData prerequisite, params QuestObjective[] objectives)
            {
                var path = Data + "Quests/" + file + ".asset";
                var quest = AssetDatabase.LoadAssetAtPath<QuestData>(path);
                if (quest == null) { quest = ScriptableObject.CreateInstance<QuestData>(); AssetDatabase.CreateAsset(quest, path); }
                quest.id = id; quest.title = title; quest.giverNpc = giver; quest.chain = "고려의 거래처 · 선택";
                quest.summary = offer; quest.offerLine = offer;
                quest.requires = prerequisite != null ? new List<QuestData> { prerequisite } : new List<QuestData>();
                quest.requiresAny = new List<QuestData>(); quest.choiceGroup = "";
                quest.objectives = new List<QuestObjective>(objectives);
                quest.rewardMoney = 10; quest.rewardCredit = 1;
                quest.finishNote = "판매 대금에서 매입 원가를 빼고, 돌아갈 때 실을 물건도 살펴보세요. 시세는 거래량에 따라 달라집니다.";
                if (!db.quests.Contains(quest)) db.quests.Add(quest);
                EditorUtility.SetDirty(quest); return quest;
            }
            Quest("Goryeo_SongakTrade", "goryeo_songak_trade", "산록에서 고른 약재", "goryeo_songak_merchant",
                "이곳 장터에서 인삼 두 뿌리를 사서 개경에 팔아 보세요. 적은 짐으로 시작할 수 있지만, 많이 사면 매입가가 오릅니다. 닥목으로 향하는 산록 길도 둘러보세요.", first,
                Trade(ObjectiveType.Buy, "송악 장터에서 인삼 2개 구매", "goryeo_songak", "ginseng", 2),
                Trade(ObjectiveType.Sell, "개경 장터에서 인삼 2개 판매", "gaegyeong", "ginseng", 2));
            // 11/08 은샘: 앞 장사를 마친 마을(송악)에서 바로 사서 판다. 장단으로 되돌아가지 않게.
            var grain = Quest("Goryeo_JangdanTrade", "goryeo_jangdan_trade", "수도로 가는 곡식", "goryeo_songak_merchant",
                "방금 곡식을 판 이 장터에서 곡식 세 꾸러미를 사서 개경에 팔아 보세요. 수도는 곡식값이 높아 여기보다 비싸게 받습니다. 무게 아홉이라 등짐에도 들어가요.", first,
                Trade(ObjectiveType.Buy, "송악 장터에서 곡식 3개 구매", "goryeo_songak", "grain", 3),
                Trade(ObjectiveType.Sell, "개경 장터에서 곡식 3개 판매", "gaegyeong", "grain", 3));
            // 11/08 은샘: 곡식을 판 개경에서 바로 땔감을 사고, 등짐 한 번에 드는 두 단만 나른다(네 단은 등짐에 안 들어가 납품이 막혔다).
            var supply = Quest("Goryeo_CartSupply", "goryeo_cart_supply", "닥나무 마을에 보내는 땔감", "gaekju",
                "곡식을 잘 팔았군. 이번엔 이 장터에서 땔감 두 단을 사서 닥나무 마을 촌장에게 전해 주게. 종이를 뜨려면 닥나무를 삶을 땔감이 늘 모자라다네. 등짐 한 번에 드는 무게일세.", grain,
                Trade(ObjectiveType.Buy, "개경 장터에서 땔감 2개 구매", "gaegyeong", "firewood", 2),
                new QuestObjective { type = ObjectiveType.Deliver, text = "닥나무 마을 촌장에게 땔감 2개 납품", targetId = "dakmok_chief", item = Item("firewood"), count = 2,
                    completeLine = "종이를 뜨려면 닥나무를 삶아야 하지. 땔감이 제때 와서 살았네." });
            supply.rewardMoney = 20; // 실제 적재 용량이 성장 조건. 기존 손수레 퀘스트 완료를 강제하지 않는다.
            var oldFreight = db.quests.Find(q => q != null && q.id == "dakmok_firewood");
            if (oldFreight == null) throw new InvalidOperationException("기존 땔감 의뢰 누락");
            supply.choiceGroup = oldFreight.choiceGroup = "goryeo_firewood_intro";
            oldFreight.rewardMoney = 20;
            oldFreight.hasLegacyReward = true; oldFreight.legacyRewardMoney = 60; oldFreight.legacyRewardCredit = 1;
            oldFreight.chain = "수레 운송 · 대안 입문";
            supply.chain = "수레 운송 · 대안 입문";
            oldFreight.summary = "땔감4단 조달을 배우는 일회 운송 의뢰. 장단의 수레 공급 의뢰와 둘 중 하나를 선택합니다.";
            oldFreight.offerLine = "땔감 네 단을 구해 주게. 처음 수레 장사를 익히도록 조달 보수 20냥을 주겠네. 장단에서 맡는 수레 공급 의뢰와는 둘 중 하나만 선택하면 되네.";
            oldFreight.finishNote = "땔감 운송은 익혔습니다. 닥목에서 종이를 사서 장단에 파는 귀환 장사도 선택할 수 있습니다. 매입 자금을 남겨 두세요.";
            supply.finishNote = oldFreight.finishNote;
            EditorUtility.SetDirty(oldFreight);
            var returning = Quest("Goryeo_ReturnTrade", "goryeo_return_trade", "빈 수레로 돌아가지 않기", "dakmok_chief",
                "돌아갈 때 종이 두 묶음을 사서 장단에 팔아 보게. 종이와 배달 짐을 함께 실을지는 자네가 정하게. 물건값은 직접 내야 하니 두 마을 시세를 확인하고, 이익이 나지 않으면 다른 장사를 먼저 해도 되네.", null,
                Trade(ObjectiveType.Buy, "닥목 장터에서 종이 2개 구매", "dakmok", "paper", 2),
                Trade(ObjectiveType.Sell, "장단 장터에서 종이 2개 판매", "goryeo_jangdan", "paper", 2));
            returning.requires.Clear(); returning.requiresAny = new List<QuestData> { oldFreight, supply };
            returning.chain = "수레 운송 · 귀환 장사"; returning.rewardMoney = 6;
            returning.finishNote = "왕복 손익을 비교해 보세요. 같은 방향 배달을 겸하면 보수를 더 받지만, 그만큼 상품을 실을 공간이 줄어듭니다. 창고는 당장 팔지 않을 일반 상품을 현지에 나누어 보관할 때 쓰세요.";
            EditorUtility.SetDirty(returning);
            var goryeo = Town("gaegyeong").region;
            goryeo.deliveryRewards = new DeliveryRewardRules { enabled = true, revision = 1 };
            EditorUtility.SetDirty(goryeo);
            EditorUtility.SetDirty(supply); EditorUtility.SetDirty(db);
            R6RewardUpgrade.Apply(db);
            Q1QuestUpgrade.Apply(db);
            AssetDatabase.SaveAssets();
        }
    }
}
