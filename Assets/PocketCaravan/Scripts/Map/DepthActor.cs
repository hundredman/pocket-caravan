using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/15 은샘: 움직이는 NPC·다른 플레이어를 마을 꾸밈(좌판 등)과 앞뒤로 맞춘다.
    /// 마을 꾸밈 그림은 내 캐릭터 발을 기준으로 4(뒤)/12(앞) 하나만 고르므로(VillageDressing),
    /// 그 그림과 겹친 NPC는 내 위치에 따라 엉뚱하게 앞이나 뒤에 그려졌다.
    /// 규칙: 발이 그림 아래 끝보다 위(그림 안)면 그림 뒤, 아니면(땅) 그림 앞. 내 캐릭터와 같은 규칙이다.
    /// </summary>
    [DefaultExecutionOrder(100)]   // VillageDressing.LateUpdate(그림 순서 정하기) 다음
    public sealed class DepthActor : MonoBehaviour
    {
        static readonly List<SpriteRenderer> props = new List<SpriteRenderer>();
        static int propsFrame = -1;

        /// <summary>VillageDressing이 이번 프레임에 순서를 정한 그림을 알려 준다</summary>
        public static void AddProps(SpriteRenderer[] rs)
        {
            if (propsFrame != Time.frameCount) { props.Clear(); propsFrame = Time.frameCount; }
            if (rs == null) return;
            foreach (var r in rs) if (r != null && r.enabled && r.gameObject.activeInHierarchy) props.Add(r);
        }

        SpriteRenderer[] rs = new SpriteRenderer[0];
        int[] setTo = new int[0];
        int[] baseOrder = new int[0];
        float recache;

        void Cache()
        {
            var now = GetComponentsInChildren<SpriteRenderer>(true);
            var b = new int[now.Length]; var s = new int[now.Length];
            for (int i = 0; i < now.Length; i++)
            {
                int old = System.Array.IndexOf(rs, now[i]);
                // 내가 바꾼 값 그대로면 원래 값을, 다른 코드가 새로 정했으면 그 값을 기준으로
                b[i] = old >= 0 && now[i].sortingOrder == setTo[old] ? baseOrder[old] : now[i].sortingOrder;
                s[i] = now[i].sortingOrder;
            }
            rs = now; baseOrder = b; setTo = s;
        }

        void OnDisable()
        {
            for (int i = 0; i < rs.Length; i++) if (rs[i] != null && rs[i].sortingOrder == setTo[i]) rs[i].sortingOrder = baseOrder[i];
        }

        void LateUpdate()
        {
            if (Time.unscaledTime >= recache || rs.Length == 0) { Cache(); recache = Time.unscaledTime + 1f; }
            // 다른 코드가 순서를 새로 정했으면 그것을 기준으로 삼는다
            for (int i = 0; i < rs.Length; i++) if (rs[i] != null && rs[i].sortingOrder != setTo[i]) baseOrder[i] = rs[i].sortingOrder;

            // 몸 = 그림자가 아닌 가장 큰 그림. 발 = 몸 그림 아래 끝
            SpriteRenderer body = null; float area = 0f;
            int lo = int.MaxValue, hi = int.MinValue;
            for (int i = 0; i < rs.Length; i++)
            {
                var r = rs[i]; if (r == null || !r.enabled || r.sprite == null) continue;
                lo = Mathf.Min(lo, baseOrder[i]); hi = Mathf.Max(hi, baseOrder[i]);
                if (r.name == "Shadow") continue;
                float a = r.bounds.size.x * r.bounds.size.y;
                if (a > area) { area = a; body = r; }
            }

            int delta = 0;
            if (body != null && propsFrame == Time.frameCount)
            {
                var bb = body.bounds; float feet = bb.min.y;
                foreach (var p in props)
                {
                    var pb = p.bounds;
                    if (bb.max.x <= pb.min.x || bb.min.x >= pb.max.x || bb.max.y <= pb.min.y || bb.min.y >= pb.max.y) continue;   // 안 겹침
                    bool behind = feet > pb.min.y;
                    if (behind && hi >= p.sortingOrder) delta = p.sortingOrder - 1 - hi;
                    else if (!behind && lo <= p.sortingOrder) delta = p.sortingOrder + 1 - lo;
                    else continue;
                    break;
                }
            }
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] == null) continue;
                setTo[i] = baseOrder[i] + delta;
                rs[i].sortingOrder = setTo[i];
            }
        }
    }
}
