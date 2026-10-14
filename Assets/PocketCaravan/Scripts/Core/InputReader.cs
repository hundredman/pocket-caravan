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

        public static Vector2Int Move()
        {
            var k = K;
            if (k == null) return Vector2Int.zero;
            if (k.upArrowKey.isPressed || k.wKey.isPressed) return Vector2Int.up;
            if (k.downArrowKey.isPressed || k.sKey.isPressed) return Vector2Int.down;
            if (k.leftArrowKey.isPressed || k.aKey.isPressed) return Vector2Int.left;
            if (k.rightArrowKey.isPressed || k.dKey.isPressed) return Vector2Int.right;
            return Vector2Int.zero;
        }

        public static bool Run() => K != null && (K.leftShiftKey.isPressed || K.rightShiftKey.isPressed);
        public static bool InteractPressed() => K != null && K.eKey.wasPressedThisFrame;
        public static bool CancelPressed() => K != null && K.escapeKey.wasPressedThisFrame;
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
        public static bool InteractPressed() => Input.GetKeyDown(KeyCode.E);
        public static bool CancelPressed() => Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
