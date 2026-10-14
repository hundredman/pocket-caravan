using System;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>길에 떨어진 짐 기록. 맵을 떠났다 와도 그 자리에 남아 있다.</summary>
    [Serializable]
    public class DropRecord
    {
        public string mapId;
        public float x, y;
        public string itemId;      // 물건이면 아이템 id
        public float unitCost;
        public int contractId;     // 의뢰 짐이면 의뢰 id (0이면 물건)
        public string label;
    }

    /// <summary>떨어진 짐. 다가가 Space로 다시 줍는다.</summary>
    public class DroppedCargo : Interactable
    {
        public DropRecord record;
        SpriteRenderer sr;
        float t;

        public override string DisplayName => $"떨어진 {record.label}";

        void Awake() => blocksMovement = false;

        public static DroppedCargo Spawn(DropRecord r, Sprite icon)
        {
            var go = new GameObject("Dropped " + r.label);
            go.transform.position = new Vector3(r.x, r.y, 0f);
            var d = go.AddComponent<DroppedCargo>();
            d.record = r;
            d.sr = go.AddComponent<SpriteRenderer>();
            d.sr.sprite = icon;
            d.sr.sortingOrder = 9;
            return d;
        }

        void Update()
        {
            t += Time.deltaTime;
            if (sr != null) sr.transform.localScale = Vector3.one * (1f + Mathf.Floor(Mathf.Sin(t * 4f) * 1.5f + 1.5f) * 0.04f);
        }

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.I;
            var p = gm.Player;
            if (record.contractId != 0)
            {
                var c = p.contracts.Find(x => x.id == record.contractId);
                if (c != null)
                {
                    if (!p.CanCarryWeight(c.weight)) { Notify.Show("짐이 너무 많아 주울 수 없다."); return; }
                    c.dropped = false;
                }
            }
            else
            {
                var item = gm.Db.items.Find(i => i != null && i.id == record.itemId);
                if (item == null) { Destroy(gameObject); return; }
                if (!p.CanCarry(item)) { Notify.Show("짐이 너무 많아 주울 수 없다."); return; }
                p.cargo.Add(item, 1, record.unitCost);
            }
            gm.Drops.Remove(record);
            Notify.Show($"{Josa.Eul(record.label)} 다시 실었다.");
            Sfx.Play("move");
            Destroy(gameObject);
        }
    }
}
