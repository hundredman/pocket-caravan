using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 북유럽 생활 콘텐츠 (10/24).
    /// - 온천 목욕 (라우가르): 김 속에서 몸을 녹이면 게임 1시간이 지나고, 달리기 체력·짐 흔들림이 회복되고,
    ///   3시간 동안 눈보라에도 덜 지친다 (NorthSave "warm" → NorthTravel).
    /// - 순록 먹이 (순록 고원 우리): 하루 한 번 이끼를 주면 하트가 떠오른다. 서로 다른 날 세 번 주면 목동 할머니가 순록 치즈 2개를 준다.
    /// - 낚시꾼 조합 (피오르 나루): 얼음 낚시 도감 (종류별 잡은 수·최대 크기)과 기록 보상.
    ///   처음 잡은 종류마다 20냥, 크기 목표(대구 60cm, 곤들매기 50cm, 황금 연어 90cm)를 넘기면 50/80/200냥 (한 번씩).
    /// </summary>
    public class NorthDaily : MonoBehaviour
    {
        Transform root;
        string lastMap;
        float bathFade = -1f;
        AudioSource sfx;
        AudioClip splash, coo;
        Sprite heart;
        readonly List<(SpriteRenderer sr, float born)> hearts = new List<(SpriteRenderer, float)>();

        public struct FishInfo { public string id, name; public int goal, goalReward; }
        public static readonly FishInfo[] Fish =
        {
            new FishInfo { id = "north_cod", name = "얼음 대구", goal = 60, goalReward = 50 },
            new FishInfo { id = "north_char", name = "북극 곤들매기", goal = 50, goalReward = 80 },
            new FishInfo { id = "north_salmon", name = "황금 연어", goal = 90, goalReward = 200 },
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthDaily>() != null) return;
            var go = new GameObject("NorthDaily");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthDaily>();
        }

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>(); sfx.playOnAwake = false;
            splash = NorthUtil.Tone("north_bath", 0.7f, new[] { 330f, 262f, 196f }, 0.25f);
            coo = NorthUtil.Tone("north_coo", 0.35f, new[] { 880f, 1046f }, 0.25f);
            heart = NorthUtil.Load("heart", new Vector2(0.5f, 0.5f));
        }

        void Update()
        {
            string map = NorthUtil.Map;
            if (map != lastMap) { lastMap = map; Rebuild(map); }
            if (bathFade >= 0f)
            {
                bathFade += Time.unscaledDeltaTime;
                if (bathFade > 2.6f) { bathFade = -1f; UIState.Release(); FinishBath(); }
            }
            for (int i = hearts.Count - 1; i >= 0; i--)
            {
                var (sr, born) = hearts[i];
                float age = Time.time - born;
                if (sr == null || age > 1.6f) { if (sr != null) Destroy(sr.gameObject); hearts.RemoveAt(i); continue; }
                sr.transform.position += new Vector3(Mathf.Sin(age * 6f) * 0.01f, Time.deltaTime * 0.8f, 0f);
                sr.color = new Color(1f, 1f, 1f, 1f - age / 1.6f);
            }
        }

        void Rebuild(string map)
        {
            if (root != null) Destroy(root.gameObject);
            root = null;
            if (map == null || !map.StartsWith("north_")) return;
            var baths = NorthData.Get(map, "baths");
            var feed = NorthData.Get(map, "feed");
            var guild = NorthData.Get(map, "guild");
            if (baths.Length < 2 && feed.Length < 2 && guild.Length < 2) return;
            root = new GameObject("North Daily").transform;
            for (int i = 0; i + 1 < baths.Length; i += 2)
                NorthSpot.Make(root, NorthUtil.Cell(map, baths[i], baths[i + 1]), () => "온천 (몸 녹이기)", Bath);
            if (feed.Length >= 2)
            {
                var pos = NorthUtil.Cell(map, feed[0], feed[1]);
                NorthSpot.Make(root, pos, () => FedToday ? "순록 우리 (오늘은 배불러)" : "순록 우리 (이끼 주기)", () => Feed(pos));
            }
            if (guild.Length >= 2)
                NorthSpot.Make(root, NorthUtil.Cell(map, guild[0], guild[1]), () => "낚시꾼 조합", Guild, Interactable.Kind.Board);
        }

        // ---------------- 온천 ----------------

        void Bath()
        {
            if (bathFade >= 0f) return;
            bathFade = 0f;
            UIState.Blocking = true;
            if (!AudioManager.Muted) sfx.PlayOneShot(splash, 0.6f);
        }

        void FinishBath()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            gm.Clock.Advance(1f);
            gm.Player.stamina = 1f;
            gm.Player.shake = 0f;
            NorthSave.Set("warm", gm.Clock.TotalHours + 3f);
            Notify.Show("몸이 사르르 녹았다. 3시간 동안 눈보라에도 덜 지친다.", 3.5f);
        }

        // ---------------- 순록 먹이 ----------------

        static int Today => GameManager.I != null ? GameManager.I.Clock.day : 0;
        static bool FedToday => NorthSave.Get("feedday", -1f) >= Today;

        void Feed(Vector3 pos)
        {
            if (FedToday)
            {
                DialogueUI.Show("순록 우리", "순록들이 배를 두드리며 누워 있다. 내일 다시 오자.");
                return;
            }
            NorthSave.Set("feedday", Today);
            int n = Mathf.RoundToInt(NorthSave.Add("feedcount", 1f));
            if (!AudioManager.Muted) sfx.PlayOneShot(coo, 0.6f);
            for (int k = 0; k < 6; k++)
            {
                if (heart == null) break;
                var go = new GameObject("Reindeer Heart");
                go.transform.position = pos + new Vector3(Random.Range(-4f, 4f), Random.Range(1.5f, 5f), 0f);
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = heart; sr.sortingOrder = 30;
                hearts.Add((sr, Time.time + k * 0.12f));
            }
            if (n >= 3 && !NorthSave.Has("reindeerfriend"))
            {
                NorthSave.Set("reindeerfriend", 1f);
                var gm = GameManager.I;
                var cheese = gm != null ? gm.ItemById("north_cheese") : null;
                string gift = "";
                if (cheese != null && gm.Player.CanCarry(cheese, 2)) { gm.Player.cargo.Add(cheese, 2, 0f); gift = "\n(순록 치즈 2개를 받았다)"; }
                else if (gm != null) { gm.Player.money += 30; gift = "\n(짐이 가득해서 대신 30냥을 받았다)"; }
                DialogueUI.Show("순록 목동 할머니", "어머나, 순록들이 너만 보면 달려오는구나! 사흘이나 챙겨 줬다며? 이건 우리 순록 젖으로 만든 치즈야." + gift);
            }
            else Notify.Show($"순록들이 이끼를 오물오물 먹는다 (먹이 준 날 {n}일)", 2.5f);
        }

        // ---------------- 낚시꾼 조합 ----------------

        public static int Caught(string id) => Mathf.RoundToInt(NorthSave.Get("fish:" + id + ":count"));
        public static int Best(string id) => Mathf.RoundToInt(NorthSave.Get("fish:" + id + ":best"));

        void Guild()
        {
            var lines = new List<string> { "얼음 낚시 도감" };
            int pay = 0;
            var claims = new List<string>();
            foreach (var f in Fish)
            {
                int c = Caught(f.id), b = Best(f.id);
                lines.Add(c > 0 ? $"· {f.name}: {c}마리, 최대 {b}cm (목표 {f.goal}cm{(b >= f.goal ? " 달성" : "")})" : $"· ???: 아직 못 잡음");
                if (c > 0 && !NorthSave.Has("guild:first:" + f.id)) { pay += 20; claims.Add("guild:first:" + f.id); }
                if (b >= f.goal && !NorthSave.Has("guild:goal:" + f.id)) { pay += f.goalReward; claims.Add("guild:goal:" + f.id); }
            }
            string body = string.Join("\n", lines);
            if (pay > 0)
                DialogueUI.Show("낚시꾼 조합장", body + $"\n\n새 기록이 있구먼! 조합에서 상금 {pay}냥을 주지.",
                    new DialogueUI.Choice($"상금 받기 ({pay}냥)", () =>
                    {
                        foreach (var k in claims) NorthSave.Set(k, 1f);
                        if (GameManager.I != null) GameManager.I.Player.money += pay;
                        Notify.Show($"낚시꾼 조합 상금 +{pay}냥", 3f);
                    }),
                    new DialogueUI.Choice("나중에", null));
            else
                DialogueUI.Show("낚시꾼 조합장", body + "\n\n처음 잡는 물고기마다 20냥, 크기 목표를 넘기면 큰 상금을 주지. 황금 연어는 오로라 뜬 밤에 잘 문다더군.");
        }

        void OnGUI()
        {
            if (bathFade < 0f) return;
            float a = Mathf.Sin(Mathf.Clamp01(bathFade / 2.6f) * Mathf.PI);
            var old = GUI.color;
            GUI.color = new Color(0.95f, 0.97f, 1f, 0.9f * a);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = old;
            UIStyle.Begin();
            GUI.depth = -20;
            GUI.Label(new Rect(0, UIStyle.VirtualHeight / 2 - 10, UIStyle.VirtualWidth, 20), "따끈한 온천에 몸을 담갔다…", UIStyle.CenterRich);
        }
    }
}
