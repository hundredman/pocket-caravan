using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 카니발 가면 장수 (2026-10-27, 제안). 카니발 밤(VeniceAtmosphere.CarnivalNight)에만 리알토 광장에 선다.
    /// 처음 말을 걸면 가면 춤(R 동작 "mask")을 공짜로 가르쳐 준다. 그 뒤엔 오늘 밤 덤 안내만.
    /// </summary>
    public class VeniceMaskSeller : Interactable
    {
        public override string DisplayName => "가면 장수";
        public override Kind Category => Kind.Market;

        SpriteRenderer body;

        void Start()
        {
            body = gameObject.AddComponent<SpriteRenderer>();
            body.sprite = VeniceUtil.Sprite("festa_mask_seller");
            body.sortingOrder = 10;
            transform.position += new Vector3(0f, -.5f, 0f);
            var mask = new GameObject("가면").AddComponent<SpriteRenderer>();
            mask.transform.SetParent(transform, false);
            mask.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            mask.sprite = VeniceUtil.Sprite("mask");
            mask.sortingOrder = 11;
        }

        void Update()
        {
            if (body != null) body.transform.localScale = new Vector3(1f, 1f + Mathf.Abs(Mathf.Sin(Time.time * 5f)) * .05f, 1f);
        }

        public override void Interact(PlayerController player)
        {
            string learned = VeniceReactions.Teach("mask");
            if (learned != null)
            {
                Sfx.Play("levelup", .7f);
                DialogueUI.Show("가면 장수", "카니발 밤엔 누구나 가면을 쓰지! 자, 이렇게 빙글빙글 뛰어 봐.\n(동작 '가면 춤'을 배웠다. R)");
                return;
            }
            DialogueUI.Show("가면 장수", "오늘 밤 리알토에선 유리·레이스를 3할 더 쳐 준다네. 불꽃 구경도 하고 가!");
        }
    }
}
