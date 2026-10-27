using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 베네치아 사람들과 어울리기 (2026-10-27, 제안). 베네치아·본토 서부 맵에서만. 공용 코드는 고치지 않는다.
    /// - 친밀도 (주요 8명): 하루 한 번 말 걸면 +1, 선물(G) +2, 좋아하는 선물 +5. 하트 3칸 (4·10·20점).
    ///   하트 1: 다정한 인사 + 좋아하는 물건 귀띔 / 하트 2: R 동작을 가르쳐 줌 / 하트 3: 특별한 선물(150냥·신용 +1·기념품 이야기).
    /// - 가까이 가면 이름 위에 하트가 보인다. 선물은 짐에서 골라 준다(방향키·Space).
    /// - 친구 수첩(F): 8명의 하트·좋아하는 물건(하트 1 이후)·다음 보상.
    /// - 사진(P): 금빛 액자에 장소·날짜를 넣어 Application.persistentDataPath/Photos에 저장하고 잠깐 보여 준다.
    /// 말 건 것은 공용 InputReader.InteractPressed() + PlayerController.TalkTarget 이름으로 안다(대화 내용은 공용 그대로).
    /// 진행: VeniceSave (friend:이름 점수, talk:이름_day, gift:이름_day, friendlv:이름 받은 하트 보상).
    /// </summary>
    [DefaultExecutionOrder(900)]
    public class VeniceSocial : MonoBehaviour
    {
        public struct Friend { public string name, liked, likedName, l1, l2, l3, teach, keepsake; }

        public static readonly Friend[] Friends =
        {
            new Friend { name = "여관 주인", liked = "venice_wine", likedName = "포도주", teach = "dance", keepsake = "여관 열쇠 모양 장식",
                l1 = "또 왔구려. 자네 발소리는 이제 계단에서부터 알겠소.", l2 = "카니발 밤에 추던 춤이 있지. 이렇게 어깨를 흔드는 거요!", l3 = "단골에게만 주는 여관 열쇠 장식이오. 어느 방이든 쉬어 가시오." },
            new Friend { name = "상인회 서기", liked = "venice_glass_art", likedName = "유리 장식", teach = "stretch", keepsake = "상인회 인장",
                l1 = "장부에 자네 이름이 자주 오르는군.", l2 = "장부를 오래 보면 허리가 굳지. 이렇게 쭉 펴 보게.", l3 = "상인회 인장을 하나 맡기겠네. 자네를 믿는다는 뜻이야." },
            new Friend { name = "과수원 일꾼", liked = "venice_salt", likedName = "석호 소금", teach = "love", keepsake = "말린 레몬 꾸러미",
                l1 = "레몬 향이 나지? 방금 따 온 거야.", l2 = "나무에 꽃이 피면 이렇게 가슴이 콩닥거려!", l3 = "올해 제일 잘 익은 레몬을 말린 거야. 먼 길에 챙겨 가." },
            new Friend { name = "유리 장인", liked = "venice_fruit", likedName = "과일 상자", teach = "spin", keepsake = "작은 유리 고양이",
                l1 = "불 앞에 오래 있으면 과일이 그리워.", l2 = "녹인 유리는 이렇게 빙글빙글 돌려야 둥글어지지.", l3 = "가마에서 남은 유리로 빚은 작은 고양이야. 너를 닮았지." },
            new Friend { name = "부라노 어부", liked = "venice_glass", likedName = "유리그릇", teach = "row", keepsake = "부라노 그물추",
                l1 = "오늘 물때가 좋아. 네 덕인가 봐.", l2 = "노는 팔이 아니라 허리로 젓는 거야. 따라 해 봐!", l3 = "할아버지 때부터 쓰던 그물추야. 물길에서 길 잃지 말라고." },
            new Friend { name = "창고지기", liked = "venice_fish", likedName = "절인 생선", teach = "nap", keepsake = "창고 열쇠 꾸러미",
                l1 = "짐 정리가 깔끔해졌구먼.", l2 = "짐 다 부리면 이렇게 잠깐 꾸벅 조는 게 최고지.", l3 = "옛 창고 열쇠 꾸러미야. 이젠 안 쓰지만 행운이 붙어 있다네." },
            new Friend { name = "곤돌라 사공", liked = "venice_lace", likedName = "레이스 손수건", teach = null, keepsake = "사공의 리본",
                l1 = "노 젓는 박자에 네 이름을 붙여 볼까.", l2 = "고맙다는 뜻이야. 뱃삯 모아 둔 걸 조금 나눌게.", l3 = "우리 사공 모자에 매는 붉은 리본이야. 이제 너도 물길 식구야." },
            new Friend { name = "레이스 뜨는 아주머니", liked = "venice_fish", likedName = "절인 생선", teach = null, keepsake = "작은 레이스 꽃",
                l1 = "손님 오면 바늘이 더 빨라진다우.", l2 = "실값에 보태라고 주는 거야. 사양 말고.", l3 = "밤새 뜬 레이스 꽃이야. 가방에 달고 다녀." },
        };
        static readonly int[] Need = { 4, 10, 20 };

        public static int Points(string n) => Mathf.RoundToInt(VeniceSave.Get("friend:" + n));
        public static int Hearts(string n) { int p = Points(n), h = 0; foreach (var v in Need) if (p >= v) h++; return h; }
        static int FriendIndex(string name) { for (int i = 0; i < Friends.Length; i++) if (Friends[i].name == name) return i; return -1; }
        static string HeartText(int h) => new string('♥', h) + new string('♡', 3 - h);

        bool book, picking, photo;
        int pickSel, pickFriend, photoFrames, lastTalkFrame = -1;
        readonly List<ItemData> pickItems = new List<ItemData>();
        string photoPath;
        Texture2D preview;
        float previewUntil, flash;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (GameManager.I != null && GameManager.I.GetComponent<VeniceSocial>() == null)
                GameManager.I.gameObject.AddComponent<VeniceSocial>();
        }

        static bool Key(char k)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            switch (k)
            {
                case 'G': return kb.gKey.wasPressedThisFrame;
                case 'F': return kb.fKey.wasPressedThisFrame;
                case 'P': return kb.pKey.wasPressedThisFrame;
                case '<': return kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame;
                case '>': return kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame;
            }
            return false;
