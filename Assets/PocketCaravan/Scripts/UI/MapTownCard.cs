using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/11 은샘: 지도(M)에서 마을 이름을 누르면 뜨는 작은 정보 카드. 모든 지역 지도(CuteMap)가 같이 쓴다.
    /// 보여 주는 것: 한 줄 소개(journeyNote), 특산품(이름과 그림), 놀거리, 시설, 여기서 몇 구간. 시세는 넣지 않는다.
    /// 놀거리 목록: Resources/MapActivities.json (에디터 메뉴 "Pocket Caravan/지도/놀거리 목록 만들기"가 맵을 돌며 만든다)
    ///   + 지금 서 있는 맵은 실제 자리에서 다시 읽는다. 떠돌이 상인(날마다 옮김)·숨은 수집물(찾는 재미)은 넣지 않는다.
    /// </summary>
    public static class MapTownCard
    {
        [Serializable] public class Entry { public string map; public List<string> names = new List<string>(); }
        [Serializable] public class ListFile { public List<Entry> maps = new List<Entry>(); }

        static Dictionary<string, List<string>> known;
        static Dictionary<string, List<string>> Known
        {
            get
            {
                if (known != null) return known;
                known = new Dictionary<string, List<string>>();
                var text = Resources.Load<TextAsset>("MapActivities");
                if (text != null)
                {
                    var file = JsonUtility.FromJson<ListFile>(text.text);
                    if (file != null && file.maps != null)
                        foreach (var e in file.maps) if (e != null && !string.IsNullOrEmpty(e.map)) known[e.map] = e.names ?? new List<string>();
                }
                return known;
            }
        }

        /// <summary>목록에 넣을 놀거리 자리인지 (떠돌이 상인·숨은 수집물 빼고)</summary>
        public static bool Listed(Interactable it, out string name)
        {
            name = null;
            if (it == null || it is Peddler.Spot || it is Collectibles.Spot) return false;
            return ActivityHints.Activity(it, out name, out _);
        }

        public static List<string> Activities(string mapId)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(mapId)) return list;
            if (Known.TryGetValue(mapId, out var names)) list.AddRange(names);
            if (MapRoot.Current != null && MapRoot.Current.mapId == mapId)
                foreach (var it in Interactable.All)
                    if (it != null && it.isActiveAndEnabled && Listed(it, out var n) && !list.Contains(n)) list.Add(n);
            return list;
        }

        // ---------------- 선택 상태 ----------------
        static string selected;
        static Rect anchor;
        static int lastFrame = -10;

        /// <summary>CuteMap.Draw가 매 OnGUI에서 부른다. 지도가 한동안 안 그려졌으면(닫혔다 열림) 고른 것을 지운다.</summary>
        public static void BeginFrame()
        {
            if (Time.frameCount - lastFrame > 2) selected = null;
            lastFrame = Time.frameCount;
        }

        public static string Selected => selected;
        public static void Select(string mapId, Rect label)
        {
            selected = selected == mapId ? null : mapId;
            anchor = label;
        }
        public static void Clear() => selected = null;

        // ---------------- 그리기 ----------------
        const float W = 196f;

        /// <summary>고른 곳의 카드를 window(지도 칸) 안에 그린다. 카드 위를 눌렀으면 true (지도 클릭으로 닫지 않게)</summary>
        public static bool Draw(Rect window)
        {
            if (string.IsNullOrEmpty(selected) || GameManager.I == null) return false;
            var gm = GameManager.I;
            var world = WorldIndex.Get();
            var info = world != null ? world.Map(selected) : null;
            string name = info != null && !string.IsNullOrEmpty(info.displayName) ? info.displayName : selected;
            var town = info != null && !string.IsNullOrEmpty(info.town) ? gm.Db.towns.Find(t => t != null && t.id == info.town) : null;
            string here = MapRoot.Current != null ? MapRoot.Current.mapId : null;
            int hops = world != null && here != null ? CargoSaleGuide.Distance(world, here, selected) : -1;
            if (here != null && GoryeoLayout.Resolve(here) == GoryeoLayout.Resolve(selected)) hops = 0;

            string note = town != null && !string.IsNullOrEmpty(town.journeyNote) ? FirstSentence(CuteUI.Plain(town.journeyNote)) : town == null ? "장터가 없는 길목이에요." : null;
            var specials = new List<ItemData>();
            if (town != null) foreach (var item in gm.Db.items) if (item != null && !item.questItem && item.originTown == town && specials.Count < 6) specials.Add(item);
            var fun = Activities(selected);
            var places = new List<string>();
            if (town != null) places.Add("장터");
            if (world != null && world.places.Exists(p => p.kind == "board" && p.mapId == selected)) places.Add("의뢰 게시판");
            if (town != null && town.facilities != null)
                foreach (var f in town.facilities) if (f != null && !string.IsNullOrEmpty(f.displayName) && !places.Contains(f.displayName)) places.Add(f.displayName);

            // 높이 먼저 잰다
            float inner = W - 20f;
            float h = 26f;
            if (note != null) h += CuteUI.WrapHeight(note, inner, true) + 4f;
            if (specials.Count > 0) h += 16f + Mathf.Ceil(specials.Count / 2f) * 19f + 2f;
            if (fun.Count > 0) h += 16f + ChipsHeight(fun, inner, true) + 2f;
            if (places.Count > 0) h += 16f + ChipsHeight(places, inner, false) + 2f;
            h += 4f;

            // 이름표 오른쪽(모자라면 왼쪽), 창 안에 둔다
            float x = anchor.xMax + 8f;
            if (x + W > window.xMax - 8f) x = anchor.x - 8f - W;
            x = Mathf.Clamp(x, window.x + 8f, window.xMax - 8f - W);
            float y = Mathf.Clamp(anchor.center.y - 22f, window.y + 8f, window.yMax - 8f - h);
            var r = new Rect(Mathf.Round(x), Mathf.Round(y), W, Mathf.Round(h));

            CuteUI.Box(r, 8, CuteUI.Paper, CuteUI.Ink, 2, 2);
            float cy = r.y + 14f;
            float tx = CuteUI.Label(r.x + 10f, cy, name, CuteUI.Text, false, true);
            string hop = hops == 0 ? "지금 여기" : hops > 0 ? $"{hops}구간" : "길 모름";
            ContractBoard.Pill(tx + 6f, cy, hop, hops == 0 ? CuteUI.Interact : CuteUI.Paper2, hops == 0 ? CuteUI.InteractLo : CuteUI.Line, hops == 0 ? CuteUI.White : CuteUI.Sub);
            bool closed = CloseMini(r.xMax - 18f, r.y + 6f);
            float yy = r.y + 26f;
            if (note != null) { CuteUI.Wrap(r.x + 10f, yy, inner, note, CuteUI.Sub, true); yy += CuteUI.WrapHeight(note, inner, true) + 4f; }

            ItemData hover = null; Rect hoverRect = default;
            if (specials.Count > 0)
            {
                yy = Heading(r.x + 10f, yy, "특산");
                for (int i = 0; i < specials.Count; i++)
                {
                    float ix = r.x + 10f + (i % 2) * (inner / 2f), iy = yy + (i / 2) * 19f;
                    TradeLedgerUI.ItemIcon(specials[i], ix, iy, 1);
                    string label = Fit(specials[i].displayName, inner / 2f - 22f);
                    CuteUI.Label(ix + 19f, iy + 8f, label, CuteUI.Text, true);
                    if (label != specials[i].displayName && new Rect(ix, iy, inner / 2f, 16f).Contains(Event.current.mousePosition)) { hover = specials[i]; hoverRect = new Rect(ix, iy, 16, 16); }
                }
                yy += Mathf.Ceil(specials.Count / 2f) * 19f + 2f;
            }
            if (fun.Count > 0)
            {
                yy = Heading(r.x + 10f, yy, "놀거리");
                yy = Chips(r.x + 10f, yy, inner, fun, true) + 2f;
            }
            if (places.Count > 0)
            {
                yy = Heading(r.x + 10f, yy, "시설");
                yy = Chips(r.x + 10f, yy, inner, places, false) + 2f;
            }
            if (hover != null)   // 이름이 잘린 특산품만
            {
                float tw = CuteUI.Width(hover.displayName, true) + 12f;
                var tip = new Rect(Mathf.Round(hoverRect.x), hoverRect.y - 17f, tw, 15f);
                CuteUI.Box(tip, 6, CuteUI.Paper, CuteUI.Ink, 1, 1);
                CuteUI.Label(tip.x + 6f, tip.center.y, hover.displayName, CuteUI.Text, true);
            }
            if (closed) { selected = null; return true; }
            return r.Contains(Event.current.mousePosition);
        }

        /// <summary>한 줄 소개: journeyNote의 첫 문장만 (뒤쪽은 값·거래 요령이라 지도 카드에는 넣지 않는다)</summary>
        static string FirstSentence(string s)
        {
            s = s.Trim();
            int i = s.IndexOf(". ", StringComparison.Ordinal);
            return i > 0 ? s.Substring(0, i + 1) : s;
        }

        static bool CloseMini(float x, float y)
        {
            var b = new Rect(x, y, 12, 12);
            bool over = b.Contains(Event.current.mousePosition);
            CuteUI.Circle(x, y, 12, over ? CuteUI.Paper2 : CuteUI.Paper, CuteUI.Line);
            CuteUI.LabelCenter(x + 6f, y + 6f, "×", CuteUI.Sub, true);
            return GUI.Button(b, GUIContent.none, GUIStyle.none);
        }

        static float Heading(float x, float y, string text)
        {
            CuteUI.Label(x, y + 6f, text, CuteUI.Sub, true, true);
            CuteUI.Fill(new Rect(x + CuteUI.Width(text, true, true) + 5f, y + 6f, W - 20f - CuteUI.Width(text, true, true) - 5f, 1f), CuteUI.Line);
            return y + 16f;
        }

        static string Fit(string s, float w)
        {
            if (CuteUI.Width(s, true) <= w) return s;
            for (int n = s.Length - 1; n > 1; n--) { string t = s.Substring(0, n) + "…"; if (CuteUI.Width(t, true) <= w) return t; }
            return s;
        }

        static float ChipWidth(string s, bool star) => Mathf.Round(CuteUI.Width((star ? "★ " : "") + s, true) + 10f);

        static float ChipsHeight(List<string> items, float w, bool star)
        {
            float x = 0, rows = 1;
            foreach (var s in items) { float cw = Mathf.Min(w, ChipWidth(s, star)); if (x > 0 && x + cw > w) { rows++; x = 0; } x += cw + 4f; }
            return rows * 17f;
        }

        static float Chips(float x0, float y, float w, List<string> items, bool star)
        {
            float x = 0;
            foreach (var s in items)
            {
                float cw = Mathf.Min(w, ChipWidth(s, star));
                if (x > 0 && x + cw > w) { y += 17f; x = 0; }
                var c = new Rect(x0 + x, y, cw, 14f);
                CuteUI.Box(c, 6, star ? CuteUI.EventPaper : CuteUI.Paper2, star ? CuteUI.Deco : CuteUI.Line);
                CuteUI.LabelCenter(c.center.x, c.center.y, (star ? "★ " : "") + s, CuteUI.Text, true);
                x += cw + 4f;
            }
            return y + 17f;
        }
    }
}
