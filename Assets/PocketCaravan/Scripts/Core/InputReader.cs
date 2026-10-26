using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 키 입력을 한곳에서 읽는다. 키를 바꾸거나 패드를 더할 때 여기만 고친다.
    /// 이동: 방향키/WASD (8방향), 달리기: Shift, 말 걸기·확인: Space, 퀘스트 목록: Q, 지도: M, 취소·메뉴: Esc
    /// 임시 시험용: G를 누르는 동안 4배 빠르게 (나중에 제거)
    /// </summary>
    public static class InputReader
    {
#if ENABLE_INPUT_SYSTEM
        static Keyboard K => Keyboard.current;

        public static Vector2 MoveVector()
        {
            var k = K;
            if (k == null) return Vector2.zero;
            float x = 0f, y = 0f;
            if (k.leftArrowKey.isPressed || k.aKey.isPressed) x -= 1f;
            if (k.rightArrowKey.isPressed || k.dKey.isPressed) x += 1f;
            if (k.downArrowKey.isPressed || k.sKey.isPressed) y -= 1f;
            if (k.upArrowKey.isPressed || k.wKey.isPressed) y += 1f;
            var v = new Vector2(x, y);
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        public static bool Run() => K != null && (K.leftShiftKey.isPressed || K.rightShiftKey.isPressed);
        /// <summary>임시 시험용: G를 누르는 동안 빠르게 이동 (PlayerController.debugFastMultiplier)</summary>
        public static bool DebugFast() => K != null && K.gKey.isPressed;
        public static bool InteractPressed() => K != null && K.spaceKey.wasPressedThisFrame;
        public static bool ConfirmPressed() => InteractPressed();
        public static bool CancelPressed() => K != null && K.escapeKey.wasPressedThisFrame;
        public static bool QuestLogPressed() => K != null && K.qKey.wasPressedThisFrame;
        public static bool MapPressed() => K != null && K.mKey.wasPressedThisFrame;
        public static bool LedgerPressed() => K != null && K.lKey.wasPressedThisFrame;
#else
        public static Vector2 MoveVector()
        {
            float x = 0f, y = 0f;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) x -= 1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) x += 1f;
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) y -= 1f;
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) y += 1f;
            var v = new Vector2(x, y);
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        public static bool Run() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        public static bool DebugFast() => Input.GetKey(KeyCode.G);
        public static bool InteractPressed() => Input.GetKeyDown(KeyCode.Space);
        public static bool ConfirmPressed() => InteractPressed();
        public static bool CancelPressed() => Input.GetKeyDown(KeyCode.Escape);
        public static bool QuestLogPressed() => Input.GetKeyDown(KeyCode.Q);
        public static bool MapPressed() => Input.GetKeyDown(KeyCode.M);
        public static bool LedgerPressed() => Input.GetKeyDown(KeyCode.L);
#endif
    }
}
