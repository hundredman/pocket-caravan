using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 북유럽 사람들과 어울리기 (10/25, 동물의 숲 오마주). 북유럽·중앙 북쪽 맵에서만.
    /// - 리액션 바퀴 (R): 방향키로 고르고 Space. 인사·끄덕이기·박수는 처음부터, 나머지는 배워서 얻는다.
    ///   내 캐릭터가 동작을 하고 머리 위에 말풍선. 근처 사람들이 따라 하거나 한마디 한다.
    /// - 친밀도 (주요 8명): 하루 한 번 말 걸면 +1, 선물(G) +2, 좋아하는 선물 +5. 하트 3칸 (4·10·20점).
    ///   하트 1: 다정한 인사 / 하트 2: 리액션을 가르쳐 주거나 답례 / 하트 3: 특별한 선물.
    /// - 사진 (P): 나무 액자 테두리에 날짜·장소를 넣어 화면 저장 (저장 폴더 Application.persistentDataPath/Photos).
    /// 진행은 NorthSave (react:이름, friend:이름, talk:이름=날, gift:이름=날, friendlv:이름).
    /// </summary>
    [DefaultExecutionOrder(1000)]   // 공용 CarryVisual이 그림 위치를 정한 뒤에 리액션 동작을 덧입힌다
    public class NorthSocial : MonoBehaviour
    {
        // ---------------- 리액션 ----------------
        public struct Reaction { public string id, name, bubble; public bool basic; }
        public static readonly Reaction[] Reactions =
        {
            new Reaction { id = "wave", name = "인사", bubble = "안녕!", basic = true },
            new Reaction { id = "nod", name = "끄덕이기", bubble = "응응", basic = true },
            new Reaction { id = "clap", name = "박수", bubble = "짝짝짝", basic = true },
            new Reaction { id = "dance", name = "춤", bubble = "♪♪", basic = false },
            new Reaction { id = "surprise", name = "놀람", bubble = "!!", basic = false },
            new Reaction { id = "joy", name = "기쁨", bubble = "야호!", basic = false },
            new Reaction { id = "think", name = "생각하기", bubble = "음…", basic = false },
            new Reaction { id = "pray", name = "날씨 기도", bubble = "맑아져라…", basic = false },
        };
        public static bool Knows(int i) => Reactions[i].basic || NorthSave.Has("react:" + Reactions[i].id);
        public static string Teach(string id)
        {
            for (int i = 0; i < Reactions.Length; i++)
                if (Reactions[i].id == id && !Knows(i)) { NorthSave.Set("react:" + id, 1f); Notify.Show($"리액션 '{Reactions[i].name}'을(를) 배웠다! (R)", 3.5f); return Reactions[i].name; }
            return null;
        }
        /// <summary>아직 모르는 리액션 하나 (떠돌이 상인의 리액션 책)</summary>
        public static string TeachRandom()
        {
            var left = new List<string>();
            for (int i = 0; i < Reactions.Length; i++) if (!Knows(i) && Reactions[i].id != "pray") left.Add(Reactions[i].id);
            return left.Count == 0 ? null : Teach(left[Random.Range(0, left.Count)]);
        }

        // ---------------- 친밀도 ----------------
        public struct Friend { public string name, liked, likedName, l1, l2, l3, teach; }
        public static readonly Friend[] Friends =
        {
            new Friend { name = "여관 할머니", liked = "north_cod", likedName = "얼음 대구", teach = "dance",
                l1 = "또 왔구나! 너만 오면 화롯불이 더 따뜻한 것 같아.", l2 = "젊을 적 축제에서 추던 춤이 있지. 자, 이렇게 발을 구르는 거야!", l3 = "우리 집 비법 생선 수프 단지야. 먼 길 갈 때 먹으렴." },
            new Friend { name = "목수 하르", liked = "north_fur", likedName = "모피", teach = "surprise",
                l1 = "자네 수레 바퀴 소리만 들어도 알겠어. 잘 굴리고 있군!", l2 = "썰매 날이 얼음에 박히면 이렇게 놀라지! 하하, 따라 해 보게.", l3 = "남은 나무로 깎은 작은 순록이야. 행운을 가져다준다네." },
            new Friend { name = "순록 목동 할머니", liked = "north_salt", likedName = "남쪽 소금", teach = "joy",
                l1 = "순록들이 네 발소리를 알아듣는구나.", l2 = "순록이 새끼를 낳던 날, 우리는 이렇게 팔짝 뛰었단다!", l3 = "고원 치즈 중에 제일 오래 묵힌 거야. 아껴 먹어." },
            new Friend { name = "룬 지기 노인", liked = "north_amber", likedName = "호박", teach = "think",
                l1 = "룬을 읽는 눈이 생겼구먼.", l2 = "룬은 서두르면 안 읽혀. 이렇게 턱을 괴고… 음…", l3 = "옛 상인들이 지니던 호박 부적일세. 길을 잃지 않게 해 주지." },
            new Friend { name = "집 보는 남편", liked = "north_cheese", likedName = "순록 치즈", teach = null,
                l1 = "집안일 요령이 좀 늘었어. 버터도 이제 안 넘쳐!", l2 = "고마워서 그래, 이거 받아. 아내가 캔 약초야.", l3 = "우리 부부가 함께 짠 털목도리야. 눈보라에도 따뜻할 거야." },
            new Friend { name = "들일 하는 아내", liked = "north_herb", likedName = "온천 약초", teach = null,
                l1 = "밭일하다 너 지나가는 거 봤어!", l2 = "이건 남편이 만든 버터 과자야. 맛은 장담 못 해.", l3 = "밭 귀퉁이에서 찾은 옛 은화야. 너한테 더 쓸모 있을 거야." },
            new Friend { name = "얄 시그리드", liked = "north_salmon", likedName = "황금 연어", teach = null,
                l1 = "비르카의 이름 있는 상인이 되어 가는군.", l2 = "길드에 자네 이름을 올려 두었네. 다들 자네를 믿어.", l3 = "얄의 회관 보물 창고에서 꺼낸 것이네. 자네 몫일세." },
            new Friend { name = "초소 대장", liked = "north_mead", likedName = "꿀술", teach = null,
                l1 = "오, 단골이군! 초소 문은 언제나 열려 있어.", l2 = "남쪽 상인들한테 들은 소식이야, 받아 둬. 그리고 이건 수고비.", l3 = "초소 경비들이 모은 감사의 표시야. 남쪽 길 조심하라고." },
        };
        static readonly int[] Need = { 4, 10, 20 };
        public static int Points(string n) => Mathf.RoundToInt(NorthSave.Get("friend:" + n));
        public static int Hearts(string n) { int p = Points(n), h = 0; foreach (var v in Need) if (p >= v) h++; return h; }
        static int Today => GameManager.I != null ? GameManager.I.Clock.day : 0;
        static int FriendIndex(string name) { for (int i = 0; i < Friends.Length; i++) if (Friends[i].name == name) return i; return -1; }

        // ---------------- 상태 ----------------
        bool wheelOpen;
        int wheelSel;
        int acting = -1;
        float actT;
        Transform body;
        Vector3 bodyRest; bool restSaved;
        readonly List<(Transform t, Vector3 rest, float until, int kind)> npcActs = new List<(Transform, Vector3, float, int)>();
        readonly List<(Vector3 world, string text, float until)> bubbles = new List<(Vector3, string, float)>();
        AudioSource sfx;
        AudioClip pop, chime, shutter;
        bool photo; int photoFrames; string photoPath; float flash;
        int lastTalkFrame = -1;
        string lastMap;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthSocial>() != null) return;
            var go = new GameObject("NorthSocial");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthSocial>();
        }

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>(); sfx.playOnAwake = false;
            pop = NorthUtil.Tone("north_react", 0.15f, new[] { 880f, 1175f }, 0.25f);
            chime = NorthUtil.Tone("north_friend", 0.5f, new[] { 659f, 784f, 1046f }, 0.3f);
            shutter = NorthUtil.Tone("north_shutter", 0.12f, new[] { 2200f, 900f }, 0.35f);
        }

        static bool Key(string k)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            switch (k) { case "R": return kb.rKey.wasPressedThisFrame; case "G": return kb.gKey.wasPressedThisFrame; case "P": return kb.pKey.wasPressedThisFrame; }
            return false;
