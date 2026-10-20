using UnityEngine;

namespace PocketCaravan
{
    /// <summary>L: 여행 중에도 직접 확인한 장터 가격을 비교한다. 원격 시장 조회는 하지 않는다.</summary>
    public sealed class TradeLedgerUI : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        Vector2 scroll, cargoScroll;
        bool showCargo = true;
        int openedFrame;

        public void Open()
        {
            var gm = GameManager.I;
            if (IsOpen || UIState.Blocking || gm == null || gm.IsTravelling) return;
            IsOpen = true;
            openedFrame = Time.frameCount;
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
            float width = Mathf.Min(550, UIStyle.VirtualWidth - 16), height = 310;
            var r = new Rect((UIStyle.VirtualWidth - width) / 2, (UIStyle.VirtualHeight - height) / 2, width, height);
            GUI.Box(r, "", UIStyle.Box);
            GUI.Label(new Rect(r.x + 12, r.y + 10, width - 110, 18), "교역 장부 · 직접 확인한 시세", UIStyle.Rich);
            if (GUI.Button(new Rect(r.xMax - 94, r.y + 8, 82, 20), "닫기 L / Esc", UIStyle.Button)) Close();
            if (GUI.Button(new Rect(r.x+12,r.y+34,(width-28)/2,20),(showCargo?"▸ ":"")+"내 짐 판매처",UIStyle.Button))showCargo=true;
            if (GUI.Button(new Rect(r.center.x+2,r.y+34,(width-28)/2,20),(!showCargo?"▸ ":"")+"전체 시세",UIStyle.Button))showCargo=false;
            var viewport = new Rect(r.x + 12, r.y + 60, width - 24, height - 112);
            if(showCargo)DrawCargo(gm,viewport);
            else
            {
                int count = 0;
                foreach (var item in gm.Db.items) if (item != null && !item.questItem) count++;
                float contentWidth = Mathf.Max(viewport.width - 18, 116 + gm.Db.towns.Count * 116);
                scroll = GUI.BeginScrollView(viewport, scroll, new Rect(0, 0, contentWidth, 30 + count * 30));
                GUI.Label(new Rect(4, 2, 106, 24), "품목 / 기준가 지역", UIStyle.Small);
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
            }
            GUI.Label(new Rect(r.x + 12, r.yMax - 42, width - 24, 14), "장터를 열면 그곳 시세를 기록합니다. 미확인 지역은 직접 방문해 보세요.", UIStyle.Small);
            GUI.Label(new Rect(r.x + 12, r.yMax - 26, width - 24, 14), showCargo ? "과거 첫1개 가격순 · 연결 수는 이동 시간/안전도와 다릅니다. 전량 이익 보장 아님." : "마지막 확인값 · 첫 1개 기준 · 도착 시 재고와 장날에 따라 값이 달라집니다.", UIStyle.Small);
        }

        void DrawCargo(GameManager gm,Rect viewport)
        {
            GUILayout.BeginArea(viewport);
            cargoScroll=GUILayout.BeginScrollView(cargoScroll);
            bool any=false;
            foreach(var item in gm.Db.items)
            {
                if(item==null || item.questItem || gm.Player.cargo.Count(item)<1)continue;
                any=true;
                GUILayout.Label($"{item.displayName} {gm.Player.cargo.Count(item)}개 · 평균 원가 {gm.Player.cargo.AverageCost(item):0.#}냥",UIStyle.Rich);
                var choices=CargoSaleGuide.Observed(gm.Player.ledger,gm.Db.towns,item,WorldIndex.Get(),MapRoot.Current!=null?MapRoot.Current.mapId:null);
                if(choices.Count==0)GUILayout.Label("이 물품의 시세 기록이 없습니다. 장터를 방문해 확인하세요.",UIStyle.Small);
                foreach(var choice in choices)
                {
                    string route=choice.hops==0?"현재 구역":choice.hops<0?"현재 위치에서 연결 미확인":$"연결 {choice.hops}구간 · 다음 {WorldIndex.Get().MapName(choice.nextMap)}";
                    float margin=choice.sell-gm.Player.cargo.AverageCost(item);
                    GUILayout.Label($"{choice.town.displayName} · 확인 판매가 {choice.sell} · 원가 대비 {margin:+0.#;-0.#;0}냥/개",UIStyle.Small);
                    GUILayout.Label(route,UIStyle.Small);
                }
                GUILayout.Space(8);
            }
            if(!any)GUILayout.Label("일반 교역품을 싣고 있지 않습니다.\n전체 시세 탭에서 방문했던 장터의 매입·판매 가격을 비교하세요.\n맡은 배달 짐과 창고 물품은 현재 교역 화물에 포함하지 않습니다.",UIStyle.Wrap);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
