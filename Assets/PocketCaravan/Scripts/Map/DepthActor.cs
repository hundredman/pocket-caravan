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

        /// <summary>11/19 짐승·마차 그림도 같은 규칙으로 (발이 그림 아래 끝보다 아래면 사람이 앞, 위면 뒤)</summary>
        public static void AddProp(SpriteRenderer r)
        {
            if (propsFrame != Time.frameCount) { props.Clear(); propsFrame = Time.frameCount; }
            if (r != null && r.enabled && r.gameObject.activeInHierarchy) props.Add(r);
        }

        public const string VehicleChild = "운반 수단";
        const int Overhead = 20;   // 11/19 이 순서 이상(머리 위 퀘스트 표시 등)은 몸과 같이 옮기지 않는다
        SpriteRenderer[] rs = new SpriteRenderer[0];
        int[] setTo = new int[0];
        int[] baseOrder = new int[0];
        float recache;

        void Cache()
        {
            // 11/19 다른 사람이 끄는 짐승·수레("운반 수단", VehicleLook)는 빼 둔다. 그 그림은 이미 옮긴 몸 순서를 기준으로 놓이니 여기서 또 옮기면 두 번 옮겨진다
            var now = System.Array.FindAll(GetComponentsInChildren<SpriteRenderer>(true), r => r.gameObject.name != VehicleChild);
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
                var r = rs[i]; if (r == null || !r.enabled || r.sprite == null || baseOrder[i] >= Overhead) continue;
                lo = Mathf.Min(lo, baseOrder[i]); hi = Mathf.Max(hi, baseOrder[i]);
                if (r.name == "Shadow") continue;
                float a = r.bounds.size.x * r.bounds.size.y;
                if (a > area) { area = a; body = r; }
            }

            int delta = 0;
            if (body != null && propsFrame == Time.frameCount)
            {
                // 11/19 겹친 그림을 모두 본다: 앞에 선 그림들보다는 위, 뒤에 선 그림들보다는 아래 (앞이 우선)
                var bb = body.bounds; float feet = bb.min.y;
                int needAbove = int.MinValue, needBelow = int.MaxValue;
                foreach (var p in props)
                {
                    if (p == null || p.transform.IsChildOf(transform)) continue;   // 11/19 내가 끄는 짐승·마차는 나와 비교하지 않는다
                    var pb = p.bounds;
                    if (bb.max.x <= pb.min.x || bb.min.x >= pb.max.x || bb.max.y <= pb.min.y || bb.min.y >= pb.max.y) continue;   // 안 겹침
                    if (feet > pb.min.y) needBelow = Mathf.Min(needBelow, p.sortingOrder - 1);
                    else if (p.sortingOrder < Overhead || !AboveDepth.Behind(bb, feet)) needAbove = Mathf.Max(needAbove, p.sortingOrder + 1);
                    // 기둥 앞으로 올린 짐승(20 이상) 앞에 섰더라도 내가 다른 물건 뒤면 올리지 않는다 (그 물건에 가려지는 게 맞다)
                }
                if (needAbove != int.MinValue && lo < needAbove) delta = needAbove - lo;
                else if (needAbove == int.MinValue && needBelow != int.MaxValue && hi > needBelow) delta = needBelow - hi;
            }
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] == null) continue;
                setTo[i] = baseOrder[i] + (baseOrder[i] >= Overhead ? 0 : delta);   // 머리 위 표시(퀘스트 표시 등)는 그대로
                rs[i].sortingOrder = setTo[i];
            }
        }
    }
}
