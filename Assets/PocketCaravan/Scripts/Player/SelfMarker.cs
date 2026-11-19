using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/15 은샘 (UT 4번 "자기 캐릭터 표시 강화"): 내 캐릭터 발밑에 청록 고리를 깐다.
    /// 다른 플레이어·NPC도 같은 동물 그림이라 내가 어디 있는지 놓쳤다. 말 걸 대상 고리(노랑, WorldFx)와 색으로 구분한다.
    /// 땅 위 그림(그림자 바로 아래)이라 좌판·건물에 가려지면 같이 가려진다. 기념 사진을 찍는 동안은 감춘다.
    /// </summary>
    public sealed class SelfMarker : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<SelfMarker>() != null) return;
            var go = new GameObject("SelfMarker");
            DontDestroyOnLoad(go);
            go.AddComponent<SelfMarker>();
        }

        static readonly Color32 Teal = new Color32(0x2E, 0xC4, 0xB6, 255);
        SpriteRenderer ring;
        PlayerController owner;

        void LateUpdate()
        {
            var pc = PlayerController.Instance;
            if (pc != owner)
            {
                owner = pc;
                if (ring != null) Destroy(ring.gameObject);
                ring = null;
                if (pc != null)
                {
                    var go = new GameObject("내 고리");
                    go.transform.SetParent(pc.transform, false);
                    ring = go.AddComponent<SpriteRenderer>();
                    ring.sprite = MakeSprite();
                }
            }
            if (ring == null || pc == null) return;
            // 그림자(9) 바로 아래. 몸 그림 순서가 앞뒤 정렬로 바뀌면 같이 따라간다
            var body = pc.anim != null ? pc.anim.GetComponent<SpriteRenderer>() : null;
            int baseOrder = body != null ? PackAnimal.BodyOrder(body) : 11;
            ring.sortingOrder = baseOrder - 3;
            if (body != null) ring.sharedMaterial = body.sharedMaterial;   // 밤 조명을 같이 받게
            var feet = pc.Motor != null ? (Vector3)pc.Motor.Feet : pc.transform.position;
            ring.transform.position = new Vector3(Mathf.Round(feet.x * 16f) / 16f, Mathf.Round((feet.y - .02f) * 16f) / 16f, pc.transform.position.z);
            bool show = !UIState.PhotoMode && pc.gameObject.activeInHierarchy;
            ring.enabled = show;
            float a = .75f + .2f * Mathf.Sin(Time.unscaledTime * 2.2f);
            ring.color = new Color(Teal.r / 255f, Teal.g / 255f, Teal.b / 255f, a);
        }

        /// <summary>20×8 타원 고리 (두께 1칸 + 안쪽 옅게), 픽셀 그대로</summary>
        static Sprite sprite;
        static Sprite MakeSprite()
        {
            if (sprite != null) return sprite;
            const int W = 20, H = 8;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[W * H];
            float cx = (W - 1) / 2f, cy = (H - 1) / 2f;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float ox = (x - cx) / (W / 2f), oy = (y - cy) / (H / 2f);
                    float ix = (x - cx) / (W / 2f - 1.2f), iy = (y - cy) / (H / 2f - 1.2f);
                    bool outside = ox * ox + oy * oy > 1f, inside = ix * ix + iy * iy < 1f;
                    px[y * W + x] = outside ? new Color32(255, 255, 255, 0) : inside ? new Color32(255, 255, 255, 60) : new Color32(255, 255, 255, 255);
                }
            tex.SetPixels32(px);
            tex.Apply();
            sprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(.5f, .5f), 16f);
            return sprite;
        }
    }
}
