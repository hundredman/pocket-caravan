using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 보물 상자 찾기 (2026-11-18 윤채, 미니게임 기획 "평소 안 가던 맵까지 돌아다니게").
    /// 어느 맵 어딘가에 상자가 숨겨지고 소문이 돈다: 처음엔 지역만 → 5분 뒤 맵 이름 → 그 맵에 들어가면 화면 위에 "차가워요 · 따뜻해요 · 뜨거워요".
    /// 여섯 칸 안으로 가까이 가면 상자가 보이고, Space로 열면 희귀품(NetCollection). 세 번 찾으면 칭호 「보물 사냥꾼」.
    /// - 온라인(서버 13단계 "games"): 서버가 숨기고(tid · idx · seed) 먼저 연 한 사람만 가진다 (trs · trsFind · trsWon · trsLate).
    ///   찾고 나서 15분 뒤 다음 상자, 40분 동안 아무도 못 찾으면 치운다.
    /// - 혼자(접속 안 함 · 예전 서버): 게임 하루에 하나. 맵 이름까지 바로 알려 준다.
    /// 맵은 세계 맵 목록(WorldIndex, 아이디 순)의 idx번째, 칸은 그 맵의 걸을 수 있는 칸 가운데 seed번째라 모두 같은 자리를 본다.
    /// </summary>
    public class NetTreasure : MonoBehaviour
    {
        const float MapHintAfter = 300f, ShowWithin = 6f;

        [Serializable] class TrsOut { public string t = "trs"; }
        [Serializable] class FindOut { public string t = "trsFind"; public long tid; }

        static NetTreasure inst;
        // 지금 소문 (online = 서버가 숨긴 것)
        long tid; int idx, seed; float bornAt; string foundBy = "";
        bool online, claimed;
        long announced = -1;
        // 11/19 소문 알림은 게임을 막 켰을 때(시작 화면·시작 연출·맵 불러오기) 바로 띄우지 않고, 맵에 도착해 몇 초 지난 뒤에 띄운다
        string pendingRumor; float settledSince = -1f;
        const float RumorDelay = 6f;
        void QueueRumor(string text) { pendingRumor = text; }
        void FlushRumor()
        {
            bool settled = GM != null && !StartScreen.IsOpen && !StartLaunch.Active && !GM.IsTravelling && MapRoot.Current != null && PlayerController.Instance != null;
            if (!settled) { settledSince = -1f; return; }
            if (settledSince < 0f) settledSince = Time.unscaledTime;
            if (pendingRumor == null || Time.unscaledTime - settledSince < RumorDelay) return;
            Notify.Event(pendingRumor, 5f); pendingRumor = null;
        }
        bool dev; Vector3Int devCell;   // 시험용: 지금 맵 가까이에 숨긴 상자 (/시험 보물)
        // 이 맵의 상자
        GameObject chest; SpriteRenderer chestSr; Vector3Int chestCell; string chestMap;
        static Sprite chestSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NetTreasure>() != null) return;
            var go = new GameObject("NetTreasure");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<NetTreasure>();
        }

        void Awake() { inst = this; NetClient.Received += OnMsg; GameEvents.MapEntered += OnMap; }
        void OnDestroy() { NetClient.Received -= OnMsg; GameEvents.MapEntered -= OnMap; }

        static GameManager GM => GameManager.I;
        static NetClient Net => NetClient.I;
        static bool Online => Net != null && Net.Status == NetClient.State.Online && Net.ServerGames;
        bool Active => tid != 0 && string.IsNullOrEmpty(foundBy) && !claimed;

        // ---------------- 소문 ----------------

        void OnMsg(NetClient.Msg m)
        {
            if (m.t == "welcome") { if (m.games) Net.Send(new TrsOut()); return; }
            if (m.t == "trs")
            {
                bool fresh = m.tid != 0 && m.tid != tid;
                online = true;
                if (fresh) claimed = false;
                tid = m.tid; idx = m.idx; seed = m.seed; foundBy = m.found ?? "";
                bornAt = Time.unscaledTime - m.age / 1000f;
                if (!string.IsNullOrEmpty(foundBy)) { RemoveChest(); pendingRumor = null; return; }
                if (tid != 0 && announced != tid)
                {
                    announced = tid;
                    QueueRumor($"보물 상자 소문! {RegionName(TargetMap)} 어딘가에 상자가 숨겨졌대요");
                }
                Place();
                return;
            }
            if (m.t == "trsWon" && m.tid == tid) { Claim(); return; }
            if (m.t == "trsLate")
            {
                claimed = true; RemoveChest();
                Notify.Show(string.IsNullOrEmpty(m.found) ? "상자가 벌써 사라졌어요" : $"{m.found} 님이 한발 먼저 열었어요!", 3.5f);
            }
        }

        /// <summary>혼자 놀 때: 게임 하루에 상자 하나 (그날 이미 찾았으면 없음)</summary>
        void LocalTick()
        {
            if (GM == null || StartScreen.IsOpen || (dev && !claimed)) return;
            int day = GM.Clock.day;
            if (online && Online) return;
            online = false;
            long want = 1000 + day;
            if (tid == want) return;
            tid = want;
            uint h = (uint)(day * 2654435761u);
            idx = (int)(h % 100000u);
            seed = (int)((h ^ 0x9E3779B9u) % 1000000000u);
            bornAt = Time.unscaledTime - MapHintAfter;   // 혼자일 때는 맵 이름까지 바로
            foundBy = "";
            claimed = GM.GetMemo("trs:local", -1f) >= day;
            if (!claimed && announced != tid) { announced = tid; QueueRumor($"보물 상자 소문! 오늘은 {MapName(TargetMap)}에 상자가 숨겨졌대요"); }
            Place();
        }

        /// <summary>상자가 숨겨진 맵 (세계 맵 목록에서 idx번째)</summary>
        string TargetMap
        {
            get
            {
                var world = WorldIndex.Get();
                if (world == null || world.maps.Count == 0) return null;
                var ids = new List<string>();
                foreach (var m in world.maps) if (m != null && !string.IsNullOrEmpty(m.mapId) && !string.IsNullOrEmpty(m.displayName)) ids.Add(m.mapId);
                ids.Sort(StringComparer.Ordinal);
                return ids.Count == 0 ? null : ids[Mathf.Abs(idx) % ids.Count];
            }
        }

        static string RegionName(string map)
        {
            if (string.IsNullOrEmpty(map)) return "어느 지역";
            if (map.StartsWith("goryeo")) return "고려";
            if (map.StartsWith("venice")) return "베네치아";
            if (map.StartsWith("arabia")) return "아라비아";
            if (map.StartsWith("north")) return "북유럽";
            return "중앙 대륙";
        }

        static string MapName(string map) => string.IsNullOrEmpty(map) ? "어딘가" : WorldIndex.Get()?.MapName(map) ?? map;

        /// <summary>수집함 · 채팅 /보물에 보이는 소문 한 줄</summary>
        public static string Rumor
        {
            get
            {
                if (inst == null || inst.tid == 0) return "보물 상자 소문: 아직 조용해요";
                if (!string.IsNullOrEmpty(inst.foundBy)) return $"보물 상자 소문: {inst.foundBy} 님이 찾았어요. 다음 상자를 기다려요";
                if (inst.claimed) return "보물 상자 소문: 이번 상자는 찾았어요";
                string map = inst.TargetMap;
                bool named = Time.unscaledTime - inst.bornAt >= MapHintAfter;
                string where = named ? $"{RegionName(map)} · {MapName(map)}" : $"{RegionName(map)} 어딘가 (곧 맵 이름이 알려져요)";
                return $"보물 상자 소문: {where}";
            }
        }

        // ---------------- 상자 ----------------

        void OnMap(string map) { RemoveChest(); Place(); }

        void Place()
        {
            if (!Active || GM == null || MapRoot.Current == null) return;
            string map = MapRoot.Current.mapId;
            if (map != TargetMap) { RemoveChest(); return; }
            if (chest != null && chestMap == map) return;
            var cell = dev ? devCell : PickCell(MapRoot.Current, seed);
            if (!cell.HasValue) return;
            RemoveChest();
            chestMap = map; chestCell = cell.Value;
            chest = new GameObject("보물 상자");
            chest.transform.position = MapRoot.Current.CellCenter(chestCell);
            var body = new GameObject("그림");
            body.transform.SetParent(chest.transform, false);
            chestSr = body.AddComponent<SpriteRenderer>();
            chestSr.sprite = ChestSprite();
            var pc = PlayerController.Instance;
            var mine = pc != null && pc.anim != null ? pc.anim.GetComponent<SpriteRenderer>() : null;
            if (mine != null) { chestSr.sortingLayerID = mine.sortingLayerID; chestSr.sortingOrder = PackAnimal.BodyOrder(mine); chestSr.sharedMaterial = mine.sharedMaterial; }
            body.transform.localPosition = new Vector3(0f, -.42f, 0f);
            var it = chest.AddComponent<Chest>();
            it.blocksMovement = false;
            chestSr.enabled = false;
        }

        void RemoveChest()
        {
            if (chest != null) Destroy(chest);
            chest = null; chestSr = null; chestMap = null;
        }

        /// <summary>그 맵의 걸을 수 있는 칸을 순서대로 모아 seed번째 (모든 게임이 같은 칸을 고른다). 다른 것이 서 있는 칸은 뺀다</summary>
        static Vector3Int? PickCell(MapRoot map, int seed)
        {
            var b = map.ground.cellBounds;
            var cells = new List<Vector3Int>();
            for (int y = b.yMin + 2; y < b.yMax - 2; y++)
                for (int x = b.xMin + 2; x < b.xMax - 2; x++)
                {
                    var c = new Vector3Int(x, y, 0);
                    if (map.ground.HasTile(c) && map.IsWalkable(c) && map.IsWalkable(c + Vector3Int.right) && map.IsWalkable(c + Vector3Int.left)) cells.Add(c);
                }
            if (cells.Count == 0) return null;
            for (int i = 0; i < 20; i++)
            {
                var c = cells[(int)((uint)(seed + i * 7919) % (uint)cells.Count)];
                if (Interactable.At(c) == null) return c;
            }
            return cells[(int)((uint)seed % (uint)cells.Count)];
        }

        class Chest : Interactable
        {
            public override string DisplayName => inst != null && inst.chestSr != null && inst.chestSr.enabled ? "보물 상자" : "";
            public override string ActionVerb => "열기";
            public override Kind Category => Kind.Quest;
            public override void Interact(PlayerController player) { if (inst != null && inst.chestSr != null && inst.chestSr.enabled) inst.Open(); }
        }

        void Open()
        {
            if (!Active) return;
            if (online && Online) { Net.Send(new FindOut { tid = tid }); Sfx.Play("open"); return; }
            if (!dev) GM.SetMemo("trs:local", GM.Clock.day);
            Claim();
            dev = false;
        }

        void Claim()
        {
            pendingRumor = null;   // 11/19 이미 찾았으면 소문은 띄우지 않는다
            claimed = true;
            RemoveChest();
            NetCollection.AddStat(NetCollection.StatTreasure);
            var r = NetCollection.RandomRare();
            NetCollection.Give(r.id, "보물 상자에서 찾았어요");
            int coins = UnityEngine.Random.Range(4, 9) * 10;
            if (GM != null) GM.Player.money += coins;
            Notify.Show($"상자 바닥에 {coins}냥도 있었어요", 3f);
        }

        void Update()
        {
            FlushRumor();
            if (!(online && Online) && !(dev && !claimed)) LocalTick();
            // 처음 들어온 맵(저장 불러오기)에는 맵 들어옴 알림이 소문보다 먼저 올 수 있다: 2초마다 자리를 확인한다
            if (chest == null && Active && Time.frameCount % 120 == 0) Place();
            if (chest == null || chestSr == null) return;
            var pc = PlayerController.Instance;
            if (pc == null) return;
            float d = Vector2.Distance(pc.transform.position, chest.transform.position);
            bool show = d <= ShowWithin;
            if (show && !chestSr.enabled) { chestSr.enabled = true; Sfx.Play("levelup", .4f); }
            else if (!show && chestSr.enabled) chestSr.enabled = false;
        }

        // ---------------- 화면: 따뜻해요 · 뜨거워요 ----------------

        void OnGUI()
        {
            if (chest == null || UIState.Blocking || UIState.PhotoMode) return;
            var pc = PlayerController.Instance;
            var cam = Camera.main;
            if (pc == null || cam == null) return;
            UIStyle.Begin();
            GUI.depth = -2;
            float d = Vector2.Distance(pc.transform.position, chest.transform.position);
            string word = d <= ShowWithin ? "바로 여기! 상자가 보여요" : d <= 12f ? "뜨거워요!" : d <= 25f ? "따뜻해요" : "차가워요…";
            Color32 c = d <= 12f ? CuteUI.Alert : d <= 25f ? CuteUI.Frame : CuteUI.Interact;
            string text = "보물 상자 · " + word;
            float w = CuteUI.Width(text, true, true) + 26;
            var r = new Rect(Mathf.Round(UIStyle.VirtualWidth - 12 - w), 40, w, 16);
            ScreenZones.Clear("treasure");
            r = ScreenZones.Dodge(r);
            ScreenZones.MarkCenter("treasure", r);
            CuteUI.Box(r, 8, CuteUI.Paper, c, 2, 1, CuteUI.SoftShadow);
            CuteUI.Icon(CuteUI.Pic.Star4, r.x + 6, r.center.y - 3, 1, c);
            CuteUI.Label(r.x + 16, r.center.y, text, CuteUI.Text, true, true);
            // 상자가 보이면 위에서 반짝
            if (chestSr != null && chestSr.enabled)
            {
                var sp = cam.WorldToScreenPoint(chest.transform.position + new Vector3(0f, .55f, 0f));
                if (sp.z > 0f)
                {
                    float bob = Mathf.Round(Mathf.Sin(Time.unscaledTime * 4f) * 2f);
                    CuteUI.Icon(CuteUI.Pic.Star4, sp.x / UIStyle.Scale - 3, (Screen.height - sp.y) / UIStyle.Scale - 6 + bob, 1, CuteUI.Deco);
                }
            }
        }

        /// <summary>시험용 (채팅 /시험 보물): 지금 맵, 내 자리에서 8칸쯤 떨어진 곳에 상자를 숨긴다 (혼자 놀이로)</summary>
        public static void DevHere()
        {
            var map = MapRoot.Current; var pc = PlayerController.Instance;
            if (inst == null || map == null || pc == null) return;
            var at = map.WorldToCell(pc.transform.position);
            Vector3Int? best = null;
            for (int r = 8; r <= 14 && best == null; r++)
                foreach (var d in new[] { new Vector3Int(r, 0, 0), new Vector3Int(-r, 0, 0), new Vector3Int(0, r, 0), new Vector3Int(0, -r, 0), new Vector3Int(r, r, 0), new Vector3Int(-r, -r, 0) })
                    if (map.IsWalkable(at + d) && Interactable.At(at + d) == null) { best = at + d; break; }
            if (best == null) { NetChat.System("시험: 가까이에 걸을 수 있는 빈 칸이 없어요"); return; }
            var world = WorldIndex.Get();
            var ids = new List<string>();
            foreach (var m in world.maps) if (m != null && !string.IsNullOrEmpty(m.mapId) && !string.IsNullOrEmpty(m.displayName)) ids.Add(m.mapId);
            ids.Sort(StringComparer.Ordinal);
            inst.online = false; inst.dev = true; inst.devCell = best.Value;
            inst.tid = -System.DateTime.Now.Ticks; inst.idx = Mathf.Max(0, ids.IndexOf(map.mapId)); inst.foundBy = ""; inst.claimed = false;
            inst.bornAt = Time.unscaledTime - MapHintAfter;
            inst.RemoveChest(); inst.Place();
            NetChat.System("시험: 이 맵에 상자를 숨겼어요. 화면 오른쪽 위 \"따뜻해요\"를 보며 찾아보세요");
        }

        /// <summary>채팅 "/보물": 지금 소문</summary>
        public static bool TryCommand(string word)
        {
            if (word.Trim() != "보물") return false;
            NetChat.System(Rumor);
            return true;
        }

        // ---------------- 상자 그림 (16×14 픽셀, 코드로 그린다) ----------------

        static Sprite ChestSprite()
        {
            if (chestSprite != null) return chestSprite;
            string[] rows =
            {
                "...XXXXXXXXXX...",
                "..XhhhhhhhhhhX..",
                ".XhHHHHHHHHHHhX.",
                ".XhhhhhyyhhhhhX.",
                "XXXXXXXyyXXXXXXX",
                "XllllllyylllllX.",
                "XlhhhhXyyXhhhlX.",
                "XlhhhhXYYXhhhlX.",
                "XlhhhhXXXXhhhlX.",
                "XlhhhhhhhhhhhlX.",
                "XlhhhhhhhhhhhlX.",
                "XllllllllllllXX.",
                "XXXXXXXXXXXXXX..",
            };
            int w = 16, h = rows.Length;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w && x < rows[y].Length; x++)
                {
                    Color32 c;
                    switch (rows[y][x])
                    {
                        case 'X': c = new Color32(0x5B, 0x3A, 0x29, 255); break;
                        case 'h': c = new Color32(0xC9, 0x8A, 0x4A, 255); break;
                        case 'H': c = new Color32(0xE6, 0xAE, 0x6A, 255); break;
                        case 'l': c = new Color32(0x9E, 0x63, 0x34, 255); break;
                        case 'y': c = CuteUI.Deco; break;
                        case 'Y': c = new Color32(0xFF, 0xF3, 0xB0, 255); break;
                        default: continue;
                    }
                    px[(h - 1 - y) * w + x] = c;
                }
            t.SetPixels32(px); t.Apply(false, false);
            chestSprite = Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(.5f, 0f), 16f);
            return chestSprite;
        }
    }
}
