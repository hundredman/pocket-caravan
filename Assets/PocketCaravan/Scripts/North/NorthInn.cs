using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 10/27 여관에서 쉬어 가기 (비르카 여관 할머니, 북문 마을·길목 마을 여관 주인 옆 "여관에서 쉬기").
    /// 15냥을 내면 화면이 어두워졌다가 4시간 뒤 아침처럼 개운하게 깬다: 체력 가득, 짐 흔들림 0,
    /// 그리고 8시간 동안 "푹 쉼" — 달릴 때 체력이 절반만 닳아 더 오래 달린다.
    /// 공용 PlayerController의 staminaDrain 값을 버프 동안만 낮췄다가 되돌린다 (저장 형식 그대로, 버프 끝은 NorthSave "rested").
    /// </summary>
    public class NorthInn : MonoBehaviour
    {
        const int Price = 15;
        const float SleepHours = 4f, BuffHours = 8f;
        Transform root;
        string builtFor;
        float fade = -1f;             // -1 꺼짐, 0→2 어두워졌다가 밝아짐
        bool slept;
        float baseDrain = -1f;
        PlayerController drainOwner;
        GUIStyle style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthInn>() != null) return;
            var go = new GameObject("NorthInn");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthInn>();
        }

        void OnEnable() { SceneManager.sceneLoaded += OnLoaded; }
        void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; }
        void OnLoaded(Scene s, LoadSceneMode m) { builtFor = null; }

        static bool Rested => GameManager.I != null && NorthSave.Get("rested", -1f) > GameManager.I.Clock.TotalHours;

        void Build()
        {
            builtFor = NorthUtil.Map;
            if (root != null) Destroy(root.gameObject);
            root = null;
            if (!NorthUtil.InNorth || MapRoot.Current == null) return;
            var inns = NorthData.Get(NorthUtil.Map, "inns");
            if (inns.Length < 2) return;
            root = new GameObject("North Inns").transform;
            for (int i = 0; i + 1 < inns.Length; i += 2)
                NorthSpot.Make(root, NorthUtil.Cell(NorthUtil.Map, inns[i], inns[i + 1]),
                    () => Rested ? "여관 (푹 쉬는 중)" : $"여관에서 쉬기 ({Price}냥)", Rest, Interactable.Kind.Board);
        }

        void Rest()
        {
            var gm = GameManager.I;
            if (gm == null || fade >= 0f) return;
            if (Rested) { DialogueUI.Show("여관", "아직 몸이 개운하다. 나중에 다시 쉬자."); return; }
            if (gm.Player.money < Price) { DialogueUI.Show("여관", $"쉬어 가려면 {Price}냥이 필요해."); return; }
            gm.Player.money -= Price;
            fade = 0f; slept = false;
            UIState.Blocking = true;
        }

        void Update()
        {
            if (builtFor != NorthUtil.Map) Build();
            ApplyBuff();
            if (fade < 0f) return;
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
            if (fade < 0f) return;
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
    }
}
