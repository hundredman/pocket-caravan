using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 세로 선택 창 (공용, 2026-11-08 공통화 4단계: 베네치아 VeniceMenu(2026-10-27, 윤채)를 공용으로 올림. 베네치아 코드는 VeniceMenu 이름 그대로 쓴다). 공용 대화창은 선택지를 가로로 늘어놓고 키보드로는 첫째(Space)·마지막(Esc)만 고를 수 있어서
    /// 선택지가 많은 곳(곤돌라 나루·떠돌이 상인·순례자)은 이 창을 쓴다. ↑↓(W/S)로 고르고 Space로 정하고 Esc로 닫는다. 마우스로 눌러도 된다.
    /// </summary>
    public class ChoiceMenu : MonoBehaviour
    {
        public struct Option { public string label; public Action action; public Option(string l, Action a) { label = l; action = a; } }

        static ChoiceMenu inst;
        string title, body;
        readonly List<Option> options = new List<Option>();
        int sel, openedFrame;
        bool open;

        public static bool IsOpen => inst != null && inst.open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (GameManager.I != null && GameManager.I.GetComponent<ChoiceMenu>() == null)
                inst = GameManager.I.gameObject.AddComponent<ChoiceMenu>();
        }

        void Awake() => inst = this;

        public static void Show(string title, string body, params Option[] opts)
        {
            if (inst == null) Attach();   // 새 게임으로 GameManager가 바뀐 뒤에도 붙인다
            if (inst == null) return;
            inst.title = title; inst.body = body;
            inst.options.Clear(); inst.options.AddRange(opts);
            inst.sel = 0; inst.open = true; inst.openedFrame = Time.frameCount;
            UIState.Blocking = true;
            Sfx.Play("open", .4f);
        }

        /// <summary>점검용: 지금 보이는 선택지 이름들</summary>
        public static string[] Labels => inst == null ? new string[0] : inst.options.ConvertAll(o => o.label).ToArray();

        static bool Key(int k)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            return k < 0 ? kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame : kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame;
#else
            return false;
#endif
        }

        void Update()
        {
            if (!open) return;
            UIState.Blocking = true;
            if (Time.frameCount == openedFrame) return;
            if (Key(-1)) sel = (sel + options.Count - 1) % options.Count;
            if (Key(1)) sel = (sel + 1) % options.Count;
            if (InputReader.InteractPressed()) Pick(sel);
            else if (InputReader.CancelPressed()) Close();
        }

        void Close() { open = false; UIState.Release(); }

        void Pick(int i)
        {
            Close();
            if (i >= 0 && i < options.Count) options[i].action?.Invoke();
        }

        void OnGUI()
        {
            if (!open) return;
            UIStyle.Begin();
            GUI.depth = -18;
            // 11/07 UI B안: 대화창과 같은 종이 창 + 왼쪽 위 주황 이름표. 고른 줄은 청록(상호작용), 나머지는 종이 줄
            const float w = 268f, rowH = 20f;
            var bodyContent = new GUIContent($"<size=9>{CuteUI.PaperColors(body ?? "")}</size>");
            float bodyH = string.IsNullOrEmpty(body) ? 0f : Mathf.Max(12f, CuteUI.PaperWrap.CalcHeight(bodyContent, w - 24));   // 설명이 몇 줄이든 선택지와 겹치지 않게
            float top = 16f + bodyH + 6f, h = top + options.Count * rowH + 22f;
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2f), Mathf.Round(UIStyle.VirtualHeight - h - 40f), w, h);
            CuteUI.Window(r);
            if (!string.IsNullOrEmpty(title))
            {
                string name = CuteUI.Plain(title);
                var tag = new Rect(r.x + 12f, r.y - 9f, CuteUI.Width(name, false, true) + 22f, 18f);
                CuteUI.Box(tag, 7, CuteUI.Frame, CuteUI.Ink, 2, 2);
                CuteUI.LabelCenter(tag.center.x, tag.center.y + 1f, name, CuteUI.Ink, false, true);
                CuteUI.LabelCenter(tag.center.x, tag.center.y, name, CuteUI.White, false, true);
            }
            if (bodyH > 0f) GUI.Label(new Rect(r.x + 12f, r.y + 15f, w - 24f, bodyH), bodyContent, CuteUI.PaperWrap);
            for (int i = 0; i < options.Count; i++)
            {
                var row = new Rect(r.x + 10f, r.y + top + i * rowH, w - 20f, rowH - 3f);
                if (i == sel)
                {
                    CuteUI.Box(row, 7, CuteUI.Interact, CuteUI.Ink, 1, 1, CuteUI.InteractLo);
                    CuteUI.Label(row.x + 8f, row.center.y, CuteUI.Plain(options[i].label), CuteUI.OnInteract, false, true);
                }
                else
                {
                    CuteUI.Box(row, 7, CuteUI.Paper, CuteUI.Line);
                    GUI.Label(new Rect(row.x + 8f, row.y, row.width - 12f, row.height), CuteUI.PaperColors(options[i].label), CuteUI.PaperRich);
                }
                if (Event.current.type == EventType.MouseDown && row.Contains(Event.current.mousePosition)) { Pick(i); Event.current.Use(); return; }
            }
            CuteUI.LabelCenter(r.center.x, r.yMax - 12f, "↑↓ 고르기 · Space 정하기 · Esc 닫기", CuteUI.Sub, true);
        }
    }
}
