using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 여관에서 쉬기 (2026-10-27, 제안). 리알토 여관 주인·대상 숙소 주인 옆 침대(VeniceDaily가 놓는다).
    /// 15냥을 내면 화면이 저물었다가 4시간 뒤 깬다: 체력 가득, 8시간 동안 "푹 쉼"(달릴 때 체력이 절반만 준다, VeniceDaily가 적용).
    /// 시간은 공용 GameClock.Advance로 넘긴다(맵 이동과 같은 방식). 저장: VeniceSave "rested_until".
    /// </summary>
    public class VeniceInn : Interactable
    {
        public override string DisplayName => "여관에서 쉬기";
        public override Kind Category => Kind.Market;
        const int Price = 15;

        float sleepT = -1f;

        void Start()
        {
            var r = gameObject.AddComponent<SpriteRenderer>();
            r.sprite = VeniceUtil.Sprite("inn_bed");
            r.sortingOrder = 8;
        }

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.I;
            if (VeniceDaily.Rested) { DialogueUI.Show("여관", "이미 푹 쉬었다. 몸이 가볍다."); return; }
            DialogueUI.Show("여관", $"푹신한 침대다. {Price}냥에 네 시간 자고 가면 한나절은 덜 지친다.",
                new DialogueUI.Choice($"쉬어 가기 ({Price}냥)", () =>
                {
                    if (gm.Player.money < Price) { Notify.Show("돈이 모자란다.", 2.5f); return; }
                    gm.Player.money -= Price;
                    Sleep();
                }),
                new DialogueUI.Choice("다음에", () => { }));
        }

        /// <summary>잠들기 시작 (돈은 부르는 쪽이 받는다)</summary>
        public void Sleep()
        {
            sleepT = 0f;
            UIState.Blocking = true;
            Sfx.Play("cancel", .6f);
        }

        void Update()
        {
            if (sleepT < 0f) return;
            UIState.Blocking = true;                                  // 대화창이 닫히며 풀어도 자는 동안은 멈춘다
            float before = sleepT;
            sleepT += Time.unscaledDeltaTime;
            if (before < 1.2f && sleepT >= 1.2f)                    // 화면이 다 어두워진 순간
            {
                var gm = GameManager.I;
                gm.Clock.Advance(4f);
                gm.Player.stamina = 1f;
                VeniceSave.Set("rested_until", gm.Clock.TotalHours + 8f);
            }
            if (sleepT >= 2.6f)
            {
                sleepT = -1f;
                UIState.Release();
                Sfx.Play("levelup", .6f);
                Notify.Show("개운하게 깼다! 8시간 동안 푹 쉼 (달릴 때 덜 지친다)", 4f);
            }
        }

        void OnGUI()
        {
            if (sleepT < 0f) return;
            UIStyle.Begin();
            GUI.depth = -30;
            float a = sleepT < 1.2f ? sleepT / 1.2f : 1f - Mathf.Clamp01((sleepT - 1.6f) / 1f);
            var c = GUI.color;
            GUI.color = new Color(.05f, .04f, .12f, a);
            GUI.DrawTexture(new Rect(0, 0, UIStyle.VirtualWidth, UIStyle.VirtualHeight), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(0, UIStyle.VirtualHeight / 2f - 10, UIStyle.VirtualWidth, 20), "<color=#C8C8FF>Z z z …</color>", UIStyle.CenterRich);
            GUI.color = c;
        }
    }
}
