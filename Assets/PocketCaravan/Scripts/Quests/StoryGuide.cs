using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 다음 이야기 길잡이 + 퀘스트 동시 진행 제한 (전 지역 공용, 2026-11-04 통합: 북유럽 NorthGuide·NorthQuestLimit + 베네치아 VeniceDaily 길잡이).
    /// [길잡이] 지금 지역의 퀘스트를 하나도 진행하지 않을 때, 다음에 받을 수 있는 그 지역 퀘스트를 누가 주는지 한 줄.
    ///   11/05 B안: 화면 위 가운데 알약 ("! 집 보는 남편을 만나기 · 비르카 서쪽 첫 집 앞"). 할 일 줄이 있으면 그 아래.
    ///   시작 튜토리얼 중에는 튜토리얼 안내가 맡는다.
    /// [동시 제한] 한 지역의 사람이 주는 퀘스트는 한 번에 3개까지. 북유럽 마을 곁가지("북쪽 마을 이야기")는 그중 2개까지.
    ///   사람이 주지 않는 자동 퀘스트는 세지만 막지 않는다.
    /// 퀘스트의 지역은 주는 사람이 있는 맵으로 정한다 (WorldIndex). 공용 QuestManager는 읽기만 하고 ExtraAcceptBlocker 자리에 조건만 더한다.
    /// </summary>
    public class StoryGuide : MonoBehaviour
    {
        public const int MaxPerRegion = 3, MaxNorthSide = 2;
        const string NorthSideChain = "북쪽 마을 이야기";

        // 퀘스트 주는 사람 id → (이름, 어디 있는지). 어디는 비우면 맵 이름으로.
        static readonly Dictionary<string, (string name, string where)> Givers = new Dictionary<string, (string, string)>
        {
            // 북유럽
            { "north_husband", ("집 보는 남편", "비르카 서쪽 첫 집 앞 (광장에서 왼쪽 위로)") },
            { "north_wife", ("들일 하는 아내", "모피 사냥꾼 마을 남쪽 건초 들판") },
            { "north_innkeeper", ("여관 할머니", "비르카 서쪽 여관 앞 (광장 왼쪽 위)") },
            { "north_bold", ("욕심쟁이 볼드", "비르카 광장 왼쪽") },
            { "north_har", ("목수 하르", "비르카 남서쪽, 장터 거리 왼쪽 끝 집 앞") },
            { "north_warehouse", ("창고지기", "피오르 나루 장터에서 오른쪽(동쪽) 부두 끝, 선장 옆") },
            { "north_goats", ("순록지기 맏형", "모피 사냥꾼 마을 게시판 왼쪽") },
            { "north_tora", ("무두장이 토라", "모피 사냥꾼 마을 북서쪽 천막 앞 (나무 다리 건너)") },
            { "north_captain", ("선장", "피오르 나루 장터에서 오른쪽(동쪽) 부두 끝, 창고지기 옆") },
            { "north_herbalist", ("약초꾼", "라우가르 온천 마을 장터 아래") },
            { "north_bathkeeper", ("목욕 회관 주인", "라우가르 온천 마을 목욕 회관 앞") },
            { "north_herd_granny", ("순록 목동 할머니", "순록 고원 마을 남쪽 모닥불 옆") },
            { "north_sledmaker", ("썰매 장인", "순록 고원 마을 남쪽, 목동 할머니 옆") },
            { "north_runekeeper", ("룬 지기 노인", "룬돌 언덕 고리 앞") },
            { "north_miner", ("광부 할아범", "얼음 동굴 골짜기 남쪽 동굴 입구") },
            // 베네치아
            { "venice_innkeeper", ("여관 주인", null) }, { "venice_clerk", ("상인회 서기", null) }, { "venice_gardener", ("과수원 일꾼", null) },
            { "venice_glassmaker", ("유리 장인", null) }, { "venice_fisher", ("부라노 어부", null) }, { "venice_warehouse", ("창고지기", null) },
            // 고려
            { "naru_chief", ("나루 마을 촌장", null) }, { "gaekju", ("개경 객주 돈돌", null) }, { "dakmok_chief", ("닥나무 마을 촌장", null) },
            { "goryeo_songak_merchant", ("산록 약재 장수", null) }, { "goryeo_jangdan_farmer", ("장단 곡물 상인", null) }, { "naru_well_woman", ("나루 우물가 아낙", null) },
            // 아라비아
            { "arabia_hasan", ("길잡이 하산", null) }, { "arabia_maryam", ("여관 주인 마리암", null) }, { "arabia_hindbad", ("짐꾼 힌드바드", null) },
            { "arabia_kitchen_keeper", ("공동 부엌 담당", null) },
        };

        static GameManager GM => GameManager.I;

        /// <summary>퀘스트 주는 사람이 있는 맵으로 지역을 정한다 (사람이 없으면 아이디 앞머리로)</summary>
        public static bool RegionOf(QuestData q, out Reactions.Region region)
        {
            region = Reactions.Region.Goryeo;
            if (q == null || q.id == null) return false;
            var map = !string.IsNullOrEmpty(q.giverNpc) ? WorldIndex.Get()?.MapOf("npc", q.giverNpc) : null;
            if (map != null) { region = Reactions.RegionOfMap(map); return true; }
            if (q.id.StartsWith("north_")) { region = Reactions.Region.North; return true; }
            if (q.id.StartsWith("venice_")) { region = Reactions.Region.Venice; return true; }
            if (q.id.StartsWith("arabia_")) { region = Reactions.Region.Arabia; return true; }
            return false;
        }
        static bool In(QuestData q, Reactions.Region r) => RegionOf(q, out var x) && x == r;

        static bool IntroActive => NorthIntro.Active || VeniceIntro.Active || GoryeoIntro.Active || ArabiaIntro.Active;

        // ---------------- 동시 제한 ----------------
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() { QuestManager.ExtraAcceptBlocker = Check; }

        static string Check(QuestData q)
        {
            if (q == null || string.IsNullOrEmpty(q.giverNpc) || GM == null || !RegionOf(q, out var region)) return null;
            int count = 0, side = 0;
            foreach (var p in GM.Quests.Active)
            {
                if (p == null || !In(p.quest, region)) continue;
                count++;
                if (p.quest.chain == NorthSideChain) side++;
            }
            if (count >= MaxPerRegion)
                return $"지금 맡은 일이 {count}개나 되는구먼. 하던 일부터 끝내고 오게.\n(한 지역 의뢰는 한 번에 {MaxPerRegion}개까지)";
            if (q.chain == NorthSideChain && side >= MaxNorthSide)
                return $"마을 부탁을 벌써 {side}개나 들고 다니는군. 하나 끝내고 다시 들르게.\n(마을 곁가지는 한 번에 {MaxNorthSide}개까지)";
            return null;
        }

        // ---------------- 길잡이 ----------------
        static StoryGuide instance;
        string who, where;
        float nextCheck;

        /// <summary>다음 이야기 줄이 떠 있는지 (알림이 그 아래로 비킨다)</summary>
        public static bool Showing => Ready && !HUD.Expanded;

        static bool Ready => instance != null && instance.isActiveAndEnabled && !string.IsNullOrEmpty(instance.who)
            && !UIState.Blocking && GM != null && !GM.IsTravelling;

        /// <summary>Tab으로 펼쳤을 때 오른쪽 할 일 카드에 넣을 다음 이야기 ("여관 주인을 만나기", "리알토"). 없으면 null</summary>
        public static (string action, string where)? Next => Ready ? (instance.who + CuteUI.Eul(instance.who) + " 만나기", instance.where) : ((string, string)?)null;

        void Awake() => instance = this;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<StoryGuide>() != null) return;
            var go = new GameObject("StoryGuide");
            DontDestroyOnLoad(go);
            go.AddComponent<StoryGuide>();
        }

        void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 0.5f;
            who = where = null;
            if (GM == null || GM.Quests == null || MapRoot.Current == null || IntroActive) return;
            var here = Reactions.Here;
            foreach (var p in GM.Quests.Active) if (p != null && In(p.quest, here)) return;   // 진행 중이면 공용 퀘스트 안내가 맡는다
            foreach (var q in GM.Db.quests)
            {
                if (q == null || q.retired || string.IsNullOrEmpty(q.giverNpc) || !In(q, here) || !GM.Quests.CanStart(q)) continue;
                string name = q.giverNpc, place = null;
                if (Givers.TryGetValue(q.giverNpc, out var g)) { name = g.name; place = g.where; }
                if (string.IsNullOrEmpty(place)) { var map = WorldIndex.Get()?.MapOf("npc", q.giverNpc); if (map != null) place = WorldIndex.Get().MapName(map); }
                who = name; where = place;
                return;
            }
        }

        void OnGUI()
        {
            if (!Showing) return;
            UIStyle.Begin();
            GUI.depth = -5;   // 머리 위 이름표보다 앞에
            CuteUI.TopPill(6 + (QuestTracker.LineVisible ? 24 : 0), true, null, who + CuteUI.Eul(who) + " 만나기", where);
        }
    }
}
