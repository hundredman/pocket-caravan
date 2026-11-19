using UnityEngine;

namespace PocketCaravan
{
    /// <summary>11/19 손님 부르기에 지나가는 지역별 사람 그림 (왼쪽으로 걷는 _l0~_l3, 사람마다 4장씩 이어서).
    /// Resources/Stall/StallPeople.asset. 그림은 Art/Characters의 원본을 가리킨다 (복사하지 않음)</summary>
    [CreateAssetMenu(menuName = "Pocket Caravan/Stall People")]
    public sealed class StallPeople : ScriptableObject
    {
        public Sprite[] goryeo, venice, arabia, north;

        public Sprite[] For(string style)
        {
            switch (style)
            {
                case "venice": return venice;
                case "arabia": return arabia;
                case "north": return north;
                default: return goryeo;
            }
        }
    }
}