#else
            return Input.GetKeyDown(k == "R" ? KeyCode.R : k == "G" ? KeyCode.G : KeyCode.P);
#endif
        }

        static bool ArrowPressed(int dir)   // 0 왼쪽, 1 오른쪽, 2 위, 3 아래
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            return dir == 0 ? kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame
                 : dir == 1 ? kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame
                 : dir == 2 ? kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame
                 : kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(dir == 0 ? KeyCode.LeftArrow : dir == 1 ? KeyCode.RightArrow : dir == 2 ? KeyCode.UpArrow : KeyCode.DownArrow);
#endif
        }

        void Update()
        {
            string map = NorthUtil.Map;
            if (map != lastMap) { lastMap = map; npcActs.Clear(); bubbles.Clear(); body = null; restSaved = false; }
            if (!NorthUtil.InNorth || GameManager.I == null) { if (wheelOpen) CloseWheel(); return; }
            if (!NorthSave.Has("tips_social")) { NorthSave.Set("tips_social", 1f); Notify.Show("R 리액션 · G 주요 인물에게 선물 · P 사진 찍기", 5f); }
            var pc = PlayerController.Instance;

            // 사진
            if (Key("P") && !photo && !wheelOpen) { photo = true; photoFrames = 0; }

            // 리액션 바퀴
            if (wheelOpen)
            {
                if (ArrowPressed(1) || ArrowPressed(3)) wheelSel = (wheelSel + 1) % Reactions.Length;
                if (ArrowPressed(0) || ArrowPressed(2)) wheelSel = (wheelSel + Reactions.Length - 1) % Reactions.Length;
                if (InputReader.CancelPressed() || Key("R")) CloseWheel();
                else if (InputReader.InteractPressed())
                {
                    if (Knows(wheelSel)) { CloseWheel(); StartReaction(wheelSel); }
                    else Notify.Show("아직 모르는 리액션이야. 사람들과 친해지거나 축제에서 배울 수 있어.", 2.5f);
                }
                return;
            }
            if (!UIState.Blocking && acting < 0 && Key("R") && pc != null) { wheelOpen = true; UIState.Blocking = true; }

            // 선물 (G): 말을 걸 수 있는 주요 인물 앞에서
            if (!UIState.Blocking && acting < 0 && Key("G") && pc != null && pc.TalkTarget != null)
            {
                int fi = FriendIndex(pc.TalkTarget.DisplayName);
                if (fi >= 0) OfferGift(fi);
                else Notify.Show("이 사람에게는 선물을 줄 수 없어. (주요 인물에게만)", 2f);
            }

            // 말 걸기 횟수 (하루 한 번 +1): 이번 프레임 Space로 주요 인물에게 말을 걸었는지
            // (이 스크립트는 PlayerController 다음에 돈다: 대화창이 방금 열렸어도 이번 프레임의 TalkTarget은 남아 있음)
            if (pc != null && acting < 0 && InputReader.InteractPressed() && pc.TalkTarget != null && lastTalkFrame != Time.frameCount)
            {
                lastTalkFrame = Time.frameCount;
                int fi = FriendIndex(pc.TalkTarget.DisplayName);
                if (fi >= 0 && NorthSave.Get("talk:" + Friends[fi].name, -1f) < Today)
                {
                    NorthSave.Set("talk:" + Friends[fi].name, Today);
                    AddPoints(fi, 1, null);
                }
            }

            if (acting >= 0)
            {
                actT += Time.deltaTime;
                if (actT > 1.8f) { acting = -1; UIState.Release(); }
            }
        }

        void CloseWheel() { wheelOpen = false; UIState.Release(); }

        // ---------------- 리액션 동작 ----------------

        void StartReaction(int i)
        {
            acting = i; actT = 0f;
            UIState.Blocking = true;
            var pc = PlayerController.Instance;
            if (pc == null) return;
            if (!AudioManager.Muted) sfx.PlayOneShot(pop, 0.6f);
            bubbles.Add((pc.transform.position + new Vector3(0f, 1.7f, 0f), Reactions[i].bubble, Time.time + 1.8f));
            if (Reactions[i].id == "pray" && GameManager.I != null)
            {
                // 날씨 기도: 하루 한 번, 함박눈·눈보라를 지금 네 시간 동안 가랑눈으로 누그러뜨린다 (NorthAtmosphere가 "calm_block"을 봄)
                int block = Mathf.FloorToInt(GameManager.I.Clock.TotalHours / 4f);
                if (NorthSave.Get("prayday", -1f) < Today && NorthAtmosphere.Shown >= NorthAtmosphere.Weather.Heavy)
                {
                    NorthSave.Set("prayday", Today); NorthSave.Set("calm_block", block);
                    Notify.Show("기도가 닿았다… 눈보라가 잦아든다.", 3f);
                }
                else Notify.Show(NorthSave.Get("prayday", -1f) >= Today ? "오늘은 이미 기도를 올렸다." : "하늘이 이미 잔잔하다.", 2f);
            }
            // 근처 사람들의 반응
            foreach (var it in Interactable.All)
            {
                if (it == null || it is NorthSpot || it is NorthFishingSpot || it is NorthSledStart) continue;
                if (it.Category != Interactable.Kind.Person && it.Category != Interactable.Kind.Quest) continue;
                float d = Vector2.Distance(it.transform.position, pc.transform.position);
                if (d > 4.5f) continue;
                var anim = it.GetComponentInChildren<CharacterAnimator>();
                var t = anim != null ? anim.transform : it.transform;
                int kind = Reactions[i].id == "dance" || Reactions[i].id == "joy" ? 1 : 0;   // 1 따라 춤, 0 끄덕
                npcActs.Add((t, t.localPosition, Time.time + 1.8f, kind));
                bubbles.Add((it.transform.position + new Vector3(0f, 1.7f, 0f), NpcLine(Reactions[i].id, it.DisplayName), Time.time + 2.2f));
            }
        }

        static string NpcLine(string reaction, string who)
        {
            bool kid = who.Contains("아이") || who.Contains("소녀") || who.Contains("꼬마");
            switch (reaction)
            {
                case "wave": return kid ? "안녕안녕!" : "어서 와!";
                case "nod": return "그렇지, 그렇지.";
                case "clap": return kid ? "짝짝!" : "하하, 좋구먼!";
                case "dance": return kid ? "나도 출래!" : "하하, 흥겹구먼!";
                case "surprise": return "뭐, 뭐야?!";
                case "joy": return "좋은 일 있나 봐?";
                case "think": return "무슨 생각해?";
                case "pray": return "날이 개면 좋겠네.";
            }
            return "…";
        }

        void LateUpdate()
        {
            var pc = PlayerController.Instance;
            if (pc != null && (body == null)) { body = pc.anim != null ? pc.anim.transform : null; restSaved = false; }
            if (body != null)
            {
                if (!restSaved) { bodyRest = body.localPosition; restSaved = true; }
                if (acting >= 0)
                {
                    float t = actT;
                    Vector3 off = Vector3.zero; float rot = 0f; float sx = 1f;
                    switch (Reactions[acting].id)
                    {
                        case "wave": rot = Mathf.Sin(t * 16f) * 8f; break;
                        case "nod": off.y = -Mathf.Abs(Mathf.Sin(t * 9f)) * 0.08f; break;
                        case "clap": off.y = Mathf.Abs(Mathf.Sin(t * 14f)) * 0.06f; sx = 1f + Mathf.Sin(t * 28f) * 0.05f; break;
                        case "dance": off.y = Mathf.Abs(Mathf.Sin(t * 8f)) * 0.22f; rot = Mathf.Sin(t * 8f) * 12f; sx = Mathf.Sin(t * 4f) >= 0 ? 1f : -1f; break;
                        case "surprise": off.y = t < 0.4f ? Mathf.Sin(t / 0.4f * Mathf.PI) * 0.4f : 0f; break;
                        case "joy": off.y = Mathf.Abs(Mathf.Sin(t * 7f)) * 0.35f; break;
                        case "think": rot = Mathf.Sin(t * 2f) * 6f; break;
                        case "pray": off.y = Mathf.Sin(t * 3f) * 0.05f; rot = Mathf.Sin(t * 1.5f) * 4f; break;
                    }
                    body.localPosition = bodyRest + off;
                    body.localRotation = Quaternion.Euler(0, 0, rot);
                    body.localScale = new Vector3(sx, 1f, 1f);
                }
                else if (body.localRotation != Quaternion.identity || body.localScale != Vector3.one)
                {
                    body.localRotation = Quaternion.identity; body.localScale = Vector3.one;
                }
            }
            for (int i = npcActs.Count - 1; i >= 0; i--)
            {
                var (t, rest, until, kind) = npcActs[i];
                if (t == null) { npcActs.RemoveAt(i); continue; }
                float left = until - Time.time;
                if (left <= 0f) { t.localPosition = rest; t.localRotation = Quaternion.identity; npcActs.RemoveAt(i); continue; }
                float k = Time.time * (kind == 1 ? 8f : 9f);
                t.localPosition = rest + new Vector3(0f, kind == 1 ? Mathf.Abs(Mathf.Sin(k)) * 0.2f : -Mathf.Abs(Mathf.Sin(k)) * 0.06f, 0f);
                t.localRotation = Quaternion.Euler(0, 0, kind == 1 ? Mathf.Sin(k) * 10f : 0f);
            }
            bubbles.RemoveAll(b => b.until < Time.time);
            if (flash > 0f) flash -= Time.unscaledDeltaTime;
        }

        // ---------------- 친밀도·선물 ----------------

        void AddPoints(int fi, int pts, string why)
        {
            var f = Friends[fi];
            int before = Hearts(f.name);
            NorthSave.Add("friend:" + f.name, pts);
            int after = Hearts(f.name);
            if (why != null) Notify.Show(why, 2.5f);
            if (after > before && after > NorthSave.Get("friendlv:" + f.name))
            {
                NorthSave.Set("friendlv:" + f.name, after);
                if (!AudioManager.Muted) sfx.PlayOneShot(chime, 0.6f);
                LevelUp(fi, after);
            }
        }

        void LevelUp(int fi, int level)
        {
            var f = Friends[fi];
            var gm = GameManager.I;
            string hearts = new string('♥', level) + new string('♡', 3 - level);
            if (level == 1) { DialogueUI.Show($"{f.name} {hearts}", f.l1); return; }
            if (level == 2)
            {
                string taught = f.teach != null ? Teach(f.teach) : null;
                if (taught == null && gm != null) { gm.Player.money += 50; }
                DialogueUI.Show($"{f.name} {hearts}", f.l2 + (taught != null ? $"\n\n(리액션 '{taught}'을(를) 배웠다!)" : "\n\n(답례 50냥을 받았다)"));
                return;
            }
            // 하트 3: 좋아하는 선물이 그 사람의 특별한 물건으로 돌아옴 (돈 150냥 + 신용 +1)
            if (gm != null) { gm.Player.money += 150; gm.Player.credit += 1; }
            DialogueUI.Show($"{f.name} {hearts}", f.l3 + "\n\n(특별한 선물: 150냥 · 신용 +1)");
        }

        void OfferGift(int fi)
        {
            var f = Friends[fi];
            var gm = GameManager.I;
            if (NorthSave.Get("gift:" + f.name, -1f) >= Today) { DialogueUI.Show(f.name, "오늘은 벌써 선물을 받았는걸. 마음만 받을게!"); return; }
            var choices = new List<DialogueUI.Choice>();
            var liked = gm.ItemById(f.liked);
            if (liked != null && gm.Player.cargo.Count(liked) > 0)
                choices.Add(new DialogueUI.Choice($"{liked.displayName} 주기 (좋아함!)", () => Give(fi, liked)));
            foreach (var kv in gm.Player.cargo.Items)
            {
                if (choices.Count >= 3) break;
                if (kv.Key == liked || kv.Value <= 0 || kv.Key.questItem) continue;
                var item = kv.Key;
                choices.Add(new DialogueUI.Choice($"{item.displayName} 주기", () => Give(fi, item)));
            }
            choices.Add(new DialogueUI.Choice("다음에", null));
            string hint = $"(좋아하는 것: {f.likedName})";
            DialogueUI.Show($"{f.name}에게 선물", choices.Count > 1 ? "무엇을 줄까?\n" + hint : "줄 만한 물건이 없어.\n" + hint, choices.ToArray());
        }

        void Give(int fi, ItemData item)
        {
            var f = Friends[fi];
            var gm = GameManager.I;
            if (item == null || !gm.Player.cargo.Remove(item, 1)) return;
            NorthSave.Set("gift:" + f.name, Today);
            bool like = item.id == f.liked;
            AddPoints(fi, like ? 5 : 2, like ? $"{f.name}: \"이거 정말 좋아해! 고마워!\"" : $"{f.name}: \"고마워, 잘 쓸게.\"");
            // 답례: 좋아하는 선물이면 값의 1.5배, 아니면 절반을 돈으로
            int back = Mathf.RoundToInt(item.basePrice * (like ? 1.5f : 0.5f));
            gm.Player.money += back;
        }

        // ---------------- 그리기 ----------------

        void OnGUI()
        {
            if (!NorthUtil.InNorth) return;
            var cam = Camera.main;
            UIStyle.Begin();
            GUI.depth = -15;
            float scale = UIStyle.Scale;
            // 말풍선
            if (cam != null)
                foreach (var b in bubbles)
                {
                    var sp = cam.WorldToScreenPoint(b.world);
                    if (sp.z < 0f) continue;
                    float x = sp.x / scale, y = (Screen.height - sp.y) / scale;
                    float w = Mathf.Max(30f, b.text.Length * 9f + 10f);
                    var r = new Rect(x - w / 2, y - 16, w, 16);
                    GUI.Box(r, GUIContent.none, UIStyle.Box);
                    GUI.Label(r, b.text, UIStyle.CenterRich);
                }
            // 주요 인물 하트 (말 걸 수 있는 거리일 때)
            var pc = PlayerController.Instance;
            if (cam != null && pc != null && pc.TalkTarget != null && !UIState.Blocking)
            {
                int fi = FriendIndex(pc.TalkTarget.DisplayName);
                if (fi >= 0)
                {
                    var sp = cam.WorldToScreenPoint(pc.TalkTarget.transform.position + new Vector3(0f, 2.1f, 0f));
                    int h = Hearts(Friends[fi].name);
                    GUI.Label(new Rect(sp.x / scale - 40, (Screen.height - sp.y) / scale - 10, 80, 14),
                        $"<color=#ff7a8a>{new string('♥', h)}</color><color=#9a8fa8>{new string('♡', 3 - h)}</color>  <size=8>G 선물</size>", UIStyle.CenterRich);
                }
            }
            if (wheelOpen) DrawWheel();
            if (photo) DrawPhotoFrame();
            if (flash > 0f)
            {
                var old = GUI.color; GUI.color = new Color(1f, 1f, 1f, flash / 0.3f);
                GUI.DrawTexture(new Rect(0, 0, UIStyle.VirtualWidth, UIStyle.VirtualHeight), Texture2D.whiteTexture);
                GUI.color = old;
            }
        }

        void DrawWheel()
        {
            float cx = UIStyle.VirtualWidth / 2f, cy = UIStyle.VirtualHeight / 2f, R = 70f;
            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0.05f, 0.4f);
            GUI.DrawTexture(new Rect(0, 0, UIStyle.VirtualWidth, UIStyle.VirtualHeight), Texture2D.whiteTexture);
            GUI.color = old;
            for (int i = 0; i < Reactions.Length; i++)
            {
                float a = -Mathf.PI / 2f + i * Mathf.PI * 2f / Reactions.Length;
                var r = new Rect(cx + Mathf.Cos(a) * R - 34, cy + Mathf.Sin(a) * R - 13, 68, 26);
                bool known = Knows(i);
                GUI.Box(r, GUIContent.none, UIStyle.Box);
                string label = known ? Reactions[i].name : "???";
                if (i == wheelSel) label = $"<color=#ffde59><b>▶ {label}</b></color>";
                else if (!known) label = $"<color=#8a8296>{label}</color>";
                GUI.Label(r, label, UIStyle.CenterRich);
            }
            GUI.Label(new Rect(cx - 80, cy - 10, 160, 20), "<b>리액션</b>", UIStyle.CenterRich);
            GUI.Label(new Rect(cx - 120, cy + R + 22, 240, 16), "← → 고르기 · Space 하기 · R/Esc 닫기", UIStyle.CenterRich);
        }

        void DrawPhotoFrame()
        {
            float w = UIStyle.VirtualWidth, h = UIStyle.VirtualHeight, b = 10f;
            var wood = new Color(0.45f, 0.29f, 0.17f); var light = new Color(0.66f, 0.46f, 0.28f);
            var old = GUI.color;
            GUI.color = wood;
            GUI.DrawTexture(new Rect(0, 0, w, b), Texture2D.whiteTexture); GUI.DrawTexture(new Rect(0, h - b - 14, w, b + 14), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, 0, b, h), Texture2D.whiteTexture); GUI.DrawTexture(new Rect(w - b, 0, b, h), Texture2D.whiteTexture);
            GUI.color = light;
            GUI.DrawTexture(new Rect(b, b, w - 2 * b, 2), Texture2D.whiteTexture); GUI.DrawTexture(new Rect(b, h - b - 16, w - 2 * b, 2), Texture2D.whiteTexture);
            GUI.color = old;
            var gm = GameManager.I;
            string place = MapRoot.Current != null ? (WorldIndex.Get() != null ? WorldIndex.Get().MapName(MapRoot.Current.mapId) : MapRoot.Current.mapId) : "";
            string when = gm != null ? $"{gm.Clock.day}일째" : "";
            GUI.Label(new Rect(b, h - b - 13, w - 2 * b, 14), $"<color=#f4e6c8>Guild & Cargo · 북유럽 · {place} · {when}</color>", UIStyle.CenterRich);
            if (Event.current.type == EventType.Repaint)
            {
                photoFrames++;
                if (photoFrames == 2)
                {
                    string dir = Path.Combine(Application.persistentDataPath, "Photos");
                    Directory.CreateDirectory(dir);
                    photoPath = Path.Combine(dir, $"north_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
                    ScreenCapture.CaptureScreenshot(photoPath);   // 이번 프레임(액자 포함)이 그려진 뒤 저장됨
                }
                else if (photoFrames >= 4)
                {
                    photo = false; flash = 0.3f;
                    if (!AudioManager.Muted) sfx.PlayOneShot(shutter, 0.7f);
                    Notify.Show("찰칵! 사진을 저장했다: " + photoPath, 4f);
                }
            }
        }
    }
}
