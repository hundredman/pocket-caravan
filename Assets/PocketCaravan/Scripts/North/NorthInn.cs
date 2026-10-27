using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 10/27 여관에서 쉬어 가기. 여관 사람(이름에 "여관"이 들어간 사람: 비르카 여관 할머니, 북문·길목 마을 여관 주인)에게
    /// 말을 걸고 대화가 끝나면 "쉬어 갈래요?"를 묻는다. 쉬면 15냥, 화면이 어두워졌다가 4시간 뒤 깬다:
    /// 체력 가득, 짐 흔들림 0, 그리고 8시간 동안 달릴 때 체력이 절반만 닳는다.
    /// 따로 이름표나 자리를 만들지 않는다 (사람 이름표 하나만 보이게). 퀘스트 대화는 그대로 먼저 나온다.
    /// 공용 PlayerController의 staminaDrain 값을 버프 동안만 낮췄다가 되돌린다 (버프 끝은 NorthSave "rested").
    /// </summary>
    public class NorthInn : MonoBehaviour
    {
        const int Price = 15;
        const float SleepHours = 4f, BuffHours = 8f;
        float fade = -1f;             // -1 꺼짐, 0→2 어두워졌다가 밝아짐
        bool slept;
        float baseDrain = -1f;
        PlayerController drainOwner;
        GUIStyle style, wrap;

        // 묻기: 여관 사람과 대화가 끝나면 연다
        string pendingHost;           // 말을 건 여관 사람 이름 (대화가 끝나길 기다림)
        bool sawDialogue;
        float pendingSince;
        string askHost;               // 묻는 창이 열려 있으면 그 사람 이름
        string askNote;
        float askOpenedAt;
        Interactable lastTarget;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthInn>() != null) return;
            var go = new GameObject("NorthInn");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthInn>();
        }

        static bool Rested => GameManager.I != null && NorthSave.Get("rested", -1f) > GameManager.I.Clock.TotalHours;

        static bool IsInnHost(Interactable it) =>
            it != null && it.DisplayName != null && it.DisplayName.Contains("여관") && it.Category != Interactable.Kind.Board && it.Category != Interactable.Kind.Market;

        void Update()
        {
            ApplyBuff();
            if (fade >= 0f) { UpdateFade(); return; }
            var pc = PlayerController.Instance;
            if (!NorthUtil.InNorth || pc == null) { pendingHost = null; return; }

            // 1) 여관 사람에게 Space: 대화(퀘스트 포함)가 먼저 나오고, 끝나면 묻는다
            //    (PlayerController가 먼저 돌면 같은 프레임에 이미 대화창이 열려 있을 수 있어, 직전 프레임의 말 걸 대상을 쓴다)
            if (askHost == null && pendingHost == null && !UIState.JustClosed && InputReader.InteractPressed() && IsInnHost(lastTarget))
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
                    if (!Rested) OpenAsk(host);
                    else Notify.Show($"{host}: 아직 푹 쉬어서 개운하네요. 다음에 또 와요!", 3f);   // 10/27 푹 쉼 중엔 다시 못 쉼 (이유를 알려 줌)
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
        }

        void UpdateFade()
        {
            fade += Time.unscaledDeltaTime / 0.9f;
            if (fade >= 1f && !slept)
            {
                slept = true;
                var gm = GameManager.I;
                if (gm != null)
                {
                    gm.Clock.Advance(SleepHours);
                    gm.Player.stamina = 1f;
                    gm.Player.shake = 0f;
                    NorthSave.Set("rested", gm.Clock.TotalHours + BuffHours);
                }
            }
            if (fade >= 2f)
            {
                fade = -1f;
                UIState.Release();
                Notify.Show($"푹 쉬었다! {BuffHours:0}시간 동안 달려도 덜 지친다.", 4f);
            }
        }

        /// <summary>푹 쉰 동안 달릴 때 체력 감소를 절반으로 (끝나면 원래 값으로)</summary>
        void ApplyBuff()
        {
            var pc = PlayerController.Instance;
            if (pc == null) return;
            if (pc != drainOwner) { drainOwner = pc; baseDrain = pc.staminaDrain; }
            float want = Rested ? baseDrain * 0.5f : baseDrain;
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

        /// <summary>쉰 효과가 남아 있으면 왼쪽 위 상태창 바로 아래에 "푹 쉼 (○시간)" (내 상태라는 걸 보이게)</summary>
        void DrawRestedBadge()
        {
            var gm = GameManager.I;
            if (gm == null || !Rested || UIState.Blocking || MapRoot.Current == null) return;
            int left = Mathf.CeilToInt(NorthSave.Get("rested", 0f) - gm.Clock.TotalHours);
            UIStyle.Begin();
            if (style == null) style = new GUIStyle(UIStyle.CenterRich) { wordWrap = false, clipping = TextClipping.Overflow };
            var r = new Rect(6f, 92f, 96f, 16f);
            GUI.Box(r, GUIContent.none, UIStyle.Box);
            GUI.Label(r, $"<color=#9fe0a0>푹 쉼</color> <size=9>({left}시간)</size>", style);
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
            if (wrap == null) wrap = new GUIStyle(UIStyle.Rich) { wordWrap = true };
            float w = 300f, h = 96f;
            var r = new Rect((UIStyle.VirtualWidth - w) / 2f, UIStyle.VirtualHeight - h - 50f, w, h);
            GUI.Box(r, GUIContent.none, UIStyle.Box);
            GUI.Label(new Rect(r.x + 12, r.y + 8, w - 24, 16), $"<b>{askHost}</b>", UIStyle.Rich);
            string text = askNote ?? $"따뜻한 방 있어요. 쉬어 갈래요?\n<color=#c9c3d6><size=9>{Price}냥 · 4시간 푹 자고 체력 가득 · 8시간 동안 달려도 덜 지침</size></color>";
            GUI.Label(new Rect(r.x + 12, r.y + 26, w - 24, 40), text, wrap);
            if (askNote == null && GUI.Button(new Rect(r.x + 12, r.yMax - 26, 130, 20), $"쉬어 갈래요 ({Price}냥)", UIStyle.Button)) Rest();
            if (GUI.Button(new Rect(r.xMax - 112, r.yMax - 26, 100, 20), "괜찮아요 Esc", UIStyle.Button)) CloseAsk();
        }
    }
}
