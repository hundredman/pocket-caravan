using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

#if UNITY_EDITOR
namespace PocketCaravan
{
    /// <summary>
    /// 베네치아 플레이 점검 로봇 (2026-10-27, 개발용·에디터에서만 컴파일). VeniceFeatureCheck.TourRunAndQuit가 Play 중에 붙인다.
    /// Input System에 실제 키 입력(R·G·F·P·Space·방향키·Esc)을 넣어 사람이 하듯 눌러 보고, 결과를 글과 화면으로 남긴다.
    /// 저장 파일은 건드리지 않는다(시작 맵이 아닌 씬에서 Play). 끝나면 Done.
    /// </summary>
    public class VeniceTourBot : MonoBehaviour
    {
        public static readonly StringBuilder Report = new StringBuilder();
        public static bool Done;
        public static string OutDir = "Logs/VeniceFeatureCheck";

        Keyboard kb;
        InputSettings.BackgroundBehavior oldBg;
        InputSettings.EditorInputBehaviorInPlayMode oldEditor;
        GameManager gm => GameManager.I;
        PlayerController P => PlayerController.Instance;
        static int shot;

        void Log(string s) { Report.AppendLine(s); Debug.Log("[투어] " + s); }
        void Ok(bool ok, string what) => Log((ok ? "  ✔ " : "  ✘ ") + what);

        IEnumerator Start()
        {
            Done = false; Report.Clear(); shot = 0;
            oldBg = InputSystem.settings.backgroundBehavior;
            oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            kb = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            yield return Run();
            InputSystem.settings.backgroundBehavior = oldBg;
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor;
            Done = true;
        }

        // ---------------- 입력 ----------------
        void Keys(params Key[] keys) => InputSystem.QueueStateEvent(kb, new KeyboardState(keys));
        IEnumerator Press(Key k, int times = 1)
        {
            for (int i = 0; i < times; i++)
            {
                Keys(k); yield return null; yield return null;
                Keys(); yield return null; yield return null;
            }
        }
        IEnumerator Hold(Key k, float seconds)
        {
            Keys(k);
            float until = Time.unscaledTime + seconds;
            while (Time.unscaledTime < until) yield return null;
            Keys(); yield return null;
        }
        IEnumerator Wait(float s) { float until = Time.unscaledTime + s; while (Time.unscaledTime < until) yield return null; }
        IEnumerator Shot(string name) { yield return null; ScreenCapture.CaptureScreenshot($"{OutDir}/tour_{++shot:00}_{name}.png"); yield return null; yield return null; }

        static object Field(object o, string f) => o?.GetType().GetField(f, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)?.GetValue(o);
        static object SField(System.Type t, string f) => t.GetField(f, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)?.GetValue(null);

        IEnumerator Travel(string map, string spawn = null)
        {
            if (spawn == null) { var d = VeniceData.Get(map); spawn = d != null && d.spawn_ids != null && d.spawn_ids.Length > 0 ? d.spawn_ids[0] : "S0"; }
            gm.TravelTo(map, spawn);
            yield return null;
            while (gm.IsTravelling) yield return null;
            yield return Wait(.6f);
        }
        void SetClock(int day, int hour) { gm.Clock.day = day; gm.Clock.minuteOfDay = hour * 60 + 5; }

        /// <summary>대상 옆(아래 칸)에 서서 대상을 바라본다</summary>
        IEnumerator Face(Component target)
        {
            var map = MapRoot.Current;
            var c = map.WorldToCell(target.transform.position);
            foreach (var d in new[] { Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.up })
            {
                var stand = c + d;
                if (!map.IsWalkable(stand)) continue;
                P.Motor.Teleport(map.CellCenter(stand), new Vector2Int(-d.x, -d.y));
                break;
            }
            yield return Wait(.3f);
        }

