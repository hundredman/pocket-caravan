using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 만국 경매 (2026-11-18 윤채, 미니게임 기획 "여럿이 · 희귀품 · 돈 쓰는 곳").
    /// 만국 상인회 마당 경매대에서 Space. 희귀품(NetCollection) 하나가 올라오고, 값을 불러 가장 높이 부른 사람이 2분 뒤 가져간다.
    /// 누가 부르면 남은 시간이 15초 아래일 때 15초로 늘어난다. 한 번에 10냥 · 50냥 더.
    /// - 온라인(서버 13단계 "games"): 서버가 물건·값·최고가를 들고 있고 접속한 사람 모두가 같이 부른다 (world.ts auc · bid · aucEnd).
    ///   낙찰되면 그 사람 게임이 돈을 내고 수집함에 넣는다(돈이 모자라면 무효). 경매가 끝나면 45초 쉬었다가 다음 물건.
    /// - 혼자(접속 안 함 · 예전 서버): 상인회 단골 셋(파리드 · 비요른 · 길보)이 저마다 속으로 정한 값까지 따라 부른다.
    /// 낙찰 세 번이면 칭호 「경매왕」.
    /// </summary>
    public class NetAuction : MonoBehaviour
    {
        const int Step = 10, Big = 50;
        const float LocalLength = 75f, Bump = 15f, Rest = 45f;

        [Serializable] class AucOut { public string t = "auc"; }
        [Serializable] class BidOut { public string t = "bid"; public int price; }

        static NetAuction inst;
        bool open;
        // 지금 경매 (온라인이면 서버가 알려 준 것, 혼자면 이 게임이 정한 것)
        string lot = "", top = "";
        int price;
        float endsAt, nextAt;
        bool mineTop;
        readonly List<string> log = new List<string>();
        string why; float whyAt = -9f;
        // 혼자 경매: 단골 상인 셋
        class Npc { public string name; public int max; public float nextAt; }
        readonly List<Npc> npcs = new List<Npc>();
        bool local;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NetAuction>() != null) return;
            var go = new GameObject("NetAuction");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<NetAuction>();
        }

        void Awake() { inst = this; NetClient.Received += OnMsg; }
        void OnDestroy() => NetClient.Received -= OnMsg;
        void OnDisable() => Close();

        static GameManager GM => GameManager.I;
        static NetClient Net => NetClient.I;
        static bool Online => Net != null && Net.Status == NetClient.State.Online && Net.ServerGames;
        static bool AtBazaar => MapRoot.Current != null && MapRoot.Current.mapId == NetGuild.Bazaar;
        float Left => Mathf.Max(0f, endsAt - Time.unscaledTime);
        int MinBid => string.IsNullOrEmpty(top) ? price : price + Step;

        /// <summary>경매대 (만국 상인회 마당)</summary>
        public static void Open()
        {
            if (inst == null || inst.open || GM == null || !UIState.TryOpen(inst, inst.Close)) return;
            inst.open = true;
            inst.local = !Online;
            if (inst.local) inst.EnsureLocal();
            else Net.Send(new AucOut());
            Sfx.Play("open");
        }

        void Close()
        {
            if (!open) return;
            open = false;
            UIState.Release(this);
        }

        // ---------------- 온라인 ----------------

        void OnMsg(NetClient.Msg m)
        {
            if (m.t == "auc")
            {
                bool fresh = !string.IsNullOrEmpty(m.lot) && m.lot != lot;
                if (!string.IsNullOrEmpty(m.top) && (m.top != top || m.price != price)) Log($"{m.top} {m.price}냥");
                if (fresh) { log.Clear(); Log($"새 물건 · 시작가 {m.price}냥"); }
                local = false;
                lot = m.lot ?? ""; top = m.top ?? ""; price = m.price;
                endsAt = Time.unscaledTime + m.left / 1000f;
                nextAt = Time.unscaledTime + m.next / 1000f;
                mineTop = Net != null && top == Net.MyName;
                if (!string.IsNullOrEmpty(m.why)) { why = m.why; whyAt = Time.unscaledTime; }
                if (fresh && !open && AtBazaar && NetCollection.Find(lot) is NetCollection.Rare r) Notify.Event($"경매대에 「{r.name}」이(가) 올라왔어요! 시작가 {price}냥", 4f);
                return;
            }
            if (m.t == "aucEnd")
            {
                var r = NetCollection.Find(m.lot);
                string name = r != null ? r.name : m.lot;
                bool me = Net != null && !string.IsNullOrEmpty(m.topId) && m.topId == Net.MyId;
                if (me) Win(m.lot, m.price);
                else if (!string.IsNullOrEmpty(m.top) && (open || AtBazaar)) Notify.Show($"{m.top} 님이 「{name}」을(를) {m.price}냥에 낙찰받았어요", 4f);
                if (lot == m.lot) { Log(string.IsNullOrEmpty(m.top) ? "유찰 (아무도 안 불렀어요)" : $"낙찰 · {m.top} {m.price}냥"); lot = ""; top = ""; nextAt = Time.unscaledTime + Rest; }
            }
        }

        // ---------------- 혼자 경매 ----------------

        void EnsureLocal()
        {
            if (!string.IsNullOrEmpty(lot) || Time.unscaledTime < nextAt) return;
            var r = NetCollection.RandomRare();
            lot = r.id; top = ""; mineTop = false;
            price = Mathf.RoundToInt(r.value * .4f / 10f) * 10;
            endsAt = Time.unscaledTime + LocalLength;
            log.Clear(); Log($"새 물건 · 시작가 {price}냥");
            npcs.Clear();
            foreach (var n in new[] { "대상 파리드", "북쪽 상인 비요른", "보부상 길보" })
                npcs.Add(new Npc { name = n, max = Mathf.RoundToInt(r.value * UnityEngine.Random.Range(.55f, 1.05f) / 10f) * 10, nextAt = Time.unscaledTime + UnityEngine.Random.Range(3f, 9f) });
        }

        void TickLocal()
        {
            if (string.IsNullOrEmpty(lot))
            {
                if (open) EnsureLocal();
                return;
            }
            if (Left <= 0f)
            {
                var r = NetCollection.Find(lot);
                if (mineTop) Win(lot, price);
                else if (!string.IsNullOrEmpty(top)) Notify.Show($"{top}이(가) 「{r?.name}」을(를) {price}냥에 가져갔어요", 3.5f);
                Log(string.IsNullOrEmpty(top) ? "유찰" : $"낙찰 · {top} {price}냥");
                lot = ""; top = ""; mineTop = false;
                nextAt = Time.unscaledTime + 8f;   // 혼자일 때는 잠깐만 쉰다
                return;
            }
            foreach (var n in npcs)
            {
                if (Time.unscaledTime < n.nextAt || top == n.name || MinBid > n.max) continue;
                int bid = Mathf.Min(n.max, MinBid + UnityEngine.Random.Range(0, 3) * Step);
                Place(n.name, bid, false);
                n.nextAt = Time.unscaledTime + UnityEngine.Random.Range(2.5f, 6f);
                break;
            }
        }

        void Place(string who, int bid, bool me)
        {
            price = bid; top = who; mineTop = me;
            endsAt = Mathf.Max(endsAt, Time.unscaledTime + Bump);
            Log($"{who} {bid}냥");
            if (open) Sfx.Play(me ? "coin" : "move", .5f);
        }

        // ---------------- 공통 ----------------

        void Win(string id, int paid)
        {
            var r = NetCollection.Find(id);
            if (r == null || GM == null) return;
            if (GM.Player.money < paid) { Notify.Event($"돈이 모자라 「{r.name}」 낙찰이 무효가 됐어요", 4f); return; }
            GM.Player.money -= paid;
            NetCollection.AddStat(NetCollection.StatAuction);
            NetCollection.Give(id, $"경매에서 {paid}냥에 낙찰");
        }

        void Log(string line) { log.Add(line); while (log.Count > 4) log.RemoveAt(0); }

        /// <summary>시험용 (채팅 /시험 경매): 혼자 경매를 10초 뒤에 끝낸다 (물건이 없으면 바로 올린다). 온라인 경매는 서버 시간이라 못 줄인다</summary>
        public static void DevFast()
        {
            if (inst == null) return;
            if (!inst.local && Online) { NetChat.System("시험: 온라인 경매는 서버 시간대로 가요 (2분)"); return; }
            inst.local = true;
            if (string.IsNullOrEmpty(inst.lot)) { inst.nextAt = 0f; inst.EnsureLocal(); }
            inst.endsAt = Time.unscaledTime + 10f;
            NetChat.System("시험: 혼자 경매가 10초 뒤에 끝나요 (경매대에서 보세요)");
        }

        void Bid(int extra)
        {
            int bid = MinBid + extra;
            if (GM.Player.money < bid) { why = $"돈이 모자라요 ({bid}냥)"; whyAt = Time.unscaledTime; Sfx.Play("deny", .55f); return; }
            if (mineTop) { why = "이미 가장 높이 불렀어요"; whyAt = Time.unscaledTime; return; }
            if (local) Place("나", bid, true);
            else { Net.Send(new BidOut { price = bid }); Sfx.Play("coin", .5f); }
        }

        void Update()
        {
            if (local || !Online) TickLocal();
            if (!open) return;
            if (InputReader.CancelPressed()) { Close(); return; }
            // 온라인 접속이 끊기면 혼자 경매로
            if (!local && !Online) { local = true; lot = ""; EnsureLocal(); }
        }

        void OnGUI()
        {
            if (!open || GM == null) return;
            UIStyle.Begin();
            GUI.depth = -10;
            float w = Mathf.Min(360, UIStyle.VirtualWidth - 16), h = 262;
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), Mathf.Round((UIStyle.VirtualHeight - h) / 2 + 6), w, h);
            CuteUI.Dim(); CuteUI.Window(r); CuteUI.TitlePlate(r.center.x, r.y - 11, "만국 경매");
            CuteUI.Keycap(r.xMax - 32 - CuteUI.KeycapWidth("Esc") - 3, r.y - 3, "Esc");
            if (CuteUI.CloseButton(r.xMax - 32, r.y - 9)) { Close(); return; }
            CuteUI.LabelRight(r.xMax - 16, r.y + 20, local ? "혼자 경매 · 상인회 단골들과" : $"온라인 경매 · 접속 {Net?.OnlineCount ?? 0}명", CuteUI.Sub, true);
            CuteUI.Label(r.x + 16, r.y + 20, $"내 돈 {GM.Player.money:N0}냥", CuteUI.Text, true, true);

            var rare = NetCollection.Find(lot);
            if (rare == null)
            {
                float wait = Mathf.Max(0f, nextAt - Time.unscaledTime);
                CuteUI.LabelCenter(r.center.x, r.y + 92, "지금은 쉬는 시간이에요", CuteUI.Text, false, true);
                CuteUI.LabelCenter(r.center.x, r.y + 110, wait > 0f ? $"다음 물건까지 {Mathf.CeilToInt(wait)}초" : "곧 다음 물건이 올라와요", CuteUI.Sub, true);
                if (!local && wait <= 0f && Time.frameCount % 120 == 0) Net.Send(new AucOut());
                DrawLog(r);
                return;
            }

            // 물건
            var card = new Rect(r.x + 16, r.y + 32, w - 32, 66);
            CuteUI.Box(card, 8, CuteUI.Paper2, CuteUI.Deco, 2);
            NetCollection.RareIcon(rare, card.x + 10, card.y + 9, 3);
            float tx = card.x + 70;
            float nx = CuteUI.Label(tx, card.y + 14, rare.name, CuteUI.Text, false, true);
            CuteUI.Label(nx + 6, card.y + 14.5f, rare.region, CuteUI.Sub, true);
            CuteUI.WrapWords(tx, card.y + 22, card.xMax - tx - 8, rare.desc, CuteUI.Sub, true);
            CuteUI.Label(tx, card.yMax - 9, $"상인회 매입가 {rare.value}냥", CuteUI.Interact, true);

            // 지금 값 · 최고가 · 남은 시간
            float y = card.yMax + 8;
            CuteUI.Label(r.x + 16, y + 10, "지금 값", CuteUI.Sub, true);
            var m = GUI.matrix;
            CuteUI.ScaleAround(2f, new Vector2(r.x + 60, y + 10));
            CuteUI.Label(r.x + 60, y + 10, $"{price}냥", mineTop ? CuteUI.Up : CuteUI.Text, false, true);
            GUI.matrix = m;
            string who = string.IsNullOrEmpty(top) ? "아직 아무도 안 불렀어요" : mineTop ? "내가 가장 높아요!" : $"{top} 님이 가장 높아요";
            CuteUI.LabelRight(r.xMax - 16, y + 10, who, mineTop ? CuteUI.Up : CuteUI.Text, true, mineTop);
            float left = Left;
            var bar = new Rect(r.x + 16, y + 26, w - 32, 6);
            CuteUI.Bar(bar, Mathf.Clamp01(left / (local ? LocalLength : 120f)), left > 15f ? CuteUI.Interact : CuteUI.Alert, CuteUI.InteractHi);
            CuteUI.LabelRight(r.xMax - 16, bar.yMax + 7, $"{Mathf.CeilToInt(left)}초 남음", left > 15f ? CuteUI.Sub : CuteUI.Down, true);

            // 부르기
            float by = bar.yMax + 18, bw = (w - 32 - 8) / 2f;
            if (CuteUI.Button(new Rect(r.x + 16, by, bw, 24), $"{MinBid}냥 부르기")) Bid(0);
            if (CuteUI.SoftButton(new Rect(r.x + 16 + bw + 8, by, bw, 24), $"{MinBid + Big}냥 크게 부르기")) Bid(Big);
            if (!string.IsNullOrEmpty(why) && Time.unscaledTime - whyAt < 2.5f) CuteUI.LabelCenter(r.center.x, by + 32, why, CuteUI.Down, true, true);
            DrawLog(r);
        }

        void DrawLog(Rect r)
        {
            float y = r.yMax - 14 - log.Count * 12;
            for (int i = 0; i < log.Count; i++)
                CuteUI.Label(r.x + 16, y + i * 12 + 6, (i == log.Count - 1 ? "· " : "  ") + log[i], i == log.Count - 1 ? CuteUI.Text : CuteUI.Sub, true);
        }
    }
}
