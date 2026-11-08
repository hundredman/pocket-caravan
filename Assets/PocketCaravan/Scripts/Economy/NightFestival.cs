using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 밤 축제 (전 지역 공용 규칙, 2026-11-04 통합).
    /// 공통: 그 지역 수도에서 밤에 열린다. 하늘에 불꽃, 그 지역 특산품을 팔면 판 값의 3할을 더, 화면 위 축제 띠.
    /// 날짜로 정해지는 축제는 전날·당일 그 지역에 들어서면 하루 한 번 알려 준다. 지역마다 날짜를 달리해 같은 날 겹치지 않는다.
    /// - 북유럽 오로라 축제: 오로라가 뜬 밤 비르카 (불꽃·축제 생선 장수는 NorthEvents가 그대로)
    /// - 베네치아 카니발: 날짜 % 7 == 5, 19~23시 리알토 (색종이·가면·불꽃은 VeniceAtmosphere·VeniceEvents가 그대로, 덤만 여기로 옮김)
    /// - 고려 연등회: 날짜 % 7 == 2, 19~23시 개경 — 불꽃 + 떠오르는 연등 (새로)
    /// - 아라비아 향료 밤시장: 날짜 % 7 == 6, 19~23시 사나 — 불꽃 (새로)
    /// </summary>
    public static class NightFestival
    {
        public class Festival
        {
            public Reactions.Region region;
            public string name, capitalMap, capitalName, townId;
            public int dayMod = -1;                 // 날짜 % 7 == dayMod인 밤 (-1이면 날짜로 안 정해짐)
            public Func<bool> tonightOverride;      // 지역 조건으로 열리는 축제 (오로라)
            public string[] items = new string[0];  // 덤을 주는 특산품
            public string itemText;
            public float bonus = .3f;
            public bool ownFx, ownArrive;           // 지역 코드가 연출·도착 알림을 이미 한다
        }

        static GameManager GM => GameManager.I;
        static int Day => GM != null ? GM.Clock.day : 0;
        static int Hour => GM != null ? GM.Clock.Hour : 12;
        public const int StartHour = 19, EndHour = 23;

        public static readonly Festival[] Festivals =
        {
            new Festival { region = Reactions.Region.North, name = "오로라 축제", capitalMap = "north_birka", capitalName = "비르카", townId = "north_birka",
                tonightOverride = () => NorthAtmosphere.AuroraOut && NorthUtil.Night, itemText = "생선 (축제 생선 장수가 1.6배)", ownFx = true, ownArrive = true },
            new Festival { region = Reactions.Region.Venice, name = "카니발", capitalMap = "venice_rialto", capitalName = "리알토", townId = "venice_rialto",
                dayMod = 5, items = new[] { "venice_glass", "venice_glass_art", "venice_lace" }, itemText = "유리·레이스", ownFx = true, ownArrive = true },
            new Festival { region = Reactions.Region.Goryeo, name = "연등회", capitalMap = "Gaegyeong", capitalName = "개경", townId = "gaegyeong",
                dayMod = 2, items = new[] { "najeon", "ginseng", "paper" }, itemText = "나전칠기·인삼·종이" },
            new Festival { region = Reactions.Region.Arabia, name = "향료 밤시장", capitalMap = "arabia_sanaa", capitalName = "사나", townId = "arabia_sanaa",
                dayMod = 6, items = new[] { "arabia_spice", "arabia_frankincense", "arabia_perfume", "arabia_carpet", "arabia_cloth" }, itemText = "향신료·유향·향유·양탄자·직물", ownFx = true },   // 11/07 기우: 11/5 새 품목 반영, 연출은 ArabiaNightMarket
        };

        public static Festival Of(Reactions.Region r) { foreach (var f in Festivals) if (f.region == r) return f; return null; }

        static bool IsDay(Festival f, int day) => f.dayMod >= 0 && ((day % 7) + 7) % 7 == f.dayMod;
        /// <summary>지금 이 축제가 열려 있는지</summary>
        public static bool Tonight(Festival f)
        {
            if (f == null || GM == null) return false;
            if (f.tonightOverride != null) return f.tonightOverride();
            return IsDay(f, Day) && Hour >= StartHour && Hour < EndHour;
        }
        /// <summary>지금 이 맵에서 축제가 열려 있는지 (수도에서만)</summary>
        public static bool HereNow(out Festival f)
        {
            f = Of(Reactions.Here);
            return f != null && MapRoot.Current != null && MapRoot.Current.mapId == f.capitalMap && Tonight(f);
        }

        // ---------------- 실행 ----------------
        class Runner : MonoBehaviour
        {
            struct Spark { public Vector2 p, v; public Color c; public float born, life; }
            struct Lantern { public Vector2 p; public float born, life, sway; }
            readonly List<Spark> sparks = new List<Spark>();
            readonly List<Lantern> lanterns = new List<Lantern>();
            double nextRocket, nextLantern;   // 11/08 온라인이면 세상 시각 칸 (WorldBeat)
            bool wasOn;

            void OnEnable() { SceneManager.sceneLoaded += OnLoaded; Hook(); }
            void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; GameEvents.ItemSold -= OnSold; }
            void Hook() { GameEvents.ItemSold -= OnSold; GameEvents.ItemSold += OnSold; }   // 새 게임에서 구독이 지워지므로 씬마다 다시

            void OnLoaded(Scene s, LoadSceneMode m)
            {
                Hook();
                wasOn = false;
                var f = Of(Reactions.Here);
                if (f == null || GM == null || MapRoot.Current == null || f.dayMod < 0) return;
                string key = $"fest:note:{f.region}";
                if (Mathf.RoundToInt(GM.GetMemo(key, -1f)) == Day) return;
                if (IsDay(f, Day) && Hour < EndHour) { GM.SetMemo(key, Day); Notify.Show($"오늘 밤 {StartHour}시부터 {f.capitalName}에서 {f.name}! {f.itemText}를 팔면 {Mathf.RoundToInt(f.bonus * 10)}할 더", 5f); }
                else if (IsDay(f, Day + 1)) { GM.SetMemo(key, Day); Notify.Show($"내일 밤 {f.capitalName}에서 {f.name}이 열린대요 ({f.itemText} {Mathf.RoundToInt(f.bonus * 10)}할 더)", 4.5f); }
            }

            void Update()
            {
                bool on = HereNow(out var f);
                if (on && !wasOn && !f.ownArrive) Notify.Show($"{f.name}이 열렸다! 오늘 밤 {f.itemText}를 팔면 {Mathf.RoundToInt(f.bonus * 10)}할을 더 받는다.", 4.5f);
                wasOn = on;
                if (!on) { sparks.Clear(); lanterns.Clear(); }
            }

            void OnSold(ItemData item, TownData town, int price, int profit)
            {
                if (item == null || town == null || price <= 0 || GM == null) return;
                foreach (var f in Festivals)
                {
                    if (f.townId != town.id || !Tonight(f) || Array.IndexOf(f.items, item.id) < 0) continue;
                    int bonus = Mathf.Max(1, Mathf.RoundToInt(price * f.bonus));
                    GM.Player.money += bonus;
                    Notify.Show($"{f.name} 손님이 더 쳐 줬다 +{bonus}냥", 2.5f);
                    return;
                }
            }

            void OnGUI()
            {
                if (!HereNow(out var f) || UIState.Blocking || GM == null || GM.IsTravelling) return;
                UIStyle.Begin();
                float vw = UIStyle.VirtualWidth, vh = UIStyle.VirtualHeight, now = Time.time;
                if (!f.ownFx)
                {
                    GUI.depth = 6;
                    if (Event.current.type == EventType.Repaint)
                    {
                        if (WorldBeat.Due(ref nextRocket, now, .7f, 1.5f, 41 + (int)f.region, out var rr))   // 11/08 온라인: 모두 같은 때 같은 불꽃
                        {
                            var center = new Vector2(rr.Range(vw * .15f, vw * .85f), rr.Range(vh * .1f, vh * .35f));
                            var cols = f.region == Reactions.Region.Goryeo
                                ? new[] { new Color(1f, .55f, .3f), new Color(1f, .85f, .4f), new Color(.95f, .35f, .3f), new Color(.5f, .85f, .6f) }
                                : new[] { new Color(1f, .8f, .35f), new Color(.35f, .75f, .85f), new Color(.95f, .45f, .6f), new Color(.6f, .5f, 1f) };
                            var col = cols[rr.Range(0, cols.Length)];
                            for (int i = 0; i < 24; i++)
                            {
                                float ang = i / 24f * Mathf.PI * 2f;
                                sparks.Add(new Spark { p = center, v = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rr.Range(26f, 38f), c = col, born = now, life = 1.3f });
                            }
                            Sfx.Play("bump", .2f);
                        }
                        if (f.region == Reactions.Region.Goryeo && WorldBeat.Due(ref nextLantern, now, .4f, .9f, 43, out var rl))   // 연등회: 아래에서 등이 천천히 떠오른다
                        {
                            lanterns.Add(new Lantern { p = new Vector2(rl.Range(20f, vw - 20f), vh + 6f), born = now, life = rl.Range(7f, 10f), sway = rl.value * 6f });
                        }
                    }
                    var old = GUI.color;
                    for (int i = lanterns.Count - 1; i >= 0; i--)
                    {
                        var l = lanterns[i];
                        float age = now - l.born;
                        if (age > l.life) { lanterns.RemoveAt(i); continue; }
                        var p = l.p + new Vector2(Mathf.Sin(age * .8f + l.sway) * 6f, -age * (vh + 20f) / l.life);
                        float a = age < 1f ? age : Mathf.Clamp01((l.life - age) / 1.5f);
                        GUI.color = new Color(1f, .5f, .2f, .35f * a); GUI.DrawTexture(new Rect(Mathf.Round(p.x - 3), Mathf.Round(p.y - 3), 9, 11), Texture2D.whiteTexture);
                        GUI.color = new Color(1f, .72f, .35f, .95f * a); GUI.DrawTexture(new Rect(Mathf.Round(p.x), Mathf.Round(p.y), 4, 5), Texture2D.whiteTexture);
                    }
                    for (int i = sparks.Count - 1; i >= 0; i--)
                    {
                        var s = sparks[i];
                        float age = now - s.born;
                        if (age > s.life) { sparks.RemoveAt(i); continue; }
                        var p = s.p + s.v * age + new Vector2(0f, 14f * age * age);
                        GUI.color = new Color(s.c.r, s.c.g, s.c.b, 1f - age / s.life);
                        GUI.DrawTexture(new Rect(Mathf.Round(p.x), Mathf.Round(p.y), 2f, 2f), Texture2D.whiteTexture);
                    }
                    GUI.color = old;
                }
                GUI.depth = -11;
                // 11/08 UI B안: 검은 띠(y 34 고정, 이야기 줄과 겹쳤다) 대신 위 가운데 알약들 아래에 쌓는 종이 알약 (Tab 펼친 동안은 숨긴다)
                if (HUD.Expanded) return;
                string where = f.items.Length > 0 ? $"{f.itemText} {Mathf.RoundToInt(f.bonus * 10)}할 더" : "";
                CuteUI.TopPill(CuteUI.TopStack(40), false, CuteUI.Pic.Star4, CuteUI.Plain(f.name), CuteUI.Plain(where), false);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (UnityEngine.Object.FindFirstObjectByType<Runner>() != null) return;
            var go = new GameObject("NightFestival");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
        }
    }
}
