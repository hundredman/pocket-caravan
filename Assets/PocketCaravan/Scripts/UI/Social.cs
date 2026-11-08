using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 사람들과 어울리기 (전 지역 공용, 2026-11-04 통합: 북유럽 NorthSocial + 베네치아 VeniceSocial을 합침).
    /// - 친밀도: 지역마다 주요 8명. 하루 한 번 말 걸면 +1, 선물(G) +2, 좋아하는 선물 +5. 하트 3칸 (4·10·20점).
    ///   하트 1: 가까워짐 + 좋아하는 물건 귀띔 / 하트 2: 리액션을 가르쳐 줌(없으면 답례 50냥) / 하트 3: 기념품 + 50냥 + 신용 +1.
    /// - 선물(G): 말 걸 수 있는 주요 인물 앞에서. 짐 목록에서 방향키로 골라 Space (좋아하는 것이 맨 앞). 하루 한 번. 돈 답례는 없다.
    /// - 친구 수첩(F): 지금 지역 8명의 하트·좋아하는 것(하트 1 이후)·다음 보상. ←→로 다른 지역 쪽도 본다.
    /// - 사진(P): 지역 색 액자(북 나무 · 베 금빛 · 고려 단청 · 아라비아 타일)에 장소·날짜를 넣어 Photos 폴더에 저장, 잠깐 보여 준다.
    /// 친구는 그 지역 맵에서만 친구로 센다 (이름이 같은 다른 지역 사람과 섞이지 않게).
    /// 저장: 공용 메모 "social:지역:이름:pts|lv|talk|gift". 통합 전 저장(north:friend:*, venice:friend:* 등)도 읽는다.
    /// </summary>
    [DefaultExecutionOrder(900)]
    public class Social : MonoBehaviour
    {
        public struct Friend { public Reactions.Region region; public string name, liked, likedName, l1, l2, l3, teach, keepsake; }

        static Friend F(Reactions.Region r, string name, string liked, string likedName, string teach, string keepsake, string l1, string l2, string l3) =>
            new Friend { region = r, name = name, liked = liked, likedName = likedName, teach = teach, keepsake = keepsake, l1 = l1, l2 = l2, l3 = l3 };

        const Reactions.Region N = Reactions.Region.North, V = Reactions.Region.Venice, G = Reactions.Region.Goryeo, A = Reactions.Region.Arabia;

        public static readonly Friend[] Friends =
        {
            // ---------- 북유럽 (기우) ----------
            F(N, "여관 할머니", "north_cod", "얼음 대구", "dance", "생선 수프 단지",
                "또 왔구나! 너만 오면 화롯불이 더 따뜻한 것 같아.", "젊을 적 축제에서 추던 춤이 있지. 자, 이렇게 발을 구르는 거야!", "우리 집 비법 생선 수프 단지야. 먼 길 갈 때 먹으렴."),
            F(N, "목수 하르", "north_fur", "모피", "surprise", "나무 순록",
                "자네 수레 바퀴 소리만 들어도 알겠어. 잘 굴리고 있군!", "썰매 날이 얼음에 박히면 이렇게 놀라지! 하하, 따라 해 보게.", "남은 나무로 깎은 작은 순록이야. 행운을 가져다준다네."),
            F(N, "순록 목동 할머니", "north_salt", "남쪽 소금", "joy", "오래 묵힌 고원 치즈",
                "순록들이 네 발소리를 알아듣는구나.", "순록이 새끼를 낳던 날, 우리는 이렇게 팔짝 뛰었단다!", "고원 치즈 중에 제일 오래 묵힌 거야. 아껴 먹어."),
            F(N, "룬 지기 노인", "north_amber", "호박", "nap", "호박 부적",
                "룬을 읽는 눈이 생겼구먼.", "룬은 서두르면 안 읽혀. 가끔은 이렇게 눈 감고 꾸벅… 쉬어야 보이지.", "옛 상인들이 지니던 호박 부적일세. 길을 잃지 않게 해 주지."),
            F(N, "집 보는 남편", "north_cheese", "순록 치즈", null, "털목도리",
                "집안일 요령이 좀 늘었어. 버터도 이제 안 넘쳐!", "고마워서 그래, 이거 받아. 아내가 캔 약초 판 돈이야.", "우리 부부가 함께 짠 털목도리야. 눈보라에도 따뜻할 거야."),
            F(N, "들일 하는 아내", "north_herb", "온천 약초", null, "옛 은화",
                "밭일하다 너 지나가는 거 봤어!", "이건 남편이 만든 버터 과자 판 돈이야. 맛은 장담 못 해.", "밭 귀퉁이에서 찾은 옛 은화야. 너한테 더 쓸모 있을 거야."),
            F(N, "얄 시그리드", "north_salmon", "황금 연어", null, "얄의 보물",
                "비르카의 이름 있는 상인이 되어 가는군.", "길드에 자네 이름을 올려 두었네. 다들 자네를 믿어.", "얄의 회관 보물 창고에서 꺼낸 것이네. 자네 몫일세."),
            F(N, "초소 대장", "north_mead", "꿀술", null, "초소 경비의 표식",
                "오, 단골이군! 초소 문은 언제나 열려 있어.", "남쪽 상인들한테 들은 소식이야, 받아 둬. 그리고 이건 수고비.", "초소 경비들이 모은 감사의 표시야. 남쪽 길 조심하라고."),

            // ---------- 베네치아 (윤채) ----------
            F(V, "여관 주인", "venice_wine", "포도주", "dance", "여관 열쇠 모양 장식",
                "또 왔구려. 자네 발소리는 이제 계단에서부터 알겠소.", "카니발 밤에 추던 춤이 있지. 이렇게 어깨를 흔드는 거요!", "단골에게만 주는 여관 열쇠 장식이오. 언제든 돌아오시오."),
            F(V, "상인회 서기", "venice_glass_art", "유리 장식", "surprise", "상인회 인장",
                "장부에 자네 이름이 자주 오르는군.", "장부에서 큰 이익이 보이면 나도 모르게 이렇게 놀란다네!", "상인회 인장을 하나 맡기겠네. 자네라면 믿을 수 있지."),
            F(V, "과수원 일꾼", "venice_salt", "석호 소금", "love", "말린 레몬 꾸러미",
                "레몬 향이 나지? 방금 따 온 거야.", "나무에 꽃이 피면 이렇게 가슴이 콩닥거려!", "올해 제일 잘 익은 레몬을 말린 거야. 먼 길에 챙겨 가."),
            F(V, "유리 장인", "venice_fruit", "과일 상자", "joy", "작은 유리 고양이",
                "불 앞에 오래 있으면 과일이 그리워.", "유리가 둥글게 잘 나오면 이렇게 팔짝 뛴다니까!", "가마에서 남은 유리로 빚은 작은 고양이야. 너 닮았지?"),
            F(V, "부라노 어부", "venice_glass", "유리그릇", "row", "부라노 그물추",
                "오늘 물때가 좋아. 네 덕인가 봐.", "노는 팔이 아니라 허리로 젓는 거야. 따라 해 봐!", "할아버지 때부터 쓰던 그물추야. 물길에서 지켜 줄 거야."),
            F(V, "창고지기", "venice_fish", "절인 생선", "nap", "창고 열쇠 꾸러미",
                "짐 정리가 깔끔해졌구먼.", "짐 다 부리면 이렇게 잠깐 꾸벅 조는 게 최고지.", "옛 창고 열쇠 꾸러미야. 이젠 안 쓰지만 행운이 깃들었지."),
            F(V, "곤돌라 사공", "venice_lace", "레이스 손수건", null, "사공의 리본",
                "노 젓는 박자에 네 이름을 붙여 볼까.", "고맙다는 뜻이야. 뱃삯 모아 둔 걸 조금 나눌게.", "우리 사공 모자에 매는 붉은 리본이야. 석호 어디서든 알아봐 줄 거야."),
            F(V, "레이스 뜨는 아주머니", "venice_fish", "절인 생선", null, "작은 레이스 꽃",
                "손님 오면 바늘이 더 빨라진다우.", "실값에 보태라고 주는 거야. 사양 말고.", "밤새 뜬 레이스 꽃이야. 가방에 달고 다녀."),

            // ---------- 고려 (은샘 · 2026-11-04 사용자 대사/기념품 승인) ----------
            F(G, "개경 객주 돈돌", "najeon", "나전칠기", "dance", "단청무늬 장부갈피",
                "자네 얼굴도 이제 눈에 익네. 오늘은 어떤 짐인가?", "일을 마쳤으니 어깨 좀 펴세. 자, 이렇게 덩실!", "장부 사이에 끼워 쓰게. 펼칠 때마다 개경 생각도 하고."),
            F(G, "나루 마을 촌장", "grain", "곡식", null, "나루 마을 부적",
                "왔는가. 강 건너 소식이 궁금하던 참이네.", "오가며 말벗이 되어 주니 고맙네. 길 떠날 때 보태 쓰게.", "나루에서 엮은 작은 부적일세. 우리 마을을 기억해 주게."),
            F(G, "나루 우물가 아낙", "salt", "소금", "love", "매듭 노리개",
                "또 만났네요. 우물가에서 잠깐 쉬었다 가요.", "반가운 사람을 보면 손이 절로 모여요. 이렇게요.", "틈틈이 엮은 노리개예요. 짐끈에 달면 잘 보이겠지요?"),
            F(G, "뱃사공", "firewood", "땔감", "joy", "작은 나무노",
                "또 길을 나서나. 오늘은 어느 포구까지 가려는가?", "배가 무사히 닿으면 나도 모르게 뛰게 된다네. 영차!", "남는 나무로 깎았네. 작아도 노 모양은 제대로지?"),
            F(G, "닥나무 마을 촌장", "ginseng", "인삼", "surprise", "닥종이 부채",
                "자네가 오니 종이 말리는 마당도 덜 심심하군.", "종이를 들췄는데 구멍이 났더군. 아이쿠, 깜짝이야!", "우리 마을 종이로 접은 작은 부채일세. 닥목 생각하며 간직하게."),
            F(G, "산록 약재 장수", "paper", "종이", "nap", "약초 향주머니",
                "이젠 얼굴만 봐도 알겠어요. 오늘은 어떤 길로 왔어요?", "약초를 뒤집어 놓고 잠깐 눈을 붙이지요. 꾸벅… 이런, 손님 앞에서.", "말린 잎을 조금 넣었어요. 주머니를 열면 산 냄새가 날 거예요."),
            F(G, "장단 곡물 상인", "transit_driedfish", "말린 생선", null, "장단 볏짚 참새",
                "들판을 지나왔소? 이삭이 제법 고개를 숙였지요.", "말린 생선 덕에 밥상이 든든했소. 이번엔 내 마음을 받으시오.", "올해 볏짚으로 엮었소. 장단 생각이 나면 한번 꺼내 보시오."),
            F(G, "땔감 장수", "salt", "소금", null, "향나무 조각",
                "또 만났군. 오늘도 길 오느라 수고했네.", "소금 덕에 끼니가 한결 낫더군. 이번엔 내가 보탤 차례네.", "나뭇결이 고와 따로 챙겼네. 그냥 태우기에는 아깝더군."),

            // ---------- 아라비아 (기우 · 2026-11-07 담당 인계 후 대사·선물 확정. 좋아하는 선물은 11/5 새 품목 6개로) ----------
            F(A, "여관 주인 마리암", "arabia_coffee", "커피 열매 자루", "dance", "마리암의 커피 잔",
                "또 왔구나. 네 방은 늘 비워 두고 있어. 오늘은 어느 길로 왔니?", "대상이 무사히 돌아온 밤엔 손뼉을 치며 이렇게 춤을 춰. 너도 같이!", "할머니께 물려받은 커피 잔이야. 어디서 마셔도 여기 생각이 날 거야."),
            F(A, "길잡이 하산", "arabia_dates", "대추야자", "surprise", "하산의 별자리 표",
                "모래바람 속에서도 길을 잃지 않는구나. 이젠 내가 배울 차례인가.", "언덕 너머로 오아시스가 보이면 몇 번을 봐도 이렇게 놀라게 돼!", "내가 쓰던 별자리 표다. 밤 사막에선 별이 길이야."),
            F(A, "짐꾼 힌드바드", "arabia_leather", "낙타 가죽", "joy", "힌드바드의 낡은 짐끈",
                "짐 싣는 솜씨가 늘었네! 처음 봤을 땐 반쪽도 못 들더니.", "짐을 다 내려놓으면 팔짝 뛰고 싶어진다니까. 같이 뛰자!", "할아버지 때부터 쓰던 짐끈이야. 이걸로 묶으면 모래바람에도 안 풀려."),
            F(A, "빵 굽는 아주머니", "arabia_salt", "바닷소금 덩이", "love", "화덕 빵 도장",
                "빵 냄새 맡고 왔지? 막 꺼냈어. 하나 집어.", "맛있다는 말 한마디에 가슴이 이렇게 따뜻해져.", "우리 화덕 빵에 찍는 도장이야. 네 빵에도 찍어 주고 싶었어."),
            F(A, "공동 부엌 담당", "arabia_spice", "향신료 자루", "nap", "부엌의 향신료 단지",
                "부엌 일 거들어 줘서 다들 좋아해. 오늘도 한 솥 끓여야지.", "솥 앞에서 오래 기다릴 땐 이렇게 꾸벅 조는 게 최고야.", "대대로 쓰던 향신료 단지야. 반쯤 남았으니 아껴 써."),
            F(A, "직물 상인 라시드", "arabia_carpet", "양탄자", null, "물들인 비단 조각",
                "자네가 가져오는 물건은 늘 값이 좋아. 눈이 있어.", "좋은 거래였네. 내 몫에서 조금 떼어 주는 걸세, 사양 말게.", "타이즈 염색 장인이 물들인 비단 조각일세. 큰 장 날 두르면 멋질 게야."),
            F(A, "수레 장인 유수프", "arabia_pearl", "진주", null, "작은 수레바퀴",
                "수레 바퀴 소리가 경쾌하군. 관리 잘했네.", "수레 고친 값 남은 걸세. 지름길 모래에 바퀴 상하면 또 오게.", "처음 만든 수레의 바퀴를 작게 깎은 걸세. 먼 길에 행운을 비네."),
            F(A, "시장 상인 나디아", "arabia_frankincense", "유향 덩이", null, "사나 은팔찌",
                "사나 시장에서 네 얼굴 모르는 사람이 없어. 오늘은 뭘 들고 왔어?", "단골 덕에 장사가 잘돼. 이건 고마운 마음이야.", "사나 은세공 장인이 만든 팔찌야. 향유 냄새랑 잘 어울릴걸."),
        };

        static readonly int[] Need = { 4, 10, 20 };
        static readonly Reactions.Region[] Pages = { N, V, G, A };
        static string RegionName(Reactions.Region r) => r == N ? "북유럽" : r == V ? "베네치아" : r == G ? "고려" : "아라비아";

        // ---------------- 저장 ----------------
        static GameManager GM => GameManager.I;
        static string K(Friend f, string what) => $"social:{f.region}:{f.name}:{what}";
        static string Old(Friend f) => f.region == N ? "north:" : f.region == V ? "venice:" : null;
        static float Get(string key) => GM != null ? GM.GetMemo(key, 0f) : 0f;

        public static int Points(Friend f)
        {
            float p = Get(K(f, "pts"));
            var o = Old(f);
            if (o != null) p = Mathf.Max(p, Get(o + "friend:" + f.name));
            return Mathf.RoundToInt(p);
        }
        static int RewardedLevel(Friend f)
        {
            float l = Get(K(f, "lv"));
            var o = Old(f);
            if (o != null) l = Mathf.Max(l, Get(o + "friendlv:" + f.name));
            return Mathf.RoundToInt(l);
        }
        public static int Hearts(Friend f) { int p = Points(f), h = 0; foreach (var v in Need) if (p >= v) h++; return h; }
        static int Today => GM != null ? GM.Clock.day : 0;
        static bool DoneToday(Friend f, string what) => GM != null && GM.HasMemo(K(f, what)) && Mathf.RoundToInt(GM.GetMemo(K(f, what))) == Today;
        static void MarkToday(Friend f, string what) { if (GM != null) GM.SetMemo(K(f, what), Today); }

        /// <summary>지금 지역에서 이 이름의 친구 (없으면 -1)</summary>
        static int FriendIndex(string name)
        {
            if (string.IsNullOrEmpty(name)) return -1;
            var here = Reactions.Here;
            for (int i = 0; i < Friends.Length; i++) if (Friends[i].region == here && Friends[i].name == name) return i;
            return -1;
        }
        /// <summary>이름으로 (점검 도구용: 지금 지역 → 아무 지역 순)</summary>
        static int AnyIndex(string name)
        {
            int i = FriendIndex(name);
            if (i >= 0) return i;
            for (int j = 0; j < Friends.Length; j++) if (Friends[j].name == name) return j;
            return -1;
        }
        public static int Points(string name) { int i = AnyIndex(name); return i >= 0 ? Points(Friends[i]) : 0; }
        public static int Hearts(string name) { int i = AnyIndex(name); return i >= 0 ? Hearts(Friends[i]) : 0; }
        static string HeartText(int h) => new string('♥', h) + new string('♡', 3 - h);

        // ---------------- 상태 ----------------
        bool book, picking, photo;
        int pickSel, pickFriend, photoFrames, page, lastTalkFrame = -1;
        readonly List<ItemData> pickItems = new List<ItemData>();
        string photoPath;
        Texture2D preview;
        bool blockedBefore;
        float previewUntil, flash;
        string lastMap;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<Social>() != null) return;
            var go = new GameObject("Social");
            DontDestroyOnLoad(go);
            go.AddComponent<Social>();
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
            switch (k)
            {
                case 'G': return Input.GetKeyDown(KeyCode.G);
                case 'F': return Input.GetKeyDown(KeyCode.F);
                case 'P': return Input.GetKeyDown(KeyCode.P);
                case '<': return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.UpArrow);
                case '>': return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.DownArrow);
            }
            return false;
