using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 순위표 (2026-11-07, 서버 11단계). 채팅 "/순위" 또는 온라인 창(O) "순위표"로 연다.
    /// - 점수 올리기: NetScores.Report("판 아이디", 점수). 계정으로 접속했을 때만 올라간다 (손님·오프라인이면 조용히 넘어간다).
    ///   서버는 사람마다 최고점만 남긴다. 지역 미니게임은 끝날 때 이 한 줄만 넣으면 순위표에 판이 생긴다 (아이디: 영문 소문자·숫자·밑줄).
    /// - "money"(부자 상인) 판은 서버가 계정 게임 저장의 돈으로 만든다.
    /// 창은 11/07 UI B안: 종이 창 + 판 고르는 알약 + 10위까지 (1~3위 메달, 내 줄은 청록 테).
    /// </summary>
    public class NetScores : MonoBehaviour
    {
        [Serializable] class ScoreOut { public string t = "score"; public string board; public int value; }
        [Serializable] class GetOut { public string t = "getBoards"; }

        /// <summary>판 이름·단위. 모르는 판은 아이디를 그대로 보인다</summary>
        static readonly Dictionary<string, (string title, string unit)> Titles = new Dictionary<string, (string, string)>
        {
            ["money"] = ("부자 상인", "냥"),
            ["gondola"] = ("곤돌라 경주", "점"),
            [NetStackGame.Board] = ("상인회 짐 쌓기", "단"),   // 11/10 만국 상인회 수첩 미니게임
        };

        static NetScores inst;
        bool open, wantOpen, loading;
        float askedAt;
        float peekedAt = -999f;

        /// <summary>11/11 은샘: 받아 둔 순위표에서 그 판 1위 (아직 못 받았으면 null). 상인회 놀이판 위 "최고 기록" 표에 쓴다</summary>
        public static NetClient.BoardRow Top(string board)
        {
            if (inst == null) return null;
            var b = inst.boards.Find(x => x != null && x.id == board);
            NetClient.BoardRow top = null;
            if (b != null && b.rows != null) foreach (var row in b.rows) if (row != null && (top == null || row.value > top.value)) top = row;
            return top;
        }

        /// <summary>11/11 순위표를 창 없이 조용히 받아 둔다 (30초에 한 번까지)</summary>
        public static void Peek(bool force = false)
        {
            if (inst == null || !Online || !NetClient.I.ServerExtras) return;
            if (!force && Time.unscaledTime - inst.peekedAt < 30f) return;
            inst.peekedAt = Time.unscaledTime;
            NetClient.I.Send(new GetOut());
        }
        int tab;
        Vector2 lastMove = Vector2.one;
        List<NetClient.Board> boards = new List<NetClient.Board>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NetScores>() != null) return;
            var go = new GameObject("NetScores");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<NetScores>();
        }

        void Awake() => inst = this;
        void OnEnable() => NetClient.Received += OnReceived;
        void OnDisable() { NetClient.Received -= OnReceived; if (open) Close(); }

        static bool Online => NetClient.I != null && NetClient.I.Status == NetClient.State.Online;

        /// <summary>점수 올리기 (계정으로 접속했을 때만)</summary>
        public static void Report(string board, int value)
        {
            var net = NetClient.I;
            if (!Online || !net.IsAccount || !net.ServerExtras || string.IsNullOrEmpty(board) || value < 0) return;
            net.Send(new ScoreOut { board = board, value = value });
        }

        /// <summary>채팅 "/순위"면 열고 true (NetChat이 부른다)</summary>
        public static bool TryCommand(string text)
        {
            string w = text.Trim().ToLowerInvariant();
            if (w != "순위" && w != "순위표" && w != "rank") return false;
            Open();
            return true;
        }

        /// <summary>순위표를 연다. 채팅·온라인 창이 닫힌 다음 프레임에 열린다</summary>
        public static void Open()
        {
            if (inst == null) return;
            if (!Online) { NetChat.System("온라인일 때 순위표를 볼 수 있어요"); return; }
            if (!NetClient.I.ServerExtras) { NetChat.System("지금 서버는 아직 순위표를 몰라요 (서버를 새로 올리면 쓸 수 있어요)"); return; }
            inst.wantOpen = true;
        }

        public static string Title(string id) => Titles.TryGetValue(id, out var t) ? t.title : id;
        static string Unit(string id) => Titles.TryGetValue(id, out var t) ? t.unit : "점";

        void OnReceived(NetClient.Msg m)
        {
            if (m.t == "boards")
            {
                loading = false;
                boards = m.boards ?? new List<NetClient.Board>();
                boards.RemoveAll(b => b == null || string.IsNullOrEmpty(b.id));
                tab = Mathf.Clamp(tab, 0, Mathf.Max(0, boards.Count - 1));
            }
            else if (m.t == "scored")
            {
                if (m.rank > 0 && m.value >= m.best) NetChat.System($"{Title(m.board)} 순위 {m.rank}위! ({m.best}{Unit(m.board)})");
                if (!open) Peek(true);   // 11/11 놀이판 위 최고 기록을 바로 새로
            }
        }

        void Update()
        {
            if (wantOpen && !UIState.Blocking && !UIState.JustClosed)
            {
                wantOpen = false;
                if (UIState.TryOpen(this, Close))
                {
                    open = true;
                    loading = true;
                    askedAt = Time.unscaledTime;
                    lastMove = Vector2.one;
                    NetClient.I.Send(new GetOut());
                    Sfx.Play("open", .4f);
                }
            }
            if (!open) return;
            if (!Online) { Close(); return; }
            if (loading && Time.unscaledTime - askedAt > 8f) loading = false;
            if (InputReader.CancelPressed()) { Close(); return; }
            Vector2 mv = InputReader.MoveVector();
            if (boards.Count > 1 && mv.sqrMagnitude > 0.25f && lastMove.sqrMagnitude <= 0.25f && Mathf.Abs(mv.x) > Mathf.Abs(mv.y))
            {
                tab = (tab + (mv.x > 0 ? 1 : boards.Count - 1)) % boards.Count;
                Sfx.Play("open", .3f);
            }
            lastMove = mv;
        }

        void Close()
        {
            if (!open) return;
            open = false;
            UIState.Release(this);
        }

        void OnGUI()
        {
            if (!open) return;
            UIStyle.Begin();
            GUI.depth = -11;
            CuteUI.Dim();
            const float w = 300f, rowH = 15f;
            float h = 236f;
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2f), Mathf.Round((UIStyle.VirtualHeight - h) / 2f + 6f), w, h);
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11f, "순위표");
            if (CuteUI.CloseButton(r.xMax - 16f, r.y - 8f)) { Close(); return; }

            float x = r.x + 16f, fw = w - 32f, y = r.y + 16f;
            if (loading && boards.Count == 0) { CuteUI.LabelCenter(r.center.x, r.center.y, "불러오는 중…", CuteUI.Sub, false, true); return; }
            if (boards.Count == 0) { CuteUI.LabelCenter(r.center.x, r.center.y, "아직 순위표가 없어요", CuteUI.Sub, false, true); return; }

            // 판 고르기: 알약 (고른 판은 청록)
            float px = x;
            for (int i = 0; i < boards.Count; i++)
            {
                string label = Title(boards[i].id);
                float pw = CuteUI.Width(label, true, true) + 16f;
                if (px + pw > x + fw) break;
                var pr = new Rect(px, y, pw, 16f);
                bool on = i == tab;
                CuteUI.Box(pr, 7, on ? CuteUI.Interact : CuteUI.Paper2, on ? CuteUI.Ink : CuteUI.Line);
                CuteUI.LabelCenter(pr.center.x, pr.center.y, label, on ? CuteUI.OnInteract : CuteUI.Text, true, true);
                if (GUI.Button(pr, GUIContent.none, GUIStyle.none) && !on) { tab = i; Sfx.Play("open", .3f); }
                px += pw + 5f;
            }
            y += 24f;

            var board = boards[Mathf.Clamp(tab, 0, boards.Count - 1)];
            string unit = Unit(board.id), me = NetClient.I != null ? NetClient.I.MyName : "";
            if (board.rows == null || board.rows.Count == 0) CuteUI.LabelCenter(r.center.x, y + 60f, "아직 기록이 없어요", CuteUI.Sub, false, true);
            else
                for (int i = 0; i < board.rows.Count && i < 10; i++)
                {
                    var row = board.rows[i];
                    var rr = new Rect(x, y + i * (rowH + 2f), fw, rowH);
                    bool mine = string.Equals(row.name, me, StringComparison.OrdinalIgnoreCase);
                    CuteUI.Box(rr, 6, mine ? CuteUI.Paper : i % 2 == 0 ? CuteUI.Paper2 : CuteUI.Paper, mine ? CuteUI.Interact : CuteUI.Line, mine ? 2 : 1);
                    Color32 medal = i == 0 ? CuteUI.Deco : i == 1 ? new Color32(0xC9, 0xC9, 0xD6, 0xFF) : i == 2 ? new Color32(0xE0, 0x9A, 0x5C, 0xFF) : CuteUI.Paper2;
                    CuteUI.Circle(rr.x + 3f, rr.y + 1.5f, 12, medal, i < 3 ? CuteUI.Ink : CuteUI.Line);
                    CuteUI.LabelCenter(rr.x + 9f, rr.center.y, (i + 1).ToString(), CuteUI.Text, true, i < 3);
                    CuteUI.Label(rr.x + 21f, rr.center.y, row.name, CuteUI.Text, false, mine);
                    CuteUI.LabelRight(rr.xMax - 7f, rr.center.y, $"{row.value:N0}{unit}", mine ? CuteUI.InteractLo : CuteUI.Text, false, true);
                }
            string hint = board.id == "money" ? "계정에 저장된 게임의 돈" : "사람마다 최고 기록";
            CuteUI.LabelCenter(r.center.x, r.yMax - 14f, (boards.Count > 1 ? "← → 판 바꾸기 · " : "") + "Esc 닫기 · " + hint, CuteUI.Sub, true);
        }
    }
}
