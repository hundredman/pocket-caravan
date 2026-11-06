using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 키 입력을 한곳에서 읽는다. 키를 바꾸거나 패드를 더할 때 여기만 고친다.
    /// 이동: 방향키/WASD (8방향), 달리기: Shift, 말 걸기·확인: Space, 상태창 펼치기: Tab, 퀘스트 목록: Q, 지도: M, 소지품: I, 취소·메뉴: Esc
    /// 임시 시험용: C를 누르는 동안 6배 빠르게 (나중에 제거). 11/01 G는 북유럽·베네치아 선물 키와 겹쳐 C로 옮김
    /// </summary>
    public static class InputReader
    {
        // 2026-11-05 화면 단추(오른쪽 위 퀘스트·지도·장부)로 누른 키: 다음 프레임에 한 번 눌린 것으로 친다
        static char virtualKey;
        static int virtualFrame = -1;
        /// <summary>화면 단추로 키를 누른다 ('q' 퀘스트, 'm' 지도, 'l' 장부)</summary>
        public static void Press(char key) { virtualKey = key; virtualFrame = Time.frameCount + 1; }
        static bool Virtual(char key) => virtualKey == key && Time.frameCount == virtualFrame;

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
        /// <summary>임시 시험용: C를 누르는 동안 빠르게 이동 (PlayerController.DebugFastMultiplier = 6배)</summary>
        public static bool DebugFast() => K != null && K.cKey.isPressed;
        public static bool InteractPressed() => K != null && K.spaceKey.wasPressedThisFrame;
        public static bool ConfirmPressed() => InteractPressed();
        public static bool CancelPressed() => K != null && K.escapeKey.wasPressedThisFrame;
        public static bool QuestLogPressed() => (K != null && K.qKey.wasPressedThisFrame) || Virtual('q');
        public static bool MapPressed() => (K != null && K.mKey.wasPressedThisFrame) || Virtual('m');
        public static bool LedgerPressed() => (K != null && K.lKey.wasPressedThisFrame) || Virtual('l');
        /// <summary>상태창·할 일 펼치기 (5초 뒤 다시 한 줄)</summary>
        public static bool StatusPressed() => K != null && K.tabKey.wasPressedThisFrame;
        /// <summary>소지품 창 (2026-10-30)</summary>
        public static bool InventoryPressed() => K != null && K.iKey.wasPressedThisFrame;
        /// <summary>창 안에서 칸 고르기: 이번 프레임에 누른 방향 (위 = +y)</summary>
        public static Vector2Int MovePressed()
        {
            var k = K; if (k == null) return Vector2Int.zero;
            int x = (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame ? 1 : 0) - (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame ? 1 : 0);
            int y = (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame ? 1 : 0) - (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame ? 1 : 0);
            return new Vector2Int(x, y);
        }
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
        public static bool DebugFast() => Input.GetKey(KeyCode.C);
        public static bool InteractPressed() => Input.GetKeyDown(KeyCode.Space);
        public static bool ConfirmPressed() => InteractPressed();
        public static bool CancelPressed() => Input.GetKeyDown(KeyCode.Escape);
        public static bool QuestLogPressed() => Input.GetKeyDown(KeyCode.Q) || Virtual('q');
        public static bool MapPressed() => Input.GetKeyDown(KeyCode.M) || Virtual('m');
        public static bool LedgerPressed() => Input.GetKeyDown(KeyCode.L) || Virtual('l');
        public static bool StatusPressed() => Input.GetKeyDown(KeyCode.Tab);
        public static bool InventoryPressed() => Input.GetKeyDown(KeyCode.I);
        public static Vector2Int MovePressed()
        {
            int x = (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) ? 1 : 0) - (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) ? 1 : 0);
            int y = (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) ? 1 : 0) - (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) ? 1 : 0);
            return new Vector2Int(x, y);
        }
#endif
    }
}
