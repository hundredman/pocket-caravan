using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 아라비아 그늘 자리에 앉기 (2026-11-07 기우). 북 모닥불·베 벤치·고려 평상처럼 지역마다 한 벌.
    /// 마을 여관·우물·오아시스 옆 양탄자 위에서 Space: 앉은 자세(고려 평상 그림 Goryeo/Seats/Art/{출신})로 바뀌고 체력이 두 배로 찬다.
    /// 4초쯤 쉬면 "더위 식힘"(1시간 동안 달릴 때 체력 덜 닳음, StaminaMods "arabia_shade" ×0.75). 방향키나 Space로 일어난다.
    /// 자리는 맵마다 코드에 적어 두고 씬을 불러올 때 세운다(씬 파일은 안 건드림). 저장: 공용 메모 "arabia:shade_until".
    /// </summary>
    [DefaultExecutionOrder(1001)]
    public class ArabiaSeat : Interactable
    {
        public override string DisplayName => "그늘에 앉기";
        public override Kind Category => Kind.Market;

        /// <summary>맵 → 자리 칸 (여관·우물·오아시스 옆 빈 땅)</summary>
        static readonly Dictionary<string, Vector2Int[]> Places = new Dictionary<string, Vector2Int[]>
        {
            // 좌표는 Maps/*.json 기준(위가 y=0). 마을: 여관·우물 옆 / 오아시스: 물가 남쪽 가장자리 (물 타일 묶음마다 하나, 11/07 스크립트로 뽑음)
            { "arabia_ahsa",   new[] { new Vector2Int(34, 23), new Vector2Int(31, 10) } },
            { "arabia_zabid",  new[] { new Vector2Int(20, 13), new Vector2Int(15, 36), new Vector2Int(41, 36) } },
            { "arabia_taiz",   new[] { new Vector2Int(46, 19) } },
            { "arabia_jeddah", new[] { new Vector2Int(24, 13) } },
            { "arabia_sanaa",  new[] { new Vector2Int(48, 32) } },
            { "arabia_central_dunes",  new[] { new Vector2Int(106, 33), new Vector2Int(45, 37), new Vector2Int(111, 31) } },
            { "arabia_central_bazaar", new[] { new Vector2Int(18, 13) } },
            { "arabia_central_steppe", new[] { new Vector2Int(27, 36), new Vector2Int(50, 32), new Vector2Int(79, 36) } },
            { "arabia_shortcut",       new[] { new Vector2Int(47, 36) } },
        };
        const string ShadeKey = "arabia:shade_until";
        const float Lift = 0.3f;

        static ArabiaSeat sitting;
        float since; bool rewarded;
        Vector3 rest; bool applied; Sprite pose;

        public static bool IsSitting => sitting != null;
        public static bool Shaded => GameManager.I != null && GameManager.I.Clock.TotalHours < GameManager.I.GetMemo(ShadeKey, -1f);

        void Awake() => blocksMovement = false;

        public override void Interact(PlayerController player)
        {
            if (sitting == this) { Stand(); return; }
            sitting = this;
            player.Motor.Teleport(transform.position, Vector2Int.down);
            var origin = GameManager.I != null && GameManager.I.Player != null ? GameManager.I.Player.origin : null;
            pose = origin != null ? Resources.Load<Sprite>("Goryeo/Seats/Art/" + origin.id) : null;
            since = Time.time; rewarded = false;
            Sfx.Play("step", .6f);
            Notify.Show("그늘에 앉았다. 방향키나 Space로 일어난다.", 2f);
        }

        void Stand() { Undo(); sitting = null; }

        void Update()
        {
            if (sitting != this) return;
            var pc = PlayerController.Instance;
            Undo();
            if (pc == null || GameManager.I == null || (!UIState.Blocking && (InputReader.MoveVector().sqrMagnitude > .01f || InputReader.InteractPressed())))
            {
                sitting = null;
                return;
            }
            var st = GameManager.I.Player;
            st.stamina = Mathf.Min(1f, st.stamina + pc.staminaRegen * 2f * Time.deltaTime);
            if (!rewarded && Time.time - since > 4f)
            {
                rewarded = true;
                GameManager.I.SetMemo(ShadeKey, GameManager.I.Clock.TotalHours + 1f);
                Notify.Show("더위를 식혔다. 1시간 동안 덜 지친다.", 3f);
            }
        }

        void LateUpdate()
        {
            if (sitting != this) return;
            var pc = PlayerController.Instance;
            if (pc == null || pc.anim == null) return;
            var t = pc.anim.transform;
            rest = t.localPosition; applied = true;
            t.localPosition = rest + new Vector3(0f, Lift, 0f);
            pc.anim.facing = Vector2Int.down;
            var sr = pc.anim.GetComponent<SpriteRenderer>();
            if (sr == null) return;
            if (pose != null) sr.sprite = pose;
            else if (pc.anim.frames != null && pc.anim.frames.Length > 0 && pc.anim.frames[0] != null) sr.sprite = pc.anim.frames[0];
        }

        void Undo()
        {
            if (!applied) return;
            var pc = PlayerController.Instance;
            if (pc != null && pc.anim != null) pc.anim.transform.localPosition = rest;
            applied = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Undo();
            if (sitting == this) sitting = null;
        }

        // ---------------- 자리 세우기 + 버프 유지 ----------------
        class Runner : MonoBehaviour
        {
            void OnEnable() { SceneManager.sceneLoaded += OnLoaded; Place(); }   // 처음 뜬 맵에도 바로
            void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; StaminaMods.Clear("arabia_shade"); }
            void Update()
            {
                var map = MapRoot.Current != null ? MapRoot.Current.mapId : null;
                StaminaMods.Set("arabia_shade", map != null && map.StartsWith("arabia_") && Shaded ? .75f : 1f);
            }
            void OnLoaded(Scene s, LoadSceneMode m) => Place();
            void Place()
            {
                var mr = MapRoot.Current;
                if (mr == null || mr.previewOnly || string.IsNullOrEmpty(mr.mapId) || !Places.TryGetValue(mr.mapId, out var cells)) return;
                var rug = Resources.Load<Sprite>("Arabia/Fair/prop_carpet");
                var road = mr.GetComponent<RoadGrid>();
                if (road == null || road.height <= 0) return;
                int h = road.height;
                foreach (var c in cells)
                {
                    var cell = new Vector3Int(c.x, h - 1 - c.y, 0);   // Places는 맵 JSON 좌표(위가 0). 씬은 MapJsonBuilder처럼 위아래 뒤집힘
                    if (!mr.IsWalkable(cell) || Interactable.At(cell) != null) { Debug.LogWarning($"[아라비아 그늘 자리] {mr.mapId} ({c.x},{c.y}) 자리가 막혀 있다"); continue; }
                    var go = new GameObject("그늘 자리");
                    go.transform.position = mr.CellCenter(cell);
                    go.AddComponent<ArabiaSeat>();
                    if (rug != null) { var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = rug; sr.sortingOrder = 5; }
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindFirstObjectByType<Runner>() != null) return;
            var go = new GameObject("ArabiaSeat");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
        }
    }
}