#endif
        }

        void Update()
        {
            var pc = PlayerController.Instance;
            var map = MapRoot.Current;
            if (pc == null || map == null || GM == null || GM.IsTravelling) { if (book || picking) Close(); return; }
            if (map.mapId != lastMap)
            {
                lastMap = map.mapId;
                if (!GM.HasMemo("social:tips")) { GM.SetMemo("social:tips", 1f); Notify.Show("R 리액션 · G 주요 인물에게 선물 · F 친구 수첩 · P 사진", 6f); }
            }

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
                if (Key('<')) page = (page + Pages.Length - 1) % Pages.Length;
                if (Key('>')) page = (page + 1) % Pages.Length;
                if (Key('F') || InputReader.CancelPressed() || InputReader.InteractPressed()) Close();
                return;
            }
            // 말 걸기: 공용 PlayerController가 먼저 돌아 대화창이 이번 프레임에 Blocking을 켜므로
            // "지난 프레임엔 막혀 있지 않았다"로 판단한다 (대화 넘기기 Space는 세지 않는다).
            bool wasFree = !blockedBefore;
            blockedBefore = UIState.Blocking;
            if (wasFree && InputReader.InteractPressed() && pc.TalkTarget != null && lastTalkFrame != Time.frameCount)
            {
                lastTalkFrame = Time.frameCount;
                int fi = FriendIndex(pc.TalkTarget.DisplayName);
                if (fi >= 0) Talked(fi);
            }
            if (UIState.Blocking) return;
            if (Key('G') && pc.TalkTarget != null)
            {
                int fi = FriendIndex(pc.TalkTarget.DisplayName);
                if (fi >= 0) OpenGift(fi);
                else Notify.Show("이 사람에게는 선물을 줄 수 없다. (주요 인물에게만)", 2f);
            }
            if (Key('F')) { page = System.Array.IndexOf(Pages, Reactions.Here); if (page < 0) page = 0; book = true; UIState.Blocking = true; Sfx.Play("open", .6f); }
            if (Key('P')) TakePhoto();
        }

        void Close() { book = picking = false; UIState.Release(); }

        // ---------------- 친밀도 ----------------
        static void Talked(int fi)
        {
            var f = Friends[fi];
            if (DoneToday(f, "talk")) return;
            MarkToday(f, "talk");
            AddPoints(fi, 1);
        }

        static void AddPoints(int fi, int pts)
        {
            var f = Friends[fi];
            int before = Hearts(f);
            GM.SetMemo(K(f, "pts"), Points(f) + pts);
            int after = Hearts(f);
            for (int lv = before + 1; lv <= after; lv++) Reward(fi, lv);
        }

        static void Reward(int fi, int level)
        {
            var f = Friends[fi];
            if (RewardedLevel(f) >= level) return;
            GM.SetMemo(K(f, "lv"), level);
            Sfx.Play("levelup", .7f);
            string title = $"{f.name} {HeartText(level)}";
            if (level == 1) { Notify.Show($"{title}  \"{f.l1}\"  — 좋아하는 것: {f.likedName}", 5f); return; }
            if (level == 2)
            {
                string taught = f.teach != null ? Reactions.Teach(f.teach) : null;
                if (taught == null) GM.Player.money += 50;
                Notify.Show(taught != null ? $"{title}  \"{f.l2}\"  — 리액션 '{taught}'을(를) 배웠다! (R)" : $"{title}  \"{f.l2}\"  (50냥)", 6f);
                return;
            }
            GM.Player.money += 50;
            GM.Player.credit += 1;
            GM.SetMemo(K(f, "keepsake"), 1f);
            Notify.Show($"{title}  \"{f.l3}\"  — {f.keepsake} · 50냥 · 신용 +1", 7f);
        }

        void OpenGift(int fi)
        {
            var f = Friends[fi];
            if (DoneToday(f, "gift")) { Notify.Show($"{f.name}에게는 오늘 이미 선물했다.", 2.5f); return; }
            pickItems.Clear();
            foreach (var kv in GM.Player.cargo.Items)
                if (kv.Value > 0 && kv.Key != null && !kv.Key.questItem) pickItems.Add(kv.Key);
            if (pickItems.Count == 0) { Notify.Show($"짐에 줄 만한 물건이 없다. ({f.name}이(가) 좋아하는 것: {(Hearts(f) >= 1 ? f.likedName : "?")})", 2.5f); return; }
            pickItems.Sort((a, b) => (b.id == f.liked).CompareTo(a.id == f.liked));   // 좋아하는 물건을 맨 앞에
            pickFriend = fi; pickSel = 0; picking = true;
            UIState.Blocking = true;
        }

        /// <summary>점검용: 이름으로 선물</summary>
        public static void TestGift(string name, ItemData item) { int fi = AnyIndex(name); if (fi >= 0 && GM.Player.cargo.Count(item) > 0) Give(fi, item); }

        static void Give(int fi, ItemData item)
        {
            var f = Friends[fi];
            if (!GM.Player.cargo.Remove(item, 1)) return;
            MarkToday(f, "gift");
            bool liked = item.id == f.liked;
            Sfx.Play(liked ? "success" : "coin", .8f);
            Notify.Show(liked ? $"{f.name}: \"{item.displayName}! 이걸 어떻게 알았어?\"  (친밀도 +5)" : $"{f.name}: \"고마워, 잘 쓸게.\"  (친밀도 +2)", 3.5f);
            AddPoints(fi, liked ? 5 : 2);
            Reactions.NpcCheer(liked ? "heart" : "note");
        }

        // ---------------- 사진 ----------------
        void TakePhoto()
        {
            photo = true; photoFrames = 0;
            var dir = Path.Combine(Application.persistentDataPath, "Photos");
            Directory.CreateDirectory(dir);
            photoPath = Path.Combine(dir, $"{Reactions.Here.ToString().ToLower()}_{GM.Clock.day}_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
        }

        void LateUpdate()
        {
            if (!photo) return;
            if (++photoFrames == 2) { ScreenCapture.CaptureScreenshot(photoPath); Sfx.Play("open", .8f); flash = 1f; }
            if (photoFrames > 2 && File.Exists(photoPath))
            {
                photo = false;
                if (preview != null) Destroy(preview);
                preview = new Texture2D(2, 2);
                preview.LoadImage(File.ReadAllBytes(photoPath));
                previewUntil = Time.unscaledTime + 3f;
                Notify.Show("찰칵! 사진을 찍었다 (Photos 폴더)", 2.5f);
            }
            if (photoFrames > 120) photo = false;
        }

        /// <summary>지역 액자 색: (바깥, 안쪽 띠, 모서리 점)</summary>
        static void FrameColors(Reactions.Region r, out Color outer, out Color inner, out Color dot)
        {
            switch (r)
            {
                case Reactions.Region.North: outer = new Color(.45f, .29f, .17f); inner = new Color(.66f, .46f, .28f); dot = new Color(.85f, .72f, .5f); break;   // 나무
                case Reactions.Region.Venice: outer = new Color(.42f, .26f, .1f); inner = new Color(.86f, .66f, .25f); dot = new Color(.98f, .84f, .45f); break;  // 금빛
                case Reactions.Region.Goryeo: outer = new Color(.14f, .34f, .3f); inner = new Color(.72f, .22f, .16f); dot = new Color(.95f, .78f, .3f); break;   // 단청 (녹청·주홍·황)
                default: outer = new Color(.1f, .22f, .48f); inner = new Color(.25f, .66f, .72f); dot = new Color(.96f, .9f, .74f); break;                        // 아라비아 타일 (남청·청록·모래)
            }
        }

        static void DrawFrame(Rect r)
        {
            FrameColors(Reactions.Here, out var outer, out var inner, out var dot);
            var c = GUI.color;
            float t = 10;
            GUI.color = outer;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t + 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t - 2, r.width, t + 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, t + 2, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - t - 2, r.y, t + 2, r.height), Texture2D.whiteTexture);
            GUI.color = inner;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), Texture2D.whiteTexture);
            if (Reactions.Here == Reactions.Region.Arabia || Reactions.Here == Reactions.Region.Goryeo)   // 타일·단청 무늬: 띠 위 작은 점
            {
                GUI.color = dot;
                for (float x = r.x + 20; x < r.xMax - 20; x += 16) { GUI.DrawTexture(new Rect(x, r.y + 3, 4, 4), Texture2D.whiteTexture); GUI.DrawTexture(new Rect(x, r.yMax - 7, 4, 4), Texture2D.whiteTexture); }
                for (float y = r.y + 20; y < r.yMax - 20; y += 16) { GUI.DrawTexture(new Rect(r.x + 3, y, 4, 4), Texture2D.whiteTexture); GUI.DrawTexture(new Rect(r.xMax - 7, y, 4, 4), Texture2D.whiteTexture); }
            }
            foreach (var p in new[] { new Vector2(r.x, r.y), new Vector2(r.xMax - 16, r.y), new Vector2(r.x, r.yMax - 16), new Vector2(r.xMax - 16, r.yMax - 16) })
            {
                GUI.color = outer; GUI.DrawTexture(new Rect(p.x, p.y, 16, 16), Texture2D.whiteTexture);
                GUI.color = dot; GUI.DrawTexture(new Rect(p.x + 4, p.y + 4, 8, 8), Texture2D.whiteTexture);
            }
            GUI.color = c;
            if (GM != null)
            {
                var place = MapRoot.Current != null ? MapRoot.Current.displayName : "";
                var box = new Rect(r.center.x - 100, r.yMax - 30, 200, 18);
                GUI.color = new Color(outer.r * .6f, outer.g * .6f, outer.b * .6f, .88f); GUI.DrawTexture(box, Texture2D.whiteTexture); GUI.color = c;
                GUI.Label(box, $"<color=#FCE6A8>{RegionName(Reactions.Here)} · {place} · {GM.Clock.day}일째</color>", UIStyle.CenterRich);
            }
        }

        // ---------------- 그리기 ----------------
        void OnGUI()
        {
            if (GM == null || MapRoot.Current == null) return;
            UIStyle.Begin();
            GUI.depth = -15;
            var vw = UIStyle.VirtualWidth; var vh = UIStyle.VirtualHeight;

            if (photo && photoFrames < 2) { DrawFrame(new Rect(0, 0, vw, vh)); return; }   // 액자째 찍힌다
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

            if (!UIState.Blocking) DrawHearts();
            if (picking) DrawPicker(vw, vh);
            if (book) DrawBook(vw, vh);
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
                var labels = MapRoot.Current != null ? MapRoot.Current.GetComponent<GoryeoPlaceLabels>() : null;
                if (labels != null && labels.OwnsOriginal)
                {
                    if (pc.TalkTarget == it) labels.DrawFriendHint(HeartText(Hearts(Friends[fi])));
                    continue;
                }
                if ((it.transform.position - pc.transform.position).sqrMagnitude > 4.5f * 4.5f) continue;
                var sp = Camera.main.WorldToScreenPoint(it.transform.position + new Vector3(0, 2.3f, 0));
                if (sp.z < 0f) continue;
                var p = new Vector2(sp.x / UIStyle.Scale, (Screen.height - sp.y) / UIStyle.Scale);
                int h = Hearts(Friends[fi]);
                string g = pc.TalkTarget == it ? " <size=9><color=#ffe08a>G</color> 선물</size>" : "";
                string text = $"<color=#F05A78>{new string('♥', h)}</color><color=#FFFFFF88>{new string('♡', 3 - h)}</color>{g}";
                float w = OneLine.CalcSize(new GUIContent(text)).x + 10f;
                var r = new Rect(p.x - w / 2f, p.y - 9, w, 15);
                if (g.Length > 0) GUI.Box(r, GUIContent.none, UIStyle.Box);
                GUI.Label(r, text, CenterLine);
            }
        }

        void DrawPicker(float vw, float vh)
        {
            var f = Friends[pickFriend];
            float w = 260, h = 60;
            var r = new Rect((vw - w) / 2f, vh - h - 70, w, h);
            GUI.Box(r, "", UIStyle.Box);
            GUI.Label(new Rect(r.x + 10, r.y + 6, w - 20, 16), $"{f.name}에게 선물  {HeartText(Hearts(f))}", UIStyle.Rich);
            var it = pickItems[pickSel];
            bool liked = it.id == f.liked && Hearts(f) >= 1;
            GUI.Label(new Rect(r.x + 10, r.y + 24, w - 20, 16),
                $"◀  {it.displayName} ×{GM.Player.cargo.Count(it)}{(liked ? "  <color=#F05A78>♥좋아함</color>" : "")}  ▶", UIStyle.CenterRich);
            GUI.Label(new Rect(r.x + 10, r.y + 42, w - 20, 14), "<size=9>방향키 고르기 · Space 주기 · G/Esc 그만</size>", UIStyle.CenterRich);
        }

        static GUIStyle oneLine, centerLine;
        // Same profile invalidation as the shared CuteUI paper styles / NetChat.
        static void LineStyles()
        {
            if(oneLine!=null&&centerLine!=null&&oneLine.font==UIStyle.RegularFont&&oneLine.fontSize==UIStyle.BodySize)return;
            oneLine=new GUIStyle(UIStyle.Rich) { wordWrap=false, clipping=TextClipping.Overflow };
            centerLine=new GUIStyle(UIStyle.CenterRich) { wordWrap=false, clipping=TextClipping.Overflow };
        }
        static GUIStyle OneLine { get { LineStyles(); return oneLine; } }
        static GUIStyle CenterLine { get { LineStyles(); return centerLine; } }

        void DrawBook(float vw, float vh)
        {
            var region = Pages[page];
            var list = new List<Friend>();
            foreach (var f in Friends) if (f.region == region) list.Add(f);
            float w = 380, h = 62 + list.Count * 20;
            var r = new Rect((vw - w) / 2f, (vh - h) / 2f, w, h);
            GUI.Box(r, "", UIStyle.Box);
            string here = region == Reactions.Here ? "" : " <size=9><color=#9a93a8>(다른 지역)</color></size>";
            GUI.Label(new Rect(r.x + 12, r.y + 6, w - 24, 16), $"◀ {RegionName(region)} 친구 수첩 ▶{here}", OneLine);
            GUI.Label(new Rect(r.x + 12, r.y + 23, w - 24, 14), "<size=9><color=#CFC6B8>하루 한 번 말 걸기 +1 · G 선물 +2 · 좋아하는 것 +5 · 하트 2에서 리액션을 배운다</color></size>", OneLine);
            for (int i = 0; i < list.Count; i++)
            {
                var f = list[i];
                int hts = Hearts(f), pts = Points(f);
                string move = f.teach != null ? Reactions.NameOf(f.teach) : null;
                string next = hts >= 3 ? $"기념품: {f.keepsake}"
                            : hts == 2 ? $"다음: 특별한 선물 ({pts}/{Need[2]})"
                            : $"다음: {(hts == 1 ? (move != null ? $"리액션 '{move}'" : "답례") : "가까워지기")} ({pts}/{Need[hts]})";
                string liked = hts >= 1 ? f.likedName : "?";
                bool today = DoneToday(f, "talk");
                float y = r.y + 42 + i * 20;
                GUI.Label(new Rect(r.x + 12, y, 44, 18), $"<color=#F05A78>{HeartText(hts)}</color>", OneLine);
                GUI.Label(new Rect(r.x + 58, y, 130, 18), f.name + (today ? " <size=9>✓</size>" : ""), OneLine);
                GUI.Label(new Rect(r.x + 190, y + 2, w - 202, 16), $"<size=9>좋아함 {liked} · {next}</size>", OneLine);
            }
            GUI.Label(new Rect(r.x + 12, r.yMax - 18, w - 24, 14), "<size=9><color=#9a93a8>← → 지역 넘기기 · F/Esc 닫기</color></size>", OneLine);
        }
    }
}
