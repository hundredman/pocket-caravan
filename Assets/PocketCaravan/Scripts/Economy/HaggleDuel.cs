using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 흥정 미니게임 "눈치 흥정" (2026-11-15 은샘 3판). 할 일은 하나: 상인 머리 위 속마음을 보고 맞는 말을 건넨다.
    /// - 3턴. 턴마다 상인 속마음(말 · 셈 · 정 · 배짱 중 하나)이 머리 위에 뜨고, 내 손에 말 3장이 놓인다.
    ///   딱 맞으면 마음 +30, 맞서는 말(말↔셈, 정↔배짱)은 0에 짜증, 나머지는 +12.
    ///   배짱은 속마음이 아니면 반반 도박(+25 또는 짜증).
    /// - 상인 성격: 너그러움은 속마음이 또렷, 보통은 한 턴이 "?", 깐깐함은 "둘 중 하나"로만 보인다.
    /// - 출신 비장의 말 한 장(고려 장부 펼치기 · 아라비아 차 대접 · 베네치아 계약서 · 북유럽 뱃노래)은 무엇이든 딱 맞는다. 한 번만.
    /// - 마음 90에 신용 최대치(ReputationPricing.MaxHaggle)만큼 깎는다. 짜증 2면 토라져 그 거래만 ±2% (거래는 막지 않는다).
    /// - 속마음·손 패·도박은 날짜·마을·물건·사고팔기로 정해 저장을 되돌려도 같다.
    /// 장터 창 위에 그리는 창이라 UIState는 장터가 그대로 쥔다.
    /// </summary>
    public class HaggleDuel : MonoBehaviour
    {
        public enum Tag { Talk, Count, Heart, Nerve }
        static readonly string[] TagNames = { "말", "셈", "정", "배짱" };
        static readonly string[] TagActs = { "칭찬하기", "시세 따지기", "정에 호소", "배짱 부리기" };
        static readonly CuteUI.Pic[] TagPics = { CuteUI.Pic.Speaker, CuteUI.Pic.Coin7, CuteUI.Pic.Basket, CuteUI.Pic.Bolt };
        static readonly Color32[] TagColors = { new Color32(0x3E, 0x9C, 0xE0, 255), new Color32(0xE0, 0x9A, 0x22, 255), new Color32(0xE8, 0x5D, 0x8A, 255), new Color32(0x8A, 0x5C, 0xD6, 255) };
        static readonly Color32[] TagSoft = { new Color32(0xDC, 0xEE, 0xFB, 255), new Color32(0xFF, 0xEE, 0xCC, 255), new Color32(0xFD, 0xE3, 0xEC, 255), new Color32(0xEC, 0xE2, 0xFA, 255) };
        static Tag Opposite(Tag t) => t == Tag.Talk ? Tag.Count : t == Tag.Count ? Tag.Talk : t == Tag.Heart ? Tag.Nerve : Tag.Heart;

        public const float FailPenalty = 0.02f;
        public const int Turns = 3, IrkLimit = 2, HandSize = 3;
        const int Match = 30, Neutral = 12, Gamble = 25, FullMind = 90;

        struct Card { public Tag tag; public bool ace; }   // ace = 출신 비장의 말 (무엇이든 맞음)

        // ---------- 상태 ----------
        static HaggleDuel inst;
        public static bool IsOpen => inst != null && inst.open;
        /// <summary>창을 닫은 프레임 (장터가 같은 Esc·Space를 또 받지 않게)</summary>
        public static int ClosedFrame = -1;
        bool open; int openedFrame; float openedAt;
        TownData town; ItemData item; bool selling; int count; float max; int baseTotal;
        Action<float, bool> done;
        int temperament; bool aceUsed; string aceName = "";
        readonly Tag[] wish = new Tag[Turns], decoy = new Tag[Turns]; int hiddenTurn = -1;
        readonly List<Card> hand = new List<Card>();
        readonly List<(Card card, int gain, int feel)> played = new List<(Card, int, int)>();
        System.Random rng;
        int turn, irk, selected; float mind, shownMind;
        enum Phase { Play, React, Result }
        Phase phase; float phaseAt;
        string line = "", lineFull = ""; float lineAt;
        int lastGain, lastFeel; float popAt = -9f;   // lastFeel: 1 딱 맞음, 0 보통, -1 짜증
        Card flying; Rect flyFrom; float flyAt = -9f; bool hasFlying;
        bool failed; float result;
        Sprite merchant;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<HaggleDuel>() != null) return;
            var go = new GameObject("HaggleDuel"); DontDestroyOnLoad(go); inst = go.AddComponent<HaggleDuel>();
        }
        void Awake() => inst = this;

        /// <summary>흥정 시작. baseTotal = 흥정 전 총액. done(할인율, 토라졌는지): 할인율은 −0.02~max</summary>
        public static bool Open(TownData town, ItemData item, bool selling, int count, float max, ReputationStanding standing, int baseTotal, Action<float, bool> done)
        {
            if (inst == null || inst.open || town == null || item == null || count < 1 || max <= 0f) return false;
            inst.Begin(town, item, selling, count, max, baseTotal, done);
            return true;
        }

        void Begin(TownData t, ItemData it, bool sell, int n, float mx, int total, Action<float, bool> cb)
        {
            town = t; item = it; selling = sell; count = n; max = mx; baseTotal = total; done = cb;
            var gm = GameManager.I; int day = gm != null ? gm.Clock.day : 0;
            rng = new System.Random((int)(Hash($"{day}|{t.id}|{it.id}|{(sell ? 1 : 0)}") & 0x7fffffff));
            temperament = HaggleRules.Temperament(t.id);
            for (int i = 0; i < Turns; i++)
            {
                wish[i] = (Tag)rng.Next(4);
                do decoy[i] = (Tag)rng.Next(4); while (decoy[i] == wish[i] || decoy[i] == Opposite(wish[i]));
            }
            hiddenTurn = temperament == 0 ? rng.Next(Turns) : -1;
            string origin = gm != null && gm.Player.origin != null ? gm.Player.origin.id : "";
            aceName = origin == "goryeo" ? "장부 펼치기" : origin == "arabia" ? "차 대접" : origin == "venice" ? "계약서" : origin == "viking" ? "뱃노래" : "";
            aceUsed = aceName.Length == 0;
            played.Clear(); turn = 0; irk = 0; mind = shownMind = 0; failed = false; result = 0; selected = 0; hasFlying = false;
            Deal();
            merchant = Resources.Load<Sprite>("Haggle/merchant_" + OriginPerks.Civ(t.region)) ?? Resources.Load<Sprite>("Haggle/merchant_center");
            Say(Opening());
            phase = Phase.Play; phaseAt = Time.unscaledTime;
            open = true; openedFrame = Time.frameCount; openedAt = Time.unscaledTime;
            Sfx.Play("open");
        }

        /// <summary>서로 다른 말 3장. 비장의 말은 둘째 턴에 한 장 섞인다 (아직 안 썼으면)</summary>
        void Deal()
        {
            hand.Clear();
            var pool = new List<Tag> { Tag.Talk, Tag.Count, Tag.Heart, Tag.Nerve };
            pool.RemoveAt(rng.Next(pool.Count));
            foreach (var tg in pool) hand.Add(new Card { tag = tg });
            if (!aceUsed && turn == 1) hand[rng.Next(hand.Count)] = new Card { tag = wish[turn], ace = true };
            selected = 0;
        }

        string Opening()
        {
            string what = $"{item.displayName} {count}개";
            string price = selling ? $"{baseTotal}냥에 받겠소" : $"{baseTotal}냥이오";
            string mood = temperament < 0 ? "흥정은 쉽지 않을 거요." : temperament > 0 ? "좋은 날이니 들어는 보지." : "어디 말해 보시오.";
            return $"{what}이라… {price}. {mood}";
        }

        void Say(string s) { lineFull = s; line = ""; lineAt = Time.unscaledTime; }

        // ---------- 판정 ----------
        void Play(int index, Rect from)
        {
            if (phase != Phase.Play || index < 0 || index >= hand.Count) return;
            var c = hand[index]; var w = wish[turn];
            int gain, feel;
            if (c.ace || c.tag == w) { gain = Match; feel = 1; }
            else if (c.tag == Opposite(w)) { gain = 0; feel = -1; }
            else if (c.tag == Tag.Nerve) { bool win = rng.Next(2) == 0; gain = win ? Gamble : 0; feel = win ? 0 : -1; }
            else { gain = Neutral; feel = 0; }
            if (temperament > 0 && feel >= 0) gain += 3;
            if (c.ace) aceUsed = true;
            if (feel < 0) irk++;
            mind = Mathf.Min(FullMind, mind + gain);
            lastGain = gain; lastFeel = feel; popAt = Time.unscaledTime;
            flying = c; flyFrom = from; flyAt = Time.unscaledTime; hasFlying = true;
            played.Add((c, gain, feel));
            Say(Reaction(c, feel, w));
            Sfx.Play(feel > 0 ? "success" : feel < 0 ? "bump" : "accept", .7f);
            turn++;
            phase = Phase.React; phaseAt = Time.unscaledTime;
        }

        string Reaction(Card c, int feel, Tag w)
        {
            if (irk >= IrkLimit) return "에잇, 그만하시오! 흥정은 끝이오.";
            if (c.ace) return $"({aceName}) …허허, 이건 당해 낼 수가 없구려!";
            if (feel > 0) switch (w)
                {
                    case Tag.Talk: return "허허, 듣기 좋은 말이오!";
                    case Tag.Count: return "옳거니, 셈이 딱 맞는구려!";
                    case Tag.Heart: return "이리 정이 많아서야…!";
                    default: return "그 배짱, 마음에 드오!";
                }
            if (feel < 0) return c.tag == Tag.Nerve && w != Tag.Heart ? "날 겁주려는 거요?" : "지금 그런 말이 듣고 싶은 게 아니오.";
            return lastGain >= Gamble ? "…으음, 그 배짱은 알아주지." : "흠, 들어는 보지.";
        }

        void AfterReact()
        {
            hasFlying = false;
            if (irk >= IrkLimit) { Finish(true); return; }
            if (turn >= Turns) { Finish(false); return; }
            Deal();
            phase = Phase.Play; phaseAt = Time.unscaledTime;
        }

        float Discount => max * Mathf.Clamp01(mind / FullMind);
        int PriceAt(float m) => Mathf.Max(1, Mathf.RoundToInt(baseTotal * (selling ? 1f + max * Mathf.Clamp01(m / FullMind) : 1f - max * Mathf.Clamp01(m / FullMind))));
        int PriceFail => Mathf.Max(1, Mathf.RoundToInt(baseTotal * (selling ? 1f - FailPenalty : 1f + FailPenalty)));

        void Finish(bool fail)
        {
            failed = fail;
            result = fail ? -FailPenalty : Mathf.Round(Discount * 200f) / 200f;   // 0.5% 단위
            int p = fail ? PriceFail : PriceAt(mind);
            if (fail) Say(selling ? $"에잇! {p}냥만 쳐 주겠소." : $"에잇! {p}냥 내시오.");
            else if (result <= 0f) Say("원래 값대로 합시다.");
            else Say(selling ? $"좋소, {p}냥에 받겠소!" : $"좋소, {p}냥에 드리리다!");
            Sfx.Play(fail ? "fail" : result > 0f ? "coin" : "accept");
            phase = Phase.Result; phaseAt = Time.unscaledTime;
        }

        void Close(bool apply)
        {
            if (!open) return;
            open = false; ClosedFrame = Time.frameCount;
            var cb = done; done = null;
            if (apply) cb?.Invoke(result, failed);
        }

        // ---------- 입력 ----------
        void Update()
        {
            if (!open) return;
            if (line.Length < lineFull.Length) line = lineFull.Substring(0, Mathf.Min(lineFull.Length, Mathf.FloorToInt((Time.unscaledTime - lineAt) * 40f)));
            shownMind = Mathf.MoveTowards(shownMind, mind, Time.unscaledDeltaTime * 80f);
            if (Time.frameCount == openedFrame) return;
            if (phase == Phase.React) { if (Time.unscaledTime - phaseAt > 1.0f || (Time.unscaledTime - phaseAt > .3f && InputReader.InteractPressed())) AfterReact(); return; }
            if (phase == Phase.Result)
            {
                if (Time.unscaledTime - phaseAt > 0.35f && (InputReader.InteractPressed() || InputReader.CancelPressed())) Close(true);
                return;
            }
            if (InputReader.CancelPressed()) { Close(false); Sfx.Play("cancel"); return; }
            int n = InputReader.NumberPressed();
            if (n >= 1 && n <= hand.Count) { Play(n - 1, cardRects[n - 1]); return; }
            if (n == 4 && turn > 0) { Finish(false); return; }
            var mv = InputReader.MovePressed();
            if (mv.x != 0 && hand.Count > 0) { selected = (selected + mv.x + hand.Count) % hand.Count; Sfx.Play("move"); }
            if (InputReader.InteractPressed() && hand.Count > 0) Play(selected, cardRects[selected]);
        }

        // ---------- 그리기 ----------
        // 눈이 가는 곳은 셋: 상인 머리 위 속마음 · 값 · 내 말 3장. 글은 상인 대사 한 줄과 카드 이름뿐.
        readonly Rect[] cardRects = new Rect[HandSize];
        const float W = 380f, H = 236f;
        static readonly Color32 Pink = new Color32(0xE8, 0x5D, 0x8A, 255), PinkFill = new Color32(0xF2, 0x7A, 0xA0, 255), PinkHi = new Color32(0xFF, 0xC2, 0xD4, 255);
        static readonly Color32 Wood = new Color32(0xC9, 0x8A, 0x4B, 255), WoodHi = new Color32(0xE6, 0xB0, 0x78, 255), Cream = new Color32(0xFF, 0xF4, 0xE6, 255);

        void OnGUI()
        {
            if (!open) return;
            UIStyle.Begin();
            GUI.depth = -30;
            float vw = UIStyle.VirtualWidth, vh = UIStyle.VirtualHeight, now = Time.unscaledTime;
            CuteUI.Fill(new Rect(0, 0, vw, vh), new Color32(30, 15, 35, 120));
            float appear = Mathf.Clamp01((now - openedAt) / .18f);
            var r = new Rect(Mathf.Round((vw - W) / 2f), Mathf.Round((vh - H) / 2f + (1f - appear) * 10f), W, H);
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 6, "흥정");
            float react = phase == Phase.React ? now - phaseAt : 9f;

            // ── 상인 (왼쪽) ──
            var stage = new Rect(r.x + 14, r.y + 18, 112, 128);
            CuteUI.Box(stage, 8, CuteUI.Paper2, CuteUI.Line);
            float bob = Mathf.Round(Mathf.Sin(now * 2.2f));
            float shake = react < .35f && lastFeel < 0 ? Mathf.Round(Mathf.Sin(react * 60f) * 2f) : 0f;
            float hop = react < .35f && lastFeel > 0 ? -Mathf.Round(Mathf.Sin(react / .35f * Mathf.PI) * 6f) : 0f;
            if (phase == Phase.Result && failed && now - phaseAt < .4f) shake = Mathf.Round(Mathf.Sin((now - phaseAt) * 60f) * 3f);
            float counterY = stage.yMax - 22;
            if (merchant != null) CuteUI.Sprite(merchant, new Rect(stage.center.x - 32 + shake, counterY - 74 + bob + hop, 64, 96));
            CuteUI.Fill(new Rect(stage.x + 1, counterY, stage.width - 2, stage.yMax - counterY - 1), Wood);
            CuteUI.Fill(new Rect(stage.x + 1, counterY, stage.width - 2, 2), WoodHi);
            string temp = temperament < 0 ? "깐깐한 상인" : temperament > 0 ? "너그러운 상인" : "보통 상인";
            CuteUI.LabelCenter(stage.center.x, counterY + 11, temp, Cream, true, true);
            DrawThought(stage, react, now);

            // ── 대사 · 값 (오른쪽) ──
            float rx = stage.xMax + 14, rw = r.xMax - 16 - rx;
            CuteUI.Wrap(rx, stage.y + 2, rw, line, CuteUI.Text, true);
            int shownPrice = phase == Phase.Result && failed ? PriceFail : PriceAt(shownMind);
            int diff = shownPrice - baseTotal; bool good = selling ? diff > 0 : diff < 0;
            float py = stage.y + 44;
            float px = CuteUI.Label(rx, py, $"{shownPrice}냥", diff == 0 ? CuteUI.Text : good ? CuteUI.Up : CuteUI.Down, false, true);
            if (diff != 0)
            {
                float ox = CuteUI.Label(px + 8, py + 1, $"{baseTotal}냥", CuteUI.Zero, true);
                CuteUI.Fill(new Rect(px + 8, py + 1, ox - px - 8, 1), CuteUI.Zero);
            }
            var bar = new Rect(rx, py + 13, rw, 10);
            CuteUI.Bar(bar, shownMind / FullMind, PinkFill, PinkHi);
            for (int q = 1; q < 3; q++) CuteUI.Fill(new Rect(bar.x + bar.width * q / 3f, bar.y + 1, 1, bar.height - 2), new Color32(255, 255, 255, 150));
            if (now - popAt < .9f && lastGain > 0)
            {
                float k = (now - popAt) / .9f; var oc = GUI.color; GUI.color = new Color(oc.r, oc.g, oc.b, 1f - k * k);
                CuteUI.LabelRight(bar.x + bar.width * Mathf.Clamp01(mind / FullMind), bar.y - 6 - k * 6f, $"+{lastGain}", Pink, false, true);
                GUI.color = oc;
            }
            // 짜증: 막대 아래 작은 점 두 개 (하나라도 켜지면 보인다)
            float iy = bar.yMax + 10;
            CuteUI.Label(rx, iy, "마음", Pink, true, true);
            float ix = bar.xMax;
            for (int i = IrkLimit - 1; i >= 0; i--)
            {
                bool on = i < irk; ix -= 13;
                CuteUI.Box(new Rect(ix, iy - 5, 10, 10), 3, on ? CuteUI.Down : CuteUI.White, on ? new Color32(0x9C, 0x1C, 0x36, 255) : CuteUI.Line);
            }
            CuteUI.LabelRight(ix - 4, iy, "짜증", irk > 0 ? CuteUI.Down : CuteUI.Zero, true);

            // ── 내 말 3장 (아래) ──
            float cy = r.y + 158, cw = 76, ch = 58, gap = 8, cx0 = r.x + 14;
            for (int i = 0; i < HandSize; i++) cardRects[i] = new Rect(cx0 + i * (cw + gap), cy, cw, ch);
            float sideX = cx0 + HandSize * (cw + gap) + 2;
            var side = new Rect(sideX, cy, r.xMax - 14 - sideX, ch);
            if (phase == Phase.Play)
            {
                for (int i = 0; i < hand.Count; i++)
                {
                    var cr = cardRects[i];
                    if (cr.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseMove) selected = i;
                    bool sel = i == selected;
                    float deal = Mathf.Clamp01((now - phaseAt - i * .06f) / .18f);
                    var dr = new Rect(cr.x, cr.y + (sel ? -4f : 0f) + (1f - deal) * 12f, cr.width, cr.height);
                    var oc = GUI.color; GUI.color = new Color(oc.r, oc.g, oc.b, deal);
                    DrawCard(dr, hand[i], sel, i + 1);
                    GUI.color = oc;
                    if (GUI.Button(dr, GUIContent.none, GUIStyle.none)) Play(i, dr);
                }
            }
            else if (phase == Phase.React && hasFlying)
            {
                float k = Mathf.Clamp01((now - flyAt) / .28f), e = 1f - (1f - k) * (1f - k);
                var to = new Rect(stage.center.x - 22, stage.y + 40, 44, 34);
                var fr = new Rect(Mathf.Lerp(flyFrom.x, to.x, e), Mathf.Lerp(flyFrom.y, to.y, e), Mathf.Lerp(flyFrom.width, to.width, e), Mathf.Lerp(flyFrom.height, to.height, e));
                var oc = GUI.color; GUI.color = new Color(oc.r, oc.g, oc.b, 1f - Mathf.Clamp01((now - flyAt - .3f) / .25f));
                DrawCard(fr, flying, false, 0);
                GUI.color = oc;
            }
            else if (phase == Phase.Result)
            {
                for (int i = 0; i < played.Count && i < HandSize; i++)
                {
                    var p = played[i]; var cr = cardRects[i]; int t = (int)p.card.tag;
                    CuteUI.Box(cr, 7, CuteUI.Paper2, CuteUI.Line);
                    IconAt(TagPics[t], cr.x + 14, cr.y + 16, 1, TagColors[t]);
                    CuteUI.Label(cr.x + 24, cr.y + 16, p.card.ace ? aceName : TagActs[t], CuteUI.Sub, true);
                    string mark = p.feel > 0 ? "♥" : p.feel < 0 ? "!" : "";
                    CuteUI.Label(cr.x + 10, cr.y + 38, $"+{p.gain}", p.gain > 0 ? Pink : CuteUI.Zero, false, true);
                    if (mark.Length > 0) CuteUI.LabelRight(cr.xMax - 10, cr.y + 38, mark, p.feel > 0 ? Pink : CuteUI.Down, false, true);
                }
            }

            if (phase == Phase.Result)
            {
                float k = Mathf.Clamp01((now - phaseAt) / .22f); float s = Mathf.Lerp(1.5f, 1f, k);
                string stamp = failed ? "토라짐" : result > 0 ? "흥정 성공" : "그대로";
                Color32 sc = failed ? CuteUI.Down : result > 0 ? CuteUI.Up : CuteUI.Sub;
                var pivot = new Vector2(side.center.x, side.y + 12);
                var m = GUI.matrix; GUIUtility.RotateAroundPivot(-8f, pivot * UIStyle.Scale); GUIUtility.ScaleAroundPivot(new Vector2(s, s), pivot * UIStyle.Scale);
                var st = new Rect(side.center.x - 34, side.y + 2, 68, 21);
                CuteUI.Box(st, 5, new Color32(255, 255, 255, 230), sc, 2);
                CuteUI.LabelCenter(st.center.x, st.center.y, stamp, sc, false, true);
                GUI.matrix = m;
                if (now - phaseAt > .35f && CuteUI.Button(new Rect(side.x, side.yMax - 24, side.width, 24), "확인")) Close(true);
            }
            else
            {
                bool can = phase == Phase.Play && turn > 0;
                var sb = new Rect(side.x, side.y + 6, side.width, 26);
                if (can) { if (CuteUI.Button(sb, "이 값으로")) Finish(false); }
                else { CuteUI.Box(sb, 8, CuteUI.ZeroSoft, CuteUI.Line); CuteUI.LabelCenter(sb.center.x, sb.center.y, "이 값으로", CuteUI.Zero, false, true); }
                CuteUI.Keycap(sb.xMax - 11, sb.y - 6, "4", true);
                // 남은 턴: 점 셋
                for (int i = 0; i < Turns; i++)
                {
                    bool left = i >= turn;
                    CuteUI.Circle(side.center.x - 15 + i * 11, sb.yMax + 10, 7, left ? CuteUI.Deco : CuteUI.ZeroSoft, left ? CuteUI.Ink : CuteUI.Line);
                }
            }

            // 장터 창이 클릭을 받지 않게
            if (Event.current.isMouse || Event.current.type == EventType.ScrollWheel) Event.current.Use();
        }

        /// <summary>상인 머리 위 속마음 구름: 너그러움 = 하나, 보통 = 하나(한 턴은 ?), 깐깐함 = 둘 중 하나</summary>
        void DrawThought(Rect stage, float react, float now)
        {
            if (phase == Phase.Result || turn >= Turns) { DrawEmote(stage, now); return; }
            if (phase == Phase.React) { DrawEmote(stage, now); return; }
            float pop = Mathf.Clamp01((now - phaseAt) / .2f);
            bool hidden = turn == hiddenTurn, two = temperament < 0;
            float w = two ? 62 : 32, h = 28;
            var c = new Rect(stage.xMax - w - 4, stage.y + 6 + (1f - pop) * 6f, w, h);
            var oc = GUI.color; GUI.color = new Color(oc.r, oc.g, oc.b, pop);
            CuteUI.Box(c, 10, CuteUI.White, CuteUI.Ink, 1, 2, CuteUI.SoftShadow);
            CuteUI.Circle(c.x + 2, c.yMax + 2, 6, CuteUI.White, CuteUI.Ink);
            CuteUI.Circle(c.x - 4, c.yMax + 8, 4, CuteUI.White, CuteUI.Ink);
            if (hidden) CuteUI.LabelCenter(c.center.x, c.center.y, "?", CuteUI.Sub, false, true);
            else if (two)
            {
                // 진짜와 미끼의 자리는 판마다 다르다
                bool swap = ((turn + (int)wish[turn]) & 1) == 1;
                Tag a = swap ? decoy[turn] : wish[turn], b = swap ? wish[turn] : decoy[turn];
                IconAt(TagPics[(int)a], c.x + 16, c.center.y, 2, TagColors[(int)a]);
                CuteUI.LabelCenter(c.center.x, c.center.y, "/", CuteUI.Zero, true);
                IconAt(TagPics[(int)b], c.xMax - 16, c.center.y, 2, TagColors[(int)b]);
            }
            else IconAt(TagPics[(int)wish[turn]], c.center.x, c.center.y, 2, TagColors[(int)wish[turn]]);
            GUI.color = oc;
        }

        void DrawEmote(Rect stage, float now)
        {
            bool result = phase == Phase.Result;
            bool bad = result ? failed : lastFeel < 0, love = result ? !failed && this.result > 0f : lastFeel > 0;
            string emo = bad ? "!!" : love ? "♥" : "…";
            float t = now - phaseAt; float y = stage.y + 8 - Mathf.Min(t, .25f) * 12f;
            CuteUI.Circle(stage.xMax - 30, y, 22, CuteUI.White, CuteUI.Ink, 1, 1);
            CuteUI.LabelCenter(stage.xMax - 19, y + 11, emo, bad ? CuteUI.Down : love ? Pink : CuteUI.Sub, false, true);
        }

        void DrawCard(Rect c, Card card, bool sel, int key)
        {
            int t = (int)card.tag;
            bool ace = card.ace;
            CuteUI.Box(c, 8, ace ? new Color32(0xFF, 0xF6, 0xD6, 255) : CuteUI.White, sel ? CuteUI.Ink : (ace ? CuteUI.Deco : CuteUI.Line), sel ? 2 : 1, sel ? 3 : 2, CuteUI.SoftShadow);
            if (c.height < 40) { IconAt(TagPics[t], c.center.x, c.center.y, 2, TagColors[t]); return; }
            CuteUI.Circle(c.center.x - 14, c.y + 6, 28, ace ? CuteUI.Deco : TagSoft[t], ace ? CuteUI.Ink : TagColors[t]);
            IconAt(ace ? CuteUI.Pic.Star5 : TagPics[t], c.center.x, c.y + 20, 2, ace ? CuteUI.Ink : TagColors[t]);
            CuteUI.LabelCenter(c.center.x, c.yMax - 11, ace ? aceName : TagActs[t], ace ? CuteUI.Text : TagColors[t], true, true);
            if (key > 0) CuteUI.Keycap(c.xMax - 10, c.y - 5, key.ToString(), true);
        }

        /// <summary>그림 글자를 (cx, cy) 가운데에 scale배로</summary>
        static void IconAt(CuteUI.Pic p, float cx, float cy, int scale, Color32 col)
        {
            var sz = CuteUI.PicSize(p) * scale;
            CuteUI.Icon(p, Mathf.Round(cx - sz.x / 2f), Mathf.Round(cy - sz.y / 2f), scale, col);
        }

        static ulong Hash(string s)
        {
            ulong h = 14695981039346656037UL;
            foreach (char c in s) { h ^= c; h *= 1099511628211UL; }
            return h;
        }
    }
}
