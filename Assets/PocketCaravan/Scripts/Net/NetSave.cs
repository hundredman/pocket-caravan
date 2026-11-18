using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 서버 저장. 접속키로 들어온 계정만.
    ///
    /// [게임 저장 통째로] (2026-11-07, 서버 10단계) 서버가 받는다고 하면(welcome.fullOk)
    /// - 이 컴퓨터 저장 파일(SaveSystem.CurrentPath)이 바뀔 때마다 압축해서 올린다 (2초마다 살피고, 6초에 한 번까지).
    ///   수레·창고·평판·퀘스트·흥정 기록까지 다 들어간다. 게임 저장 코드는 고치지 않고 파일만 읽는다.
    /// - 로그인했을 때 서버 저장과 이 컴퓨터 저장이 다르면:
    ///   이 컴퓨터 것이 더 최근이면 그냥 올리고, 서버 것이 더 최근이면(다른 컴퓨터에서 했다) 시작 화면 위에 고르는 창을 띄운다.
    ///   "서버 것"을 고르면 받아서 이 컴퓨터 저장으로 쓰고(예전 파일은 SaveSystem이 .bak으로 남긴다)
    ///   시작 화면을 "이어하기"부터 다시 연다 (GameManager.UseSaveFromServer).
    /// - 게임 중에 처음 로그인했는데 서버 것이 더 최근이면 덮어쓰지 않고 알려만 준다 (다시 켜서 로그인하면 고를 수 있다).
    /// - 지역 시험 세션(전용 저장 칸)은 올리지 않는다.
    ///
    /// [돈·짐] (2026-10-30, 8단계) 예전 게임(통째 저장을 모르는 버전)도 볼 수 있게 돈·짐도 계속 올린다.
    /// - 서버가 통째 저장을 모르면 예전처럼: 게임이 시작된 뒤 서버 돈·짐과 이 컴퓨터가 다르면 둘 중 하나를 고르는 창을 띄운다.
    /// - 짐은 교역품만 다룬다. 퀘스트로 맡은 물건(questItem)과 의뢰 짐(contracts)은 이 컴퓨터 것을 그대로 둔다.
    ///   불러올 때 이 게임에 없는 물건(다른 브랜치 물건 등)은 빼고, 몇 개 뺐는지 알려 준다.
    ///
    /// 고르는 창은 11/07 UI B안 (종이 창 + 카드 두 장, 고른 쪽 청록). 공용 코드는 공개된 것만 쓴다.
    /// </summary>
    public class NetSave : MonoBehaviour
    {
        const float UploadEvery = 3f, FullCheckEvery = 2f, FullSendGap = 6f, FetchWait = 10f;
        const int FullZipMax = 120000;   // 서버 FULL_ZIP_MAX와 같게

        [Serializable] class SaveOut { public string t = "save"; public int money; public List<string> items = new List<string>(); public List<int> counts = new List<int>(); public List<float> costs = new List<float>(); }
        [Serializable] class FullOut { public string t = "full"; public long at; public string h; public string zip; public int day; public int money; public string map; public string origin; }
        [Serializable] class GetFullOut { public string t = "getFull"; }

        /// <summary>이 컴퓨터 저장 파일 (at: 파일을 쓴 시각 ms)</summary>
        class LocalSave { public string json, hash, map, origin; public long at; public int day, money; }

        // 돈·짐 (8단계)
        NetClient.Msg waiting;      // 게임이 시작되기를 기다리는 접속 인사 (welcome)
        NetClient.SaveData offer;   // 예전 서버: 고르는 창에 보여 줄 서버 돈·짐
        string decidedFor;          // 이번 실행에서 이미 정한 계정 이름
        bool live;                  // 돈·짐 올리기 중
        string lastSent;
        float nextUpload;

        // 게임 저장 통째로 (10단계)
        NetClient.Msg waitingFull;      // 접속 인사. 다음 Update에서 정한다
        string fullDecidedFor;
        bool fullLive;                  // 저장 파일이 바뀌면 올린다
        string fullSentHash;            // 서버에 있다고 아는 저장 글의 해시
        long fullSeenWrite;             // 마지막으로 살핀 저장 파일의 쓴 시각 (ticks)
        float nextFullCheck, nextFullSend;
        NetClient.FullInfo fullOffer;   // 고르는 창: 서버 게임 저장
        LocalSave localOffer;           // 고르는 창: 이 컴퓨터 게임 저장 (없으면 null)
        bool fetching;                  // 서버 게임 저장을 받는 중
        // 11/14 이 컴퓨터 저장이 다른 계정(또는 누구 것인지 모르는) 것: 잠근 동안은 무엇도 올리지 않는다.
        //   풀리는 때: 서버 저장을 받아 왔거나, 서버 저장이 없는 계정이 새로 시작했을 때 (NewGameStarted)
        static bool foreignLocked, foreignServerHas;
        float fetchUntil;
        string fullNote;                // 처음 올라가면 채팅에 남길 말
        bool warnedBig;

        // 고르는 창 (돈·짐 / 게임 저장 같이 씀)
        bool showing;
        int pickIdx;                    // 0 = 서버 것, 1 = 이 컴퓨터 것
        Vector2 lastMove = Vector2.one;

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

        bool Offering => offer != null || fullOffer != null;

        void OnReceived(NetClient.Msg m)
        {
            if (SaveSystem.UsingSeparateSlot) return;
            switch (m.t)
            {
                case "welcome":
                    live = false;
                    lastSent = null;
                    fullLive = false;
                    CloseOffer();
                    waiting = m;
                    waitingFull = m.fullOk ? m : null;
                    break;
                case "fullSaved":
                    if (fullNote != null) { NetChat.System(fullNote); fullNote = null; }
                    break;
                case "fullData":
                    if (fetching) GotServerSave(m);
                    break;
            }
        }

        /// <summary>시작 화면이 닫히고 맵에 들어와 내 돈·짐이 정해졌다</summary>
        static bool GameStarted => !StartScreen.IsOpen && !NetLogin.IsOpen && MapRoot.Current != null && Player != null
                                   && !(GameManager.I != null && GameManager.I.IsTravelling);

        void Update()
        {
            // Region/editor fixtures can still connect for multiplayer tests, but
            // neither full saves nor the legacy money/cargo summary belong to the account.
            if (SaveSystem.UsingSeparateSlot)
            {
                live = false; fullLive = false; waiting = null; waitingFull = null;
                if (Offering) CloseOffer();
                return;
            }
            var net = NetClient.I;
            bool online = net != null && net.Status == NetClient.State.Online && net.IsAccount;
            if (!online) { live = false; fullLive = false; waiting = null; waitingFull = null; if (Offering) CloseOffer(); return; }

            if (waitingFull != null) { var m = waitingFull; waitingFull = null; DecideFull(m); }
            if (waiting != null && GameStarted) { var m = waiting; waiting = null; Decide(m); }

            if (Offering && !showing) TryShow();
            if (showing) Keys();
            if (fetching && Time.unscaledTime > fetchUntil) { Notify.Show("서버 게임 저장을 받지 못했어요. 이 컴퓨터 저장을 써요", 4f); UseLocal(); }

            if (fullLive) FullTick(net);

            if (!live || Player == null || Time.unscaledTime < nextUpload) return;
            nextUpload = Time.unscaledTime + UploadEvery;
            var s = Snapshot();
            string sig = Signature(s);
            if (sig == lastSent) return;
            lastSent = sig;
            net.Send(s);
        }

        // ---------------- 게임 저장 통째로 ----------------

        void DecideFull(NetClient.Msg m)
        {
            if (!m.account || SaveSystem.UsingSeparateSlot) return;
            var server = m.full != null && !string.IsNullOrEmpty(m.full.h) ? m.full : null;
            if (fullDecidedFor == NetClient.I.MyName) { StartFull(null, server?.h); return; }   // 같은 실행에서 다시 붙었다: 묻지 않는다
            var local = ReadLocal();
            // 11/14 은샘(UT "처음 계정인데 이어하기가 눌린다"): 저장은 컴퓨터에 하나라, 한 컴퓨터를 돌려 쓰면 앞사람 저장이
            //   새 계정의 "이어하기"가 되고 그 계정 서버 저장으로 올라갔다. 이 컴퓨터 저장에 마지막으로 올린 계정을 적어 두고(.account),
            //   다른 계정 것이면 올리지도 이어하지도 않는다. 누구 것인지 모르는 옛 저장은: 새 계정이면 남의 것으로, 서버 저장이 있으면 고르게 한다
            string me = NetClient.I.MyName, owner = ReadOwner();
            foreignLocked = false;   // 계정마다 새로 정한다
            bool same = local != null && server != null && local.hash == server.h;
            bool foreign = local != null && !same && owner != me && (owner != null || server == null);
            if (same) WriteOwner(me);
            if (foreign)
            {
                foreignLocked = true; foreignServerHas = server != null;
                if (server == null)
                {
                    if (ForgetForeign(owner)) NetChat.System("이 컴퓨터의 게임 저장은 다른 사람 것이라 새로 시작해요 (그 저장은 따로 남겨 두었어요)");
                    else NetChat.System("이 컴퓨터 게임 저장은 다른 계정 것이라 서버에 올리지 않아요");
                    StartFull(null, null);   // 잠금이 풀리기 전(새로 시작 전)에는 올리지 않는다
                    return;
                }
                if (StartScreen.IsOpen) { BackupForeign(owner); fullOffer = server; localOffer = null; fullDecidedFor = me; ChooseServer(); return; }   // 이 계정 서버 저장을 불러온다 (앞사람 파일은 옆에 남긴다)
                fullDecidedFor = me;   // 이미 게임 중: 덮어쓰지도 올리지도 않는다 (잠금은 이번 실행 동안 유지)
                NetChat.System("이 컴퓨터 게임 저장은 다른 계정 것이라 서버에 올리지 않아요. 게임을 다시 켜고 로그인하면 이 계정 저장을 불러와요");
                return;
            }
            if (server == null) { StartFull(local != null ? "이 컴퓨터 게임 저장을 서버에 처음 올렸어요" : null, null); return; }
            if (same || (owner == me && local != null && local.at >= server.at - 2000)) { StartFull(null, server.h); return; }   // 같거나, 내 것이고 이 컴퓨터 것이 더 최근
            if (local != null && owner == null && local.at >= server.at - 2000 && !StartScreen.IsOpen)
            {
                // 누구 것인지 모르는 저장으로 이미 게임 중: 서버 저장을 덮지 않는다 (다시 켜서 로그인하면 고르는 창)
                fullDecidedFor = me; foreignLocked = true;   // 같은 실행에서 다시 붙어도 올리지 않게
                NetChat.System("이 컴퓨터 게임 저장이 이 계정 것인지 몰라서 서버에 올리지 않아요. 게임을 다시 켜고 로그인하면 고를 수 있어요");
                return;
            }
            // 서버 것이 더 최근이다 (다른 컴퓨터에서 했다)
            if (!StartScreen.IsOpen)
            {
                fullDecidedFor = NetClient.I.MyName;
                foreignLocked = true;   // 11/14 다시 붙어도 더 오래된 이 컴퓨터 저장이 서버 것을 덮지 않게 (이번 실행 동안)
                NetChat.System($"서버에 더 최근 게임 저장이 있어요 ({When(server.at)} · {server.day}일째). 게임을 다시 켜고 로그인하면 고를 수 있어요");
                return;
            }
            fullOffer = server;
            localOffer = local;
            pickIdx = 0;
        }

        void StartFull(string note, string knownHash)
        {
            fullDecidedFor = NetClient.I.MyName;
            fullLive = true;
            fullSentHash = knownHash;
            fullSeenWrite = 0;
            nextFullCheck = 0f;
            fullNote = note;
        }

        /// <summary>저장 파일이 바뀌었으면 올린다</summary>
        void FullTick(NetClient net)
        {
            if (Time.unscaledTime < nextFullCheck || SaveSystem.UsingSeparateSlot) return;
            nextFullCheck = Time.unscaledTime + FullCheckEvery;
            long write;
            try
            {
                string path = SaveSystem.CurrentPath;
                if (!File.Exists(path)) return;
                write = File.GetLastWriteTimeUtc(path).Ticks;
            }
            catch (Exception) { return; }
            if (write == fullSeenWrite || Time.unscaledTime < nextFullSend) return;
            var local = ReadLocal();
            if (local == null) return;   // 게임이 쓰는 중이었다: 다음에
            fullSeenWrite = write;
            if (foreignLocked || local.hash == fullSentHash) return;   // 11/14 다른 계정 저장은 올리지 않는다
            string zip = Zip(local.json);
            if (zip.Length > FullZipMax)
            {
                if (!warnedBig) { warnedBig = true; NetChat.System("게임 저장이 너무 커서 서버에 올리지 못했어요"); }
                return;
            }
            net.Send(new FullOut { at = local.at, h = local.hash, zip = zip, day = local.day, money = local.money, map = local.map ?? "", origin = local.origin ?? "" });
            fullSentHash = local.hash;
            WriteOwner(net.MyName);   // 11/14 이 컴퓨터 저장 = 이 계정 것
            nextFullSend = Time.unscaledTime + FullSendGap;
        }

        void ChooseServer()
        {
            fetching = true;
            fetchUntil = Time.unscaledTime + FetchWait;
            NetClient.I.Send(new GetFullOut());
        }

        void GotServerSave(NetClient.Msg m)
        {
            fetching = false;
            SaveData d = null;
            try { if (!string.IsNullOrEmpty(m.zip)) d = JsonUtility.FromJson<SaveData>(Unzip(m.zip)); }
            catch (Exception) { d = null; }
            var gm = GameManager.I;
            if (d == null || gm == null || !gm.UseSaveFromServer(d))
            {
                Notify.Show("서버 게임 저장을 불러오지 못했어요. 이 컴퓨터 저장을 써요", 4f);
                UseLocal();
                return;
            }
            CloseOffer();
            var now = ReadLocal();
            WriteOwner(NetClient.I.MyName);   // 11/14 받아 온 서버 저장 = 이 계정 것
            foreignLocked = false;
            StartFull(null, now != null ? now.hash : m.h);
            NetChat.System($"서버 게임 저장을 불러왔어요 ({d.day}일째 · {d.money}냥). 이 컴퓨터 저장은 .bak 파일로 남겼어요");
            Notify.Show($"서버 게임 저장을 불러왔어요 · {d.day}일째 · {d.money}냥", 3.5f);
        }

        /// <summary>이 컴퓨터 저장을 쓴다: 다음 확인 때 올린다 (저장이 없으면 새 게임이 저장될 때 올라간다)</summary>
        void UseLocal()
        {
            string serverHash = fullOffer?.h;
            bool hasLocal = localOffer != null;
            CloseOffer();
            if (foreignLocked)
            {
                // 11/14 이 컴퓨터 것은 남의 것인데 이 계정 서버 저장을 못 받았다: 잠근 채로 둔다 (새로 시작해도 서버 저장을 덮지 않게)
                CloseOffer();
                fullDecidedFor = NetClient.I.MyName;
                ForgetForeign(ReadOwner());   // 남의 저장을 이어서 하지 않게 "새로 시작"부터 (서버 저장이 있는 계정이라 새로 시작해도 잠금은 그대로)
                NetChat.System("이 계정 서버 저장을 받지 못했어요. 이 컴퓨터 저장은 다른 계정 것이라 올리지 않아요. 게임을 다시 켜고 로그인해 주세요");
                return;
            }
            StartFull(hasLocal ? "이 컴퓨터 게임 저장을 서버에 올렸어요" : null, serverHash);
        }

        // 11/14 이 컴퓨터 저장을 마지막으로 서버에 올린(또는 서버에서 받아 온) 계정 이름. 저장 파일 옆 .account 파일
        static string OwnerPath => SaveSystem.CurrentPath + ".account";
        static string ReadOwner()
        {
            try { return File.Exists(OwnerPath) ? File.ReadAllText(OwnerPath).Trim() : null; }
            catch (Exception) { return null; }
        }
        static void WriteOwner(string name)
        {
            if (string.IsNullOrEmpty(name) || SaveSystem.UsingSeparateSlot) return;
            try { if (ReadOwner() != name) File.WriteAllText(OwnerPath, name); } catch (Exception) { }
        }
        /// <summary>11/14 다른 계정 저장: 파일을 옆에 복사해 두고(.other-계정) 시작 화면을 "새로 시작"부터 연다</summary>
        static bool ForgetForeign(string owner)
        {
            if (GameManager.I == null || !StartScreen.IsOpen) return false;
            BackupForeign(owner);
            if (!GameManager.I.ForgetLocalSave()) return false;
            ClearOwner();
            return true;
        }

        /// <summary>
        /// 11/14 새로 시작(GameManager.BeginNewGame): 이 컴퓨터 저장의 주인 표시를 지운다. 새 게임이 서버에 올라갈 때 FullTick이 다시 적는다
        /// (새 게임이 저장되기 전에 꺼져도 앞사람 파일에 내 이름이 붙지 않게).
        /// 남의 저장을 숨기고 새로 시작한 계정은 잠금을 푼다. 단 그 계정 서버 저장을 못 받은 채라면 덮지 않게 잠근 채로 둔다
        /// </summary>
        public static void NewGameStarted()
        {
            if (SaveSystem.UsingSeparateSlot) return;
            ClearOwner();
            if (!(foreignLocked && foreignServerHas)) foreignLocked = false;
        }

        /// <summary>11/14 남의 저장 파일을 옆에 복사해 둔다 (.other-계정, 모르면 .other-unknown)</summary>
        static void BackupForeign(string owner)
        {
            try
            {
                string tag = string.IsNullOrEmpty(owner) ? "unknown" : string.Join("_", owner.Split(Path.GetInvalidFileNameChars()));
                if (File.Exists(SaveSystem.CurrentPath)) File.Copy(SaveSystem.CurrentPath, SaveSystem.CurrentPath + ".other-" + tag, true);
            }
            catch (Exception) { }
        }

        static void ClearOwner()
        {
            try { if (File.Exists(OwnerPath)) File.Delete(OwnerPath); } catch (Exception) { }
        }

        static LocalSave ReadLocal()
        {
            try
            {
                string path = SaveSystem.CurrentPath;
                if (!File.Exists(path)) return null;
                long at = new DateTimeOffset(File.GetLastWriteTimeUtc(path)).ToUnixTimeMilliseconds();
                string json = File.ReadAllText(path);
                var d = JsonUtility.FromJson<SaveData>(json);
                if (d == null) return null;
                return new LocalSave { json = json, hash = Sha256(json), at = at, day = d.day, money = d.money, map = d.scene, origin = d.origin };
            }
            catch (Exception) { return null; }
        }

        static string Sha256(string s)
        {
            using (var sha = SHA256.Create())
            {
                var b = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
                var sb = new StringBuilder(64);
                foreach (var x in b) sb.Append(x.ToString("x2"));
                return sb.ToString();
            }
        }

        static string Zip(string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            using (var ms = new MemoryStream())
            {
                using (var gz = new GZipStream(ms, System.IO.Compression.CompressionLevel.Optimal, true)) gz.Write(bytes, 0, bytes.Length);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        static string Unzip(string b64)
        {
            using (var input = new MemoryStream(Convert.FromBase64String(b64)))
            using (var gz = new GZipStream(input, CompressionMode.Decompress))
            using (var reader = new StreamReader(gz, Encoding.UTF8))
                return reader.ReadToEnd();
        }

        // ---------------- 돈·짐 ----------------

        void Decide(NetClient.Msg m)
        {
            if (!m.account || Player == null) return;
            var net = NetClient.I;
            // 게임 저장을 통째로 받는 서버: 돈·짐은 고르지 않고 올리기만 한다 (게임 저장 고르기가 대신한다)
            if (m.fullOk) { Begin(null); return; }
            if (decidedFor == net.MyName || !m.hasSave || m.save == null) { Begin(m.hasSave ? null : "서버에 돈·짐을 처음 저장했어요"); return; }
            if (Signature(m.save.money, m.save.items, m.save.counts) == Signature(Snapshot())) { Begin(null); return; }
            offer = m.save;
            pickIdx = 0;
            Notify.Show("서버에 저장된 돈·짐이 있어요. 창을 닫으면 고를 수 있어요", 4f);
        }

        void Begin(string note)
        {
            decidedFor = NetClient.I.MyName;
            live = true;
            nextUpload = 0f;
            if (note != null) NetChat.System(note);
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

        /// <summary>서버 돈·짐을 이 컴퓨터에 넣는다. 교역품만 바꾸고 맡은 물건은 남긴다</summary>
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

        // ---------------- 고르는 창 ----------------

        void TryShow()
        {
            if (fullOffer != null)
            {
                if (!StartScreen.IsOpen) { CloseOffer(); return; }   // 그새 게임이 시작됐다: 덮어쓰지 않고 둔다
                if (NetLogin.IsOpen) return;                          // 로그인 창이 닫히면 띄운다
                showing = true;
                StartScreen.Held = true;
                lastMove = Vector2.one;
                return;
            }
            if (offer != null && !UIState.Blocking && !UIState.JustClosed)
            {
                showing = true;
                UIState.Blocking = true;
                lastMove = Vector2.one;
            }
        }

        void CloseOffer()
        {
            if (showing)
            {
                if (fullOffer != null) StartScreen.Held = false;
                else UIState.Release();
            }
            offer = null;
            fullOffer = null;
            localOffer = null;
            showing = false;
            fetching = false;
        }

        void Keys()
        {
            if (fetching) return;
            Vector2 mv = InputReader.MoveVector();
            if (mv.sqrMagnitude > 0.25f && lastMove.sqrMagnitude <= 0.25f && Mathf.Abs(mv.x) > Mathf.Abs(mv.y))
            {
                int p = mv.x < 0 ? 0 : 1;
                if (p != pickIdx) { pickIdx = p; Sfx.Play("open"); }
            }
            lastMove = mv;
            if (InputReader.EnterPressed()) Pick(pickIdx);   // 11/18 게임 들어가기 전 화면은 Enter
        }

        void Pick(int idx)
        {
            Sfx.Play("accept", .5f);
            if (fullOffer != null)
            {
                if (idx == 0) ChooseServer();
                else UseLocal();
                return;
            }
            if (idx == 0) LoadOffer();
            CloseOffer();
            Begin(idx == 0 ? null : "이 컴퓨터의 돈·짐을 서버에 올렸어요");
        }

        void OnGUI()
        {
            if (!showing || !Offering) return;
            bool full = fullOffer != null;
            if (!full && Player == null) return;
            UIStyle.Begin();
            GUI.depth = full ? -650 : -12;   // 게임 저장은 시작 화면(-500)·로그인 화면(-600) 위
            float W = UIStyle.VirtualWidth, H = UIStyle.VirtualHeight;
            if (!full) CuteUI.Dim();

            const float w = 344f, h = 180f;
            var box = new Rect(Mathf.Round((W - w) / 2f), Mathf.Round((H - h) / 2f) + 6f, w, h);
            CuteUI.Window(box);
            CuteUI.TitlePlate(box.center.x, box.y - 11, full ? "게임 저장 고르기" : "돈·짐 고르기");
            string head = !full ? "서버에 저장된 돈·짐이 있어요"
                : localOffer == null ? "서버에 저장된 게임이 있어요" : "다른 컴퓨터에서 한 게임이 서버에 있어요";
            CuteUI.LabelCenter(box.center.x, box.y + 24, head, CuteUI.Text, false, true);

            float cw = (w - 36f - 12f) / 2f, cy = box.y + 38f, ch = 70f;
            var left = new Rect(box.x + 18f, cy, cw, ch);
            var right = new Rect(left.xMax + 12f, cy, cw, ch);
            if (full)
            {
                DrawCard(left, "서버", When(fullOffer.at), $"{fullOffer.day}일째 · {fullOffer.money}냥", Place(fullOffer.map), 0);
                if (localOffer != null) DrawCard(right, "이 컴퓨터", When(localOffer.at), $"{localOffer.day}일째 · {localOffer.money}냥", Place(localOffer.map), 1);
                else DrawCard(right, "이 컴퓨터", null, null, null, 1);
            }
            else
            {
                var here = Snapshot();
                DrawCard(left, "서버", When(offer.at), $"{offer.money}냥", $"짐 {CountOf(offer.counts)}개", 0);
                DrawCard(right, "이 컴퓨터", "지금", $"{here.money}냥", $"짐 {CountOf(here.counts)}개", 1);
            }

            string note = !full ? "고르지 않은 쪽은 사라져요 · 맡은 물건·의뢰 짐은 그대로예요"
                : localOffer == null ? "안 쓰면 다음에 저장할 때 서버 저장이 새 게임으로 바뀌어요"
                : "고르지 않은 쪽은 덮어써요 · 이 컴퓨터 저장은 .bak 파일로 남아요";
            CuteUI.LabelCenter(box.center.x, cy + ch + 13f, note, CuteUI.Sub, true);

            var b0 = new Rect(left.x, box.yMax - 34f, cw, 22f);
            var b1 = new Rect(right.x, b0.y, cw, 22f);
            string l0 = "서버 것으로 하기", l1 = full && localOffer == null ? "서버 것 안 쓰기" : "이 컴퓨터 것 쓰기";
            if (fetching) CuteUI.LabelCenter(box.center.x, b0.center.y, "서버에서 받는 중…", CuteUI.Sub, false, true);
            else
            {
                bool c0 = pickIdx == 0 ? CuteUI.Button(b0, l0) : CuteUI.SoftButton(b0, l0);
                bool c1 = pickIdx == 1 ? CuteUI.Button(b1, l1) : CuteUI.SoftButton(b1, l1);
                if (c0) Pick(0);
                else if (c1) Pick(1);
            }
            // 11/18 은샘: 창 밖 글씨가 배경 그림 위에서 안 보였다 → 키캡 + 종이 알약
            {
                float kw0 = CuteUI.KeycapWidth("← →"), kw1 = CuteUI.KeycapWidth("Enter");
                float tw0 = CuteUI.Width("고르기", true), tw1 = CuteUI.Width("결정", true);
                float pw = 10f + kw0 + 4f + tw0 + 12f + kw1 + 4f + tw1 + 10f;
                var pill = new Rect(Mathf.Round(box.center.x - pw / 2f), Mathf.Round(box.yMax + 5f), Mathf.Round(pw), 19f);
                CuteUI.Box(pill, 8, CuteUI.Paper, CuteUI.Ink, 1, 2, CuteUI.SoftShadow);
                float kx = pill.x + 10f, ky = pill.center.y;
                kx += CuteUI.Keycap(kx, ky - 6.5f, "← →") + 4f;
                kx = CuteUI.Label(kx, ky, "고르기", CuteUI.Text, true) + 12f;
                kx += CuteUI.Keycap(kx, ky - 6.5f, "Enter") + 4f;
                CuteUI.Label(kx, ky, "결정", CuteUI.Text, true);
            }
        }

        /// <summary>서버 / 이 컴퓨터 카드. 고른 쪽은 청록 테. when이 null이면 "저장 없음"</summary>
        void DrawCard(Rect r, string tag, string when, string main, string sub, int idx)
        {
            bool on = pickIdx == idx;
            if (on) CuteUI.Box(new Rect(r.x - 3f, r.y - 3f, r.width + 6f, r.height + 6f), 10, CuteUI.Interact, CuteUI.Ink);
            CuteUI.Card(r);
            var t = new Rect(r.x + 7f, r.y + 7f, CuteUI.Width(tag, true, true) + 12f, 13f);
            CuteUI.Box(t, 6, on ? CuteUI.Interact : CuteUI.Paper2, CuteUI.Ink);
            CuteUI.LabelCenter(t.center.x, t.center.y, tag, on ? CuteUI.OnInteract : CuteUI.Text, true, true);
            if (when == null) CuteUI.LabelCenter(r.center.x, r.center.y + 7f, "저장 없음", CuteUI.Sub, false, true);
            else
            {
                CuteUI.LabelRight(r.xMax - 8f, t.center.y, when, CuteUI.Sub, true);
                CuteUI.Icon(CuteUI.Pic.Coin, r.x + 9f, r.y + 31f);
                CuteUI.Label(r.x + 22f, r.y + 35f, main, CuteUI.Text, false, true);
                CuteUI.Label(r.x + 9f, r.y + 54f, sub, CuteUI.Sub, true);
            }
            if (!fetching && CuteUI.Hit(r, GUIContent.none, GUIStyle.none) && pickIdx != idx) { pickIdx = idx; Sfx.Play("open"); }
        }

        static string When(long ms) =>
            ms > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(ms).ToLocalTime().ToString("M월 d일 HH:mm") : "?";

        static string Place(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return "어딘가";
            var index = WorldIndex.Get();
            return index != null ? index.MapName(mapId) : mapId;
        }
    }
}
