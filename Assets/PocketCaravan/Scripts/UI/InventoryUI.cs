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
        Vector2 contractScroll, itemScroll, detailScroll;
        int columns = Cols, visibleSelection = -1;
        Vector2 lastItemViewSize;
        Rect lastWindow, itemViewport, detailViewport, contractViewport;
        readonly List<Rect> itemRects = new List<Rect>();
        readonly List<KeyValuePair<ItemData, int>> rows = new List<KeyValuePair<ItemData, int>>();

        public void Open()
        {
            var gm = GameManager.I;
            if (IsOpen || UIState.Blocking || gm == null || gm.IsTravelling) return;
            if (!UIState.TryOpen(this, Close)) return;
            IsOpen = true; openedFrame = Time.frameCount; selected = 0; visibleSelection = -1; itemScroll = detailScroll = Vector2.zero;
            Sfx.Play("open");
            Opened?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            UIState.Release(this);
        }

        void Update()
        {
            if (IsOpen)
            {
                if (Time.frameCount != openedFrame && (InputReader.InventoryPressed() || InputReader.CancelPressed())) { Close(); return; }
                var mv = InputReader.MovePressed();
                if (mv != Vector2Int.zero && rows.Count > 0)
                    selected = Mathf.Clamp(selected + mv.x - mv.y * columns, 0, rows.Count - 1);
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
            float width = Mathf.Min(470, UIStyle.VirtualWidth - 16), height = UIStyle.WindowHeight(300) - 6;
            var r = lastWindow = new Rect(Mathf.Round((UIStyle.VirtualWidth - width) / 2), Mathf.Round((UIStyle.VirtualHeight - height) / 2 + 6), width, height);
            CuteUI.Dim(); CuteUI.Window(r); CuteUI.TitlePlate(r.center.x, r.y - 11, "소지품");
            if (CuteUI.CloseButton(r.xMax - 16f, r.y - 8f)) { Close(); return; }   // 11/08 오른쪽 위 X
            string vehicle = p.vehicle != null ? p.vehicle.displayName : "등짐";
            string summary = $"돈 {p.money}냥 · 신용 {p.credit} · 운반 {vehicle}";
            float summaryHeight = Mathf.Max(18, CuteUI.PaperWrap.CalcHeight(new GUIContent(summary), width - 28));
            GUI.Label(new Rect(r.x + 14, r.y + 14, width - 28, summaryHeight), summary, CuteUI.PaperWrap);
            float capacityY = r.y + 14 + summaryHeight + 3;
            GUI.Label(new Rect(r.x + 14, capacityY, 120, 14), $"짐 {p.TotalWeight:0.#} / {p.Capacity:0.#}", CuteUI.PaperSmall);
            UIStyle.Bar(new Rect(r.x + 110, capacityY + 5, width - 138, 6), p.LoadRatio, p.LoadRatio > 0.85f ? CuteUI.Down : CuteUI.Load);

            float gx = r.x + 14, gy = capacityY + 19, cy = r.yMax - 94;
            int newColumns = Mathf.Clamp(Mathf.FloorToInt((width - 190) / (Slot + Gap)), 1, Cols);
            if (columns != newColumns) visibleSelection = -1;
            columns = newColumns;
            float gridWidth = columns * (Slot + Gap) + 14;
            float gridHeight = Mathf.Max(Slot, Mathf.Min(Slot * 2 + Gap, cy - 8 - gy));
            itemViewport = new Rect(gx, gy, gridWidth, gridHeight);
            if (lastItemViewSize != itemViewport.size) { visibleSelection = -1; lastItemViewSize = itemViewport.size; }
            int shown = Mathf.Max(rows.Count, columns * 2);
            float contentHeight = Mathf.Ceil(shown / (float)columns) * (Slot + Gap) - Gap;
            if (visibleSelection != selected)
            {
                float top = (selected / columns) * (Slot + Gap), bottom = top + Slot;
                if (top < itemScroll.y) itemScroll.y = top;
                else if (bottom > itemScroll.y + gridHeight) itemScroll.y = bottom - gridHeight;
                detailScroll = Vector2.zero; visibleSelection = selected;
            }
            itemScroll.y = Mathf.Round(Mathf.Clamp(itemScroll.y, 0, Mathf.Max(0, contentHeight - gridHeight)));
            itemScroll = GUI.BeginScrollView(itemViewport, itemScroll, new Rect(0, 0, columns * (Slot + Gap) - Gap, contentHeight));
            itemRects.Clear();
            for (int i = 0; i < shown; i++)
            {
                var cell = new Rect((i % columns) * (Slot + Gap), (i / columns) * (Slot + Gap), Slot, Slot); itemRects.Add(cell);
                CuteUI.Box(cell, 4, CuteUI.Paper2, i == selected && i < rows.Count ? CuteUI.Interact : CuteUI.Line, i == selected && i < rows.Count ? 2 : 1);
                if (i >= rows.Count) continue;
                var item = rows[i].Key;
                if (GUI.Button(cell, "", GUIStyle.none)) selected = i;
                DrawIcon(new Rect(cell.x + 6, cell.y + 4, 28, 28), item);
                GUI.Label(new Rect(cell.x, cell.yMax - 13, cell.width - 3, 12), $"<b>{rows[i].Value}</b>", RightSmall);
            }
            GUI.EndScrollView();

            float dx = itemViewport.xMax + 8;
            var info = new Rect(dx, gy, r.xMax - 14 - dx, gridHeight);
            CuteUI.Box(info, 4, CuteUI.Paper2, CuteUI.Line);
            string details = "칸을 고르면\n자세한 정보가 나와요.";
            if (rows.Count > 0)
            {
                var item = rows[selected].Key; int n = rows[selected].Value;
                float avg = p.cargo.AverageCost(item);
                var lines = new List<string> { $"<b>{item.displayName}</b> × {n}", $"무게 {item.weight:0.#} (모두 {item.weight * n:0.#})",
                    item.questItem ? "부탁받은 물건 · 팔 수 없음" : avg > 0 ? $"평균 원가 {avg:0.#}냥" : "얻은 물건 (원가 없음)" };
                if (!item.questItem)
                {
                    var choices = CargoSaleGuide.Observed(p.ledger, gm.Db.towns, item, WorldIndex.Get(), MapRoot.Current != null ? MapRoot.Current.mapId : null);
                    lines.Add(choices.Count > 0 ? $"확인한 최고 판매처: {choices[0].town.displayName} {choices[0].sell}냥" : "판매처 미확인 · 장부(L)");
                }
                details = string.Join("\n", lines);
            }
            else details = "짐이 비어 있어요.\n장터에서 물건을 살 수 있어요.";
            detailViewport = new Rect(info.x + 5, info.y + 4, info.width - 10, info.height - 8);
            float detailWidth = Mathf.Max(1, detailViewport.width - 18);
            float detailHeight = Mathf.Max(detailViewport.height, CuteUI.PaperSmall.CalcHeight(new GUIContent(details), detailWidth));
            detailScroll = GUI.BeginScrollView(detailViewport, detailScroll, new Rect(0, 0, detailWidth, detailHeight));
            GUI.Label(new Rect(0, 0, detailWidth, detailHeight), details, CuteUI.PaperSmall); GUI.EndScrollView();

            GUI.Label(new Rect(r.x + 14, cy, width - 28, 16), $"맡은 배달 짐 {p.contracts.Count}개", CuteUI.PaperRich);
            contractViewport = new Rect(r.x + 14, cy + 18, width - 28, 36);
            var contractLines = new List<string>(); var contractHeights = new List<float>(); float totalHeight = 0;
            var cm = gm.Contracts;
            foreach (var c in p.contracts)
            {
                string to = cm != null ? cm.TownName(c.toTown) : c.toTown, left = cm != null ? cm.TimeLeftText(c) : "";
                string pay = c.atonement ? $"보증금 {c.deposit}냥 반환" : $"보수 {c.reward}냥";
                string line = $"{c.cargo} (무게 {c.weight:0.#}) → {to}\n{left} · {pay}";
                float h = Mathf.Max(24, CuteUI.PaperSmall.CalcHeight(new GUIContent(line), contractViewport.width - 18));
                contractLines.Add(line); contractHeights.Add(h); totalHeight += h + 4;
            }
            contractScroll = GUI.BeginScrollView(contractViewport, contractScroll, new Rect(0, 0, contractViewport.width - 18, Mathf.Max(36, totalHeight)));
            if (contractLines.Count == 0) GUI.Label(new Rect(0, 0, contractViewport.width - 18, 30), "없음 · 마을 게시판에서 의뢰를 맡을 수 있어요.", CuteUI.PaperSmall);
            float y = 0;
            for (int i = 0; i < contractLines.Count; i++) { GUI.Label(new Rect(0, y, contractViewport.width - 18, contractHeights[i]), contractLines[i], CuteUI.PaperSmall); y += contractHeights[i] + 4; }
            GUI.EndScrollView();
            GUI.Label(new Rect(r.x + 14, r.yMax - 34, width - 124, 28), "방향키로 칸 선택 · 창고 물품은 보관소에서 확인", CuteUI.PaperSmall);
            if (GUI.Button(new Rect(r.xMax - 96, r.yMax - 28, 82, 20), "닫기 I / Esc", CuteUI.PaperButton)) Close();
        }

        static GUIStyle rightSmall, rightBase;   // 글꼴 프로필이 바뀌면(UIStyle.Small이 새로 만들어지면) 다시 만든다
        static GUIStyle RightSmall => rightSmall != null && rightBase == CuteUI.PaperSmall ? rightSmall
            : (rightSmall = new GUIStyle(rightBase = CuteUI.PaperSmall) { alignment = TextAnchor.LowerRight, wordWrap = false });

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
            else GUI.Label(r, string.IsNullOrEmpty(item.displayName) ? "?" : item.displayName.Substring(0, 1), CuteUI.PaperRich);
        }
    }
}
