using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 플레이어가 바라보고 E를 누르면 반응하는 것 (장터, 의뢰 게시판, NPC 등).
    /// 이 클래스를 상속해 Interact만 구현하면 된다.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        static readonly List<Interactable> all = new List<Interactable>();

        [Tooltip("이 칸을 막는지")] public bool blocksMovement = true;

        public Vector3Int Cell => MapRoot.Current != null ? MapRoot.Current.WorldToCell(transform.position) : Vector3Int.zero;

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
    }
}
