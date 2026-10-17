using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 북유럽 1장 퀘스트 중 공용 퀘스트 기능만으로 안 되는 연출을 맡는다 (공용 코드는 고치지 않음).
    /// - Q3: 나루 맵의 마을 사람 "톰테 할아버지"를 정직 시험 NPC(NorthTomte)로 바꿔 끼움
    /// - Q4: Q2를 마치고 게임 시간 8시간 뒤 까마귀 무니가 눈보라 소식을 전하고 퀘스트를 자동 수락. 사냥꾼 마을 꿀술이 바닥남 (재고가 시간이 지나며 회복되므로 빨리 갈수록 이득)
    /// - Q6: 볼드의 싹쓸이 퀘스트 중 나루에 처음 들어가면 볼드가 모피를 쏟아부어 값이 떨어짐
    /// 북유럽 퀘스트가 GameDatabase에 없으면 아무것도 하지 않는다.
    /// </summary>
    public class NorthQuestDirector : MonoBehaviour
    {
        const float StormDelayHours = 8f;
        float q2DoneAt = -1f;
        bool furDumped;
        string lastMap;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameManager.I == null || FindQuest("north_q2_soup") == null) return;
            if (FindFirstObjectByType<NorthQuestDirector>() != null) return;
            var go = new GameObject("NorthQuestDirector");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthQuestDirector>();
        }

        static QuestData FindQuest(string id) => GameManager.I.Db.quests.Find(q => q != null && q.id == id);
        static TownData Town(string id) => GameManager.I.Db.towns.Find(t => t != null && t.id == id);
        static ItemData Item(string id) => GameManager.I.ItemById(id);

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.IsTravelling) return;
            string map = MapRoot.Current != null ? MapRoot.Current.mapId : null;
            if (map != lastMap) { lastMap = map; OnMapChanged(map); }
            CheckStorm();
        }

        void OnMapChanged(string map)
        {
            if (map == "north_naru")
            {
                SwapTomte();
                DumpFurIfRival();
            }
        }

        // ---------- Q3 ----------
        void SwapTomte()
        {
            foreach (var t in FindObjectsByType<TalkNpc>(FindObjectsSortMode.None))
            {
                if (t.displayName != NorthTomte.DisplayLabel) continue;
                var go = t.gameObject;
                bool block = t.blocksMovement;
                Destroy(t);
                var tomte = go.AddComponent<NorthTomte>();
                tomte.blocksMovement = block;
            }
        }

        // ---------- Q4 ----------
        void CheckStorm()
        {
            var gm = GameManager.I;
            var q2 = FindQuest("north_q2_soup");
            var q4 = FindQuest("north_q4_storm");
            if (q2 == null || q4 == null || !gm.Quests.IsFinished(q2) || !gm.Quests.CanStart(q4)) return;
            if (q2DoneAt < 0f) { q2DoneAt = gm.Clock.TotalHours; return; }
            if (gm.Clock.TotalHours - q2DoneAt < StormDelayHours) return;

            Notify.Show("까마귀 무니: 까악! 사냥꾼 마을에 눈보라가 몰아쳐서 꿀술이 똑 떨어졌대요! 눈이 그치면 다른 상인들이 몰려올 거예요, 서둘러요!", 7f);
            Sfx.Play("accept");
            var hunter = Town("north_hunter");
            var mead = Item("north_mead");
            if (hunter != null && mead != null)
            {
                var m = gm.GetMarket(hunter);
                for (int i = 0; i < 50 && m.Stock(mead) > 1f; i++) m.RecordBuy(mead, 0);   // 재고를 바닥으로: 값이 뜀
            }
            gm.Quests.Accept(q4);
        }

        // ---------- Q6 ----------
        void DumpFurIfRival()
        {
            var gm = GameManager.I;
            var q6 = FindQuest("north_q6_bold");
            if (furDumped || q6 == null) return;
            bool active = false;
            foreach (var p in gm.Quests.Active) if (p.quest == q6) active = true;
            if (!active) return;
            var naru = Town("north_naru");
            var fur = Item("north_fur");
            if (naru == null || fur == null) return;
            var m = gm.GetMarket(naru);
            for (int i = 0; i < 12; i++) m.RecordNpcSell(fur, 0);
            furDumped = true;
            Notify.Show("볼드: 하하하! 나루 장에는 내가 먼저 왔지! 모피를 몽땅 풀었다고! (나루 모피 값이 뚝 떨어졌다)", 7f);
        }
    }
}
