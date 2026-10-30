using System.Text;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 리알토 상인회관 전시실 (2026-10-26, 제안). 베네치아·본토 특산품 여섯 가지를 하나씩 기증하면 진열장이 채워진다.
    /// 다 채우면 "전시실 후원자": 상금 200냥, 신용 +1. 진행: VeniceSave "museum:물건 id".
    /// </summary>
    public class VeniceMuseum : Interactable
    {
        static readonly (string id, string name)[] Shelf =
        {
            ("venice_salt", "석호 소금"), ("venice_fruit", "과일 상자"), ("venice_fish", "절인 생선"),
            ("venice_wine", "포도주"), ("venice_glass_art", "유리 장식"), ("venice_lace", "레이스 손수건"),
        };

        public override string DisplayName => VeniceLabels.Place(this, "상인회관 전시실");   // 가까이 가야 이름표가 뜬다
        public override Kind Category => Kind.Market;

        static int Filled { get { int n = 0; foreach (var s in Shelf) if (VeniceSave.Has("museum:" + s.id)) n++; return n; } }

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.I;
            var text = new StringBuilder("상인회 전시실 진열장:\n");
            (string id, string name)? give = null;
            for (int i = 0; i < Shelf.Length; i++)
            {
                bool has = VeniceSave.Has("museum:" + Shelf[i].id);
                text.Append(has ? "● " : "○ ").Append(Shelf[i].name).Append(i % 3 == 2 ? "\n" : "  ");
                if (!has && give == null && gm.Player.cargo.Count(VeniceUtil.Item(Shelf[i].id)) > 0) give = Shelf[i];
            }
            if (Filled >= Shelf.Length) { DialogueUI.Show("전시실 관리인", text + "모두 채웠소. 자네 이름이 후원자 판에 올랐지."); return; }
            if (give == null) { DialogueUI.Show("전시실 관리인", text + "빈 칸에 놓을 물건을 가져오면 받겠소."); return; }
            var g = give.Value;
            DialogueUI.Show("전시실 관리인", text + $"{g.name}을(를) 기증하겠소?",
                new DialogueUI.Choice($"{g.name} 1개 기증", () =>
                {
                    gm.Player.cargo.Remove(VeniceUtil.Item(g.id), 1);
                    VeniceSave.Set("museum:" + g.id, 1f);
                    Sfx.Play("success");
                    if (Filled >= Shelf.Length)
                    {
                        gm.Player.money += 200;
                        gm.Player.credit += 1;
                        Sfx.Play("levelup");
                        Notify.Show("전시실이 다 찼다! '전시실 후원자' 상금 200냥, 신용 +1", 6f);
                    }
                    else Notify.Show($"{g.name}을(를) 진열했다 ({Filled} / {Shelf.Length})", 3f);
                }),
                new DialogueUI.Choice("다음에", () => { }));
        }
    }
}
