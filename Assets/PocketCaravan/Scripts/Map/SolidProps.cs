using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 11/16 팀 피드백 "말을 걸 수 없는 NPC·물건 중 일부가 그냥 지나가진다": 맵이 열리면 서 있는 것들의 발밑 칸을 막는다.
    /// 씬 파일은 고치지 않는다 (모든 지역 공용, 맵이 열린 뒤 찾아서 등록).
    /// - 막음: 말을 걸 수 없고 움직이지 않는 사람(CharacterAnimator만 있는 주민), 큰 물건(석등·항아리·빨래·개·닭·좌판·자루 등), 북쪽 장터 사람.
    /// - 그대로: 말을 걸 수 있는 것(Interactable이 알아서 막거나 비킴), 오가는 일꾼·행인·도적·낙타·순록, 구름 그림자·연기·불빛 같은 효과.
    /// - 바닥 깔개(carpet)는 좌판·게시판 앞에 서는 자리라 막지 않는다.
    /// 막는 칸은 프레임마다 한 번 지금 자리로 잰다. 물건이 넓으면 아래 줄 너비만큼 막는다.
    /// 플레이어가 지금 밟고 있는 칸은 막지 않는다 (시간 따라 나타나는 물건·불러온 자리에서 갇히지 않게).
    /// Enabled = false로 끄면 예전처럼 지나간다 (검사·비교용).
    /// </summary>
    public sealed class SolidProps : MonoBehaviour
    {
        public static bool Enabled = true;
        struct Body { public Transform t; public SpriteRenderer sr; public bool wide; }
        static readonly List<Body> bodies = new List<Body>();
        static Scene bodiesScene;

        /// <summary>이 칸을 막는 서 있는 것이 있는지 (MapRoot.IsWalkable이 부른다)</summary>
        public static bool Blocks(MapRoot root, Vector3Int c)
        {
            if (!Enabled || root == null || bodies.Count == 0 || root.gameObject.scene != bodiesScene) return false;
            // 길찾기는 한 번에 수만 번 묻는다: 막는 칸은 한 프레임에 한 번만 잰다
            if (cellsFrame != Time.frameCount || cellsRoot != root) Rebuild(root);
            return cells.Contains(c);
        }

        static readonly HashSet<Vector3Int> cells = new HashSet<Vector3Int>();
        static int cellsFrame = -1; static MapRoot cellsRoot;

        static void Rebuild(MapRoot root)
        {
            cells.Clear(); cellsFrame = Time.frameCount; cellsRoot = root;
            for (int i = 0; i < bodies.Count; i++)
            {
                var b = bodies[i];
                if (b.t == null || b.sr == null || !b.sr.enabled || !b.t.gameObject.activeInHierarchy) continue;
                var bounds = b.sr.bounds;
                float y = bounds.min.y + 0.25f;
                if (!b.wide) { cells.Add(root.WorldToCell(new Vector3(bounds.center.x, y, 0f))); continue; }
                var a = root.WorldToCell(new Vector3(bounds.min.x + 0.2f, y, 0f));
                var z = root.WorldToCell(new Vector3(bounds.max.x - 0.2f, y, 0f));
                for (int x = a.x; x <= z.x; x++) cells.Add(new Vector3Int(x, a.y, a.z));
            }
            // 플레이어가 지금 밟고 있는 칸은 막지 않는다 (시간 따라 나타나는 개·빨래, 불러온 자리 위에 선 물건에 갇히지 않게)
            var pc = PlayerController.Instance; var m = pc != null ? pc.Motor : null;
            if (m != null && cells.Count > 0)
            {
                Vector2 f = (Vector2)m.transform.position + m.feetOffset, h = m.feetSize * 0.5f;
                for (int sx = -1; sx <= 1; sx += 2)
                    for (int sy = -1; sy <= 1; sy += 2)
                        cells.Remove(root.WorldToCell(new Vector3(f.x + sx * h.x, f.y + sy * h.y, 0f)));
            }
        }

        /// <summary>지금 막고 있는 것 수 (검사용)</summary>
        public static int Count => bodies.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<SolidProps>() != null) return;
            var go = new GameObject("SolidProps"); DontDestroyOnLoad(go); go.AddComponent<SolidProps>();
        }

        void OnEnable() => SceneManager.sceneLoaded += OnLoaded;
        void OnDisable() => SceneManager.sceneLoaded -= OnLoaded;

        void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            bodies.Clear(); bodiesScene = scene; cellsFrame = -1;
            Scan(scene);   // 씬 안에 이미 있는 것은 바로 (저장 자리·도착 자리 고르기 전에)
            StopAllCoroutines();
            StartCoroutine(ScanSoon(scene));
        }

        IEnumerator ScanSoon(Scene scene)
        {
            // 장터·꾸미기 같은 것들은 Start에서 생기므로 조금 기다렸다가 찾고, 늦게 생기는 것을 위해 한 번 더 찾는다
            yield return null; yield return null;
            Scan(scene); cellsFrame = -1;
            yield return new WaitForSecondsRealtime(1.5f);
            Scan(scene); cellsFrame = -1;
        }

        // 막는 물건 이름 (맵 제작 도구가 붙인 이름 그대로)
        static readonly string[] PropNames = { "lamp", "jars", "laundry", "dog", "chicken", "Market Keeper", "Fair Crowd", "Spit Cook" };
        static readonly string[] PropPrefixes = { "Market stall", "prop_" };
        // 움직이거나 효과인 것 (막지 않음)
        static readonly string[] SkipWords = { "carpet", "Carpet", "rug", "Critter", "caravan", "Caravan", "Cloud", "Smoke", "glow", "sand", "돌풍", "모랫길", "Wander", "Patrol" };

        static void Scan(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            var seen = new HashSet<Transform>();
            foreach (var b in bodies) if (b.t != null) seen.Add(b.t);
            foreach (var root in scene.GetRootGameObjects())
            {
                // 사람: CharacterAnimator만 있고 말을 걸 수 없고 스스로 움직이지 않는 것
                foreach (var ca in root.GetComponentsInChildren<CharacterAnimator>(false))
                {
                    var t = ca.transform;
                    if (seen.Contains(t) || Skip(t.name) || !OnlyIdle(ca.gameObject)) continue;
                    var sr = ca.GetComponent<SpriteRenderer>();
                    if (sr == null) continue;
                    bodies.Add(new Body { t = t, sr = sr, wide = false }); seen.Add(t);
                }
                // 물건: 이름으로 고른다
                foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(false))
                {
                    var t = sr.transform;
                    if (seen.Contains(t) || sr.sprite == null || !PropName(t.name) || Skip(t.name)) continue;
                    if (t.GetComponentInParent<Interactable>() != null || t.GetComponentInParent<CharacterAnimator>() != null) continue;
                    bodies.Add(new Body { t = t, sr = sr, wide = sr.sprite.rect.width > 24f }); seen.Add(t);
                }
            }
        }

        static bool PropName(string n)
        {
            foreach (var p in PropNames) if (n == p) return true;
            foreach (var p in PropPrefixes) if (n.StartsWith(p)) return true;
            return false;
        }

        static bool Skip(string n)
        {
            foreach (var w in SkipWords) if (n.Contains(w)) return true;
            return false;
        }

        /// <summary>그림 넘기기·앞뒤 맞추기 말고 다른 동작(말 걸기·걷기·순찰 등)이 붙어 있지 않은 사람</summary>
        static bool OnlyIdle(GameObject go)
        {
            foreach (var c in go.GetComponents<MonoBehaviour>())
            {
                if (c == null) continue;
                if (c is CharacterAnimator || c is DepthActor) continue;
                return false;
            }
            return go.GetComponentInParent<PlayerController>() == null;
        }
    }
}
