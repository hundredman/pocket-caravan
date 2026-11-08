using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 리알토 광장 비둘기 모이 주기 (2026-10-26, 제안). 하루 한 번 1냥으로 모이를 뿌리면 광장 비둘기가 몰려온다.
    /// 사흘 연속으로 주면 비둘기들이 반짝이는 단추를 물어 온다(한 번, 30냥). 진행: VeniceSave "feed_*".
    /// 2026-11-08 (은샘, 윤채 님께 전달): 하루·연속은 내 플레이 날짜(Clock.PlayDay)로 센다. 온라인에서는 세상 날짜가
    /// 접속하지 않아도 흘러서, 6분만 자리를 비워도 연속이 끊겼다. 옛 세상 날짜 기록(feed_last)은 쓰지 않는다(연속은 한 번 새로 시작).
    /// </summary>
    public class VeniceFeed : Interactable
    {
        public override string DisplayName => VeniceLabels.Place(this, "비둘기 모이 주머니");   // 가까이 가야 이름표가 뜬다
        public override Kind Category => Kind.Market;

        void Start()
        {
            var r = gameObject.AddComponent<SpriteRenderer>();
            r.sprite = VeniceUtil.Sprite("seed_sack");
            r.sortingOrder = 9;
        }

        /// <summary>오늘(내 플레이 날짜) 이미 모이를 줬는지. 놀거리 알림도 이것을 본다.</summary>
        public static bool FedToday => GameManager.I != null && (int)VeniceSave.Get("feed_playlast", -10) == GameManager.I.Clock.PlayDay;

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.I;
            if (FedToday)
            {
                DialogueUI.Show("광장 비둘기", "구구… (오늘은 배가 부른 모양이다)");
                return;
            }
            if (gm.Player.money < 1) { DialogueUI.Show("광장 비둘기", "모이를 살 1냥이 없다."); return; }
            gm.Player.money -= 1;
            int today = gm.Clock.PlayDay;
            int last = (int)VeniceSave.Get("feed_playlast", -10);
            int streak = last == today - 1 ? (int)VeniceSave.Get("feed_streak") + 1 : 1;
            VeniceSave.Set("feed_playlast", today);
            VeniceSave.Set("feed_streak", streak);
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
