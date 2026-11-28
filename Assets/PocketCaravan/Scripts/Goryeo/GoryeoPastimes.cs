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
    /// 1) 투호 (개경): Space를 꾹 눌러 옆의 힘 게이지를 채우고 떼면 던진다, 8번. 어디 떨어질지는 표시하지 않는다 (11/25 4차, 은샘). 넣은 만큼 구경꾼 엽전 (하루 한 번).
    /// 2) 떡메치기 (예성 나루): 박자가 네모 칸에 들어올 때 Space로 내려치기. 빨간 손 박자(아주머니가 떡을 뒤집음)에는 치지 않기. 20초. 품삯 (하루 한 번).
    /// 3) 제기차기 (닭목): 제기가 발 높이에 왔을 때 Space. 떨어뜨리면 끝. 열 번 넘기면 마을 평판 (하루 한 번).
    /// 시작 (11/25 4차, 은샘): 푯말 대신 투호는 마당의 항아리, 떡메치기는 떡집 아주머니, 제기차기는 아이에게 말을 걸면 시작한다.
    /// 그림: 배경은 이미 있는 고려 건물·나무 그림을 모아 구운 것, 사람은 내 캐릭터와 장터 사람들, 소품(병·화살·떡메·떡·제기)만 새로 찍었다
    /// (Resources/Goryeo/Pastimes, 만드는 도구 Tools/ArtGen/goryeo_pastimes_art.py). 저장: 공용 메모 "goryeo:pastime:이름:day" · ":best".
    /// </summary>
    public static partial class GoryeoPastimes
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
        /// <summary>놀이 소품을 가운데 기준으로 (돌려 구운 화살·떡메)</summary>
        static void PropC(string name, float cx, float cy)
        {
            var sp = Art("Pastimes/" + name); if (sp == null) return;
            Put(sp, cx, cy + sp.rect.height, 2f, false);
        }
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

            /// <summary>말을 걸어 시작하는 놀이면 그 사람 이름 (비우면 바로 시작)</summary>
            protected virtual string Greeter => null;
            protected virtual string Greeting => "";
            /// <summary>맵에 세울 그림 (Resources/Goryeo/…)</summary>
            public virtual string MapSprite => "Pastimes/pot";
            /// <summary>맵 그림을 칸 안에서 위로 올리는 높이 (앞에 소품을 둘 때)</summary>
            public virtual float MapLift => 0f;
            public struct Decor { public string name; public float x, y; public int order; }
            /// <summary>맵에서 곁에 놓는 소품 (지나가는 길은 막지 않는다)</summary>
            public virtual Decor[] MapDecor => new Decor[0];
            public override void Interact(PlayerController player)
            {
                if (string.IsNullOrEmpty(Greeter)) { base.Interact(player); return; }
                DialogueUI.Show(Greeter, Greeting, new DialogueUI.Choice("해 볼게요", () => Open(player)), new DialogueUI.Choice("다음에 할게요", null));
            }
            void Open(PlayerController player) => base.Interact(player);

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
            public override string DisplayName => "투호 항아리";
            public override string ActionVerb => "투호 하기";
            public override Decor[] MapDecor => new[] { new Decor { name = "arrow_stuck", x = -.62f, y = -.05f, order = 9 }, new Decor { name = "arrow_stuck", x = .58f, y = -.2f, order = 11 } };
            public override string Title => "투호";
            public override string Where => "개경 장터 마당";
            public override string Hint => "Space를 꾹 눌러 힘을 모으고, 떼면 던집니다. 힘이 셀수록 멀리 날아가요. 화살 여덟 대, 넣으면 병이 자리를 옮깁니다.";
            const int Arrows = 8;
            int thrown, hits, combo; float aim, aimDir, pot, flyT, flyTo, wait, wobble, lean, lastPower = -1f; bool flying, charging, lastHit, near;
#pragma warning disable 0649
            bool holdForTest;   // 자동 플레이 확인용 (에디터 점검 도구가 Space를 누르고 있는 것처럼 둔다)
