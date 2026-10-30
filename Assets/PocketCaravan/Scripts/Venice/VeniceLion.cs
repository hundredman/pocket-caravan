using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 숨은 '날개 달린 사자' 문장 7개와 전설 (2026-10-26, 제안). 베네치아의 상징 사자를 바탕으로 지어낸 각색 이야기다.
    /// 석호 다섯 마을과 본토 두 곳(포도밭·습지)의 길에서 먼 구석에 이끼 낀 돌판이 있다(자리: VeniceLife.json "lion"). 밤에는 은은히 빛난다.
    /// 읽을 때마다 전설 한 조각(VeniceSave "lion:n"). 일곱 개를 다 읽으면 상금 300냥·신용 +2와 "사자의 축복"(큰 장 덤이 2할 → 2할 5푼).
    /// </summary>
    public class VeniceLion : Interactable
    {
        public static readonly string[] Maps = { "venice_rialto", "venice_santerasmo", "venice_murano", "venice_burano", "venice_chioggia", "venice_c_vineyard", "venice_c_marsh" };
        static readonly string[] Legend =
        {
            "옛날 석호의 배 한 척이 폭풍을 만났다.\n뱃사람들은 구름 사이로 날개 달린 사자를 보았다고 했다.",
            "사자는 펼친 책을 앞발로 누르고 있었다.\n책에는 '평화'라는 한 낱말이 적혀 있었다.",
            "상인들은 배와 창고에 사자를 새겼다.\n사자 아래에서는 속이지 않는다는 약속이었다.",
            "유리장이들은 가마 불빛 속에서\n사자의 날개가 반짝이는 것을 보았다고 한다.",
            "어부들은 그물을 던지기 전에\n문장의 사자를 한 번씩 쓰다듬었다.",
            "본토로 건너간 상인들도 길목마다 작은 사자를 숨겼다.\n돌아오는 길을 잊지 않으려고.",
            "일곱 사자를 다 찾은 상인은\n어느 항구에서든 믿을 사람으로 통했다고 한다.",
        };

        public int index;
        SpriteRenderer glow;

        public override string DisplayName => VeniceLabels.Place(this, "이끼 낀 사자 문장");   // 가까이 가야 이름표가 뜬다
        public override Kind Category => Kind.Quest;

        public static int Found
        {
            get { int n = 0; for (int i = 0; i < Maps.Length; i++) if (VeniceSave.Has("lion:" + i)) n++; return n; }
        }
        public static bool Blessed => VeniceSave.Get("lion_blessing") > 0f;

        void Start()
        {
            var r = gameObject.AddComponent<SpriteRenderer>();
            r.sprite = VeniceUtil.Sprite("lion");
            r.sortingOrder = 9;
            var g = new GameObject("사자 빛");
            g.transform.SetParent(transform, false);
            glow = g.AddComponent<SpriteRenderer>();
            glow.sprite = VeniceUtil.Sprite("glow");
            glow.sortingOrder = 19;
            if (VeniceUtil.Unlit != null) glow.sharedMaterial = VeniceUtil.Unlit;
        }

        void Update()
        {
            if (glow == null) return;
            float night = Mathf.Clamp01((1f - (GameManager.I != null ? GameManager.I.Clock.Daylight : 1f)) * 1.6f);
            bool read = VeniceSave.Has("lion:" + index);
            glow.enabled = night > .02f;
            glow.color = new Color(1f, read ? .95f : .85f, read ? .7f : .5f, night * (.45f + Mathf.Sin(Time.time * 2f + index) * .15f));
        }

        public override void Interact(PlayerController player)
        {
            bool first = !VeniceSave.Has("lion:" + index);
            VeniceSave.Set("lion:" + index, 1f);
            int found = Found;
            string text = Legend[Mathf.Clamp(index, 0, Legend.Length - 1)] + $"\n(사자 문장 {found} / {Maps.Length})";
            DialogueUI.Show("사자 문장", text);
            if (!first) return;
            Sfx.Play("success");
            if (found >= Maps.Length && !Blessed)
            {
                VeniceSave.Set("lion_blessing", 1f);
                var gm = GameManager.I;
                gm.Player.money += 300;
                gm.Player.credit += 2;
                Sfx.Play("levelup");
                Notify.Show("일곱 사자를 모두 찾았다! 상금 300냥, 신용 +2, '사자의 축복'(큰 장 덤 2할 5푼)", 6f);
            }
            else Notify.Show($"사자 문장을 찾았다 ({found} / {Maps.Length})", 3f);
        }
    }
}
