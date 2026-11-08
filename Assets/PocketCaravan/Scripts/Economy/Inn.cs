using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// "푹 쉼" 버프 (2026-11-03 통합: 북유럽 NorthInn + 베네치아 VeniceInn을 공용으로 합침).
    /// 여관에서 자면 8시간 동안 달릴 때 체력이 절반만 닳는다. 끝나는 시각을 공용 메모 "rest:until"에 저장한다.
    /// 2026-11-08: 시각은 내 의뢰 시간(GameClock.PlayHours) 기준. 옛 저장은 PlayHours가 세상 시간에서 이어받으므로 그대로 맞는다.
    /// 옛 저장(north:rested, venice:rested_until)도 읽어서 기존 버프가 끊기지 않게 한다.
    /// </summary>
    public static class RestBuff
    {
        const string Key = "rest:until";
        public static float Until
        {
            get
            {
                var gm = GameManager.I;
                if (gm == null) return -1f;
                float u = gm.GetMemo(Key, -1f);
                u = Mathf.Max(u, gm.GetMemo("north:rested", -1f));          // 통합 전 북유럽 저장
                u = Mathf.Max(u, gm.GetMemo("venice:rested_until", -1f));   // 통합 전 베네치아 저장
                return u;
            }
        }
        public static bool Rested => GameManager.I != null && GameManager.I.Clock.PlayHours < Until;
        public static int HoursLeft => GameManager.I == null ? 0 : Mathf.Max(0, Mathf.CeilToInt(Until - GameManager.I.Clock.PlayHours));
        public static void Set(float untilHours) { if (GameManager.I != null) GameManager.I.SetMemo(Key, untilHours); }
    }

    /// <summary>
    /// 달릴 때 체력 소모(PlayerController.staminaDrain) 배율을 한 곳에서 곱한다.
    /// 지역 기능(여관 푹 쉼, 벤치 한숨 돌림 등)이 각자 staminaDrain을 덮어쓰면 서로 되돌리다 꼬이므로,
    /// 각자 자기 id로 배율만 등록하고 실제 적용은 Inn이 매 프레임 한 번 한다.
    /// </summary>
    /// <summary>
    /// 11/08 공통화 3단계 (팀 기준): 앉아 쉬기 기본 효과는 모든 지역이 같다 — 앉아 있는 동안 체력이 평소의 3배로 찬다
    /// (평소 회복 1배 + 여기서 2배 더). 지역 덤(베 "한숨 돌림", 아 "더위 식힘", 북 "몸 녹임")은 각 지역에 둔다.
    /// </summary>
    public static class SeatRest
    {
        public const float ExtraRegen = 2f;
        static int lastFrame = -10;
        /// <summary>11/08 기우: 지금 앉아 있는지 (어느 지역 자리든 앉아 있는 동안 매 프레임 Tick을 부른다) — 온라인에 알려 남들도 앉은 모습을 본다</summary>
        public static bool Active => Time.frameCount - lastFrame <= 2;
        public static void Tick(PlayerController pc, float dt)
        {
            lastFrame = Time.frameCount;
            var gm = GameManager.I;
            if (pc == null || gm == null || !(dt > 0f)) return;
            gm.Player.stamina = Mathf.Min(1f, gm.Player.stamina + pc.staminaRegen * ExtraRegen * dt);
        }
    }

    /// <summary>
    /// 11/08 공통화 3단계 (팀 기준): 배 타기 뱃삯은 걸리는 시간에 비례한다 — 4냥 + 시간당 4냥.
    /// 곤돌라 30분 6냥, 고려 나룻배 1시간 8냥, 북유럽 피오르 나룻배 2시간 12냥. 걸린 시간은 의뢰 시간에서 쓴다(Clock.Advance).
    /// </summary>
    public static class BoatFare
    {
        public static int For(float hours) => Mathf.RoundToInt(4f + 4f * Mathf.Max(0f, hours));
    }

    public static class StaminaMods
    {
        static readonly Dictionary<string, float> mods = new Dictionary<string, float>();
        public static void Set(string id, float multiplier)
        {
            if (Mathf.Approximately(multiplier, 1f)) mods.Remove(id); else mods[id] = multiplier;
        }
        public static void Clear(string id) => mods.Remove(id);
        public static float Product
        {
            get { float m = 1f; foreach (var v in mods.Values) m *= v; return m; }
        }
    }

    /// <summary>
    /// 여관에서 쉬어 가기 (전 지역 공용). 들어가는 길은 두 가지, 결과는 같다:
    /// 1) 이름에 "여관"·"숙소"·"객주"가 든 사람에게 말 걸기 → 대화(퀘스트 포함)가 먼저 나오고 끝나면 "쉬어 갈래요?"
    /// 2) 장터 시설창의 손님방(FacilityKind.Rest) "이용"
    /// 쉬면 15냥, 화면이 어두워졌다 밝아진다: 체력 가득, 짐 흔들림 0, 8시간 동안 달려도 덜 지침(RestBuff).
    /// 2026-11-08 여관 = 버프를 주는 곳 (은샘 결정):
    /// - 온라인(공용 세상 시계): 시간을 건너뛰지 않고 의뢰 시간도 쓰지 않는다. 버프만.
    /// - 혼자 하기: 밤(18시~새벽 5시)에 자면 다음 아침 7시에 깨고, 낮에는 예전처럼 4시간 뒤 깬다.
    /// 푹 쉼 중에는 다시 쉴 수 없다(이유를 알려 줌). 따로 이름표나 자리를 만들지 않는다.
    /// </summary>
    public class Inn : MonoBehaviour
    {
        Rect askWindow, askTextRect, yesRect, noRect;
        public const int Price = 15;
        public const float SleepHours = 4f, BuffHours = 8f;
        public const int WakeHour = 7;

        /// <summary>지금 자면 건너뛸 시간. 온라인은 0 (세상 시계는 아무도 넘길 수 없다)</summary>
        public static float HoursToSleep(GameClock clock)
        {
            if (clock == null || clock.Shared) return 0f;
            float h = clock.minuteOfDay / 60f;
            if (h >= 18f) return 24f - h + WakeHour;
            if (h < 5f) return WakeHour - h;
            return SleepHours;
        }
        static readonly string[] HostWords = { "여관", "숙소", "객주" };

        static Inn instance;
        /// <summary>여관에서 자고 깬 순간 (지역 리액션 배우기 등이 듣는다)</summary>
        public static event Action Rested;
        float fade = -1f;             // -1 꺼짐, 0→2 어두워졌다가 밝아짐
        bool slept;
        float baseDrain = -1f;
        PlayerController drainOwner;
        GUIStyle style, wrap;

        string pendingHost;           // 말을 건 여관 사람 이름 (대화가 끝나길 기다림)
        bool sawDialogue;
        float pendingSince;
        string askHost;               // 묻는 창이 열려 있으면 그 사람·시설 이름
        string askNote;
        float askOpenedAt;
        Interactable lastTarget;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (instance != null || FindFirstObjectByType<Inn>() != null) return;
            var go = new GameObject("Inn");
            DontDestroyOnLoad(go);
            go.AddComponent<Inn>();
        }
        void Awake() { if (instance == null) instance = this; }

        public static bool IsHost(Interactable it)
        {
            if (it == null || it.DisplayName == null || it.Category == Interactable.Kind.Board || it.Category == Interactable.Kind.Market) return false;
            if (it.DisplayName.Contains("손님")) return false;   // "대상 숙소 손님" 같은 투숙객은 여관 주인이 아니다
            foreach (var w in HostWords) if (it.DisplayName.Contains(w)) return true;
            return false;
        }

        /// <summary>다른 UI(시설창 등)에서 "쉬어 갈래요?" 창을 연다. 이미 푹 쉼이면 알림만.</summary>
        public static void Offer(string host)
        {
            if (UIState.Blocking) return;
            if (instance == null) Boot();
            if (instance == null) return;
            if (RestBuff.Rested) { Notify.Show($"{host}: 아직 푹 쉬어서 개운하네요. 다음에 또 와요!", 3f); return; }
            instance.OpenAsk(host);
        }

        /// <summary>묻지도 받지도 않고 바로 잠든다 (돈은 부르는 쪽이 받는다. 점검 도구용)</summary>
        public static void SleepNow()
        {
            if (instance == null) Boot();
            if (instance == null) return;
            instance.askHost = null; instance.fade = 0f; instance.slept = false;
            UIState.Blocking = true;
        }

        void Update()
        {
            ApplyDrain();
            if (fade >= 0f) { UpdateFade(); return; }
            var pc = PlayerController.Instance;
            if (pc == null || GameManager.I == null) { pendingHost = null; return; }

            // 여관 사람에게 Space: 대화(퀘스트 포함)가 먼저 나오고, 끝나면 묻는다
            // (PlayerController가 먼저 돌면 같은 프레임에 이미 대화창이 열려 있을 수 있어, 직전 프레임의 말 걸 대상을 쓴다)
            if (askHost == null && pendingHost == null && !UIState.JustClosed && InputReader.InteractPressed() && IsHost(lastTarget))
            {
                pendingHost = lastTarget.DisplayName; sawDialogue = UIState.Blocking; pendingSince = Time.unscaledTime;
                return;
            }
            if (!UIState.Blocking) lastTarget = pc.TalkTarget;
            if (pendingHost != null)
            {
                if (UIState.Blocking) sawDialogue = true;
                bool dialogueDone = sawDialogue && !UIState.Blocking;
                bool noDialogue = !sawDialogue && Time.unscaledTime - pendingSince > 0.3f;
                if (dialogueDone || noDialogue)
                {
                    string host = pendingHost; pendingHost = null;
                    Offer(host);
                }
            }
        }

        void OpenAsk(string host)
        {
            askHost = host; askNote = null; askOpenedAt = Time.unscaledTime;
            UIState.Blocking = true;
        }

        void CloseAsk() { askHost = null; UIState.Release(); }

        void Rest()
        {
            var gm = GameManager.I;
            if (gm == null) { CloseAsk(); return; }
            if (gm.Player.money < Price) { askNote = $"앗, {Price}냥이 있어야 쉴 수 있어요."; return; }
            gm.Player.money -= Price;
            askHost = null;
            fade = 0f; slept = false;   // Blocking은 그대로 (자는 동안 못 움직임)
            Sfx.Play("cancel", .6f);
        }

        void UpdateFade()
        {
            UIState.Blocking = true;    // 다른 창이 닫히며 풀어도 자는 동안은 멈춘다
            fade += Time.unscaledDeltaTime / 0.9f;
            if (fade >= 1f && !slept)
            {
                slept = true;
                var gm = GameManager.I;
                if (gm != null)
                {
                    gm.Clock.Advance(HoursToSleep(gm.Clock));
                    gm.Player.stamina = 1f;
                    gm.Player.shake = 0f;
                    RestBuff.Set(gm.Clock.PlayHours + BuffHours);
                    gm.SaveNow();
                }
            }
            if (fade >= 2f)
            {
                fade = -1f;
                UIState.Release();
                Sfx.Play("levelup", .6f);
                Notify.Show($"푹 쉬었다! {BuffHours:0}시간 동안 달려도 덜 지친다.", 4f);
                Rested?.Invoke();
            }
        }

        /// <summary>체력 소모 배율 적용: 푹 쉼이면 절반, 그 밖의 지역 배율(StaminaMods)도 함께 곱한다</summary>
        void ApplyDrain()
        {
            var pc = PlayerController.Instance;
            if (pc == null) return;
            if (pc != drainOwner) { drainOwner = pc; baseDrain = pc.staminaDrain; }
            float want = baseDrain * (RestBuff.Rested ? 0.5f : 1f) * StaminaMods.Product;
            if (!Mathf.Approximately(pc.staminaDrain, want)) pc.staminaDrain = want;
        }

        void OnGUI()
        {
            if (askHost != null) { DrawAsk(); return; }
            if (fade < 0f) { DrawRestedBadge(); return; }
            GUI.depth = -900;
            UIStyle.Begin();
            float a = fade < 1f ? fade : 2f - fade;
            var old = GUI.color;
            GUI.color = new Color(0.04f, 0.04f, 0.1f, Mathf.Clamp01(a * 1.3f));
            GUI.DrawTexture(new Rect(0, 0, UIStyle.VirtualWidth, UIStyle.VirtualHeight), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(a * 1.5f));
            if (style == null) style = new GUIStyle(UIStyle.CenterRich) { wordWrap = false, clipping = TextClipping.Overflow };
            GUI.Label(new Rect(0, UIStyle.VirtualHeight / 2f - 10f, UIStyle.VirtualWidth, 20f), "따뜻한 이불 속에서 푹 쉬었다… Zzz", style);
            GUI.color = old;
        }

        /// <summary>쉰 효과가 남아 있으면 왼쪽 위 상태창 바로 아래에 "푹 쉼 (○시간)"</summary>
        void DrawRestedBadge()
        {
            var gm = GameManager.I;
            if (gm == null || !RestBuff.Rested || UIState.Blocking || HUD.Expanded || MapRoot.Current == null || gm.IsTravelling) return;   // 11/08 Tab으로 펼친 상태 카드 안에 겹쳐 그려졌다
            UIStyle.Begin();
            if (style == null) style = new GUIStyle(UIStyle.CenterRich) { wordWrap = false, clipping = TextClipping.Overflow };
            var r = new Rect(6f, 92f, 96f, 16f);
            GUI.Box(r, GUIContent.none, UIStyle.Box);
            GUI.Label(r, $"<color=#9fe0a0>푹 쉼</color> <size=9>({RestBuff.HoursLeft}시간)</size>", style);
        }

        void DrawAsk()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && Time.unscaledTime - askOpenedAt > 0.3f)   // 대화를 닫은 Space가 바로 "쉬기"로 먹지 않게
            {
                if (e.keyCode == KeyCode.Space || e.keyCode == KeyCode.Return || e.keyCode == KeyCode.Y) { e.Use(); Rest(); return; }
                if (e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.N) { e.Use(); CloseAsk(); return; }
            }
            UIStyle.Begin();
            GUI.depth = -900;
            wrap = CuteUI.PaperWrap;
            float w = Mathf.Min(360, UIStyle.VirtualWidth - 16);
            var clock = GameManager.I != null ? GameManager.I.Clock : null;
            float sleep = HoursToSleep(clock);
            string rest = sleep <= 0f ? "잠깐 푹 쉬기" : clock != null && clock.minuteOfDay / 60f + sleep >= 24f || clock != null && clock.minuteOfDay < 300f ? $"아침 {WakeHour}시까지 휴식" : $"{sleep:0}시간 휴식";
            // 11/08 은샘: 값·쉬는 시간·체력 변화를 그림 줄로. 지금 체력 → 가득을 막대로 보여 준다
            var gm = GameManager.I;
            string text = askNote ?? "따뜻한 방 있어요. 쉬어 갈래요?";
            float textHeight = Mathf.Max(16, CuteUI.WrapHeight(text, w - 28));
            float rowsH = askNote == null ? 3 * 19 + 4 : 0;
            float h = textHeight + rowsH + 58;
            var r = askWindow = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), Mathf.Round(UIStyle.VirtualHeight - h - 50), w, h);
            CuteUI.Dim(); CuteUI.Window(r); CuteUI.TitlePlate(r.center.x, r.y - 11, askHost);
            if (CuteUI.CloseButton(r.xMax - 16f, r.y - 8f)) { CloseAsk(); return; }   // 11/08 오른쪽 위 X
            askTextRect = new Rect(r.x + 14, r.y + 16, w - 28, textHeight + rowsH);
            CuteUI.Wrap(r.x + 14, r.y + 16, w - 28, text, CuteUI.Text);
            if (askNote == null && gm != null)
            {
                float ry = r.y + 20 + textHeight, rx = r.x + 16, rw = w - 32;
                bool poor = gm.Player.money < Price;
                InnRow(rx, ry, rw, CuteUI.Pic.Coin7, "값", $"{Price}냥" + (poor ? $" (가진 돈 {gm.Player.money}냥)" : ""), poor ? CuteUI.Down : CuteUI.Text);
                ry += 19;
                CuteUI.Box(new Rect(rx, ry, rw, 17), 5, CuteUI.Paper2, CuteUI.Paper2, 0);
                CuteUI.Icon(CuteUI.Pic.Bolt, rx + 4, ry + 3);
                CuteUI.Label(rx + 16, ry + 8.5f, "체력", CuteUI.Sub, true);
                float bx = rx + 50, bw = rw - 50 - 46;
                CuteUI.Bar(new Rect(bx, ry + 5, bw, 7), 1f, CuteUI.StaminaHi, CuteUI.StaminaHi);
                CuteUI.Bar(new Rect(bx, ry + 5, bw, 7), gm.Player.stamina, CuteUI.Stamina, CuteUI.StaminaHi);
                CuteUI.LabelRight(rx + rw - 6, ry + 8.5f, "→ 가득", CuteUI.Up, true, true);
                ry += 19;
                InnRow(rx, ry, rw, CuteUI.Pic.Star4, rest, $"{BuffHours:0}시간 동안 달릴 때 덜 지침", CuteUI.Text);
            }
            float yesWidth = Mathf.Floor((w - 36) * .56f);
            yesRect = new Rect(r.x + 14, r.yMax - 30, yesWidth, 22);
            noRect = new Rect(r.x + 22 + yesWidth, r.yMax - 30, w - 36 - yesWidth, 22);
            if (askNote == null && CuteUI.Button(yesRect, $"쉬어 갈래요 ({Price}냥)")) Rest();
            if (CuteUI.SoftButton(noRect, askNote == null ? "괜찮아요" : "알겠어요")) CloseAsk();
        }

        static void InnRow(float x, float y, float w, CuteUI.Pic pic, string label, string value, Color32 col)
        {
            CuteUI.Box(new Rect(x, y, w, 17), 5, CuteUI.Paper2, CuteUI.Paper2, 0);
            var ps = CuteUI.PicSize(pic);
            CuteUI.Icon(pic, x + 9 - ps.x / 2f, y + 8.5f - ps.y / 2f);
            CuteUI.Label(x + 16, y + 8.5f, label, CuteUI.Sub, true);
            CuteUI.LabelRight(x + w - 6, y + 8.5f, value, col, true, true);
        }

    }
}
