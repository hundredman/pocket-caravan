using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 아라비아 첫 구간 자동 플레이 시험. 실제 씬을 Play하고 실제 출구 판정(PlayerController→MapExit)으로 3개 씬을 왕복하며,
    /// 실제 NPC 대화창 선택지·장터 구매 처리·퀘스트 판정으로 Q1·Q2를 완주한다.
    /// 1단계: Q1 완료 후 저장하고 Play 종료 → 2단계: 다시 Play해 전용 저장을 이어 불러온 뒤 Q2 완주.
    /// - 본편 저장 파일(pocket_caravan_save.json)은 시작·끝 해시를 비교해 변경되지 않았는지 확인한다.
    /// - 아라비아 전용 시험 저장이 이미 있으면 시작 전에 옆에 보관했다가 끝나면 되돌린다.
    /// 키보드 입력·화면 렌더링·조작감은 이 시험이 대신하지 않는다 (수동 플레이로 따로 확인).
    /// </summary>
    [InitializeOnLoad]
    public static class ArabiaPlaytest
    {
        const string PhaseKey = "PocketCaravan.ArabiaPlaytest.Phase";      // 0 없음, 1 1단계, 2 재시작 대기, 3 2단계, 4 정리
        const string LogKey = "PocketCaravan.ArabiaPlaytest.Log";
        const string MainHashKey = "PocketCaravan.ArabiaPlaytest.MainHash";
        const string SavedKey = "PocketCaravan.ArabiaPlaytest.Saved";
        const string FailKey = "PocketCaravan.ArabiaPlaytest.Failed";
        const string ReportKey = "PocketCaravan.ArabiaPlaytest.Report";
        const string RunInBackgroundKey = "PocketCaravan.ArabiaPlaytest.RunInBackground";

        static IEnumerator routine;
        static object current;

        internal static int Phase { get => SessionState.GetInt(PhaseKey, 0); set => SessionState.SetInt(PhaseKey, value); }
        static string MainSavePath => Path.Combine(Application.persistentDataPath, "pocket_caravan_save.json");
        static string ArabiaSavePath => Path.Combine(Application.persistentDataPath, ArabiaRegionSetup.SaveFileName);
        static string ManualBackupPath => ArabiaSavePath + ".manual-backup";

        static ArabiaPlaytest()
        {
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        [MenuItem("Pocket Caravan/아라비아/5. 자동 플레이 시험 (Q1·Q2·왕복·저장)", priority = 221)]
        public static void StartMenu() => Begin(false, true);

        [MenuItem("Pocket Caravan/아라비아/6. 자동 보행·화면 캡처 시험 (Q1 경로)", priority = 222)]
        public static void WalkMenu() => Begin(true, true);

        /// <summary>walk=true: 실제 이동 처리(PlayerMotor 충돌)로 걸어서 Q1 경로를 지나며 Game 화면을 캡처한다.</summary>
        public static void Begin(bool walk, bool interactive)
        {
            if (EditorApplication.isPlaying || Phase != 0)
            {
                if (interactive && !EditorUtility.DisplayDialog("아라비아 자동 시험", "이미 진행 중이거나 이전 시험 상태가 남아 있습니다. 상태를 지우고 새로 시작할까요?", "새로 시작", "취소")) return;
                Phase = 0;
                if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; if (!interactive) throw new InvalidOperationException("Play 중이었습니다. 종료 후 다시 요청하세요."); return; }
            }
            if (interactive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetString(LogKey, "");
            SessionState.SetBool(FailKey, false);
            SessionState.SetString(ReportKey, walk ? "walk.md" : "autoplay.md");
            SessionState.SetBool(RunInBackgroundKey, PlayerSettings.runInBackground);
            SessionState.SetString(MainHashKey, ArabiaRegionSetup.FileHash(MainSavePath));
            Log($"# 아라비아 {(walk ? "자동 보행·화면 캡처" : "자동 플레이")} 시험 {DateTime.Now:yyyy-MM-dd HH:mm:ss} · Unity {Application.unityVersion}");
            Log($"본편 저장 파일: {MainSavePath} (시작 해시 {Short(SessionState.GetString(MainHashKey, ""))})");
            if (File.Exists(ArabiaSavePath))
            {
                // 중단된 이전 자동 시험이 남긴 보관본이 있으면 그것(사람이 하던 저장)을 우선 보존한다.
                if (!File.Exists(ManualBackupPath)) File.Copy(ArabiaSavePath, ManualBackupPath, false);
                File.Delete(ArabiaSavePath);
                Log("기존 아라비아 시험 저장을 .manual-backup으로 보관 후 새 시험 시작");
            }
            EditorSceneManager.OpenScene(WorldRegistry.Load().Map(ArabiaRegionSetup.AhsaMap).scenePath);
            Phase = walk ? 11 : 1;
            EditorApplication.isPlaying = true;
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // 에디터 창이 뒤에 있어도 자동 시험이 멈추지 않게 이번 Play에서만 켠다 (프로젝트 설정 저장 안 함).
                if (Phase == 1 || Phase == 3 || Phase == 11) Application.runInBackground = true;
                if (Phase == 1) { routine = PhaseOne(); current = null; }
                else if (Phase == 11) { routine = Run(ArabiaWalk.Steps()); current = null; }
                else if (Phase == 3) { routine = PhaseTwo(); current = null; }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                routine = null; current = null;
                if (Phase != 0)
                {
                    bool rib = SessionState.GetBool(RunInBackgroundKey, false);
                    if (PlayerSettings.runInBackground != rib) PlayerSettings.runInBackground = rib;
                    Application.runInBackground = rib;
                }
                // Phase 2 → 3 재시작은 Tick에서 한다 (에디터가 뒤에 있으면 delayCall이 늦게 와 멈추는 문제가 있었음).
                if (Phase == 4) Finish();
            }
        }

        // ---------- 진행기 ----------

        internal class Until { public Func<bool> cond; public double deadline; public string what; }
        internal static Until Wait(Func<bool> cond, float seconds, string what) =>
            new Until { cond = cond, deadline = EditorApplication.timeSinceStartup + seconds, what = what };
        internal static Until Frames(int n) { int left = n; return Wait(() => --left <= 0, 10, "frames"); }

        static void Tick()
        {
            // 1단계 종료 후 재시작 요청이 이벤트 순서 때문에 빠졌다면 여기서 다시 Play한다.
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode && Phase == 2)
            { Phase = 3; EditorApplication.isPlaying = true; return; }
            if (routine == null || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
            if (current is Until u)
            {
                bool ok;
                try { ok = u.cond(); } catch { ok = false; }
                if (!ok)
                {
                    if (EditorApplication.timeSinceStartup > u.deadline) Abort("시간 초과: " + u.what);
                    return;
                }
                current = null;
            }
            try
            {
                // 한 Tick에 대기가 나올 때까지 진행 (중첩 IEnumerator는 Run으로 펼친다)
                while (routine != null && current == null)
                {
                    if (!routine.MoveNext()) { routine = null; return; }
                    current = routine.Current;
                }
            }
            catch (Exception e) { Abort("예외: " + e); }
        }

        internal static IEnumerator Run(params IEnumerator[] parts)
        {
            foreach (var part in parts)
            {
                var stack = new Stack<IEnumerator>(); stack.Push(part);
                while (stack.Count > 0)
                {
                    var top = stack.Peek();
                    if (!top.MoveNext()) { stack.Pop(); continue; }
                    if (top.Current is IEnumerator nested) stack.Push(nested);
                    else yield return top.Current;
                }
            }
        }

        static void Abort(string why)
        {
            Check("시험 진행", false, why);
            routine = null; current = null;
            Phase = 4;
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false; else Finish();
        }

        static void Finish()
        {
            Phase = 0;
            string mainBefore = SessionState.GetString(MainHashKey, ""), mainAfter = ArabiaRegionSetup.FileHash(MainSavePath);
            Check("본편 저장 파일 변경 없음 (시험 전후 해시)", mainBefore == mainAfter, $"{Short(mainBefore)} → {Short(mainAfter)}");
            // 시험이 만든 전용 저장은 확인용 사본만 남기고 치운다. 이전 수동 시험 저장이 있었다면 되돌린다.
            if (File.Exists(ArabiaSavePath)) File.Copy(ArabiaSavePath, ArabiaSavePath + ".autotest-last", true);
            foreach (var p in new[] { ArabiaSavePath, ArabiaSavePath + ".bak", ArabiaSavePath + ".tmp" }) if (File.Exists(p)) File.Delete(p);
            if (File.Exists(ManualBackupPath)) { File.Move(ManualBackupPath, ArabiaSavePath); Log("이전 아라비아 시험 저장을 복원"); }
            bool failed = SessionState.GetBool(FailKey, false);
            Log(failed ? "\n결과: 실패 항목 있음" : "\n결과: 모든 항목 통과");
            string text = SessionState.GetString(LogKey, "");
            ArabiaRegionSetup.WriteReport(SessionState.GetString(ReportKey, "autoplay.md"), text);
            if (failed) Debug.LogError("[아라비아] 자동 플레이 시험 실패\n" + text); else Debug.Log("[아라비아] 자동 플레이 시험 통과\n" + text);
        }

        internal static void Log(string line) => SessionState.SetString(LogKey, SessionState.GetString(LogKey, "") + line + "\n");
        internal static bool Check(string name, bool ok, string detail = "")
        {
            Log($"- [{(ok ? "PASS" : "FAIL")}] {name}{(string.IsNullOrEmpty(detail) ? "" : " — " + detail)}");
            if (!ok) SessionState.SetBool(FailKey, true);
            return ok;
        }
        static string Short(string hash) => string.IsNullOrEmpty(hash) || hash.Length < 12 ? hash : hash.Substring(0, 12);

        // ---------- 게임 조작 도우미 ----------

        internal static GameManager GM => GameManager.I;
        internal static PlayerState P => GM.Player;
        internal static QuestData Quest(string id) => GM.Db.quests.Find(q => q != null && q.id == id);
        internal static ItemData Item(string id) => GM.ItemById(id);
        internal static bool Ready(string map) => GM != null && MapRoot.Current != null && MapRoot.Current.mapId == map && !GM.IsTravelling && PlayerController.Instance != null;

        internal static IEnumerator TravelVia(string target, string expectSpawn)
        {
            string from = MapRoot.Current.mapId;
            var exit = MapExit.All.FirstOrDefault(e => e.targetScene == target);
            if (!Check($"{from}에 {target} 출구 있음", exit != null)) yield break;
            var map = MapRoot.Current;
            var origin = map.WorldToCell(exit.transform.position);
            var cell = origin + (exit.size.y > 1 ? new Vector3Int(0, exit.size.y / 2, 0) : new Vector3Int(exit.size.x / 2, 0, 0));
            var motor = PlayerController.Instance.Motor;
            var facing = cell.x >= map.WorldToCell(motor.Feet).x ? Vector2Int.right : Vector2Int.left;
            float hours = GM.Clock.TotalHours;
            motor.Teleport(map.CellCenter(cell) - (Vector3)motor.feetOffset, facing);   // 출구 칸 위 → PlayerController가 실제 출구 처리
            yield return Wait(() => Ready(target), 20, $"{from} → {target} 이동");
            yield return Frames(3);
            var spawn = SpawnPoint.Find(expectSpawn);
            var feet = MapRoot.Current.WorldToCell(PlayerController.Instance.Motor.Feet);
            bool inside = false;
            if (spawn != null)
            {
                var first = MapRoot.Current.WorldToCell(spawn.transform.position);
                inside = feet.x >= first.x && feet.x < first.x + spawn.size.x && feet.y >= first.y && feet.y < first.y + spawn.size.y;
            }
            Check($"이동 {from} → {target}: 도착 스폰 {expectSpawn}", inside, $"발 위치 {feet}");
            Check($"{target} 도착 칸이 출구가 아님 (즉시 재이동 없음)", MapExit.At(feet) == null);
            float travel = MapRoot.Current.travelHours;
            if (travel > 0) Check($"{target} 길 통과 시간 +{travel}시간", GM.Clock.TotalHours - hours >= travel - 0.01f, $"{GM.Clock.TotalHours - hours:0.0}시간");
        }

        internal static IEnumerator Approach(Interactable target)
        {
            var map = MapRoot.Current;
            var c = target.Cell;
            var spots = new[] { (c + Vector3Int.down, Vector2Int.up), (c + Vector3Int.up, Vector2Int.down), (c + Vector3Int.left, Vector2Int.right), (c + Vector3Int.right, Vector2Int.left) };
            var free = spots.Where(s => map.IsWalkable(s.Item1) && MapExit.At(s.Item1) == null).ToList();
            if (!Check($"{target.DisplayName} 옆에 설 자리", free.Count > 0)) yield break;
            var spot = free[0];
            var motor = PlayerController.Instance.Motor;
            motor.Teleport(map.CellCenter(spot.Item1) - (Vector3)motor.feetOffset, spot.Item2);
            yield return Frames(3);
            Check($"{target.DisplayName} 대화 대상 인식 (Space 대상)", PlayerController.Instance.TalkTarget == target,
                PlayerController.Instance.TalkTarget != null ? PlayerController.Instance.TalkTarget.DisplayName : "없음");
        }

        static readonly BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        static DialogueUI Dialogue => GM.GetComponent<DialogueUI>();
        internal static bool DialogueOpen => (bool)typeof(DialogueUI).GetField("open", Any).GetValue(Dialogue);
        static string DialogueText => (string)typeof(DialogueUI).GetField("text", Any).GetValue(Dialogue);
        static List<DialogueUI.Choice> DialogueChoices => (List<DialogueUI.Choice>)typeof(DialogueUI).GetField("choices", Any).GetValue(Dialogue);
        internal static bool PickChoice(string label)
        {
            var choice = DialogueChoices.FirstOrDefault(c => c.label == label);
            if (choice.label != label) return false;
            typeof(DialogueUI).GetMethod("Pick", Any).Invoke(Dialogue, new object[] { choice });
            return true;
        }
        internal static void CloseDialogue() { if (DialogueOpen) typeof(DialogueUI).GetMethod("Close", Any).Invoke(Dialogue, null); }

        internal static string Talk(string npcId)
        {
            var npc = UnityEngine.Object.FindObjectsByType<QuestNpc>(FindObjectsSortMode.None).First(n => n.npcId == npcId);
            npc.Interact(PlayerController.Instance);
            return DialogueOpen ? DialogueText : "";
        }
        internal static QuestNpc Npc(string id) => UnityEngine.Object.FindObjectsByType<QuestNpc>(FindObjectsSortMode.None).FirstOrDefault(n => n.npcId == id);
        internal static TownMarket Market() => UnityEngine.Object.FindObjectsByType<TownMarket>(FindObjectsSortMode.None).FirstOrDefault();
        static string MarketCall(TownMarket m, string method, ItemData item)
        {
            typeof(TownMarket).GetMethod(method, Any).Invoke(m, new object[] { item });
            return (string)typeof(TownMarket).GetField("message", Any).GetValue(m);
        }
        internal static void MarketOpen(TownMarket m) => m.Interact(PlayerController.Instance);
        internal static void MarketClose(TownMarket m) => typeof(TownMarket).GetMethod("Close", Any).Invoke(m, null);
        static string ReadArabiaSave() => File.Exists(ArabiaSavePath) ? File.ReadAllText(ArabiaSavePath) : "";

        // ---------- 1단계: 세션 시작, Q1 ----------

        static IEnumerator PhaseOne() => Run(PhaseOneSteps());
        static IEnumerator PhaseOneSteps()
        {
            yield return Wait(() => Ready(ArabiaRegionSetup.AhsaMap), 30, "알 아흐사 Play 시작");
            yield return Frames(5);
            Log("\n## 1단계 — 새 세션, Q1 배달, 저장");
            var session = GM.ActiveRegionSession;
            Check("아라비아 시험 세션 시작", session != null && session.regionId == "arabia");
            Check("전용 저장 파일 사용", SaveSystem.FileName == ArabiaRegionSetup.SaveFileName && SaveSystem.CurrentPath == ArabiaSavePath, SaveSystem.FileName);
            Check("자동 저장 켜짐 (전용 파일)", GM.AutosaveEnabled);
            Check("시작 자금 = 세션 설정", session != null && P.money == session.startMoney, P.money.ToString());
            Check("출신 = 아라비아, 등짐", P.origin != null && P.origin.id == "arabia" && P.vehicle != null && P.vehicle.id == "walk", $"{P.origin?.id}/{P.vehicle?.id} 적재 {P.Capacity}");
            Check("S0 스폰에서 시작", SpawnPoint.Find(SpawnPoint.StartId) != null);
            Check("시작 직후 본편 저장 파일 그대로", ArabiaRegionSetup.FileHash(MainSavePath) == SessionState.GetString(MainHashKey, ""));

            var qm = GM.Quests; var q1 = Quest(ArabiaRegionSetup.Q1Id); var q2 = Quest(ArabiaRegionSetup.Q2Id);
            var delivery = Item(ArabiaRegionSetup.DeliveryDatesId); var dates = Item(ArabiaRegionSetup.DatesId);
            Check("힌드바드 머리 위 표식 = 받을 의뢰(!)", qm.MarkerFor("arabia_hindbad") == QuestMarker.Available);
            Check("Q1 완료 전 마리암은 Q2를 제안하지 않음", qm.AvailableAllFrom("arabia_maryam").Count == 0 && qm.MarkerFor("arabia_maryam") == QuestMarker.None);

            // 짐 부족 안내 (수락 차단)
            int fill = Mathf.FloorToInt((P.Capacity - P.TotalWeight) / dates.weight) - 1;
            P.cargo.Add(dates, fill);
            string block = qm.AcceptBlocker(q1);
            Check("짐이 가득하면 Q1 수락을 막고 이유 안내", block != null, block);
            P.cargo.Remove(dates, fill);

            yield return Approach(Npc("arabia_hindbad"));
            string offer = Talk("arabia_hindbad");
            Check("힌드바드 대화창에 Q1 제안·보상 표시", offer.Contains(q1.title) && offer.Contains($"보상: {q1.rewardMoney}냥"), offer.Split('\n')[0]);
            Check("대화창 '수락' 선택", PickChoice("수락"));
            yield return Frames(2);
            Check("Q1 진행 중·맡긴 대추야자 2개 받음", qm.Active.Any(p => p.quest == q1) && P.cargo.Count(delivery) == 2, $"보유 {P.cargo.Count(delivery)}");
            Check("힌드바드 표식 = 진행 중", qm.MarkerFor("arabia_hindbad") == QuestMarker.InProgress);
            float loaded = P.LoadRatio;
            Check("의뢰품 적재로 짐 비율 증가 (감속 반영)", loaded > 0f, $"짐 {P.TotalWeight}/{P.Capacity}");

            var market = Market();
            MarketOpen(market);
            string sellMsg = MarketCall(market, "Sell", delivery);
            MarketClose(market);
            Check("맡긴 짐은 장터에 팔 수 없음", P.cargo.Count(delivery) == 2 && sellMsg.Contains("팔 수 없"), sellMsg);

            yield return TravelVia(ArabiaRegionSetup.FarmroadMap, "arabia_farmroad_spawn_w_01");
            yield return TravelVia(ArabiaRegionSetup.ZabidMap, "arabia_zabid_spawn_w_01");

            var keeper = Npc("arabia_kitchen_keeper");
            Check("부엌 담당 표식 = 완료 가능(?)", qm.MarkerFor("arabia_kitchen_keeper") == QuestMarker.TurnIn);
            yield return Approach(keeper);
            int money0 = P.money, credit0 = P.credit;
            P.cargo.Remove(delivery, 1);
            string lack = Talk("arabia_kitchen_keeper");
            Check("짐이 모자라면 납품 거절·이유 안내", lack.Contains($"{q1.objectives[0].count}개가 필요") && P.money == money0 && qm.Active.Any(p => p.quest == q1), lack);
            CloseDialogue();
            P.cargo.Add(delivery, 1);
            yield return Frames(2);

            string done = Talk("arabia_kitchen_keeper");
            Check("납품 대사", done.Contains("장부에 적어 둘게"), done);
            PickChoice("확인");
            yield return Frames(2);
            Check($"Q1 완료: 보수 +{q1.rewardMoney}, 신용 +{q1.rewardCredit}, 의뢰품 제거",
                qm.IsFinished(q1) && P.money == money0 + q1.rewardMoney && P.credit == credit0 + q1.rewardCredit && P.cargo.Count(delivery) == 0,
                $"돈 {money0}→{P.money}, 신용 {credit0}→{P.credit}");
            Talk("arabia_kitchen_keeper"); CloseDialogue();
            yield return Frames(2);
            Check("다시 말 걸어도 보수 중복 지급 없음", P.money == money0 + q1.rewardMoney && P.credit == credit0 + q1.rewardCredit);
            Check("Q1 완료 후 마리암이 Q2 제안 가능", qm.AvailableAllFrom("arabia_maryam").Contains(q2));

            Check("저장하기 (전용 파일)", GM.SaveNow());
            string json = ReadArabiaSave();
            Check("전용 저장에 Q1 완료·현재 씬 기록", json.Contains(q1.id) && json.Contains("\"scene\":\"arabia_zabid\""));
            SessionState.SetString(SavedKey, $"{P.money}|{P.credit}");
            Log("1단계 종료: Play를 멈춘 뒤 알 아흐사에서 다시 Play해 이어 불러오기를 확인");
            Phase = 2;
            EditorApplication.isPlaying = false;
        }

        // ---------- 2단계: 이어 불러오기, Q2 ----------

        static IEnumerator PhaseTwo() => Run(PhaseTwoSteps());
        static IEnumerator PhaseTwoSteps()
        {
            yield return Wait(() => Ready(ArabiaRegionSetup.ZabidMap), 30, "알 아흐사 Play → 저장된 자비드로 이어 불러오기");
            yield return Frames(5);
            Log("\n## 2단계 — 이어 불러오기, Q2 구매·납품");
            var saved = SessionState.GetString(SavedKey, "0|0").Split('|');
            var qm = GM.Quests; var q1 = Quest(ArabiaRegionSetup.Q1Id); var q2 = Quest(ArabiaRegionSetup.Q2Id);
            var grain = Item(ArabiaRegionSetup.GrainId); var dates = Item(ArabiaRegionSetup.DatesId);
            Check("이어 불러오기: 저장된 씬(자비드)·돈·신용·Q1 완료 복원",
                P.money.ToString() == saved[0] && P.credit.ToString() == saved[1] && qm.IsFinished(q1), $"돈 {P.money}, 신용 {P.credit}");
            Check("이어 불러오기도 전용 저장 파일 사용", GM.ActiveRegionSession != null && SaveSystem.FileName == ArabiaRegionSetup.SaveFileName);

            yield return TravelVia(ArabiaRegionSetup.FarmroadMap, "arabia_farmroad_spawn_e_01");
            yield return TravelVia(ArabiaRegionSetup.AhsaMap, "arabia_ahsa_spawn_e_01");
            Check("마리암 표식 = 받을 의뢰(!)", qm.MarkerFor("arabia_maryam") == QuestMarker.Available);
            yield return Approach(Npc("arabia_maryam"));
            int moneyAccept = P.money;
            string offer = Talk("arabia_maryam");
            Check("마리암 대화창에 Q2 제안·보상", offer.Contains(q2.title) && offer.Contains($"보상: {q2.rewardMoney}냥"), offer.Split('\n')[0]);
            Check("대화창 '수락' 선택", PickChoice("수락"));
            yield return Frames(2);
            Check("Q2 진행 중 (수락 시 돈 변화 없음)", qm.Active.Any(p => p.quest == q2) && P.money == moneyAccept);

            // 구매처 판정: 알 아흐사에서 산 곡물은 Q2 구매 단계로 인정하지 않는다
            var ahsaMarket = Market();
            yield return Approach(ahsaMarket);
            MarketOpen(ahsaMarket);
            int m0 = P.money;
            string boughtHere = MarketCall(ahsaMarket, "Buy", grain);
            var prog = qm.Active.First(p => p.quest == q2);
            Check("다른 장터(알 아흐사) 구매는 Q2 구매 단계로 세지 않음", P.cargo.Count(grain) == 1 && prog.step == 0 && prog.amount == 0, boughtHere);
            string soldBack = MarketCall(ahsaMarket, "Sell", grain);
            MarketClose(ahsaMarket);
            Log($"  (참고) 알 아흐사 곡물 1개 매입/재판매: {m0}→{P.money}냥 · {soldBack}");

            yield return TravelVia(ArabiaRegionSetup.FarmroadMap, "arabia_farmroad_spawn_w_01");
            yield return TravelVia(ArabiaRegionSetup.ZabidMap, "arabia_zabid_spawn_w_01");
            var market = Market();
            Check("자비드 장터 = 자비드 마을", market != null && market.town != null && market.town.id == ArabiaRegionSetup.ZabidTown);
            yield return Approach(market);
            MarketOpen(market);

            int keep = P.money; P.money = 0;
            string poor = MarketCall(market, "Buy", grain);
            Check("돈 부족: 구매 안 됨·이유 안내", P.cargo.Count(grain) == 0 && poor.Contains("돈이 모자랍니다"), poor);
            P.money = keep;
            int fill = Mathf.FloorToInt((P.Capacity - P.TotalWeight) / dates.weight + 0.001f);
            P.cargo.Add(dates, fill);
            string full = MarketCall(market, "Buy", grain);
            Check("짐 가득: 구매 안 됨·이유 안내", P.cargo.Count(grain) == 0 && full.Contains("더 실을 수 없습니다"), full);
            P.cargo.Remove(dates, fill);
            Check("실패한 구매는 Q2 진행에 반영되지 않음", prog.step == 0 && prog.amount == 0);

            int before = P.money;
            string b1 = MarketCall(market, "Buy", grain);
            string b2 = MarketCall(market, "Buy", grain);
            MarketClose(market);
            int cost = before - P.money;
            Check("자비드에서 곡물 2개 구매 → Q2 납품 단계로", P.cargo.Count(grain) == 2 && prog.step == 1, $"{b1} / {b2} · 구매비 {cost}냥");

            yield return TravelVia(ArabiaRegionSetup.FarmroadMap, "arabia_farmroad_spawn_e_01");
            yield return TravelVia(ArabiaRegionSetup.AhsaMap, "arabia_ahsa_spawn_e_01");
            Check("마리암 표식 = 완료 가능(?)", qm.MarkerFor("arabia_maryam") == QuestMarker.TurnIn);
            yield return Approach(Npc("arabia_maryam"));
            int money0 = P.money, credit0 = P.credit;
            string done = Talk("arabia_maryam");
            Check("Q2 납품 대사", done.Contains("네 힘으로 채웠구나"), done);
            PickChoice("확인");
            yield return Frames(2);
            Check($"Q2 완료: 곡물 2개 제거, 보수 +{q2.rewardMoney}, 신용 +{q2.rewardCredit}",
                qm.IsFinished(q2) && P.cargo.Count(grain) == 0 && P.money == money0 + q2.rewardMoney && P.credit == credit0 + q2.rewardCredit,
                $"돈 {money0}→{P.money}, 신용 {credit0}→{P.credit}");
            Check("Q2 실제 이익 = 보수 − 구매비 > 0", q2.rewardMoney - cost > 0, $"{q2.rewardMoney} − {cost} = {q2.rewardMoney - cost}냥");
            Talk("arabia_maryam"); CloseDialogue();
            yield return Frames(2);
            Check("다시 말 걸어도 Q2 보수 중복 없음", P.money == money0 + q2.rewardMoney && P.credit == credit0 + q2.rewardCredit);

            var offers = GM.Contracts.Available(ArabiaRegionSetup.AhsaTown);
            Check("선지출 없는 재기 통로: 알 아흐사 게시판 배달 의뢰 (아라비아 안 목적지만)",
                offers.Count > 0 && offers.All(c => c.toTown != ArabiaRegionSetup.AhsaTown && System.Array.IndexOf(ArabiaRegionSetup.TownIds, c.toTown) >= 0),
                $"{offers.Count}건 → {string.Join(", ", offers.Select(c => c.toTown).Distinct())}");

            Check("저장하기 (전용 파일)", GM.SaveNow());
            Check("전용 저장에 Q2 완료 기록", ReadArabiaSave().Contains(q2.id));
            Check("본편 저장 파일 그대로 (Play 중)", ArabiaRegionSetup.FileHash(MainSavePath) == SessionState.GetString(MainHashKey, ""));
            Phase = 4;
            EditorApplication.isPlaying = false;
        }
    }
}
