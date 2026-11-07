using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 무라노 미니게임: 유리 불기 (2026-10-26, 제안). 가마 앞 작업대에서 Space.
    /// 재료값 10냥. 세 번 불기: 움직이는 열 바늘이 금빛 칸에 들어왔을 때 Space (칸은 점점 좁아진다).
    /// 맞힌 수에 따라 유리그릇(venice_glass)을 받는다: 3번 = 2개 + 상금 30냥, 2번 = 2개, 1번 = 1개, 0번 = 깨짐.
    /// 하루 한 번(VeniceSave "glass"). 게임 중에는 UIState.Blocking으로 이동·시간을 멈춘다.
    /// </summary>
    public class VeniceGlassSpot : Interactable
    {
        public override string DisplayName => VeniceLabels.Place(this, "유리 가마 작업대");   // 가까이 가야 이름표가 뜬다
        public override Kind Category => Kind.Market;

        bool playing;
        int round, hits, startFrame;
        float needle, speed, zone, zoneW, resultUntil;
        string message;
        readonly bool[] results = new bool[3];

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.I;
            if (VeniceSave.DoneToday("glass"))
            {
                DialogueUI.Show("공방 조수", "오늘 가마 불은 다 썼어요.\n내일 다시 와서 불어 봐요.");
                return;
            }
            DialogueUI.Show("공방 조수", "유리를 불어 볼래요? 재료값은 10냥.\n바늘이 금빛 칸에 들어올 때 Space!\n세 번 다 맞히면 상금도 줘요.",
                new DialogueUI.Choice("불어 보기 (10냥)", () =>
                {
                    if (gm.Player.money < 10) { DialogueUI.Show("공방 조수", "재료값이 모자라요."); return; }
                    gm.Player.money -= 10;
                    Begin();
                }),
                new DialogueUI.Choice("다음에", () => { }));
        }

        void Begin()
        {
            playing = true; round = 0; hits = 0; startFrame = Time.frameCount;
            message = null; resultUntil = 0f;
            NewRound();
            UIState.Blocking = true;
            Sfx.Play("open");
        }

        void NewRound()
        {
            zoneW = new[] { .22f, .16f, .11f }[Mathf.Min(round, 2)];
            zone = Random.Range(.2f + zoneW / 2f, .9f - zoneW / 2f);
            speed = 1.1f + round * .45f;
            needle = 0f;
        }

        void Update()
        {
            if (!playing) return;
            UIState.Blocking = true;
            if (resultUntil > 0f)
            {
                if (Time.unscaledTime >= resultUntil) Finish();
                return;
            }
            needle = Mathf.PingPong(Time.unscaledTime * speed, 1f);
            if (Time.frameCount > startFrame + 1 && InputReader.InteractPressed())
            {
                bool hit = Mathf.Abs(needle - zone) <= zoneW / 2f;
                results[round] = hit;
                if (hit) { hits++; Sfx.Play("success"); } else Sfx.Play("fail");
                round++;
                if (round >= 3) Reward();
                else NewRound();
            }
            if (InputReader.CancelPressed() && resultUntil <= 0f) { round = 3; Reward(); }
        }

        void Reward()
        {
            var gm = GameManager.I;
            VeniceSave.MarkToday("glass");
            int count = hits >= 2 ? 2 : hits;
            string got = VeniceUtil.Give("venice_glass", count);
            if (hits == 3) { gm.Player.money += 30; Sfx.Play("gold"); }
            message = hits == 3 ? "완벽한 유리! 상금 30냥. " + got
                    : hits > 0 ? $"{hits}번 맞혔다. " + got
                    : "유리가 금이 가 버렸다… 내일 다시.";
            resultUntil = Time.unscaledTime + 2.2f;
            VeniceSave.Set("glass_best", Mathf.Max(VeniceSave.Get("glass_best"), hits));
        }

        void Finish()
        {
            playing = false;
            UIState.Release();
            if (!string.IsNullOrEmpty(message)) Notify.Show(message, 4f);
        }

        void OnGUI()
        {
            if (!playing) return;
            UIStyle.Begin();
            GUI.depth = -20;
            // 11/07 UI B안: 종이 창 + 주황 제목. 왼쪽 위 세 번의 결과(초록 맞힘 · 빨강 놓침 · 회색 아직),
            // 오른쪽 위 유리 방울(맞힐수록 커진다), 가운데 열 막대(금빛 칸에 바늘을 맞춘다), 아래 안내 키캡 또는 결과
            const float w = 300f, h = 136f;
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2f), Mathf.Round(UIStyle.VirtualHeight / 2f - h / 2f), w, h);
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11f, "무라노 유리 불기");
            for (int i = 0; i < 3; i++)
            {
                var fill = i < round ? (results[i] ? CuteUI.Up : CuteUI.Down) : CuteUI.Paper2;
                CuteUI.Circle(r.x + 18f + i * 16f, r.y + 22f, 12, fill, i < round ? CuteUI.Ink : CuteUI.Line);
            }
            CuteUI.Label(r.x + 70f, r.y + 28f, $"{Mathf.Min(round + 1, 3)} / 3번째", CuteUI.Sub, true);
            int glob = 10 + hits * 6;
            CuteUI.Circle(r.xMax - 34f - glob / 2f, r.y + 34f - glob / 2f, glob, CuteUI.Frame, CuteUI.Ink, 1, 2);
            CuteUI.Fill(new Rect(r.xMax - 34f - glob / 4f, r.y + 34f - glob / 4f, 2f, 2f), CuteUI.White);   // 유리 반짝임
            var bar = new Rect(r.x + 18f, r.y + 60f, w - 36f, 16f);
            CuteUI.Box(bar, 4, new Color32(0x8A, 0x2C, 0x1C, 0xFF), CuteUI.Ink);
            CuteUI.Fill(new Rect(bar.x + 1f + (zone - zoneW / 2f) * (bar.width - 2f), bar.y + 1f, zoneW * (bar.width - 2f), bar.height - 2f), CuteUI.Deco);
            float nx = Mathf.Round(bar.x + needle * bar.width);
            CuteUI.Box(new Rect(nx - 2f, bar.y - 5f, 5f, bar.height + 10f), 2, CuteUI.White, CuteUI.Ink);
            if (resultUntil > 0f) CuteUI.WrapCenter(r.x + 14f, r.y + 88f, w - 28f, message ?? "", CuteUI.Text);
            else
            {
                float x = r.center.x - (CuteUI.KeycapWidth("Space") + CuteUI.Width(" 불기", true) + 14f + CuteUI.KeycapWidth("Esc") + CuteUI.Width(" 그만두기", true)) / 2f;
                x += CuteUI.Keycap(x, r.y + 96f, "Space", true);
                x = CuteUI.Label(x + 3f, r.y + 102.5f, "불기", CuteUI.Text, true) + 14f;
                x += CuteUI.Keycap(x, r.y + 96f, "Esc", true);
                CuteUI.Label(x + 3f, r.y + 102.5f, "그만두기", CuteUI.Text, true);
            }
        }
    }
}
