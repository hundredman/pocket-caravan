using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 여관 사랑방 · 나그네 소문 (2026-11-18 은샘 B안). 여관에서 자고 일어나면 생기는 일.
    /// - 다른 지역 나그네 셋이 엎어 둔 소문 카드를 내민다. 카드 앞에는 어느 지역 소문인지만 보이고 내용은 감춰져 있다.
    /// - 하나만 골라 들을 수 있다 (←→·마우스로 고르고 Space·클릭). 고르면 카드가 뒤집혀 소문이 보인다.
    /// - 소문은 셋 다 참이다: 그 장터의 지금 시세(비싸게 사 가는 물건 또는 싸게 파는 물건). 들은 시세는 장부에 직접 본 것처럼 적힌다.
    /// - 돈·짐은 주지 않는다 (정보만). 지금 있는 마을은 빼고, 지역은 서로 다르게 뽑는다.
    /// Inn이 잠에서 깬 뒤 Begin을 부르고, 닫히면 done을 부른다. UIState.Blocking은 그동안 Inn이 쥔 채로 둔다.
    /// </summary>
    public class InnRumor : MonoBehaviour
    {
        class Rumor
        {
            public string civ, regionName, teller, line;
            public TownData town; public ItemData item; public int price; public bool sellHigh;
            public Sprite face;
        }

        static InnRumor inst;
        readonly List<Rumor> cards = new List<Rumor>();
        int focus, picked = -1;
        float openedAt, pickedAt;
        System.Action done;
        Vector2 lastMove = Vector2.one;
        public static bool IsOpen => inst != null && inst.done != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<InnRumor>() != null) return;
            var go = new GameObject("InnRumor"); DontDestroyOnLoad(go); inst = go.AddComponent<InnRumor>();
        }
        void Awake() => inst = this;

        static readonly Dictionary<string, Color32> CivColor = new Dictionary<string, Color32>
        {
            { "goryeo", new Color32(0x2E, 0x8C, 0x86, 255) }, { "venice", new Color32(0xC0, 0x4A, 0x5A, 255) },
            { "arabia", new Color32(0xD9, 0x8A, 0x1E, 255) }, { "north", new Color32(0x4F, 0x7F, 0xD0, 255) },
            { "center", new Color32(0x7A, 0x8C, 0x3A, 255) },
        };
        static readonly Dictionary<string, string> Tellers = new Dictionary<string, string>
        {
            { "goryeo", "고려 보부상" }, { "venice", "베네치아 뱃사람" }, { "arabia", "아라비아 대상" }, { "north", "북쪽 모피 장수" }, { "center", "길목 마부" },
        };

        /// <summary>자고 일어난 뒤 소문 카드를 편다. 소문을 만들 수 없으면(장터가 셋도 안 되면) 바로 done</summary>
        public static void Begin(System.Action onDone)
        {
            if (inst == null) Boot();
            if (inst == null || !inst.Build()) { onDone?.Invoke(); return; }
            inst.done = onDone; inst.focus = 1; inst.picked = -1; inst.openedAt = Time.unscaledTime; inst.lastMove = Vector2.one;
            Sfx.Play("open");
        }

        bool Build()
        {
            cards.Clear();
            var gm = GameManager.I;
            if (gm == null || gm.Db == null) return false;
            string hereMap = MapRoot.Current != null ? MapRoot.Current.mapId : null;
            var index = WorldIndex.Get();
            // 지역별 장터 (지금 맵의 장터는 뺀다)
            var byCiv = new Dictionary<string, List<TownData>>();
            foreach (var t in gm.Db.towns)
            {
                if (t == null || t.region == null) continue;
                if (index != null && hereMap != null && index.MapOf("market", t.id) == hereMap) continue;
                string civ = OriginPerks.Civ(t.region);
                if (!byCiv.TryGetValue(civ, out var list)) byCiv[civ] = list = new List<TownData>();
                list.Add(t);
            }
            var civs = new List<string>(byCiv.Keys);
            // 섞기
            for (int i = civs.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (civs[i], civs[j]) = (civs[j], civs[i]); }
            foreach (var civ in civs)
            {
                if (cards.Count >= 3) break;
                var towns = byCiv[civ];
                for (int tries = 0; tries < 4 && towns.Count > 0; tries++)
                {
                    var town = towns[Random.Range(0, towns.Count)];
                    var r = Make(gm, civ, town);
                    if (r != null) { cards.Add(r); break; }
                }
            }
            return cards.Count == 3;
        }

        static Rumor Make(GameManager gm, string civ, TownData town)
        {
            var m = gm.GetMarket(town);
            if (m == null) return null;
            bool sellHigh = Random.value < .5f;
            ItemData best = null; float bestScore = 0f; int bestPrice = 0;
            for (int pass = 0; pass < 2 && best == null; pass++, sellHigh = !sellHigh)
                foreach (var st in town.stock)   // 그 장터가 다루는 물건 (KnownItems는 내가 본 것만이라 쓰지 않는다)
                {
                    var item = st.item;
                    if (item == null || item.questItem || item.basePrice <= 0) continue;
                    if (!sellHigh && !m.CanBuy(item)) continue;
                    int price = sellHigh ? m.SellPrice(item) : m.BuyPrice(item);
                    if (price <= 0) continue;
                    float score = sellHigh ? price / item.basePrice : item.basePrice / price;
                    if (best == null || score > bestScore) { best = item; bestScore = score; bestPrice = price; }
                }
            if (best == null) return null;
            sellHigh = !sellHigh;   // 위 for가 끝날 때 한 번 더 뒤집혔다
            string name = best.displayName, place = town.displayName;
            string line = sellHigh
                ? $"{place} 장터에선 요즘 {Josa.Ga(name)} 귀하대. 하나에 {bestPrice}냥은 쳐 준다더군."
                : $"{place} 장터에 {Josa.Ga(name)} 넘쳐 난다네. {bestPrice}냥이면 하나 산다지.";
            return new Rumor
            {
                civ = civ, regionName = OriginPerks.CivName(civ), teller = Tellers.TryGetValue(civ, out var tl) ? tl : "나그네",
                line = line, town = town, item = best, price = bestPrice, sellHigh = sellHigh,
                face = Resources.Load<Sprite>("Haggle/merchant_" + civ) ?? Resources.Load<Sprite>("Haggle/merchant_center"),
            };
        }

        void Pick(int i)
        {
            if (picked >= 0 || i < 0 || i >= cards.Count) return;
            picked = i; pickedAt = Time.unscaledTime;
            var r = cards[i]; var gm = GameManager.I;
            if (gm != null) gm.Player.ledger.Observe(r.town, gm.GetMarket(r.town), new[] { r.item }, gm.Clock.day);   // 들은 시세를 장부에
            Sfx.Play("accept");
        }

        void Close()
        {
            var cb = done; done = null; cards.Clear();
            Sfx.Play("move", .6f);
            cb?.Invoke();
        }

        void Update()
        {
            if (done == null) return;
            float age = Time.unscaledTime - openedAt;
            if (age < .35f) return;   // 깨자마자 누른 키는 받지 않는다
            if (picked < 0)
            {
                var mv = InputReader.MoveVector();
                if (mv.sqrMagnitude > .25f && lastMove.sqrMagnitude <= .25f && Mathf.Abs(mv.x) > Mathf.Abs(mv.y))
                { focus = Mathf.Clamp(focus + (mv.x > 0 ? 1 : -1), 0, cards.Count - 1); Sfx.Play("move", .5f); }
                lastMove = mv;
                if (InputReader.InteractPressed()) Pick(focus);
                else if (InputReader.CancelPressed()) Close();   // 안 듣고 그냥 잔다
                return;
            }
            if (Time.unscaledTime - pickedAt > .5f && (InputReader.InteractPressed() || InputReader.CancelPressed())) Close();
        }

        // ---------------- 그리기 ----------------
        const float CW = 124f, CH = 170f;

        void OnGUI()
        {
            if (done == null) return;
            UIStyle.Begin();
            GUI.depth = -910;   // 여관 어둠(-900) 위
            float vw = UIStyle.VirtualWidth, vh = UIStyle.VirtualHeight, now = Time.unscaledTime;
            CuteUI.Fill(new Rect(0, 0, vw, vh), new Color32(12, 10, 26, 200));   // 밤 사랑방
            float k = Mathf.Clamp01((now - openedAt) / .25f), ease = 1f - (1f - k) * (1f - k);
            float w = 3 * CW + 2 * 12 + 36, h = CH + 70;
            var r = new Rect(Mathf.Round((vw - w) / 2f), Mathf.Round((vh - h) / 2f + 6 + (1f - ease) * 10f), w, h);
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11, "여관 사랑방 · 나그네 소문");
            CuteUI.LabelCenter(r.center.x, r.y + 18, picked < 0 ? "나그네 셋이 소문을 들려주겠대요. 하나만 골라 들을 수 있어요" : "들은 시세는 장부(L)에 적어 두었어요", CuteUI.Sub, true);

            for (int i = 0; i < cards.Count; i++)
            {
                float cx = r.x + 18 + i * (CW + 12), cy = r.y + 32;
                // 하나씩 차례로 놓인다
                float ap = Mathf.Clamp01((now - openedAt - i * .08f) / .22f);
                cy += Mathf.Round((1f - ap) * 14f);
                bool on = picked < 0 ? i == focus : i == picked;
                if (picked < 0 && on) cy -= 4f;
                var card = new Rect(cx, cy, CW, CH);
                var oc = GUI.color;
                float alpha = picked >= 0 && i != picked ? .45f : 1f;
                GUI.color = new Color(oc.r, oc.g, oc.b, oc.a * ap * alpha);
                // 뒤집기: 고른 카드는 0.3초 동안 가로로 접혔다 펼쳐진다
                bool face = picked == i && now - pickedAt > .15f;
                float sx = 1f;
                if (picked == i && now - pickedAt < .3f) sx = Mathf.Abs(Mathf.Cos((now - pickedAt) / .3f * Mathf.PI));
                var m = GUI.matrix;
                if (sx < .999f) { var pv = card.center; GUI.matrix = GUI.matrix * Matrix4x4.TRS(pv, Quaternion.identity, new Vector3(Mathf.Max(.02f, sx), 1f, 1f)) * Matrix4x4.TRS(-pv, Quaternion.identity, Vector3.one); }
                if (face) DrawFace(card, cards[i]); else DrawBack(card, cards[i], on, now);
                GUI.matrix = m;
                GUI.color = oc;
                if (picked < 0 && ap >= 1f)
                {
                    var e = Event.current;
                    if (e.type == EventType.MouseMove && card.Contains(e.mousePosition) && focus != i) focus = i;
                    if (e.type == EventType.MouseDown && e.button == 0 && card.Contains(e.mousePosition)) { focus = i; Pick(i); e.Use(); }
                }
            }

            // 아래 줄 (키 안내)
            float ky = r.yMax - 22;
            if (picked < 0)
            {
                float tw = CuteUI.KeycapWidth("← →") + 4 + CuteUI.Width("고르기", true) + 14 + CuteUI.KeycapWidth("Space") + 4 + CuteUI.Width("듣기", true) + 14 + CuteUI.KeycapWidth("Esc") + 4 + CuteUI.Width("그냥 자기", true);
                float kx = Mathf.Round(r.center.x - tw / 2f);
                kx += CuteUI.Keycap(kx, ky - 6.5f, "← →") + 4; kx = CuteUI.Label(kx, ky, "고르기", CuteUI.Sub, true) + 14;
                kx += CuteUI.Keycap(kx, ky - 6.5f, "Space") + 4; kx = CuteUI.Label(kx, ky, "듣기", CuteUI.Sub, true) + 14;
                kx += CuteUI.Keycap(kx, ky - 6.5f, "Esc") + 4; CuteUI.Label(kx, ky, "그냥 자기", CuteUI.Sub, true);
            }
            else if (now - pickedAt > .5f)
            {
                float tw = CuteUI.KeycapWidth("Space") + 4 + CuteUI.Width("잘 자요", true);
                float kx = Mathf.Round(r.center.x - tw / 2f);
                kx += CuteUI.Keycap(kx, ky - 6.5f, "Space") + 4; CuteUI.Label(kx, ky, "잘 자요", CuteUI.Sub, true);
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && r.Contains(Event.current.mousePosition)) { Event.current.Use(); Close(); }
            }
            if (Event.current.isMouse) Event.current.Use();
        }

        void DrawBack(Rect c, Rumor r, bool on, float now)
        {
            var col = CivColor.TryGetValue(r.civ, out var cc) ? cc : CuteUI.Interact;
            CuteUI.Box(c, 8, on ? CuteUI.White : CuteUI.Paper2, on ? CuteUI.Interact : CuteUI.Ink, on ? 2 : 1, 2);
            // 위 띠: 어느 지역 소문인지
            var band = new Rect(c.x + 5, c.y + 5, c.width - 10, 20);
            CuteUI.Box(band, 6, col, CuteUI.Ink, 1);
            CuteUI.LabelCenter(band.center.x, band.center.y, r.regionName + " 소문", CuteUI.White, true, true);
            // 나그네
            var face = new Rect(c.center.x - 33, c.y + 32, 66, 74);
            CuteUI.Box(face, 8, CuteUI.Paper, CuteUI.Line, 1);
            float bob = on ? Mathf.Round(Mathf.Sin(now * 3f)) : 0f;
            if (r.face != null) CuteUI.Sprite(r.face, new Rect(face.center.x - 24, face.y + 2 + bob, 48, 72));
            CuteUI.LabelCenter(c.center.x, c.y + 116, r.teller, CuteUI.Text, true, true);
            // 봉한 소문 (내용은 감춤)
            var seal = new Rect(c.x + 14, c.y + 128, c.width - 28, 30);
            CuteUI.Box(seal, 6, CuteUI.Paper, CuteUI.Line, 1);
            for (int i = 0; i < 3; i++) CuteUI.Fill(new Rect(seal.x + 8, seal.y + 8 + i * 6, seal.width - 16 - (i == 2 ? 24 : 0), 2), new Color32(0xD6, 0xCB, 0xC0, 255));
            CuteUI.Circle(seal.center.x - 7, seal.center.y - 7, 14, col, CuteUI.Ink, 1, 1);
            CuteUI.LabelCenter(seal.center.x, seal.center.y, "?", CuteUI.White, true, true);
        }

        void DrawFace(Rect c, Rumor r)
        {
            var col = CivColor.TryGetValue(r.civ, out var cc) ? cc : CuteUI.Interact;
            CuteUI.Box(c, 8, CuteUI.White, col, 2, 2);
            var band = new Rect(c.x + 5, c.y + 5, c.width - 10, 20);
            CuteUI.Box(band, 6, col, CuteUI.Ink, 1);
            CuteUI.LabelCenter(band.center.x, band.center.y, r.teller, CuteUI.White, true, true);
            // 소문 한마디
            CuteUI.Wrap(c.x + 9, c.y + 32, c.width - 18, "\"" + r.line + "\"", CuteUI.Text, true);
            // 물건 · 값
            var chip = new Rect(c.x + 8, c.yMax - 52, c.width - 16, 24);
            CuteUI.Box(chip, 6, r.sellHigh ? CuteUI.SellSoft : CuteUI.BuySoft, r.sellHigh ? CuteUI.Sell : CuteUI.Buy, 1);
            if (r.item != null && r.item.icon != null) CuteUI.Sprite(r.item.icon, new Rect(chip.x + 4, chip.y + 4, 16, 16));
            CuteUI.Label(chip.x + 23, chip.center.y, r.sellHigh ? "팔면" : "사면", CuteUI.Sub, true);
            CuteUI.LabelRight(chip.xMax - 6, chip.center.y, r.price + "냥", r.sellHigh ? CuteUI.Sell : CuteUI.Buy, false, true);
            // 장부에 적음 도장
            var st = new Rect(c.center.x - 34, c.yMax - 22, 68, 15);
            CuteUI.Box(st, 6, CuteUI.Paper2, CuteUI.Interact, 1);
            CuteUI.LabelCenter(st.center.x, st.center.y, "장부에 적음", CuteUI.Interact, true, true);
        }
    }
}
