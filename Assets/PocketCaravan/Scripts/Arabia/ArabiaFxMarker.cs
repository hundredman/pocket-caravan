using UnityEngine;

namespace PocketCaravan
{
    /// <summary>아라비아 씬 연출 자리 표시 (밤 불빛 · 분수 · 관람차). ArabiaAmbience가 읽는다.</summary>
    public class ArabiaFxMarker : MonoBehaviour
    {
        public string kind;          // light · fountain · ferris
        public float radius = 3f;
        public string colorName = "warm";
        public bool flicker;
    }
}
