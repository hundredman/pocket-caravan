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
    /// - 고려(장승, 길가 어귀)·아라비아(별 돌, 길에서 먼 구석): 맵 일곱 곳(Set.maps)에서 자리를 맵을 열 때 계산해 세운다. 일곱 다 읽으면 그 지역 안에서 달릴 때 체력 소모 1할 감소.
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
            public bool roadside;                         // true면 길가(길 옆 칸, 장터에서 먼 어귀)에, false면 길에서 가장 먼 구석에
            public string blessingName;                   // 일곱 다 읽은 뒤 효과 이름 (그 지역 안에서 달릴 때 체력 소모 1할 감소)
            /// <summary>11/08 자동 자리가 UI에 가리는 맵만 손으로 고정 (맵 JSON 좌표, 위가 y=0). 없으면 자동</summary>
            public Dictionary<string, Vector2Int> fixedAt;
        }

        public static readonly Dictionary<Reactions.Region, Set> Sets = new Dictionary<Reactions.Region, Set>
        {
            { Reactions.Region.Goryeo, new Set {
                name = "말 없는 장승", readName = "말 건넨 장승", sprite = "Common/jangseung", glow = null, autoPlace = true, roadside = true,
                blessingName = "장승의 보살핌",
                // 11/08 은샘: 자동 자리가 강 건너 빈 땅(장단)·막힌 칸 사이(나루터)·상태 카드 밑(나루)이라 고정. 장단은 GoryeoBridges가 놓는 다리로 건너 논 어귀
                fixedAt = new Dictionary<string, Vector2Int> { { "goryeo_jangdan", new Vector2Int(50, 7) }, { "transit_ferry", new Vector2Int(49, 43) }, { "Naru", new Vector2Int(26, 9) } },
                maps = new[] { "Gaegyeong", "Dakmok", "Naru", "goryeo_jangdan", "goryeo_songak", "transit_ferry", "transit_woodtown" },
                lore = new[] {
                    "개경 어귀 장승이 말했다.\n\"밤마다 우리 일곱은 서로 말을 건넨다네. 자네 발소리도 벌써 송악까지 갔지.\"",
                    "닥목 장승이 말했다.\n\"닥나무 껍질 두드리는 소리가 멎으면 비가 온다는 뜻이야. 종이 짐은 덮고 가게.\"",
                    "나루 장승이 말했다.\n\"사공이 노를 두 번 두드리면 물살이 세다는 신호지. 세 번이면 오늘은 건너지 말게.\"",
                    "장단 장승이 말했다.\n\"들판 곡식은 첫 수확이 제일 싸고 제일 좋아. 장터보다 밭에서 사는 게 상인의 눈이지.\"",
                    "송악 장승이 말했다.\n\"고개 넘는 길에 짐을 내려놓고 쉬는 이는 끝까지 가고, 서두르는 이는 반에서 돌아오더군.\"",
                    "버들나루 장승이 말했다.\n\"강 건너 사람들은 말이 달라도 저울은 같아. 저울을 속이지 않는 자에게 길이 열리지.\"",
                    "솔재 장승이 말했다.\n\"일곱 장승의 말을 다 들은 상인은 어느 어귀에서든 우리가 지켜 준다네. 잘 가게.\"",
                },
                completeText = "일곱 장승의 말을 모두 들었다! 상금 300냥, 신용 +2, '장승의 보살핌'(고려 안에서 달릴 때 덜 지침)",
            } },
            { Reactions.Region.Arabia, new Set {
                name = "별을 새긴 돌", readName = "읽은 별 돌", sprite = "Common/starstone", glow = "Common/glow", autoPlace = true, roadside = false,
                blessingName = "별의 길잡이",
                maps = new[] { "arabia_sanaa", "arabia_taiz", "arabia_zabid", "arabia_ahsa", "arabia_jeddah", "arabia_central_dunes", "arabia_central_canyon" },
                // 11/08 자동 자리(길에서 가장 먼 칸)가 마을에선 늘 맵 모서리라 상태 카드·Q/M/L 단추·조작 줄에 가렸다 → 가장자리에서 들어온, 길에서 떨어진, 길·출구에서 걸어 갈 수 있는 자리로 고정
                fixedAt = new Dictionary<string, Vector2Int> { { "arabia_sanaa", new Vector2Int(17, 28) }, { "arabia_taiz", new Vector2Int(14, 32) }, { "arabia_zabid", new Vector2Int(21, 11) }, { "arabia_ahsa", new Vector2Int(19, 18) }, { "arabia_jeddah", new Vector2Int(44, 27) }, { "arabia_central_dunes", new Vector2Int(20, 36) }, { "arabia_central_canyon", new Vector2Int(127, 36) } },
                lore = new[] {
                    "옛날 사막을 건너던 대상이 길을 잃었다.\n밤하늘의 별 하나가 유난히 밝아 그 별을 따라 사나에 닿았다고 한다.",
                    "대상의 우두머리는 그 별을 돌에 새겼다.\n뒤에 오는 이들이 같은 별을 보고 길을 찾도록.",
                    "타이즈의 향료 장수들은 별 돌 앞에서\n값을 부풀리지 않겠다고 맹세했다.",
                    "자비드의 글방 선생은 별 돌의 글귀를\n아이들에게 첫 글로 가르쳤다.",
                    "알 아흐사의 대추야자 농부들은 첫 열매를\n별 돌 아래 묻었다. 사막이 샘을 마르게 하지 않도록.",
                    "제다의 뱃사람들도 바다로 나가기 전에\n별 돌을 한 번씩 쓰다듬었다.",
                    "일곱 별 돌을 다 읽은 상인은\n어느 대상에 끼어도 맨 앞자리를 내주었다고 한다.",
                },
                completeText = "일곱 별 돌을 모두 읽었다! 상금 300냥, 신용 +2, '별의 길잡이'(아라비아 안에서 달릴 때 덜 지침)",
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
                if (!string.IsNullOrEmpty(set.blessingName)) StaminaMods.Set("collect:blessing", 0.9f);
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

        /// <summary>
        /// 숨길 칸. roadside=false: 길(road 타일)에서 가장 먼 걸을 수 있는 칸(길이 없으면 가운데서 가장 먼 칸).
        /// roadside=true: 길 옆(길은 아니고 길과 맞닿은) 칸 중 장터에서 가장 먼 칸 = 마을 어귀. 같으면 해시로 고정.
        /// </summary>
        static Vector3Int? FindCell(MapRoot mr, int seed, bool roadside)
        {
            if (mr == null || mr.ground == null) return null;
            var b = mr.ground.cellBounds;
            var roads = new HashSet<Vector2Int>();
            if (mr.road != null)
                foreach (var p in mr.road.cellBounds.allPositionsWithin) if (mr.road.HasTile(p)) roads.Add(new Vector2Int(p.x, p.y));
            var market = UnityEngine.Object.FindAnyObjectByType<TownMarket>();
            Vector2 anchor = new Vector2(b.center.x, b.center.y);
            if (market != null) { var mc = mr.WorldToCell(market.transform.position); anchor = new Vector2(mc.x, mc.y); }
            Vector2 center = new Vector2(b.center.x, b.center.y);
            float best = -1f; Vector3Int bestCell = default; bool any = false;
            foreach (var p in b.allPositionsWithin)
            {
                if (!mr.ground.HasTile(p) || !mr.IsWalkable(p) || Interactable.At(p) != null) continue;
                if (p.x <= b.xMin + 1 || p.x >= b.xMax - 2 || p.y <= b.yMin + 1 || p.y >= b.yMax - 2) continue;   // 가장자리(출구) 1칸은 뺀다
                var q = new Vector2Int(p.x, p.y);
                float d;
                if (roadside && roads.Count > 0)
                {
                    if (roads.Contains(q)) continue;
                    bool adj = roads.Contains(q + Vector2Int.left) || roads.Contains(q + Vector2Int.right) || roads.Contains(q + Vector2Int.up) || roads.Contains(q + Vector2Int.down);
                    if (!adj) continue;
                    d = (anchor - (Vector2)q).sqrMagnitude;
                }
                else if (roads.Count > 0)
                {
                    d = float.MaxValue;
                    foreach (var r in roads) { float dd = (r - q).sqrMagnitude; if (dd < d) d = dd; }
                }
                else d = (center - (Vector2)q).sqrMagnitude;
                d += ((p.x * 73856093 ^ p.y * 19349663 ^ seed) & 0xff) / 1000f;
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
                // 축복: 일곱 다 읽은 지역 안에서 달릴 때 체력 소모 1할 감소 (북·베는 자기 축복이 따로 있어 blessingName이 비어 있음)
                StaminaMods.Set("collect:blessing", set != null && !string.IsNullOrEmpty(set.blessingName) && Complete(r) ? 0.9f : 1f);
                if (set == null || !set.autoPlace || set.maps == null) return;
                int idx = Array.IndexOf(set.maps, MapRoot.Current.mapId);
                if (idx < 0) return;
                Vector3Int? cell = null;
                if (set.fixedAt != null && set.fixedAt.TryGetValue(MapRoot.Current.mapId, out var fx))
                {
                    var rg = MapRoot.Current.GetComponent<RoadGrid>();
                    if (rg != null && rg.height > 0) { var c = new Vector3Int(fx.x, rg.height - 1 - fx.y, 0); if (MapRoot.Current.IsWalkable(c) && Interactable.At(c) == null) cell = c; }
                }
                if (cell == null) cell = FindCell(MapRoot.Current, idx + 1, set.roadside);
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
