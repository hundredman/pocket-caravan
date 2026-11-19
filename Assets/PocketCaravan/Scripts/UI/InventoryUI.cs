using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// I: 소지품 창 (2026-10-30, 유저테스트 "인벤토리 필수"). 모든 문명 공용. 11/19 윤채: 위 적재량·돈 / 왼쪽 물품·맡은 배달 / 오른쪽 물품 정보로 정리
    /// 위: 적재량(무게) 막대 · 운반 수단 · 돈. 왼쪽: 물건 칸(그림 · 개수) + 배달 주머니(맡은 의뢰 짐 · 목적지 · 남은 시간).
    /// 오른쪽: 고른 칸의 물품 정보(무게 · 평균 원가 · 확인한 판매처). 방향키로 칸을 고르고 I/Esc로 닫는다. 열려 있는 동안 이동·시간은 멈춘다(UIState.Blocking).
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
            IsOpen = true; openedFrame = Time.frameCount; selected = -1; visibleSelection = -1; itemScroll = detailScroll = Vector2.zero;   // 11/19 열 때는 고르기 전
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
                    selected = selected < 0 ? 0 : Mathf.Clamp(selected + mv.x - mv.y * columns, 0, rows.Count - 1);   // 11/19 고르기 전이면 첫 칸부터
            }
            else if (InputReader.InventoryPressed() && !UIState.JustClosed) Open();
        }

        void OnDisable() => Close();

        void Collect(GameManager gm)
        {
            rows.Clear();
            foreach (var kv in gm.Player.cargo.Items) if (kv.Key != null && kv.Value > 0) rows.Add(kv);
            rows.Sort((a, b) => a.Key.questItem != b.Key.questItem ? (a.Key.questItem ? 1 : -1) : string.CompareOrdinal(a.Key.displayName, b.Key.displayName));
            if (selected >= rows.Count) selected = rows.Count - 1;
        }

        // 11/19 윤채 (디자인 의견 "용량·돈 확인 → 물품 고르기 → 자세히 보기가 한눈에"): 겹 테두리 · 겹치는 정보를 정리했다.
        //   위 = 왼쪽 적재량(무게) 막대와 가방 종류 · 오른쪽 돈. 신용은 여정 수첩(성장)에서 보므로 여기서는 뺐다.
        //   가운데 = 왼쪽 약 60% 보유 물품 칸(종류 수, 칸은 한도가 아니라 보기용) + 그 아래 맡은 배달(의뢰마다 한 줄, 최대 수/3),
        //   오른쪽 약 40% 물품 정보(빈 가방 · 고르기 전 · 고른 뒤). 아래 = 조작 한 줄 + 보관소 안내 한 줄.
        //   "0/10"은 칸 수가 아니라 실은 무게(물건 + 의뢰 짐)와 가방 한도다 — 그래서 "적재량"이라 쓴다. 한도·규칙은 바꾸지 않았다.
        const float Cell = 36f, CellGap = 5f;
        static readonly Color32 SlotFill = new Color32(0xF6, 0xEA, 0xD4, 255), SlotShade = new Color32(0xE4, 0xD2, 0xB2, 255);
        void OnGUI()
        {
            var gm = GameManager.I;
            if (!IsOpen || gm == null) return;
            var p = gm.Player;
            Collect(gm);
            UIStyle.Begin();
            GUI.depth = -10;
            float width = Mathf.Min(500, UIStyle.VirtualWidth - 16), height = Mathf.Min(290, UIStyle.VirtualHeight - 34);
            var r = lastWindow = new Rect(Mathf.Round((UIStyle.VirtualWidth - width) / 2), Mathf.Round((UIStyle.VirtualHeight - height) / 2 + 8), width, height);
            CuteUI.Dim(); CuteUI.Window(r); CuteUI.TitlePlate(r.center.x, r.y - 11, "소지품");
            var paper = CuteUI.Paper;
            CuteUI.Fill(new Rect(r.x + 6, r.y + 6, r.width - 12, r.height - 12), new Color32(paper.r, paper.g, paper.b, 210));   // 창 안 점무늬를 옅게 (물품 그림 · 글이 먼저 보이게)
            if (CuteUI.CloseButton(r.xMax - 32, r.y - 9)) { Close(); return; }

            // ---- 위: 적재량 · 가방 종류 · 돈 ----
            float tx = r.x + 18, ty = r.y + 26;
            bool over = p.TotalWeight > p.Capacity + 0.001f;
            CuteUI.Icon(CuteUI.Pic.Sack, tx, ty - 5);
            float lx = CuteUI.Label(tx + 13, ty, "적재량", CuteUI.Text, true, true) + 6;
            CuteUI.Bar(new Rect(lx, ty - 4, 110, 8), p.TotalWeight / Mathf.Max(0.01f, p.Capacity), over ? CuteUI.Alert : CuteUI.Load, over ? CuteUI.AlertSoft : CuteUI.LoadHi);
            float vx = CuteUI.Label(lx + 116, ty, $"{p.TotalWeight:0.#} / {p.Capacity:0.#}", over ? CuteUI.Down : CuteUI.Text, true, true);
            if (over) CuteUI.Label(vx + 5, ty, "넘침", CuteUI.Down, true, true);
            if (over) CuteUI.Label(tx + 13, ty + 13, "짐이 넘쳤어요 · 장터에서 더 살 수 없고 걸음이 느려요", CuteUI.Down, true, true);
            else CuteUI.Label(tx + 13, ty + 13, $"{(p.vehicle != null ? p.vehicle.displayName : "등짐")} · 무게 {p.Capacity:0.#}까지 실어요", CuteUI.Sub, true);
            string money = p.money.ToString("N0") + "냥";
            CuteUI.LabelRight(r.xMax - 18, ty, money, CuteUI.Text, false, true);
            CuteUI.Icon(CuteUI.Pic.Coin7, r.xMax - 18 - CuteUI.Width(money, false, true) - 11, ty - 4);

            // ---- 가운데: 왼쪽 목록(약 60%) · 오른쪽 정보(약 40%) ----
            float bodyTop = r.y + 50, bodyBottom = r.yMax - 30;
            float innerW = width - 36;
            float leftW = Mathf.Round(innerW * .6f);
            float gx = r.x + 18;
            int newColumns = Mathf.Clamp(Mathf.FloorToInt((leftW - 8 + CellGap) / (Cell + CellGap)), 1, 8);
            if (columns != newColumns) visibleSelection = -1;
            columns = newColumns;
            float split = gx + leftW;
            for (float yy = bodyTop; yy < bodyBottom; yy += 3) CuteUI.Fill(new Rect(split, yy, 1, 1), CuteUI.Line);   // 좌우 얇은 점선

            // 보유 물품
            float hx = CuteUI.Label(gx, bodyTop + 6, "보유 물품", CuteUI.Text, true, true);
            CuteUI.Label(hx + 5, bodyTop + 6, rows.Count + "종", CuteUI.Sub, true);
            float gridHeight = Cell * 2 + CellGap;
            itemViewport = new Rect(gx, bodyTop + 16, columns * (Cell + CellGap) - CellGap + 6, gridHeight);
            if (lastItemViewSize != itemViewport.size) { visibleSelection = -1; lastItemViewSize = itemViewport.size; }
            int shown = Mathf.Max(columns * 2, Mathf.CeilToInt(rows.Count / (float)columns) * columns);
            float contentHeight = Mathf.Ceil(shown / (float)columns) * (Cell + CellGap) - CellGap;
            if (selected >= 0 && visibleSelection != selected)
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
                if (at.yMax < 0 || at.y > gridHeight) continue;
                bool on = i == selected && i < rows.Count;
                // 가방 칸: 연한 베이지 + 위·왼쪽 얕은 안쪽 그림자. 고른 칸은 같은 자리에 청록 테 두 줄 (크기 그대로)
                CuteUI.Box(at, 4, SlotFill, on ? CuteUI.Interact : SlotShade, on ? 2 : 1);
                CuteUI.Fill(new Rect(at.x + 3, at.y + 2, at.width - 6, 1), SlotShade);
                CuteUI.Fill(new Rect(at.x + 2, at.y + 3, 1, at.height - 6), SlotShade);
                if (i >= rows.Count) continue;
                var item = rows[i].Key;
                TradeLedgerUI.ItemIcon(item, at.x + 2, at.y + 2, 2);
                if (item.questItem) CuteUI.Icon(CuteUI.Pic.Star4, at.x + 2, at.y + 2);
                string n = rows[i].Value.ToString();
                float nw = Mathf.Round(CuteUI.Width(n, true, true) + 6);
                CuteUI.Box(new Rect(at.xMax - nw - 1, at.yMax - 12, nw, 12), 5, CuteUI.White, CuteUI.Ink);
                CuteUI.LabelCenter(at.xMax - 1 - nw / 2f, at.yMax - 6, n, CuteUI.Ink, true, true);
                if (CuteUI.Hit(at, GUIContent.none, GUIStyle.none)) { selected = i; detailScroll = Vector2.zero; }
            }
            GUI.EndGroup();

            // 배달 주머니: 맡은 의뢰 짐 (의뢰마다 한 줄 — 의뢰 하나가 짐 하나라 칸이 아니라 목록으로)
            float cy0 = itemViewport.yMax + 12;
            float cx = CuteUI.Label(gx, cy0 + 4, "배달 주머니", CuteUI.Text, true, true);
            CuteUI.Label(cx + 5, cy0 + 4, $"{p.contracts.Count}/{ContractManager.MaxActiveFor(p)}", CuteUI.Sub, true);
            contractViewport = new Rect(gx, cy0 + 12, leftW - 8, bodyBottom - (cy0 + 12));
            DrawContracts(gm, p, contractViewport);

            // 오른쪽: 물품 정보
            var d = detailViewport = new Rect(split + 12, bodyTop, r.xMax - 18 - (split + 12), bodyBottom - bodyTop);
            CuteUI.Label(d.x, bodyTop + 6, "물품 정보", CuteUI.Text, true, true);
            DrawDetail(gm, p, new Rect(d.x, bodyTop + 16, d.width, d.height - 16));

            // ---- 아래: 조작 한 줄 · 보관소 안내 (따로) ----
            float by = r.yMax - 17;
            CuteUI.Label(r.x + 18, by, "방향키 선택 · I / Esc 닫기", CuteUI.Sub, true);
            CuteUI.LabelRight(r.xMax - 18, by, "창고에 둔 물건은 보관소에서 봐요", CuteUI.Sub, true);
        }

        void DrawDetail(GameManager gm, PlayerState p, Rect d)
        {
            if (rows.Count == 0)
            {
                // 빈 가방: 작은 보따리 + 두 줄
                var ps = CuteUI.PicSize(CuteUI.Pic.Sack);
                CuteUI.Icon(CuteUI.Pic.Sack, d.center.x - ps.x, d.y + 24, 2);
                CuteUI.LabelCenter(d.center.x, d.y + 62, "아직 담은 물건이 없어요", CuteUI.Text, true, true);
                CuteUI.LabelCenter(d.center.x, d.y + 76, "장터에서 물건을 사 보세요", CuteUI.Sub, true);
                return;
            }
            if (selected < 0 || selected >= rows.Count)
            {
                CuteUI.LabelCenter(d.center.x, d.y + 50, "물건을 골라", CuteUI.Text, true, true);
                CuteUI.LabelCenter(d.center.x, d.y + 64, "자세히 살펴보세요", CuteUI.Sub, true);
                return;
            }
            var item = rows[selected].Key; int n = rows[selected].Value;
            float x0 = d.x, w0 = d.width;
            CuteUI.Box(new Rect(x0, d.y + 2, 38, 38), 4, SlotFill, SlotShade, 1);
            TradeLedgerUI.ItemIcon(item, x0 + 3, d.y + 5, 2);
            float qn = CuteUI.Width("× " + n, true) + 4;   // 이름은 개수 자리만 남기고 최대한 다 보이게 (좁은 창에서도)
            float nx = CuteUI.Label(x0 + 46, d.y + 12, TradeLedgerUI.Fit(item.displayName, w0 - 46 - qn, false, true), CuteUI.Text, false, true);
            CuteUI.Label(nx + 4, d.y + 12.5f, "× " + n, CuteUI.Sub, true);
            CuteUI.Label(x0 + 46, d.y + 28, TradeLedgerUI.Fit(item.questItem ? "부탁받은 물건" : item.originTown != null ? OriginPerks.ItemCivName(item) + " · " + item.originTown.displayName + " 특산" : "여러 마을", w0 - 48, true, false), CuteUI.Sub, true);

            float y = d.y + 46;
            float avg = p.cargo.AverageCost(item);
            Row(x0, y, w0, "무게", $"{item.weight:0.#} × {n} = {item.weight * n:0.#}", CuteUI.Text, true); y += 19;
            Row(x0, y, w0, "평균 원가", item.questItem ? "팔 수 없음" : avg > 0 ? $"{avg:0.#}냥" : "원가 없음", CuteUI.Text, false); y += 22;
            if (item.questItem)
            {
                CuteUI.WrapWords(x0 + 4, y, w0 - 8, "부탁받은 물건은 장터에서 팔 수 없어요. 맡긴 사람에게 가져다주세요", CuteUI.Sub, true);
                return;
            }
            CuteUI.Label(x0 + 4, y + 4, "확인한 판매처", CuteUI.Sub, true, true);
            CuteUI.LabelRight(x0 + w0 - 4, y + 4, "원가보다", CuteUI.Sub, true);
            y += 12;
            var choices = CargoSaleGuide.Observed(p.ledger, gm.Db.towns, item, WorldIndex.Get(), MapRoot.Current != null ? MapRoot.Current.mapId : null);
            if (choices.Count == 0)
            {
                CuteUI.Label(x0 + 4, y + 9, "아직 이 물건 값을 본 장터가 없어요", CuteUI.Text, true);
                CuteUI.Label(x0 + 4, y + 22, "장터를 열면 장부(L)에 적혀요", CuteUI.Sub, true);
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

        /// <summary>맡은 배달: 의뢰마다 한 줄 (짐 · 목적지 · 남은 시간 · 보수). 없으면 작은 한 문장</summary>
        void DrawContracts(GameManager gm, PlayerState p, Rect area)
        {
            var cm = gm.Contracts;
            if (p.contracts.Count == 0)
            {
                CuteUI.Label(area.x, area.y + 8, "마을 게시판에서 의뢰를 받을 수 있어요", CuteUI.Sub, true);
                return;
            }
            const float rowH = 22f;
            contractScroll.y = TradeLedgerUI.Scroll(area, contractScroll.y, p.contracts.Count * rowH);
            GUI.BeginGroup(area);
            float w = area.width - 6;
            for (int i = 0; i < p.contracts.Count; i++)
            {
                var c = p.contracts[i];
                float y = 2 + i * rowH - contractScroll.y;
                if (y + rowH < 0 || y > area.height) continue;
                CuteUI.Box(new Rect(0, y, w, 19), 5, SlotFill, c.dropped ? CuteUI.Alert : SlotShade, 1);
                CuteUI.Icon(CuteUI.Pic.Sack, 5, y + 4);
                string pay = c.atonement ? $"보증금 {c.deposit}" : $"{c.reward}냥";
                float pw = CuteUI.Width(pay, true, true);
                CuteUI.LabelRight(w - 6, y + 9.5f, pay, c.atonement ? CuteUI.Text : CuteUI.Up, true, true);
                string left = c.dropped ? "떨어뜨림" : cm != null ? CuteUI.Plain(cm.TimeLeftText(c)) : "";
                float lw = CuteUI.Width(left, true);
                CuteUI.LabelRight(w - 12 - pw, y + 9.5f, left, c.dropped ? CuteUI.Down : CuteUI.Sub, true);
                string to = cm != null ? cm.TownName(c.toTown) : c.toTown;
                CuteUI.Label(18, y + 9.5f, TradeLedgerUI.Fit($"{c.cargo} → {to}", w - 18 - 18 - pw - lw, true, true), CuteUI.Text, true, true);
            }
            GUI.EndGroup();
        }
    }
}
