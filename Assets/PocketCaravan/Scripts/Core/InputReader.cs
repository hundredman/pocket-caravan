using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>키 입력을 한곳에서 읽는다. 키를 바꾸거나 패드를 더할 때 여기만 고친다.</summary>
    public static class InputReader
    {
#if ENABLE_INPUT_SYSTEM
        static Keyboard K => Keyboard.current;

        static bool Up => K.upArrowKey.isPressed || K.wKey.isPressed;
        static bool Down => K.downArrowKey.isPressed || K.sKey.isPressed;
        static bool Left => K.leftArrowKey.isPressed || K.aKey.isPressed;
        static bool Right => K.rightArrowKey.isPressed || K.dKey.isPressed;

        static Vector2Int last;
        static int lastFrame = -1;

        /// <summary>여러 방향키를 같이 누르면 마지막에 누른 쪽으로 간다.</summary>
        public static Vector2Int Move()
        {
            var k = K;
            if (k == null) return Vector2Int.zero;
            if (lastFrame != Time.frameCount)
            {
                lastFrame = Time.frameCount;
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) last = Vector2Int.up;
                else if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) last = Vector2Int.down;
                else if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) last = Vector2Int.left;
                else if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) last = Vector2Int.right;
            }
            if (Held(last)) return last;
            if (Up) return last = Vector2Int.up;
            if (Down) return last = Vector2Int.down;
            if (Left) return last = Vector2Int.left;
            if (Right) return last = Vector2Int.right;
            return Vector2Int.zero;
        }

        static bool Held(Vector2Int d) =>
            (d == Vector2Int.up && Up) || (d == Vector2Int.down && Down) || (d == Vector2Int.left && Left) || (d == Vector2Int.right && Right);

        public static bool Run() => K != null && (K.leftShiftKey.isPressed || K.rightShiftKey.isPressed);
        /// <summary>말 걸기·확인: E 또는 Space</summary>
        public static bool InteractPressed() => K != null && (K.eKey.wasPressedThisFrame || K.spaceKey.wasPressedThisFrame);
        public static bool CancelPressed() => K != null && K.escapeKey.wasPressedThisFrame;
        public static bool ConfirmPressed() => K != null && (K.enterKey.wasPressedThisFrame || K.numpadEnterKey.wasPressedThisFrame);
        public static bool QuestLogPressed() => K != null && K.jKey.wasPressedThisFrame;
#else
        public static Vector2Int Move()
        {
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) return Vector2Int.up;
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) return Vector2Int.down;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) return Vector2Int.left;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) return Vector2Int.right;
            return Vector2Int.zero;
        }

        public static bool Run() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        public static bool InteractPressed() => Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space);
        public static bool CancelPressed() => Input.GetKeyDown(KeyCode.Escape);
        public static bool ConfirmPressed() => Input.GetKeyDown(KeyCode.Return);
        public static bool QuestLogPressed() => Input.GetKeyDown(KeyCode.J);
#endif
    }
}
