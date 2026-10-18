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
                quest.requires = new List<QuestData> { prerequisite };
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
            var grain = Quest("Goryeo_JangdanTrade", "goryeo_jangdan_trade", "들마을의 곡식", "goryeo_jangdan_farmer",
                "장단 장터에서 곡식 세 꾸러미를 사서 개경에 팔아 보세요. 무게 아홉이라 등짐에도 들어갑니다. 올 때 종이를 실어 오면 귀환 장사도 할 수 있어요.", first,
                Trade(ObjectiveType.Buy, "장단 장터에서 곡식 3개 구매", "goryeo_jangdan", "grain", 3),
                Trade(ObjectiveType.Sell, "개경 장터에서 곡식 3개 판매", "gaegyeong", "grain", 3));
            var supply = Quest("Goryeo_CartSupply", "goryeo_cart_supply", "수레에 싣는 땔감", "goryeo_jangdan_farmer",
                "장단에서 땔감 네 단을 사서 닥목 촌장에게 전해 주세요. 무게 열여섯이니 손수레에 싣는 편이 좋습니다. 돌아올 때 종이 가격도 비교해 보세요.", grain,
                Trade(ObjectiveType.Buy, "장단 장터에서 땔감 4개 구매", "goryeo_jangdan", "firewood", 4),
                new QuestObjective { type = ObjectiveType.Deliver, text = "닥목 촌장에게 땔감 4개 납품", targetId = "dakmok_chief", item = Item("firewood"), count = 4,
                    completeLine = "종이를 뜨려면 닥나무를 삶아야 하지. 이 짐은 등짐 한 번으로 옮기기 어려웠을 게야." });
            supply.rewardMoney = 20; // 실제 적재 용량이 성장 조건. 기존 손수레 퀘스트 완료를 강제하지 않는다.
            EditorUtility.SetDirty(supply); EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
        }
    }
}
