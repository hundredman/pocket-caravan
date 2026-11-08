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

        // 11/08 은샘: 글 줄 나열이던 소지품 창을 장부·게시판과 같은 모양으로. 위 줄 = 짐 막대(넘치면 빨강)·운반·신용·돈,
        // 왼쪽 = 물건 칸(그림·개수), 오른쪽 = 고른 물건(그림·무게·원가·확인한 판매처와 차익), 왼쪽 아래 = 맡은 배달 짐.
        const float Cell = 36f, CellGap = 4f;
        void OnGUI()
        {
            var gm = GameManager.I;
            if (!IsOpen || gm == null) return;
            var p = gm.Player;
            Collect(gm);
            UIStyle.Begin();
            GUI.depth = -10;
            float width = Mathf.Min(500, UIStyle.VirtualWidth - 16), height = Mathf.Min(286, UIStyle.VirtualHeight - 34);
            var r = lastWindow = new Rect(Mathf.Round((UIStyle.VirtualWidth - width) / 2), Mathf.Round((UIStyle.VirtualHeight - height) / 2 + 8), width, height);
            CuteUI.Dim(); CuteUI.Window(r); CuteUI.TitlePlate(r.center.x, r.y - 11, "소지품");
            CuteUI.Keycap(r.xMax - 32 - CuteUI.KeycapWidth("I") - 3, r.y - 3, "I");
            if (CuteUI.CloseButton(r.xMax - 32, r.y - 9)) { Close(); return; }

            ContractBoard.DrawStatus(p, new Rect(r.x + 15, r.y + 17, width - 30, 20), true);
            var content = new Rect(r.x + 15, r.y + 43, width - 30, height - 43 - 30);
            CuteUI.Box(content, 7, CuteUI.Paper, CuteUI.Ink);

            // 왼쪽: 물건 칸
            float leftW = Mathf.Min(Cols * (Cell + CellGap) + 6, content.width - 190);
            int newColumns = Mathf.Clamp(Mathf.FloorToInt((leftW - 6) / (Cell + CellGap)), 1, Cols);
            if (columns != newColumns) visibleSelection = -1;
            columns = newColumns;
            float gx = content.x + 8, gy = content.y + 20;
            CuteUI.Label(gx + 1, content.y + 10, "실은 물건", CuteUI.Sub, true, true);
            CuteUI.Label(gx + 1 + CuteUI.Width("실은 물건", true, true) + 5, content.y + 10, rows.Count + "가지", CuteUI.Sub, true);
            float gridHeight = Cell * 3 + CellGap * 2;
            itemViewport = new Rect(gx, gy, columns * (Cell + CellGap) + 4, gridHeight);
            if (lastItemViewSize != itemViewport.size) { visibleSelection = -1; lastItemViewSize = itemViewport.size; }
            int shown = Mathf.Max(rows.Count, columns * 3);
            float contentHeight = Mathf.Ceil(shown / (float)columns) * (Cell + CellGap) - CellGap;
            if (visibleSelection != selected)
            {
                float top = (selected / columns) * (Cell + CellGap), bottom = top + Cell;
                if (top < itemScroll.y) itemScroll.y = top;
                else if (bottom > itemScroll.y + gridHeight) itemScroll.y = bottom - gridHeight;
                visibleSelection = selected;
            }
            itemScroll.y = Mathf.Round(TradeLedgerUI.Scroll(itemViewport, itemScroll.y, contentHeight));
            GUI.BeginGroup(itemViewport);
            itemRects.Clear();
            for (int i = 0; i < shown; i++)
            {
                var cell = new Rect((i % columns) * (Cell + CellGap), (i / columns) * (Cell + CellGap), Cell, Cell); itemRects.Add(cell);
                var at = new Rect(cell.x, cell.y - itemScroll.y, Cell, Cell);
                bool on = i == selected && i < rows.Count;
                if (at.yMax < 0 || at.y > gridHeight) continue;
                CuteUI.Box(at, 5, on ? CuteUI.Paper : CuteUI.Paper2, on ? CuteUI.Interact : CuteUI.Line, on ? 2 : 1);
                if (i >= rows.Count) continue;
                var item = rows[i].Key;
                TradeLedgerUI.ItemIcon(item, at.x + 2, at.y + 2, 2);
                if (item.questItem) CuteUI.Icon(CuteUI.Pic.Star4, at.x + 2, at.y + 2);
                string n = rows[i].Value.ToString();
                float nw = Mathf.Round(CuteUI.Width(n, true, true) + 6);
                CuteUI.Box(new Rect(at.xMax - nw - 1, at.yMax - 12, nw, 12), 5, CuteUI.White, CuteUI.Ink);
                CuteUI.LabelCenter(at.xMax - 1 - nw / 2f, at.yMax - 6, n, CuteUI.Ink, true, true);
                if (GUI.Button(at, GUIContent.none, GUIStyle.none)) { selected = i; detailScroll = Vector2.zero; }
            }
            GUI.EndGroup();

            // 왼쪽 아래: 맡은 배달 짐
            float cy0 = itemViewport.yMax + 10;
            CuteUI.Label(gx + 1, cy0 + 4, "맡은 배달 짐", CuteUI.Sub, true, true);
            CuteUI.Label(gx + 1 + CuteUI.Width("맡은 배달 짐", true, true) + 5, cy0 + 4, $"{p.contracts.Count}/{ContractManager.MaxActive}", CuteUI.Sub, true);
            contractViewport = new Rect(gx, cy0 + 12, itemViewport.width, content.yMax - 6 - (cy0 + 12));
            DrawContracts(gm, p, contractViewport);
            for (float yy = content.y + 8; yy < content.yMax - 8; yy += 3) CuteUI.Fill(new Rect(itemViewport.xMax + 5, yy, 1, 1), CuteUI.Line);

            // 오른쪽: 고른 물건
            float dx = itemViewport.xMax + 14;
            detailViewport = new Rect(dx, content.y + 6, content.xMax - 8 - dx, content.height - 12);
            DrawDetail(gm, p, detailViewport);

            bool over = p.TotalWeight > p.Capacity + 0.001f;
            CuteUI.Icon(CuteUI.Pic.Bulb, content.x + 4, r.yMax - 22);
            CuteUI.Label(content.x + 15, r.yMax - 17, over ? "짐이 넘쳤어요 · 장터에서 더 살 수 없고 걸음이 느려요" : "방향키로 칸 고르기 · 창고에 둔 물건은 보관소에서 봐요", over ? CuteUI.Down : CuteUI.Sub, true);
        }

        void DrawDetail(GameManager gm, PlayerState p, Rect d)
        {
            if (rows.Count == 0)
            {
                CuteUI.Icon(CuteUI.Pic.Sack, d.center.x - 13, d.y + 50, 3);
                CuteUI.LabelCenter(d.center.x, d.y + 92, "짐이 비어 있어요", CuteUI.Text, true, true);
                CuteUI.LabelCenter(d.center.x, d.y + 107, "장터에서 물건을 사 보세요", CuteUI.Sub, true);
                return;
            }
            var item = rows[selected].Key; int n = rows[selected].Value;
            float x0 = d.x, w0 = d.width;
            CuteUI.FramedCircle(x0, d.y + 2, 40);
            TradeLedgerUI.ItemIcon(item, x0 + 4, d.y + 6, 2);
            float nx = CuteUI.Label(x0 + 48, d.y + 13, TradeLedgerUI.Fit(item.displayName, w0 - 90, false, true), CuteUI.Text, false, true);
            CuteUI.Label(nx + 4, d.y + 13, "× " + n, CuteUI.Sub, true);
            CuteUI.Icon(CuteUI.Pic.Pin, x0 + 48, d.y + 21, 1, CuteUI.Sub);
            CuteUI.Label(x0 + 56, d.y + 25, item.questItem ? "부탁받은 물건" : item.originTown != null ? item.originTown.displayName + " 특산" : "여러 마을", CuteUI.Sub, true);

            float y = d.y + 48;
            float avg = p.cargo.AverageCost(item);
            Row(x0, y, w0, "무게", $"{item.weight:0.#} × {n} = {item.weight * n:0.#}", CuteUI.Text, true); y += 19;
            Row(x0, y, w0, "평균 원가", item.questItem ? "팔 수 없음" : avg > 0 ? $"{avg:0.#}냥" : "원가 없음", CuteUI.Text, false); y += 24;
            if (item.questItem)
            {
                CuteUI.Wrap(x0 + 4, y, w0 - 8, "부탁받은 물건은 장터에서 팔 수 없어요. 맡긴 사람에게 가져다주세요.", CuteUI.Sub, true);
                return;
            }
            CuteUI.Label(x0 + 4, y + 4, "확인한 판매처", CuteUI.Sub, true, true);
            CuteUI.LabelRight(x0 + w0 - 4, y + 4, "원가보다", CuteUI.Sub, true);
            y += 12;
            var choices = CargoSaleGuide.Observed(p.ledger, gm.Db.towns, item, WorldIndex.Get(), MapRoot.Current != null ? MapRoot.Current.mapId : null);
            if (choices.Count == 0)
            {
                CuteUI.Label(x0 + 4, y + 10, "아직 이 물건 값을 본 장터가 없어요", CuteUI.Text, true);
                CuteUI.Label(x0 + 4, y + 24, "장터를 열면 장부(L)에 적혀요", CuteUI.Sub, true);
                return;
            }
            for (int i = 0; i < choices.Count && y + 20 <= d.yMax; i++)
            {
                var ch = choices[i];
                CuteUI.Box(new Rect(x0, y, w0, 19), 6, i == 0 ? CuteUI.SellSoft : CuteUI.Paper2, i == 0 ? CuteUI.Sell : CuteUI.Paper2, i == 0 ? 1 : 0);
                float bl = CuteUI.GainBadge(x0 + w0 - 4, y + 9.5f, avg > 0 ? ch.sell - avg : 0);
                float vx = bl - 6 - CuteUI.Width(ch.sell.ToString(), false, true);
                CuteUI.Label(vx, y + 9.5f, ch.sell.ToString(), CuteUI.Sell, false, true);
                CuteUI.Icon(CuteUI.Pic.Coin7, vx - 10, y + 6);
                string hop = ch.hops == 0 ? "여기" : ch.hops > 0 ? ch.hops + "구간" : "";
                float tx = CuteUI.Label(x0 + 6, y + 9.5f, TradeLedgerUI.Fit(ch.town.displayName, vx - 14 - (x0 + 6) - CuteUI.Width(hop, true) - 4, true, true), CuteUI.Text, true, true);
                if (hop != "") CuteUI.Label(tx + 4, y + 9.5f, hop, CuteUI.Sub, true);
                y += 21;
            }
        }

        static void Row(float x, float y, float w, string label, string value, Color32 col, bool striped)
        {
            if (striped) CuteUI.Box(new Rect(x, y, w, 17), 5, CuteUI.Paper2, CuteUI.Paper2, 0);
            CuteUI.Label(x + 6, y + 8.5f, label, CuteUI.Sub, true);
            CuteUI.LabelRight(x + w - 6, y + 8.5f, value, col, true, true);
        }

        void DrawContracts(GameManager gm, PlayerState p, Rect area)
        {
            var cm = gm.Contracts;
            if (p.contracts.Count == 0)
            {
                CuteUI.Box(new Rect(area.x, area.y + 2, area.width - 4, 22), 6, CuteUI.Paper2, CuteUI.Line);
                CuteUI.Label(area.x + 8, area.y + 13, "없음 · 마을 게시판에서 맡을 수 있어요", CuteUI.Sub, true);
                return;
            }
            const float rowH = 24f;
            contractScroll.y = TradeLedgerUI.Scroll(area, contractScroll.y, p.contracts.Count * rowH);
            GUI.BeginGroup(area);
            float w = area.width - 6;
            for (int i = 0; i < p.contracts.Count; i++)
            {
                var c = p.contracts[i];
                float y = 2 + i * rowH - contractScroll.y;
                if (y + rowH < 0 || y > area.height) continue;
                CuteUI.Box(new Rect(0, y, w, 22), 6, CuteUI.Paper2, c.dropped ? CuteUI.Alert : CuteUI.Line);
                CuteUI.Icon(CuteUI.Pic.Sack, 5, y + 6);
                string pay = c.atonement ? $"보증금 {c.deposit}" : $"{c.reward}냥";
                float pw = CuteUI.Width(pay, true, true);
                CuteUI.LabelRight(w - 6, y + 11, pay, c.atonement ? CuteUI.Text : CuteUI.Up, true, true);
                string left = c.dropped ? "떨어뜨림" : cm != null ? cm.TimeLeftText(c) : "";
                float lw = CuteUI.Width(left, true);
                CuteUI.LabelRight(w - 12 - pw, y + 11, left, c.dropped ? CuteUI.Down : CuteUI.Sub, true);
                string to = cm != null ? cm.TownName(c.toTown) : c.toTown;
                CuteUI.Label(18, y + 11, TradeLedgerUI.Fit($"{c.cargo} → {to}", w - 18 - 18 - pw - lw, true, true), CuteUI.Text, true, true);
            }
            GUI.EndGroup();
        }
    }
}
