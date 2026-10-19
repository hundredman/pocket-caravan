using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 마을 의뢰 게시판. Space로 열어 의뢰를 맡거나, 이 마을로 가는 의뢰를 납품한다.
    /// </summary>
    public class ContractBoard : Interactable
    {
        public string town;

        bool open;
        int openedFrame;
        string message = "";
        Vector2 scroll;

        ContractManager Cm => GameManager.I.Contracts;

        public override string DisplayName => "의뢰 게시판";
        public override Kind Category => Kind.Board;

        public override void Interact(PlayerController player)
        {
            open = true;
            openedFrame = Time.frameCount;
            message = "";
            UIState.Blocking = true;
            Sfx.Play("open");
        }

        void Close()
        {
            open = false;
            UIState.Release();
        }

        void Update()
        {
            if (!open || Time.frameCount == openedFrame) return;
            if (InputReader.CancelPressed() || InputReader.InteractPressed()) Close();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (open) Close();
        }

        void OnGUI()
        {
            if (!open || GameManager.I == null) return;
            UIStyle.Begin();
            GUI.depth = -10;   // 다른 글자들 위에
            var gm = GameManager.I;
            var p = gm.Player;

            float w = Mathf.Min(470, UIStyle.VirtualWidth - 16), h = 310;
            var wrapRich = new GUIStyle(UIStyle.Rich) { wordWrap = true };
            var rect = new Rect((UIStyle.VirtualWidth - w) / 2, (UIStyle.VirtualHeight - h) / 2, w, h);
            GUI.Box(rect, "", UIStyle.Box);
            GUILayout.BeginArea(new Rect(rect.x + 12, rect.y + 8, w - 24, h - 16));
            GUILayout.Label($"<color=#FFE08A>{Cm.TownName(town)} 의뢰 게시판</color>", UIStyle.Rich);
            GUILayout.Label($"돈 {p.money}   신용 {p.credit}   짐 {p.TotalWeight:0.#}/{p.Capacity:0.#}   맡은 의뢰 {p.contracts.Count}/{ContractManager.MaxActive}", UIStyle.Small);

            GUILayout.Label($"남는 적재 공간 {Mathf.Max(0, p.Capacity-p.TotalWeight):0.#} · 배달 짐도 공간을 씁니다. 귀환 상품과 비교하세요.", UIStyle.Small);
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(202));

            // 1) 이 마을로 가져온 짐
            bool header = false;
            foreach (var c in p.contracts.ToArray())
            {
                if (c.toTown != town) continue;
                if (!header) { GUILayout.Label("납품할 짐", UIStyle.Rich); header = true; }
                GUILayout.BeginHorizontal();
                string state = c.dropped ? "<color=#ff8a7a>길에 떨어뜨림</color>" : $"상태 {Mathf.RoundToInt(c.condition * 100)}%";
                GUILayout.Label($"{c.cargo}  ·  {state}  ·  남은 시간 {Cm.TimeLeftText(c)}  ·  보수 {c.reward}", wrapRich, GUILayout.Width(w - 110));
                GUI.enabled = !c.dropped;
                if (GUILayout.Button("납품", UIStyle.Button, GUILayout.Width(60), GUILayout.Height(18)))
                {
                    var r = Cm.Deliver(c, town);
                    message = r.message;
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            if (header) GUILayout.Space(6);

            // 2) 맡을 수 있는 의뢰
            GUILayout.Label("맡을 수 있는 의뢰", UIStyle.Rich);
            var list = Cm.Available(town);
            if (list.Count == 0) GUILayout.Label("지금은 의뢰가 없다. 몇 시간 뒤에 다시 와 보자.", UIStyle.Small);
            foreach (var c in list.ToArray())
            {
                GUILayout.BeginHorizontal();
                string route = c.quotedRouteSteps > 0 ? $" · 연결 {c.quotedRouteSteps}구간" : "";
                string premium = c.Premium ? $" <color=#ffd84a>(신용 {c.minCredit}↑)</color>" : "";
                GUILayout.Label($"{c.cargo}{premium}\n→ {Cm.TownName(c.toTown)} · 무게 {c.weight:0} {route} · 기한 {c.hours:0}시간 · <color=#a8f0a0>보수 {c.reward}</color>",
                    wrapRich, GUILayout.Width(w - 110));
                string why = Cm.CanAccept(c);
                if (GUILayout.Button("맡기", UIStyle.Button, GUILayout.Width(60), GUILayout.Height(18)))
                {
                    if (why != null) { message = why; Sfx.Play("cancel"); }
                    else if (Cm.Accept(town, c)) message = $"{Josa.Eul(c.cargo)} 맡았다. {Cm.TownName(c.toTown)} 게시판에 가져가자.";
                    else message = "이 의뢰는 더 이상 맡을 수 없습니다. 게시판을 다시 확인해 주세요.";
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(2);
            }
            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.Label(message, UIStyle.Wrap);
            if (GUILayout.Button("닫기 (Esc)", UIStyle.Button, GUILayout.Width(80), GUILayout.Height(18))) Close();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
