using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 10/27 모닥불 옆 통나무 의자 (북유럽·중앙 북쪽 맵의 모든 모닥불).
    /// 맵 생성기가 모닥불 바로 옆 두 칸을 앉는 자리로 남겨 두면(NorthLife.json "seats"), 여기서 통나무 의자를 그리고
    /// Space로 앉는다. 앉으면 불을 바라보고 몸을 낮춘 채 체력이 천천히 차고, 3초쯤 쬐면 1시간 동안 눈보라에 덜 지친다(온천과 같은 "warm").
    /// 방향키·Space·Esc로 일어난다. 이동·저장 형식은 건드리지 않는다 (몸 따뜻함은 NorthSave 표에 이미 있는 "warm" 키).
    /// </summary>
    [DefaultExecutionOrder(1100)]   // NorthSocial(리액션)이 몸 위치를 되돌린 뒤에 앉은 자세를 덧씌운다
    public class NorthFireSeat : MonoBehaviour
    {
        Transform root;
        string builtFor;
        Vector3[] seats = new Vector3[0];
        Vector3[] fires = new Vector3[0];
        int sitting = -1;
        float sitT;
        bool warmed;
        Transform body;
        Vector3 bodyRest;
        Vector3 standPos;            // 앉기 전에 서 있던 자리 (일어날 때 돌아감: 의자 칸이 돌·불에 붙어 있어도 갇히지 않게)
        Vector2Int standFace;
        GUIStyle hint;

        static readonly string[] Murmurs =
        {
            "불꽃이 탁탁 튄다.", "손끝이 녹는다…", "장작 냄새가 좋다.", "눈송이가 불 위에서 사라진다.",
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthFireSeat>() != null) return;
            var go = new GameObject("NorthFireSeat");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthFireSeat>();
        }

        void OnEnable() { SceneManager.sceneLoaded += OnLoaded; }
        void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; }
        void OnLoaded(Scene s, LoadSceneMode m) { builtFor = null; if (sitting >= 0) Stand(false); }

        void Build()
        {
            builtFor = NorthUtil.Map;
            if (root != null) Destroy(root.gameObject);
            seats = new Vector3[0]; fires = new Vector3[0];
            if (!NorthUtil.InNorth || MapRoot.Current == null) return;
            string map = NorthUtil.Map;
            var s = NorthData.Get(map, "seats");
            var f = NorthData.Get(map, "fires");
            if (s.Length < 2) return;
            root = new GameObject("Fire Seats").transform;
            seats = new Vector3[s.Length / 2];
            fires = new Vector3[f.Length / 2];
            for (int i = 0; i < fires.Length; i++) fires[i] = NorthUtil.Cell(map, f[2 * i], f[2 * i + 1]);
            var sprite = NorthUtil.Load("fire_seat", new Vector2(0.5f, 0f));
            for (int i = 0; i < seats.Length; i++)
            {
                var pos = NorthUtil.Cell(map, s[2 * i], s[2 * i + 1]);
                seats[i] = pos;
                if (sprite != null)
                {
                    var go = new GameObject("Seat");
                    go.transform.SetParent(root, false);
                    go.transform.position = pos + new Vector3(0f, -0.5f, 0f);
                    var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = 8;
                }
                int k = i;
                NorthSpot.Make(root, pos, () => "불 쬐기", () => Sit(k), Interactable.Kind.Board);
            }
        }

        Vector3 NearestFire(Vector3 p)
        {
            Vector3 best = p + Vector3.right; float bd = float.MaxValue;
            foreach (var f in fires) { float d = (f - p).sqrMagnitude; if (d < bd) { bd = d; best = f; } }
            return best;
        }

        void Sit(int i)
        {
            var pc = PlayerController.Instance;
            if (pc == null || pc.Motor == null || sitting >= 0) return;
            var to = NearestFire(seats[i]) - seats[i];
            var face = Mathf.Abs(to.x) >= Mathf.Abs(to.y) ? new Vector2Int(to.x > 0 ? 1 : -1, 0) : new Vector2Int(0, to.y > 0 ? 1 : -1);
            standPos = pc.transform.position; standFace = pc.Motor.Facing;
            pc.Motor.Teleport(seats[i] - (Vector3)pc.Motor.feetOffset, face);
            if (pc.anim != null) { pc.anim.facing = face; pc.anim.walking = false; }
            sitting = i; sitT = 0f; warmed = false;
            UIState.Blocking = true;
            body = pc.anim != null ? pc.anim.transform : null;
            if (body != null) bodyRest = body.localPosition;
            NorthBubble(Murmurs[Random.Range(0, Murmurs.Length)]);
        }

        void Stand(bool release = true)
        {
            if (body != null) { body.localPosition = bodyRest; body.localScale = Vector3.one; }
            var pc = PlayerController.Instance;
            if (release && pc != null && pc.Motor != null) pc.Motor.Teleport(standPos, standFace);
            body = null;
            sitting = -1;
            if (release) UIState.Release(); else UIState.Blocking = false;
        }

        static void NorthBubble(string text) => Notify.Show(text, 2f);

        void Update()
        {
            if (builtFor != NorthUtil.Map) Build();
            if (sitting < 0) return;
            var gm = GameManager.I;
            float dt = Time.unscaledDeltaTime;
            sitT += dt;
            if (gm != null)
            {
                gm.Player.stamina = Mathf.Min(1f, gm.Player.stamina + dt * 0.12f);
                if (!warmed && sitT > 3f)
                {
                    warmed = true;
                    NorthSave.Set("warm", Mathf.Max(NorthSave.Get("warm", 0f), gm.Clock.TotalHours + 1f));
                    Notify.Show("몸이 따뜻해졌다. 1시간 동안 눈보라에도 덜 지친다.", 3f);
                }
            }
            if (sitT > 0.35f && StandPressed()) Stand();
        }

        void LateUpdate()
        {
            if (sitting < 0 || body == null) return;
            // 앉은 자세: 몸을 조금 낮추고 납작하게, 숨 쉬듯 살짝 오르내림
            float breathe = Mathf.Sin(sitT * 2.2f) * 0.012f;
            body.localPosition = bodyRest + new Vector3(0f, -0.16f + breathe, 0f);
            body.localScale = new Vector3(1.04f, 0.86f, 1f);
        }

        void OnGUI()
        {
            if (sitting < 0) return;
            UIStyle.Begin();
            if (hint == null) hint = new GUIStyle(UIStyle.CenterRich) { wordWrap = false, clipping = TextClipping.Overflow };
            string t = "<color=#ffcf7a>모닥불을 쬐는 중</color>   방향키·Space: 일어나기";
            float w = hint.CalcSize(new GUIContent(t)).x + 24f;   // 10/27 글자 길이에 맞춰 상자 폭
            var r = new Rect(UIStyle.VirtualWidth / 2f - w / 2f, UIStyle.VirtualHeight - 60f, w, 18f);
            GUI.Box(r, GUIContent.none, UIStyle.Box);
            GUI.Label(r, t, hint);
        }

        static bool StandPressed()
        {
            // 공용 입력(InputReader)도 같이 본다: 키 설정이 바뀌어도 일어날 수 있게
            if (InputReader.MoveVector().sqrMagnitude > 0.25f || InputReader.InteractPressed()) return true;
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            return kb.wKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame
                || kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame
                || kb.rightArrowKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D)
                || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.LeftArrow)
                || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}
