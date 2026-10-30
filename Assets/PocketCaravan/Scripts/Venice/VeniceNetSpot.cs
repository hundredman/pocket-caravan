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
            float w = 300f, h = 140f;
            var r = new Rect((UIStyle.VirtualWidth - w) / 2f, UIStyle.VirtualHeight / 2f - h / 2f, w, h);
            GUI.Box(r, "", UIStyle.Box);
            GUI.Label(new Rect(r.x + 12, r.y + 8, w - 24, 18), $"부라노 그물 던지기   남은 그물 {throwsLeft}   잡음 {caught}", UIStyle.Rich);
            var c = GUI.color;
            // 물 (가까움 → 멂), 물고기 떼 그림자, 던질 거리 표시
            var water = new Rect(r.x + 16, r.y + 32, w - 32, 44);
            GUI.color = new Color(.23f, .55f, .6f, 1f);
            GUI.DrawTexture(water, Texture2D.whiteTexture);
            GUI.color = new Color(.1f, .25f, .3f, .7f);
            float fx = water.x + fish * water.width;
            GUI.DrawTexture(new Rect(fx - 10, water.y + 14, 20, 12), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(fx - 6, water.y + 10, 12, 20), Texture2D.whiteTexture);
            if (Time.unscaledTime < flashUntil)
            {
                GUI.color = lastHit ? new Color(.95f, .95f, .7f, .9f) : new Color(1f, 1f, 1f, .5f);
                GUI.DrawTexture(new Rect(water.x + power * water.width - 12, water.y + 8, 24, 28), Texture2D.whiteTexture);
            }
            // 힘 막대
            var bar = new Rect(water.x, water.y + 54, water.width, 10);
            GUI.color = new Color(.3f, .25f, .2f, 1f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = new Color(.95f, .8f, .4f, 1f);
            GUI.DrawTexture(new Rect(bar.x, bar.y, power * bar.width, bar.height), Texture2D.whiteTexture);
            GUI.color = c;
            GUI.Label(new Rect(r.x + 12, r.y + 104, w - 24, 30),
                resultUntil > 0f ? message : "Space: 던지기 (막대 끝이 물고기 떼에 닿을 때)   Esc: 그만", UIStyle.Rich);
        }
    }
}
