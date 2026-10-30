using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 베네치아 세로 선택 창 (2026-10-27). 공용 대화창은 선택지를 가로로 늘어놓고 키보드로는 첫째(Space)·마지막(Esc)만 고를 수 있어서
    /// 선택지가 많은 곳(곤돌라 나루·떠돌이 상인·순례자)은 이 창을 쓴다. ↑↓(W/S)로 고르고 Space로 정하고 Esc로 닫는다. 마우스로 눌러도 된다.
    /// </summary>
    public class VeniceMenu : MonoBehaviour
    {
        public struct Option { public string label; public Action action; public Option(string l, Action a) { label = l; action = a; } }

        static VeniceMenu inst;
        string title, body;
        readonly List<Option> options = new List<Option>();
        int sel, openedFrame;
        bool open;

        public static bool IsOpen => inst != null && inst.open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (GameManager.I != null && GameManager.I.GetComponent<VeniceMenu>() == null)
                inst = GameManager.I.gameObject.AddComponent<VeniceMenu>();
        }

        void Awake() => inst = this;

        public static void Show(string title, string body, params Option[] opts)
        {
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
            float w = 260, rowH = 18;
            var bodyContent = new GUIContent($"<size=9>{body}</size>");
            float bodyH = Mathf.Max(12f, UIStyle.Wrap.CalcHeight(bodyContent, w - 24));     // 설명이 몇 줄이든 선택지와 겹치지 않게
            float top = 24 + bodyH + 4, h = top + options.Count * rowH + 16;
            var r = new Rect((UIStyle.VirtualWidth - w) / 2f, UIStyle.VirtualHeight - h - 40, w, h);
            GUI.Box(r, "", UIStyle.Box);
            GUI.Label(new Rect(r.x + 12, r.y + 6, w - 24, 16), $"<color=#FCE6A8>{title}</color>", UIStyle.Rich);
            GUI.Label(new Rect(r.x + 12, r.y + 22, w - 24, bodyH), bodyContent, UIStyle.Wrap);
            for (int i = 0; i < options.Count; i++)
            {
                var row = new Rect(r.x + 10, r.y + top + i * rowH, w - 20, rowH - 2);
                if (i == sel)
                {
                    var c = GUI.color; GUI.color = new Color(1f, .85f, .4f, .9f);
                    GUI.DrawTexture(row, Texture2D.whiteTexture); GUI.color = c;
                }
                GUI.Label(row, i == sel ? $"<color=#3A2E3A>▶ {options[i].label}</color>" : $"   {options[i].label}", UIStyle.Rich);
                if (Event.current.type == EventType.MouseDown && row.Contains(Event.current.mousePosition)) { Pick(i); Event.current.Use(); return; }
            }
            GUI.Label(new Rect(r.x + 10, r.yMax - 14, w - 20, 12), "<size=9><color=#CFC6B8>↑↓ 고르기 · Space 정하기 · Esc 닫기</color></size>", UIStyle.CenterRich);
        }
    }
}
