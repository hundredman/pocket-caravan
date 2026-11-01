using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 돈·짐 서버 저장 (2026-10-30, feature/online 8단계). 접속키로 들어온 계정만.
    /// - 로그인은 게임 첫 화면에서 하지만, 고르기는 게임이 실제로 시작된 뒤(이어하기·새로 시작으로 맵에 들어간 뒤) 한다 (2026-10-31).
    ///   그래야 이 컴퓨터 저장을 불러온 다음에 서버 것과 견준다.
    /// - 들어오면: 서버에 저장된 게 없으면 지금 돈·짐을 올린다. 있는데 이 컴퓨터와 다르면 둘 중 하나를 고르는 창을 띄운다
    ///   ("서버 것 불러오기" / "이 컴퓨터 것으로 올리기"). 같으면 그대로.
    /// - 그다음 접속해 있는 동안 돈·짐이 바뀌면 3초에 한 번 올린다. 같은 실행에서 다시 붙었을 때는 묻지 않고 올린다.
    /// - 짐은 교역품만 다룬다. 퀘스트로 맡은 물건(questItem)과 의뢰 짐(contracts)은 이 컴퓨터 것을 그대로 둔다.
    ///   불러올 때 이 게임에 없는 물건(다른 브랜치 물건 등)은 빼고, 몇 개 뺐는지 알려 준다.
    /// - 불러온 돈·짐은 이 컴퓨터 저장(SaveSystem)에도 다음 저장 때 같이 들어간다.
    /// 공용 코드는 공개된 것만 쓴다(PlayerState.money · Inventory.Add/Remove/Items · GameManager.ItemById). 고치지 않는다.
    /// </summary>
    public class NetSave : MonoBehaviour
    {
        const float UploadEvery = 3f;

        [Serializable] class SaveOut { public string t = "save"; public int money; public List<string> items = new List<string>(); public List<int> counts = new List<int>(); public List<float> costs = new List<float>(); }

        NetClient.Msg waiting;      // 게임이 시작되기를 기다리는 접속 인사 (welcome)
        NetClient.SaveData offer;   // 고르는 창에 보여 줄 서버 저장
        bool showing;               // 고르는 창이 떠 있다 (다른 창이 열려 있으면 닫힐 때까지 기다린다)
        string decidedFor;          // 이번 실행에서 이미 정한 계정 이름
        bool live;                  // 올리기 중
        string lastSent;
        float nextUpload;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NetSave>() != null) return;
            var go = new GameObject("NetSave");
            DontDestroyOnLoad(go);
            go.AddComponent<NetSave>();
        }

        void OnEnable() => NetClient.Received += OnReceived;
        void OnDisable() => NetClient.Received -= OnReceived;

        static PlayerState Player => GameManager.I != null ? GameManager.I.Player : null;

        void OnReceived(NetClient.Msg m)
        {
            if (m.t != "welcome") return;
            live = false;
            lastSent = null;
            CloseOffer();
            waiting = m;
        }

        /// <summary>시작 화면이 닫히고 맵에 들어와 내 돈·짐이 정해졌다</summary>
        static bool GameStarted => !StartScreen.IsOpen && !NetLogin.IsOpen && MapRoot.Current != null && Player != null
                                   && !(GameManager.I != null && GameManager.I.IsTravelling);

        void Decide(NetClient.Msg m)
        {
            if (!m.account || Player == null) return;
            var net = NetClient.I;
            if (decidedFor == net.MyName || !m.hasSave || m.save == null) { Begin(m.hasSave ? null : "서버에 돈·짐을 처음 저장했어요"); return; }
            if (Signature(m.save.money, m.save.items, m.save.counts) == Signature(Snapshot())) { Begin(null); return; }
            offer = m.save;
            Notify.Show("서버에 저장된 돈·짐이 있어요. 창을 닫으면 고를 수 있어요", 4f);
        }

        void Begin(string note)
        {
            decidedFor = NetClient.I.MyName;
            live = true;
            nextUpload = 0f;
            if (note != null) NetChat.System(note);
        }

        void Update()
        {
            var net = NetClient.I;
            bool online = net != null && net.Status == NetClient.State.Online && net.IsAccount;
            if (!online) { live = false; waiting = null; if (offer != null) CloseOffer(); return; }
            if (waiting != null)
            {
                if (!GameStarted) return;
                var m = waiting;
                waiting = null;
                Decide(m);
            }
            if (offer != null && !showing && !UIState.Blocking && !UIState.JustClosed)
            {
                showing = true;
                UIState.Blocking = true;
            }
            if (!live || Player == null || Time.unscaledTime < nextUpload) return;
            nextUpload = Time.unscaledTime + UploadEvery;
            var s = Snapshot();
            string sig = Signature(s);
            if (sig == lastSent) return;
            lastSent = sig;
            net.Send(s);
        }

        /// <summary>지금 돈·짐 (교역품만)</summary>
        static SaveOut Snapshot()
        {
            var p = Player;
            var s = new SaveOut { money = p.money };
            foreach (var kv in p.cargo.Items)
            {
                if (kv.Key == null || kv.Key.questItem || kv.Value <= 0 || s.items.Count >= 200) continue;
                s.items.Add(kv.Key.id);
                s.counts.Add(kv.Value);
                s.costs.Add(Mathf.Round(p.cargo.TotalCost(kv.Key) * 100f) / 100f);
            }
            return s;
        }

        static string Signature(SaveOut s) => Signature(s.money, s.items, s.counts);

        /// <summary>돈과 물건·개수로 만든 비교용 글 (물건 순서는 상관없게 정렬)</summary>
        static string Signature(int money, List<string> items, List<int> counts)
        {
            var rows = new List<string>();
            for (int i = 0; i < items.Count && i < counts.Count; i++) rows.Add(items[i] + "=" + counts[i]);
            rows.Sort(string.CompareOrdinal);
            var b = new StringBuilder().Append(money);
            foreach (var r in rows) b.Append('|').Append(r);
            return b.ToString();
        }

        static int CountOf(List<int> counts) { int n = 0; foreach (var c in counts) n += c; return n; }

        /// <summary>서버 저장을 이 컴퓨터에 넣는다. 교역품만 바꾸고 맡은 물건은 남긴다</summary>
        void LoadOffer()
        {
            var p = Player;
            var gm = GameManager.I;
            if (p == null || gm == null || offer == null) return;
            var trade = new List<KeyValuePair<ItemData, int>>();
            foreach (var kv in p.cargo.Items) if (kv.Key != null && !kv.Key.questItem) trade.Add(kv);
            foreach (var kv in trade) p.cargo.Remove(kv.Key, kv.Value);

            int skipped = 0;
            for (int i = 0; i < offer.items.Count && i < offer.counts.Count; i++)
            {
                var item = gm.ItemById(offer.items[i]);
                int n = offer.counts[i];
                if (item == null || item.questItem || n <= 0) { skipped += Mathf.Max(0, n); continue; }
                float total = i < offer.costs.Count ? offer.costs[i] : 0f;
                p.cargo.Add(item, n, total / n);
            }
            p.money = offer.money;
            NetChat.System(skipped > 0 ? $"서버에 저장된 돈·짐을 불러왔어요 (이 게임에 없는 물건 {skipped}개는 뺐어요)" : "서버에 저장된 돈·짐을 불러왔어요");
            Notify.Show($"서버 저장을 불러왔어요 · {p.money}냥", 3f);
        }

        void CloseOffer()
        {
            if (offer == null) return;
            offer = null;
            if (showing) UIState.Release();
            showing = false;
        }

        void OnGUI()
        {
            if (offer == null || !showing || Player == null) return;
            UIStyle.Begin();
            GUI.depth = -12;
            float w = 320f, h = 150f;
            var r = new Rect((UIStyle.VirtualWidth - w) / 2f, (UIStyle.VirtualHeight - h) / 2f, w, h);
            GUI.Box(r, "", UIStyle.Box);
            float x = r.x + 14f, fw = w - 28f, y = r.y + 8f;
            GUI.Label(new Rect(r.x, y, w, 16f), "<b>서버에 저장된 돈·짐이 있어요</b>", UIStyle.CenterRich);
            y += 24f;
            var here = Snapshot();
            string when = offer.at > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(offer.at).ToLocalTime().ToString("M월 d일 HH:mm") : "?";
            GUI.Label(new Rect(x, y, fw, 16f), $"서버 ({when}): <color=#ffde59>{offer.money}냥</color> · 짐 {CountOf(offer.counts)}개", UIStyle.Rich);
            y += 18f;
            GUI.Label(new Rect(x, y, fw, 16f), $"이 컴퓨터: <color=#ffde59>{here.money}냥</color> · 짐 {CountOf(here.counts)}개", UIStyle.Rich);
            y += 20f;
            GUI.Label(new Rect(x, y, fw, 16f), $"<size={UIStyle.SmallSize}><color=#c9c3d6>고르지 않은 쪽은 사라져요. 맡은 물건·의뢰 짐은 그대로예요.</color></size>", UIStyle.Rich);
            y += 22f;
            var b = new Rect(x, y, (fw - 8f) / 2f, 22f);
            if (GUI.Button(b, "서버 것 불러오기", UIStyle.Button))
            {
                LoadOffer();
                CloseOffer();
                Begin(null);
            }
            if (GUI.Button(new Rect(b.xMax + 8f, y, b.width, 22f), "이 컴퓨터 것으로 올리기", UIStyle.Button))
            {
                CloseOffer();
                Begin("이 컴퓨터의 돈·짐을 서버에 올렸어요");
            }
        }
    }
}
