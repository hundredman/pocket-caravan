using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 흥정 미니게임 "말솜씨 대결" (2026-11-15 은샘, UT "미니게임 강화 · 경제와 5:5"). Potionomics의 흥정 카드를 참고했다.
    /// 장터 창의 흥정 버튼 → 상인과 3턴. 턴마다 말솜씨 카드 3장 중 하나를 낸다.
    /// - 마음(0~100)이 찰수록 값이 내려간다(팔 때는 올라간다). 깎는 최대치는 신용 단계(ReputationPricing.MaxHaggle).
    /// - 상인마다 좋아하는 말(×1.5, 짜증 −1)과 싫어하는 말(×0.5, 짜증 +1)이 있다. 한 번 보면 그 마을 상인은 기억해 둔다(메모).
    /// - 짜증이 3이 되면 토라져 그 거래만 2% 불리 (거래는 막지 않는다, 11/15 은샘).
    /// - 카드 순서·운은 날짜·마을·물건·사고팔기로 정해져 저장을 되돌려도 같다.
    /// 출신마다 고유 카드 2장 (고려 장부 펼치기 · 아라비아 차 대접 · 베네치아 계약서 · 북유럽 뱃노래).
    /// 장터 창 위에 그리는 창이라 UIState는 장터가 그대로 쥔다.
    /// </summary>
    public class HaggleDuel : MonoBehaviour
    {
        public enum Tag { Talk, Count, Heart, Nerve }
        static readonly string[] TagNames = { "말", "셈", "정", "배짱" };
        static readonly Color32[] TagColors = { new Color32(0x3E, 0x9C, 0xE0, 255), new Color32(0xE0, 0x9A, 0x22, 255), new Color32(0xE8, 0x5D, 0x8A, 255), new Color32(0x8A, 0x5C, 0xD6, 255) };
        static readonly Color32[] TagSoft = { new Color32(0xDC, 0xEE, 0xFB, 255), new Color32(0xFF, 0xEE, 0xCC, 255), new Color32(0xFD, 0xE3, 0xEC, 255), new Color32(0xEC, 0xE2, 0xFA, 255) };

        public const float FailPenalty = 0.02f;
        public const int Turns = 3, IrkLimit = 3, HandSize = 3;

        class Card
        {
            public string id, name, desc; public Tag tag; public int mind, irk;
            public Card(string id, string name, Tag tag, int mind, int irk, string desc) { this.id = id; this.name = name; this.tag = tag; this.mind = mind; this.irk = irk; this.desc = desc; }
        }

        static Card[] BaseDeck() => new[]
        {
            new Card("praise", "칭찬", Tag.Talk, 12, 0, "물건 보는 눈을 추어올린다"),
            new Card("praise", "칭찬", Tag.Talk, 12, 0, "물건 보는 눈을 추어올린다"),
            new Card("banter", "너스레", Tag.Talk, 0, 0, "농담 한마디. 마음 +4~26, 운에 맡긴다"),
            new Card("quote", "시세 들먹이기", Tag.Count, 22, 1, "옆 장터 값을 꺼낸다"),
            new Card("quote", "시세 들먹이기", Tag.Count, 22, 1, "옆 장터 값을 꺼낸다"),
            new Card("bulk", "대량 거래", Tag.Count, 0, 0, "많이 거래한다고 내세운다. 수량만큼"),
            new Card("regular", "단골 약속", Tag.Heart, 14, 0, "또 오겠다고 약속한다. 신용 우호부터 +8"),
            new Card("gift", "작은 선물", Tag.Heart, 20, 0, "말린 과일 한 줌을 건넨다"),
            new Card("bluff", "떠나는 척", Tag.Nerve, 34, 1, "등을 돌린다. 깐깐한 상인은 짜증 +1 더"),
            new Card("wait", "뜸 들이기", Tag.Nerve, 6, -1, "말없이 기다린다. 짜증 −1"),
        };

        static Card[] OriginCards(string origin)
        {
            switch (origin)
            {
                case "goryeo": return Two(new Card("ledger", "장부 펼치기", Tag.Count, 16, 0, "개성 장부를 펼친다. 다음 셈 카드 두 배"));
                case "arabia": return Two(new Card("tea", "차 대접", Tag.Heart, 12, -1, "향 좋은 차를 따른다. 짜증 −1"));
                case "venice": return Two(new Card("contract", "계약서", Tag.Count, 20, 0, "약속을 글로 쓴다. 싫어해도 짜증 없음"));
                case "viking": return Two(new Card("song", "뱃노래", Tag.Talk, 14, 0, "흥겨운 노래. 너그러운 상인은 두 배"));
                default: return new Card[0];
            }
        }
        static Card[] Two(Card c) => new[] { c, new Card(c.id, c.name, c.tag, c.mind, c.irk, c.desc) };

        // ---------- 상태 ----------
        static HaggleDuel inst;
        public static bool IsOpen => inst != null && inst.open;
        /// <summary>창을 닫은 프레임 (장터가 같은 Esc·Space를 또 받지 않게)</summary>
        public static int ClosedFrame = -1;
        bool open; int openedFrame; float openedAt;
        TownData town; ItemData item; bool selling; int count; float max; int baseTotal; ReputationStanding standing;
        Action<float, bool> done;
        int temperament, like, dislike; bool likeKnown, dislikeKnown;
        readonly List<Card> deck = new List<Card>();
        readonly List<Card> hand = new List<Card>();
        System.Random rng;
        int turn, irk, selected; float mind, shownMind; bool ledgerBoost;
        enum Phase { Play, React, Result }
        Phase phase; float phaseAt;
        string line = "", lineFull = ""; float lineAt;
        int lastGain; Tag lastTag; int lastFeel; float popAt = -9f;   // lastFeel: 1 좋아함, -1 싫어함
        Card flying; Rect flyFrom; float flyAt = -9f;
        readonly List<(Card card, int gain, int feel)> played = new List<(Card, int, int)>();   // 낸 카드 (결과 때 아래 줄에 보인다)
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
            inst.Begin(town, item, selling, count, max, standing, baseTotal, done);
            return true;
        }

        void Begin(TownData t, ItemData it, bool sell, int n, float mx, ReputationStanding st, int total, Action<float, bool> cb)
        {
            town = t; item = it; selling = sell; count = n; max = mx; standing = st; baseTotal = total; done = cb;
            var gm = GameManager.I; int day = gm != null ? gm.Clock.day : 0;
            rng = new System.Random((int)(Hash($"{day}|{t.id}|{it.id}|{(sell ? 1 : 0)}") & 0x7fffffff));
            temperament = HaggleRules.Temperament(t.id);
            ulong h = Hash("taste|" + t.id);
            like = (int)(h % 4); dislike = (like + 1 + (int)((h / 4) % 3)) % 4;
            likeKnown = gm != null && gm.GetMemo(MemoKey("like"), 0f) > 0f;
            dislikeKnown = gm != null && gm.GetMemo(MemoKey("dislike"), 0f) > 0f;
            deck.Clear(); deck.AddRange(BaseDeck());
            string origin = gm != null && gm.Player.origin != null ? gm.Player.origin.id : "";
            deck.AddRange(OriginCards(origin));
            for (int i = deck.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (deck[i], deck[j]) = (deck[j], deck[i]); }
            played.Clear(); turn = 0; irk = 0; mind = shownMind = 0; ledgerBoost = false; failed = false; result = 0; selected = 0;
            Draw();
            string region = OriginPerks.Civ(t.region);
            merchant = Resources.Load<Sprite>("Haggle/merchant_" + region) ?? Resources.Load<Sprite>("Haggle/merchant_center");
            Say(Opening());
            phase = Phase.Play; phaseAt = Time.unscaledTime;
            open = true; openedFrame = Time.frameCount; openedAt = Time.unscaledTime;
            Sfx.Play("open");
        }

        string MemoKey(string k) => $"haggle:{town.id}:{k}";

        void Draw()
        {
            hand.Clear();
            for (int i = 0; i < HandSize && deck.Count > 0; i++) { hand.Add(deck[0]); deck.RemoveAt(0); }
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, hand.Count - 1));
        }

        string Opening()
        {
            string what = $"{item.displayName} {count}개";
            string price = selling ? $"{baseTotal}냥에 받겠소" : $"{baseTotal}냥이오";
            string mood = temperament < 0 ? "값은 깎아 드릴 생각 없소." : temperament > 0 ? "좋은 날이니 이야기는 들어 보지." : "흥정이야 늘 하는 일이지.";
            string[] hints = { "요즘 손님들이 통 말이 없어 심심하구려.", "셈만 맞으면 이야기는 빠르지.", "오래 볼 사이면 좋겠소.", "배짱 있는 상인이 마음에 들더군." };
            return $"{what}이라… {price}. {mood} {hints[like]}";
        }

        void Say(string s) { lineFull = s; line = ""; lineAt = Time.unscaledTime; }

        // ---------- 판정 ----------
        int Gain(Card c, out int irkDelta, out int feel)
        {
            float g = c.mind; irkDelta = c.irk; feel = 0;
            if (c.id == "banter") g = 4 + rng.Next(23);
            if (c.id == "bulk") g = 6 + Mathf.Min(count, 14);
            if (c.id == "regular" && standing >= ReputationStanding.Friendly) g += 8;
            if (c.id == "bluff" && temperament < 0) irkDelta += 1;
            if (c.id == "song" && temperament > 0) g *= 2f;
            if (c.tag == Tag.Count && ledgerBoost && c.id != "ledger") g *= 2f;
            if ((int)c.tag == like) { g *= 1.5f; irkDelta -= 1; feel = 1; }
            else if ((int)c.tag == dislike) { g *= 0.5f; if (c.id != "contract") irkDelta += 1; feel = -1; }
            g *= temperament < 0 ? 0.85f : temperament > 0 ? 1.15f : 1f;
            return Mathf.RoundToInt(g);
        }

        /// <summary>카드에 보일 효과: 운(너스레)은 "?", 좋고 싫음은 알게 된 것만 반영</summary>
        void Effect(Card c, out string gain, out int irkDelta)
        {
            float g = c.id == "banter" ? 15 : c.id == "bulk" ? 6 + Mathf.Min(count, 14) : c.mind;
            if (c.id == "regular" && standing >= ReputationStanding.Friendly) g += 8;
            if (c.id == "song" && temperament > 0) g *= 2f;
            if (c.tag == Tag.Count && ledgerBoost && c.id != "ledger") g *= 2f;
            int ik = c.irk + (c.id == "bluff" && temperament < 0 ? 1 : 0);
            if (likeKnown && (int)c.tag == like) { g *= 1.5f; ik -= 1; }
            if (dislikeKnown && (int)c.tag == dislike) { g *= 0.5f; if (c.id != "contract") ik += 1; }
            g *= temperament < 0 ? 0.85f : temperament > 0 ? 1.15f : 1f;
            gain = c.id == "banter" ? "+?" : $"+{Mathf.RoundToInt(g)}";
            irkDelta = ik;
        }

        void Play(int index, Rect from)
        {
            if (phase != Phase.Play || index < 0 || index >= hand.Count) return;
            var c = hand[index];
            int gain = Gain(c, out int dIrk, out int feel);
            mind = Mathf.Clamp(mind + gain, 0, 100);
            irk = Mathf.Clamp(irk + dIrk, 0, IrkLimit);
            ledgerBoost = c.id == "ledger" || (ledgerBoost && c.tag != Tag.Count);
            lastGain = gain; lastTag = c.tag; lastFeel = feel; popAt = Time.unscaledTime;
            flying = c; flyFrom = from; flyAt = Time.unscaledTime; played.Add((c, gain, feel));
            var gm = GameManager.I;
            if (feel > 0 && !likeKnown) { likeKnown = true; gm?.SetMemo(MemoKey("like"), 1f); }
            if (feel < 0 && !dislikeKnown) { dislikeKnown = true; gm?.SetMemo(MemoKey("dislike"), 1f); }
            Say(Reaction(c, feel, gain));
            Sfx.Play(feel > 0 ? "success" : feel < 0 ? "bump" : "accept", .7f);
            hand.RemoveAt(index);
            turn++;
            phase = Phase.React; phaseAt = Time.unscaledTime;
        }

        string Reaction(Card c, int feel, int gain)
        {
            if (irk >= IrkLimit) return "에잇, 그만하시오! 흥정은 끝이오.";
            if (feel > 0) switch (c.tag)
                {
                    case Tag.Talk: return "허허, 말솜씨가 좋구려! (좋아하는 눈치다)";
                    case Tag.Count: return "옳거니, 셈이 밝은 분이군. (좋아하는 눈치다)";
                    case Tag.Heart: return "이리 정이 많아서야… (좋아하는 눈치다)";
                    default: return "그 배짱, 마음에 드오! (좋아하는 눈치다)";
                }
            if (feel < 0) switch (c.tag)
                {
                    case Tag.Talk: return "말만 번지르르하구려. (싫어하는 눈치다)";
                    case Tag.Count: return "셈 따지는 손님은 질색이오. (싫어하는 눈치다)";
                    case Tag.Heart: return "정으로 장사하는 거 아니오. (싫어하는 눈치다)";
                    default: return "날 겁주려는 거요? (싫어하는 눈치다)";
                }
            return gain >= 25 ? "음… 그건 일리가 있군." : gain >= 12 ? "흠, 들어는 보지." : "그게 다요?";
        }

        void AfterReact()
        {
            if (irk >= IrkLimit) { Finish(true); return; }
            if (turn >= Turns || (hand.Count == 0 && deck.Count == 0)) { Finish(false); return; }
            Draw();
            phase = Phase.Play; phaseAt = Time.unscaledTime;
        }

        float Discount => max * Mathf.Clamp01(mind / 100f);
        int PriceNow => Mathf.Max(1, Mathf.RoundToInt(baseTotal * (selling ? 1f + Discount : 1f - Discount)));
        int PriceFail => Mathf.Max(1, Mathf.RoundToInt(baseTotal * (selling ? 1f - FailPenalty : 1f + FailPenalty)));

        void Finish(bool fail)
        {
            failed = fail;
            result = fail ? -FailPenalty : Mathf.Round(Discount * 200f) / 200f;   // 0.5% 단위
            if (fail) Say(selling ? $"에잇! 흥정은 그만이오. {PriceFail}냥만 쳐 주겠소." : $"에잇! 흥정은 그만이오. {PriceFail}냥 내시오.");
            else if (result <= 0f) Say("흥정할 거리가 없구려. 원래 값대로 합시다.");
            else Say(selling ? $"허허, 셈이 밝구려. {PriceNow}냥에 받겠소!" : $"허허, 셈이 밝구려. {PriceNow}냥에 드리리다!");
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
            shownMind = Mathf.MoveTowards(shownMind, mind, Time.unscaledDeltaTime * 90f);
            if (Time.frameCount == openedFrame) return;
            if (phase == Phase.React) { if (Time.unscaledTime - phaseAt > 0.9f || InputReader.InteractPressed()) AfterReact(); return; }
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
        // 11/15 2판: 번잡하다는 의견 → 한 화면에 눈이 가는 곳을 셋(상인 · 값 · 카드)으로 줄였다.
        // 카드에는 갈래·이름·마음 숫자만, 설명은 고른 카드 한 줄로. 남은 말·짜증은 점 몇 개, 단축키 안내는 키캡만.
        readonly Rect[] cardRects = new Rect[HandSize];
        const float W = 400f, H = 250f;
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

            // 머리줄: 물건 · 남은 말(점)
            float hx = r.x + 16;
            if (item.icon != null) { CuteUI.Sprite(item.icon, new Rect(hx, r.y + 14, 16, 16)); hx += 20; }
            CuteUI.Label(hx, r.y + 22, $"{item.displayName} ×{count} {(selling ? "팔기" : "사기")}", CuteUI.Text, false, true);
            for (int i = 0; i < Turns; i++)
            {
                bool left = i >= turn;
                CuteUI.Circle(r.xMax - 18 - (Turns - 1 - i) * 11, r.y + 18, 8, left ? CuteUI.Deco : CuteUI.ZeroSoft, left ? CuteUI.Ink : CuteUI.Line);
            }

            // 왼쪽: 상인과 좌판
            var stage = new Rect(r.x + 14, r.y + 38, 104, 118);
            CuteUI.Box(stage, 8, CuteUI.Paper2, CuteUI.Line);
            float bob = Mathf.Round(Mathf.Sin(now * 2.2f) * 1f);
            float shake = react < .35f && lastFeel < 0 ? Mathf.Round(Mathf.Sin(react * 60f) * 2f) : 0f;
            float hop = react < .35f && lastFeel > 0 ? -Mathf.Round(Mathf.Sin(react / .35f * Mathf.PI) * 6f) : 0f;
            if (phase == Phase.Result && failed && now - phaseAt < .4f) shake = Mathf.Round(Mathf.Sin((now - phaseAt) * 60f) * 3f);
            float counterY = stage.yMax - 24;
            if (merchant != null) CuteUI.Sprite(merchant, new Rect(stage.center.x - 36 + shake, counterY - 86 + bob + hop, 72, 108));
            CuteUI.Fill(new Rect(stage.x + 1, counterY, stage.width - 2, stage.yMax - counterY - 1), Wood);
            CuteUI.Fill(new Rect(stage.x + 1, counterY, stage.width - 2, 2), WoodHi);
            string temp = temperament < 0 ? "깐깐함" : temperament > 0 ? "너그러움" : "보통";
            CuteUI.LabelCenter(stage.center.x, counterY + 12, temp, Cream, true, true);
            // 감정 (반응할 때만)
            if ((phase == Phase.React && react < .9f) || phase == Phase.Result)
            {
                bool bad = phase == Phase.Result ? failed : lastFeel < 0;
                bool love = phase == Phase.Result ? !failed && result > 0f : lastFeel > 0;
                string emo = bad ? "!!" : love ? "♥" : "…";
                float ey = stage.y + 14 - Mathf.Min(react, .25f) * 16f;
                CuteUI.Circle(stage.xMax - 28, ey, 20, CuteUI.White, CuteUI.Ink, 1, 1);
                CuteUI.LabelCenter(stage.xMax - 18, ey + 10, emo, bad ? CuteUI.Down : love ? Pink : CuteUI.Sub, false, true);
            }

            // 오른쪽: 말풍선
            float rx = stage.xMax + 12, rw = r.xMax - 16 - rx;
            var bub = new Rect(rx, stage.y, rw, 40);
            CuteUI.Box(bub, 7, CuteUI.White, CuteUI.Ink, 1, 2, CuteUI.SoftShadow);
            CuteUI.Fill(new Rect(bub.x - 4, bub.y + 13, 5, 1), CuteUI.Ink); CuteUI.Fill(new Rect(bub.x - 4, bub.y + 19, 5, 1), CuteUI.Ink);
            CuteUI.Fill(new Rect(bub.x - 4, bub.y + 13, 1, 7), CuteUI.Ink); CuteUI.Fill(new Rect(bub.x - 3, bub.y + 14, 4, 5), CuteUI.White);
            CuteUI.Wrap(bub.x + 8, bub.y + 5, bub.width - 16, line, CuteUI.Text, true);

            // 오른쪽: 값 (한눈에)
            int nowPrice = phase == Phase.Result && failed ? PriceFail : Mathf.Max(1, Mathf.RoundToInt(baseTotal * (selling ? 1f + max * shownMind / 100f : 1f - max * shownMind / 100f)));
            int diff = nowPrice - baseTotal; bool good = selling ? diff > 0 : diff < 0;
            float py = bub.yMax + 22;
            float px = CuteUI.Label(rx, py, $"{nowPrice}냥", diff == 0 ? CuteUI.Text : good ? CuteUI.Up : CuteUI.Down, false, true);
            if (diff != 0)
            {
                float ox = CuteUI.Label(px + 8, py, $"{baseTotal}냥", CuteUI.Zero, true);
                CuteUI.Fill(new Rect(px + 8, py, ox - px - 8, 1), CuteUI.Zero);
                CuteUI.Label(ox + 6, py, $"{(diff > 0 ? "+" : "−")}{Mathf.Abs(diff)}", good ? CuteUI.Up : CuteUI.Down, true, true);
            }
            // 마음 막대 + 짜증 점
            var bar = new Rect(rx, py + 14, rw - 52, 10);
            CuteUI.Bar(bar, shownMind / 100f, PinkFill, PinkHi);
            CuteUI.Label(rx, bar.yMax + 9, "마음", Pink, true, true);
            if (now - popAt < .9f && lastGain > 0)
            {
                float k = (now - popAt) / .9f; var oc = GUI.color; GUI.color = new Color(oc.r, oc.g, oc.b, 1f - k * k);
                CuteUI.Label(bar.x + bar.width * Mathf.Clamp01(mind / 100f) - 8, bar.yMax + 9 - k * 4f, $"+{lastGain}", Pink, true, true);
                GUI.color = oc;
            }
            for (int i = 0; i < IrkLimit; i++)
            {
                bool on = i < irk; float ix = bar.xMax + 10 + i * 14;
                float pulse = on && phase == Phase.React && react < .4f && i == irk - 1 ? Mathf.Round(Mathf.Sin(react * 40f) * 1.5f) : 0f;
                CuteUI.Box(new Rect(ix, bar.y - 1 + pulse, 11, 12), 3, on ? CuteUI.Down : CuteUI.White, on ? new Color32(0x9C, 0x1C, 0x36, 255) : CuteUI.Line);
                if (on) CuteUI.LabelCenter(ix + 5.5f, bar.y + 5 + pulse, "!", CuteUI.White, true, true);
            }
            CuteUI.Label(bar.xMax + 10, bar.yMax + 9, "짜증", CuteUI.Zero, true);
            // 알게 된 취향 (알 때만)
            string taste = (likeKnown ? $"♥ {TagNames[like]}" : "") + (likeKnown && dislikeKnown ? "   " : "") + (dislikeKnown ? $"✕ {TagNames[dislike]}" : "");
            if (taste.Length > 0) CuteUI.LabelRight(r.xMax - 16, bub.yMax + 22, taste, CuteUI.Sub, true);

            // 아래: 카드 · 이 값으로
            float cy = r.y + 166, cw = 82, ch = 60, gap = 8, cx0 = r.x + 14;
            for (int i = 0; i < HandSize; i++) cardRects[i] = new Rect(cx0 + i * (cw + gap), cy, cw, ch);
            float sideX = cx0 + HandSize * (cw + gap) + 4;
            var side = new Rect(sideX, cy, r.xMax - 14 - sideX, ch);
            if (phase == Phase.Play)
            {
                for (int i = 0; i < hand.Count; i++)
                {
                    var cr = cardRects[i];
                    if (cr.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseMove) selected = i;
                    bool sel = i == selected;
                    float deal = Mathf.Clamp01((now - phaseAt - i * .06f) / .18f);
                    var dr = new Rect(cr.x, cr.y + (sel ? -4f : 0f) + (1f - deal) * 14f, cr.width, cr.height);
                    var oc = GUI.color; GUI.color = new Color(oc.r, oc.g, oc.b, deal);
                    DrawCard(dr, hand[i], sel, i + 1);
                    GUI.color = oc;
                    if (GUI.Button(dr, GUIContent.none, GUIStyle.none)) Play(i, dr);
                }
                if (selected < hand.Count) CuteUI.Label(cx0, cy + ch + 12, hand[selected].desc, CuteUI.Sub, true);
            }
            else if (phase == Phase.React && flying != null)
            {
                float k = Mathf.Clamp01((now - flyAt) / .3f), e = 1f - (1f - k) * (1f - k);
                var to = new Rect(stage.center.x - 25, stage.y + 30, 50, 37);
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
                    CuteUI.Box(new Rect(cr.x, cr.y, 4, cr.height), 2, TagColors[t], TagColors[t], 0);
                    CuteUI.Label(cr.x + 10, cr.y + 14, p.card.name, CuteUI.Text, true, true);
                    CuteUI.Label(cr.x + 10, cr.y + 32, $"+{p.gain}", Pink, false, true);
                    if (p.feel != 0) CuteUI.LabelRight(cr.xMax - 8, cr.y + 32, p.feel > 0 ? "♥" : "✕", p.feel > 0 ? Pink : CuteUI.Down, false, true);
                }
            }

            if (phase == Phase.Result)
            {
                float k = Mathf.Clamp01((now - phaseAt) / .22f); float s = Mathf.Lerp(1.5f, 1f, k);
                string stamp = failed ? "토라짐" : result > 0 ? "흥정 성공" : "그대로";
                Color32 sc = failed ? CuteUI.Down : result > 0 ? CuteUI.Up : CuteUI.Sub;
                var pivot = new Vector2(side.center.x, side.y + 14);
                var m = GUI.matrix; GUIUtility.RotateAroundPivot(-8f, pivot * UIStyle.Scale); GUIUtility.ScaleAroundPivot(new Vector2(s, s), pivot * UIStyle.Scale);
                var st = new Rect(side.center.x - 36, side.y + 3, 72, 22);
                CuteUI.Box(st, 5, new Color32(255, 255, 255, 230), sc, 2);
                CuteUI.LabelCenter(st.center.x, st.center.y, stamp, sc, false, true);
                GUI.matrix = m;
                if (now - phaseAt > .35f && CuteUI.Button(new Rect(side.x, side.yMax - 24, side.width, 24), "확인")) Close(true);
            }
            else
            {
                bool can = phase == Phase.Play && turn > 0;
                var sb = new Rect(side.x, side.y + 8, side.width, 28);
                if (can) { if (CuteUI.Button(sb, "이 값으로")) Finish(false); }
                else { CuteUI.Box(sb, 8, CuteUI.ZeroSoft, CuteUI.Line); CuteUI.LabelCenter(sb.center.x, sb.center.y, "이 값으로", CuteUI.Zero, false, true); }
                CuteUI.Keycap(sb.xMax - 12, sb.y - 6, "4", true);
                CuteUI.LabelCenter(side.center.x, sb.yMax + 14, "Esc 그만두기", CuteUI.Zero, true);
            }

            // 장터 창이 클릭을 받지 않게
            if (Event.current.isMouse || Event.current.type == EventType.ScrollWheel) Event.current.Use();
        }

        void DrawCard(Rect c, Card card, bool sel, int key)
        {
            int t = (int)card.tag;
            CuteUI.Box(c, 7, CuteUI.White, sel ? CuteUI.Ink : CuteUI.Line, sel ? 2 : 1, sel ? 3 : 2, CuteUI.SoftShadow);
            float hh = Mathf.Min(16, c.height / 3f);
            CuteUI.Box(new Rect(c.x + 1, c.y + 1, c.width - 2, hh), 6, TagSoft[t], TagSoft[t], 0);
            CuteUI.Fill(new Rect(c.x + 1, c.y + hh / 2f, c.width - 2, hh / 2f + 1), TagSoft[t]);
            CuteUI.Label(c.x + 8, c.y + 8, TagNames[t], TagColors[t], true, true);
            if (likeKnown && t == like) CuteUI.LabelRight(c.xMax - 8, c.y + 8, "♥", Pink, true, true);
            else if (dislikeKnown && t == dislike) CuteUI.LabelRight(c.xMax - 8, c.y + 8, "✕", CuteUI.Down, true, true);
            if (c.height < 40) return;
            CuteUI.Label(c.x + 8, c.y + 27, card.name, CuteUI.Text, true, true);
            Effect(card, out string gain, out int ik);
            float x = CuteUI.Label(c.x + 8, c.yMax - 12, gain, Pink, false, true);
            if (ik != 0) CuteUI.Label(x + 5, c.yMax - 11, ik > 0 ? $"짜증+{ik}" : $"짜증{ik}", ik > 0 ? CuteUI.Down : CuteUI.Up, true);
            if (key > 0) CuteUI.Keycap(c.xMax - 10, c.y - 5, key.ToString(), true);
        }

        static ulong Hash(string s)
        {
            ulong h = 14695981039346656037UL;
            foreach (char c in s) { h ^= c; h *= 1099511628211UL; }
            return h;
        }
    }
}
