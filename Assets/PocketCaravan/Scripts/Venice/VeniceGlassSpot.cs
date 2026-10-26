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
        public override string DisplayName => "유리 가마 작업대";
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
            float w = 300f, h = 128f;
            var r = new Rect((UIStyle.VirtualWidth - w) / 2f, UIStyle.VirtualHeight / 2f - h / 2f, w, h);
            GUI.Box(r, "", UIStyle.Box);
            GUI.Label(new Rect(r.x + 12, r.y + 8, w - 24, 18), $"무라노 유리 불기   {Mathf.Min(round + 1, 3)} / 3", UIStyle.Rich);
            // 유리 방울 (맞힐수록 커진다)
            float glob = 8f + hits * 6f;
            var c = GUI.color;
            GUI.color = new Color(1f, .62f, .25f, .95f);
            GUI.DrawTexture(new Rect(r.x + w - 40 - glob / 2f, r.y + 34 - glob / 2f + 10f, glob, glob), Texture2D.whiteTexture);
            // 열 막대: 빨강 → 금빛 칸 → 빨강, 바늘
            var bar = new Rect(r.x + 16, r.y + 62, w - 32, 16);
            GUI.color = new Color(.45f, .16f, .1f, 1f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = new Color(1f, .82f, .3f, 1f);
            GUI.DrawTexture(new Rect(bar.x + (zone - zoneW / 2f) * bar.width, bar.y, zoneW * bar.width, bar.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(bar.x + needle * bar.width - 1.5f, bar.y - 4, 3, bar.height + 8), Texture2D.whiteTexture);
            // 지난 결과
            for (int i = 0; i < 3; i++)
            {
                GUI.color = i < round ? (results[i] ? new Color(.5f, .9f, .5f) : new Color(.9f, .4f, .35f)) : new Color(.6f, .6f, .6f, .6f);
                GUI.DrawTexture(new Rect(r.x + 16 + i * 14, r.y + 34, 10, 10), Texture2D.whiteTexture);
            }
            GUI.color = c;
            GUI.Label(new Rect(r.x + 12, r.y + 90, w - 24, 30),
                resultUntil > 0f ? message : "Space: 불기   Esc: 그만두기", UIStyle.Rich);
        }
    }
}
