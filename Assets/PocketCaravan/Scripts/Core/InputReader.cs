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
    /// 11/08 개발용 키(C 빠르게 · 북유럽 F9 속도 창)는 Unity 에디터·Development Build에서만 (배포판에서 치트가 되지 않게)
    /// </summary>
    public static class InputReader
    {
        // 2026-11-05 화면 단추(오른쪽 위 퀘스트·지도·장부)로 누른 키: 다음 프레임에 한 번 눌린 것으로 친다
        static char virtualKey;
        static int virtualFrame = -1;
        /// <summary>화면 단추로 키를 누른다 ('q' 퀘스트, 'm' 지도, 'l' 장부 · 11/08 ' ' Space, '\x1b' Esc, 'i' 소지품, '\t' Tab: 마우스로도 같은 일을 하게)</summary>
        public static void Press(char key) { virtualKey = key; virtualFrame = Time.frameCount + 1; }
        static bool Virtual(char key) => virtualKey == key && Time.frameCount == virtualFrame;

        // 11/08 미니게임 마우스: 놀이 화면 빈 곳을 누르면 Space를 누른 것, 누르고 있으면 Space를 누르고 있는 것과 같다
        static bool mouseSpace;
        /// <summary>미니게임 OnGUI 맨 끝에서 부른다 (그 창의 단추·X보다 뒤에 불러야 단추 클릭을 빼앗지 않는다). 왼쪽 클릭 = Space</summary>
        public static void ClickAsSpace()
        {
            var e = Event.current;
            if (e != null && e.type == EventType.MouseDown && e.button == 0) { Press(' '); mouseSpace = true; e.Use(); }
        }
        /// <summary>Space를 누르고 있는지 (키보드, 또는 ClickAsSpace로 누른 채인 마우스 왼쪽 단추)</summary>
        public static bool SpaceHeld()
        {
            if (mouseSpace && !MouseLeftHeld()) mouseSpace = false;
            return SpaceKeyHeld() || mouseSpace;
        }

        /// <summary>개발용 키를 받는지: Unity 에디터이거나 Development Build</summary>
        public static bool DevKeys => Application.isEditor || Debug.isDebugBuild;

#if ENABLE_INPUT_SYSTEM
        static Keyboard K => Keyboard.current;
        static bool SpaceKeyHeld() => K != null && K.spaceKey.isPressed;
        static bool MouseLeftHeld() => Mouse.current != null && Mouse.current.leftButton.isPressed;

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
        public static bool DebugFast() => DevKeys && K != null && K.cKey.isPressed;
        /// <summary>숫자 키 1~9 가운데 이번 프레임에 누른 것 (없으면 0). 대화 선택지를 바로 고른다</summary>
        public static int NumberPressed()
        {
            var k = K; if (k == null) return 0;
            for (int i = 0; i < 9; i++) if (k[Key.Digit1 + i].wasPressedThisFrame || k[Key.Numpad1 + i].wasPressedThisFrame) return i + 1;
            return 0;
        }
        public static bool InteractPressed() => (K != null && K.spaceKey.wasPressedThisFrame) || Virtual(' ');
        public static bool ConfirmPressed() => InteractPressed();
        public static bool CancelPressed() => (K != null && K.escapeKey.wasPressedThisFrame) || Virtual('\x1b');
        public static bool QuestLogPressed() => (K != null && K.qKey.wasPressedThisFrame) || Virtual('q');
        public static bool MapPressed() => (K != null && K.mKey.wasPressedThisFrame) || Virtual('m');
        public static bool LedgerPressed() => (K != null && K.lKey.wasPressedThisFrame) || Virtual('l');
        /// <summary>상태창·할 일 펼치기 (5초 뒤 다시 한 줄)</summary>
        public static bool StatusPressed() => (K != null && K.tabKey.wasPressedThisFrame) || Virtual('\t');
        /// <summary>소지품 창 (2026-10-30)</summary>
        public static bool InventoryPressed() => (K != null && K.iKey.wasPressedThisFrame) || Virtual('i');
        /// <summary>창 안에서 칸 고르기: 이번 프레임에 누른 방향 (위 = +y)</summary>
        public static Vector2Int MovePressed()
        {
            var k = K; if (k == null) return Vector2Int.zero;
            int x = (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame ? 1 : 0) - (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame ? 1 : 0);
            int y = (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame ? 1 : 0) - (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame ? 1 : 0);
            return new Vector2Int(x, y);
        }
#else
        static bool SpaceKeyHeld() => Input.GetKey(KeyCode.Space);
        static bool MouseLeftHeld() => Input.GetMouseButton(0);
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
        public static bool DebugFast() => DevKeys && Input.GetKey(KeyCode.C);
        public static int NumberPressed()
        {
            for (int i = 0; i < 9; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) return i + 1;
            return 0;
        }
        public static bool InteractPressed() => Input.GetKeyDown(KeyCode.Space) || Virtual(' ');
        public static bool ConfirmPressed() => InteractPressed();
        public static bool CancelPressed() => Input.GetKeyDown(KeyCode.Escape) || Virtual('\x1b');
        public static bool QuestLogPressed() => Input.GetKeyDown(KeyCode.Q) || Virtual('q');
        public static bool MapPressed() => Input.GetKeyDown(KeyCode.M) || Virtual('m');
        public static bool LedgerPressed() => Input.GetKeyDown(KeyCode.L) || Virtual('l');
        public static bool StatusPressed() => Input.GetKeyDown(KeyCode.Tab) || Virtual('\t');
        public static bool InventoryPressed() => Input.GetKeyDown(KeyCode.I) || Virtual('i');
        public static Vector2Int MovePressed()
        {
            int x = (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) ? 1 : 0) - (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) ? 1 : 0);
            int y = (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) ? 1 : 0) - (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) ? 1 : 0);
            return new Vector2Int(x, y);
        }
#endif
    }
}
