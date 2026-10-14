using UnityEngine;

namespace PocketCaravan
{
    /// <summary>가로로 이어 붙인 그림 한 장(16px 칸)을 차례로 보여 준다. 물결, 흔들리는 꽃 등.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAnim : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 4f;
        float t;
        SpriteRenderer sr;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            t = Random.value * 10f;
        }

        void Update()
        {
            if (frames == null || frames.Length == 0) return;
            t += Time.deltaTime;
            sr.sprite = frames[(int)(t * fps) % frames.Length];
        }
    }
}
