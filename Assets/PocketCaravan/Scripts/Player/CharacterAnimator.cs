using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 16×16 캐릭터 4방향 걷기 애니메이션. frames는 [방향×4 + 프레임], 방향 순서는 아래·위·왼쪽·오른쪽.
    /// 멈춰 있으면 첫 프레임. 플레이어는 PlayerController가, NPC는 정지 상태로 쓴다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterAnimator : MonoBehaviour
    {
        public Sprite[] frames = new Sprite[16];
        public float framesPerSecond = 8f;
        public Vector2Int facing = Vector2Int.down;
        public bool walking;

        SpriteRenderer sr;
        float t;

        void Awake() => sr = GetComponent<SpriteRenderer>();

        static int DirIndex(Vector2Int f) =>
            f.y < 0 ? 0 : f.y > 0 ? 1 : f.x < 0 ? 2 : 3;

        void LateUpdate()
        {
            if (frames == null || frames.Length < 16) return;
            if (walking) t += Time.deltaTime * framesPerSecond; else t = 0f;
            int frame = walking ? Mathf.FloorToInt(t) % 4 : 0;
            var s = frames[DirIndex(facing) * 4 + frame];
            if (s != null) sr.sprite = s;
        }
    }
}
