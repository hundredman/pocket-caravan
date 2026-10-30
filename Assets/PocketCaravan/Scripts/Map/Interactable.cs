using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 플레이어가 가까이 가서 Space를 누르면 반응하는 것 (장터, 퀘스트 NPC, 마을 사람 등).
    /// 이 클래스를 상속해 Interact만 구현하면 된다. 머리 위 이름표는 NamePlates가 그린다.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        public enum Kind { Person, Quest, Market, Board, Danger }

        static readonly List<Interactable> all = new List<Interactable>();
        public static IReadOnlyList<Interactable> All => all;

        [Tooltip("이 칸을 막는지")] public bool blocksMovement = true;

        public Vector3Int Cell => MapRoot.Current != null ? MapRoot.Current.WorldToCell(transform.position) : Vector3Int.zero;
        public abstract string DisplayName { get; }
        public virtual Kind Category => Kind.Person;
        /// <summary>가까이 갔을 때 안내 "○○ (Space)"의 ○○. 비우면 종류에 따라 말 걸기·사고팔기 등</summary>
        public virtual string ActionVerb => null;

        protected virtual void OnEnable() => all.Add(this);
        protected virtual void OnDisable() => all.Remove(this);

        public abstract void Interact(PlayerController player);

        public static Interactable At(Vector3Int cell)
        {
            foreach (var it in all) if (it.Cell == cell) return it;
            return null;
        }

        public static Interactable BlockerAt(Vector3Int cell)
        {
            foreach (var it in all) if (it.blocksMovement && it.Cell == cell) return it;
            return null;
        }

        /// <summary>
        /// 말을 걸 수 있는 가장 가까운 대상. 거리 range 칸 안에서, 바라보는 쪽에 있는 대상을 우선한다.
        /// </summary>
        public static Interactable Nearest(Vector2 from, Vector2 facing, float range)
        {
            Interactable best = null;
            float bestScore = float.MaxValue;
            foreach (var it in all)
            {
                Vector2 d = (Vector2)it.transform.position - from;
                float dist = d.magnitude;
                if (dist > range) continue;
                float dot = dist > 0.01f ? Vector2.Dot(d / dist, facing) : 1f;
                float score = dist - dot * 0.6f;  // 앞에 있으면 더 가깝게 친다
                if (score < bestScore) { bestScore = score; best = it; }
            }
            return best;
        }
    }
}
