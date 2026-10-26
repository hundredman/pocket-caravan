using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 베네치아 지역 기능 점검 (개발용, 2026-10-26). Play에서 생활 연출 개수·밤 등불·미니게임 화면과 보상을 실제로 확인하고
    /// Logs/VeniceFeatureCheck/(report.txt, *.png)에 남긴다. 저장 파일은 건드리지 않는다(시작 맵이 아닌 씬에서 Play).
    /// 명령줄: -executeMethod PocketCaravan.EditorTools.VeniceFeatureCheck.RunAndQuit
    /// </summary>
    [InitializeOnLoad]
    public static class VeniceFeatureCheck
    {
        const string OutDir = "Logs/VeniceFeatureCheck";
        const string KeyActive = "VeniceFeatureCheck.active", KeyQuit = "VeniceFeatureCheck.quit", KeyDone = "VeniceFeatureCheck.done";
        static int step = -1;
        static float waitUntil;
        static readonly StringBuilder report = new StringBuilder();
        static int moneyBefore;

        static VeniceFeatureCheck() { EditorApplication.update += Tick; }

        [MenuItem("Pocket Caravan/베네치아/지역 기능 점검")]
        public static void Run() => Begin(false);

        public static void RunAndQuit()
        {
            VeniceRegionSetup.Build();
            Begin(true);
        }

        [Serializable] class MapJson { public string id; public int w, h; public int[] blocked; }

        /// <summary>
        /// 명령줄용: 베네치아·본토 씬을 하나씩 열어 칸마다 '걸을 수 있는지'(바닥 타일 있음 + 막힘 타일 없음)를 맵 JSON과 비교한다.
        /// 다르면 Logs/VeniceFeatureCheck/walk.txt에 칸 좌표를 남긴다. Play 없이 빠르게 끝난다.
        /// </summary>
        public static void WalkCheckAndQuit()
        {
            Directory.CreateDirectory(OutDir);
            var sb = new StringBuilder("[걷기 일치 점검] JSON(위가 0) 기준 좌표\n");
            foreach (var region in new[] { "venice", "center_venice" })
            {
                var text = File.ReadAllText($"Assets/PocketCaravan/World/Regions/{region}.json");
                foreach (System.Text.RegularExpressions.Match mm in System.Text.RegularExpressions.Regex.Matches(text, "\"mapId\":\\s*\"([a-z_]+)\""))
                {
                    var entry = WorldRegistry.Load().Map(mm.Groups[1].Value);
                    var json = JsonUtility.FromJson<MapJson>(File.ReadAllText(entry.jsonPath));
                    EditorSceneManager.OpenScene(entry.scenePath, OpenSceneMode.Single);
                    var map = UnityEngine.Object.FindAnyObjectByType<MapRoot>();
                    int bad = 0; var list = new StringBuilder();
                    for (int y = 0; y < json.h; y++)
                        for (int x = 0; x < json.w; x++)
                        {
                            bool jsonWalk = json.blocked[y * json.w + x] == 0;
                            var c = new Vector3Int(x, json.h - 1 - y, 0);
                            bool sceneWalk = map.ground != null && map.ground.HasTile(c) && !(map.blocked != null && map.blocked.HasTile(c));
                            if (jsonWalk != sceneWalk) { bad++; if (bad <= 12) list.Append($"({x},{y} json {(jsonWalk ? "걸음" : "막힘")}) "); }
                        }
                    sb.AppendLine($"{json.id}: 다른 칸 {bad} {list}");
                }
            }
            File.WriteAllText($"{OutDir}/walk.txt", sb.ToString());
            Debug.Log(sb.ToString());
            EditorApplication.Exit(0);
        }

        // ---------- Play 중 걷기 점검: 맵마다 MapRoot.IsWalkable(상호작용 대상 포함)과 맵 JSON을 비교 ----------
        const string KeyWalk = "VeniceFeatureCheck.walkrun";
        static readonly string[] WalkMaps = { "venice_rialto", "venice_santerasmo", "venice_murano", "venice_burano", "venice_chioggia",
            "venice_c_landing", "venice_c_vineyard", "venice_c_marsh", "venice_c_pass", "venice_c_bridge", "venice_c_post", "venice_c_gate" };

        public static void RuntimeWalkAndQuit()
        {
            SessionState.SetBool(KeyWalk, true);
            Begin(true);
        }

        static void WalkStep(GameManager gm, float now)
        {
            int i = step;
            if (i > 0)   // 지금 맵 점검 (i-1번째)
            {
                var id = MapRoot.Current.mapId;
                var entry = WorldRegistry.Load().Map(id);
                var json = JsonUtility.FromJson<MapJson>(File.ReadAllText(entry.jsonPath));
                int bad = 0; var list = new StringBuilder();
                for (int y = 0; y < json.h; y++)
                    for (int x = 0; x < json.w; x++)
                    {
                        bool jsonWalk = json.blocked[y * json.w + x] == 0;
                        var c = new Vector3Int(x, json.h - 1 - y, 0);
                        if (jsonWalk && !MapRoot.Current.IsWalkable(c))
                        {
                            bad++;
                            var who = Interactable.BlockerAt(c);
                            if (bad <= 10) list.Append($"({x},{y} {(who != null ? who.DisplayName : "?")}) ");
                        }
                    }
                report.AppendLine($"{id}: JSON은 걸음인데 Play에서 막힌 칸 {bad} {list}");
            }
            if (i >= WalkMaps.Length) { step = 99; waitUntil = now; return; }
            var target = WalkMaps[i];
            var ent = WorldRegistry.Load().Map(target);
            var spawnText = File.ReadAllText(ent.jsonPath);
            var m = System.Text.RegularExpressions.Regex.Match(spawnText, "\"id\":\"(from_[^\"]+)\"");
            gm.TravelTo(target, m.Success ? m.Groups[1].Value : "S0");
            step = i + 1; waitUntil = now + 2.5f;
        }

        // ---------- R 리액션 동작 6가지: 주민 옆으로 옮겨 차례로 누르고 동작 한가운데에서 찍는다 ----------
        const string KeyReact = "VeniceFeatureCheck.react";
        static readonly float[] ReactShotAt = { .3f, .95f, .7f, 1.0f, 1.0f, 2.0f };
        static readonly string[] ReactNames = { "hop", "spin", "stretch", "dance", "love", "nap" };

        [MenuItem("Pocket Caravan/베네치아/리액션 동작 점검")]
        public static void ReactionRun()
        {
            SessionState.SetBool(KeyReact, true);
            Begin(false);
        }

        public static void ReactionRunAndQuit()
        {
            SessionState.SetBool(KeyReact, true);
            Begin(true);
        }

        static void ReactStep(GameManager gm, float now)
        {
            var player = PlayerController.Instance;
            if (step == 0)
            {
                CharacterAnimator best = null;
                foreach (var a in UnityEngine.Object.FindObjectsByType<CharacterAnimator>(FindObjectsSortMode.None))
                    if (a != player.anim && !a.walking && (best == null || (a.transform.position - player.transform.position).sqrMagnitude < (best.transform.position - player.transform.position).sqrMagnitude))
                        best = a;
                if (best != null) player.Motor.Teleport(best.transform.position + new Vector3(1.5f, 0f, 0f), Vector2Int.left);
                report.AppendLine($"리액션 점검: 옆 주민 {(best != null ? best.transform.parent != null ? best.transform.parent.name : best.name : "없음")}");
                step = 1; waitUntil = now + 1f; return;
            }
            int i = (step - 1) / 2;
            if (i >= ReactNames.Length) { step = 99; waitUntil = now; Step(gm, now); return; }
            if ((step - 1) % 2 == 0) { VeniceReactions.TestPress(); step++; waitUntil = now + ReactShotAt[i]; }
            else
            {
                var body = player.anim.transform;
                report.AppendLine($"  {ReactNames[i]}: 그림 위치 {body.localPosition.y:0.00} 크기 {body.localScale.x:0.00}x{body.localScale.y:0.00} 기울기 {body.localEulerAngles.z:0}");
                Shot($"react_{i + 1}_{ReactNames[i]}");
                step++; waitUntil = now + 3.6f - ReactShotAt[i];
            }
        }

        // ---------- 베네치아 1장 Q1~Q7 자동 완주 ----------
        const string KeyQuest = "VeniceFeatureCheck.quest";

        public static void QuestRunAndQuit()
        {
            SessionState.SetBool(KeyQuest, true);
            Begin(true);
        }

        /// <summary>퀘스트 데이터 연결을 확인한다: 차례로 수락 → 단계마다 실제 게임 이벤트(구매·판매·이익·수레 구입)나 NPC 완료 호출 → 보상.
        /// 대상 NPC·장터가 등록된 맵에 실제로 있는지(WorldIndex)도 본다. 걷기·UI는 다른 점검이 맡는다.</summary>
        static void QuestStep(GameManager gm)
        {
            var index = WorldIndex.Get();
            var chain = gm.Db.quests.FindAll(q => q != null && q.id != null && q.id.StartsWith("venice_q"));
            chain.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            report.AppendLine($"베네치아 1장 퀘스트 {chain.Count}개");
            gm.Player.money = 5000;
            foreach (var q in chain)
            {
                int money0 = gm.Player.money, credit0 = gm.Player.credit;
                string blocker = gm.Quests.AcceptBlocker(q);
                if (!gm.Quests.CanStart(q) || blocker != null) { report.AppendLine($"  {q.id}: 수락 불가 ({blocker ?? "앞 퀘스트 미완료"})"); continue; }
                gm.Quests.Accept(q);
                var notes = new List<string>();
                for (int guard = 0; guard < 20 && !gm.Quests.IsFinished(q); guard++)
                {
                    QuestManager.Progress p = null;
                    foreach (var a in gm.Quests.Active) if (a.quest == q) p = a;
                    var o = p?.Current;
                    if (o == null) break;
                    var town = gm.Db.towns.Find(t => t != null && t.id == o.targetId);
                    var where = gm.Quests.TargetOf(o);
                    string mapOf = where.HasValue && index != null ? index.MapOf(where.Value.kind, where.Value.id) : null;
                    notes.Add($"{o.type}({o.targetId}{(mapOf != null ? "@" + mapOf : where.HasValue ? "@없음!" : "")})");
                    switch (o.type)
                    {
                        case ObjectiveType.Talk:
                        case ObjectiveType.Deliver:
                            if (o.item != null && gm.Player.cargo.Count(o.item) < o.count) gm.Player.cargo.Add(o.item, o.count - gm.Player.cargo.Count(o.item));
                            gm.Quests.TryCompleteAt(o.targetId, out _);
                            break;
                        case ObjectiveType.Buy:
                            for (int i = 0; i < o.count; i++) { gm.Player.cargo.Add(o.item, 1, 10); GameEvents.RaiseItemBought(o.item, town, 10); }
                            break;
                        case ObjectiveType.Sell:
                            for (int i = 0; i < o.count; i++) GameEvents.RaiseItemSold(o.item, town, 20, 5);
                            break;
                        case ObjectiveType.Profit:
                            GameEvents.RaiseItemSold(null, null, o.count, o.count);
                            break;
                        case ObjectiveType.BuyVehicle:
                            gm.Player.vehicle = o.vehicle;
                            GameEvents.RaiseVehicleBought(o.vehicle);
                            break;
                        default:
                            gm.Quests.Tick();
                            break;
                    }
                }
                bool done = gm.Quests.IsFinished(q);
                report.AppendLine($"  {q.id} '{q.title}': {(done ? "완료" : "미완료")}, 보상 돈 {gm.Player.money - money0:+0;-0;0} 신용 {gm.Player.credit - credit0:+0;-0;0} | {string.Join(" → ", notes)}");
            }
        }

        static void Begin(bool quit)
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(OutDir);
            foreach (var f in Directory.GetFiles(OutDir)) File.Delete(f);
            SessionState.SetBool(KeyActive, true);
            SessionState.SetBool(KeyQuit, quit);
            SessionState.SetBool(KeyDone, false);
            EditorSceneManager.OpenScene("Assets/PocketCaravan/Scenes/Venice/venice_rialto.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        static void Shot(string name) => ScreenCapture.CaptureScreenshot($"{OutDir}/{name}.png");

        static int Count(string name)
        {
            var root = GameObject.Find("베네치아 생활 연출");
            if (root == null) return -1;
            int n = 0;
            foreach (Transform t in root.transform) if (t.name == name) n++;
            return n;
        }

        static void Call(object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(target, null);

        static void Tick()
        {
            if (!SessionState.GetBool(KeyActive, false)) return;
            if (!EditorApplication.isPlaying)
            {
                if (SessionState.GetBool(KeyDone, false)) Finish();
                return;
            }
            Application.runInBackground = true;
            var gm = GameManager.I;
            if (gm == null || MapRoot.Current == null || gm.IsTravelling) return;
            float now = Time.realtimeSinceStartup;
            if (step == -1) { step = 0; waitUntil = now + 3f; report.AppendLine("[베네치아 지역 기능 점검]"); return; }
            if (now < waitUntil) return;
            try
            {
                if (SessionState.GetBool(KeyQuest, false) && step < 99) { QuestStep(gm); step = 99; waitUntil = now + 1f; }
                else if (SessionState.GetBool(KeyWalk, false) && step < 99) WalkStep(gm, now);
                else if (SessionState.GetBool(KeyReact, false) && step < 99) ReactStep(gm, now);
                else Step(gm, now);
            }
            catch (Exception e) { report.AppendLine($"단계 {step} 오류: {e.Message}"); step = 99; waitUntil = now; }
        }

        static void Step(GameManager gm, float now)
        {
            switch (step)
            {
                case 0:
                    report.AppendLine($"리알토 생활 연출: 비둘기 {Count("비둘기")}, 곤돌라 {Count("곤돌라")}, 가로등 {Count("가로등")}, 갈매기 {Count("갈매기")}, 연기 칸(가로등 빛 {Count("가로등 빛")})");
                    Shot("1_rialto_day");
                    gm.Clock.minuteOfDay = 22 * 60;
                    step = 1; waitUntil = now + 1.5f; break;
                case 1:
                    var glow = GameObject.Find("가로등 빛")?.GetComponent<SpriteRenderer>();
                    report.AppendLine($"밤 22시 등불 빛: {(glow != null ? (glow.enabled ? $"켜짐 (투명도 {glow.color.a:0.00})" : "꺼짐") : "없음")}");
                    Shot("2_rialto_night");
                    gm.Clock.minuteOfDay = 11 * 60;
                    gm.TravelTo("venice_murano", "from_venice_rialto_25");
                    step = 2; waitUntil = now + 2.5f; break;
                case 2:
                    report.AppendLine($"무라노 생활 연출: 연기 칸 {Count("연기")}, 곤돌라 {Count("곤돌라")}, 가로등 {Count("가로등")}");
                    var glass = UnityEngine.Object.FindAnyObjectByType<VeniceGlassSpot>();
                    report.AppendLine("무라노 유리 불기 자리: " + (glass != null ? $"있음 ({glass.Cell})" : "없음"));
                    report.AppendLine("무라노 장식 공방: " + (UnityEngine.Object.FindAnyObjectByType<VeniceWorkshop>() != null ? "있음" : "없음"));
                    if (glass != null) { Call(glass, "Begin"); moneyBefore = gm.Player.money; }
                    step = 3; waitUntil = now + 1.2f; break;
                case 3:
                    Shot("3_glass_minigame");
                    var g2 = UnityEngine.Object.FindAnyObjectByType<VeniceGlassSpot>();
                    if (g2 != null)
                    {
                        typeof(VeniceGlassSpot).GetField("hits", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(g2, 3);
                        Call(g2, "Reward");
                    }
                    step = 4; waitUntil = now + 1.0f; break;
                case 4:
                    Shot("4_glass_result");
                    var glassItem = gm.Db.items.Find(i => i != null && i.id == "venice_glass");
                    report.AppendLine($"유리 불기 보상(3번 성공 가정): 돈 {moneyBefore}→{gm.Player.money}, 유리그릇 {gm.Player.cargo.Count(glassItem)}개, 오늘 함 {VeniceSave.DoneToday("glass")}");
                    step = 5; waitUntil = now + 2.0f; break;
                case 5:
                    gm.TravelTo("venice_burano", "from_venice_rialto_59");
                    step = 6; waitUntil = now + 2.5f; break;
                case 6:
                    var net = UnityEngine.Object.FindAnyObjectByType<VeniceNetSpot>();
                    report.AppendLine("부라노 그물 자리: " + (net != null ? $"있음 ({net.Cell})" : "없음") + $", 곤돌라 {Count("곤돌라")}, 갈매기 {Count("갈매기")}");
                    report.AppendLine("부라노 레이스 공방: " + (UnityEngine.Object.FindAnyObjectByType<VeniceWorkshop>() != null ? "있음" : "없음"));
                    if (net != null) Call(net, "Begin");
                    step = 7; waitUntil = now + 1.2f; break;
                case 7:
                    Shot("5_net_minigame");
                    var n2 = UnityEngine.Object.FindAnyObjectByType<VeniceNetSpot>();
                    if (n2 != null)
                    {
                        typeof(VeniceNetSpot).GetField("caught", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(n2, 2);
                        Call(n2, "Reward");
                    }
                    step = 8; waitUntil = now + 1.0f; break;
                case 8:
                    var fish = gm.Db.items.Find(i => i != null && i.id == "venice_fish");
                    report.AppendLine($"그물 보상(2번 성공 가정): 절인 생선 {gm.Player.cargo.Count(fish)}개, 오늘 함 {VeniceSave.DoneToday("net")}");
                    while (VeniceFair.Today.town != "venice_c_landing") gm.Clock.day++;   // 오늘을 나루터 큰 장 날로
                    gm.TravelTo("venice_c_landing", "from_venice_chioggia");
                    step = 9; waitUntil = now + 2.5f; break;
                case 9:
                    var town = gm.Db.towns.Find(t => t != null && t.id == "venice_c_landing");
                    var wine = gm.Db.items.Find(i => i != null && i.id == "venice_wine");
                    report.AppendLine($"본토 장터·새 물건: 나루터 {(town != null ? "있음" : "없음")}, 양조장 {(gm.Db.towns.Exists(t => t != null && t.id == "venice_c_vineyard") ? "있음" : "없음")}, 포도주 {(wine != null ? "있음" : "없음")}, 유리 장식 {(gm.Db.items.Exists(i => i != null && i.id == "venice_glass_art") ? "있음" : "없음")}, 레이스 {(gm.Db.items.Exists(i => i != null && i.id == "venice_lace") ? "있음" : "없음")}");
                    report.AppendLine($"오늘 큰 장: {VeniceFair.Today.name}, 장식 {(GameObject.Find("큰 장 장식") != null ? "있음" : "없음")}, 나루터 장터 {(UnityEngine.Object.FindAnyObjectByType<TownMarket>() != null ? "있음" : "없음")}");
                    int before = gm.Player.money;
                    if (town != null && wine != null) GameEvents.RaiseItemSold(wine, town, 100, 10);
                    report.AppendLine($"큰 장 덤(100냥 판매 가정): 돈 {before}→{gm.Player.money}");
                    Shot("6_fair_landing");
                    gm.TravelTo("venice_rialto", "from_venice_chioggia_49");
                    step = 10; waitUntil = now + 2.5f; break;
                case 10:
                    var lion = UnityEngine.Object.FindAnyObjectByType<VeniceLion>();
                    report.AppendLine("리알토 사자 문장: " + (lion != null ? $"있음 ({lion.Cell}, 번호 {lion.index})" : "없음"));
                    if (lion != null) { lion.Interact(PlayerController.Instance); report.AppendLine($"   읽은 문장 {VeniceLion.Found} / 7"); }
                    var feed = UnityEngine.Object.FindAnyObjectByType<VeniceFeed>();
                    var museum = UnityEngine.Object.FindAnyObjectByType<VeniceMuseum>();
                    report.AppendLine($"모이 주머니 {(feed != null ? "있음" : "없음")}, 전시실 {(museum != null ? "있음" : "없음")}, 날씨 {VeniceAtmosphere.Now}");
                    if (feed != null) { int m0 = gm.Player.money; feed.Interact(PlayerController.Instance); report.AppendLine($"   모이 주기: 돈 {m0}→{gm.Player.money}, 오늘 함 {VeniceSave.DoneToday("feed")}"); }
                    var snd = gm.GetComponent<VeniceSound>();
                    report.AppendLine($"베네치아 음악: {(snd != null ? "붙음" : "없음")}");
                    while (gm.Clock.day % 7 != 5) gm.Clock.day++;
                    gm.Clock.minuteOfDay = 20 * 60;
                    Shot("7_rialto_feed");
                    step = 11; waitUntil = now + 1.5f; break;
                case 11:
                    report.AppendLine($"카니발 밤: {VeniceAtmosphere.CarnivalNight}, 가면 무리 {(GameObject.Find("카니발") != null ? "있음" : "없음")}");
                    var rialto = gm.Db.towns.Find(t => t != null && t.id == "venice_rialto");
                    var glassI = gm.Db.items.Find(i => i != null && i.id == "venice_glass");
                    int m1 = gm.Player.money;
                    if (rialto != null && glassI != null) GameEvents.RaiseItemSold(glassI, rialto, 100, 10);
                    report.AppendLine($"카니발 덤(유리 100냥 판매 가정): 돈 {m1}→{gm.Player.money}");
                    var src = gm.GetComponents<AudioSource>();
                    string playing = "";
                    foreach (var a in src) if (a.isPlaying && a.clip != null) playing += a.clip.name + " ";
                    report.AppendLine("재생 중인 소리: " + playing);
                    Shot("8_carnival");
                    var q3 = gm.Db.quests.Find(q => q != null && q.id == "venice_q3_crate");
                    if (q3 != null)   // 시험용: 앞 퀘스트를 건너뛰고 Q3 진행 상태를 직접 넣는다 (저장 안 함)
                    {
                        var activeField = typeof(QuestManager).GetField("active", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (activeField?.GetValue(gm.Quests) is System.Collections.IDictionary act && !act.Contains(q3))
                            act[q3] = new QuestManager.Progress { quest = q3 };
                    }
                    gm.Clock.minuteOfDay = 11 * 60;
                    gm.TravelTo("venice_chioggia", "from_venice_rialto_49");
                    step = 12; waitUntil = now + 2.5f; break;
                case 12:
                    var insp = UnityEngine.Object.FindAnyObjectByType<VeniceHonesty>();
                    report.AppendLine($"Q3 정직 시험: 퀘스트 진행 중 {VeniceHonesty.Active}, 부두 검수원 {(insp != null ? $"있음 ({insp.Cell})" : "없음")}");
                    if (insp != null) insp.Interact(PlayerController.Instance);
                    step = 13; waitUntil = now + 1.0f; break;
                case 13:
                    Shot("9_honesty");
                    step = 14; waitUntil = now + 1.0f; break;
                default:
                    File.WriteAllText($"{OutDir}/report.txt", report.ToString());
                    Debug.Log(report.ToString());
                    SessionState.SetBool(KeyDone, true);
                    step = -1;
                    EditorApplication.ExitPlaymode();
                    break;
            }
        }

        static void Finish()
        {
            bool quit = SessionState.GetBool(KeyQuit, false);
            SessionState.SetBool(KeyActive, false);
            SessionState.SetBool(KeyDone, false);
            SessionState.SetBool(KeyWalk, false);
            SessionState.SetBool(KeyQuest, false);
            SessionState.SetBool(KeyReact, false);
            Debug.Log("[지역 기능 점검] 끝. 결과: " + Path.GetFullPath(OutDir));
            if (quit) EditorApplication.Exit(0);
        }
    }
}
