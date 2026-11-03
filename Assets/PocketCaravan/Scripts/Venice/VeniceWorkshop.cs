namespace PocketCaravan
{
    /// <summary>
    /// 베네치아 특산 공방 자리 (2026-10-26). 2026-11-04 통합: 만드는 규칙은 공용 Crafting
    /// ("venice_glassart" 무라노 장식 공방: 유리그릇 2 + 15냥 → 유리 장식, "venice_lace" 부라노 레이스 공방: 25냥 → 레이스 손수건, 하루 2장).
    /// 바로 받는 공방(시간 0)이라 플레이는 예전과 같다.
    /// </summary>
    public class VeniceWorkshop : Interactable
    {
        public enum Recipe { GlassArt, Lace }
        public Recipe recipe;

        string Id => recipe == Recipe.GlassArt ? "venice_glassart" : "venice_lace";
        public override string DisplayName => VeniceLabels.Place(this, recipe == Recipe.GlassArt ? "무라노 장식 공방" : "부라노 레이스 공방");   // 가까이 가야 이름표가 뜬다
        public override Kind Category => Kind.Market;
        public override void Interact(PlayerController player) => Crafting.Open(Id);
    }
}
