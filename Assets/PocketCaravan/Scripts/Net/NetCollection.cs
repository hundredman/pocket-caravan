using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 희귀품 · 칭호 (2026-11-18 윤채, 미니게임 기획 "미니게임을 해서 특별한 아이템을 얻게").
    /// - 희귀품: 경매(NetAuction) 낙찰 · 보물 상자(NetTreasure) · 진품 감정(NetAppraise)을 잘하면 얻는다. 짐칸이 아니라 수집함에 모인다.
    ///   만국 상인회가 정해진 값에 사 준다(이 창에서 팔기). 개수는 게임 메모(GetMemo "rare:아이디")라 저장 파일에 같이 남는다.
    /// - 칭호: 미니게임 기록 · 수집으로 얻는다. 하나를 골라 달면 온라인 이름표 위에 보인다.
    ///   이름표 표시(tag)는 "guild"(만국 상인회 표시) 뒤에 "__칭호아이디"를 붙여 보낸다 (서버는 그대로 전달만 한다).
    /// - 창: O(온라인) 창 "수집 · 칭호" 단추 · 채팅 /도감. 왼쪽 희귀품 여덟 칸, 오른쪽 칭호 목록, 위에 보물 소문 한 줄.
    /// </summary>
    public class NetCollection : MonoBehaviour
    {
        public class Rare { public string id, name, region, icon, desc; public int value; }

        /// <summary>희귀품 여덟 가지 (그림은 같은 지역 교역품 그림에 금빛 테). 서버 경매 목록(world.ts AUCTION_LOTS)과 아이디가 같다</summary>
        public static readonly Rare[] Rares =
        {
            new Rare { id = "gold_ginseng", name = "백 년 산삼", region = "고려", icon = "ginseng", value = 320, desc = "깊은 산에서 백 년을 자란 산삼. 의원들이 서로 사겠대요" },
            new Rare { id = "royal_najeon", name = "왕실 나전함", region = "고려", icon = "najeon", value = 360, desc = "자개가 별처럼 박힌 왕실의 함" },
            new Rare { id = "murano_master", name = "무라노 걸작 유리", region = "베네치아", icon = "venice_glass_art", value = 380, desc = "장인이 한 해에 딱 하나만 부는 유리" },
            new Rare { id = "queen_lace", name = "여왕의 레이스", region = "베네치아", icon = "venice_lace", value = 330, desc = "부라노에서 석 달 동안 뜬 레이스" },
            new Rare { id = "sultan_carpet", name = "술탄의 양탄자", region = "아라비아", icon = "arabia_carpet", value = 400, desc = "궁전 바닥에 깔던 붉은 양탄자" },
            new Rare { id = "moon_pearl", name = "달빛 진주", region = "아라비아", icon = "arabia_pearl", value = 370, desc = "보름달 밤에만 캔다는 진주" },
            new Rare { id = "sun_amber", name = "얼어붙은 햇빛", region = "북유럽", icon = "north_amber", value = 350, desc = "옛 벌레가 든 커다란 호박" },
            new Rare { id = "silver_fur", name = "은여우 털옷", region = "북유럽", icon = "north_furcoat", value = 340, desc = "눈보다 하얀 은여우 털옷" },
        };

        public class Title { public string id, label, how; public Func<bool> met; public Func<string> progress; }

        /// <summary>칭호 (얻으면 다시 잃지 않는다: 메모 "title:got:아이디")</summary>
        public static readonly Title[] Titles =
        {
            new Title { id = "guild", label = "만국 상인", how = "만국 상인회에 가입", met = () => !string.IsNullOrEmpty(NetGuild.Badge), progress = () => "" },
            new Title { id = "appraiser", label = "감정의 달인", how = "진품 감정 5번 맞히기", met = () => Stat(StatAppraise) >= 5, progress = () => $"{Mathf.Min(5, Stat(StatAppraise))}/5" },
            new Title { id = "auction", label = "경매왕", how = "경매 3번 낙찰", met = () => Stat(StatAuction) >= 3, progress = () => $"{Mathf.Min(3, Stat(StatAuction))}/3" },
            new Title { id = "hunter", label = "보물 사냥꾼", how = "보물 상자 3개 찾기", met = () => Stat(StatTreasure) >= 3, progress = () => $"{Mathf.Min(3, Stat(StatTreasure))}/3" },
            new Title { id = "collector", label = "수집가", how = "희귀품 5종 모으기", met = () => Kinds >= 5, progress = () => $"{Mathf.Min(5, Kinds)}/5" },
            new Title { id = "stacker", label = "짐 쌓기 장인", how = "짐 쌓기 8단", met = () => Memo("net:guild:stack:best") >= 8, progress = () => $"{Mathf.Min(8, Memo("net:guild:stack:best"))}/8" },
            new Title { id = "rich", label = "큰손", how = "돈 3,000냥 모으기", met = () => GameManager.I != null && GameManager.I.Player.money >= 3000, progress = () => "" },
        };

        public const string StatAppraise = "stat:appraise", StatAuction = "stat:auction", StatTreasure = "stat:treasure";
        const string SelKey = "title:sel";

        static NetCollection inst;
        bool open, wantOpen;
        int pickRare = -1;
        float lastCheck;
        Rect lastWindow;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NetCollection>() != null) return;
            var go = new GameObject("NetCollection");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<NetCollection>();
        }

        void Awake() => inst = this;
        void OnDisable() => Close();

        // ---------------- 수집 · 칭호 데이터 ----------------

        static GameManager GM => GameManager.I;
        static int Memo(string key) => GM == null ? 0 : Mathf.RoundToInt(GM.GetMemo(key, 0f));
        public static int Stat(string key) => Memo(key);
        public static void AddStat(string key) { if (GM != null) GM.SetMemo(key, GM.GetMemo(key, 0f) + 1f); }
        public static int Count(string id) => Memo("rare:" + id);
        public static int Kinds { get { int n = 0; foreach (var r in Rares) if (Count(r.id) > 0) n++; return n; } }
        public static Rare Find(string id) => Array.Find(Rares, r => r.id == id);
        public static Rare RandomRare() => Rares[UnityEngine.Random.Range(0, Rares.Length)];

        /// <summary>희귀품 하나를 수집함에 넣고 알린다 (why = 어디서 얻었는지, 알림 한 줄)</summary>
        public static void Give(string id, string why)
        {
            var r = Find(id);
            if (r == null || GM == null) return;
            GM.SetMemo("rare:" + id, Count(id) + 1);
            Sfx.Play("accept");
            Notify.Event($"희귀품 「{r.name}」을(를) 얻었어요! {why} · 수집함(/도감)", 5f);
            GM.SaveNow();
        }

        /// <summary>달고 있는 칭호 (없으면 null)</summary>
        public static Title Selected
        {
            get
            {
                int i = Memo(SelKey) - 1;
                return i >= 0 && i < Titles.Length && Has(Titles[i]) ? Titles[i] : null;
            }
        }

        public static bool Has(Title t) => t != null && GM != null && (GM.GetMemo("title:got:" + t.id, 0f) >= 1f || SafeMet(t));
        static bool SafeMet(Title t) { try { return t.met(); } catch { return false; } }

        /// <summary>서버에 보내는 이름표 표시: 만국 상인회 표시("guild") + "__칭호아이디"</summary>
        public static string Tag
        {
            get
            {
                string badge = NetGuild.Badge, title = Selected?.id;
                return string.IsNullOrEmpty(title) ? badge : badge + "__" + title;
            }
        }

        public static bool HasBadge(string tag) => !string.IsNullOrEmpty(tag) && (tag == NetGuild.BadgeId || tag.StartsWith(NetGuild.BadgeId + "__"));

        /// <summary>남의 이름표 표시에서 칭호 글 (없으면 null)</summary>
        public static string TitleOfTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return null;
            int i = tag.IndexOf("__", StringComparison.Ordinal);
            if (i < 0) return null;
            string id = tag.Substring(i + 2);
            var t = Array.Find(Titles, x => x.id == id);
            return t != null ? t.label : null;
        }

        void Update()
        {
            if (wantOpen && !UIState.Blocking && !UIState.JustClosed)
            {
                wantOpen = false;
                if (GM != null && UIState.TryOpen(this, Close)) { open = true; pickRare = -1; Sfx.Play("open"); }
            }
            if (open && InputReader.CancelPressed()) { Close(); return; }
            // 1초마다: 새로 얻은 칭호를 알린다
            if (GM == null || StartScreen.IsOpen || Time.unscaledTime - lastCheck < 1f) return;
            lastCheck = Time.unscaledTime;
            foreach (var t in Titles)
            {
                if (GM.GetMemo("title:got:" + t.id, 0f) >= 1f || !SafeMet(t)) continue;
                GM.SetMemo("title:got:" + t.id, 1f);
                if (t.id == "guild") continue;   // 상인회 가입은 그쪽 이야기가 따로 알린다
                Notify.Event($"칭호 「{t.label}」을(를) 얻었어요! 수집함(/도감)에서 달면 이름표에 보여요", 5f);
                Sfx.Play("accept");
            }
        }

        // ---------------- 창 ----------------

        public static bool IsOpen => inst != null && inst.open;

        /// <summary>수집함을 연다. 채팅·온라인 창이 닫힌 다음 프레임에 열린다 (순위표와 같은 방식)</summary>
        public static void Open() { if (inst != null && !inst.open) inst.wantOpen = true; }

        void Close()
        {
            if (!open) return;
            open = false;
            UIState.Release(this);
        }

        /// <summary>채팅 "/도감" · "/칭호" · "/수집". 편집기·개발 빌드에서는 "/시험 돈 · 보물 · 경매 · 수집"도 (미니게임 시험용)</summary>
        public static bool TryCommand(string word)
        {
            string w = word.Trim();
            if (w.StartsWith("시험"))
            {
                if (!InputReader.DevKeys || GM == null) return false;
                if (w.Contains("돈")) { GM.Player.money += 1000; NetChat.System("시험: 1,000냥을 더했어요"); }
                else if (w.Contains("보물")) NetTreasure.DevHere();
                else if (w.Contains("경매")) NetAuction.DevFast();
                else if (w.Contains("수집")) { foreach (var r in Rares) GM.SetMemo("rare:" + r.id, Count(r.id) + 1); NetChat.System("시험: 희귀품을 한 개씩 넣었어요"); }
                else NetChat.System("시험: /시험 돈 · /시험 보물 · /시험 경매 · /시험 수집");
                return true;
            }
            if (w != "도감" && w != "칭호" && w != "수집" && w != "수집함") return false;
            Open();
            return true;
        }

        void OnGUI()
        {
            if (!open || GM == null) return;
            UIStyle.Begin();
            GUI.depth = -10;
            float w = Mathf.Min(460, UIStyle.VirtualWidth - 16), h = Mathf.Min(282, UIStyle.VirtualHeight - 30);
            var r = lastWindow = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), Mathf.Round((UIStyle.VirtualHeight - h) / 2 + 6), w, h);
            CuteUI.Dim(); CuteUI.Window(r); CuteUI.TitlePlate(r.center.x, r.y - 11, "수집 · 칭호");
            CuteUI.Keycap(r.xMax - 32 - CuteUI.KeycapWidth("Esc") - 3, r.y - 3, "Esc");
            if (CuteUI.CloseButton(r.xMax - 32, r.y - 9)) { Close(); return; }

            // 보물 소문 한 줄
            string rumor = NetTreasure.Rumor;
            var rr = new Rect(r.x + 16, r.y + 18, w - 32, 16);
            CuteUI.Box(rr, 6, CuteUI.Paper2, CuteUI.Line, 1);
            CuteUI.Icon(CuteUI.Pic.Star4, rr.x + 6, rr.center.y - 3, 1, CuteUI.Deco);
            CuteUI.Label(rr.x + 16, rr.center.y, TradeLedgerUI.Fit(rumor, rr.width - 22, true, false), CuteUI.Text, true);

            float top = r.y + 42;
            DrawRares(new Rect(r.x + 16, top, 200, r.yMax - 14 - top));
            DrawTitles(new Rect(r.x + 228, top, w - 244, r.yMax - 14 - top));
        }

        void DrawRares(Rect a)
        {
            CuteUI.Label(a.x, a.y + 6, $"희귀품 {Kinds}/{Rares.Length}", CuteUI.Text, false, true);
            const float cell = 46, gap = 5;
            for (int i = 0; i < Rares.Length; i++)
            {
                var rare = Rares[i];
                int n = Count(rare.id);
                var c = new Rect(a.x + (i % 4) * (cell + gap), a.y + 16 + (i / 4) * (cell + gap), cell, cell);
                bool on = i == pickRare;
                CuteUI.Box(c, 6, n > 0 ? CuteUI.Paper : CuteUI.ZeroSoft, on ? CuteUI.Interact : n > 0 ? CuteUI.Deco : CuteUI.Line, on ? 2 : 1);
                RareIcon(rare, c.center.x - 16, c.center.y - 18, 2, n > 0);
                if (n > 1) CuteUI.LabelRight(c.xMax - 4, c.yMax - 7, "×" + n, CuteUI.Text, true, true);
                if (CuteUI.Hit(c, GUIContent.none, GUIStyle.none)) { pickRare = on ? -1 : i; Sfx.Play("open", .4f); }
            }
            float dy = a.y + 16 + 2 * (cell + gap) + 4;
            var d = new Rect(a.x, dy, a.width, a.yMax - dy);
            CuteUI.Box(d, 6, CuteUI.Paper2, CuteUI.Line, 1);
            if (pickRare < 0)
            {
                CuteUI.WrapWords(d.x + 8, d.y + 6, d.width - 16, "경매 낙찰 · 보물 상자 · 진품 감정으로 모아요. 칸을 누르면 자세히 보여요", CuteUI.Sub, true);
                return;
            }
            var p = Rares[pickRare];
            int have = Count(p.id);
            float nx = CuteUI.Label(d.x + 8, d.y + 10, have > 0 ? p.name : "???", CuteUI.Text, false, true);
            CuteUI.Label(nx + 6, d.y + 10.5f, p.region, CuteUI.Sub, true);
            CuteUI.WrapWords(d.x + 8, d.y + 18, d.width - 16, have > 0 ? p.desc : "아직 못 본 희귀품이에요", CuteUI.Sub, true);
            if (have > 0)
            {
                var sell = new Rect(d.x + 8, d.yMax - 26, d.width - 16, 20);
                if (CuteUI.SoftButton(sell, $"상인회에 팔기 (+{p.value}냥)"))
                {
                    GM.SetMemo("rare:" + p.id, have - 1);
                    GM.Player.money += p.value;
                    Sfx.Play("coin");
                    Notify.Show($"「{p.name}」을(를) 상인회에 {p.value}냥에 팔았어요", 3f);
                    GM.SaveNow();
                }
            }
        }

        void DrawTitles(Rect a)
        {
            var sel = Selected;
            CuteUI.Label(a.x, a.y + 6, "칭호", CuteUI.Text, false, true);
            CuteUI.LabelRight(a.xMax, a.y + 6, sel != null ? $"달고 있는 칭호 「{sel.label}」" : "칭호를 골라 달아 보세요", CuteUI.Sub, true);
            float y = a.y + 16;
            const float rowH = 22;
            for (int i = 0; i < Titles.Length; i++)
            {
                var t = Titles[i];
                bool got = Has(t), on = sel == t;
                var row = new Rect(a.x, y, a.width, rowH);
                CuteUI.Box(row, 6, on ? CuteUI.Paper : got ? CuteUI.Paper2 : CuteUI.ZeroSoft, on ? CuteUI.Interact : CuteUI.Line, on ? 2 : 1);
                CuteUI.Label(row.x + 8, row.center.y, got ? $"「{t.label}」" : t.label, got ? CuteUI.Text : CuteUI.Sub, true, got);
                string right = got ? (on ? "떼기" : "달기") : t.how + (string.IsNullOrEmpty(t.progress()) ? "" : " " + t.progress());
                if (got)
                {
                    var b = new Rect(row.xMax - 40, row.y + 3, 34, rowH - 6);
                    if (CuteUI.SoftButton(b, right))
                    {
                        GM.SetMemo(SelKey, on ? 0 : i + 1);
                        Sfx.Play("accept", .6f);
                        Notify.Show(on ? "칭호를 뗐어요" : $"칭호 「{t.label}」을(를) 달았어요 · 온라인 이름표에 보여요", 3f);
                    }
                }
                else CuteUI.LabelRight(row.xMax - 8, row.center.y, TradeLedgerUI.Fit(right, row.width - 90, true, false), CuteUI.Sub, true);
                y += rowH + 4;
            }
        }

        /// <summary>희귀품 그림: 같은 지역 교역품 그림 + 금빛 반짝임. have가 false면 어두운 그림자</summary>
        public static void RareIcon(Rare r, float x, float y, int scale, bool have = true)
        {
            var db = GM != null ? GM.Db : null;
            var item = db != null ? db.items.Find(i => i != null && i.id == r.icon) : null;
            var old = GUI.color;
            if (!have) GUI.color = new Color(.25f, .2f, .28f, old.a * .55f);
            if (item != null) TradeLedgerUI.ItemIcon(item, x, y, scale);
            else CuteUI.Icon(CuteUI.Pic.Star5, x + 4 * scale, y + 4 * scale, scale, CuteUI.Deco);
            GUI.color = old;
            if (have)
            {
                float tw = Mathf.Sin(Time.unscaledTime * 3f + x * .1f) > .3f ? 1f : 0f;
                if (tw > 0f) CuteUI.Icon(CuteUI.Pic.Star4, x + 16 * scale - 6, y - 2, 1, CuteUI.Deco);
            }
        }
    }
}
