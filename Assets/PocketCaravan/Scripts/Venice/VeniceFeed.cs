using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 리알토 광장 비둘기 모이 주기 (2026-10-26, 제안). 하루 한 번 1냥으로 모이를 뿌리면 광장 비둘기가 몰려온다.
    /// 사흘 연속으로 주면 비둘기들이 반짝이는 단추를 물어 온다(한 번, 30냥). 진행: VeniceSave "feed_*".
    /// </summary>
    public class VeniceFeed : Interactable
    {
        public override string DisplayName => "비둘기 모이 주머니";
        public override Kind Category => Kind.Market;

        void Start()
        {
            var r = gameObject.AddComponent<SpriteRenderer>();
            r.sprite = VeniceUtil.Sprite("seed_sack");
            r.sortingOrder = 9;
        }

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.I;
            if (VeniceSave.DoneToday("feed"))
            {
                DialogueUI.Show("광장 비둘기", "구구… (오늘은 배가 부른 모양이다)");
                return;
            }
            if (gm.Player.money < 1) { DialogueUI.Show("광장 비둘기", "모이를 살 1냥이 없다."); return; }
            gm.Player.money -= 1;
            int last = (int)VeniceSave.Get("feed_last", -10);
            int streak = last == gm.Clock.day - 1 ? (int)VeniceSave.Get("feed_streak") + 1 : 1;
            VeniceSave.Set("feed_last", gm.Clock.day);
            VeniceSave.Set("feed_streak", streak);
            VeniceSave.MarkToday("feed");
            VeniceLife.Attract(player.transform.position, 6f);
            Sfx.Play("coin");
            if (streak >= 3 && !VeniceSave.Has("feed_gift"))
            {
                VeniceSave.Set("feed_gift", 1f);
                gm.Player.money += 30;
                Notify.Show("사흘째 모이를 주자 비둘기들이 반짝이는 단추를 물어 왔다. 30냥!", 5f);
            }
            else Notify.Show($"비둘기들이 몰려왔다 (연속 {streak}일째)", 3f);
        }
    }
}