        void CloseAll()
        {
            var d = FindAnyObjectByType<DialogueUI>();
            if (d != null && (bool)Field(d, "open")) typeof(DialogueUI).GetMethod("Close", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(d, null);
            UIState.Release();
        }

        // ---------------- 점검 ----------------
        IEnumerator Run()
        {
            Log("[베네치아 플레이 점검 (키 입력)]");
            gm.Player.money = 3000;
            SetClock(5, 9);
            yield return Travel("venice_rialto", "S0");
            yield return Common();
            yield return Rialto();
            yield return Murano();
            yield return Burano();
            yield return Chioggia();
            yield return SantErasmo();
            yield return Mainland();
            Log("[끝]");
        }

        IEnumerator Common()
        {
            Log("■ 공통");
            Ok(VeniceSave.Has("keyhint"), "1 키 안내를 한 번 보여 줌 (keyhint 기록)");
            // 2 동작 바퀴
            var rx = gm.GetComponent<VeniceReactions>();
            yield return Press(Key.R);
            Ok((bool)Field(rx, "wheel"), "2 R로 동작 바퀴가 열림");
            int known = 0; for (int i = 0; i < 10; i++) if (VeniceReactions.Knows(i)) known++;
            Ok(known == 3, $"2 처음 아는 동작 {known}개 (기대 3)");
            yield return Press(Key.RightArrow, 3);
            Ok((int)Field(rx, "sel") == 3, $"2 잠긴 칸으로 커서 이동 (sel {(int)Field(rx, "sel")})");
            yield return Shot("wheel_locked");
            yield return Press(Key.Space);
            Ok((bool)Field(rx, "wheel"), "2 잠긴 동작은 Space로 실행되지 않음 (바퀴 그대로)");
            yield return Press(Key.Escape);
            Ok(!(bool)Field(rx, "wheel") && !UIState.Blocking, "2 Esc로 닫힘");
            // 2 동작 → 걸으면 멈춤
            yield return Press(Key.R); yield return Press(Key.LeftArrow, 3); yield return Press(Key.Space);   // sel 0 인사
            yield return Wait(.2f);
            int before = ((IList)Field(rx, "bodies")).Count;
            yield return Hold(Key.D, .35f);
            int after = 0; foreach (var b in (IList)Field(rx, "bodies")) if (!(bool)Field(b, "npc")) after++;
            Ok(before > 0 && after == 0, $"2 동작 중 걸으면 멈춤 (동작 {before} → 내 동작 {after})");
            // 2 주민 반응
            var clerk = FindByName("상인회 서기");
            if (clerk != null)
            {
                yield return Face(clerk);
                yield return Press(Key.R); yield return Press(Key.RightArrow, 2); yield return Press(Key.Space);   // 박수
                yield return Wait(.5f);
                int says = ((IList)Field(rx, "says")).Count;
                Ok(says > 0, $"2 주민이 한마디 (말풍선 {says})");
                yield return Shot("npc_reaction");
            }
            // 3 친구 수첩·사진
            yield return Wait(1.5f);
            yield return Press(Key.F);
            var so = gm.GetComponent<VeniceSocial>();
            Ok((bool)Field(so, "book"), "3 F로 친구 수첩 열림");
            yield return Shot("friend_book");
            yield return Press(Key.F);
            Ok(!(bool)Field(so, "book"), "3 F로 친구 수첩 닫힘");
            var photos = Path.Combine(Application.persistentDataPath, "Photos");
            int n0 = Directory.Exists(photos) ? Directory.GetFiles(photos).Length : 0;
            yield return Press(Key.P);
            yield return Wait(1.5f);
            int n1 = Directory.Exists(photos) ? Directory.GetFiles(photos).Length : 0;
            Ok(n1 > n0, $"3 P 사진 파일 저장 ({n0}→{n1}, {photos})");
            yield return Shot("photo_preview");
            // 5 다음 이야기 줄
            yield return Wait(1f);
            yield return Shot("guide_line");
            // 8 말소리·웅성거림
            var snd = gm.GetComponent<VeniceSound>();
            var crowd = Field(snd, "crowd") as AudioSource;
            Ok(crowd != null && crowd.volume > 0f, $"8 낮 장터 근처 웅성거림 (소리 {crowd?.volume:0.00})");
            if (clerk != null)
            {
                yield return Face(clerk);
                yield return Press(Key.Space);
                yield return Wait(.15f);
                Ok((float)Field(snd, "babbleLeft") > 0f || (bool)Field(snd, "wasOpen"), "8 대화창이 열리면 말소리");
                yield return Shot("dialogue_babble");
                for (int i = 0; i < 6 && UIState.Blocking; i++) yield return Press(Key.Space);
                CloseAll();
            }
        }

        IEnumerator Rialto()
        {
            Log("■ 리알토");
            // 1 집 둘러보기
            foreach (var p in new[] { new Vector2Int(26, 30), new Vector2Int(66, 40), new Vector2Int(44, 54) })
            {
                var data = VeniceData.Get("venice_rialto");
                var w = VeniceUtil.Cell(p.x, p.y);
                if (MapRoot.Current.IsWalkable(MapRoot.Current.WorldToCell(w))) P.Motor.Teleport(w, Vector2Int.down);
                yield return Wait(.5f);
                yield return Shot($"rialto_view_{p.x}_{p.y}");
            }
            // 5 친밀도: 하트 표시, G 선물 고르기, 같은 날 두 번째 막힘
            var inn = FindByName("여관 주인");
            if (inn != null)
            {
                gm.Player.cargo.Add(VeniceUtil.Item("venice_wine"), 2);
                gm.Player.cargo.Add(VeniceUtil.Item("venice_salt"), 1);
                yield return Face(inn);
                yield return Press(Key.Space);                        // 말 걸기 +1
                for (int i = 0; i < 6 && UIState.Blocking; i++) yield return Press(Key.Space);
                CloseAll();
                Ok(VeniceSocial.Points("여관 주인") == 1, $"5 하루 첫 말 걸기 +1 (점수 {VeniceSocial.Points("여관 주인")})");
                yield return Shot("hearts_overhead");
                yield return Press(Key.G);
                var so = gm.GetComponent<VeniceSocial>();
                Ok((bool)Field(so, "picking"), "5 G로 선물 고르기 창");
                yield return Shot("gift_picker");
                yield return Press(Key.Space);
                yield return Wait(.3f);
                Ok(VeniceSocial.Points("여관 주인") == 6, $"5 좋아하는 포도주를 맨 앞에 두고 Space로 줌 (점수 {VeniceSocial.Points("여관 주인")}, 기대 6)");
                yield return Face(inn);
                yield return Press(Key.G);
                Ok(!(bool)Field(so, "picking"), "5 같은 날 두 번째 선물은 막힘");
                yield return Shot("gift_blocked");
            }
            // 2 여관
            var bed = FindAnyObjectByType<VeniceInn>();
            Ok(bed != null, "2 여관 주인 옆 침대");
            if (bed != null)
            {
                yield return Face(bed);
                yield return Press(Key.Space);           // 대화창
                yield return Wait(.2f);
                yield return Press(Key.Space);           // 첫 선택지 '쉬어 가기'
                yield return Wait(1f);
                yield return Shot("inn_sleep");
                yield return Wait(2.2f);
                Ok(VeniceDaily.Rested && !UIState.Blocking, $"2 쉬고 깸, 푹 쉼 {VeniceDaily.Rested}, 시계 {gm.Clock.Label}");
            }
            // 3 벤치
            var seats = FindObjectsByType<VeniceSeat>(FindObjectsSortMode.None);
            if (seats.Length > 0)
            {
                P.Motor.Teleport(seats[0].transform.position + new Vector3(0, -1f, 0), Vector2Int.up);
                yield return Wait(.3f);
                yield return Press(Key.Space);
                yield return Wait(4.5f);
                Ok(VeniceSeat.IsSitting && VeniceSeat.Breather, $"3 앞에서 Space로 앉음, 한숨 돌림 {VeniceSeat.Breather}");
                yield return Shot("bench_sit");
                yield return Hold(Key.A, .2f);
                Ok(!VeniceSeat.IsSitting, "3 방향키로 일어남");
            }
            else Ok(false, "3 벤치 없음");
            // 4 곤돌라 나루: 무거우면 거절, 메뉴, 경주
            var dock = FindAnyObjectByType<VeniceTraghetto>();
            if (dock != null)
            {
                var salt = VeniceUtil.Item("venice_salt");
                while (gm.Player.LoadRatio <= .7f) gm.Player.cargo.Add(salt, 1);
                yield return Face(dock);
                yield return Press(Key.Space);
                yield return Wait(.2f);
                Ok(!VeniceMenu.IsOpen, "4 짐이 7할 넘으면 태워 주지 않음");
                yield return Shot("dock_refuse");
                CloseAll();
                while (gm.Player.cargo.Count(salt) > 0) gm.Player.cargo.Remove(salt, 1);
                yield return Face(dock);
                yield return Press(Key.Space);
                yield return Wait(.2f);
                Ok(VeniceMenu.IsOpen && VeniceMenu.Labels.Length == 6, $"4 나루 메뉴 {VeniceMenu.Labels.Length}줄: {string.Join(" / ", VeniceMenu.Labels)}");
                yield return Shot("dock_menu");
                yield return Press(Key.Space);           // 첫 줄: 곤돌라 경주
                yield return RaceBot();
            }
            // 8 떠돌이 상인 (5일째)
            var mer = FindAnyObjectByType<VeniceMerchant>();
            Ok(mer != null, $"8 5일째 떠돌이 상인이 리알토에 ({VeniceDaily.MerchantMap})");
            if (mer != null)
            {
                yield return Face(mer);
                yield return Press(Key.Space);
                yield return Wait(.2f);
                yield return Shot("merchant_menu");
                int m0 = gm.Player.money;
                yield return Press(Key.Space);           // 특가 1개
                Ok(gm.Player.money < m0, $"8 특가 사기 ({m0}→{gm.Player.money})");
                yield return Face(mer); yield return Press(Key.Space); yield return Wait(.1f);
                yield return Press(Key.DownArrow); yield return Press(Key.Space);   // 동작 비법서
                int known = 0; for (int i = 0; i < 10; i++) if (VeniceReactions.Knows(i)) known++;
                Ok(known >= 4, $"8 동작 비법서로 배움 (아는 동작 {known})");
                yield return Face(mer); yield return Press(Key.Space); yield return Wait(.1f);
                yield return Press(Key.DownArrow, 2); yield return Press(Key.Space);   // 사자 소문
                yield return Wait(.3f);
                yield return Shot("merchant_rumor");
                CloseAll();
            }
            // 7 카니발
            SetClock(12, 20);
            yield return Travel("venice_rialto", "S0");
            var seller = FindAnyObjectByType<VeniceMaskSeller>();
            Ok(seller != null, "7 카니발 밤 가면 장수");
            if (seller != null)
            {
                yield return Face(seller);
                yield return Press(Key.Space);
                Ok(VeniceSave.Has("react:mask"), "7 Space로 가면 춤 배움");
                yield return Wait(.5f);
                yield return Shot("carnival");
                CloseAll();
            }
            SetClock(13, 10);
        }

        IEnumerator RaceBot()
        {
            var race = gm.GetComponent<VeniceRace>();
            yield return Wait(.2f);
            Ok((bool)Field(race, "running"), "4 Space로 곤돌라 경주 시작");
            float shotAt = Time.unscaledTime + 6f; bool took = false;
            Key held = Key.None;
            while ((bool)Field(race, "running"))
            {
                // 앞(아래로 내려오는 물건 중 배 위 120px 안)에 있는 막힘을 피해, 구슬·동전 쪽으로
                float boatX = (float)Field(race, "boatX");
                var things = (IList)Field(race, "things");
                float best = boatX, bestScore = float.MinValue;
                for (float x = 24; x <= 196; x += 8)
                {
                    float s = -Mathf.Abs(x - boatX) * .05f;
                    foreach (var t in things)
                    {
                        float ty = (float)Field(t, "y"), tx = (float)Field(t, "x"), tw = (float)Field(t, "w");
                        if (ty < 260 - 52 - 140 || ty > 260 - 18) continue;
                        bool over = x + 7 > tx - 3 && x - 7 < tx + tw + 3;
                        var kind = Field(t, "kind").ToString();
                        if (!over) continue;
                        s += kind == "Bead" ? 3 : kind == "Coin" ? 2 : -20;
                    }
                    if (s > bestScore) { bestScore = s; best = x; }
                }
                Key want = best < boatX - 3 ? Key.LeftArrow : best > boatX + 3 ? Key.RightArrow : Key.None;
                if (want != held) { if (want == Key.None) Keys(); else Keys(want); held = want; }
                if (!took && Time.unscaledTime > shotAt) { took = true; yield return Shot("race_play"); }
                yield return null;
            }
            Keys();
            yield return Wait(.5f);
            yield return Shot("race_result");
            Ok(true, $"4 경주 결과 (로봇이 조종): 최고 {VeniceRace.Best}점, 부딪힘 {(int)Field(race, "hits")} — 150점이면 명예 사공 {VeniceSave.Has("race_honor")}");
            yield return Wait(.9f);
            yield return Press(Key.Space);
            Ok(!UIState.Blocking, "4 Space로 결과 닫힘");
        }

        IEnumerator Murano()
        {
            Log("■ 무라노");
            SetClock(7, 8);                                  // 아쿠아 알타
            yield return Travel("venice_murano");
            yield return Shot("murano_view");
            Ok(VeniceEvents.AcquaAlta && gm.GetComponent<VeniceEvents>().PuddleCount > 0, $"5 아쿠아 알타 물 칸 {gm.GetComponent<VeniceEvents>().PuddleCount}");
            var d = VeniceData.Get("venice_murano");
            var pts = VeniceData.Points(d.flood);
            P.Motor.Teleport(VeniceUtil.Cell(d.market[0], d.market[1] + 2), Vector2Int.down);   // 마른 땅에서 먼저 잰다
            yield return Wait(.3f);
            float w0 = P.Motor.walkSpeed;
            P.Motor.Teleport(VeniceUtil.Cell(pts[pts.Count / 2].x, pts[pts.Count / 2].y), Vector2Int.down);
            yield return Wait(.3f);
            float w1 = P.Motor.walkSpeed;
            P.Motor.Teleport(VeniceUtil.Cell(d.market[0], d.market[1] + 2), Vector2Int.down);
            yield return Wait(.3f);
            float w2 = P.Motor.walkSpeed;
            Ok(w1 < w0 && Mathf.Approximately(w2, w0), $"5 물 찬 칸 걸음 {w0:0.00}→{w1:0.00}, 나오면 {w2:0.00}");
            yield return Shot("acqua_alta");
            SetClock(8, 10);                                 // 8일째: 떠돌이 상인 무라노
            yield return Travel("venice_murano");
            Ok(FindAnyObjectByType<VeniceMerchant>() != null, "6 8일째 떠돌이 상인");
            // 2 유리 불기 (바늘이 금빛 칸 가운데 올 때 Space)
            var glass = FindAnyObjectByType<VeniceGlassSpot>();
            if (glass != null)
            {
                yield return Face(glass);
                yield return Press(Key.Space); yield return Wait(.2f);
                yield return Press(Key.Space);           // 첫 선택지 '불어 보기'
                yield return Wait(.3f);
                int guard = 0;
                while ((bool)Field(glass, "playing") && (float)Field(glass, "resultUntil") <= 0f && guard++ < 3000)
                {
                    float needle = (float)Field(glass, "needle"), zone = (float)Field(glass, "zone"), zw = (float)Field(glass, "zoneW");
                    if (Mathf.Abs(needle - zone) < zw * .3f) { yield return Press(Key.Space); yield return Wait(.15f); }
                    else yield return null;
                }
                Ok((int)Field(glass, "hits") >= 2, $"2 유리 불기 타이밍 맞추기: {(int)Field(glass, "hits")}/3");
                yield return Shot("glass_result");
                yield return Wait(2.5f);
            }
            // 4 유리 장인 친밀도 (좋아하는 것: 과일 상자)
            var gm2 = FindByName("유리 장인");
            if (gm2 != null)
            {
                gm.Player.cargo.Add(VeniceUtil.Item("venice_fruit"), 1);
                yield return Face(gm2);
                yield return Press(Key.G); yield return Press(Key.Space);
                Ok(VeniceSocial.Points("유리 장인") == 5, $"4 유리 장인에게 과일 상자 (점수 {VeniceSocial.Points("유리 장인")})");
            }
        }

        IEnumerator Burano()
        {
            Log("■ 부라노");
            SetClock(6, 10);
            yield return Travel("venice_burano");
            yield return Shot("burano_view");
            Ok(FindAnyObjectByType<VeniceMerchant>() != null, "5 6일째 떠돌이 상인");
            var net = FindAnyObjectByType<VeniceNetSpot>();
            if (net != null)
            {
                yield return Face(net);
                yield return Press(Key.Space); yield return Wait(.2f);
                yield return Press(Key.Space);
                yield return Wait(.3f);
                int guard = 0;
                while ((bool)Field(net, "playing") && (float)Field(net, "resultUntil") <= 0f && guard++ < 3000)
                {
                    float power = (float)Field(net, "power"), fish = (float)Field(net, "fish"), flash = (float)Field(net, "flashUntil");
                    if (Time.unscaledTime > flash && Mathf.Abs(power - fish) < .03f) { yield return Press(Key.Space); yield return Wait(.1f); }
                    else yield return null;
                }
                Ok((int)Field(net, "caught") >= 2, $"2 그물 던지기 타이밍: {(int)Field(net, "caught")}/3");
                yield return Shot("net_result");
                yield return Wait(2.5f);
            }
            foreach (var name in new[] { "부라노 어부", "레이스 뜨는 아주머니" })
                Ok(FindByName(name) != null, $"4 {name} 있음");
        }

        IEnumerator Chioggia()
        {
            Log("■ 키오자");
            SetClock(4, 10);
            yield return Travel("venice_chioggia");
            yield return Shot("chioggia_view");
            Ok(FindAnyObjectByType<VeniceMerchant>() != null, "4 4일째 떠돌이 상인");
            var dock = FindAnyObjectByType<VeniceTraghetto>();
            var ferry = FindByName("본토행 뱃사공");
            Ok(dock != null && ferry != null && Vector3.Distance(dock.transform.position, ferry.transform.position) > 4f,
               $"2 곤돌라 나루와 본토행 배가 떨어져 있음 ({(dock != null && ferry != null ? Vector3.Distance(dock.transform.position, ferry.transform.position) : -1):0.0}칸)");
            // 곤돌라로 리알토 왕복 시험 (메뉴 둘째 줄)
            if (dock != null)
            {
                foreach (var kv in new List<KeyValuePair<ItemData, int>>(gm.Player.cargo.Items)) gm.Player.cargo.Remove(kv.Key, kv.Value);   // 짐을 비우고 탄다
                yield return Face(dock);
                yield return Press(Key.Space); yield return Wait(.2f);
                string labels = string.Join(" / ", VeniceMenu.Labels);
                yield return Press(Key.Escape);
                Ok(labels.Contains("리알토"), $"곤돌라 메뉴: {labels}");
            }
        }

        IEnumerator SantErasmo()
        {
            Log("■ 산테라스모");
            SetClock(2, 10);
            yield return Travel("venice_santerasmo");
            yield return Shot("santerasmo_view");
            Ok(FindAnyObjectByType<VeniceMerchant>() != null, "3 2일째 떠돌이 상인");
            Ok(FindByName("과수원 일꾼") != null, "2 과수원 일꾼 있음");
        }

        IEnumerator Mainland()
        {
            Log("■ 본토 서부");
            foreach (var (map, day) in new[] { ("venice_c_landing", 3), ("venice_c_vineyard", 7), ("venice_c_post", 1) })
            {
                SetClock(day, 10);
                yield return Travel(map);
                yield return Shot(map + "_view");
                Ok(FindAnyObjectByType<VeniceMerchant>() != null, $"{MapRoot.Current.displayName}: {day}일째 떠돌이 상인");
                var dir = FindAnyObjectByType<RoadEventDirector>();
                Ok(true, $"{MapRoot.Current.displayName}: 길 사건 자리 {(dir != null ? dir.events.Count : 0)}");
                if (map == "venice_c_post") Ok(FindAnyObjectByType<VeniceInn>() != null, "대상 숙소: 숙소 주인 옆 침대");
            }
            // 밤 도적 (나루터 직행로)
            SetClock(3, 23);
            yield return Travel("venice_c_landing");
            Ok(FindObjectsByType<BanditSpot>(FindObjectsSortMode.None).Length > 0, $"나루터: 도적 자리 {FindObjectsByType<BanditSpot>(FindObjectsSortMode.None).Length}");
            yield return Shot("landing_night");
            // 순례자: 비 오는 날 습지 → 고갯길 → 돌다리 → 숙소
            for (int d = 1; d < 60; d++)
            {
                bool found = false;
                for (int h = 6; h < 20; h++) { SetClock(d, h); if (VeniceEvents.LostWeather && !VeniceEvents.AcquaAlta) { found = true; break; } }
                if (found) break;
            }
            foreach (var map in new[] { "venice_c_marsh", "venice_c_pass", "venice_c_bridge", "venice_c_gate" })
            {
                yield return Travel(map);
                var lost = FindAnyObjectByType<VeniceLostTraveler>();
                if (map == "venice_c_gate") Ok(lost == null, $"{MapRoot.Current.displayName}: 같은 날씨에 두 번째 순례자는 나오지 않음");
                else Ok(lost != null, $"{MapRoot.Current.displayName}: {VeniceAtmosphere.Now} 날 순례자");
                yield return Shot(map + "_view");
                if (map == "venice_c_bridge" && lost != null)
                {
                    yield return Face(lost);
                    yield return Press(Key.Space); yield return Wait(.2f);
                    yield return Press(Key.Space);           // 따라오세요
                    yield return Wait(.3f);
                    Ok(VeniceEvents.Following, "순례자가 따라옴");
                    yield return Hold(Key.A, .6f);
                    yield return Shot("pilgrim_follow");
                    int m0 = gm.Player.money, c0 = gm.Player.credit;
                    yield return Travel("venice_c_post");
                    Ok(!VeniceEvents.Following && gm.Player.money - m0 == 40 && gm.Player.credit - c0 == 1, $"대상 숙소에 데려다줌: 돈 +{gm.Player.money - m0}, 신용 +{gm.Player.credit - c0}");
                }
                if (map == "venice_c_gate")
                {
                    var guard = FindByName("동문 경비");
                    Ok(guard != null, "동문: 경비 있음");
                }
            }
        }

        static Interactable FindByName(string name)
        {
            VeniceLabels.ShowAll = true;                 // 멀리 있는 장소도 이름으로 찾는다 (장소 이름표는 평소 가까이에서만 뜬다)
            try { foreach (var it in Interactable.All) if (it != null && it.DisplayName == name) return it; }
            finally { VeniceLabels.ShowAll = false; }
            return null;
        }
    }
}
#endif
