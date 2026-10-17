using UnityEngine;

namespace PocketCaravan
{
    [CreateAssetMenu(menuName="Pocket Caravan/Map Modules/Weather Skin")]
    public class WeatherSkin : ScriptableObject
    {
        public string id, displayName;
        public Color sceneTint=Color.white;
        public Color haze=new Color(.8f,.85f,.9f,0);
        public Color particleColor=Color.white;
        [Range(0,160)] public int particleCount;
        public Vector2 drift=new Vector2(-2,-10);
        public Vector2 size=new Vector2(.025f,.3f);
        [Range(0,2)] public float flutter;
        public int seed=73;
        [TextArea] public string note="시각 효과만 적용. 도로/충돌/속도/피해/공유 날씨 상태와 별도.";
    }
}
