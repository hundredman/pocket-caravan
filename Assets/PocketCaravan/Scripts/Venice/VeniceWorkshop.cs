using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 베네치아 특산 공방 (2026-10-26, 제안). 재료·삯을 내면 특산품을 만들어 준다. 진행은 VeniceSave.
    /// - 무라노 장식 공방: 유리그릇 2개 + 삯 15냥 → 유리 장식 1개 (가볍고 비싸다. 멀리 갈수록 값이 오른다)
    /// - 부라노 레이스 공방: 삯 25냥 → 레이스 손수건 1개 (하루 2장까지, 손으로 뜨는 데 시간이 걸린다)
    /// 장터 시세·저장 형식은 바꾸지 않는다. 만든 물건은 짐에 싣고, 실을 수 없으면 돈으로 받는다.
    /// </summary>
    public class VeniceWorkshop : Interactable
    {
        public enum Recipe { GlassArt, Lace }
        public Recipe recipe;

        public override string DisplayName => recipe == Recipe.GlassArt ? "무라노 장식 공방" : "부라노 레이스 공방";
        public override Kind Category => Kind.Market;

        public override void Interact(PlayerController player)
        {
            if (recipe == Recipe.GlassArt) GlassArt();
            else Lace();
        }

        void GlassArt()
        {
            var gm = GameManager.I;
            var glass = VeniceUtil.Item("venice_glass");
            int have = gm.Player.cargo.Count(glass);
            DialogueUI.Show("장식 공방 장인", $"유리그릇 두 개를 녹여 장식을 빚어 주지.\n삯은 15냥. 지금 유리그릇 {have}개.\n장식은 가볍고 멀리 갈수록 값이 오르네.",
                new DialogueUI.Choice("만들기 (유리그릇 2 + 15냥)", () =>
                {
                    if (have < 2) { DialogueUI.Show("장식 공방 장인", "유리그릇이 두 개 있어야 해."); return; }
                    if (gm.Player.money < 15) { DialogueUI.Show("장식 공방 장인", "삯이 모자라네."); return; }
                    gm.Player.cargo.Remove(glass, 2);
                    gm.Player.money -= 15;
                    Sfx.Play("success");
                    Notify.Show("유리 장식 완성! " + VeniceUtil.Give("venice_glass_art", 1), 4f);
                    VeniceSave.Set("glassart_made", VeniceSave.Get("glassart_made") + 1);
                }),
                new DialogueUI.Choice("다음에", () => { }));
        }

        void Lace()
        {
            var gm = GameManager.I;
            int today = VeniceSave.DoneToday("lace") ? (int)VeniceSave.Get("lace_count") : 0;
            if (today >= 2)
            {
                DialogueUI.Show("레이스 뜨는 할머니", "오늘은 두 장 다 떴단다.\n내일 다시 오렴.");
                return;
            }
            DialogueUI.Show("레이스 뜨는 할머니", $"한 장에 25냥이야. 오늘 {2 - today}장 더 뜰 수 있어.\n가벼워서 짐 걱정 없이 멀리 팔 수 있지.",
                new DialogueUI.Choice("한 장 부탁해요 (25냥)", () =>
                {
                    if (gm.Player.money < 25) { DialogueUI.Show("레이스 뜨는 할머니", "냥이 모자라구나."); return; }
                    gm.Player.money -= 25;
                    VeniceSave.MarkToday("lace");
                    VeniceSave.Set("lace_count", today + 1);
                    Sfx.Play("success");
                    Notify.Show("레이스 손수건을 받았다. " + VeniceUtil.Give("venice_lace", 1), 4f);
                }),
                new DialogueUI.Choice("다음에", () => { }));
        }
    }
}
