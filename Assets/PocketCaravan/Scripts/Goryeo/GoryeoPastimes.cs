using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 고려 놀이 3개 (2026-11-25, 점검 2차: 고려는 미니게임이 낚시 하나뿐이었다. 같은 날 3차에서 화면·연출을 다시 만들었다).
    /// 창과 규칙은 아라비아 놀거리 틀(ArabiaPastimes.Game)을 쓴다:
    /// 참가비 없음 · Esc는 손해 없이 닫힘 · 끝나면 Space로 닫기 · 보상은 하루 한 번 소액(또는 평판) · 최고 기록은 남는다.
    /// 1) 투호 (개경): 땅 위를 오가는 표시가 병 밑 노란 띠에 올 때 Space로 화살 던지기, 8번. 넣은 만큼 구경꾼 엽전 (하루 한 번).
    /// 2) 떡메치기 (예성 나루): 박자가 네모 칸에 들어올 때 Space로 내려치기. 빨간 손 박자(아주머니가 떡을 뒤집음)에는 치지 않기. 20초. 품삯 (하루 한 번).
    /// 3) 제기차기 (닭목): 제기가 발 높이에 왔을 때 Space. 떨어뜨리면 끝. 열 번 넘기면 마을 평판 (하루 한 번).
    /// 그림: 배경은 이미 있는 고려 건물·나무 그림을 모아 구운 것, 사람은 내 캐릭터와 장터 사람들, 소품(병·화살·떡메·떡·제기)만 새로 찍었다
    /// (Resources/Goryeo/Pastimes, 만드는 도구 Tools/ArtGen/goryeo_pastimes_art.py). 저장: 공용 메모 "goryeo:pastime:이름:day" · ":best".
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

        // 색 (고려 장터 그림에 맞춘 흙·나무·쪽빛)
        static readonly Color32 Ink = new Color32(58, 40, 36, 255), Skin = new Color32(244, 208, 170, 255), Cloth = new Color32(246, 242, 232, 255),
            Wood = new Color32(150, 108, 66, 255), WoodHi = new Color32(196, 152, 100, 255), Rice = new Color32(250, 246, 236, 255),
            Red = new Color32(214, 84, 72, 255), Gold = new Color32(246, 196, 72, 255), Green = new Color32(112, 176, 108, 255), Dust = new Color32(222, 204, 176, 255);

        // 그리기 기준점 흔들림 (쿵 할 때 화면이 살짝 떨린다)
        static float ox, oy;
        static void F(float x, float y, float w, float h, Color32 c) => CuteUI.Fill(new Rect(Mathf.Round(x + ox), Mathf.Round(y + oy), Mathf.Round(w), Mathf.Round(h)), c);

        static readonly Dictionary<string, Sprite> art = new Dictionary<string, Sprite>();
        static Sprite Art(string path)
        {
            Sprite sp;
            if (!art.TryGetValue(path, out sp) || sp == null) art[path] = sp = Resources.Load<Sprite>("Goryeo/" + path);
            return sp;
        }
        static void Put(Sprite sp, float x, float bottom, float scale, bool flipX, float squashX = 1f, float squashY = 1f)
        {
            if (sp == null) return;
            float w = sp.rect.width * scale * squashX, h = sp.rect.height * scale * squashY;
            CuteUI.Sprite(sp, new Rect(Mathf.Round(x - w / 2f + ox), Mathf.Round(bottom - h + oy), Mathf.Round(w), Mathf.Round(h)), new Rect(0, 0, 1, 1), flipX);
        }
        /// <summary>고려 그림(Resources/Goryeo/…)을 2배로. x는 가운데, bottom은 발밑</summary>
        static void Pic(string path, float x, float bottom, float scale = 2f, bool flipX = false) => Put(Art(path), x, bottom, scale, flipX);
        /// <summary>놀이 소품(Resources/Goryeo/Pastimes/…)</summary>
        static void Prop(string name, float x, float bottom, bool flipX = false, float sx = 1f, float sy = 1f) => Put(Art("Pastimes/" + name), x, bottom, 2f, flipX, sx, sy);
        const string Keeper = "Stalls/Keepers/";
        static bool Blink(int i) => ((int)(Time.unscaledTime * 6f) + i) % 2 == 0;
        static void Shadow(float x, float y, float w, byte a = 60)
        {
            var c = new Color32(60, 40, 40, a);
            F(x - w / 2f, y, w, 3, c); F(x - w / 2f + 3, y - 1, w - 6, 5, c);
        }
        /// <summary>계단식 픽셀 선 (떡메 자루)</summary>
        static void Line(float x0, float y0, float x1, float y1, float th, Color32 c)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)) / 2f));
            for (int i = 0; i <= n; i++) { float k = i / (float)n; F(Mathf.Lerp(x0, x1, k) - th / 2f, Mathf.Lerp(y0, y1, k) - th / 2f, th, th, c); }
        }

        /// <summary>
        /// 세 놀이가 함께 쓰는 무대: 배경 그림, 내 캐릭터, 구경꾼, 먼지·별 가루, 흔들림, "준비 → 시작!", 끝난 뒤 별 셋.
        /// </summary>
        public abstract class Stage : ArabiaPastimes.Game
        {
            protected const float Ready = 1.3f;                       // 시작 전 준비 시간 (이 동안은 입력을 받지 않는다)
            protected bool Playing => t >= Ready;
            protected float W = 352f, H = 132f;
            protected float Feet => H - 14f;                           // 앞줄 사람 발밑
            protected float Back => H - 34f;                           // 뒷줄 구경꾼 발밑
            protected int grade;                                       // 끝났을 때 별 0~3
            float shake, cheer;
            struct Bit { public Vector2 p, v; public float life, max, size; public Color32 c; }
            readonly List<Bit> bits = new List<Bit>();
            float lastDraw;

            protected void Fresh() { bits.Clear(); shake = cheer = 0f; grade = 0; lastDraw = Time.unscaledTime; }
            protected void Shake(float amount = 1f) { shake = Mathf.Max(shake, amount); }
            protected void Cheer(float seconds = .7f) { cheer = Mathf.Max(cheer, seconds); }
            protected bool Cheering => cheer > 0f;
            protected void Burst(float x, float y, int n, Color32 c, float speed = 70f, float up = 60f, float size = 3f)
            {
                for (int i = 0; i < n; i++)
                {
                    float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f), sp = UnityEngine.Random.Range(.4f, 1f) * speed;
                    float life = UnityEngine.Random.Range(.35f, .7f);
                    bits.Add(new Bit { p = new Vector2(x, y), v = new Vector2(Mathf.Cos(a) * sp, Mathf.Sin(a) * sp - up), life = life, max = life, size = size, c = c });
                }
            }
            protected void Confetti(float x, float y)
            {
                Burst(x, y, 6, Gold, 90f, 90f); Burst(x, y, 5, Red, 90f, 90f); Burst(x, y, 5, new Color32(120, 170, 214, 255), 90f, 90f);
            }

            /// <summary>배경을 깔고 흔들림·가루 시간을 한 프레임 진행한다 (Draw 맨 처음에 부른다)</summary>
            protected void Backdrop(Rect r, string bg)
            {
                W = r.width; H = r.height;
                if (Event.current != null && Event.current.type == EventType.Repaint)
                {
                    float dt = Mathf.Min(.05f, Time.unscaledTime - lastDraw); lastDraw = Time.unscaledTime;
                    shake = Mathf.Max(0f, shake - dt * 5f); cheer = Mathf.Max(0f, cheer - dt);
                    for (int i = bits.Count - 1; i >= 0; i--)
                    {
                        var b = bits[i]; b.life -= dt;
                        if (b.life <= 0f) { bits.RemoveAt(i); continue; }
                        b.v.y += 260f * dt; b.p += b.v * dt; bits[i] = b;
                    }
                }
                ox = oy = 0f;
                F(0, 0, W, H, new Color32(214, 226, 214, 255));
                var sp = Art("Pastimes/" + bg);
                if (sp != null) CuteUI.Sprite(sp, new Rect(Mathf.Round((W - sp.rect.width * 2f) / 2f), H - sp.rect.height * 2f, sp.rect.width * 2f, sp.rect.height * 2f), new Rect(0, 0, 1, 1), false);
                ox = shake > 0f ? Mathf.Round(Mathf.Sin(Time.unscaledTime * 80f) * 2f * shake) : 0f;
                oy = shake > 0f ? Mathf.Round(Mathf.Cos(Time.unscaledTime * 65f) * 2f * shake) : 0f;
            }

            /// <summary>내 캐릭터 (오른쪽을 보는 걷기 그림). step이 참이면 발을 내딛는 그림</summary>
            protected void Me(float x, float bottom, bool step = false, bool faceLeft = false)
            {
                var pc = PlayerController.Instance;
                var fr = pc != null && pc.anim != null ? pc.anim.frames : null;
                var sp = fr != null && fr.Length >= 16 ? fr[(faceLeft ? 2 : 3) * 4 + (step ? 1 : 0)] : null;
                Shadow(x, bottom - 2, 22);
                if (sp != null) Put(sp, x, bottom, 2f, false); else Pic(Keeper + "npc_resident_scholar", x, bottom, 2f, faceLeft);
            }
            /// <summary>구경꾼 한 사람: 평소엔 가만히 숨 쉬듯 까딱, 환호할 땐 번갈아 폴짝</summary>
            protected void Fan(string path, int i, float x, float bottom, bool flipX = false, float scale = 2f)
            {
                float hop = Cheering && Blink(i) ? 4f : !Cheering && ((int)(Time.unscaledTime * 1.6f) + i) % 5 == 0 ? 1f : 0f;
                Shadow(x, bottom - 2, 20 * scale / 2f, 46);
                Pic(path, x, bottom - hop, scale, flipX);
            }

            /// <summary>가루·준비 글씨·별 (Draw 맨 끝에 부른다)</summary>
            protected void Overlay()
            {
                foreach (var b in bits)
                {
                    var c = b.c; c.a = (byte)(b.life / b.max < .3f ? 120 : 255);   // 사라지기 직전에만 반투명 (단색 그림 캐시가 늘지 않게 두 단계)
                    F(b.p.x - b.size / 2f, b.p.y - b.size / 2f, b.size, b.size, c);
                }
                ox = oy = 0f;
                if (!Playing)
                {
                    bool go = t >= Ready - .45f;
                    var plate = new Rect(W / 2f - 44, H / 2f - 16, 88, 28);
                    CuteUI.Box(plate, 7, CuteUI.Paper, CuteUI.Ink, 1, 2);
                    CuteUI.LabelCenter(W / 2f, H / 2f - 2, go ? "시작!" : "준비…", go ? CuteUI.Up : CuteUI.Text, false, true);
                }
                if (finished)
                {
                    float since = Time.unscaledTime - finishedAt;
                    for (int i = 0; i < 3; i++)
                    {
                        float a = since - .15f - i * .22f; if (a < 0f) continue;
                        float pop = a < .16f ? 1.35f - a / .16f * .35f : 1f;      // 톡 튀어나왔다 제자리
                        Put(Art("Pastimes/" + (i < grade ? "star_on" : "star_off")), W / 2f + (i - 1) * 30f, 18f + 11f * pop, 2f, false, pop, pop);
                    }
                }
                CuteUI.Keycap(4, 4, "Space");
            }
            protected void Finale(float x, float y)
            {
                Finish();
                if (grade >= 1) { Cheer(2.5f); Confetti(x, y); if (grade >= 3) { Confetti(x - 60, y + 10); Confetti(x + 60, y + 10); } }
            }
            protected static readonly string[] Shouts = { "와!", "얼쑤!", "잘한다!", "좋다!" };
        }

        // ---------------- 1) 투호 ----------------

        public class Tuho : Stage
        {
            public override string Id => "tuho";
            public override string DisplayName => "투호";
            public override string Title => "투호";
            public override string Where => "개경 장터 마당";
            public override string Hint => "붉은 표시가 병 밑 노란 띠에 올 때 Space! 화살 여덟 대를 던집니다. 병은 던질 때마다 자리를 옮겨요.";
            const int Arrows = 8;
            int thrown, hits, combo; float aim, aimDir, pot, flyT, flyTo, wait, wobble, lean; bool flying, lastHit;
            readonly List<float> stuck = new List<float>();
            protected override string Status => $"남은 화살 {Arrows - thrown} · 넣음 {hits} · 최고 {Best(Id)}";
            protected override void Begin() { Fresh(); thrown = hits = combo = 0; flying = lastHit = false; wait = wobble = lean = 0f; stuck.Clear(); NextPot(); }
            void NextPot() { pot = UnityEngine.Random.Range(.3f, .95f); aim = 0f; aimDir = 1f; }
            float Speed => 0.75f + thrown * 0.09f;   // 갈수록 빨라진다
            float Window => Mathf.Lerp(.09f, .055f, thrown / (float)Arrows);
            float X(float v) => 78f + v * (W - 112f);
            float PotX => X(pot);
            protected override void Tick(float dt)
            {
                wobble = Mathf.Max(0f, wobble - dt * 2.5f); lean = Mathf.Max(0f, lean - dt * 4f);
                if (!Playing) return;
                if (wait > 0f) { wait -= dt; if (wait <= 0f) { if (thrown >= Arrows) End(); else NextPot(); } return; }
                if (flying)
                {
                    flyT += dt * 2.1f;
                    if (flyT >= 1f)
                    {
                        flying = false; thrown++;
                        lastHit = Mathf.Abs(flyTo - pot) <= Window;
                        if (lastHit)
                        {
                            hits++; combo++; wobble = 1f; Shake(.6f); Cheer(); Sfx.Play("coin", .5f);
                            Burst(PotX, Feet - 46, 10, Gold, 60f, 80f);
                            Pop(combo >= 2 ? $"쏙! {combo}연속" : "쏙!", new Vector2(PotX, Feet - 70), CuteUI.Up);
                            Pop(Shouts[UnityEngine.Random.Range(0, Shouts.Length)], new Vector2(118 + UnityEngine.Random.Range(0, 5) * 50, Back - 62), CuteUI.Text);
                        }
                        else
                        {
                            combo = 0; stuck.Add(flyTo); Sfx.Play("step", .45f);
                            Burst(X(flyTo), Feet - 2, 6, Dust, 40f, 30f);
                            Pop(flyTo < pot ? "짧다" : "길다", new Vector2(X(flyTo), Feet - 30), CuteUI.Down);
                        }
                        wait = .6f;
                    }
                    return;
                }
                aim += aimDir * Speed * dt;
                if (aim >= 1f) { aim = 1f; aimDir = -1f; } else if (aim <= 0f) { aim = 0f; aimDir = 1f; }
                if (InputReader.InteractPressed()) { flying = true; flyT = 0f; flyTo = aim; lean = 1f; Sfx.Play("open", .35f); }
            }
            void End()
            {
                bool rec = SetBest(Id, hits); grade = hits >= 7 ? 3 : hits >= 5 ? 2 : hits >= 3 ? 1 : 0;
                Finale(W / 2f, 40f);
                var gm = GameManager.I; int coins = hits >= 3 ? hits * 3 : 0;
                if (coins > 0 && !DoneToday(Id)) { gm.Player.money += coins; MarkToday(Id); result = $"여덟 대 중 {hits}대! 구경꾼들이 엽전 {coins}냥을 던져 줬다."; Sfx.Play("levelup", .6f); }
                else if (coins > 0) result = $"여덟 대 중 {hits}대! (오늘 엽전은 이미 받았다)";
                else result = $"여덟 대 중 {hits}대… 세 대는 넣어야 박수를 받는다.";
                if (rec) result += " 최고 기록!";
            }
            static readonly string[] Crowd = { Keeper + "npc_capital_peddler", Keeper + "npc_resident_silk", Keeper + "npc_wood_seller", Keeper + "npc_resident_boatman", "VillageLife/Trader/0" };
            protected override void Draw(Rect r)
            {
                Backdrop(r, "bg_tuho");
                for (int i = 0; i < Crowd.Length; i++) Fan(Crowd[i], i, 118 + i * 50, Back, i > 2);
                Pic("VillageLife/Art/dog_" + (Blink(0) ? "0" : "1"), W - 24, Back + 6, 1f, true);
                // 빗나가 땅에 꽂힌 화살
                foreach (float s in stuck) Prop("arrow_stuck", X(s), Feet + 4);
                // 병 밑 노란 띠: 여기에 표시가 올 때 던지면 들어간다
                float px = PotX + (wobble > 0f ? Mathf.Round(Mathf.Sin(Time.unscaledTime * 40f) * 2f * wobble) : 0f);
                if (!finished && Playing)
                {
                    float half = Window * (W - 112f);
                    F(PotX - half, Feet + 4, half * 2f, 4, new Color32(246, 196, 72, 200)); F(PotX - half, Feet + 8, half * 2f, 1, new Color32(190, 140, 50, 220));
                }
                // 병 (들어간 화살이 입 밖으로 삐죽)
                Shadow(PotX, Feet - 3, 34);
                for (int i = 0; i < hits; i++) { float ax = px - 5 + (i % 4) * 3 + (i / 4); F(ax, Feet - 60 - (i % 3) * 3, 2, 22, Wood); F(ax - 1, Feet - 62 - (i % 3) * 3, 4, 4, Red); }
                Prop("pot", px, Feet);
                // 던지는 나
                Me(30 + lean * 4f, Feet, lean > .3f);
                // 과녁 표시 (땅 위를 오간다)
                if (!flying && wait <= 0f && !finished && Playing) Prop("reticle", X(aim), Feet + 16);
                // 날아가는 화살: 올라갈 땐 위로, 내려올 땐 아래로 기운다
                if (flying)
                {
                    float x = Mathf.Lerp(46, X(flyTo), flyT), y = Feet - 34 - Mathf.Sin(flyT * Mathf.PI) * 62f;
                    Prop(flyT < .38f ? "arrow_up" : flyT < .62f ? "arrow_flat" : "arrow_down", x, y + 8);
                }
                // 남은 화살
                for (int i = 0; i < Arrows - thrown; i++) Prop("arrow_stuck", W - 10 - i * 10, 30);
                Overlay();
            }
        }

        // ---------------- 2) 떡메치기 ----------------

        public class RiceCake : Stage
        {
            public override string Id => "tteok";
            public override string DisplayName => "떡메치기";
            public override string Title => "떡메치기";
            public override string Where => "예성 나루 떡집 앞";
            public override string Hint => "초록 박자가 네모 칸에 들어올 때 Space로 내려치세요. 빨간 손 박자는 아주머니가 떡을 뒤집으니 치면 안 돼요!";
            const float Length = 20f, NoteSpeed = 120f;
            int good, bad, combo, k; float swing, flinch, flip; 
            readonly List<float> times = new List<float>(); readonly List<bool> hands = new List<bool>();
            protected override string Status => $"잘 침 {good} · 실수 {bad} · 최고 {Best(Id)}";
            protected override void Begin()
            {
                Fresh(); good = bad = combo = k = 0; swing = flinch = flip = 0f; times.Clear(); hands.Clear();
                float at = Ready + 1.1f; int i = 0; bool prev = false;
                while (at < Ready + Length)
                {
                    bool hand = !prev && i >= 3 && (i % 4 == 3 || (i > 8 && UnityEngine.Random.value < .2f));
                    times.Add(at); hands.Add(hand); prev = hand; i++;
                    at += Mathf.Lerp(.95f, .55f, (at - Ready) / Length);
                }
            }
            float CX => W / 2f;
            int Stage3 => good >= 14 ? 2 : good >= 6 ? 1 : 0;
            void Good(string text)
            {
                good++; combo++; Shake(.8f); Sfx.Play("bump", .7f);
                Burst(CX, Feet - 26, 9, Rice, 80f, 70f);
                Pop(text, new Vector2(72, 48), CuteUI.Up);
                if (combo > 0 && combo % 5 == 0) { Cheer(); Pop($"{combo}연속!", new Vector2(CX, Feet - 64), CuteUI.Up); }
            }
            void Bad(string text, bool ouch)
            {
                bad++; combo = 0;
                if (ouch) { flinch = 1f; Shake(1f); Sfx.Play("deny", .55f); } else Sfx.Play("step", .4f);
                Pop(text, new Vector2(72, 48), CuteUI.Down);
            }
            protected override void Tick(float dt)
            {
                swing = Mathf.Max(0f, swing - dt * 4.5f); flinch = Mathf.Max(0f, flinch - dt * 2.5f); flip = Mathf.Max(0f, flip - dt * 3f);
                if (!Playing) return;
                bool press = InputReader.InteractPressed();
                if (k < times.Count)
                {
                    float off = t - times[k];
                    if (press)
                    {
                        swing = 1f;
                        if (off < -.3f) { Sfx.Play("step", .3f); }                 // 박자가 아직 멀다: 헛손질 (점수는 그대로)
                        else
                        {
                            if (hands[k]) Bad("아이쿠!", true);
                            else if (Mathf.Abs(off) <= .13f) Good(Mathf.Abs(off) <= .05f ? "쿵! 딱 맞다" : "쿵!");
                            else Bad(off < 0f ? "이르다" : "늦다", false);
                            k++;
                        }
                    }
                    else if (off > .2f)
                    {
                        if (hands[k]) { good++; flip = 1f; Sfx.Play("open", .3f); Pop("척!", new Vector2(CX + 30, Feet - 50), CuteUI.Up); }
                        else Bad("놓쳤다", false);
                        k++;
                    }
                }
                else if (press) swing = 1f;
                if (k >= times.Count && t >= times[times.Count - 1] + .7f) End();
            }
            void End()
            {
                int score = Mathf.Max(0, good - bad); bool rec = SetBest(Id, score);
                grade = score >= 20 ? 3 : score >= 14 ? 2 : score >= 4 ? 1 : 0;
                Finale(CX, 40f);
                var gm = GameManager.I; int pay = score >= 14 ? 18 : score >= 8 ? 12 : score >= 4 ? 6 : 0;
                if (pay > 0 && !DoneToday(Id)) { gm.Player.money += pay; MarkToday(Id); result = $"잘 친 것 {good} · 실수 {bad}. 떡집에서 품삯 {pay}냥을 줬다."; Sfx.Play("levelup", .6f); }
                else if (pay > 0) result = $"잘 친 것 {good} · 실수 {bad}. (오늘 품삯은 이미 받았다)";
                else result = $"잘 친 것 {good} · 실수 {bad}… 박자를 다시 맞춰 보자.";
                if (rec) result += " 최고 기록!";
            }
            protected override void Draw(Rect r)
            {
                Backdrop(r, "bg_tteok");
                float cx = CX, fy = Feet;
                Fan(Keeper + "npc_resident_boatman", 0, 34, fy - 6); Fan(Keeper + "npc_paper_worker", 1, W - 36, fy - 6, true);
                Pic("VillageLife/Art/chicken_" + (Blink(0) ? "0" : "1"), W - 70, fy + 6, 1f, true);
                // 떡판과 떡 (잘 칠수록 매끈해진다)
                Shadow(cx, fy - 2, 84);
                Prop("board", cx, fy + 2);
                float sq = swing > .55f ? (swing - .55f) / .45f : 0f;
                Prop("tteok_" + Stage3, cx, fy - 12 + sq * 4f, flip > .5f, 1f + sq * .18f, 1f - sq * .3f);
                // 떡집 아주머니: 손 박자에 떡을 뒤집는다
                bool handNext = k < times.Count && hands[k];
                float until = handNext ? times[k] - t : 9f;
                float reach = handNext ? Mathf.Clamp01(1f - (until - .05f) / .4f) * (1f - flinch) : flip > 0f ? flip : 0f;
                Fan(Keeper + "npc_resident_ricecake", 2, cx + 64 + flinch * 6f, fy + 2 - flinch * 4f, true);
                if (reach > 0f)
                {
                    float hx = cx + 50 - reach * 34f;
                    F(hx + 4, fy - 30, cx + 54 - hx, 5, Cloth); F(hx + 4, fy - 25, cx + 54 - hx, 1, Ink); F(hx + 4, fy - 31, cx + 54 - hx, 1, Ink);
                    F(hx - 2, fy - 31, 7, 7, Skin); F(hx - 3, fy - 31, 1, 7, Ink);
                }
                // 메치는 나 + 떡메 (자루는 손에서 메 머리까지)
                Me(cx - 62, fy + 2, swing > .5f);
                float hxm = Mathf.Lerp(cx - 36, cx - 4, swing), hym = Mathf.Lerp(fy - 66, fy - 30, swing);
                Line(cx - 52, fy - 26, hxm, hym + 8, 3, Wood);
                Prop("mallet_head", hxm, hym + 18);
                // 박자 길: 오른쪽에서 흘러와 왼쪽 네모 칸에 닿는다
                var lane = new Rect(40, 6, W - 80, 30); float hitX = lane.x + 32;
                if (finished) { Overlay(); return; }
                CuteUI.Box(lane, 7, new Color32(255, 252, 244, 235), CuteUI.Ink, 1);
                bool nearNow = k < times.Count && Mathf.Abs(times[k] - t) <= .13f;
                var ring = nearNow ? (hands[k] ? Red : Green) : Ink;
                F(hitX - 15, lane.y + 2, 30, 2, ring); F(hitX - 15, lane.yMax - 4, 30, 2, ring); F(hitX - 15, lane.y + 2, 2, 26, ring); F(hitX + 13, lane.y + 2, 2, 26, ring);
                for (int i = k; i < times.Count; i++)
                {
                    float nx = hitX + (times[i] - t) * NoteSpeed;
                    if (nx > lane.xMax - 14) break;
                    if (nx < lane.x + 14) continue;
                    Prop(hands[i] ? "note_hand" : "note_hit", nx, lane.y + 28);
                }
                // 남은 시간 띠
                float left = Mathf.Clamp01(1f - (t - Ready) / Length);
                F(lane.x + 6, lane.yMax + 2, (lane.width - 12) * left, 3, Green); F(lane.x + 6 + (lane.width - 12) * left, lane.yMax + 2, (lane.width - 12) * (1f - left), 3, new Color32(58, 40, 36, 60));
                Overlay();
            }
        }

        // ---------------- 3) 제기차기 ----------------

        public class Jegi : Stage
        {
            public override string Id => "jegi";
            public override string DisplayName => "제기차기";
            public override string Title => "제기차기";
            public override string Where => "닭목 고갯마을 장터 마당";
            public override string Hint => "제기가 발 높이(초록 띠)에 내려왔을 때 Space! 떨어뜨리면 끝이에요. 찰수록 띠가 좁아집니다.";
            int kicks; float y, vy, x, vx, kickAnim; bool dropped;
            protected override string Status => $"찬 횟수 {kicks} · 최고 {Best(Id)}";
            protected override void Begin() { Fresh(); kicks = 0; y = .7f; vy = .9f; x = 0f; vx = 0f; kickAnim = 0f; dropped = false; }
            float Zone => Mathf.Lerp(.24f, .12f, Mathf.Clamp01(kicks / 30f));   // 발 높이 띠 (0 = 바닥)
            const float Top = 14f;
            float KX => W / 2f + x * 40f;
            protected override void Tick(float dt)
            {
                kickAnim = Mathf.Max(0f, kickAnim - dt * 5f);
                if (!Playing || dropped) return;
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
                        float jy = Feet - y * (Feet - Top);
                        Burst(KX + 6, jy, sweet ? 6 : 3, sweet ? Gold : Dust, 50f, 20f, 2f);
                        if (kicks % 5 == 0)
                        {
                            Cheer(1f); Confetti(KX, 50f); Sfx.Play("coin", .4f);
                            Pop(Shouts[(kicks / 5) % Shouts.Length], new Vector2(kicks % 10 == 0 ? 60 : W - 60, Feet - 62), CuteUI.Text);
                        }
                    }
                    else Sfx.Play("step", .25f);
                }
                if (y < 0f) { y = 0f; dropped = true; Sfx.Play("fail", .5f); Burst(KX, Feet, 8, Dust, 50f, 30f); End(); }
            }
            void End()
            {
                bool rec = SetBest(Id, kicks); grade = kicks >= 30 ? 3 : kicks >= 20 ? 2 : kicks >= 10 ? 1 : 0;
                Finale(W / 2f, 40f);
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
                Backdrop(r, "bg_jegi");
                float fy = Feet, cx = W / 2f, kx = KX;
                Fan(Keeper + "npc_capital_peddler", 0, 30, fy - 4); Fan(Keeper + "npc_resident_silk", 1, 62, fy);
                Fan(Keeper + "npc_paper_worker", 2, W - 34, fy - 2, true); Pic("VillageLife/Art/dog_" + (Blink(0) ? "0" : "1"), W - 76, fy + 6, 1f, true);
                // 찬 횟수 (하늘에 크게)
                if (!finished)
                {
                    var plate = new Rect(cx - 30, 8, 60, 26);
                    CuteUI.Box(plate, 7, new Color32(255, 252, 244, 235), CuteUI.Ink, 1);
                    CuteUI.LabelCenter(cx, 21, kicks + "번", kicks >= 10 ? CuteUI.Up : CuteUI.Text, false, true);
                }
                // 발 높이 띠: 제기가 들어오면 밝아진다
                float zh = Zone * (fy - Top);
                bool inZone = !dropped && Playing && vy < 0f && y <= Zone;
                if (!finished)
                {
                    F(kx - 34, fy - zh, 68, zh, inZone ? new Color32(150, 226, 130, 170) : new Color32(255, 252, 244, 110));
                    F(kx - 34, fy - zh, 68, 2, Green); F(kx - 34, fy - zh, 2, zh, Green); F(kx + 32, fy - zh, 2, zh, Green);
                }
                // 차는 나 (제기를 따라 좌우로 움직인다)
                Me(kx - 12, fy - (kickAnim > .5f ? 3 : 0), kickAnim > .2f);
                // 제기와 땅 위 그림자 (높을수록 작고 흐리게)
                float h01 = Mathf.Clamp01(y);
                Shadow(kx + 6, fy - 1, Mathf.Lerp(16, 8, h01), (byte)(Mathf.RoundToInt(Mathf.Lerp(80, 30, h01) / 10f) * 10));   // 10 단위 (단색 그림 캐시)
                float jy2 = fy - Mathf.Clamp(y, 0f, 1.15f) * (fy - Top);
                Prop(vy > 0f && !dropped ? "jegi_down" : "jegi_up", kx + 6, jy2 + 4);
                Overlay();
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
