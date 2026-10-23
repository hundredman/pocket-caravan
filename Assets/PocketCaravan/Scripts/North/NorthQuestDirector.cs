using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 북유럽 1장 퀘스트 중 공용 퀘스트 기능만으로 안 되는 연출을 맡는다 (공용 코드·저장 형식은 고치지 않음).
    /// 별도 상태를 저장하지 않고, 이미 저장되는 퀘스트 진행·시장 재고만 보고 판단한다.
    /// - Q3: 나루 맵의 마을 사람 "톰테 할아버지"를 정직 시험 NPC(NorthTomte)로 바꿔 끼움 (맵에 들어올 때마다, 상태 없음)
    /// - Q4: Q2를 마친 뒤 자유 구간 퀘스트(Q3·Q5·Q6)를 처음 받으면 까마귀 무니가 눈보라를 알리고 Q4를 자동 수락.
    ///       Q4가 수락되면 저장되므로 다시 켜도 반복되지 않는다. 사냥꾼 마을 꿀술 재고를 비우는 것도 이때 한 번.
    /// - Q6: "모피 5장 사기" 단계를 이번 실행 중에 실제로 끝낸 순간 한 번 나루 장에 모피를 푼다.
    ///       바뀐 재고는 시장 저장에 들어가고, 불러온 게임은 이미 다음 단계라 다시 실행되지 않는다.
    /// - 새 게임(GameManager가 새로 생김)이면 관찰 상태를 초기화한다.
    /// 북유럽 퀘스트가 GameDatabase에 없으면 아무것도 하지 않는다.
    /// </summary>
    public class NorthQuestDirector : MonoBehaviour
    {
        GameManager owner;   // 이 GameManager 기준으로 관찰 중 (새 게임이면 바뀜)
        string lastMap;
        int lastQ6Step = -1; // 이번 실행에서 처음 본 Q6 단계 (-1 = 아직 못 봄)

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
            if (gm == null) return;
            if (gm != owner) { owner = gm; lastMap = null; lastQ6Step = -1; }   // 새 게임·처음 실행
            if (gm.IsTravelling) return;
            string map = MapRoot.Current != null ? MapRoot.Current.mapId : null;
            if (map != lastMap) { lastMap = map; if (map == "north_naru") SwapTomte(); }
            CheckStorm(gm);
            CheckRivalDump(gm);
        }

        static bool Started(QuestManager qm, QuestData q)
        {
            if (q == null) return false;
            if (qm.IsFinished(q)) return true;
            foreach (var p in qm.Active) if (p.quest == q) return true;
            return false;
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
                go.AddComponent<NorthTomte>().blocksMovement = block;
            }
        }

        // ---------- Q4 ----------
        void CheckStorm(GameManager gm)
        {
            var qm = gm.Quests;
            var q2 = FindQuest("north_q2_soup");
            var q4 = FindQuest("north_q4_storm");
            if (q2 == null || q4 == null || !qm.IsFinished(q2) || !qm.CanStart(q4)) return;
            bool freeStarted = Started(qm, FindQuest("north_q3_tomte")) || Started(qm, FindQuest("north_q5_troll")) || Started(qm, FindQuest("north_q6_bold"));
            if (!freeStarted) return;

            Notify.Show("까마귀 무니: 까악! 사냥꾼 마을에 눈보라가 몰아쳐서 꿀술이 똑 떨어졌대요! 눈이 그치면 다른 상인들이 몰려올 거예요, 서둘러요!", 7f);
            var hunter = Town("north_hunter");
            var mead = Item("north_mead");
            if (hunter != null && mead != null)
            {
                var m = gm.GetMarket(hunter);
                for (int i = 0; i < 50 && m.Stock(mead) > 1f; i++) m.RecordBuy(mead, 0);   // 재고를 바닥으로: 값이 뜀 (시간이 지나면 회복)
            }
            qm.Accept(q4);
        }

        // ---------- Q6 ----------
        void CheckRivalDump(GameManager gm)
        {
            var q6 = FindQuest("north_q6_bold");
            if (q6 == null) return;
            int step = -1;
            foreach (var p in gm.Quests.Active) if (p.quest == q6) step = p.step;
            if (step < 0) { if (lastQ6Step >= 0) lastQ6Step = int.MaxValue; return; }   // 진행 중 아님 (끝났으면 다시 안 함)
            if (lastQ6Step == -1) { lastQ6Step = step; return; }                          // 처음 봄: 불러온 상태는 그대로 둔다
            if (lastQ6Step == 0 && step >= 1) DumpFur(gm);                               // 이번 실행에서 "모피 5장 사기"를 끝낸 순간
            lastQ6Step = step;
        }

        static void DumpFur(GameManager gm)
        {
            var naru = Town("north_naru");
            var fur = Item("north_fur");
            if (naru == null || fur == null) return;
            var m = gm.GetMarket(naru);
            for (int i = 0; i < 12; i++) m.RecordNpcSell(fur, 0);
            Notify.Show("볼드: 하하하! 나루 장에는 내가 먼저 가서 모피를 몽땅 풀었지! (나루 모피 값이 뚝 떨어졌다)", 7f);
        }
    }
}
