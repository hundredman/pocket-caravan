using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace PocketCaravan
{
    /// <summary>
    /// 숨은 수집물 7개 + 전설 (전 지역 공용, 2026-11-05 통합: 북유럽 NorthLegends 룬돌 + 베네치아 VeniceLion 사자 문장).
    /// 지역마다 맵 일곱 곳에 하나씩, 길에서 먼 구석에 숨어 있다. 읽을 때마다 전설 한 조각, 일곱을 다 읽으면 300냥·신용 +2 + 지역 축복(OnComplete).
    /// - 북유럽: 자리·전설·축복 의식(룬돌 고리)은 NorthLegends가 예전 그대로. 여기엔 이름·키만 등록해 수첩 표시·놀거리 알림이 같이 보이게 한다.
    /// - 베네치아: 자리는 VeniceLife가 세우고(VeniceLife.json "lion"), 전설·저장·보상은 공용. 축복(큰 장 덤 2할 5푼)은 OnComplete.
    /// - 고려·아라비아: 맵 일곱 곳(Set.maps)에서 "길에서 가장 먼 걸을 수 있는 칸"을 맵을 열 때 계산해 세운다(초안 전설 포함, 지역 담당이 다듬기).
    /// 저장: 공용 메모 "collect:지역:n" (북 "north:rune:n+1", 베 "venice:lion:n"은 KeyFor로 이어받음).
    /// </summary>
    public static class Collectibles
    {
        public const int Total = 7, Reward = 300;

        public class Set
        {
            public string name, readName, sprite, glow;   // 이름표(안 읽음/읽음), Resources 그림, 밤 빛 그림(없으면 없음)
            public string[] maps, lore;
            public string completeText;
            public bool autoPlace;                        // true면 공용 Runner가 맵을 열 때 세운다
        }

        public static readonly Dictionary<Reactions.Region, Set> Sets = new Dictionary<Reactions.Region, Set>
        {
            { Reactions.Region.Goryeo, new Set {
                name = "이끼 낀 옛 비석", readName = "읽은 옛 비석", sprite = "Common/stele", glow = "Common/glow", autoPlace = true,
                maps = new[] { "Gaegyeong", "Dakmok", "Naru", "goryeo_jangdan", "goryeo_songak", "transit_ferry", "transit_woodtown" },
                lore = new[] {
                    "옛날 벽란도에 큰 배 한 척이 닿았다.\n뱃사람들은 먼 바다 너머에서 향료와 유리를 싣고 왔다고 했다.",
                    "개경의 상인들은 저울을 속이지 않겠다고\n비석 앞에서 맹세했다. 글자는 비바람에 닳았어도 약속은 남았다.",
                    "닥나무 껍질을 두드리던 장인은\n종이 한 장마다 비석의 글귀를 한 줄씩 옮겨 적었다고 한다.",
                    "나루의 사공들은 배를 띄우기 전에\n비석에 물 한 바가지를 끼얹었다. 강이 길을 열어 주기를 빌며.",
                    "장단 들판의 농부들은 첫 곡식을 비석 앞에 놓았다.\n장터에 가져갈 짐이 가벼워지라고.",
                    "송악 산길을 넘던 보부상들도\n고갯마루 비석에 짐을 내려놓고 쉬어 갔다.",
                    "일곱 비석을 다 읽은 상인은\n어느 장터에서든 말 한마디면 믿음을 샀다고 한다.",
                },
                completeText = "일곱 비석을 모두 읽었다! 상금 300냥, 신용 +2, '비석의 믿음'",
            } },
            { Reactions.Region.Arabia, new Set {
                name = "별을 새긴 돌", readName = "읽은 별 돌", sprite = "Common/stele", glow = "Common/glow", autoPlace = true,
                maps = new[] { "arabia_sanaa", "arabia_taiz", "arabia_zabid", "arabia_ahsa", "arabia_jeddah", "arabia_central_dunes", "arabia_central_canyon" },
                lore = new[] {
                    "옛날 사막을 건너던 대상이 길을 잃었다.\n밤하늘의 별 하나가 유난히 밝아 그 별을 따라 사나에 닿았다고 한다.",
                    "대상의 우두머리는 그 별을 돌에 새겼다.\n뒤에 오는 이들이 같은 별을 보고 길을 찾도록.",
                    "타이즈의 향료 장수들은 별 돌 앞에서\n값을 부풀리지 않겠다고 맹세했다.",
                    "자비드의 글방 선생은 별 돌의 글귀를\n아이들에게 첫 글로 가르쳤다.",
                    "알아사의 대추야자 농부들은 첫 열매를\n별 돌 아래 묻었다. 사막이 샘을 마르게 하지 않도록.",
                    "제다의 뱃사람들도 바다로 나가기 전에\n별 돌을 한 번씩 쓰다듬었다.",
                    "일곱 별 돌을 다 읽은 상인은\n어느 대상에 끼어도 맨 앞자리를 내주었다고 한다.",
                },
                completeText = "일곱 별 돌을 모두 읽었다! 상금 300냥, 신용 +2, '별의 길잡이'",
            } },
        };

        /// <summary>저장 키 규칙 (옛 저장 호환). 없으면 "collect:지역:n"</summary>
        public static readonly Dictionary<Reactions.Region, Func<int, string>> KeyFor = new Dictionary<Reactions.Region, Func<int, string>>();
        /// <summary>다 모았을 때 지역이 더 할 일 (베: 사자의 축복)</summary>
        public static readonly Dictionary<Reactions.Region, Action> OnComplete = new Dictionary<Reactions.Region, Action>();

        static string Key(Reactions.Region r, int i) => KeyFor.TryGetValue(r, out var f) && f != null ? f(i) : $"collect:{(int)r}:{i}";
        public static bool Read(Reactions.Region r, int i) => GameManager.I != null && GameManager.I.HasMemo(Key(r, i));
        public static int Found(Reactions.Region r) { int n = 0; for (int i = 0; i < Total; i++) if (Read(r, i)) n++; return n; }
        public static bool Complete(Reactions.Region r) => Found(r) >= Total;
        public static Set Of(Reactions.Region r) => Sets.TryGetValue(r, out var s) ? s : null;

        /// <summary>읽기: 전설 한 조각 + 처음이면 진행, 일곱 번째면 보상</summary>
        public static void Open(Reactions.Region r, int index)
        {
            var set = Of(r); var gm = GameManager.I;
            if (set == null || gm == null) return;
            bool first = !Read(r, index);
            gm.SetMemo(Key(r, index), 1f);
            int found = Found(r);
            string text = set.lore != null && set.lore.Length > 0 ? set.lore[Mathf.Clamp(index, 0, set.lore.Length - 1)] : "";
            DialogueUI.Show(set.name, text + $"\n({set.name} {found} / {Total})");
            if (!first) return;
            Sfx.Play("success");
            string doneKey = Key(r, 99);
            if (found >= Total && !gm.HasMemo(doneKey))
            {
                gm.SetMemo(doneKey, 1f);
                gm.Player.money += Reward; gm.Player.credit += 2;
                Sfx.Play("levelup");
                if (OnComplete.TryGetValue(r, out var more)) more?.Invoke();
                Notify.Show(set.completeText ?? $"일곱 개를 모두 찾았다! 상금 {Reward}냥, 신용 +2", 6f);
            }
            else Notify.Show($"{set.name}을(를) 찾았다 ({found} / {Total})", 3f);
        }

        /// <summary>숨은 물건 자리. 지역 코드가 세우거나(베), autoPlace면 Runner가 세운다.</summary>
        public class Spot : Interactable
        {
            public Reactions.Region region;
            public int index;
            SpriteRenderer glow;
            public override string DisplayName { get { var s = Of(region); return s == null ? "" : Read(region, index) ? s.readName : s.name; } }
            public override Kind Category => Kind.Quest;
            public override void Interact(PlayerController player) => Open(region, index);

            public void Dress(Sprite body, Sprite light)
            {
                if (body != null) { var r = gameObject.AddComponent<SpriteRenderer>(); r.sprite = body; r.sortingOrder = 9; }
                if (light != null)
                {
                    var g = new GameObject("빛"); g.transform.SetParent(transform, false);
                    glow = g.AddComponent<SpriteRenderer>(); glow.sprite = light; glow.sortingOrder = 19;
                }
            }
            void Update()
            {
                if (glow == null) return;
                float night = Mathf.Clamp01((1f - (GameManager.I != null ? GameManager.I.Clock.Daylight : 1f)) * 1.6f);
                bool read = Read(region, index);
                glow.enabled = night > .02f;
                glow.color = new Color(1f, read ? .95f : .85f, read ? .7f : .5f, night * (.45f + Mathf.Sin(Time.time * 2f + index) * .15f));
            }
        }

        public static Sprite LoadSprite(string path, Vector2 pivot)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var t = Resources.Load<Texture2D>(path);
            if (t == null) return null;
            t.filterMode = FilterMode.Point;
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot, 16f);
        }

        /// <summary>길(road 타일)에서 가장 먼, 걸을 수 있는 칸. 길이 없으면 장터/가운데에서 가장 먼 칸. 같으면 해시로.</summary>
        static Vector3Int? FarCorner(MapRoot mr, int seed)
        {
            if (mr == null || mr.ground == null) return null;
            var b = mr.ground.cellBounds;
            var roads = new List<Vector2Int>();
            if (mr.road != null)
                foreach (var p in mr.road.cellBounds.allPositionsWithin) if (mr.road.HasTile(p)) roads.Add(new Vector2Int(p.x, p.y));
            Vector2 center = new Vector2(b.center.x, b.center.y);
            float best = -1f; Vector3Int bestCell = default; bool any = false;
            foreach (var p in b.allPositionsWithin)
            {
                if (!mr.ground.HasTile(p) || !mr.IsWalkable(p) || Interactable.At(p) != null) continue;
                // 맵 가장자리 1칸은 뺀다 (출구 근처)
                if (p.x <= b.xMin + 1 || p.x >= b.xMax - 2 || p.y <= b.yMin + 1 || p.y >= b.yMax - 2) continue;
                float d;
                if (roads.Count > 0)
                {
                    d = float.MaxValue;
                    foreach (var r in roads) { float dd = (r.x - p.x) * (r.x - p.x) + (r.y - p.y) * (r.y - p.y); if (dd < d) d = dd; }
                }
                else d = (center - new Vector2(p.x, p.y)).sqrMagnitude;
                d += ((p.x * 73856093 ^ p.y * 19349663 ^ seed) & 0xff) / 1000f;   // 같은 거리면 해시로 고정
                if (d > best) { best = d; bestCell = p; any = true; }
            }
            return any ? bestCell : (Vector3Int?)null;
        }

        class Runner : MonoBehaviour
        {
            void OnEnable() { SceneManager.sceneLoaded += OnLoaded; }
            void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; }
            void OnLoaded(Scene s, LoadSceneMode m)
            {
                if (GameManager.I == null || MapRoot.Current == null) return;
                var r = Reactions.Here; var set = Of(r);
                if (set == null || !set.autoPlace || set.maps == null) return;
                int idx = Array.IndexOf(set.maps, MapRoot.Current.mapId);
                if (idx < 0) return;
                var cell = FarCorner(MapRoot.Current, idx + 1);
                if (cell == null) { Debug.LogWarning($"[수집물] {MapRoot.Current.mapId}: 숨길 자리가 없다"); return; }
                var go = new GameObject(set.name);
                go.transform.position = MapRoot.Current.CellCenter(cell.Value);
                var sp = go.AddComponent<Spot>(); sp.region = r; sp.index = idx; sp.blocksMovement = true;
                sp.Dress(LoadSprite(set.sprite, new Vector2(0.5f, 0.1f)), LoadSprite(set.glow, new Vector2(0.5f, 0.5f)));
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (UnityEngine.Object.FindFirstObjectByType<Runner>() != null) return;
            var go = new GameObject("Collectibles");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
        }
    }
}
