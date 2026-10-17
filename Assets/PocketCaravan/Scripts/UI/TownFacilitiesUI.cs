using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>장터에서 이용하는 시설. 거점 등급과 개인 신용을 함께 보여 준다.</summary>
    public class TownFacilitiesUI : MonoBehaviour
    {
        static TownFacilitiesUI instance;
        TownData town;
        TownFacility warehouse;
        bool open;
        int openedFrame;
        Vector2 scroll;
        string message = "";
        void Awake() => instance = this;
        public static void Show(TownData value)
        {
            if (instance == null || value == null || UIState.Blocking) return;
            instance.town = value; instance.warehouse = null;
            instance.open = true; instance.openedFrame = Time.frameCount;
            instance.scroll = Vector2.zero; instance.message = ""; UIState.Blocking = true;
        }
        void Close() { open = false; UIState.Release(); }
        void OnDisable() { if (open) Close(); if (instance == this) instance = null; }
        void Update()
        {
            if (open && Time.frameCount != openedFrame && InputReader.CancelPressed()) Close();
        }
        bool InRange()
        {
            var p = PlayerController.Instance;
            if (p == null || GameManager.I == null || GameManager.I.IsTravelling) return false;
            foreach (var m in FindObjectsByType<TownMarket>(FindObjectsSortMode.None))
                if (m.town == town && Vector2.Distance(m.transform.position, p.transform.position) <= p.talkRange + 0.1f) return true;
            return false;
        }
        void Use(TownFacility f)
        {
            var gm = GameManager.I;
            message = !InRange() ? "장터 가까이에서 이용하세요." : FacilityAccess.Denial(town, f, gm.Player);
            if (message != null) return;
            if (f.kind == FacilityKind.Rest) { gm.Player.stamina = 1f; message = "짐을 내려놓고 쉬었습니다. 체력이 회복됐습니다."; }
            else if (f.kind == FacilityKind.RouteInformation) message = f.description;
            else { warehouse = f; scroll.y = 10000; message = "이 마을에 맡긴 물건은 여기서 다시 찾을 수 있습니다."; }
            gm.SaveNow();
        }
        void Transfer(ItemData item, bool deposit)
        {
            if (!InRange()) { message = "장터 가까이에서 이용하세요."; return; }
            var gm = GameManager.I;
            string denial = gm.Player.warehouse.Transfer(town, warehouse, gm.Player, item, deposit, gm.ItemById);
            message = denial ?? $"{item.displayName} 1개를 " + (deposit ? "맡겼습니다." : "찾았습니다.");
            if (denial == null) gm.SaveNow();
        }
        void OnGUI()
        {
            if (!open || GameManager.I == null) return;
            UIStyle.Begin(); GUI.depth = -12;
            var gm = GameManager.I; var p = gm.Player;
            float w = Mathf.Min(530, UIStyle.VirtualWidth - 16), h = 326;
            var rect = new Rect((UIStyle.VirtualWidth-w)/2, (UIStyle.VirtualHeight-h)/2, w, h);
            GUI.Box(rect, "", UIStyle.Box);
            GUILayout.BeginArea(new Rect(rect.x+12, rect.y+8, w-24, h-94));
            GUILayout.Label($"{town.displayName} 시설 · 거점 {town.SettlementTier}등급 · 내 신용 {p.credit}", UIStyle.Rich);
            GUILayout.Label("마을 방문·기본 매매는 신용과 관계없이 가능합니다.", UIStyle.Small);
            scroll = GUILayout.BeginScrollView(scroll);
            foreach (var f in town.facilities)
            {
                if (f == null) continue;
                string denial = FacilityAccess.Denial(town, f, p);
                GUILayout.Label($"{f.displayName} · 거점 {f.minTier}등급 / 신용 {f.minCredit}", UIStyle.Rich);
                GUILayout.BeginHorizontal();
                GUILayout.Label(denial ?? (f.kind == FacilityKind.Warehouse ? $"보관 한도 {f.storageCapacity:0} · 일반 교역품" : f.kind == FacilityKind.Rest ? "체력 회복" : "주변 길과 거래처 안내"), UIStyle.Small);
                GUI.enabled = denial == null;
                if (GUILayout.Button("이용", UIStyle.Button, GUILayout.Width(52))) Use(f);
                GUI.enabled = true; GUILayout.EndHorizontal();
            }
            if (warehouse != null)
            {
                GUILayout.Label("창고 · 한 개씩 맡기기 / 찾기", UIStyle.Rich);
                foreach (var item in gm.Db.items)
                {
                    if (item == null || item.questItem || p.cargo.Count(item) == 0) continue;
                    GUILayout.BeginHorizontal(); GUILayout.Label($"{item.displayName} 보유 {p.cargo.Count(item)}개", UIStyle.Small);
                    if (GUILayout.Button("맡기기", UIStyle.Button, GUILayout.Width(60))) Transfer(item, true);
                    GUILayout.EndHorizontal();
                }
                foreach (var s in new List<StoredCargo>(p.warehouse.At(town.id)))
                {
                    var item = gm.ItemById(s.itemId);
                    GUILayout.BeginHorizontal(); GUILayout.Label($"{(item != null ? item.displayName : s.itemId)} 보관 {s.count}개", UIStyle.Small);
                    GUI.enabled = item != null;
                    if (GUILayout.Button("찾기", UIStyle.Button, GUILayout.Width(60))) Transfer(item, false);
                    GUI.enabled = true; GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView(); GUILayout.EndArea();
            GUI.Label(new Rect(rect.x+12, rect.yMax-79, w-24, 48), message ?? "", UIStyle.Wrap);
            if (GUI.Button(new Rect(rect.xMax-96, rect.yMax-26, 82, 20), "닫기 (Esc)", UIStyle.Button)) Close();
        }
    }
}
