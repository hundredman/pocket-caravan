using UnityEngine;

namespace PocketCaravan
{
    /// <summary>창이 열려 있으면 이동과 시간을 멈춘다.</summary>
    public static class UIState
    {
        public static bool Blocking;
    }

    /// <summary>임시 UI(OnGUI)용 한글 폰트와 화면 배율. 정식 UI로 바꾸면 지워도 된다.</summary>
    public static class UIStyle
    {
        public const float VirtualHeight = 360f;
        static Font font;

        public static float Scale => Screen.height / VirtualHeight;
        public static float VirtualWidth => Screen.width / Scale;

        static GUIStyle center, right, rich, wrap;
        public static GUIStyle Center => center ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        public static GUIStyle Right => right ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };
        public static GUIStyle Rich => rich ??= new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12 };
        public static GUIStyle Wrap => wrap ??= new GUIStyle(GUI.skin.label) { wordWrap = true, richText = true };

        public static void Begin()
        {
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont(new[] { "Apple SD Gothic Neo", "Malgun Gothic", "NanumGothic", "Noto Sans CJK KR", "Arial" }, 12);
            GUI.skin.font = font;
            GUI.skin.label.fontSize = 11;
            GUI.skin.button.fontSize = 11;
            GUI.skin.box.fontSize = 11;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Scale, Scale, 1f));
        }
    }
}
