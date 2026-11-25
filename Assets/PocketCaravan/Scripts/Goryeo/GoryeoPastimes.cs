using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 고려 놀이 3개 (2026-11-25, 점검 2차: 고려는 미니게임이 낚시 하나뿐이었다). 창과 규칙은 아라비아 놀거리 틀(ArabiaPastimes.Game)을 그대로 쓴다:
    /// 참가비 없음 · Esc는 손해 없이 닫힘 · 끝나면 Space로 닫기 · 보상은 하루 한 번 소액(또는 평판) · 최고 기록은 남는다.
    /// 1) 투호 (개경): 땅 위를 오가는 표시가 항아리에 올 때 Space로 화살 던지기, 8번. 넣은 만큼 구경꾼 엽전 (하루 한 번).
    /// 2) 떡메치기 (예성 나루): 고리가 떡판에 닿을 때 Space로 내려치기. 손이 들어온 박자("뒤집기")에는 치지 않기. 20초. 품삯 (하루 한 번).
    /// 3) 제기차기 (닭목): 제기가 발 높이에 왔을 때 Space. 떨어뜨리면 끝. 열 번 넘기면 마을 평판 (하루 한 번).
    /// 그림은 새 파일 없이 고려 장터에 이미 있는 사람·소품 그림(Resources/Goryeo)을 쓰고, 항아리·떡판·제기만 네모로 그린다. 저장: 공용 메모 "goryeo:pastime:이름:day" · ":best".
    /// </summary>
    public static class GoryeoPastimes
    {
        static readonly RegionMemo Memo = new RegionMemo("goryeo:pastime:");
        public static bool DoneToday(string id) => Memo.DoneToday($"{id}:day");
        static void MarkToday(string id) => Memo.MarkToday($"{id}:day");
        public static int Best(string id) => GameManager.I == null ? 0 : Mathf.RoundToInt(GameManager.I.GetMemo($"goryeo:pastime:{id}:best", 0f));
        static bool SetBest(string id, int score)
        {
            if (GameManager.I == null || score <= Best(id)) return false;
            GameManager.I.SetMemo($"goryeo:pastime:{id}:best", score);
            return true;
        }

        // 색 (고려 장터 그림에 맞춘 흙·기와·쪽빛)
        static readonly Color32 Sky = new Color32(238, 232, 210, 255), Ground = new Color32(206, 178, 132, 255), GroundLo = new Color32(176, 146, 104, 255),
            Ink = new Color32(64, 44, 32, 255), Skin = new Color32(244, 208, 170, 255), Cloth = new Color32(246, 242, 232, 255), Indigo = new Color32(70, 98, 150, 255),
            Clay = new Color32(122, 74, 52, 255), ClayHi = new Color32(168, 108, 76, 255), Wood = new Color32(150, 108, 66, 255), WoodHi = new Color32(196, 152, 100, 255),
            Rice = new Color32(250, 246, 236, 255), Red = new Color32(204, 72, 60, 255), Gold = new Color32(232, 184, 72, 255), Green = new Color32(96, 160, 96, 255);

        static void F(float x, float y, float w, float h, Color32 c) => CuteUI.Fill(new Rect(Mathf.Round(x), Mathf.Round(y), Mathf.Round(w), Mathf.Round(h)), c);

        static readonly Dictionary<string, Sprite> art = new Dictionary<string, Sprite>();
        /// <summary>고려 장터에 이미 있는 그림(Resources/Goryeo/…)을 그대로 쓴다. x는 가운데, bottom은 발밑</summary>
        static void Pic(string path, float x, float bottom, float scale = 2f, bool flipX = false)
        {
            Sprite sp;
            if (!art.TryGetValue(path, out sp) || sp == null) art[path] = sp = Resources.Load<Sprite>("Goryeo/" + path);
            if (sp == null) return;
            float w = sp.rect.width * scale, h = sp.rect.height * scale;
            CuteUI.Sprite(sp, new Rect(Mathf.Round(x - w / 2f), Mathf.Round(bottom - h), w, h), new Rect(0, 0, 1, 1), flipX);
        }
        const string Keeper = "Stalls/Keepers/";
        static bool Blink(int i) => ((int)(Time.unscaledTime * 8f) + i) % 2 == 0;

        // ---------------- 1) 투호 ----------------

        public class Tuho : ArabiaPastimes.Game
        {
            public override string Id => "tuho";
            public override string DisplayName => "투호";
            public override string Title => "투호";
            public override string Where => "개경 장터 마당";
            public override string Hint => "땅 위 표시가 항아리에 올 때 Space! 화살 여덟 대를 던집니다. 항아리는 던질 때마다 자리를 옮겨요.";
            const int Arrows = 8;
            int thrown, hits; float aim, aimDir, pot, flyT, flyTo, wait; bool flying, lastHit;
            protected override string Status => $"남은 화살 {Arrows - thrown} · 넣음 {hits} · 최고 {Best(Id)}";
            protected override void Begin() { thrown = hits = 0; flying = false; wait = 0f; NextPot(); }
            void NextPot() { pot = UnityEngine.Random.Range(.35f, .92f); aim = 0f; aimDir = 1f; }
            float Speed => 0.75f + thrown * 0.09f;   // 갈수록 빨라진다
            float Window => Mathf.Lerp(.09f, .055f, thrown / (float)Arrows);
            protected override void Tick(float dt)
            {
                if (wait > 0f) { wait -= dt; if (wait <= 0f) { if (thrown >= Arrows) End(); else NextPot(); } return; }
                if (flying)
                {
                    flyT += dt * 2.2f;
                    if (flyT >= 1f)
                    {
                        flying = false; thrown++;
                        lastHit = Mathf.Abs(flyTo - pot) <= Window;
                        if (lastHit) { hits++; Sfx.Play("coin", .5f); Pop("쏙!", new Vector2(PotX, lastH - 66), CuteUI.Up); }
                        else { Sfx.Play("step", .45f); Pop(flyTo < pot ? "짧다" : "길다", new Vector2(X(flyTo), lastH - 40), CuteUI.Down); }
                        wait = .55f;
                    }
                    return;
                }
                aim += aimDir * Speed * dt;
                if (aim >= 1f) { aim = 1f; aimDir = -1f; } else if (aim <= 0f) { aim = 0f; aimDir = 1f; }
                if (InputReader.InteractPressed()) { flying = true; flyT = 0f; flyTo = aim; Sfx.Play("open", .35f); }
            }
            float lastW = 320f, lastH = 130f;
            float X(float v) => 52f + v * (lastW - 84f);
            float PotX => X(pot);
            void End()
            {
                Finish(); bool rec = SetBest(Id, hits);
                var gm = GameManager.I; int coins = hits >= 3 ? hits * 3 : 0;
                if (coins > 0 && !DoneToday(Id)) { gm.Player.money += coins; MarkToday(Id); result = $"여덟 대 중 {hits}대! 구경꾼들이 엽전 {coins}냥을 던져 줬다."; Sfx.Play("levelup", .6f); }
                else if (coins > 0) result = $"여덟 대 중 {hits}대! (오늘 엽전은 이미 받았다)";
                else result = $"여덟 대 중 {hits}대… 세 대는 넣어야 박수를 받는다.";
                if (rec) result += " 최고 기록!";
            }
            protected override void Draw(Rect r)
            {
                lastW = r.width; lastH = r.height;
                float gy = r.height - 44, fy = r.height - 16;
                F(0, 0, r.width, r.height, Sky); F(0, gy, r.width, 44, Ground); F(0, gy, r.width, 2, GroundLo);
                // 구경꾼 (뒤쪽): 들어가면 폴짝
                bool cheer = lastHit && wait > 0f;
                for (int i = 0; i < Crowd.Length; i++) Pic(Crowd[i], 118 + i * 50, gy + 6 - (cheer && Blink(i) ? 3 : 0), 2f, i > 2);
                // 던지는 사람
                Pic(Keeper + "npc_resident_scholar", 26, fy - (flying ? 2 : 0));
                // 항아리 (목이 좁은 투호 병)
                float px = PotX;
                F(px - 9, fy - 20, 18, 20, Clay); F(px - 6, fy - 32, 12, 12, Clay); F(px - 8, fy - 34, 16, 3, ClayHi); F(px - 6, fy - 18, 3, 13, ClayHi);
                F(px - 13, fy - 29, 4, 6, Clay); F(px + 9, fy - 29, 4, 6, Clay);   // 귀
                // 표시 (땅 위를 오간다)
                if (!flying && wait <= 0f && !finished)
                {
                    float ax = X(aim);
                    F(ax - 5, fy + 7, 10, 3, Red); F(ax - 1, fy + 3, 2, 4, Red);
                }
                // 날아가는 화살
                if (flying)
                {
                    float x = Mathf.Lerp(40, X(flyTo), flyT), y = fy - 32 - Mathf.Sin(flyT * Mathf.PI) * 56f;
                    F(x - 6, y, 12, 2, Wood); F(x + 5, y - 1, 3, 4, Ink); F(x - 8, y - 1, 3, 4, Red);
                }
                // 꽂힌 화살 수
                for (int i = 0; i < hits; i++) F(px - 5 + (i % 4) * 3, fy - 45 - (i / 4) * 3, 1, 12, Wood);
                CuteUI.Keycap(4, 4, "Space");
            }
            static readonly string[] Crowd = { Keeper + "npc_capital_peddler", Keeper + "npc_resident_silk", Keeper + "npc_wood_seller", Keeper + "npc_resident_boatman", "VillageLife/Trader/0" };
        }

        // ---------------- 2) 떡메치기 ----------------

        public class RiceCake : ArabiaPastimes.Game
        {
            public override string Id => "tteok";
            public override string DisplayName => "떡메치기";
            public override string Title => "떡메치기";
            public override string Where => "예성 나루 떡집 앞";
            public override string Hint => "고리가 떡판에 닿을 때 Space로 내려치세요. 아주머니 손이 들어오는 박자에는 치면 안 돼요! 20초.";
            const float Length = 20f;
            int good, bad, beat; float phase, swing, flinch; bool hand, judged;
            protected override string Status => $"남은 {Mathf.Max(0, Length - t):0}초 · 잘 침 {good} · 실수 {bad} · 최고 {Best(Id)}";
            protected override void Begin() { good = bad = beat = 0; phase = 0f; swing = flinch = 0f; hand = false; judged = false; }
            float Period => Mathf.Lerp(.95f, .55f, t / Length);
            protected override void Tick(float dt)
            {
                phase += dt / Period; swing = Mathf.Max(0f, swing - dt * 5f); flinch = Mathf.Max(0f, flinch - dt * 2.5f);
                bool press = InputReader.InteractPressed();
                float off = Mathf.Abs(phase - 1f) * Period;   // 박자(phase = 1)까지 남은 시간
                if (press && !judged)
                {
                    swing = 1f; judged = true;
                    if (hand) { bad++; flinch = 1f; Sfx.Play("deny", .55f); Pop("아이쿠!", new Vector2(lastW * .5f + 30, 40), CuteUI.Down); }
                    else if (off <= .13f) { good++; Sfx.Play("bump", .7f); Pop(off <= .05f ? "쿵! 딱 맞다" : "쿵!", new Vector2(lastW * .5f, 40), CuteUI.Up); }
                    else { bad++; Sfx.Play("step", .4f); Pop(phase < 1f ? "이르다" : "늦다", new Vector2(lastW * .5f, 40), CuteUI.Down); }
                }
                if (phase >= 1.3f)
                {
                    if (!judged && !hand) { bad++; Pop("놓쳤다", new Vector2(lastW * .5f, 40), CuteUI.Down); }
                    if (!judged && hand) { good++; Pop("척!", new Vector2(lastW * .5f + 30, 40), CuteUI.Up); Sfx.Play("open", .3f); }
                    phase -= 1f; phase = Mathf.Max(0f, phase - .3f); judged = false; beat++;
                    hand = beat >= 3 && beat % 4 == 3 || (beat > 8 && UnityEngine.Random.value < .18f);
                }
                if (t >= Length) End();
            }
            float lastW = 320f, lastH = 130f;
            void End()
            {
                Finish(); int score = Mathf.Max(0, good - bad); bool rec = SetBest(Id, score);
                var gm = GameManager.I; int pay = score >= 14 ? 18 : score >= 8 ? 12 : score >= 4 ? 6 : 0;
                if (pay > 0 && !DoneToday(Id)) { gm.Player.money += pay; MarkToday(Id); result = $"잘 친 것 {good} · 실수 {bad}. 떡집에서 품삯 {pay}냥을 줬다."; Sfx.Play("levelup", .6f); }
                else if (pay > 0) result = $"잘 친 것 {good} · 실수 {bad}. (오늘 품삯은 이미 받았다)";
                else result = $"잘 친 것 {good} · 실수 {bad}… 박자를 다시 맞춰 보자.";
                if (rec) result += " 최고 기록!";
            }
            protected override void Draw(Rect r)
            {
                lastW = r.width; lastH = r.height;
                float gy = r.height - 34, fy = r.height - 14, cx = r.width * .5f;
                F(0, 0, r.width, r.height, Sky); F(0, gy, r.width, 34, Ground); F(0, gy, r.width, 2, GroundLo);
                Pic("VillageLife/Art/jars", 34, gy + 8, 1f); Pic("VillageLife/Art/chicken_" + (Blink(0) ? "0" : "1"), r.width - 30, fy + 4, 1f, true);
                // 떡판 + 떡
                F(cx - 34, fy - 12, 68, 12, Wood); F(cx - 34, fy - 12, 68, 2, WoodHi); F(cx - 28, fy, 6, 4, Wood); F(cx + 22, fy, 6, 4, Wood);
                float squash = swing * 3f;
                F(cx - 20 - squash, fy - 20 + squash, 40 + squash * 2, 9 - squash, Rice); F(cx - 16, fy - 22 + squash, 32, 3, Rice);
                // 메치는 사람 (왼쪽) + 떡메
                Pic(Keeper + "npc_wood_seller", cx - 62, fy + 2 - (swing > .5f ? 2 : 0));
                float my = fy - 58 + swing * 32f;
                F(cx - 50, my + 6, 34, 3, Wood); F(cx - 22, my, 14, 14, WoodHi); F(cx - 22, my, 14, 2, Wood);
                // 떡집 아주머니 (오른쪽): 손이 들어오는 박자
                Pic(Keeper + "npc_resident_ricecake", cx + 62 + flinch * 4f, fy + 2, 2f, true);
                if (hand)
                {
                    float reach = Mathf.Clamp01(phase * 1.6f) * (1f - flinch);
                    F(cx + 50 - reach * 36f, fy - 24, 8 + reach * 30f, 4, Cloth); F(cx + 48 - reach * 36f, fy - 25, 5, 5, Skin);
                    CuteUI.LabelCenter(cx + 62, fy - 64, "뒤집어요!", CuteUI.Down, true, true);
                }
                // 박자 고리: 위에서 내려와 떡판 줄에 닿는다
                float ringY = Mathf.Lerp(8f, fy - 24f, Mathf.Clamp01(phase));
                var col = hand ? Red : Green;
                F(cx - 46, fy - 25, 92, 1, hand ? Red : Ink);
                F(cx - 40, ringY, 80, 2, col); F(cx - 40, ringY - 3, 2, 8, col); F(cx + 38, ringY - 3, 2, 8, col);
                CuteUI.Keycap(4, 4, "Space");
            }
        }

        // ---------------- 3) 제기차기 ----------------

        public class Jegi : ArabiaPastimes.Game
        {
            public override string Id => "jegi";
            public override string DisplayName => "제기차기";
            public override string Title => "제기차기";
            public override string Where => "닭목 고갯마을 장터 마당";
            public override string Hint => "제기가 발 높이(초록 띠)에 내려왔을 때 Space! 떨어뜨리면 끝이에요. 찰수록 띠가 좁아집니다.";
            int kicks; float y, vy, x, vx, kickAnim; bool dropped;
            protected override string Status => $"찬 횟수 {kicks} · 최고 {Best(Id)}";
            protected override void Begin() { kicks = 0; y = .55f; vy = 1.25f; x = 0f; vx = 0f; kickAnim = 0f; dropped = false; }
            float Zone => Mathf.Lerp(.24f, .12f, Mathf.Clamp01(kicks / 30f));   // 발 높이 띠 (0 = 바닥)
            protected override void Tick(float dt)
            {
                if (dropped) return;
                kickAnim = Mathf.Max(0f, kickAnim - dt * 5f);
                float g = 2.6f + Mathf.Min(1.6f, kicks * .05f);
                vy -= g * dt; y += vy * dt; x += vx * dt;
                if (Mathf.Abs(x) > 1f) { x = Mathf.Sign(x); vx = -vx; }
                if (InputReader.InteractPressed())
                {
                    kickAnim = 1f;
                    if (vy < 0f && y <= Zone && y >= 0f)
                    {
                        kicks++;
                        bool sweet = y <= Zone * .55f;
                        vy = sweet ? 1.5f : 1.2f + UnityEngine.Random.Range(0f, .25f);
                        vx = UnityEngine.Random.Range(-.5f, .5f);
                        Sfx.Play("step", .5f);
                        Pop(sweet ? $"{kicks}!" : $"{kicks}", new Vector2(lastW * .5f + x * 40f, lastH - 60), CuteUI.Up);
                    }
                }
                if (y < 0f) { dropped = true; Sfx.Play("fail", .5f); End(); }
            }
            float lastW = 320f, lastH = 130f;
            void End()
            {
                Finish(); bool rec = SetBest(Id, kicks);
                if (kicks >= 10 && !DoneToday(Id))
                {
                    MarkToday(Id);
                    GoodDeeds.ForRegion(Reactions.Region.Goryeo, ReputationSize.Small, "goryeo:jegi");
                    result = $"{kicks}번! 마을 사람들이 손뼉을 쳤다. 마을에 좋은 소문이 났다."; Sfx.Play("levelup", .6f);
                }
                else if (kicks >= 10) result = $"{kicks}번! 마을 사람들이 또 손뼉을 쳤다.";
                else result = $"{kicks}번… 열 번은 차야 마을 사람들이 알아준다.";
                if (rec) result += " 최고 기록!";
            }
            protected override void Draw(Rect r)
            {
                lastW = r.width; lastH = r.height;
                float gy = r.height - 34, fy = r.height - 14, cx = r.width * .5f, top = 10f;
                F(0, 0, r.width, r.height, Sky); F(0, gy, r.width, 34, Ground); F(0, gy, r.width, 2, GroundLo);
                // 구경꾼: 찰 때마다 폴짝
                int hop = kickAnim > .5f ? 3 : 0;
                Pic(Keeper + "npc_capital_peddler", cx - 140, fy - 4 - hop); Pic(Keeper + "npc_resident_silk", cx - 106, fy);
                Pic(Keeper + "npc_paper_worker", cx + 134, fy - 2 - hop, 2f, true); Pic("VillageLife/Art/dog_" + (Blink(0) ? "0" : "1"), cx + 98, fy + 4, 1f, true);
                // 차는 사람
                float kx = cx + x * 40f;
                Pic("VillageLife/Trader/0", kx - 12, fy);
                if (kickAnim > 0f) F(kx - 5, fy - 5 - kickAnim * 9f, 8, 4, Ink);   // 올라가는 발
                // 발 높이 띠
                float zh = Zone * (fy - top);
                if (!finished) { F(cx - 70, fy - zh, 140, 1, Green); F(cx - 70, fy - zh, 2, zh, Green); F(cx + 68, fy - zh, 2, zh, Green); }
                // 제기 (엽전 + 술)
                float jy = fy - Mathf.Clamp(y, 0f, 1.2f) * (fy - top);
                F(kx - 3, jy - 3, 7, 4, Gold); F(kx - 1, jy - 2, 3, 2, Ink);
                F(kx - 4, jy - 9, 2, 6, Cloth); F(kx, jy - 11, 2, 8, Rice); F(kx + 3, jy - 9, 2, 6, Cloth);
                CuteUI.Keycap(4, 4, "Space");
            }
        }

        // ---------------- 자리 ----------------

        struct Place { public string map; public Type game; }
        static readonly Place[] Places =
        {
            new Place { map = "Gaegyeong", game = typeof(Tuho) },
            new Place { map = "Naru", game = typeof(RiceCake) },
            new Place { map = "Dakmok", game = typeof(Jegi) },
        };

        /// <summary>그 맵 장터에서 오른쪽 아래로 몇 칸 떨어진, 걸을 수 있고 비어 있는 칸 (장터가 없으면 길 가운데)</summary>
        static Vector3? SpotNearMarket(MapRoot mr)
        {
            Vector3Int from = default; bool found = false;
            foreach (var m in UnityEngine.Object.FindObjectsByType<TownMarket>(FindObjectsSortMode.None))
                if (m != null && m.gameObject.scene == mr.gameObject.scene) { from = mr.WorldToCell(m.transform.position); found = true; break; }
            if (!found)
            {
                if (mr.ground == null) return null;
                var b = mr.ground.cellBounds; from = new Vector3Int(Mathf.RoundToInt(b.center.x), Mathf.RoundToInt(b.center.y), 0);
            }
            var others = new List<Vector3Int>();
            foreach (var it in UnityEngine.Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None))
                if (it != null && it.gameObject.scene == mr.gameObject.scene) others.Add(mr.WorldToCell(it.transform.position));
            float best = float.MaxValue; Vector3Int bc = default; bool any = false;
            for (int dx = -9; dx <= 9; dx++)
                for (int dy = -8; dy <= 8; dy++)
                {
                    int far = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (far < 5) continue;   // 장터 바로 앞은 비워 둔다
                    var c = new Vector3Int(from.x + dx, from.y + dy, 0);
                    if (!Free(mr, c) || !Free(mr, c + Vector3Int.down) || !Free(mr, c + Vector3Int.left) || !Free(mr, c + Vector3Int.right)) continue;
                    float d = far + (dy > 0 ? 2f : 0f) + (dx < 0 ? .5f : 0f);
                    foreach (var o in others) { int od = Mathf.Abs(o.x - c.x) + Mathf.Abs(o.y - c.y); if (od <= 2) d += 6f; else if (od == 3) d += 2f; }   // 가게·사람 이름표와 겹치지 않게
                    if (d < best) { best = d; bc = c; any = true; }
                }
            return any ? mr.CellCenter(bc) : (Vector3?)null;
        }
        static bool Free(MapRoot mr, Vector3Int c) => mr.IsWalkable(c) && Interactable.At(c) == null;

        class Runner : MonoBehaviour
        {
            void OnEnable() { SceneManager.sceneLoaded += OnLoaded; }
            void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; }
            void OnLoaded(Scene s, LoadSceneMode m) => StartCoroutine(PlaceLater());
            System.Collections.IEnumerator PlaceLater()
            {
                yield return null;   // 장터·사람이 자리를 잡은 다음 프레임에
                var mr = MapRoot.Current;
                if (mr == null || GameManager.I == null) yield break;
                foreach (var p in Places)
                {
                    if (p.map != mr.mapId) continue;
                    var pos = SpotNearMarket(mr);
                    if (pos == null) { Debug.LogWarning($"[고려 놀이] {p.map}: 자리가 없다"); continue; }
                    var go = new GameObject("고려 놀이");
                    go.transform.SetParent(mr.transform, true);
                    go.transform.position = pos.Value;
                    go.AddComponent(p.game);
                    var t = Resources.Load<Texture2D>("Common/signpost");
                    if (t != null) { t.filterMode = FilterMode.Point; var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.1f), 16f); sr.sortingOrder = 9; }
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            ActivityHints.Rules[typeof(Tuho)] = _ => ("투호", DoneToday("tuho"));
            ActivityHints.Rules[typeof(RiceCake)] = _ => ("떡메치기", DoneToday("tteok"));
            ActivityHints.Rules[typeof(Jegi)] = _ => ("제기차기", DoneToday("jegi"));
            if (UnityEngine.Object.FindFirstObjectByType<Runner>() != null) return;
            var go = new GameObject("GoryeoPastimes");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
        }
    }
}
