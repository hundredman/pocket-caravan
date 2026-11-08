using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// L: 여행 중에도 직접 확인한 장터 가격을 비교한다. 원격 시장 조회는 하지 않는다.
    /// 11/05 B안(CuteUI):
    /// - 전체 시세: 왼쪽 물건 목록(최대 차익 = 가장 비싸게 파는 값 − 가장 싸게 사는 값, 이득 초록·0 회색·손해 빨강)
    ///   + 오른쪽 고른 물건의 경로 요약(살 곳 → 팔 곳)과 장터별 값·본 날짜. 살 때는 파랑, 팔 때는 주황.
    /// - 내 짐 판매처: 실은 물건마다 본 판매처와 원가 대비. 짐이 없으면 지금 차익이 가장 큰 물건을 추천한다.
    /// 마우스 휠로 목록을 굴린다.
    /// </summary>
    public sealed class TradeLedgerUI : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        int tab;   // 0 내 짐 판매처 · 1 전체 시세 · 2 최근 거래 (11/08)
        bool showCargo { get => tab == 0; set => tab = value ? 0 : 1; }
        int openedFrame;
        string selected;
        float listScroll, detailScroll, cargoScroll;

        /// <summary>물건 하나의 장부 요약: 본 장터 수, 가장 싸게 사는 곳, 가장 비싸게 파는 곳</summary>
        struct Summary
        {
            public ItemData item;
            public int known, buy, sell;
            public TownData buyTown, sellTown;
            public int Gain => sell - buy;
        }

        public void Open()
        {
            var gm = GameManager.I;
            if (IsOpen || UIState.Blocking || gm == null || gm.IsTravelling) return;
            if (!UIState.TryOpen(this, Close)) return;
            IsOpen = true;
            openedFrame = Time.frameCount;
            Sfx.Play("open");
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            UIState.Release(this);
        }

        void Update()
        {
            if (IsOpen && Time.frameCount != openedFrame && (InputReader.LedgerPressed() || InputReader.CancelPressed())) Close();
            else if (InputReader.LedgerPressed() && !UIState.JustClosed) Open();
        }

        void OnDisable() => Close();

        void OnGUI()
        {
            var gm = GameManager.I;
            if (!IsOpen || gm == null) return;
            UIStyle.Begin();
            GUI.depth = -10;
            CuteUI.Dim();
            float width = Mathf.Min(500, UIStyle.VirtualWidth - 16), height = Mathf.Min(286, UIStyle.VirtualHeight - 34);
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - width) / 2), Mathf.Round((UIStyle.VirtualHeight - height) / 2 + 8), width, height);
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11, "교역 장부");
            CuteUI.Keycap(r.xMax - 32 - CuteUI.KeycapWidth("Esc") - 3, r.y - 3, "Esc");
            if (CuteUI.CloseButton(r.xMax - 32, r.y - 9)) { Close(); return; }

            var content = new Rect(r.x + 15, r.y + 50, r.width - 30, r.height - 63);
            CuteUI.Box(content, 7, CuteUI.Paper, CuteUI.Ink);
            if (CuteUI.Tab(content.x + 6, content.y, "내 짐 판매처", CuteUI.Pic.Sack, tab == 0, out float w1)) tab = 0;
            if (CuteUI.Tab(content.x + 6 + w1 + 3, content.y, "전체 시세", CuteUI.Pic.Chart, tab == 1, out float w2)) tab = 1;
            if (CuteUI.Tab(content.x + 6 + w1 + 3 + w2 + 3, content.y, "최근 거래", CuteUI.Pic.Coin, tab == 2, out _)) tab = 2;
            CuteUI.LabelRight(content.xMax - 4, content.y - 9,
                tab == 0 ? "실은 물건을 어디에 팔면 좋을지 알려 줘요" : tab == 1 ? "직접 본 값이에요 · 가면 달라질 수 있어요" : $"최근 {TradeLedger.RecentMax}번 사고판 것", CuteUI.Sub, true);
            if (tab == 0) DrawCargo(gm, content); else if (tab == 1) DrawAll(gm, content); else DrawRecent(gm, content);
        }

        // ---------------- 전체 시세 ----------------

        void DrawAll(GameManager gm, Rect c)
        {
            var known = new List<Summary>();
            var unknown = new List<Summary>();
            foreach (var item in gm.Db.items)
            {
                if (item == null || item.questItem) continue;
                var s = Summarize(gm, item);
                (s.known > 0 ? known : unknown).Add(s);
            }
            known.AddRange(unknown);   // 본 물건 먼저 (각각 데이터 순서)
            var list = known;
            if (list.Count == 0) { CuteUI.LabelCenter(c.center.x, c.center.y, "교역 물건이 없어요", CuteUI.Sub, true); return; }
            int sel = list.FindIndex(s => s.item.id == selected);
            if (sel < 0) { sel = 0; selected = list[0].item.id; }

            // 왼쪽: 물건 목록
            const float lw = 178f, rowH = 30f;
            CuteUI.Label(c.x + 12, c.y + 10, "물건", CuteUI.Sub, true);
            CuteUI.LabelRight(c.x + lw, c.y + 10, "최대 차익", CuteUI.Sub, true);
            var area = new Rect(c.x + 6, c.y + 18, lw + 4, c.height - 22);
            listScroll = Scroll(area, listScroll, list.Count * rowH);
            GUI.BeginGroup(area);
            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i];
                float y = i * rowH - listScroll;
                if (y + rowH < 0 || y > area.height) continue;
                bool on = i == sel;
                var row = new Rect(0, y, lw, 27);
                if (on)
                {
                    CuteUI.Box(row, 8, CuteUI.Interact, CuteUI.Ink, 1, 2, CuteUI.InteractLo);
                    CuteUI.Fill(new Rect(5, y + 2, lw - 10, 1), CuteUI.InteractHi);
                }
                CuteUI.Circle(4, y + 3, 21, on ? CuteUI.Paper : CuteUI.Paper2, on ? CuteUI.Ink : CuteUI.Line);
                ItemIcon(s.item, 6, y + 5, 1);
                float badgeLeft = s.known > 0 ? CuteUI.GainBadge(lw - 5, y + 13.5f, s.Gain) : CuteUI.UnknownBadge(lw - 5, y + 13.5f);
                CuteUI.Label(30, y + 13.5f, Fit(s.item.displayName, badgeLeft - 36, false, on), on ? CuteUI.OnInteract : CuteUI.Text, false, on);
                if (GUI.Button(row, GUIContent.none, GUIStyle.none)) { selected = s.item.id; detailScroll = 0; }
            }
            GUI.EndGroup();
            for (float yy = c.y + 8; yy < c.yMax - 8; yy += 3) CuteUI.Fill(new Rect(c.x + 192, yy, 1, 1), CuteUI.Line);

            DrawDetail(gm, list[sel], new Rect(c.x + 202, c.y, c.xMax - 8 - (c.x + 202), c.height));
        }

        void DrawDetail(GameManager gm, Summary s, Rect d)
        {
            float x0 = d.x, w0 = d.width;
            CuteUI.FramedCircle(x0, d.y + 7, 40);
            ItemIcon(s.item, x0 + 4, d.y + 11, 2);
            CuteUI.Label(x0 + 48, d.y + 18, Fit(s.item.displayName, w0 - 50, false, true), CuteUI.Text, false, true);
            CuteUI.Icon(CuteUI.Pic.Pin, x0 + 48, d.y + 26, 1, CuteUI.Sub);
            CuteUI.Label(x0 + 56, d.y + 30, s.item.originTown != null ? s.item.originTown.displayName + " 특산" : "여러 마을", CuteUI.Sub, true);
            if (s.known == 0)
            {
                CuteUI.Label(x0 + 4, d.y + 66, "아직 이 물건 시세를 본 적이 없어요", CuteUI.Text, true, true);
                CuteUI.Label(x0 + 4, d.y + 82, "장터를 열면 그곳 값이 적혀요", CuteUI.Sub, true);
                return;
            }

            // 경로 요약: 가장 싸게 사는 곳 → 가장 비싸게 파는 곳 + 차익
            float ry = d.y + 54;
            CuteUI.Box(new Rect(x0, ry, w0, 28), 8, CuteUI.Paper2, CuteUI.Line);
            float badgeLeft = CuteUI.GainBadge(x0 + w0 - 6, ry + 14, s.Gain, true);
            string bt = s.buyTown.displayName, st = s.sellTown.displayName;
            float room = badgeLeft - 6 - (x0 + 6);
            if (CuteUI.RouteWidth(bt, s.buy, st, s.sell) > room)
            {
                float each = (room - 15 - CuteUI.ChipWidth("", s.buy) - CuteUI.ChipWidth("", s.sell)) / 2f;
                bt = Fit(bt, each, true, false); st = Fit(st, each, true, false);
            }
            CuteUI.Route(x0 + 6, ry + 14, bt, s.buy, st, s.sell);

            // 장터별 값: 숫자 크게, 가장 싸게 사는 값·가장 비싸게 파는 값만 색 테두리
            float hy = d.y + 95, cbR = w0 - 70, csR = w0 - 12;
            CuteUI.Label(x0 + 8, hy, "장터", CuteUI.Sub, true);
            CuteUI.Icon(CuteUI.Pic.Basket, x0 + cbR - CuteUI.Width("살 때", true) - 10, hy - 3);
            CuteUI.LabelRight(x0 + cbR, hy, "살 때", CuteUI.Buy, true);
            CuteUI.Icon(CuteUI.Pic.Coin7, x0 + csR - CuteUI.Width("팔 때", true) - 10, hy - 4);
            CuteUI.LabelRight(x0 + csR, hy, "팔 때", CuteUI.Sell, true);

            var rows = new List<(TownData town, TradeLedger.Quote q)>();
            int notSeen = 0;
            foreach (var town in gm.Db.towns)
            {
                if (town == null) continue;
                var q = gm.Player.ledger.Find(town.id, s.item.id);
                if (q != null) rows.Add((town, q)); else notSeen++;
            }
            const float rowH = 27f;
            var area = new Rect(x0, hy + 7, w0, d.yMax - 4 - (hy + 7));
            detailScroll = Scroll(area, detailScroll, rows.Count * rowH + (notSeen > 0 ? 22 : 0));
            GUI.BeginGroup(area);
            for (int j = 0; j < rows.Count; j++)
            {
                float y = j * rowH - detailScroll;
                if (y + rowH < 0 || y > area.height) continue;
                var (town, q) = rows[j];
                if (j % 2 == 0) CuteUI.Box(new Rect(0, y, w0 - 5, 25), 6, CuteUI.Paper2, CuteUI.Paper2, 0);
                CuteUI.Label(8, y + 8, Fit(town.displayName, cbR - 50, false, true), CuteUI.Text, false, true);
                CuteUI.Label(8, y + 19, SeenText(gm, q), CuteUI.Sub, true);
                Value(q.buy, cbR, y, town == s.buyTown, CuteUI.Buy, CuteUI.BuySoft);
                Value(q.sell, csR, y, town == s.sellTown, CuteUI.Sell, CuteUI.SellSoft);
            }
            if (notSeen > 0) CuteUI.Label(8, rows.Count * rowH - detailScroll + 10, $"아직 안 가 본 장터 {notSeen}곳", CuteUI.Sub, true);
            GUI.EndGroup();
        }

        static void Value(int v, float right, float y, bool best, Color32 col, Color32 soft)
        {
            string s = v.ToString();
            if (best)
            {
                float bw = CuteUI.Width(s, false, true) + 12;
                CuteUI.Box(new Rect(right - bw + 5, y + 4, bw, 17), 6, soft, col);
            }
            CuteUI.LabelRight(right, y + 12.5f, s, best ? col : CuteUI.Text, false, best);
        }

        static string SeenText(GameManager gm, TradeLedger.Quote q)
        {
            if (q.seenDay <= 0 || gm.Clock == null) return "언제 봤는지 몰라요";
            return TradeLedger.Ago(gm.Clock.day, q.seenDay) + " 확인";
        }

        static Summary Summarize(GameManager gm, ItemData item)
        {
            var s = new Summary { item = item };
            foreach (var town in gm.Db.towns)
            {
                if (town == null) continue;
                var q = gm.Player.ledger.Find(town.id, item.id);
                if (q == null) continue;
                if (s.known == 0 || q.buy < s.buy) { s.buy = q.buy; s.buyTown = town; }
                if (s.known == 0 || q.sell > s.sell) { s.sell = q.sell; s.sellTown = town; }
                s.known++;
            }
            return s;
        }

        // ---------------- 내 짐 판매처 ----------------

        void DrawCargo(GameManager gm, Rect c)
        {
            var here = MapRoot.Current != null ? MapRoot.Current.mapId : null;
            var blocks = new List<(ItemData item, List<CargoSaleGuide.Choice> choices, float h)>();
            float total = 0;
            foreach (var item in gm.Db.items)
            {
                if (item == null || item.questItem || gm.Player.cargo.Count(item) < 1) continue;
                var choices = CargoSaleGuide.Observed(gm.Player.ledger, gm.Db.towns, item, WorldIndex.Get(), here);
                float h = 28 + Mathf.Max(1, choices.Count) * 21 + 8;
                blocks.Add((item, choices, h));
                total += h;
            }
            if (blocks.Count == 0) { DrawEmpty(gm, c); return; }

            var area = new Rect(c.x + 6, c.y + 7, c.width - 12, c.height - 26);
            cargoScroll = Scroll(area, cargoScroll, total);
            GUI.BeginGroup(area);
            float y = -cargoScroll, w = area.width - 6;
            foreach (var b in blocks)
            {
                if (y + b.h >= 0 && y <= area.height) DrawCargoBlock(gm, b.item, b.choices, y, w);
                y += b.h;
            }
            GUI.EndGroup();
            Tip(c, "첫 1개 값 기준이에요 · 한꺼번에 많이 팔면 값이 내려가요");
        }

        static void DrawCargoBlock(GameManager gm, ItemData item, List<CargoSaleGuide.Choice> choices, float y, float w)
        {
            float cost = gm.Player.cargo.AverageCost(item);
            CuteUI.Box(new Rect(0, y, w, 24), 7, CuteUI.Paper2, CuteUI.Line);
            ItemIcon(item, 5, y + 4, 1);
            float tx = CuteUI.Label(26, y + 12, item.displayName, CuteUI.Text, false, true);
            CuteUI.Label(tx + 8, y + 12.5f, $"{gm.Player.cargo.Count(item)}개 · 평균 원가 {cost:0.#}냥", CuteUI.Sub, true);
            float ry = y + 28;
            if (choices.Count == 0)
            {
                CuteUI.Label(10, ry + 8, "이 물건 시세를 본 장터가 없어요 · 장터를 열면 적혀요", CuteUI.Sub, true);
                return;
            }
            float sx = w - 104;
            foreach (var ch in choices)
            {
                string route = ch.hops == 0 ? "현재 구역" : ch.hops < 0 ? "연결 미확인" : $"{ch.hops}구간 · 다음 {WorldIndex.Get().MapName(ch.nextMap)}";
                float nx = CuteUI.Label(10, ry + 8, ch.town.displayName, CuteUI.Text, false, true);
                CuteUI.Label(nx + 8, ry + 8.5f, Fit(route, sx - 10 - (nx + 8), true, false), CuteUI.Sub, true);
                CuteUI.Icon(CuteUI.Pic.Coin7, sx, ry + 4);
                CuteUI.Label(sx + 10, ry + 8, ch.sell.ToString(), CuteUI.Sell, false, true);
                CuteUI.GainBadge(w - 6, ry + 8, ch.sell - cost);
                ry += 21;
            }
        }

        void DrawEmpty(GameManager gm, Rect c)
        {
            float mid = Mathf.Round(c.center.x);
            bool roomy = c.height >= 210;
            float top = c.y + (roomy ? 92 : 12);
            if (roomy)
            {
                CuteUI.Circle(mid - 40, c.y + 8, 80, CuteUI.Paper2, CuteUI.Paper2, 0);
                var pc = PlayerController.Instance;
                var sr = pc != null && pc.anim != null ? pc.anim.GetComponent<SpriteRenderer>() : null;
                if (sr != null && sr.sprite != null)
                {
                    var sp = sr.sprite.rect;
                    float k = Mathf.Max(1f, Mathf.Floor(Mathf.Min(40f / sp.width, 60f / sp.height)));
                    CuteUI.Sprite(sr.sprite, new Rect(mid + 2 - sp.width * k, c.y + 84 - sp.height * k, sp.width * k, sp.height * k));
                }
                CuteUI.Icon(CuteUI.Pic.Sack, mid + 6, c.y + 58, 3);
                CuteUI.Box(new Rect(mid + 8, c.y + 16, 34, 18), 7, CuteUI.Paper, CuteUI.Ink, 1, 1);
                CuteUI.Fill(new Rect(mid + 14, c.y + 33, 3, 1), CuteUI.Ink);
                CuteUI.Fill(new Rect(mid + 15, c.y + 34, 1, 1), CuteUI.Ink);
                CuteUI.LabelCenter(mid + 25, c.y + 25, "텅~", CuteUI.Text, false, true);
            }
            CuteUI.LabelCenter(mid, top + 8, "실은 교역품이 없어요", CuteUI.Text, false, true);

            // 지금 차익이 가장 큰 물건
            Summary best = default;
            foreach (var item in gm.Db.items)
            {
                if (item == null || item.questItem) continue;
                var s = Summarize(gm, item);
                if (s.known > 0 && s.Gain > 0 && (best.item == null || s.Gain > best.Gain)) best = s;
            }
            if (best.item != null)
            {
                CuteUI.LabelCenter(mid, top + 25, "지금 차익이 가장 큰 물건", CuteUI.Sub, true);
                string name = best.item.displayName;
                float badgeW = CuteUI.Width(CuteUI.GainText(best.Gain), false, true) + 14;
                float total = 19 + CuteUI.Width(name, false, true) + 8 + CuteUI.RouteWidth(best.buyTown.displayName, best.buy, best.sellTown.displayName, best.sell) + 8 + badgeW;
                float rx = Mathf.Round(mid - total / 2f);
                CuteUI.Box(new Rect(rx - 8, top + 33, total + 16, 26), 8, CuteUI.Paper2, CuteUI.Line);
                ItemIcon(best.item, rx, top + 38, 1);
                float nx = CuteUI.Label(rx + 19, top + 46, name, CuteUI.Text, false, true) + 8;
                CuteUI.Route(nx, top + 46, best.buyTown.displayName, best.buy, best.sellTown.displayName, best.sell);
                CuteUI.GainBadge(rx + total, top + 46, best.Gain, true);
            }
            else CuteUI.LabelCenter(mid, top + 34, "장터에 들러 시세를 적어 보세요", CuteUI.Sub, true);

            float bw = CuteUI.Width("전체 시세 보기", false, true) + 34;
            if (CuteUI.Button(new Rect(mid - Mathf.Round(bw / 2f), top + 68, bw, 22), "전체 시세 보기", CuteUI.Pic.Chart)) showCargo = false;
            Tip(c, "배달 짐과 창고 물건은 여기 나오지 않아요");
        }

        // ---------------- 최근 거래 (11/08, 공용) ----------------

        void DrawRecent(GameManager gm, Rect c)
        {
            var list = gm.Player.ledger.Recent;
            if (list.Count == 0)
            {
                CuteUI.LabelCenter(c.center.x, c.y + 60, "아직 사고판 기록이 없어요", CuteUI.Text, false, true);
                CuteUI.LabelCenter(c.center.x, c.y + 78, "장터에서 사거나 팔면 여기 남아요", CuteUI.Sub, true);
                return;
            }
            float y = c.y + 14, w = c.width;
            for (int i = 0; i < list.Count; i++)
            {
                var t = list[i];
                var item = gm.ItemById(t.item);
                var town = gm.Db.towns.Find(x => x != null && x.id == t.town);
                var row = new Rect(c.x + 6, y, w - 12, 30);
                CuteUI.Box(row, 7, i == 0 ? CuteUI.Paper2 : CuteUI.Paper, CuteUI.Line);
                // 산/판 딱지
                var tag = new Rect(row.x + 6, row.y + 7, 30, 16);
                CuteUI.Box(tag, 6, t.sell ? CuteUI.SellSoft : CuteUI.BuySoft, t.sell ? CuteUI.Sell : CuteUI.Buy, 1);
                CuteUI.LabelCenter(tag.center.x, tag.center.y, t.sell ? "판매" : "구매", t.sell ? CuteUI.Sell : CuteUI.Buy, true, true);
                float x = tag.xMax + 6;
                if (item != null) { ItemIcon(item, x, row.y + 7, 1); x += 19; }
                x = CuteUI.Label(x, row.y + 15, (item != null ? item.displayName : t.item) + $" ×{t.count}", CuteUI.Text, false, true) + 8;
                string where = (town != null ? town.displayName : t.town) + " · " + TradeLedger.Ago(gm.Clock.day, t.day);
                CuteUI.Label(x, row.y + 15.5f, where, CuteUI.Sub, true);
                // 오른쪽: 합계 냥 (+ 판매면 손익)
                float rx = row.xMax - 8;
                if (t.sell) rx = CuteUI.GainBadge(rx, row.y + 15, t.profit) - 6;
                CuteUI.Icon(CuteUI.Pic.Coin7, rx - 10 - CuteUI.Width(t.total.ToString(), false, true), row.y + 11);
                CuteUI.LabelRight(rx, row.y + 15, t.total.ToString(), t.sell ? CuteUI.Sell : CuteUI.Buy, false, true);
                y += 34;
            }
            Tip(c, "같은 장터에서 이어서 사고판 것은 한 줄로 묶어요");
        }

        // ---------------- 도우미 ----------------

        static void Tip(Rect c, string text)
        {
            CuteUI.Icon(CuteUI.Pic.Bulb, c.x + 8, c.yMax - 16);
            CuteUI.Label(c.x + 19, c.yMax - 11, text, CuteUI.Sub, true);
        }

        static void ItemIcon(ItemData item, float x, float y, int scale)
        {
            if (item != null && item.icon != null) CuteUI.Sprite(item.icon, new Rect(x, y, 16 * scale, 16 * scale));
            else CuteUI.Icon(CuteUI.Pic.Sack, x + 3 * scale, y + 4 * scale, scale);
        }

        /// <summary>칸보다 길면 끝을 "…"로 줄인다</summary>
        static string Fit(string s, float width, bool small, bool bold)
        {
            if (string.IsNullOrEmpty(s) || width <= 0 || CuteUI.Width(s, small, bold) <= width) return s;
            while (s.Length > 1 && CuteUI.Width(s + "…", small, bold) > width) s = s.Substring(0, s.Length - 1);
            return s + "…";
        }

        /// <summary>마우스 휠로 굴리기 + 오른쪽 가는 막대. 새 위치를 돌려준다</summary>
        static float Scroll(Rect area, float offset, float contentH)
        {
            var e = Event.current;
            float max = Mathf.Max(0f, contentH - area.height);
            if (e.type == EventType.ScrollWheel && area.Contains(e.mousePosition)) { offset += e.delta.y * 10f; e.Use(); }
            offset = Mathf.Clamp(offset, 0f, max);
            if (max > 0f)
            {
                float bh = Mathf.Max(12f, area.height * area.height / contentH);
                float by = area.y + (area.height - bh) * (offset / max);
                CuteUI.Fill(new Rect(area.xMax - 3, area.y, 2, area.height), CuteUI.Paper2);
                CuteUI.Fill(new Rect(area.xMax - 3, by, 2, bh), CuteUI.Line);
            }
            return offset;
        }
    }
}
