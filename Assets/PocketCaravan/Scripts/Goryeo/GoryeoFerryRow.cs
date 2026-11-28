using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    public static partial class GoryeoPastimes
    {
        /// <summary>
        /// 나룻배 노 젓기 (2026-11-28, 은샘: 고려도 배를 탈 때 놀이를 넣자 · 사공과 장단 맞추기 · 보상은 더 빨리 닿는 것뿐).
        /// 사공이 "어기…" 하고 노를 들었다가 "여차!" 하고 저을 때 Space로 같이 젓는다. 열다섯 번, 뒤로 갈수록 장단이 빨라진다.
        /// 맞춘 만큼 걸리는 시간이 준다 (GoryeoFerry: 느리면 2시간, 다 맞추면 1시간). 뱃삯은 그대로, 돈으로 주는 보상은 없다.
        /// Esc는 건너뛰기(느린 시간으로 그대로 건너간다). 배는 어차피 가므로 못 맞춰도 잃는 것은 없다.
        /// 맵에 세우는 놀이가 아니라 사공에게 목적지를 고르면 GoryeoFerry가 Play로 연다. 그림은 Resources/Goryeo/Pastimes/ferry_*.
        /// </summary>
        public class FerryRow : Stage
        {
            public override string Id => "ferry";
            public override string DisplayName => "나룻배";
            public override string Title => "나룻배 노 젓기";
            public override string Where => to + " 가는 배";
            public override string Hint => "사공이 \"여차!\" 하고 저을 때 Space로 함께 저으세요. 장단이 잘 맞을수록 빨리 닿아요.";
            protected override string QuitLabel => "건너뛰기 · Esc";
            protected override string DoneLabel => "내리기 · Space";
            protected override string Status => finished ? "" : $"{to}까지 · 맞춘 노 {good}/{times.Count}";

            string from = "", to = "";
            Action<float> done;                    // 맞춘 비율 0~1 (건너뛰면 0)
            bool started, reported;
            int good, k, hk, combo; bool raised; float mine, his, lurch, wobble, dist, speed; string call = ""; float callAt = -9f;
            readonly List<float> times = new List<float>(); readonly List<float> gaps = new List<float>();

            /// <summary>사공에게 목적지를 고른 뒤 부른다. 놀이가 닫히면(끝까지 했든 건너뛰었든) onDone(맞춘 비율)을 한 번 부른다</summary>
            public static void Play(PlayerController player, string from, string to, Action<float> onDone)
            {
                var go = new GameObject("나룻배 노 젓기");
                go.transform.position = new Vector3(-9999f, -9999f, 0f);   // 맵 밖 (이름표·말 걸기에 잡히지 않게)
                var g = go.AddComponent<FerryRow>();
                g.from = from; g.to = to; g.done = onDone;
                g.Interact(player);
                g.started = true;
                if (!g.open) g.Report(0f);          // 다른 창이 열려 있어 못 열었으면 그대로 건너간다
            }

            float Ratio => times.Count == 0 ? 0f : good / (float)times.Count;
            void Report(float ratio)
            {
                if (reported) return; reported = true;
                var cb = done; done = null;
                Destroy(gameObject);
                if (cb != null) cb(ratio);
            }
            void LateUpdate() { if (started && !open) Report(finished ? Ratio : 0f); }

            protected override void Begin()
            {
                Fresh(); good = k = hk = combo = 0; raised = false; mine = his = lurch = wobble = dist = 0f; speed = 26f; call = ""; callAt = -9f;
                times.Clear(); gaps.Clear();
                // 세 마디: 느리게 넷 · 보통 다섯 · 빠르게 여섯. 마디 사이에는 한 박자 쉰다
                float at = Ready + 1.2f;
                float[] gap = { 1.0f, .82f, .66f }; int[] count = { 4, 5, 6 };
                for (int p = 0; p < 3; p++)
                {
                    for (int i = 0; i < count[p]; i++) { times.Add(at); gaps.Add(gap[p]); at += gap[p]; }
                    at += gap[p] * .6f;
                }
            }

            void Say(string text) { call = text; callAt = Time.unscaledTime; }

            protected override void Tick(float dt)
            {
                mine = Mathf.Max(0f, mine - dt * 3.2f); his = Mathf.Max(0f, his - dt * 3.2f);
                lurch = Mathf.Max(0f, lurch - dt * 2.2f); wobble = Mathf.Max(0f, wobble - dt * 2f);
                speed = Mathf.MoveTowards(speed, 26f, 30f * dt);
                dist += speed * dt;
                if (!Playing) return;
                bool press = InputReader.InteractPressed();
                // 사공: 박자 반 박 앞에서 "어기…" 하고 노를 들고, 박자에 "여차!" 하고 젓는다 (내가 언제 누르든 사공은 제 장단대로)
                if (hk < times.Count)
                {
                    float ho = t - times[hk];
                    if (!raised && ho >= -gaps[hk] * .5f) { raised = true; Say("어기…"); }
                    if (ho >= 0f) { Say("여차!"); his = 1f; raised = false; hk++; Sfx.Play("drop", .25f); speed = Mathf.Max(speed, 44f); }
                }
                if (k < times.Count)
                {
                    float off = t - times[k];
                    if (press)
                    {
                        mine = 1f;
                        if (off < -.3f) Sfx.Play("step", .25f);                      // 박자가 아직 멀다: 헛젓기 (셈하지 않는다)
                        else
                        {
                            if (Mathf.Abs(off) <= .14f)
                            {
                                good++; combo++; lurch = 1f; speed = 96f; Sfx.Play("drop", .55f);
                                Burst(BoatX + 34, Feet - 6, 7, new Color32(214, 240, 246, 255), 60f, 70f, 2f);
                                Pop(Mathf.Abs(off) <= .05f ? "딱 맞다!" : "좋다!", new Vector2(BoatX + 30, 30), CuteUI.Up);
                                if (combo % 5 == 0) { Cheer(); Pop($"{combo}번 내리!", new Vector2(W / 2f, 16), CuteUI.Up); }
                            }
                            else { combo = 0; wobble = 1f; Sfx.Play("step", .4f); Pop(off < 0f ? "이르다" : "늦다", new Vector2(BoatX + 30, 30), CuteUI.Down); }
                            k++;
                        }
                    }
                    else if (off > .22f) { combo = 0; k++; }                          // 못 저은 박자: 조용히 지나간다
                }
                if (k >= times.Count && t >= times[times.Count - 1] + .9f) End();
            }

            void End()
            {
                float r = Ratio;
                grade = r >= .85f ? 3 : r >= .6f ? 2 : r >= .3f ? 1 : 0;
                Finale(BoatX, 40f);
                float hours = GoryeoFerry.HoursFor(r), saved = GoryeoFerry.SlowHours - hours;
                result = $"{to}에 닿았다. {GoryeoFerry.TimeText(hours)} 걸렸다.";
                if (saved > .01f) result += $" 장단이 맞아 {GoryeoFerry.TimeText(saved)} 빨리 왔다!";
                Sfx.Play(grade >= 2 ? "levelup" : "success", .5f);
            }

            float BoatX => Mathf.Round(W * .42f + lurch * 6f);

            protected override void Draw(Rect r)
            {
                Backdrop(r, "bg_ferry");
                const float waterY = 60f;
                // 건너편 강둑: 배가 나아가는 만큼 왼쪽으로 흐른다 (두 장을 이어 붙인다)
                var bank = Art("Pastimes/ferry_bank");
                if (bank != null)
                {
                    float bw = bank.rect.width * 2f, bh = bank.rect.height * 2f, sx = -Mathf.Repeat(dist * .5f, bw);
                    for (float x = sx; x < W; x += bw)
                        CuteUI.Sprite(bank, new Rect(Mathf.Round(x), waterY + 2f - bh, bw, bh), new Rect(0, 0, 1, 1), false);
                }
                // 흐르는 물결
                var hi = new Color32(150, 204, 214, 255); var lo = new Color32(66, 128, 152, 255);
                for (int i = 0; i < 16; i++)
                {
                    float y = waterY + 6f + (i * 29) % (H - waterY - 10f);
                    float x = W - Mathf.Repeat(dist * (1f + (i % 3) * .25f) + i * 61f, W + 24f);
                    F(x, Mathf.Round(y / 2f) * 2f, 8 + (i % 3) * 4, 2, i % 4 == 0 ? lo : hi);
                }
                                float bx = BoatX, bob = Mathf.Round(Mathf.Sin(Time.unscaledTime * 2.4f) * 1.5f) + Mathf.Round(Mathf.Sin(wobble * 18f) * wobble * 3f);
                float by = H - 6f + bob;                      // 배 그림 아랫줄
                float deck = by - 26f;                        // 사람 발밑 (뱃전에 살짝 가린다)
                // 배가 지나간 물길
                for (int i = 0; i < 4; i++) F(bx - 84f - i * 16f - Mathf.Repeat(dist, 16f), by - 14f + (i % 2) * 4f, 10 - i * 2, 2, new Color32(214, 240, 246, 255));
                Prop("ferry_boat", bx, by);
                // 사공(뒤)과 나(앞). 노는 손을 축으로 돈다: 들어 올림 → 물에 꽂음 → 당김 → 뒤로 뺌
                int hf = his > .66f ? 1 : his > .33f ? 2 : his > 0f ? 3 : raised ? 0 : 3;
                int mf = mine > .66f ? 1 : mine > .33f ? 2 : mine > 0f ? 3 : 0;
                Prop("ferry_boatman_" + (his > .33f ? 1 : 0), bx - 40f, deck);
                Me(bx + 20f, deck, mine > .33f);
                Prop("ferry_boat_front", bx, by);
                PropC("ferry_oar_" + hf, bx - 30f, deck - 20f);
                PropC("ferry_oar_" + mf, bx + 30f, deck - 20f);
                Prop("ferry_lotus", W + 30f - Mathf.Repeat(dist * 1.3f, W + 300f), H + 4f);   // 앞쪽 연잎이 지나간다
                if (finished) { Overlay(); return; }
                // 사공의 소리
                float age = Time.unscaledTime - callAt;
                if (Playing && age < .5f && call != "")
                {
                    bool go = call == "여차!";
                    float cw = go ? 50f : 46f; var box = new Rect(bx - 40f - cw / 2f, deck - 76f - (go ? 2f : 0f), cw, 20f);
                    CuteUI.Box(box, 6, CuteUI.Paper, CuteUI.Ink, 1, 2);
                    CuteUI.LabelCenter(box.center.x, box.center.y - 1f, call, go ? CuteUI.Up : CuteUI.Text, false, true);
                }
                // 남은 길: 위쪽 가는 띠에 작은 배가 건너간다
                float left = Mathf.Clamp01((t - Ready) / Mathf.Max(1f, times[times.Count - 1] + .9f - Ready));
                F(40, 5, W - 80, 2, new Color32(58, 40, 36, 70)); F(40, 5, (W - 80) * left, 2, Green);
                Overlay();
            }
        }
    }
}