#else
            return false;
#endif
        }

        void Update()
        {
            var pc = PlayerController.Instance;
            if (!VeniceUtil.InVenice || pc == null) { if (book || picking) Close(); return; }

            if (picking)
            {
                if (Key('<')) pickSel = (pickSel + pickItems.Count - 1) % pickItems.Count;
                if (Key('>')) pickSel = (pickSel + 1) % pickItems.Count;
                if (InputReader.InteractPressed()) { var item = pickItems[pickSel]; Close(); Give(pickFriend, item); }
                else if (InputReader.CancelPressed() || Key('G')) Close();
                return;
            }
            if (book)
            {
                if (Key('F') || InputReader.CancelPressed() || InputReader.InteractPressed()) Close();
                return;
            }
            if (UIState.Blocking) return;

            // 말 걸기 (대화창은 공용이 연다. 같은 프레임의 TalkTarget으로 누구인지 안다)
            if (InputReader.InteractPressed() && pc.TalkTarget != null && lastTalkFrame != Time.frameCount)
            {
                lastTalkFrame = Time.frameCount;
                int fi = FriendIndex(pc.TalkTarget.DisplayName);
                if (fi >= 0) Talked(fi);
            }
            if (Key('G') && pc.TalkTarget != null)
            {
                int fi = FriendIndex(pc.TalkTarget.DisplayName);
                if (fi >= 0) OpenGift(fi);
                else Notify.Show("이 사람에게는 선물을 줄 수 없다.", 2f);
            }
            if (Key('F')) { book = true; UIState.Blocking = true; Sfx.Play("open", .6f); }
            if (Key('P')) TakePhoto();
        }

        void Close()
        {
            book = picking = false;
            UIState.Release();
        }

        // ---------------- 친밀도 ----------------
        static void Talked(int fi)
        {
            var f = Friends[fi];
            if (VeniceSave.DoneToday("talk:" + f.name)) return;
            VeniceSave.MarkToday("talk:" + f.name);
            AddPoints(fi, 1);
        }

        static void AddPoints(int fi, int pts)
        {
            var f = Friends[fi];
            int before = Hearts(f.name);
            VeniceSave.Set("friend:" + f.name, Points(f.name) + pts);
            int after = Hearts(f.name);
            if (after > before) Reward(fi, after);
        }

        static void Reward(int fi, int level)
        {
            var f = Friends[fi];
            if (VeniceSave.Get("friendlv:" + f.name) >= level) return;
            VeniceSave.Set("friendlv:" + f.name, level);
            var gm = GameManager.I;
            Sfx.Play("levelup", .7f);
            string title = $"{f.name} {HeartText(level)}";
            if (level == 1) { Notify.Show($"{f.name}와(과) 가까워졌다! 좋아하는 것: {f.likedName}", 4f); return; }
            if (level == 2)
            {
                string taught = f.teach != null ? VeniceReactions.Teach(f.teach) : null;
                if (taught == null) gm.Player.money += 50;
                Notify.Show(taught != null ? $"{f.name}: \"{f.l2}\"  — 동작 '{taught}'을(를) 배웠다! (R)" : $"{f.name}: \"{f.l2}\"  (50냥)", 6f);
                return;
            }
            gm.Player.money += 150;
            gm.Player.credit += 1;
            VeniceSave.Set("keepsake:" + f.name, 1f);
            Notify.Show($"{title}  \"{f.l3}\"  — {f.keepsake}·150냥·신용 +1", 7f);
        }

        /// <summary>대화창이 열린 뒤 하트 인사를 덧붙이고 싶을 때 쓰는 한 줄 (하트 1 이상)</summary>
        public static string Greeting(string name)
        {
            int fi = FriendIndex(name);
            if (fi < 0) return null;
            int h = Hearts(name);
            return h >= 1 ? Friends[fi].l1 : null;
        }

        void OpenGift(int fi)
        {
            var f = Friends[fi];
            if (VeniceSave.DoneToday("gift:" + f.name)) { Notify.Show($"{f.name}에게는 오늘 이미 선물했다.", 2.5f); return; }
            pickItems.Clear();
            foreach (var kv in GameManager.I.Player.cargo.Items)
                if (kv.Value > 0 && kv.Key != null && !kv.Key.questItem) pickItems.Add(kv.Key);
            if (pickItems.Count == 0) { Notify.Show("짐에 줄 만한 물건이 없다.", 2.5f); return; }
            pickItems.Sort((a, b) => (b.id == f.liked).CompareTo(a.id == f.liked));   // 좋아하는 물건을 앞에
            pickFriend = fi; pickSel = 0; picking = true;
            UIState.Blocking = true;
        }

        /// <summary>점검용: 이름으로 선물</summary>
        public static void TestGift(string name, ItemData item) { int fi = FriendIndex(name); if (fi >= 0 && GameManager.I.Player.cargo.Count(item) > 0) Give(fi, item); }

        static void Give(int fi, ItemData item)
        {
            var f = Friends[fi];
            if (!GameManager.I.Player.cargo.Remove(item, 1)) return;
            VeniceSave.MarkToday("gift:" + f.name);
            bool liked = item.id == f.liked;
            Sfx.Play(liked ? "success" : "coin", .8f);
            Notify.Show(liked ? $"{f.name}: \"{item.displayName}! 이걸 어떻게 알았어?\"  (친밀도 +5)" : $"{f.name}: \"고마워, 잘 쓸게.\"  (친밀도 +2)", 3.5f);
            AddPoints(fi, liked ? 5 : 2);
            VeniceReactions.NpcCheer(item.id == f.liked ? "heart" : "note");
        }

        // ---------------- 사진 ----------------
        void TakePhoto()
        {
            photo = true; photoFrames = 0;
            var dir = Path.Combine(Application.persistentDataPath, "Photos");
            Directory.CreateDirectory(dir);
            var gm = GameManager.I;
            photoPath = Path.Combine(dir, $"venice_{gm.Clock.day}_{System.DateTime.Now:HHmmss}.png");
        }

        void LateUpdate()
        {
            if (!photo) return;
            if (++photoFrames == 2) { ScreenCapture.CaptureScreenshot(photoPath); Sfx.Play("open", .8f); flash = 1f; }
            if (photoFrames > 2 && File.Exists(photoPath))
            {
                photo = false;
                preview = new Texture2D(2, 2);
                preview.LoadImage(File.ReadAllBytes(photoPath));
                previewUntil = Time.unscaledTime + 3f;
                Notify.Show("사진을 찍었다 (Photos 폴더)", 2.5f);
            }
            if (photoFrames > 120) photo = false;
        }

        // ---------------- 그리기 ----------------
        void OnGUI()
        {
            if (!VeniceUtil.InVenice) return;
            UIStyle.Begin();
            GUI.depth = -15;
            var vw = UIStyle.VirtualWidth; var vh = UIStyle.VirtualHeight;

            if (photo && photoFrames < 2) { DrawFrame(new Rect(0, 0, vw, vh), true); return; }   // 액자째 찍힌다
            if (flash > 0f)
            {
                flash -= Time.unscaledDeltaTime * 3f;
                var c = GUI.color; GUI.color = new Color(1, 1, 1, Mathf.Clamp01(flash) * .7f);
                GUI.DrawTexture(new Rect(0, 0, vw, vh), Texture2D.whiteTexture); GUI.color = c;
            }
            if (preview != null && Time.unscaledTime < previewUntil)
            {
                float w = 150, h = w * preview.height / Mathf.Max(1, preview.width);
                var r = new Rect(vw - w - 14, vh - h - 40, w, h);
                GUI.DrawTexture(new Rect(r.x - 3, r.y - 3, w + 6, h + 6), Texture2D.whiteTexture);
                GUI.DrawTexture(r, preview);
            }

            DrawHearts();
            if (picking) DrawPicker(vw, vh);
            if (book) DrawBook(vw, vh);
        }

        static void DrawFrame(Rect r, bool caption)
        {
            var c = GUI.color;
            var gold = new Color(.86f, .66f, .25f, 1f); var dark = new Color(.42f, .26f, .1f, 1f);
            float t = 10;
            GUI.color = dark;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t + 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t - 2, r.width, t + 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, t + 2, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - t - 2, r.y, t + 2, r.height), Texture2D.whiteTexture);
            GUI.color = gold;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), Texture2D.whiteTexture);
            foreach (var p in new[] { new Vector2(r.x, r.y), new Vector2(r.xMax - 16, r.y), new Vector2(r.x, r.yMax - 16), new Vector2(r.xMax - 16, r.yMax - 16) })
            {
                GUI.color = dark; GUI.DrawTexture(new Rect(p.x, p.y, 16, 16), Texture2D.whiteTexture);
                GUI.color = new Color(.98f, .84f, .45f); GUI.DrawTexture(new Rect(p.x + 4, p.y + 4, 8, 8), Texture2D.whiteTexture);
            }
            GUI.color = c;
            if (caption && GameManager.I != null)
            {
                var place = MapRoot.Current != null ? MapRoot.Current.displayName : "베네치아";
                var box = new Rect(r.center.x - 90, r.yMax - 30, 180, 18);
                GUI.color = new Color(.2f, .12f, .06f, .85f); GUI.DrawTexture(box, Texture2D.whiteTexture); GUI.color = c;
                GUI.Label(box, $"<color=#FCE6A8>{place} · {GameManager.I.Clock.day + 1}일째</color>", UIStyle.CenterRich);
            }
        }

        void DrawHearts()
        {
            var pc = PlayerController.Instance;
            if (pc == null || Camera.main == null) return;
            foreach (var it in Interactable.All)
            {
                if (it == null) continue;
                int fi = FriendIndex(it.DisplayName);
                if (fi < 0) continue;
                if ((it.transform.position - pc.transform.position).sqrMagnitude > 4.5f * 4.5f) continue;
                var sp = Camera.main.WorldToScreenPoint(it.transform.position + new Vector3(0, 2.3f, 0));
                var p = new Vector2(sp.x / UIStyle.Scale, (Screen.height - sp.y) / UIStyle.Scale);
                int h = Hearts(Friends[fi].name);
                GUI.Label(new Rect(p.x - 30, p.y - 8, 60, 14), $"<color=#F05A78>{new string('♥', h)}</color><color=#FFFFFF88>{new string('♡', 3 - h)}</color>", UIStyle.CenterRich);
            }
        }

        void DrawPicker(float vw, float vh)
        {
            var f = Friends[pickFriend];
            float w = 260, h = 60;
            var r = new Rect((vw - w) / 2f, vh - h - 70, w, h);
            GUI.Box(r, "", UIStyle.Box);
            GUI.Label(new Rect(r.x + 10, r.y + 6, w - 20, 16), $"{f.name}에게 선물  {HeartText(Hearts(f.name))}", UIStyle.Rich);
            var it = pickItems[pickSel];
            bool liked = it.id == f.liked && Hearts(f.name) >= 1;
            GUI.Label(new Rect(r.x + 10, r.y + 24, w - 20, 16),
                $"◀  {it.displayName} ×{GameManager.I.Player.cargo.Count(it)}{(liked ? "  <color=#F05A78>♥좋아함</color>" : "")}  ▶", UIStyle.CenterRich);
            GUI.Label(new Rect(r.x + 10, r.y + 42, w - 20, 14), "<size=9>방향키 고르기 · Space 주기 · G/Esc 그만</size>", UIStyle.CenterRich);
        }

        static GUIStyle oneLine;
        static GUIStyle OneLine => oneLine ??= new GUIStyle(UIStyle.Rich) { wordWrap = false, clipping = TextClipping.Overflow };

        void DrawBook(float vw, float vh)
        {
            float w = 360, h = 52 + Friends.Length * 20;
            var r = new Rect((vw - w) / 2f, (vh - h) / 2f, w, h);
            GUI.Box(r, "", UIStyle.Box);
            GUI.Label(new Rect(r.x + 12, r.y + 6, w - 24, 16), "베네치아 친구 수첩", OneLine);
            GUI.Label(new Rect(r.x + 12, r.y + 23, w - 24, 14), "<size=9><color=#CFC6B8>하루 한 번 말 걸기 +1 · G 선물 +2 · 좋아하는 것 +5 · 하트 2에서 동작을 배운다</color></size>", OneLine);
            for (int i = 0; i < Friends.Length; i++)
            {
                var f = Friends[i];
                int hts = Hearts(f.name), pts = Points(f.name);
                string move = f.teach != null ? VeniceReactions.NameOf(f.teach) : null;
                string next = hts >= 3 ? $"기념품: {f.keepsake}"
                            : hts == 2 ? $"다음: 특별한 선물 ({pts}/{Need[2]})"
                            : $"다음: {(hts == 1 ? (move != null ? $"동작 '{move}'" : "답례") : "가까워지기")} ({pts}/{Need[hts]})";
                string liked = hts >= 1 ? f.likedName : "?";
                bool today = VeniceSave.DoneToday("talk:" + f.name);
                float y = r.y + 42 + i * 20;
                GUI.Label(new Rect(r.x + 12, y, 44, 18), $"<color=#F05A78>{HeartText(hts)}</color>", OneLine);
                GUI.Label(new Rect(r.x + 58, y, 120, 18), f.name + (today ? " <size=9>✓</size>" : ""), OneLine);
                GUI.Label(new Rect(r.x + 180, y + 2, w - 192, 16), $"<size=9>좋아함 {liked} · {next}</size>", OneLine);
            }
        }
    }
}
