using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 11/08 은샘: 고려·중앙 동쪽 장터의 낮은 좌판(그릇 좌판 등) 뒤에 장꾼을 세운다. "사 주는 좌판"이다.
    /// - 장꾼마다 다른 마을 물건 몇 가지를 이 마을 장터 판매가보다 2할 5푼 비싸게 사 준다. 장꾼 한 명이 하루에 사는 개수는 정해져 있다.
    /// - 값은 이 마을 장터 살 값보다 1냥 낮게 묶어, 장터에서 사서 바로 넘기는 되팔기는 되지 않는다.
    /// - 판 것은 장터에서 판 것과 똑같이 장부·사건(GameEvents.ItemSold)에 남는다. 장터 시세는 건드리지 않는다.
    /// 자리·장꾼·사는 물건은 Resources/Goryeo/Stalls/Keepers.json. 그림은 승인된 사람 NPC 정면 그림을 그대로 쓴다.
    /// 장면·저장 형식은 바꾸지 않는다(오늘 산 개수는 공용 메모 "gstall:맵:번호:날짜").
    /// </summary>
    public sealed class GoryeoStalls : MonoBehaviour
    {
        [Serializable] public class Keeper { public int at; public int stall = -1; public int cx = -1, cy = -1; public string name, title, sprite, hello; public string[] wants; }   // stall: 이 맵 stalls의 번호(새 좌판). cx,cy: 기존 장터 그림 왼쪽 아래 칸(옮긴 뒤). 둘 다 없으면 at = 기존 좌판 번호
        [Serializable] public class Stall { public string asset; public int x, y; }   // 11/08 빈 공터에 더 놓는 승인 좌판 그림(왼쪽 아래 칸)
        [Serializable] public class Lift { public int x, y, dy; }   // 11/08 공터 아래 끝에 붙은 기존 장터 그림(왼쪽 아래 칸 x,y)을 dy칸 안쪽(위)으로 뺀다
        [Serializable] public class Map { public string id; public Stall[] stalls; public Keeper[] keepers; public Lift[] lift; }
        [Serializable] public class Config { public int schemaVersion; public string status; public float markup = 1.25f; public int dailyLimit = 5; public Map[] maps; }

        static readonly string[] CounterArt = { "wares_counter", "spice_stall", "fish_rack" };
        static Config config;
        static GoryeoStalls instance;

        // ---------------- 세우기 ----------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() { config = null; instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (instance != null || FindFirstObjectByType<GoryeoStalls>() != null) return;
            var go = new GameObject("Goryeo stall keepers");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GoryeoStalls>();
        }

        MapRoot applied;
        readonly List<Spot> spots = new List<Spot>();

        void OnEnable() => SceneManager.sceneLoaded += OnLoaded;
        void OnDisable() => SceneManager.sceneLoaded -= OnLoaded;
        void OnLoaded(Scene s, LoadSceneMode m) { applied = null; }

        void Update()
        {
            var map = MapRoot.Current;
            if (map != applied && map != null && GameManager.I != null) { applied = map; Install(map); }
            if (open != null) UpdateShop();
        }

        static Config Load()
        {
            if (config != null) return config;
            var text = Resources.Load<TextAsset>("Goryeo/Stalls/Keepers");
            if (text == null) return null;
            config = JsonUtility.FromJson<Config>(text.text);
            return config;
        }

        readonly List<(SpriteRenderer keeper, SpriteRenderer counter)> sorting = new List<(SpriteRenderer, SpriteRenderer)>();

        void Install(MapRoot map)
        {
            spots.RemoveAll(s => s == null);
            sorting.Clear();
            if (map.previewOnly || GoryeoTravelTransition.RegionOf(map.mapId) == null) return;
            var cfg = Load();
            var row = cfg != null && cfg.maps != null ? Array.Find(cfg.maps, m => m.id == map.mapId) : null;
            if (row == null || row.keepers == null) return;
            var market = map.transform.Find("Everyday - market");
            var town = FindFirstObjectByType<TownMarket>()?.town;
            var dressing = map.GetComponent<VillageDressing>();
            if (market == null || town == null || dressing == null) return;
            var counters = new List<SpriteRenderer>();
            foreach (var sr in market.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.sprite != null && Array.IndexOf(CounterArt, sr.sprite.name) >= 0) counters.Add(sr);
            counters.Sort((a, b) => a.transform.position.x != b.transform.position.x ? a.transform.position.x.CompareTo(b.transform.position.x) : a.transform.position.y.CompareTo(b.transform.position.y));
            // 11/08 은샘 "살짝 뒤로 빼서": 공터 아래 끝에 붙은 기존 좌판을 안쪽으로. 그림과 막힌 칸(맨 아랫줄)을 같이 옮긴다
            var art = new List<SpriteRenderer>();
            foreach (var sp in dressing.spaces)
                if (sp.normal != null) art.AddRange(sp.normal.GetComponentsInChildren<SpriteRenderer>(true));
            if (row.lift != null)
            {
                foreach (var l in row.lift)
                {
                    if (l == null || l.dy == 0) continue;
                    var c = art.Find(r => r != null && !r.name.EndsWith(" (moved)") && map.WorldToCell(r.transform.position + new Vector3(0.5f, 0.5f, 0f)) == new Vector3Int(l.x, l.y, 0));
                    if (c == null) { Debug.LogWarning($"[GoryeoStalls] {map.mapId}: 옮길 장터 그림이 ({l.x},{l.y})에 없다"); continue; }
                    c.transform.position += new Vector3(0f, l.dy, 0f);
                    c.name += " (moved)";
                    foreach (var sp in dressing.spaces)
                        for (int f = 0; f < sp.footprints.Length; f++)
                            if (sp.footprints[f].x == l.x && sp.footprints[f].y == l.y)
                                sp.footprints[f] = new RectInt(l.x, l.y + l.dy, sp.footprints[f].width, sp.footprints[f].height);
                }
            }
            var root = new GameObject("Goryeo stall keepers").transform;
            root.SetParent(map.transform, false);
            var footprints = new List<RectInt>();
            // 11/08 은샘: 빈 공터에 승인 좌판 그림을 더 놓는다 (일자로 늘어서지 않게 자리를 엇갈려 둔다). 막힘은 맨 아랫줄만, 기존 좌판과 같다
            var extra = new List<SpriteRenderer>();
            if (row.stalls != null)
                foreach (var st in row.stalls)
                {
                    var pic = st != null ? Resources.Load<Sprite>("Goryeo/Stalls/Art/" + st.asset) : null;
                    if (pic == null) { extra.Add(null); continue; }
                    var go = new GameObject(st.asset);
                    go.transform.SetParent(root, false);
                    var cc = map.CellCenter(new Vector3Int(st.x, st.y, 0)); go.transform.position = new Vector3(cc.x - .5f, cc.y - .5f, 0f);
                    var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = pic; sr.sortingOrder = 4;
                    extra.Add(sr);
                    footprints.Add(new RectInt(st.x, st.y, Mathf.RoundToInt(pic.rect.width / pic.pixelsPerUnit), 1));
                }
            for (int i = 0; i < row.keepers.Length; i++)
            {
                var k = row.keepers[i];
                if (k == null) continue;
                SpriteRenderer c = k.stall >= 0 ? (k.stall < extra.Count ? extra[k.stall] : null)
                    : k.cx >= 0 ? art.Find(r => r != null && map.WorldToCell(r.transform.position + new Vector3(0.5f, 0.5f, 0f)) == new Vector3Int(k.cx, k.cy, 0))
                    : (k.at >= 0 && k.at < counters.Count ? counters[k.at] : null);
                var sprite = Resources.Load<Sprite>("Goryeo/Stalls/Keepers/" + k.sprite);
                if (c == null || sprite == null) continue;
                float w = c.sprite.rect.width / c.sprite.pixelsPerUnit;
                var basePos = c.transform.position;   // 좌판 그림 기준점 = 왼쪽 아래
                // 그림: 좌판 바로 뒤 (좌판과 같은 순서, 조금 뒤 깊이라 좌판 앞면이 몸 아래를 가린다)
                var body = new GameObject(k.title + " " + k.name);
                body.transform.SetParent(root, false);
                body.transform.position = new Vector3(basePos.x + w / 2f, basePos.y + 1.5f, basePos.z + 0.05f);
                var sr = body.AddComponent<SpriteRenderer>();
                sr.sprite = sprite; sr.sortingOrder = c.sortingOrder;
                sorting.Add((sr, c));
                // 장꾼이 선 칸은 막는다 (좌판 뒤로 걸어 들어가 장꾼과 겹치지 않게)
                var kc = map.WorldToCell(body.transform.position);
                footprints.Add(new RectInt(kc.x, kc.y, 1, 1));
                // 말 거는 자리: 좌판 가운데 칸 (원래 막힌 칸이라 길은 그대로). 이름표는 따로 그린다
                var spot = new GameObject("말 거는 자리").AddComponent<Spot>();
                spot.transform.SetParent(body.transform, false);
                spot.transform.position = new Vector3(basePos.x + w / 2f, basePos.y + 0.5f, 0f);
                spot.blocksMovement = false;
                spot.keeper = k; spot.index = i; spot.town = town; spot.mapId = map.mapId; spot.face = sprite; spot.head = body.transform;
                spots.Add(spot);
            }
            // 기존 마을 꾸밈(VillageDressing)에 한 공간으로 더해 같은 막힘·앞뒤 정렬을 쓴다
            var space = new VillageDressing.Space { id = "goryeo-stall-keepers", normal = root.gameObject, footprints = footprints.ToArray() };
            space.renderers = extra.FindAll(r => r != null).ToArray();
            var list = new List<VillageDressing.Space>(dressing.spaces);
            list.RemoveAll(x => x.id == space.id);
            list.Add(space);
            dressing.spaces = list.ToArray();
        }

        void LateUpdate()
        {
            foreach (var (keeper, counter) in sorting)
                if (keeper != null && counter != null) keeper.sortingOrder = counter.sortingOrder;
        }

        /// <summary>좌판 앞 말 거는 자리. 이름은 비워 공용 이름표를 띄우지 않고, 장꾼 머리 위에 따로 적는다</summary>
        public sealed class Spot : Interactable
        {
            public Keeper keeper; public int index; public TownData town; public string mapId; public Sprite face; public Transform head;
            public override string DisplayName => "";
            public override Kind Category => Kind.Market;
            public override string ActionVerb => "물건 팔기";
            public override void Interact(PlayerController player) { if (instance != null) instance.Open(this); }
        }

        // ---------------- 값 ----------------

        public static int Price(TownData town, ItemData item)
        {
            var gm = GameManager.I;
            var cfg = Load();
            var m = gm.GetMarket(town);
            int sell = m.SellPrice(item), buy = m.BuyPrice(item);
            int want = Mathf.Max(sell + 1, Mathf.RoundToInt(sell * (cfg != null ? cfg.markup : 1.25f)));
            return Mathf.Max(1, Mathf.Min(want, buy - 1));   // 여기서 사서 바로 넘기면 손해가 되게
        }

        static string DayKey(Spot s) => $"gstall:{s.mapId}:{s.index}:{(GameManager.I != null ? GameManager.I.Clock.day : 0)}";
        public static int BoughtToday(Spot s) => GameManager.I != null ? Mathf.RoundToInt(GameManager.I.GetMemo(DayKey(s), 0f)) : 0;
        public static int Limit => Load() != null ? Mathf.Max(1, Load().dailyLimit) : 5;

        // ---------------- 창 ----------------

        Spot open;
        int openedFrame, sel;
        string message;
        float listScroll;

        void Open(Spot s)
        {
            if (!UIState.TryOpen(this, Close)) return;
            open = s; openedFrame = Time.frameCount; sel = 0; listScroll = 0; message = null;
            Sfx.Play("open");
        }

        void Close() { open = null; UIState.Release(this); }

        List<ItemData> Wants()
        {
            var list = new List<ItemData>();
            var gm = GameManager.I;
            if (open == null || gm == null) return list;
            foreach (var id in open.keeper.wants) { var it = gm.ItemById(id); if (it != null && !list.Contains(it)) list.Add(it); }
            // 가진 물건을 위로
            list.Sort((a, b) => (gm.Player.cargo.Count(b) > 0 ? 1 : 0).CompareTo(gm.Player.cargo.Count(a) > 0 ? 1 : 0));
            return list;
        }

        void UpdateShop()
        {
            if (Time.frameCount == openedFrame) return;
            if (InputReader.CancelPressed()) { Close(); return; }
            var list = Wants();
            var mv = InputReader.MovePressed();
            if (mv.y != 0 && list.Count > 0) sel = Mathf.Clamp(sel - mv.y, 0, list.Count - 1);
            if (InputReader.InteractPressed() && sel < list.Count) Sell(list[sel], 1);
        }

        void Sell(ItemData item, int count)
        {
            var gm = GameManager.I; var p = gm.Player;
            int have = p.cargo.Count(item), left = Limit - BoughtToday(open);
            if (have <= 0) { message = $"{Josa.Eul(item.displayName)} 갖고 있지 않아요."; Sfx.Play("cancel"); return; }
            if (left <= 0) { message = "오늘은 더 못 사요. 내일 또 와요."; Sfx.Play("cancel"); return; }
            int n = Mathf.Min(count, Mathf.Min(have, left));
            int total = 0; float profitSum = 0;
            bool ok = SavedEconomyChange.Try(gm, () =>
            {
                for (int i = 0; i < n; i++)
                {
                    int price = Price(open.town, item);
                    float cost = p.cargo.AverageCost(item);
                    int profit = Mathf.RoundToInt(price - cost);
                    p.cargo.Remove(item);
                    p.money += price;
                    total += price; profitSum += profit;
                    p.ledger.Record(true, open.town, item, price, profit, gm.Clock.day);
                    GameEvents.RaiseItemSold(item, open.town, price, profit);
                }
                gm.SetMemo(DayKey(open), BoughtToday(open) + n);
            });
            if (!ok) { message = SavedEconomyChange.Failure; Sfx.Play("cancel"); return; }
            Sfx.Play("coin");
            message = $"{item.displayName} {n}개 · {total}냥 받았어요 (손익 {(profitSum >= 0 ? "+" : "")}{Mathf.RoundToInt(profitSum)}냥)";
        }

        void OnGUI()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            UIStyle.Begin();
            if (open == null) { DrawNames(); return; }
            DrawShop(gm);
        }

        /// <summary>장꾼 머리 위 이름 (공용 이름표와 같은 자리 높이)</summary>
        void DrawNames()
        {
            var cam = Camera.main;
            if (cam == null || UIState.Blocking || MapRoot.Current == null) return;
            foreach (var s in spots)
            {
                if (s == null || s.head == null) continue;
                var sp = cam.WorldToScreenPoint(s.head.position + new Vector3(0f, 1.45f, 0f));
                if (sp.z < 0) continue;
                float k = UIStyle.Scale > 0 ? UIStyle.Scale : 1f;
                var gp = new Vector2(sp.x / k, (Screen.height - sp.y) / k);
                if (gp.x < -60 || gp.x > UIStyle.VirtualWidth + 60 || gp.y < -20 || gp.y > UIStyle.VirtualHeight + 20) continue;
                float a = WorldLabels.NearAlpha(s.head.position);   // 11/13 기우(팀 UI 통일): 좌판 이름표도 가까이에서만
                if (a <= 0f) continue;
                var oc = GUI.color; GUI.color = new Color(oc.r, oc.g, oc.b, oc.a * a);
                CuteUI.WorldText(gp.x, gp.y, s.keeper.title, true);   // 좌판이 붙어 있어 이름까지 쓰면 겹친다 (이름은 창 제목에)
                GUI.color = oc;
            }
        }

        void DrawShop(GameManager gm)
        {
            var p = gm.Player; var k = open.keeper;
            var list = Wants();
            GUI.depth = -10;
            float w = Mathf.Min(420, UIStyle.VirtualWidth - 16);
            float rowH = 34f;
            float listH = Mathf.Min(list.Count, 4) * rowH;
            float h = Mathf.Min(UIStyle.VirtualHeight - 34, 66 + 14 + listH + 36);
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), Mathf.Round((UIStyle.VirtualHeight - h) / 2 + 8), w, h);
            CuteUI.Dim(); CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11, $"{k.title} {k.name}");
            CuteUI.Keycap(r.xMax - 32 - CuteUI.KeycapWidth("Esc") - 3, r.y - 3, "Esc");
            if (CuteUI.CloseButton(r.xMax - 32, r.y - 9)) { Close(); return; }

            // 장꾼 얼굴 + 말풍선
            float fx = r.x + 16, fy = r.y + 18;
            CuteUI.FramedCircle(fx, fy, 40);
            if (open.face != null)
            {
                var tr = open.face.rect; float headH = 15f;
                CuteUI.Sprite(open.face, new Rect(fx + 4, fy + 6, 32, headH * 2), new Rect(0, 1f - headH / tr.height, 1, headH / tr.height));
            }
            var bubble = new Rect(fx + 50, fy, r.xMax - 16 - (fx + 50), 40);
            CuteUI.Box(bubble, 8, CuteUI.Paper2, CuteUI.Line);
            CuteUI.Wrap(bubble.x + 8, bubble.y + 5, bubble.width - 16, k.hello, CuteUI.Text, true);

            // 오늘 남은 개수
            int left = Mathf.Max(0, Limit - BoughtToday(open));
            float hy = r.y + 72;
            CuteUI.Label(r.x + 16, hy, "삽니다", CuteUI.Sub, true, true);
            CuteUI.LabelRight(r.xMax - 16, hy, left > 0 ? $"오늘 {left}개 더 사요" : "오늘은 다 샀어요 · 내일 또", left > 0 ? CuteUI.Sub : CuteUI.Down, true);

            var area = new Rect(r.x + 14, hy + 8, w - 28, listH);
            listScroll = TradeLedgerUI.Scroll(area, listScroll, list.Count * rowH);
            if (sel * rowH < listScroll) listScroll = sel * rowH; else if ((sel + 1) * rowH > listScroll + listH) listScroll = (sel + 1) * rowH - listH;
            GUI.BeginGroup(area);
            float cw = area.width - (list.Count * rowH > listH ? 7 : 0);
            for (int i = 0; i < list.Count; i++)
            {
                float y = i * rowH - listScroll;
                if (y + rowH < 0 || y > listH) continue;
                var item = list[i];
                int have = p.cargo.Count(item);
                bool on = i == sel;
                var row = new Rect(0, y, cw, rowH - 4);
                CuteUI.Box(row, 7, on ? CuteUI.Paper : CuteUI.Paper2, on ? CuteUI.Interact : CuteUI.Line, on ? 2 : 1);
                CuteUI.Circle(5, y + 3, 24, CuteUI.Paper, CuteUI.Line);
                TradeLedgerUI.ItemIcon(item, 9, y + 7, 1);
                CuteUI.Label(36, y + 10, item.displayName, have > 0 ? CuteUI.Text : CuteUI.Sub, false, true);
                CuteUI.Label(36, y + 22, have > 0 ? $"내 짐 {have}개" : "갖고 있지 않아요", have > 0 ? CuteUI.Sub : CuteUI.Zero, true);

                // 오른쪽: 단추 두 개, 그 왼쪽 값
                bool can = have > 0 && left > 0;
                var all = new Rect(cw - 6 - 40, y + 6, 40, 18);
                var one = new Rect(all.x - 4 - 44, y + 6, 44, 18);
                if (can)
                {
                    if (CuteUI.Button(one, "팔기")) { sel = i; Sell(item, 1); }
                    if (CuteUI.SoftButton(all, "모두")) { sel = i; Sell(item, have); }
                }
                else
                {
                    foreach (var b in new[] { one, all }) CuteUI.Box(b, 8, CuteUI.ZeroSoft, CuteUI.KeyLine);
                    CuteUI.LabelCenter(one.center.x, one.center.y, "팔기", CuteUI.Sub, true);
                    CuteUI.LabelCenter(all.center.x, all.center.y, "모두", CuteUI.Sub, true);
                    if (GUI.Button(one, GUIContent.none, GUIStyle.none) || GUI.Button(all, GUIContent.none, GUIStyle.none)) { sel = i; Sell(item, 1); }
                }
                int price = Price(open.town, item);
                int market = gm.GetMarket(open.town).SellPrice(item);
                float px = one.x - 8;
                string ps = price.ToString();
                CuteUI.LabelRight(px - CuteUI.Width("냥", true) - 1, y + 11, ps, CuteUI.Sell, false, true);
                CuteUI.Label(px - CuteUI.Width("냥", true), y + 12, "냥", CuteUI.Sell, true);
                CuteUI.Icon(CuteUI.Pic.Coin7, px - CuteUI.Width("냥", true) - 1 - CuteUI.Width(ps, false, true) - 10, y + 7);
                CuteUI.LabelRight(px, y + 23, price > market ? $"장터보다 +{price - market}" : "장터와 같아요", price > market ? CuteUI.Up : CuteUI.Sub, true);
                if (GUI.Button(new Rect(0, y, one.x - 90, rowH - 4), GUIContent.none, GUIStyle.none)) sel = i;
            }
            GUI.EndGroup();

            string note = string.IsNullOrEmpty(message) ? "↑↓ 고르기 · Space 1개 팔기 · 다른 마을 물건을 가져오면 장터보다 비싸게 사요" : message;
            CuteUI.Icon(CuteUI.Pic.Bulb, r.x + 16, r.yMax - 22);
            CuteUI.Label(r.x + 27, r.yMax - 17, TradeLedgerUI.Fit(note, w - 44, true, false), string.IsNullOrEmpty(message) ? CuteUI.Sub : CuteUI.Text, true);
        }
    }
}
