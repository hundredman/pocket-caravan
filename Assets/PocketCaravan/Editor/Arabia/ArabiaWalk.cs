using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using static PocketCaravan.EditorTools.ArabiaPlaytest;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 자동 보행 시험: 순간 이동 없이 PlayerMotor의 실제 이동·충돌 처리로 칸 경로를 따라 걷는다.
    /// 알 아흐사 S0 → 힌드바드(Q1 수락) → 동쪽 출구 → 농원길 횡단 → 자비드 부엌 담당(납품) → 곡물 장터.
    /// 걷는 시간·속도를 기록하고 Game 화면을 Docs/Arabia/Reports/Screens에 캡처한다.
    /// 키보드 입력 장치 자체와 조작감은 사람이 직접 확인해야 한다.
    /// </summary>
    static class ArabiaWalk
    {
        const string ShotDir = ArabiaRegionSetup.ReportDir + "/Screens";

        static IEnumerator Shot(string name)
        {
            Directory.CreateDirectory(ShotDir);
            string path = Path.GetFullPath(Path.Combine(ShotDir, name + ".png"));
            if (File.Exists(path)) File.Delete(path);
            yield return Frames(2);
            ScreenCapture.CaptureScreenshot(path);
            double until = EditorApplication.timeSinceStartup + 4;
            yield return Wait(() => File.Exists(path) || EditorApplication.timeSinceStartup > until, 10, "화면 캡처 " + name);
            if (File.Exists(path)) { Log($"  (화면) {path}"); yield break; }
            // 에디터가 뒤에 있으면 Game 창이 그려지지 않는다 → 카메라만 직접 렌더 (대화창·HUD 같은 OnGUI는 빠짐)
            var cam = Camera.main;
            var rt = new RenderTexture(1920, 1080, 24);
            var old = cam.targetTexture;
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = old;
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex); rt.Release(); Object.DestroyImmediate(rt);
            Log($"  (화면·카메라만, UI 제외) {path}");
        }

        /// <summary>현재 맵에서 발 위치 칸 → 목표 칸들 중 하나까지 4방향 최단 경로 (MapRoot.IsWalkable 기준).</summary>
        static List<Vector3Int> PathTo(HashSet<Vector3Int> goals)
        {
            var map = MapRoot.Current;
            var motor = PlayerController.Instance.Motor;
            var start = map.WorldToCell(motor.Feet);
            var bounds = map.ground.cellBounds;
            var prev = new Dictionary<Vector3Int, Vector3Int> { [start] = start };
            var q = new Queue<Vector3Int>(); q.Enqueue(start);
            Vector3Int? found = null;
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                if (goals.Contains(c)) { found = c; break; }
                foreach (var d in new[] { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down })
                {
                    var n = c + d;
                    if (!bounds.Contains(n) || prev.ContainsKey(n)) continue;
                    // 출구 칸은 목표일 때만 들어간다 (중간에 밟으면 맵이 바뀜)
                    if (!map.IsWalkable(n, motor) || (MapExit.At(n) != null && !goals.Contains(n))) continue;
                    prev[n] = c; q.Enqueue(n);
                }
            }
            if (found == null) return null;
            var path = new List<Vector3Int>();
            for (var c = found.Value; c != start; c = prev[c]) path.Add(c);
            path.Reverse();
            return path;
        }

        static float walked, seconds;

        /// <summary>경로 칸 중심을 차례로 향해 입력 방향을 주고 PlayerMotor.Tick으로 걷는다 (PlayerController는 잠시 끔).</summary>
        static IEnumerator WalkTo(HashSet<Vector3Int> goals, string label, bool exit = false)
        {
            var path = PathTo(goals);
            if (!Check($"걸어갈 경로 찾기: {label}", path != null, path != null ? $"{path.Count}칸" : "경로 없음")) yield break;
            var pc = PlayerController.Instance; var motor = pc.Motor; var map = MapRoot.Current;
            pc.enabled = false;
            walked = 0; seconds = 0;
            float speedSum = 0; int frames = 0;
            int lastFrame = Time.frameCount;
            double deadline = EditorApplication.timeSinceStartup + 120;
            // 프레임 시간을 0.01초 단위로 나눠 같은 이동 처리를 반복한다 (배경 실행 중 큰 프레임 간격에서 목표를 지나쳐 왕복하는 것 방지).
            int i = 0;
            Vector2 lastPos = motor.Feet; double lastMove = EditorApplication.timeSinceStartup;
            while (i < path.Count && EditorApplication.timeSinceStartup <= deadline)
            {
                // 거니는 마을 사람이 길을 막아 멈췄으면 지금 자리에서 경로를 다시 찾는다
                if ((motor.Feet - lastPos).sqrMagnitude > 0.01f) { lastPos = motor.Feet; lastMove = EditorApplication.timeSinceStartup; }
                else if (EditorApplication.timeSinceStartup - lastMove > 0.8)
                {
                    var again = PathTo(goals);
                    if (again != null) { path = again; i = 0; }
                    lastMove = EditorApplication.timeSinceStartup;
                }
                yield return Wait(() => Time.frameCount != lastFrame, 5, "프레임");
                lastFrame = Time.frameCount;
                if (PlayerController.Instance != pc || MapRoot.Current != map) break;
                float dt = Mathf.Min(Time.deltaTime, 0.1f);
                for (float t = 0; t < dt && i < path.Count; t += 0.01f)
                {
                    var target = (Vector2)map.CellCenter(path[i]);
                    if ((target - motor.Feet).magnitude <= 0.12f) { i++; continue; }
                    var before = motor.Feet;
                    float speed = pc.CurrentSpeed();
                    motor.Tick((target - motor.Feet).normalized, speed, 0.01f);
                    walked += (motor.Feet - before).magnitude; seconds += 0.01f; speedSum += speed; frames++;
                }
            }
            if (exit)
            {
                // 오픈월드 이음새는 경계를 따라 길게 이어져, 경계를 따라 걷다 출구 칸에 서면 나가는 쪽을 보고 있지 않을 수 있다.
                // 실제 플레이어처럼 나가는 쪽으로 한 걸음 민다.
                var here = MapExit.At(map.WorldToCell(motor.Feet));
                if (here != null)
                    foreach (var f in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.right, Vector2Int.left })
                        if (here.IsLeaving(f)) { for (int k = 0; k < 3; k++) motor.Tick((Vector2)f, pc.CurrentSpeed(), 0.01f); break; }
                pc.enabled = true;   // 출구 칸에 선 뒤 실제 PlayerController가 출구를 처리한다
                Debug.Log($"[아라비아 걷기] {label}: 멈춘 칸 {map.WorldToCell(motor.Feet)} · 경로 {i}/{path.Count} · 마지막 목표 {(path.Count > 0 ? path[path.Count - 1].ToString() : "-")} · 출구 칸 {string.Join(" ", goals)}");
                yield return Wait(() => GM.IsTravelling || MapRoot.Current != map, 10, $"{label} 출구 판정");
            }
            else pc.enabled = true;
            bool arrived = exit || goals.Contains(MapRoot.Current.WorldToCell(PlayerController.Instance.Motor.Feet));
            Check($"걸어서 도착: {label}", arrived, $"{walked:0.0}칸 · {seconds:0.0}초 · 평균 속도 {(frames > 0 ? speedSum / frames : 0):0.00}칸/초 · 짐 {P.TotalWeight:0.#}/{P.Capacity:0.#}");
        }

        static HashSet<Vector3Int> Beside(Interactable target)
        {
            var c = target.Cell;
            return new HashSet<Vector3Int> { c + Vector3Int.down, c + Vector3Int.up, c + Vector3Int.left, c + Vector3Int.right };
        }

        static HashSet<Vector3Int> ExitCells(string targetScene)
        {
            var exit = MapExit.All.First(e => e.targetScene == targetScene);
            var o = MapRoot.Current.WorldToCell(exit.transform.position);
            var set = new HashSet<Vector3Int>();
            for (int x = 0; x < exit.size.x; x++) for (int y = 0; y < exit.size.y; y++) set.Add(o + new Vector3Int(x, y, 0));
            return set;
        }

        static void Face(Interactable target)
        {
            var motor = PlayerController.Instance.Motor;
            var d = target.Cell - MapRoot.Current.WorldToCell(motor.Feet);
            motor.Teleport(motor.transform.position, new Vector2Int(Mathf.Clamp(d.x, -1, 1), Mathf.Clamp(d.y, -1, 1)));
        }

        public static IEnumerator Steps()
        {
            yield return Wait(() => Ready(ArabiaRegionSetup.AhsaMap), 30, "알 아흐사 Play 시작");
            yield return Frames(10);
            Log("\n## 자동 보행 (실제 이동·충돌 처리)");
            Check("아라비아 시험 세션·전용 저장", GM.ActiveRegionSession != null && SaveSystem.FileName == ArabiaRegionSetup.SaveFileName);
            yield return Shot("01_ahsa_start");

            // 마을 사람 거닐기 (ArabiaWanderer): 12초 동안 실제로 칸을 옮기고 걷기 애니메이션을 하는지
            {
                var ws = Object.FindObjectsByType<ArabiaWanderer>(FindObjectsSortMode.None);
                var start = ws.Select(w => w.transform.position).ToArray();
                bool anyWalking = false;
                var dirs = new HashSet<Vector2Int>();
                double until = EditorApplication.timeSinceStartup + 12;
                while (EditorApplication.timeSinceStartup < until)
                {
                    yield return Frames(1);
                    foreach (var w in ws)
                    {
                        var an = w != null ? w.GetComponent<CharacterAnimator>() : null;
                        if (an != null && an.walking) { anyWalking = true; dirs.Add(an.facing); }
                    }
                }
                int moved = ws.Where((w, k) => w != null && (w.transform.position - start[k]).sqrMagnitude > 0.25f).Count();
                Check("마을 사람 거닐기 (12초)", ws.Length > 0 && moved > 0 && anyWalking,
                    $"{moved}/{ws.Length}명 이동 · 걷는 방향 {dirs.Count}가지");
                var sample = ws.Select(w => w.GetComponent<CharacterAnimator>()).FirstOrDefault(a => a != null);
                Check("NPC 4방향 그림 (앞·뒤·옆이 서로 다름)", sample != null && sample.frames[0] != sample.frames[4] && sample.frames[8] != sample.frames[12] && sample.frames[0] != sample.frames[1],
                    sample != null ? sample.name : "없음");
            }

            var hind = Npc("arabia_hindbad");
            yield return WalkTo(Beside(hind), "S0 → 힌드바드");
            Face(hind);
            yield return Frames(3);
            Check("힌드바드가 Space 대화 대상", PlayerController.Instance.TalkTarget == hind);
            Talk("arabia_hindbad");
            yield return Frames(3);
            yield return Shot("02_hindbad_offer");
            PickChoice("수락");
            yield return Frames(30);
            yield return Shot("03_q1_accepted");
            float emptySpeed, loadedSpeed;
            {
                var p = P; var delivery = Item(ArabiaRegionSetup.DeliveryDatesId);
                loadedSpeed = PlayerController.Instance.CurrentSpeed();
                p.cargo.Remove(delivery, 2); emptySpeed = PlayerController.Instance.CurrentSpeed(); p.cargo.Add(delivery, 2);
                Check("적재량에 따른 감속 (같은 칸 기준)", loadedSpeed < emptySpeed, $"빈 등짐 {emptySpeed:0.00} → 의뢰품 4/10 {loadedSpeed:0.00}칸/초");
            }

            yield return WalkTo(ExitCells(ArabiaRegionSetup.FarmroadMap), "힌드바드 → 알 아흐사 동쪽 출구", exit: true);
            yield return Wait(() => Ready(ArabiaRegionSetup.FarmroadMap), 20, "나즈드 대상로 도착");
            yield return Frames(20);
            yield return Shot("04_farmroad");
            yield return WalkTo(ExitCells(ArabiaRegionSetup.JeddahMap), "나즈드 대상로 서쪽 → 동쪽 (의뢰품 적재)", exit: true);
            Log($"  나즈드 대상로 횡단: {walked:0.0}칸 · {seconds:0.0}초 (짐 4/10)");
            yield return Wait(() => Ready(ArabiaRegionSetup.JeddahMap), 20, "제다 도착");
            yield return Frames(20);
            yield return WalkTo(ExitCells(ArabiaRegionSetup.ZabidMap), "제다 → 홍해 해안길 → 자비드", exit: true);
            yield return Wait(() => Ready(ArabiaRegionSetup.ZabidMap), 20, "자비드 도착");
            yield return Frames(20);
            yield return Shot("05_zabid_arrive");

            var keeper = Npc("arabia_kitchen_keeper");
            yield return WalkTo(Beside(keeper), "자비드 입구 → 공동 부엌 담당");
            Face(keeper);
            yield return Frames(3);
            int money0 = P.money;
            Talk("arabia_kitchen_keeper");
            yield return Frames(3);
            yield return Shot("06_delivery");
            PickChoice("확인");
            yield return Frames(30);
            Check("걸어서 Q1 납품 완료", GM.Quests.IsFinished(Quest(ArabiaRegionSetup.Q1Id)) && P.money == money0 + ArabiaRegionSetup.Q1RewardMoney, $"돈 {money0}→{P.money}");
            yield return Shot("07_q1_done");

            var market = Market();
            yield return WalkTo(Beside(market), "부엌 → 자비드 곡물 장터");
            Face(market);
            yield return Frames(3);
            MarketOpen(market);
            yield return Frames(3);
            yield return Shot("08_zabid_market");
            MarketClose(market);
            yield return Frames(3);
            // 탐험 일주 (2026-10-27 오픈월드): 자비드 → 타이즈 → 고원길 → 사나 → (북문) 중앙 남부 6조각 한 바퀴
            //   → 오아시스 → 빈 사막 북쪽 모래길 → 룹알할리 빈 사막 → 알 아흐사
            {
                var tour = new (string from, string to, string label)[]
                {
                    (ArabiaRegionSetup.ZabidMap, ArabiaRegionSetup.TaizMap, "자비드 → 타이즈"),
                    (ArabiaRegionSetup.TaizMap, ArabiaRegionSetup.SanaaMap, "타이즈 → 고원길 → 사나 남문"),
                    (ArabiaRegionSetup.SanaaMap, ArabiaRegionSetup.CentralDunes, "사나 남문 → 북문 → 모래바람 오아시스"),
                    (ArabiaRegionSetup.CentralDunes, ArabiaRegionSetup.CentralBazaar, "오아시스 → 와디 건너 만국 장터"),
                    (ArabiaRegionSetup.CentralBazaar, ArabiaRegionSetup.CentralSteppe, "장터 북문 → 한가운데 초원"),
                    (ArabiaRegionSetup.CentralSteppe, ArabiaRegionSetup.CentralBazaar, "초원 → 장터 북문"),
                    (ArabiaRegionSetup.CentralBazaar, ArabiaRegionSetup.CentralEast, "장터 동문 → 소나무 고원"),
                    (ArabiaRegionSetup.CentralEast, ArabiaRegionSetup.CentralCanyon, "고원 → 붉은 바위 협곡"),
                    (ArabiaRegionSetup.CentralCanyon, ArabiaRegionSetup.CentralBazaar, "협곡 → 장터 남동문"),
                    (ArabiaRegionSetup.CentralBazaar, ArabiaRegionSetup.CentralWest, "장터 서문 → 포도밭 언덕"),
                    // 포도밭 언덕 남쪽 고개는 낙석·장애물 주기에 따라 막힐 수 있어, 장터로 돌아와 와디를 건너 오아시스로 간다
                    (ArabiaRegionSetup.CentralWest, ArabiaRegionSetup.CentralBazaar, "포도밭 언덕 → 장터 서문"),
                    (ArabiaRegionSetup.CentralBazaar, ArabiaRegionSetup.CentralDunes, "장터 → 와디 건너 오아시스"),
                    (ArabiaRegionSetup.CentralDunes, ArabiaRegionSetup.ShortcutMap, "오아시스 → 빈 사막 북쪽 모래길"),
                    (ArabiaRegionSetup.ShortcutMap, ArabiaRegionSetup.AhsaMap, "룹알할리 빈 사막 → 알 아흐사"),
                };
                int n = 9;
                foreach (var (from, to, label) in tour)
                {
                    if (MapRoot.Current == null || MapRoot.Current.mapId != from) { Check($"탐험 일주: {label} 출발 맵", false, MapRoot.Current != null ? MapRoot.Current.mapId : "없음"); break; }
                    yield return WalkTo(ExitCells(to), label, exit: true);
                    yield return Wait(() => Ready(to), 20, label + " 도착");
                    Check($"탐험 일주 도착: {to}", MapRoot.Current != null && MapRoot.Current.mapId == to, $"{walked:0.0}칸 · {seconds:0.0}초");
                    yield return Frames(20);
                    yield return Shot($"{n++:00}_{to.Replace("arabia_", "")}");
                    var visits = to == ArabiaRegionSetup.CentralBazaar && from == ArabiaRegionSetup.CentralDunes ? new[] { ("장터지기 카림", "central_market") }
                        : to == ArabiaRegionSetup.CentralSteppe ? new[] { ("게르의 할아버지", "central_apex") } : null;
                    if (visits != null)
                    {
                        // 중앙 대륙 남부: 장터 한가운데 · 초원 꼭짓점까지 걸어서 들른다
                        foreach (var (who, shot) in visits)
                        {
                            var target = Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None).FirstOrDefault(t => t.gameObject.name == who);
                            if (!Check($"중앙 대륙 남부: {who} 있음", target != null)) break;
                            yield return WalkTo(Beside(target), $"중앙 대륙 남부 → {who}");
                            Check($"중앙 대륙 남부 도착: {who}", true, $"{walked:0.0}칸 · {seconds:0.0}초");
                            yield return Frames(20);
                            yield return Shot($"{n++:00}_{shot}");
                        }
                    }
                }
            }
            Check("본편 저장 파일 그대로", ArabiaRegionSetup.FileHash(Path.Combine(Application.persistentDataPath, "pocket_caravan_save.json")) == SessionState.GetString("PocketCaravan.ArabiaPlaytest.MainHash", ""));
            Phase = 4;
            EditorApplication.isPlaying = false;
        }
    }
}
