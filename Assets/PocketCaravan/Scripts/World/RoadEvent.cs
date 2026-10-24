using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 돌발 사건 2종.
    /// 쓰러진 행인: 도우면 1시간이 지나고 관계 지역에 평판 기여.
    /// 떠돌이 상인: 물건 하나를 싸게 판다 (몇 개 안 됨).
    /// </summary>
    public class RoadEvent : Interactable
    {
        public enum Type { FallenTraveler, Peddler }
        public Type type;
        public ReputationScope reputationScope;
        public string reputationTarget = "goryeo";
        public ReputationSize reputationSize = ReputationSize.Small;
        public Transform body;   // 쓰러진 모습은 그림을 눕혀서

        bool done;
        ItemData offer;
        int offerPrice, offerLeft;

        public override string DisplayName => type == Type.Peddler ? "떠돌이 상인" : (done ? "행인" : "쓰러진 행인");
        public override Kind Category => type == Type.Peddler ? Kind.Market : Kind.Person;

        void Start()
        {
            if (type == Type.FallenTraveler && body != null) body.localRotation = Quaternion.Euler(0, 0, 90);
            if (type == Type.Peddler)
            {
                var gm = GameManager.I;
                var items = gm.Db.items.FindAll(i => i != null && !i.questItem && i.basePrice >= 8);
                offer = items[Random.Range(0, items.Count)];
                // 기준가로 만든 제안 가격. 도착지 재고에 따라 이익이 보장되지는 않는다.
                offerPrice = Mathf.Max(1, Mathf.RoundToInt(offer.basePrice * 1.5f * 0.6f));
                offerLeft = Random.Range(2, 5);
            }
        }

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.I;
            if (type == Type.FallenTraveler)
            {
                if (done) { DialogueUI.Show("행인", "덕분에 살았소. 이 은혜는 꼭 소문내리다."); return; }
                DialogueUI.Show("쓰러진 행인", "물... 물 좀 주시오. 길을 오래 걸었더니 다리가 풀렸소.",
                    new DialogueUI.Choice("돕는다 (1시간)", () =>
                    {
                        if (!HelpTraveler()) return;
                        Sfx.Play("success");
                        DialogueUI.Show("행인", "고맙소, 고마워! 개경 상인들에게 당신 이야기를 해 두겠소.");
                    }),
                    new DialogueUI.Choice("지나간다", null));
                return;
            }

            if (offerLeft <= 0) { DialogueUI.Show("떠돌이 상인", "다 팔렸소. 또 봅시다!"); return; }
            DialogueUI.Show("떠돌이 상인",
                $"벽란도에서 떼 온 {offer.displayName}이오. 한 개 {offerPrice}냥, {offerLeft}개 남았소. 장부에 적은 값과 비교해 보시오.",
                new DialogueUI.Choice("하나 산다", () => Buy(1)),
                new DialogueUI.Choice("모두 산다", () => Buy(offerLeft)),
                new DialogueUI.Choice("됐소", null));
        }

        /// <summary>One contribution per event instance. No credit reward for goodwill.</summary>
        public bool HelpTraveler()
        {
            if (done || type != Type.FallenTraveler) return false;
            done = true;
            GameManager.I.Clock.Advance(1f);
            ReputationAwards.Emit(reputationScope, reputationTarget, reputationSize, 1, "road:fallen_traveler");
            if (body != null) body.localRotation = Quaternion.identity;
            return true;
        }

        void Buy(int n)
        {
            var p = GameManager.I.Player;
            int bought = 0;
            for (int i = 0; i < n; i++)
            {
                if (offerLeft <= 0 || p.money < offerPrice || !p.CanCarry(offer)) break;
                p.money -= offerPrice;
                p.cargo.Add(offer, 1, offerPrice);
                offerLeft--;
                bought++;
            }
            if (bought > 0)
            {
                Sfx.Play("coin");
                DialogueUI.Show("떠돌이 상인", $"{offer.displayName} {bought}개, 좋은 거래였소!");
            }
            else DialogueUI.Show("떠돌이 상인", p.money < offerPrice ? "돈이 모자라구려." : "짐이 꽉 찼구려.");
        }
    }
}
