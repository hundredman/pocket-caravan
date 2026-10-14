using UnityEngine;

namespace PocketCaravan
{
    /// <summary>M: 전체 지도. 맵을 칸으로, 이어진 길을 선으로 보여 주고 지금 있는 곳·마을 등급·의뢰 목적지를 표시한다.</summary>
    public class WorldMapUI : MonoBehaviour
    {
        bool open;

        void Update()
        {
            if (open && (InputReader.MapPressed() || InputReader.CancelPressed())) { open = false; UIState.Release(); return; }
            if (!open && InputReader.MapPressed() && !UIState.Blocking && GameManager.I != null && !GameManager.I.IsTravelling)
            {
                open = true;
                UIState.Blocking = true;
                Sfx.Play("open");
            }
        }

        void OnGUI()
        {
            if (!open) return;
            var gm = GameManager.I;
            var index = WorldIndex.Get();
            if (gm == null || index == null) return;
            UIStyle.Begin();
            GUI.depth = -10;

            float w = 460, h = 160;
            var r = new Rect((UIStyle.VirtualWidth - w) / 2, (UIStyle.VirtualHeight - h) / 2, w, h);
            GUI.Box(r, "", UIStyle.Box);
            GUI.Label(new Rect(r.x + 12, r.y + 8, w, 14), "<color=#FFE08A>고려 · 예성강에서 개경까지</color>     (M 닫기)", UIStyle.Rich);

            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var m in index.maps) { minX = Mathf.Min(minX, m.coord.x); maxX = Mathf.Max(maxX, m.coord.x); minY = Mathf.Min(minY, m.coord.y); maxY = Mathf.Max(maxY, m.coord.y); }
            if (minX > maxX) return;
            float cellW = 96, cellH = 50;
            float ox = r.x + (w - (maxX - minX + 1) * cellW) / 2, oy = r.y + 40;
            Rect Box(Vector2Int c) => new Rect(ox + (c.x - minX) * cellW + 8, oy + (maxY - c.y) * (cellH + 30) + 10, cellW - 16, cellH);

            // 길
            foreach (var m in index.maps)
                foreach (var n in m.neighbors)
                {
                    var o = index.Map(n);
                    if (o == null) continue;
                    var a = Box(m.coord); var b = Box(o.coord);
                    var mid = new Rect(Mathf.Min(a.xMax, b.xMax) - 2, a.center.y - 2, Mathf.Abs(b.center.x - a.center.x) - cellW + 20, 4);
                    UIStyle.Bar(mid, 1f, new Color32(0xC9, 0xA0, 0x6A, 0xFF));
                }

            string here = MapRoot.Current != null ? MapRoot.Current.mapId : "";
            foreach (var m in index.maps)
            {
                var b = Box(m.coord);
                bool isHere = m.mapId == here;
                UIStyle.Bar(b, 1f, isHere ? new Color32(0x6E, 0x9A, 0x58, 0xFF) : new Color32(0x3A, 0x33, 0x50, 0xFF));
                string label = $"{m.displayName}";
                if (!string.IsNullOrEmpty(m.town))
                {
                    var t = gm.Db.towns.Find(x => x != null && x.id == m.town);
                    if (t != null) label += $"\n<size=9>{gm.GetMarket(t).LevelName}</size>";
                    foreach (var c in gm.Player.contracts)
                        if (c.toTown == m.town) { label += "\n<color=#ffd84a><size=9>의뢰 목적지</size></color>"; break; }
                }
                else label += "\n<size=9>길</size>";
                if (isHere) label += "\n<color=#ffffff><size=9>▲ 지금 여기</size></color>";
                GUI.Label(b, label, UIStyle.CenterRich);
            }

            GUI.Label(new Rect(r.x + 12, r.yMax - 34, w - 24, 28),
                "길에 들어서면 시간이 흐른다. 의뢰 기한과 장날(5일마다)을 생각해 길을 고르자. 밤길에는 도적이 잦다.", UIStyle.Small);
        }
    }
}
