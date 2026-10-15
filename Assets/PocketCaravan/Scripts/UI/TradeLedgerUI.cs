using UnityEngine;

namespace PocketCaravan
{
    /// <summary>L: 여행 중에도 직접 확인한 장터 가격을 비교한다. 원격 시장 조회는 하지 않는다.</summary>
    public sealed class TradeLedgerUI : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        Vector2 scroll;

        public void Open()
        {
            var gm = GameManager.I;
            if (IsOpen || UIState.Blocking || gm == null || gm.IsTravelling) return;
            IsOpen = true;
            UIState.Blocking = true;
            Sfx.Play("open");
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            UIState.Release();
        }

        void Update()
        {
            if (IsOpen && (InputReader.LedgerPressed() || InputReader.CancelPressed())) Close();
            else if (InputReader.LedgerPressed() && !UIState.JustClosed) Open();
        }

        void OnDisable() => Close();

        void OnGUI()
        {
            var gm = GameManager.I;
            if (!IsOpen || gm == null) return;
            UIStyle.Begin();
            GUI.depth = -10;
            float width = Mathf.Min(550, UIStyle.VirtualWidth - 16), height = 310;
            var r = new Rect((UIStyle.VirtualWidth - width) / 2, (UIStyle.VirtualHeight - height) / 2, width, height);
            GUI.Box(r, "", UIStyle.Box);
            GUI.Label(new Rect(r.x + 12, r.y + 10, width - 110, 18), "교역 장부 · 직접 확인한 시세", UIStyle.Rich);
            if (GUI.Button(new Rect(r.xMax - 94, r.y + 8, 82, 20), "닫기 L / Esc", UIStyle.Button)) Close();
            var viewport = new Rect(r.x + 12, r.y + 38, width - 24, height - 90);
            int count = 0;
            foreach (var item in gm.Db.items) if (item != null && !item.questItem) count++;
            float contentWidth = Mathf.Max(viewport.width - 18, 116 + gm.Db.towns.Count * 116);
            scroll = GUI.BeginScrollView(viewport, scroll, new Rect(0, 0, contentWidth, 30 + count * 30));
            GUI.Label(new Rect(4, 2, 106, 24), "품목 / 산지", UIStyle.Small);
            for (int t = 0; t < gm.Db.towns.Count; t++)
                if (gm.Db.towns[t] != null) GUI.Label(new Rect(116 + t * 116, 2, 110, 24), gm.Db.towns[t].displayName, UIStyle.Small);
            int row = 0;
            foreach (var item in gm.Db.items)
            {
                if (item == null || item.questItem) continue;
                float y = 30 + row++ * 30;
                GUI.Label(new Rect(4, y, 108, 12), item.displayName, UIStyle.Small);
                GUI.Label(new Rect(4, y + 12, 108, 12), item.originTown != null ? item.originTown.displayName : "여러 마을", UIStyle.Small);
                for (int t = 0; t < gm.Db.towns.Count; t++)
                {
                    var town = gm.Db.towns[t];
                    if (town == null) continue;
                    var quote = gm.Player.ledger.Find(town.id, item.id);
                    float x = 116 + t * 116;
                    if (quote == null) GUI.Label(new Rect(x, y + 5, 110, 16), "미확인", UIStyle.Small);
                    else
                    {
                        GUI.Label(new Rect(x, y, 110, 12), $"살 때 {quote.buy}냥", UIStyle.Small);
                        GUI.Label(new Rect(x, y + 12, 110, 12), $"팔 때 {quote.sell}냥", UIStyle.Small);
                    }
                }
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(r.x + 12, r.yMax - 42, width - 24, 14), "장터를 열면 그곳 시세를 기록합니다. 미확인 지역은 직접 방문해 보세요.", UIStyle.Small);
            GUI.Label(new Rect(r.x + 12, r.yMax - 26, width - 24, 14), "마지막 확인값 · 첫 1개 기준 · 도착 시 재고와 장날에 따라 값이 달라집니다.", UIStyle.Small);
        }
    }
}
