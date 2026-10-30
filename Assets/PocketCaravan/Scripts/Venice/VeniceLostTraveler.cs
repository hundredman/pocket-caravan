using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 길 잃은 순례자 (2026-10-27, 제안). 본토 길 맵에 안개·비가 올 때 VeniceEvents가 세운다.
    /// "따라오세요"를 고르면 VeniceEvents가 동행을 시작한다(장터 마을에 함께 들어가면 사례).
    /// "물 한 모금 나눠 주기"는 5냥을 내고 신용 +1(바로 끝). 고르지 않고 떠나면 다음 날씨까지 다시 오지 않는다.
    /// </summary>
    public class VeniceLostTraveler : Interactable
    {
        public override string DisplayName => "길 잃은 순례자";
        public override Kind Category => Kind.Quest;

        void Start()
        {
            var r = gameObject.AddComponent<SpriteRenderer>();
            r.sprite = VeniceUtil.Sprite("pilgrim");
            r.sortingOrder = 10;
            transform.position += new Vector3(0f, -.5f, 0f);
        }

        void Update()
        {
            transform.localScale = new Vector3(1f, 1f + Mathf.Sin(Time.time * 2f) * .02f, 1f);
        }

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.I;
            string weather = VeniceAtmosphere.Now == VeniceAtmosphere.Weather.Fog ? "안개" : "비";
            VeniceMenu.Show("길 잃은 순례자", $"{weather} 때문에 길을 잃었소. 장터 있는 마을까지만 데려다주겠소?",
                new VeniceMenu.Option("따라오세요", () => gm.GetComponent<VeniceEvents>()?.StartFollow(this)),
                new VeniceMenu.Option("물 한 모금 나눠 주기 (5냥)", () =>
                {
                    if (gm.Player.money < 5) { Notify.Show("돈이 모자란다.", 2.5f); return; }
                    gm.Player.money -= 5;
                    gm.Player.credit += 1;
                    VeniceEvents.Met();
                    Sfx.Play("success", .7f);
                    Notify.Show("순례자가 고마워하며 길을 물어 떠났다. 신용 +1", 3.5f);
                    Destroy(gameObject);
                }),
                new VeniceMenu.Option("갈 길이 바빠서", () => { }));
        }
    }
}
