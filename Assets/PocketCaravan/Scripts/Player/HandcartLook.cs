using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 손수레 그림 (11/09 은샘 승인, 모든 지역 공용). 내 손수레(CarryVisual)와 온라인 다른 사람 손수레(VehicleLook)가 같이 쓴다.
    /// 그림 한 장을 돌리던 방식 대신 방향마다 따로 그린 그림: 옆(side, 오른쪽은 뒤집기) · 앞(front, 아래로 갈 때) · 뒤(back, 위로 갈 때),
    /// 짐은 그림 안에 상자 0~3단계. 옆모습은 손잡이를 따로 사람 앞에 한 겹 더 그려 옆구리를 가린다.
    /// 그림: Resources/Transport/Handcart (Tools/ArtGen/handcart_v3.py, ArtReview/Approved/HandcartV2). 표시만, 충돌·속도·저장과 무관.
    /// </summary>
    public static class HandcartLook
    {
        const string HandleName = "손잡이 앞";
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static Sprite Art(string name)
        {
            if (cache.TryGetValue(name, out var s) && s != null) return s;
            return cache[name] = Resources.Load<Sprite>("Transport/Handcart/" + name);
        }

        /// <summary>짐 비율 → 그림 단계 0~3 (0 넘으면 1, 1/3 넘으면 2, 2/3 넘으면 3)</summary>
        public static int Stage(float loadRatio) => (loadRatio > 0f ? 1 : 0) + (loadRatio > 1f / 3f ? 1 : 0) + (loadRatio > 2f / 3f ? 1 : 0);

        /// <summary>
        /// cart를 owner(사람) 옆 자리에 놓고 방향·짐 단계에 맞는 그림으로 바꾼다. body = 사람 몸 그림(정렬·발 높이 기준).
        /// 자리: 아래로 갈 때 수레는 사람 위 1칸, 위로 갈 때 사람 아래 0.95칸(앞에 그림), 옆으로 갈 때 사람 뒤 1.25칸에서 바퀴 바닥이 발보다 1px 아래.
        /// </summary>
        public static void Place(SpriteRenderer cart, Transform owner, SpriteRenderer body, Vector2 facing, int stage)
        {
            if (cart == null || owner == null) return;
            if (facing.sqrMagnitude < 0.01f) facing = Vector2.down;
            stage = Mathf.Clamp(stage, 0, 3);
            bool side = Mathf.Abs(facing.x) > Mathf.Abs(facing.y);
            int dir = facing.x > 0 ? 1 : -1;
            int baseOrder = body != null ? body.sortingOrder : 10;
            var tr = cart.transform;
            tr.localRotation = Quaternion.identity;
            tr.localScale = Vector3.one;
            cart.flipY = false;
            Vector3 o = owner.position;
            Vector3 at;
            if (side)
            {
                float feet = o.y - (body != null && body.sprite != null ? body.sprite.pivot.y / body.sprite.pixelsPerUnit : 0.375f);
                cart.sprite = Art("side_" + stage);
                cart.flipX = dir > 0;
                at = new Vector3(o.x - dir * 1.25f, feet + 0.875f, 0f);
                cart.sortingOrder = baseOrder - 2;
            }
            else if (facing.y < 0)
            {
                cart.sprite = Art("front_" + stage);
                cart.flipX = false;
                at = new Vector3(o.x, o.y + 1f, 0f);
                cart.sortingOrder = baseOrder - 2;
            }
            else
            {
                cart.sprite = Art("back_" + stage);
                cart.flipX = false;
                at = new Vector3(o.x, o.y - 0.95f + 1.5f / 16f, 0f);   // 그림 가운데가 손잡이 기준점보다 1.5px 위
                cart.sortingOrder = baseOrder + 1;
            }
            tr.position = new Vector3(at.x, at.y, tr.position.z);
            if (body != null) cart.sortingLayerID = body.sortingLayerID;

            var handle = Handle(cart, side);
            if (handle != null)
            {
                handle.sprite = Art("side_handle");
                handle.flipX = dir > 0;
                handle.transform.localPosition = new Vector3(dir * 2.5f / 16f, 0f, 0f);   // 손잡이 그림은 사람 쪽으로 5px 넓다
                handle.sortingLayerID = cart.sortingLayerID;
                handle.sharedMaterial = cart.sharedMaterial;
                handle.sortingOrder = baseOrder + 1;   // 사람 몸 앞 → 옆구리를 가린다
            }
        }

        /// <summary>손수레가 아닐 때 손잡이 겹을 끈다 (같은 그림 칸을 낙타·썰매로 쓸 때)</summary>
        public static void HideHandle(SpriteRenderer cart) => Handle(cart, false);

        static SpriteRenderer Handle(SpriteRenderer cart, bool show)
        {
            if (cart == null) return null;
            var t = cart.transform.Find(HandleName);
            if (t == null)
            {
                if (!show) return null;
                t = new GameObject(HandleName).transform;
                t.SetParent(cart.transform, false);
                t.gameObject.AddComponent<SpriteRenderer>();
            }
            t.gameObject.SetActive(show);
            return show ? t.GetComponent<SpriteRenderer>() : null;
        }
    }
}
