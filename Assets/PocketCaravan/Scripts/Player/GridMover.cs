using System;
using System.Collections;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>칸 단위 이동 (포켓몬식). 한 칸 이동 시간 = baseStepTime ÷ 속도 배율.</summary>
    public class GridMover : MonoBehaviour
    {
        [Tooltip("속도 배율 1일 때 한 칸 걷는 시간(초)")] public float baseStepTime = 0.24f;

        public bool IsMoving { get; private set; }
        public Vector3Int Cell { get; private set; }
        public Vector2Int Facing { get; private set; } = Vector2Int.down;
        /// <summary>0~1. 한 칸 이동 중 진행도 (걷기 모션용)</summary>
        public float StepProgress { get; private set; }

        public event Action<Vector3Int> Stepped;
        public event Action<Vector2Int> Bumped;

        void Start()
        {
            if (MapRoot.Current != null) SnapToCell(MapRoot.Current.WorldToCell(transform.position));
        }

        public void SnapToCell(Vector3Int cell)
        {
            StopAllCoroutines();
            IsMoving = false;
            Cell = cell;
            if (MapRoot.Current != null) transform.position = MapRoot.Current.CellCenter(cell);
        }

        public void Face(Vector2Int dir)
        {
            if (dir != Vector2Int.zero) Facing = dir;
        }

        public bool TryMove(Vector2Int dir, float speedMultiplier)
        {
            Face(dir);
            if (IsMoving || MapRoot.Current == null) return false;
            var target = Cell + new Vector3Int(dir.x, dir.y, 0);
            if (!MapRoot.Current.IsWalkable(target))
            {
                Bumped?.Invoke(dir);
                return false;
            }
            StartCoroutine(Step(target, baseStepTime / Mathf.Max(0.05f, speedMultiplier)));
            return true;
        }

        IEnumerator Step(Vector3Int target, float duration)
        {
            IsMoving = true;
            Vector3 from = transform.position;
            Vector3 to = MapRoot.Current.CellCenter(target);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                StepProgress = Mathf.Clamp01(t / duration);
                transform.position = Vector3.Lerp(from, to, StepProgress);
                yield return null;
            }
            transform.position = to;
            Cell = target;
            StepProgress = 0f;
            IsMoving = false;
            Stepped?.Invoke(target);
        }
    }
}