#pragma warning restore 0649
            readonly List<float> stuck = new List<float>(), lying = new List<float>();
            protected override string Status => $"남은 화살 {Arrows - thrown} · 넣음 {hits} · 최고 {Best(Id)}";
            protected override void Begin()
            {
                Fresh(); thrown = hits = combo = 0; flying = charging = lastHit = near = false; wait = wobble = lean = aim = 0f; lastPower = -1f;
                stuck.Clear(); lying.Clear(); pot = UnityEngine.Random.Range(.45f, .65f);
            }
            void NextPot() { float was = pot; do pot = UnityEngine.Random.Range(.25f, .9f); while (Mathf.Abs(pot - was) < .18f); lastPower = -1f; }
            float Speed => 0.85f + thrown * 0.06f;   // 갈수록 게이지가 빨리 찬다
            float Window => Mathf.Lerp(.07f, .05f, thrown / (float)Arrows);
            float X(float v) => 96f + v * (W - 128f);
            float PotX => X(pot);
            const float HandX = 60f;
            Vector2 Fly(float k)
            {
                bool inPot = Mathf.Abs(flyTo - pot) <= Window;
                float ex = inPot ? PotX : X(flyTo), ey = inPot ? Feet - 44f : Feet - 4f, sy = Feet - 30f;
                return new Vector2(Mathf.Lerp(HandX, ex, k), Mathf.Lerp(sy, ey, k) - Mathf.Sin(k * Mathf.PI) * (44f + flyTo * 30f));
            }
            protected override void Tick(float dt)
            {
                wobble = Mathf.Max(0f, wobble - dt * 2.5f); lean = Mathf.Max(0f, lean - dt * 4f);
                if (!Playing) return;
                if (wait > 0f) { wait -= dt; if (wait <= 0f) { if (thrown >= Arrows) End(); else if (lastHit) NextPot(); } return; }
                if (flying)
                {
                    flyT += dt * 1.9f;
                    if (flyT >= 1f)
                    {
                        flying = false; thrown++;
                        float off = Mathf.Abs(flyTo - pot);
                        lastHit = off <= Window; near = !lastHit && off <= Window * 2.4f;
                        if (lastHit)
                        {
                            hits++; combo++; wobble = 1f; Shake(.6f); Cheer(); Sfx.Play("coin", .5f);
                            Burst(PotX, Feet - 46, 10, Gold, 60f, 80f);
                            Pop(combo >= 2 ? $"쏙! {combo}연속" : "쏙!", new Vector2(PotX, Feet - 70), CuteUI.Up);
                            Pop(Shouts[UnityEngine.Random.Range(0, Shouts.Length)], new Vector2(118 + UnityEngine.Random.Range(0, 5) * 50, Back - 62), CuteUI.Text);
                        }
                        else if (near)
                        {
                            combo = 0; wobble = .6f; Sfx.Play("bump", .5f);
                            lying.Add(X(flyTo) + (flyTo < pot ? -16f : 16f));
                            Pop("아깝다! 병에 맞았다", new Vector2(PotX, Feet - 70), CuteUI.Text);
                        }
                        else
                        {
                            combo = 0; stuck.Add(flyTo); Sfx.Play("step", .45f);
                            Burst(X(flyTo), Feet - 2, 6, Dust, 40f, 30f);
                            Pop(flyTo < pot ? "힘이 모자랐다" : "너무 셌다", new Vector2(X(flyTo), Feet - 30), CuteUI.Down);
                        }
                        wait = .7f;
                    }
                    return;
                }
                if (!charging)
                {
                    if (InputReader.InteractPressed()) { charging = true; aim = 0f; aimDir = 1f; Sfx.Play("click", .3f); }
                    return;
                }
                aim += aimDir * Speed * dt;
                if (aim >= 1f) { aim = 1f; aimDir = -1f; } else if (aim <= 0f) { aim = 0f; aimDir = 1f; }
                if (!(InputReader.SpaceHeld() || holdForTest))
                {
                    charging = false; flying = true; flyT = 0f; flyTo = aim; lastPower = aim; lean = 1f; Sfx.Play("open", .35f);
                }
            }
            void End()
            {
                bool rec = SetBest(Id, hits); grade = hits >= 6 ? 3 : hits >= 4 ? 2 : hits >= 2 ? 1 : 0;
                Finale(W / 2f, 40f);
                var gm = GameManager.I; int coins = hits >= 2 ? hits * 3 : 0;
                if (coins > 0 && !DoneToday(Id)) { gm.Player.money += coins; MarkToday(Id); result = $"여덟 대 중 {hits}대! 구경꾼들이 엽전 {coins}냥을 던져 줬다."; Sfx.Play("levelup", .6f); }
                else if (coins > 0) result = $"여덟 대 중 {hits}대! (오늘 엽전은 이미 받았다)";
                else result = $"여덟 대 중 {hits}대… 두 대는 넣어야 박수를 받는다.";
                if (rec) result += " 최고 기록!";
            }
            static readonly string[] Crowd = { Keeper + "npc_capital_peddler", Keeper + "npc_resident_silk", Keeper + "npc_wood_seller", Keeper + "npc_resident_boatman", "VillageLife/Trader/0" };
            protected override void Draw(Rect r)
            {
                Backdrop(r, "bg_tuho");
                for (int i = 0; i < Crowd.Length; i++) Fan(Crowd[i], i, 118 + i * 50, Back, i > 2);
                Pic("VillageLife/Art/dog_" + (Blink(0) ? "0" : "1"), W - 24, Back + 6, 1f, true);
                // 빗나간 화살: 땅에 꽂힌 것, 병에 맞고 누운 것
                foreach (float s in stuck) Prop("arrow_stuck", X(s), Feet + 4);
                foreach (float lx in lying) PropC("arrow_a4", lx, Feet - 2);
                // 병 (들어간 화살이 입 밖으로 삐죽)
                float px = PotX + (wobble > 0f ? Mathf.Round(Mathf.Sin(Time.unscaledTime * 40f) * 2f * wobble) : 0f);
                Shadow(PotX, Feet - 3, 34);
                for (int i = 0; i < hits; i++) { float ax = px - 5 + (i % 4) * 3 + (i / 4); F(ax, Feet - 60 - (i % 3) * 3, 2, 22, Wood); F(ax - 1, Feet - 62 - (i % 3) * 3, 4, 4, Red); }
                Prop("pot", px, Feet);
                // 던지는 나: 힘을 모을 땐 몸을 뒤로, 던질 땐 앞으로
                bool canThrow = !flying && wait <= 0f && !finished && Playing;
                float back = charging ? Mathf.Round(aim * 4f) : 0f;
                Me(46 - back + lean * 5f, Feet, lean > .3f);
                if (canThrow) PropC(charging ? "arrow_a1" : "arrow_a2", HandX - 4 - back * 1.5f, Feet - 26 - back);
                // 날아가는 화살: 날아가는 방향대로 기운다
                if (flying)
                {
                    Vector2 a = Fly(flyT), b = Fly(Mathf.Min(1f, flyT + .04f));
                    float deg = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
                    PropC("arrow_a" + Mathf.Clamp(Mathf.RoundToInt((deg + 60f) / 15f), 0, 9), a.x, a.y);
                }
                // 힘 게이지 (내 왼쪽): 누르고 있는 동안 차오른다. 지난번에 던진 힘은 작은 눈금으로 남는다
                if (!finished && Playing)
                {
                    float gx = 14f, gb = Feet + 2f, ih = 72f;
                    Prop("gauge", gx, gb);
                    if (charging)
                    {
                        float h = Mathf.Round(ih * aim);
                        F(gx - 8, gb - 4 - h, 16, h, aim < .34f ? Green : aim < .67f ? Gold : Red);
                        F(gx - 8, gb - 4 - h, 16, 2, Ink);
                    }
                    if (lastPower >= 0f) { float ly = gb - 4 - Mathf.Round(ih * lastPower); F(gx + 10, ly - 1, 5, 2, Ink); F(gx + 12, ly - 2, 3, 4, Ink); }
                    CuteUI.LabelCenter(gx + 2, gb - 90, "힘", CuteUI.Text, true, true);
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
            public override string DisplayName => "떡집 아주머니";
            public override Kind Category => Kind.Person;
            public override string MapSprite => Keeper + "npc_resident_ricecake";
            public override float MapLift => .3f;
            public override Decor[] MapDecor => new[] { new Decor { name = "map_tteok", x = 0f, y = -.08f, order = 11 } };   // 아주머니 앞에 놓인 떡판
            protected override string Greeter => "떡집 아주머니";
            protected override string Greeting => DoneToday(Id)
                ? "아까는 고마웠어요. 품삯은 드렸지만, 또 쳐 보고 싶으면 떡메는 저기 있어요."
                : "마침 잘 왔네요! 떡메 칠 사람이 없어서요.\n내가 떡을 뒤집을 땐 멈추고, 손을 빼면 쿵 하고 쳐 줘요. 품삯은 드릴게요.";
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
                        if (hands[k]) { good++; flip = 1f; Sfx.Play("open", .3f); Burst(CX + 8, Feet - 30, 5, new Color32(170, 210, 232, 255), 40f, 50f, 2f); Pop("척!", new Vector2(CX + 30, Feet - 56), CuteUI.Up); }
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
                // 떡집 아주머니: 떡판 오른쪽 끝 뒤에 서 있다가, 손 박자가 오면 판 쪽으로 다가와 허리를 숙이고 뒤집는다.
                // 떡판보다 먼저 그려서 판 뒤에 선 것으로 보이게 한다 (판 위에 올라선 것처럼 보이면 안 된다)
                bool handNext = k < times.Count && hands[k];
                float until = handNext ? times[k] - t : 9f;
                float reach = handNext ? Mathf.Clamp01(1f - (until - .05f) / .35f) * (1f - flinch) : flip > 0f ? flip : 0f;
                Fan(Keeper + "npc_resident_ricecake", 2, cx + 54 - Mathf.Round(reach * 14f) + flinch * 10f, fy - 6 + Mathf.Round(reach * 3f) - flinch * 4f, true);
                // 떡판(땅에 놓인 안반)과 떡: 잘 칠수록 매끈해지고, 맞는 순간 납작해진다
                Shadow(cx, fy - 1, 86);
                Prop("board", cx, fy + 2);
                float sq = swing > .55f ? (swing - .55f) / .45f : 0f;
                float turn = flip > 0f ? Mathf.Sin(flip * Mathf.PI) : 0f;        // 뒤집히는 동안 살짝 들렸다 놓인다
                Prop("tteok_" + Stage3, cx, fy - 8 + sq * 4f - turn * 5f, flip > .5f, 1f + sq * .18f, (1f - sq * .3f) * (1f - turn * .35f));
                // 메치는 나 + 떡메: 치켜든 자세에서 내려쳤다가 다시 올라간다 (자루째 돌려 구운 그림 4장)
                int mf = swing > .55f ? 3 : swing > .35f ? 2 : swing > .15f ? 1 : 0;
                Me(cx - 60, fy + 2, mf >= 2);
                PropC("mallet_" + mf, cx - (mf == 3 ? 49 : mf == 2 ? 50 : 52), fy - (mf == 3 ? 28 : mf == 2 ? 25 : 20));
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
            public override string DisplayName => "제기 차는 아이";
            public override Kind Category => Kind.Person;
            public override string MapSprite => "Pastimes/child";
            public override Decor[] MapDecor => new[] { new Decor { name = "jegi_up", x = .5f, y = -.05f, order = 11 } };
            protected override string Greeter => "제기 차는 아이";
            protected override string Greeting => DoneToday(Id)
                ? "또 차러 왔어요? 이번엔 내 기록도 넘어 봐요!"
                : "제기 찰 줄 알아요?\n열 번만 넘기면 마을 사람들이 다 알아줄걸요!";
            public override string Title => "제기차기";
            public override string Where => "닭목 고갯마을 장터 마당";
            public override string Hint => "제기가 발등 높이(발 옆 연한 띠)까지 내려왔을 때 Space! 떨어뜨리면 끝이에요. 찰수록 제기가 빨리 떨어집니다.";
            int kicks; float y, vy, x, vx, kickAnim; bool dropped;
            protected override string Status => $"찬 횟수 {kicks} · 최고 {Best(Id)}";
            protected override void Begin() { Fresh(); kicks = 0; y = .75f; vy = .6f; x = 0f; vx = 0f; kickAnim = 0f; dropped = false; }
            // 발등 높이 띠 (0 = 바닥, 놀이 칸에서 14픽셀쯤: 발목~무릎 아래). 11/25 5차 은샘: 띠가 허리까지 올라와 너무 높아 보였다.
            // 띠를 낮추는 대신 제기가 술 때문에 천천히 내려오게 해서(내려오는 빠르기에 한계) 누를 틈은 전과 비슷하다
            const float Zone = .135f;
            float Fall => Mathf.Lerp(.7f, 1.1f, Mathf.Clamp01(kicks / 30f));   // 내려오는 빠르기 한계 (찰수록 빨라진다)
            const float Top = 14f;
            float KX => W / 2f + x * 40f;
            protected override void Tick(float dt)
            {
                kickAnim = Mathf.Max(0f, kickAnim - dt * 5f);
                if (!Playing || dropped) return;
                vy = Mathf.Max(-Fall, vy - 3f * dt); y += vy * dt; x += vx * dt;
                if (Mathf.Abs(x) > 1f) { x = Mathf.Sign(x); vx = -vx; }
                if (InputReader.InteractPressed())
                {
                    kickAnim = 1f;
                    if (vy < 0f && y <= Zone && y >= 0f)
                    {
                        kicks++;
                        bool sweet = y <= Zone * .55f;
                        vy = sweet ? 1.95f : 1.6f + UnityEngine.Random.Range(0f, .25f);
                        vx = UnityEngine.Random.Range(-.5f, .5f);
                        Sfx.Play("step", .5f);
                        float jy = Feet - y * (Feet - Top);
                        Burst(KX + 9, jy, sweet ? 6 : 3, sweet ? Gold : Dust, 50f, 20f, 2f);
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
                Fan(Keeper + "npc_capital_peddler", 0, 30, fy - 4); Fan("Pastimes/child", 1, 62, fy);
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
                    F(kx - 4, fy - zh, 26, zh, inZone ? new Color32(150, 226, 130, 170) : new Color32(255, 252, 244, 90));
                    for (int i = 0; i < 4; i++) F(kx - 4 + i * 7, fy - zh, 5, 2, inZone ? Green : Ink);
                }
                // 차는 나 (제기를 따라 좌우로 움직인다)
                Me(kx - 12, fy - (kickAnim > .5f ? 3 : 0), kickAnim > .2f);
                // 제기와 땅 위 그림자 (높을수록 작고 흐리게)
                float h01 = Mathf.Clamp01(y);
                Shadow(kx + 9, fy - 1, Mathf.Lerp(12, 6, h01), (byte)(Mathf.RoundToInt(Mathf.Lerp(80, 30, h01) / 10f) * 10));   // 10 단위 (단색 그림 캐시)
                float jy2 = fy - Mathf.Clamp(y, 0f, 1.15f) * (fy - Top);
                Prop(vy > 0f && !dropped ? "jegi_down" : "jegi_up", kx + 9, jy2 + 2);
                Overlay();
            }
        }

        // ---------------- 자리 ----------------

        // at = 맵에서 직접 고른 칸 (11/25 5차 은샘: 좌판 바로 앞·길 한가운데는 어색하다). 그 칸이 막혀 있으면 장터 근처 빈 칸을 찾는다.
        // 개경 투호는 장터 옆 잔디 마당, 나루 아주머니는 장터 마당 가장자리, 닭목 아이는 장터 아래 풀밭
        struct Place { public string map; public Type game; public Vector2Int at; }
        static readonly Place[] Places =
        {
            new Place { map = "Gaegyeong", game = typeof(Tuho), at = new Vector2Int(31, 22) },
            new Place { map = "Naru", game = typeof(RiceCake), at = new Vector2Int(17, 53) },
            new Place { map = "Dakmok", game = typeof(Jegi), at = new Vector2Int(4, 20) },
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

        static void Deco(Transform parent, Sprite sp, Vector2 at, int order)
        {
            var pic = new GameObject("그림"); pic.transform.SetParent(parent, false);
            pic.transform.localPosition = at;
            var sr = pic.AddComponent<SpriteRenderer>(); sr.sprite = sp; sr.sortingOrder = order;
        }

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
                    var want = new Vector3Int(p.at.x, p.at.y, 0);
                    Vector3? pos = Free(mr, want) ? mr.CellCenter(want) : SpotNearMarket(mr);
                    if (pos == null) { Debug.LogWarning($"[고려 놀이] {p.map}: 자리가 없다"); continue; }
                    var go = new GameObject("고려 놀이");
                    go.transform.SetParent(mr.transform, true);
                    go.transform.position = pos.Value;
                    var game = (Stage)go.AddComponent(p.game);
                    game.blocksMovement = true;      // 항아리·사람이라 그 칸은 지나가지 못한다 (자리를 고를 때 사방이 트인 칸만 고른다)
                    var sp = Resources.Load<Sprite>("Goryeo/" + game.MapSprite);
                    if (sp == null) continue;
                    // 놀이 그림은 가운데가 기준점이라 발이 칸 아래쪽에 오게 올린다 (장터 사람 그림은 기준점이 이미 발밑)
                    bool centered = game.MapSprite.StartsWith("Pastimes/");
                    Deco(go.transform, sp, new Vector2(0f, (centered ? sp.bounds.extents.y - .4f : 0f) + game.MapLift), 10);
                    foreach (var d in game.MapDecor)
                    {
                        var ds = Resources.Load<Sprite>("Goryeo/Pastimes/" + d.name);
                        if (ds != null) Deco(go.transform, ds, new Vector2(d.x, d.y + ds.bounds.extents.y - .4f), d.order);
                    }
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
