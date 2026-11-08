using System;

namespace PocketCaravan
{
    /// <summary>
    /// 베네치아 세로 선택 창 (2026-10-27). 2026-11-08 공통화 4단계: 창은 공용 ChoiceMenu로 올렸고,
    /// 베네치아 코드(곤돌라 나루·점검 도구)가 부르던 이름만 남겨 넘긴다.
    /// </summary>
    public static class VeniceMenu
    {
        public struct Option { public string label; public Action action; public Option(string l, Action a) { label = l; action = a; } }
        public static bool IsOpen => ChoiceMenu.IsOpen;
        public static string[] Labels => ChoiceMenu.Labels;
        public static void Show(string title, string body, params Option[] opts)
        {
            var list = new ChoiceMenu.Option[opts != null ? opts.Length : 0];
            for (int i = 0; i < list.Length; i++) list[i] = new ChoiceMenu.Option(opts[i].label, opts[i].action);
            ChoiceMenu.Show(title, body, list);
        }
    }
}
