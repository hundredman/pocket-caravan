using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// I: 소지품 창 (2026-10-30, 유저테스트 "인벤토리 필수"). 모든 문명 공용.
    /// 위: 돈 · 신용 · 운반 수단 · 적재 막대. 가운데: 물건 칸(그림 · 개수), 고른 칸의 자세한 정보(무게 · 평균 원가 · 확인한 판매처).
    /// 아래: 맡은 배달 짐(목적지 · 남은 시간). 방향키로 칸을 고르고 I/Esc로 닫는다. 열려 있는 동안 이동·시간은 멈춘다(UIState.Blocking).
    /// 데이터는 읽기만 한다(버리기·쓰기 없음). 튜토리얼 등은 Opened 이벤트를 듣는다.
    /// </summary>
    public sealed class InventoryUI : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        /// <summary>창이 열릴 때마다 (튜토리얼 퀘스트가 듣는다)</summary>
        public static event System.Action Opened;

        const int Cols = 6;
        const float Slot = 40f, Gap = 4f;
        int openedFrame, selected;
        Vector2 contractScroll;
        readonly List<KeyValuePair<ItemData, int>> rows = new List<KeyValuePair<ItemData, int>>();

        public void Open()
        {
            var gm = GameManager.I;
            if (IsOpen || UIState.Blocking || gm == null || gm.IsTravelling) return;
            IsOpen = true; openedFrame = Time.frameCount; selected = 0;
            UIState.Blocking = true;
            Sfx.Play("open");
            Opened?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            UIState.Release();
        }

        void Update()
        {
            if (IsOpen)
            {
                if (Time.frameCount != openedFrame && (InputReader.InventoryPressed() || InputReader.CancelPressed())) { Close(); return; }
                var mv = InputReader.MovePressed();
                if (mv != Vector2Int.zero && rows.Count > 0)
                    selected = Mathf.Clamp(selected + mv.x - mv.y * Cols, 0, rows.Count - 1);
            }
            else if (InputReader.InventoryPressed() && !UIState.JustClosed) Open();
        }

        void OnDisable() => Close();

        void Collect(GameManager gm)
        {
            rows.Clear();
            foreach (var kv in gm.Player.cargo.Items) if (kv.Key != null && kv.Value > 0) rows.Add(kv);
            rows.Sort((a, b) => a.Key.questItem != b.Key.questItem ? (a.Key.questItem ? 1 : -1) : string.CompareOrdinal(a.Key.displayName, b.Key.displayName));
            if (selected >= rows.Count) selected = Mathf.Max(0, rows.Count - 1);
        }

        void OnGUI()
        {
            var gm = GameManager.I;
            if (!IsOpen || gm == null) return;
            var p = gm.Player;
            Collect(gm);
            UIStyle.Begin();
            GUI.depth = -10;
            float width = Mathf.Min(470, UIStyle.VirtualWidth - 16), height = UIStyle.WindowHeight(300);
            var r = new Rect((UIStyle.VirtualWidth - width) / 2, (UIStyle.VirtualHeight - height) / 2, width, height);
            GUI.Box(r, "", UIStyle.Box);
            GUI.Label(new Rect(r.x + 12, r.y + 9, width - 110, 18), "소지품", UIStyle.Rich);
            if (GUI.Button(new Rect(r.xMax - 94, r.y + 8, 82, 20), "닫기 I / Esc", UIStyle.Button)) Close();

            // 위: 돈 · 신용 · 운반 수단 · 적재
            string vehicle = p.vehicle != null ? p.vehicle.displayName : "등짐";
            GUI.Label(new Rect(r.x + 12, r.y + 31, width - 24, 14),
                $"<color=#ffe08a>돈 {p.money}냥</color>   신용 {p.credit}   운반: {vehicle}", UIStyle.Rich);
            GUI.Label(new Rect(r.x + 12, r.y + 49, 120, 12), $"짐 {p.TotalWeight:0.#} / {p.Capacity:0.#}", UIStyle.Small);
            UIStyle.Bar(new Rect(r.x + 100, r.y + 52, width - 124, 6), p.LoadRatio, p.LoadRatio > 0.85f ? new Color32(230, 120, 90, 255) : new Color32(226, 196, 120, 255));

            // 가운데 왼쪽: 물건 칸
            float gx = r.x + 12, gy = r.y + 68;
            int shown = Mathf.Max(rows.Count, Cols * 2);
            for (int i = 0; i < shown; i++)
            {
                var cell = new Rect(gx + (i % Cols) * (Slot + Gap), gy + (i / Cols) * (Slot + Gap), Slot, Slot);
                if (cell.yMax > r.y + height - 70) break;
                var old = GUI.color;
                if (i == selected && i < rows.Count) GUI.color = new Color(1f, 0.82f, 0.35f);   // 고른 칸은 금빛 테두리
                GUI.Box(cell, "", UIStyle.Box);
                GUI.color = old;
                if (i >= rows.Count) continue;
                var item = rows[i].Key;
                if (GUI.Button(cell, "", GUIStyle.none)) selected = i;
                DrawIcon(new Rect(cell.x + 6, cell.y + 4, 28, 28), item);
                GUI.Label(new Rect(cell.x, cell.yMax - 13, cell.width - 3, 12), $"<b>{rows[i].Value}</b>", RightSmall);
            }
            if (rows.Count == 0)
                GUI.Label(new Rect(gx, gy + Slot * 2 + Gap * 2 + 4, Cols * (Slot + Gap), 30), "짐이 비어 있어요. 장터(Space)에서 물건을 사 보세요.", UIStyle.Small);

            // 가운데 오른쪽: 고른 물건 자세히
            float dx = gx + Cols * (Slot + Gap) + 6, dw = r.xMax - 12 - dx;
            var info = new Rect(dx, gy, dw, Slot * 2 + Gap);
            GUI.Box(info, "", UIStyle.Box);
            if (rows.Count > 0)
            {
                var item = rows[selected].Key; int n = rows[selected].Value;
                float avg = p.cargo.AverageCost(item);
                var lines = new List<string>
                {
                    $"<b>{item.displayName}</b> × {n}",
                    $"무게 {item.weight:0.#} (모두 {item.weight * n:0.#})",
                    item.questItem ? "<color=#9ad0ff>부탁받은 물건 · 팔 수 없음</color>" : avg > 0 ? $"평균 원가 {avg:0.#}냥" : "얻은 물건 (원가 없음)",
                };
                if (!item.questItem)
                {
                    var choices = CargoSaleGuide.Observed(p.ledger, gm.Db.towns, item, WorldIndex.Get(), MapRoot.Current != null ? MapRoot.Current.mapId : null);
                    lines.Add(choices.Count > 0 ? $"확인한 최고 판매처: {choices[0].town.displayName} {choices[0].sell}냥" : "판매처 미확인 · 장부(L)");
                }
                GUI.Label(new Rect(info.x + 6, info.y + 4, info.width - 12, info.height - 8), string.Join("\n", lines), UIStyle.Small);
            }
            else GUI.Label(new Rect(info.x + 6, info.y + 4, info.width - 12, 40), "칸을 고르면\n자세한 정보가 나와요.", UIStyle.Small);

            // 아래: 맡은 배달 짐
            float cy = r.yMax - 66;
            GUI.Label(new Rect(r.x + 12, cy, width - 24, 14), $"맡은 배달 짐 {p.contracts.Count}개", UIStyle.Rich);
            var view = new Rect(r.x + 12, cy + 16, width - 24, 32);
            contractScroll = GUI.BeginScrollView(view, contractScroll, new Rect(0, 0, view.width - 18, Mathf.Max(32, p.contracts.Count * 14)));
            if (p.contracts.Count == 0) GUI.Label(new Rect(0, 0, view.width - 18, 14), "없음 · 마을 게시판에서 의뢰를 맡을 수 있어요.", UIStyle.Small);
            var cm = gm.Contracts;
            for (int i = 0; i < p.contracts.Count; i++)
            {
                var c = p.contracts[i];
                string to = cm != null ? cm.TownName(c.toTown) : c.toTown;
                string left = cm != null ? cm.TimeLeftText(c) : "";
                GUI.Label(new Rect(0, i * 14, view.width - 18, 14), $"{c.cargo} (무게 {c.weight:0.#}) → {to} · {left} · 보수 {c.reward}냥", UIStyle.Small);
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(r.x + 12, r.yMax - 15, width - 24, 12), "방향키로 칸 고르기 · 창고에 맡긴 물건은 보관소에서 볼 수 있어요.", UIStyle.Small);
        }

        static GUIStyle rightSmall, rightBase;   // 글꼴 프로필이 바뀌면(UIStyle.Small이 새로 만들어지면) 다시 만든다
        static GUIStyle RightSmall => rightSmall != null && rightBase == UIStyle.Small ? rightSmall
            : (rightSmall = new GUIStyle(rightBase = UIStyle.Small) { alignment = TextAnchor.LowerRight, wordWrap = false });

        static void DrawIcon(Rect r, ItemData item)
        {
            var s = item.icon;
            if (s != null && s.texture != null)
            {
                var t = s.texture; var tr = s.textureRect;
                float k = Mathf.Min(r.width / tr.width, r.height / tr.height);
                var d = new Rect(r.center.x - tr.width * k / 2, r.center.y - tr.height * k / 2, tr.width * k, tr.height * k);
                GUI.DrawTextureWithTexCoords(d, t, new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height));
            }
            else GUI.Label(r, string.IsNullOrEmpty(item.displayName) ? "?" : item.displayName.Substring(0, 1), UIStyle.CenterRich);
        }
    }
}
