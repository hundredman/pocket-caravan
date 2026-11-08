using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 부라노 미니게임: 그물 던지기 (2026-10-26, 제안). 잔교 끝에서 Space.
    /// 물속 물고기 떼(그림자)가 오가는데, 던질 힘 막대가 저절로 오르내린다. 막대가 물고기 떼 거리와 맞을 때 Space로 던진다.
    /// 세 번 던져 잡은 만큼 절인 생선(venice_fish)을 받는다(실을 수 없으면 돈). 하루 한 번(VeniceSave "net"). 무료.
    /// </summary>
    public class VeniceNetSpot : Interactable
    {
        public override string DisplayName => VeniceLabels.Place(this, "그물 던지는 잔교");   // 가까이 가야 이름표가 뜬다
        public override Kind Category => Kind.Market;

        bool playing;
        int throwsLeft, caught, startFrame;
        float power, fish, fishSpeed, flashUntil, resultUntil;
        bool lastHit;
        string message;

        public override void Interact(PlayerController player)
        {
            if (VeniceSave.DoneToday("net"))
            {
                DialogueUI.Show("부라노 어부", "오늘 물고기는 다 놀라서 숨었어.\n내일 다시 던져 보게.");
                return;
            }
            DialogueUI.Show("부라노 어부", "그물 한번 던져 볼 텐가?\n힘 막대가 물고기 떼 거리와 맞을 때 Space.\n세 번 던져서 잡은 건 자네 몫이야.",
                new DialogueUI.Choice("던져 보기", Begin),
                new DialogueUI.Choice("다음에", () => { }));
        }

        void Begin()
        {
            playing = true; throwsLeft = 3; caught = 0; startFrame = Time.frameCount;
            fish = Random.Range(.3f, .9f); fishSpeed = Random.Range(.15f, .3f) * (Random.value < .5f ? -1f : 1f);
            flashUntil = 0f; resultUntil = 0f; message = null;
            UIState.Blocking = true;
            Sfx.Play("open");
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
            float dt = Time.unscaledDeltaTime;
            fish += fishSpeed * dt;
            if (fish > .92f || fish < .22f) { fishSpeed = -fishSpeed; fish = Mathf.Clamp(fish, .22f, .92f); }
            power = Mathf.PingPong(Time.unscaledTime * (1.1f + (3 - throwsLeft) * .3f), 1f);
            if (Time.frameCount > startFrame + 1 && InputReader.InteractPressed() && Time.unscaledTime > flashUntil)
            {
                lastHit = Mathf.Abs(power - fish) <= .08f;
                if (lastHit) { caught++; Sfx.Play("success"); } else Sfx.Play("drop");
                flashUntil = Time.unscaledTime + .6f;
                throwsLeft--;
                fish = Random.Range(.3f, .9f);
                if (throwsLeft <= 0) Reward();
            }
            if (InputReader.CancelPressed()) { throwsLeft = 0; Reward(); }
        }

        void Reward()
        {
            VeniceSave.MarkToday("net");
            string got = VeniceUtil.Give("venice_fish", caught);
            message = caught > 0 ? $"{caught}번 건졌다! " + got : "빈 그물… 물고기들이 빨랐다.";
            resultUntil = Time.unscaledTime + 2.2f;
            VeniceSave.Set("net_total", VeniceSave.Get("net_total") + caught);
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
            // 11/07 UI B안: 종이 창 + 주황 제목. 위에 남은 그물·잡은 수 알약, 가운데 물(물고기 떼 그림자), 아래 힘 막대, 안내 키캡 또는 결과
            const float w = 300f, h = 150f;
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2f), Mathf.Round(UIStyle.VirtualHeight / 2f - h / 2f), w, h);
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11f, "부라노 그물 던지기");
            string left = $"남은 그물 {throwsLeft}", caughtLabel = $"잡음 {caught}";
            var p1 = new Rect(r.x + 16f, r.y + 14f, CuteUI.Width(left, true, true) + 16f, 15f);
            var p2 = new Rect(p1.xMax + 6f, p1.y, CuteUI.Width(caughtLabel, true, true) + 16f, 15f);
            CuteUI.Box(p1, 7, CuteUI.Paper2, CuteUI.Line);
            CuteUI.LabelCenter(p1.center.x, p1.center.y, left, CuteUI.Text, true, true);
            CuteUI.Box(p2, 7, caught > 0 ? CuteUI.Up : CuteUI.Paper2, caught > 0 ? CuteUI.Ink : CuteUI.Line);
            CuteUI.LabelCenter(p2.center.x, p2.center.y, caughtLabel, caught > 0 ? CuteUI.White : CuteUI.Text, true, true);
            // 물 (가까움 → 멂), 물고기 떼 그림자, 던질 거리 표시
            var water = new Rect(r.x + 16f, r.y + 36f, w - 32f, 44f);
            CuteUI.Box(water, 5, new Color32(0x3B, 0x8C, 0x99, 0xFF), CuteUI.Ink);
            for (int i = 0; i < 6; i++)   // 잔물결
                CuteUI.Fill(new Rect(water.x + 14f + (i * 47) % (water.width - 34f), water.y + 8f + (i * 13) % 28, 8f, 1f), new Color32(0x7F, 0xC4, 0xCC, 0xFF));
            float fx = Mathf.Round(water.x + fish * water.width);
            CuteUI.Box(new Rect(fx - 11f, water.y + 13f, 22f, 16f), 7, new Color32(0x1E, 0x4F, 0x58, 0xFF), new Color32(0x1E, 0x4F, 0x58, 0xFF));
            if (Time.unscaledTime < flashUntil)
                CuteUI.Box(new Rect(Mathf.Round(water.x + power * water.width - 12f), water.y + 8f, 24f, 28f), 6,
                    lastHit ? CuteUI.Deco : CuteUI.Paper, CuteUI.Ink);
            CuteUI.Bar(new Rect(water.x, water.y + 52f, water.width, 10f), power, CuteUI.Deco, CuteUI.White);
            if (resultUntil > 0f) CuteUI.WrapCenter(r.x + 14f, r.y + 104f, w - 28f, message ?? "", CuteUI.Text);
            else
            {
                float x = r.center.x - (CuteUI.KeycapWidth("Space") + CuteUI.Width(" 던지기", true) + 14f + CuteUI.KeycapWidth("Esc") + CuteUI.Width(" 그만", true)) / 2f;
                x += CuteUI.Keycap(x, r.y + 106f, "Space", true);
                x = CuteUI.Label(x + 3f, r.y + 112.5f, "던지기", CuteUI.Text, true) + 14f;
                x += CuteUI.Keycap(x, r.y + 106f, "Esc", true);
                CuteUI.Label(x + 3f, r.y + 112.5f, "그만", CuteUI.Text, true);
                CuteUI.LabelCenter(r.center.x, r.y + 129f, "막대 끝이 물고기 떼에 닿을 때 던져요 (클릭해도 돼요)", CuteUI.Sub, true);
            }
            if (CuteUI.CloseButton(r.xMax - 16f, r.y - 8f)) InputReader.Press('\x1b');   // 11/08 X = Esc(그만)
            InputReader.ClickAsSpace();   // 11/08 빈 곳 클릭 = Space(던지기)
        }
    }
}
