using UnityEngine;

namespace PocketCaravan
{
    /// <summary>마을 등급이 minLevel 이상이면 보이는 소품. 장터가 커지는 것을 눈으로 보여 준다.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class TownGrowthProp : MonoBehaviour
    {
        public string town;
        public int minLevel = 2;
        SpriteRenderer sr;

        void Awake() => sr = GetComponent<SpriteRenderer>();

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            var t = gm.Db.towns.Find(x => x != null && x.id == town);
            sr.enabled = t != null && gm.GetMarket(t).Level >= minLevel;
        }
    }
}
