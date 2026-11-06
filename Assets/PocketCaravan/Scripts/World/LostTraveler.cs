using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 날씨 사건: 길 잃은 사람 데려다주기 (전 지역 공용, 2026-11-05 통합: 북유럽 NorthEvents 길 잃은 상인 + 베네치아 VeniceEvents 길 잃은 순례자).
    /// 궂은 날씨(Weather[지역])인 4시간 블록마다 한 번, 장터 없는 길 맵에 길 잃은 사람이 나타난다.
    /// "따라오세요" → 맵을 넘어서도 뒤따라오고, 장터 있는 마을에 함께 들어가면 사례 40냥·신용 +1·지역 선물 1개(실을 수 있으면).
    /// 동행 중에 그 지역을 벗어나면 거기서 헤어진다.
    /// - 날씨: 북(함박눈·눈보라)·베(안개·비)는 자기 날씨를 등록. 아라비아는 블록 규칙(세 블록에 하나 "모래바람 때") + 화면 모래바람 연출.
    ///   고려는 아직 날씨가 없어 블록 규칙만 쓴다(네 블록에 하나 "궂은 때"). 은샘이 날씨를 넣으면 Weather[Goryeo]에 한 줄 등록.
    /// - 자리: 북(NorthData "events")·베(VeniceLife "lost")는 자기 자리를 등록. 기본은 길 타일 중 맵 가운데에 가장 가까운 칸.
    /// - 지역 고유 선택지(북 꿀술 나눠 주기 / 베 물 한 모금)는 Extras에 등록.
    /// 저장: 공용 메모 "lost:met:블록"(이 블록에 이미 만남), "lost:follow"(동행 중, 값 = 지역 번호 + 1).
    /// 통합 전 북 "north:lost:블록", 베 "venice:lost:날:블록"은 블록 하나짜리라 이어받지 않는다.
    /// </summary>
    public static class LostTraveler
    {
        public const int Reward = 40;

        public class Profile
        {
            public string name, sprite, gift;      // 이름표, Resources 그림, 사례 선물 물건 id
            public string ask, follow, arrive;     // 말 걸 때 / 동행 중 / 도착 대사
        }

        public static readonly Dictionary<Reactions.Region, Profile> Profiles = new Dictionary<Reactions.Region, Profile>
        {
            { Reactions.Region.North, new Profile { name = "눈보라에 길 잃은 상인", sprite = "North/Minigame/merchant_d0", gift = "north_fur",
                ask = "으으, 추워… 눈보라에 길을 잃었어요. 가까운 마을까지만 데려다줄 수 있나요? 사례는 꼭 할게요.",
                follow = "장터 불빛이 보이는 마을까지만 부탁해요. 이 눈보라에선 한 치 앞도 안 보여서…",
                arrive = "살았다! 마을 불빛이에요. 정말 고마워요, 이 은혜는 장부에 꼭 적어 둘게요." } },
            { Reactions.Region.Venice, new Profile { name = "길 잃은 순례자", sprite = "Venice/Life/pilgrim", gift = "venice_wine",
                ask = "안개 때문에 길을 잃었소. 장터 있는 마을까지만 데려다주겠소?",
                follow = "종소리가 들리는 마을까지만… 이 안개엔 다리도 안 보이오.",
                arrive = "종탑이 보이는군! 고맙소. 이 은혜는 성인께 전하겠소." } },
            { Reactions.Region.Goryeo, new Profile { name = "길 잃은 보부상", sprite = "Common/traveler", gift = "ginseng",
                ask = "궂은 날에 고개를 잘못 들었나 봅니다. 장터 있는 마을까지만 같이 가 주시겠소? 사례는 꼭 하리다.",
                follow = "장터 북소리 나는 데까지만… 이 날씨엔 이정표도 안 보이오.",
                arrive = "객주 등불이오! 고맙소. 이 은혜는 장부에 적어 두겠소." } },
            { Reactions.Region.Arabia, new Profile { name = "대상에서 떨어진 길손", sprite = "Common/traveler", gift = "arabia_dates",
                ask = "모래바람에 대상을 놓쳤소. 장터 있는 마을까지만 데려다주겠소? 사례는 꼭 하리다.",
                follow = "첨탑이 보이는 데까지만… 모래바람에 별도 안 보이오.",
                arrive = "첨탑이다! 고맙소. 알라께서 그대의 길을 지키시길." } },
        };

        /// <summary>지금 궂은 날씨인가. 없으면 블록 규칙(아라비아 3블록에 1, 그 외 4블록에 1).</summary>
        public static readonly Dictionary<Reactions.Region, Func<bool>> Weather = new Dictionary<Reactions.Region, Func<bool>>();
        /// <summary>이 맵에서 길 잃은 사람이 설 자리. 없으면 기본(길 타일 중 맵 가운데에 가장 가까운 칸). null을 돌려주면 기본.</summary>
        public static readonly Dictionary<Reactions.Region, Func<string, Vector3?>> Place = new Dictionary<Reactions.Region, Func<string, Vector3?>>();
        /// <summary>지역 고유 선택지. 선택지 안에서 끝내려면 LostTraveler.Dismiss(spot)을 부른다.</summary>
        public static readonly Dictionary<Reactions.Region, Func<Spot, List<DialogueUI.Choice>>> Extras = new Dictionary<Reactions.Region, Func<Spot, List<DialogueUI.Choice>>>();

        static int Block => GameManager.I != null ? Mathf.FloorToInt(GameManager.I.Clock.TotalHours / 4f) : 0;
        static string MetKey => "lost:met:" + Block;
        public static bool MetThisBlock => GameManager.I != null && GameManager.I.HasMemo(MetKey);
        public static void Met() { if (GameManager.I != null) GameManager.I.SetMemo(MetKey, 1f); }
        public static bool Following => GameManager.I != null && GameManager.I.GetMemo("lost:follow", 0f) > 0f;
        static Reactions.Region FollowRegion => (Reactions.Region)(Mathf.RoundToInt(GameManager.I.GetMemo("lost:follow", 0f)) - 1);

        public static bool BadWeather(Reactions.Region r)
        {
            if (Weather.TryGetValue(r, out var f) && f != null) return f();
            return r == Reactions.Region.Arabia ? Block % 3 == 1 : Block % 4 == 1;
        }

        static Profile Of(Reactions.Region r) => Profiles.TryGetValue(r, out var p) ? p : Profiles[Reactions.Region.Goryeo];

        static bool IsMarketMap(string map)
        {
            var idx = WorldIndex.Get();
            var info = idx != null ? idx.Map(map) : null;
            return info != null && !string.IsNullOrEmpty(info.town);
        }

        /// <summary>길 타일 중 맵 가운데에 가장 가까운 칸 (길이 없으면 걸을 수 있는 칸 중 가운데)</summary>
        static Vector3? DefaultPlace(MapRoot mr)
        {
            if (mr == null || mr.ground == null) return null;
            var b = mr.ground.cellBounds;
            Vector2 center = new Vector2(b.center.x, b.center.y);
            float best = float.MaxValue; Vector3Int bestCell = default; bool any = false;
            for (int pass = 0; pass < 2 && !any; pass++)
            {
                var tm = pass == 0 ? mr.road : mr.ground;
                if (tm == null) continue;
                foreach (var p in tm.cellBounds.allPositionsWithin)
                {
                    if (!tm.HasTile(p) || !mr.IsWalkable(p) || Interactable.At(p) != null) continue;
                    if (p.x <= b.xMin + 1 || p.x >= b.xMax - 2 || p.y <= b.yMin + 1 || p.y >= b.yMax - 2) continue;
                    float d = (center - new Vector2(p.x, p.y)).sqrMagnitude;
                    if (d < best) { best = d; bestCell = p; any = true; }
                }
            }
            return any ? mr.CellCenter(bestCell) : (Vector3?)null;
        }

        // ---------------- 말 걸기 ----------------

        public static void Open(Spot spot)
        {
            var r = spot.region; var p = Of(r);
            if (Following) { DialogueUI.Show(p.name, p.follow); return; }
            var choices = new List<DialogueUI.Choice> { new DialogueUI.Choice("따라오세요 (마을까지 안내)", () => StartFollow(spot)) };
            if (Extras.TryGetValue(r, out var extra) && extra != null) { var more = extra(spot); if (more != null) choices.AddRange(more); }
            choices.Add(new DialogueUI.Choice("미안해요, 바빠서", null));
            DialogueUI.Show(p.name, p.ask, choices.ToArray());
        }

        static void StartFollow(Spot spot)
        {
            var gm = GameManager.I; var r = spot.region;
            Met();
            gm.SetMemo("lost:follow", (int)r + 1);
            UnityEngine.Object.Destroy(spot.gameObject);
            Runner.I?.MakeFollower(r);
            Notify.Show($"{Of(r).name}이(가) 뒤따라온다. 장터 있는 마을까지 데려다주자.", 4f);
        }

        /// <summary>지역 고유 선택지가 "여기서 헤어짐"으로 끝낼 때</summary>
        public static void Dismiss(Spot spot)
        {
            Met();
            if (spot != null) UnityEngine.Object.Destroy(spot.gameObject);
        }

        static void Arrive(Reactions.Region r)
        {
            var gm = GameManager.I; var p = Of(r);
            gm.SetMemo("lost:follow", 0f);
            gm.Player.money += Reward; gm.Player.credit += 1;
            Sfx.Play("levelup", .7f);
            string extra = "";
            var gift = !string.IsNullOrEmpty(p.gift) ? gm.ItemById(p.gift) : null;
            if (gift != null && gm.Player.CanCarry(gift, 1)) { gm.Player.cargo.Add(gift, 1, 0f); extra = $" · {gift.displayName} 1개"; }
            DialogueUI.Show(p.name, $"{p.arrive}\n(사례 {Reward}냥 · 신용 +1{extra})");
        }

        static void Farewell(Reactions.Region r)
        {
            GameManager.I.SetMemo("lost:follow", 0f);
            Notify.Show($"{Of(r).name}: \"여기서부턴 혼자 가 볼게요. 고마웠어요!\"", 4f);
        }

        public static Sprite LoadSprite(string path)
        {
            var t = Resources.Load<Texture2D>(path);
            if (t == null && path != "Common/traveler") t = Resources.Load<Texture2D>("Common/traveler");
            if (t == null) return null;
            t.filterMode = FilterMode.Point;
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.1f), 16f);
        }

        /// <summary>길에 서 있는 길 잃은 사람</summary>
        public class Spot : Interactable
        {
            public Reactions.Region region;
            public override string DisplayName => Of(region).name;
            public override Kind Category => Kind.Quest;
            public override void Interact(PlayerController player) => Open(this);
            void Update() { transform.localScale = new Vector3(1f + Mathf.Sin(Time.time * 6f) * .02f, 1f, 1f); }   // 덜덜
        }

        // ---------------- 실행 ----------------

        class Runner : MonoBehaviour
        {
            public static Runner I;
            Transform follower;
            void OnEnable() { I = this; SceneManager.sceneLoaded += OnLoaded; }
            void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; if (I == this) I = null; }

            void OnLoaded(Scene s, LoadSceneMode m)
            {
                follower = null;
                var gm = GameManager.I;
                if (gm == null || MapRoot.Current == null) return;
                var r = Reactions.Here;
                string map = MapRoot.Current.mapId;
                if (Following)
                {
                    var fr = FollowRegion;
                    if (fr != r) { Farewell(fr); return; }
                    if (IsMarketMap(map)) { Arrive(r); return; }
                    MakeFollower(r);
                    return;
                }
                if (IsMarketMap(map) || !BadWeather(r) || MetThisBlock) return;
                Vector3? pos = null;
                if (Place.TryGetValue(r, out var f) && f != null) pos = f(map);
                if (pos == null) pos = DefaultPlace(MapRoot.Current);
                if (pos == null) return;
                var go = new GameObject("길 잃은 사람");
                go.transform.position = pos.Value;
                var spot = go.AddComponent<Spot>(); spot.region = r; spot.blocksMovement = false;
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = LoadSprite(Of(r).sprite); sr.sortingOrder = 10;
                if (r == Reactions.Region.Arabia) ArabiaAmbience.Current?.StartGust(14f);   // 모래바람 연출
            }

            public void MakeFollower(Reactions.Region r)
            {
                var pc = PlayerController.Instance;
                var go = new GameObject("따라오는 사람");
                follower = go.transform;
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = LoadSprite(Of(r).sprite); sr.sortingOrder = 10;
                if (pc != null) follower.position = pc.transform.position + new Vector3(-.8f, 0f, 0f);
            }

            void Update()
            {
                var pc = PlayerController.Instance;
                if (follower == null || pc == null) return;
                var target = pc.transform.position + new Vector3(-.8f, 0f, 0f);
                if ((target - follower.position).magnitude > 0.2f)
                    follower.position = Vector3.MoveTowards(follower.position, target, Time.deltaTime * Mathf.Max(3f, (target - follower.position).magnitude * 3f));
                follower.localScale = new Vector3(1f, 1f + Mathf.Abs(Mathf.Sin(Time.time * 8f)) * .04f, 1f);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (UnityEngine.Object.FindFirstObjectByType<Runner>() != null) return;
            var go = new GameObject("LostTraveler");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
        }
    }
}
