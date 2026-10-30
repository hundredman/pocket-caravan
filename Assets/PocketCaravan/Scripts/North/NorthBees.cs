using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 10/30 라우가르 서북쪽 꿀벌 겨울 오두막 (기우 기획). 공용 코드는 고치지 않는다.
    /// - 자리: NorthLife.json "beeHives" [x,y]×5, "beeStraw" [x,y](짚단 더미 왼쪽 칸), "beeKeeper" [x,y] (n4_build.py place_bees)
    /// - 실제 맵 심부름: 양봉가 마르타에게 말 걸기 → 짚단 더미에서 Space로 짚 들기 → 안 감싼 벌통에서 Space로 감싸기 ×3.
    ///   밤사이 바람에 짚이 날아가서 날마다 앞의 세 통이 다시 맨 벌통이 된다(하루 한 번 할 일).
    /// - 보상: 하루 한 번 벌꿀 단지 1 (양조장에서 꿀술 2 대신 쓸 수 있음, NorthCraft). 처음 도우면 벌통 둘레에 겨울 벌 몇 마리가 날아다님.
    /// - 저장: NorthSave "bees:day"(감싸기 시작한 날), "bees:mask"(오늘 감싼 벌통 비트), "bees:honey"(꿀 받은 날), "bees:first"
    /// </summary>
    public class NorthBees : MonoBehaviour
    {
        public const string HoneyId = "north_honey";
        const string MapId = "north_laugar";
        const int Need = 3;   // 날마다 다시 감싸야 하는 벌통 수 (앞의 세 통)

        Transform root;
        string lastMap;
        Sprite bare, wrapped, carrySp, beeSp;
        readonly List<(SpriteRenderer sr, NorthSpot spot, Vector3 pos)> hives = new List<(SpriteRenderer, NorthSpot, Vector3)>();
        readonly List<(SpriteRenderer sr, float phase, int hive)> bees = new List<(SpriteRenderer, float, int)>();
        readonly List<(SpriteRenderer sr, Sprite[] f)> idlers = new List<(SpriteRenderer, Sprite[])>();
        SpriteRenderer carry;
        bool carrying;
        AudioSource sfx;
        AudioClip rustle, done;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthBees>() != null) return;
            var obj = new GameObject("NorthBees");
            DontDestroyOnLoad(obj);
            obj.AddComponent<NorthBees>();
        }

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>(); sfx.playOnAwake = false;
            rustle = NorthUtil.Tone("north_bees_rustle", 0.12f, new[] { 420f, 360f }, 0.2f);
            done = NorthUtil.Tone("north_bees_done", 0.5f, new[] { 784f, 988f, 1175f }, 0.3f);
            bare = NorthUtil.Load("skep_bare", new Vector2(0.5f, 0f));
            wrapped = NorthUtil.Load("skep_wrapped", new Vector2(0.5f, 0f));
            carrySp = NorthUtil.Load("straw_carry", new Vector2(0.5f, 0f));
            beeSp = NorthUtil.Load("bee", new Vector2(0.5f, 0.5f));
        }

        void Play(AudioClip c, float v = 0.5f) { if (c != null && !AudioManager.Muted) sfx.PlayOneShot(c, v); }
        static int Today => GameManager.I != null && GameManager.I.Clock != null ? GameManager.I.Clock.day : 0;
        static bool Started => (int)NorthSave.Get("bees:day", -1f) == Today;
        static int Mask => Started ? (int)NorthSave.Get("bees:mask", 0f) : 0;
        static int Wrapped { get { int n = 0, m = Mask; for (int i = 0; i < Need; i++) if ((m & (1 << i)) != 0) n++; return n; } }
        static bool HoneyToday => (int)NorthSave.Get("bees:honey", -1f) == Today;
        static bool IsWrapped(int i) => i >= Need || (Mask & (1 << i)) != 0;   // 뒤의 두 통은 늘 감싸 둔 상태

        void Update()
        {
            string map = NorthUtil.Map;
            if (map != lastMap) { lastMap = map; carrying = false; Rebuild(map); }
            if (root == null) return;
            foreach (var it in idlers)
                if (it.sr != null && it.f[0] != null && it.f[1] != null) it.sr.sprite = it.f[(int)(Time.time * 1.5f) % 2];
            for (int i = 0; i < hives.Count; i++)
                if (hives[i].sr != null) hives[i].sr.sprite = IsWrapped(i) ? wrapped : bare;
            // 짚 들고 다니기: 머리 위에 짚단
            var pc = PlayerController.Instance;
            if (carry != null)
            {
                carry.enabled = carrying && pc != null;
                if (carry.enabled) carry.transform.position = pc.transform.position + new Vector3(0f, 0.85f, 0f);
            }
            // 겨울 벌: 처음 도운 뒤부터 벌통 둘레를 맴돈다
            foreach (var b in bees)
            {
                if (b.sr == null || b.hive >= hives.Count) continue;
                float t = Time.time * 2.2f + b.phase;
                b.sr.transform.position = hives[b.hive].pos + new Vector3(Mathf.Cos(t) * 0.45f, 0.9f + Mathf.Sin(t * 1.7f) * 0.25f, 0f);
            }
        }

        void Rebuild(string map)
        {
            if (root != null) Destroy(root.gameObject);
            root = null; hives.Clear(); bees.Clear(); idlers.Clear(); carry = null; hasStraw = false;
            if (map != MapId || MapRoot.Current == null) return;
            var hv = NorthData.Get(map, "beeHives");
            if (hv.Length < 2) return;
            root = new GameObject("North Bees").transform;
            for (int i = 0; i + 1 < hv.Length; i += 2)
            {
                int k = i / 2;
                var pos = NorthUtil.Cell(map, hv[i], hv[i + 1]);
                var spot = NorthSpot.Make(root, pos, () => "", () => Hive(k), Interactable.Kind.Person);
                spot.verb = "짚 감싸기";
                var go = new GameObject("Hive");
                go.transform.SetParent(root, false);
                go.transform.position = pos + new Vector3(0f, -0.5f, 0f);
                var sr = go.AddComponent<SpriteRenderer>(); sr.sortingOrder = 9; sr.sprite = IsWrapped(k) ? wrapped : bare;
                hives.Add((sr, spot, pos));
            }
            var st = NorthData.Get(map, "beeStraw");
            if (st.Length >= 2)
            {
                var pos = NorthUtil.Cell(map, st[0], st[1]) + new Vector3(0.5f, 0f, 0f);
                strawPos = pos; hasStraw = true;
                var s = NorthSpot.Make(root, pos, () => "짚단 더미", Straw, Interactable.Kind.Board);
                s.verb = "짚 들기";
                var go = new GameObject("Straw Pile");
                go.transform.SetParent(root, false);
                go.transform.position = pos + new Vector3(0f, -0.5f, 0f);
                var sr = go.AddComponent<SpriteRenderer>(); sr.sortingOrder = 9; sr.sprite = NorthUtil.Load("straw_pile", new Vector2(0.5f, 0f));
            }
            var kp = NorthData.Get(map, "beeKeeper");
            if (kp.Length >= 2)
            {
                var s = NorthSpot.Make(root, NorthUtil.Cell(map, kp[0], kp[1]), () => "양봉가 마르타", Keeper, Interactable.Kind.Person);
                var f = new Sprite[2];
                for (int j = 0; j < 2; j++) f[j] = NorthUtil.Load("beekeeper_u" + j, new Vector2(0.5f, 0.1f));
                if (f[0] != null) { var sr = s.gameObject.AddComponent<SpriteRenderer>(); sr.sprite = f[0]; sr.sortingOrder = 10; idlers.Add((sr, f)); }
            }
            var cg = new GameObject("Carried Straw");
            cg.transform.SetParent(root, false);
            carry = cg.AddComponent<SpriteRenderer>(); carry.sprite = carrySp; carry.sortingOrder = 12; carry.enabled = false;
            if (NorthSave.Has("bees:first") && beeSp != null)
                for (int i = 0; i < 4; i++)
                {
                    var go = new GameObject("Bee");
                    go.transform.SetParent(root, false);
                    var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = beeSp; sr.sortingOrder = 12;
                    bees.Add((sr, i * 1.7f, (i * 2 + 1) % hives.Count));
                }
        }

        // ---------------- 어디로 가야 하는지 표시 (10/30 기우: 짚을 어디 두는지 잘 안 보임) ----------------
        Vector3 strawPos; bool hasStraw;

        void OnGUI()
        {
            if (root == null || UIState.Blocking || !Started || Wrapped >= Need) return;
            var cam = Camera.main;
            if (cam == null) return;
            UIStyle.Begin();
            float bob = Mathf.Sin(Time.time * 5f) * 0.08f;
            if (carrying)
            {
                for (int i = 0; i < hives.Count && i < Need; i++)
                    if (!IsWrapped(i)) Arrow(cam, hives[i].pos + new Vector3(0f, 0.75f + bob, 0f), "짚 감싸기");
            }
            else if (hasStraw) Arrow(cam, strawPos + new Vector3(0f, 0.7f + bob, 0f), "짚 들기");
        }

        static void Arrow(Camera cam, Vector3 world, string text)
        {
            var s = cam.WorldToScreenPoint(world);
            var p = new Vector2(s.x / UIStyle.Scale, (Screen.height - s.y) / UIStyle.Scale);
            UIStyle.Outlined(new Rect(p.x - 40, p.y - 22, 80, 12), $"<color=#ffd75a>{text}</color>", UIStyle.CenterRich);
            UIStyle.Outlined(new Rect(p.x - 10, p.y - 11, 20, 14), "<color=#ffd75a><b>▼</b></color>", UIStyle.CenterRich);
        }

        // ---------------- 말 걸기 ----------------

        void Keeper()
        {
            if (HoneyToday)
            {
                DialogueUI.Show("양봉가 마르타", "오늘 몫의 꿀은 벌써 줬지? 벌들도 짚 이불 덮고 푹 자고 있어. 내일 또 바람이 불면 도와주렴.");
                return;
            }
            if (Started && Wrapped >= Need)
            {
                GiveHoney();
                return;
            }
            if (Started)
            {
                DialogueUI.Show("양봉가 마르타", $"짚단 더미에서 짚을 들고, 맨 벌통에 감싸 주렴. ({Wrapped}/{Need})\n벌들이 얼지 않게 꼭꼭 여며 줘야 해.");
                return;
            }
            DialogueUI.Show("양봉가 마르타",
                "아이고, 간밤 바람에 벌통 짚이 다 날아갔지 뭐야. 이 늙은 다리로는 짚 나르기가 버거운데…\n짚단 더미에서 짚을 들어다 맨 벌통 세 개만 감싸 줄 수 있겠니? 꿀 한 단지 줄게.",
                new DialogueUI.Choice("도와드릴게요", () => { NorthSave.Set("bees:day", Today); NorthSave.Set("bees:mask", 0f); Notify.Show("짚단 더미에서 짚을 들자 (Space)", 3f); }),
                new DialogueUI.Choice("다음에", null));
        }

        void Straw()
        {
            if (!Started || Wrapped >= Need) { Notify.Show(Started ? "벌통은 다 감쌌다. 마르타에게 말해 보자." : "양봉가 마르타의 짚단이다.", 2.5f); return; }
            if (carrying) { Notify.Show("이미 짚을 들고 있다. 맨 벌통으로 가자.", 2f); return; }
            carrying = true; Play(rustle);
            Notify.Show("짚을 들었다. 노란 ▼ 표시가 있는 맨 벌통으로 가서 Space!", 3f);
        }

        void Hive(int k)
        {
            if (IsWrapped(k)) { Notify.Show("짚으로 따뜻하게 감싼 벌통이다. 안에서 웅웅 소리가 난다.", 2f); return; }
            if (!Started) { Notify.Show("짚이 날아가 버린 벌통이다. 추워 보인다… (양봉가에게 말 걸기)", 2.5f); return; }
            if (!carrying) { Notify.Show("짚단 더미에서 짚을 먼저 들고 오자.", 2f); return; }
            carrying = false;
            NorthSave.Set("bees:mask", Mask | (1 << k));
            Play(rustle, 0.6f);
            if (Wrapped >= Need) { Play(done); Notify.Show("벌통을 다 감쌌다! 양봉가 마르타에게 말해 보자.", 3f); }
            else Notify.Show($"벌통을 감쌌다 ({Wrapped}/{Need})", 2f);
        }

        void GiveHoney()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            var item = gm.ItemById(HoneyId);
            if (item != null && !gm.Player.CanCarry(item)) { DialogueUI.Show("양봉가 마르타", "고마워라! 그런데 짐이 가득하구나. 꿀 단지 들어갈 자리를 좀 비우고 오렴."); return; }
            NorthSave.Set("bees:honey", Today);
            if (item != null) gm.Player.cargo.Add(item, 1, 0f);
            bool first = !NorthSave.Has("bees:first");
            if (first) NorthSave.Set("bees:first", 1f);
            DialogueUI.Show("양봉가 마르타", "아이고 고마워라, 벌들이 이제 겨울을 나겠구나. 자, 우리 벌들이 여름내 모은 꿀이야.\n양조장 할머니한테 가져가면 약초 꿀술을 빚어 줄 거야."
                + (item != null ? "\n(벌꿀 단지 1개)" : "") + (first ? "\n\n벌통 둘레로 겨울 벌 몇 마리가 고맙다는 듯 날아다닌다." : ""));
            if (first) Rebuild(NorthUtil.Map);
        }
    }
}
