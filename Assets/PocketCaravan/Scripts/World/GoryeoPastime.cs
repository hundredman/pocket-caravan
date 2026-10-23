using UnityEngine;

namespace PocketCaravan
{
    /// <summary>A short optional catch-and-release fishing game. Never mutates trade/save rewards.</summary>
    public sealed class GoryeoPastime : Interactable
    {
        public override string DisplayName => "나루 낚시";
        bool open;
        int openedFrame, attempts, catches, best;
        float elapsed;
        bool waiting;
        string result;
        public bool IsOpen => open;
        public int Attempts => attempts;
        public int Catches => catches;
        public float Needle => .5f + .46f * Mathf.Sin(elapsed * (2.1f + attempts * .22f) - Mathf.PI * .5f);
        public float Target => attempts == 0 ? .64f : attempts == 1 ? .35f : .7f;

        void Awake() => blocksMovement = false;

        public override void Interact(PlayerController player)
        {
            if (open || UIState.Blocking) return;
            open = true;
            openedFrame = Time.frameCount;
            attempts = catches = 0;
            elapsed = 0f;
            waiting = true;
            result = "찌가 청록색 구간에 들어오면 낚아채세요.";
            UIState.Blocking = true;
        }

        void Update()
        {
            if (!open || Time.frameCount == openedFrame) return;
            if (InputReader.CancelPressed()) { Close(); return; }
            if (!waiting && attempts < 3) elapsed += Time.unscaledDeltaTime;
            if (InputReader.ConfirmPressed())
            {
                if (waiting && attempts < 3) Cast();
                else if (attempts < 3) Strike();
            }
        }

        public void Cast()
        {
            if (!open || !waiting || attempts >= 3) return;
            elapsed = 0f;
            waiting = false;
            result = "물결을 보고 천천히 기다려 보세요.";
        }

        public void Strike()
        {
            if (!open || waiting || attempts >= 3 || Time.frameCount == openedFrame) return;
            bool caught = Mathf.Abs(Needle - Target) <= .105f;
            attempts++;
            if (caught) catches++;
            waiting = true;
            best = Mathf.Max(best, catches);
            result = caught ? "작은 물고기를 낚았어요. 다시 물로 놓아줍니다." : "살짝 빗나갔어요. 다음 물결을 기다려 볼까요?";
            if (attempts == 3) result = $"세 번의 물결, {catches}마리를 만났어요.";
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            UIState.Release();
        }

        protected override void OnDisable()
        {
            Close();
            base.OnDisable();
        }

        void OnGUI()
        {
            if (!open) return;
            UIStyle.Begin();
            float w = Mathf.Min(340f, UIStyle.VirtualWidth - 20f);
            var r = new Rect((UIStyle.VirtualWidth - w) * .5f, 94, w, 202);
            GUI.Box(r, "", UIStyle.Box);
            GUI.Label(new Rect(r.x+14,r.y+10,w-28,22), "예성 나루 · 잠깐의 낚시", UIStyle.Center);
            GUI.Label(new Rect(r.x+14,r.y+36,w-28,30), "잠시 낚시를 즐기고, 물고기는 다시 놓아줘요.", UIStyle.Wrap);
            var track = new Rect(r.x+24,r.y+79,w-48,22);
            var old = GUI.color;
            GUI.color = new Color(.12f,.22f,.28f); GUI.DrawTexture(track,Texture2D.whiteTexture);
            if (attempts < 3)
            {
                GUI.color = new Color(.34f,.67f,.58f);
                GUI.DrawTexture(new Rect(track.x+(Target-.105f)*track.width,track.y,.21f*track.width,track.height),Texture2D.whiteTexture);
                GUI.color = new Color(.97f,.85f,.56f);
                GUI.DrawTexture(new Rect(track.x+Needle*track.width-2,track.y-3,4,track.height+6),Texture2D.whiteTexture);
            }
            GUI.color = old;
            GUI.Label(new Rect(r.x+14,r.y+110,w-28,34),result,UIStyle.Wrap);
            GUI.Label(new Rect(r.x+14,r.y+145,w-28,20),$"{attempts}/3번 · 만난 물고기 {catches} · 이번 방문 최고 {best}",UIStyle.Center);
            if (attempts < 3 && GUI.Button(new Rect(r.x+14,r.y+173,w-96,22),waiting ? "낚싯줄 던지기 · Space" : "낚아채기 · Space",UIStyle.Button))
            { if (waiting) Cast(); else Strike(); }
            if (GUI.Button(new Rect(r.x+w-74,r.y+173,60,22),"닫기 · Esc",UIStyle.Button)) Close();
        }
    }
}
