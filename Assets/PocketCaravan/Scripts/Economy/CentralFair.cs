using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 돌아가며 열리는 큰 장 (전 지역 공용, 2026-11-04 통합: 북유럽 NorthCraft + 베네치아 VeniceFair의 규칙 부분을 합침).
    /// 규칙은 Docs/NorthFair.md 그대로:
    /// 1) 하루에 한 곳 — 게임 날짜 % 마을 수. 기본은 "조각마다 한 곳"(북·서·동·남 조각에 하루 한 곳씩 = 매일 네 곳).
    ///    Mode를 Whole로 바꾸면 "중앙 전체에서 하루 한 곳"(시연·멀티 모임용).
    /// 2) 큰 장 마을 장터에서 팔면 판 값의 2할을 덤으로 (조각별 덤 바꾸기: BonusOverride).
    /// 3) 중앙 조각에 들어서면 하루 한 번 "오늘 큰 장: ○○" 알림, 큰 장 마을에서는 들어설 때마다 + 화면 위 띠.
    /// 좌판·구경꾼 같은 장식은 지역마다 그림이 달라 지역 코드가 IsFairMap을 보고 따로 세운다.
    /// 저장: 공용 메모 "fair:note:조각"(오늘 알림을 했는지). 공용 저장 형식은 그대로.
    /// </summary>
    public static class CentralFair
    {
        public enum Mode { PerChunk, Whole }
        /// <summary>여기 한 줄로 A(조각마다)/B(전체 한 곳)를 바꾼다.</summary>
        public static Mode mode = Mode.PerChunk;
        public const float BaseBonus = 0.2f;

        public struct Town { public string chunk, townId, mapId, name; }

        /// <summary>조각별 순서는 통합 전과 같다 (북유럽 날짜%4, 베네치아 날짜%3 그대로 이어짐).</summary>
        static readonly Town[] Towns =
        {
            new Town { chunk = "north", townId = "north_post",  mapId = "north_c_gatetown", name = "북문 마을" },
            new Town { chunk = "north", townId = "north_cwood", mapId = "north_c_snowwood", name = "눈숲 마을" },
            new Town { chunk = "north", townId = "north_ccreek", mapId = "north_c_creek",   name = "개울 나루 마을" },
            new Town { chunk = "north", townId = "north_cross", mapId = "north_c_crossing", name = "북쪽 길목 마을" },
            new Town { chunk = "west", townId = "venice_c_caravanserai", mapId = "venice_c_post", name = "대상 숙소" },
            new Town { chunk = "west", townId = "venice_c_landing", mapId = "venice_c_landing", name = "본토 나루터" },
            new Town { chunk = "west", townId = "venice_c_vineyard", mapId = "venice_c_vineyard", name = "포도밭 양조장" },
            new Town { chunk = "east", townId = "transit_ferry", mapId = "transit_ferry", name = "버들나루 마을" },
            new Town { chunk = "east", townId = "transit_woodtown", mapId = "transit_woodtown", name = "솔재 길목 마을" },
            // 2026-11-07 아라비아(남쪽 조각): 중앙 길 맵(arabia_central_*)에는 장터 마을이 없어 작은 마을 셋이 돌아가며 선다 (수도 사나·항구 제다는 제외)
            new Town { chunk = "south", townId = "arabia_zabid", mapId = "arabia_zabid", name = "자비드" },
            new Town { chunk = "south", townId = "arabia_ahsa",  mapId = "arabia_ahsa",  name = "알 아흐사" },
            new Town { chunk = "south", townId = "arabia_taiz",  mapId = "arabia_taiz",  name = "타이즈" },
        };

        /// <summary>조각별 덤 비율 바꾸기 (예: 베네치아 사자의 축복이면 서쪽 2할 5푼)</summary>
        public static readonly Dictionary<string, Func<float>> BonusOverride = new Dictionary<string, Func<float>>();

        static int Day => GameManager.I != null ? GameManager.I.Clock.day : 0;
        static int Mod(int a, int n) => ((a % n) + n) % n;

        /// <summary>맵이 속한 중앙 조각 (중앙이 아니면 null)</summary>
        public static string ChunkOfMap(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return null;
            if (mapId.StartsWith("north_c_")) return "north";
            if (mapId.StartsWith("venice_c_")) return "west";
            if (mapId.StartsWith("transit_")) return "east";
            if (mapId.StartsWith("arabia_")) return "south";   // 11/07 아라비아는 지역 전체가 한 조각
            return null;
        }

        /// <summary>오늘 이 조각의 큰 장 마을 (Whole 모드면 조각과 관계없이 오늘의 한 곳)</summary>
        public static Town Today(string chunk)
        {
            if (mode == Mode.Whole) return Towns[Mod(Day, Towns.Length)];
            var list = new List<Town>();
            foreach (var t in Towns) if (t.chunk == chunk) list.Add(t);
            if (list.Count == 0) return Towns[Mod(Day, Towns.Length)];
            return list[Mod(Day, list.Count)];
        }

        public static bool IsFairTown(string townId)
        {
            if (string.IsNullOrEmpty(townId)) return false;
            foreach (var t in Towns) if (t.townId == townId) return Today(t.chunk).townId == townId;
            return false;
        }

        public static bool IsFairMap(string mapId)
        {
            var c = ChunkOfMap(mapId);
            return c != null && Today(c).mapId == mapId;
        }

        public static float Bonus(string chunk) =>
            chunk != null && BonusOverride.TryGetValue(chunk, out var f) && f != null ? f() : BaseBonus;

        static string Pct(float b) => b * 10f % 1f == 0f ? $"{Mathf.RoundToInt(b * 10f)}할" : $"{b * 100f:0}%";

        /// <summary>안내 한 줄 (NPC 대사·알림에 그대로 쓴다)</summary>
        public static string News(string chunk)
        {
            var t = Today(chunk);
            return $"오늘 큰 장: {t.name} (팔면 {Pct(Bonus(t.chunk))} 더)";
        }

        static string ChunkOfTown(string townId)
        {
            foreach (var t in Towns) if (t.townId == townId) return t.chunk;
            return null;
        }

        // ---------------- 실행 부분 ----------------
        class Runner : MonoBehaviour
        {
            string bannerFor;
            void OnEnable() { SceneManager.sceneLoaded += OnLoaded; Hook(); }
            void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; GameEvents.ItemSold -= OnSold; }
            void Hook() { GameEvents.ItemSold -= OnSold; GameEvents.ItemSold += OnSold; }   // 새 게임에서 구독이 지워지므로 씬마다 다시

            void OnLoaded(Scene s, LoadSceneMode m)
            {
                Hook();
                var map = MapRoot.Current != null ? MapRoot.Current.mapId : null;
                var chunk = ChunkOfMap(map);
                bannerFor = IsFairMap(map) ? map : null;
                if (chunk == null || GameManager.I == null) return;
                var t = Today(chunk);
                if (IsFairMap(map)) { Say($"큰 장이 섰다! 오늘 {t.name} 장터에서 팔면 {Pct(Bonus(t.chunk))}을 더 받는다.", 4f); return; }
                string key = "fair:note:" + chunk;
                if (Mathf.RoundToInt(GameManager.I.GetMemo(key, -1f)) == Day) return;
                GameManager.I.SetMemo(key, Day);
                Say(News(chunk), 4f);
            }

            /// <summary>11/11 은샘: 맵에 들어설 때 소식은 한 프레임 뒤에 띄운다. 그때 시작 튜토리얼 중이면 띄우지 않는다 (새로 시작한 첫 화면에 알림이 몰리던 것)</summary>
            void Say(string text, float seconds) => StartCoroutine(SayLater(text, seconds));
            System.Collections.IEnumerator SayLater(string text, float seconds)
            {
                yield return null;
                if (!StoryGuide.IntroActive) Notify.Show(text, seconds);
            }

            void OnSold(ItemData item, TownData town, int price, int profit)
            {
                if (town == null || price <= 0 || GameManager.I == null || !IsFairTown(town.id)) return;
                int bonus = Mathf.Max(1, Mathf.RoundToInt(price * Bonus(ChunkOfTown(town.id))));
                GameManager.I.Player.money += bonus;
                Notify.Show($"큰 장 덤 +{bonus}냥 ({Today(ChunkOfTown(town.id)).name})", 2f);
                Sfx.Play("levelup", .35f);
            }

            void OnGUI()
            {
                if (bannerFor == null || MapRoot.Current == null || MapRoot.Current.mapId != bannerFor || UIState.Blocking) return;
                // 11/13 기우: 위 가운데 알약 대신 할 일 카드의 오늘 소식 (Tab으로 펼쳤을 때. 들어설 때는 아래 알림이 알려 준다)
                TaskCard.News("fair", CuteUI.Pic.Coin, "오늘 큰 장", CuteUI.Plain($"팔면 {Pct(Bonus(ChunkOfMap(bannerFor)))} 덤"));
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (UnityEngine.Object.FindFirstObjectByType<Runner>() != null) return;
            var go = new GameObject("CentralFair");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
        }
    }
}
