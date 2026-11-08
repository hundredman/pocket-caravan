using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 북유럽 날씨·밤 이벤트 (10/24).
    /// 1) 눈보라에 길 잃은 상인: 공용 LostTraveler (2026-11-05). 북유럽은 날씨(함박눈·눈보라)·자리(돌발 사건 자리)·꿀술 선택지만 등록.
    ///    - "따라오세요": 상인이 뒤따라오고, 장터 있는 마을에 함께 들어가면 사례 40냥 + 신용 +1 (+ 모피 1장, 실을 수 있으면)
    ///    - "꿀술 한 병 나눠 주기": 꿀술 1병 → 호박 1개 (실을 수 없으면 25냥)
    /// 2) 오로라 축제: 오로라가 뜬 밤의 비르카 광장. 하늘에 불꽃이 터지고, 축제 생선 장수가 물고기를 평소의 1.6배로 사 준다.
    ///    오로라 꿀술 한 잔(5냥)이면 달리기 체력이 다 찬다.
    /// 진행 기록은 NorthSave ("lost:블록" = 그 날씨 블록에 이미 만남).
    /// </summary>
    public class NorthEvents : MonoBehaviour
    {
        Transform root;
        string lastMap;
        // 길 잃은 상인은 2026-11-05부터 공용 LostTraveler (날씨·자리·꿀술 선택지는 RegisterCommon에서 등록)
        // 축제
        Transform festival;
        readonly List<(SpriteRenderer sr, Vector3 v, float born, float life)> sparks = new List<(SpriteRenderer, Vector3, float, float)>();
        Sprite spark;
        float nextBurst;
        AudioSource sfx;
        AudioClip pop, thanks;

        static readonly Color[] Colors = { new Color(0.45f, 1f, 0.7f), new Color(1f, 0.8f, 0.35f), new Color(1f, 0.45f, 0.45f), new Color(0.6f, 0.75f, 1f) };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            RegisterCommon();
            if (FindFirstObjectByType<NorthEvents>() != null) return;
            var go = new GameObject("NorthEvents");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthEvents>();
        }

        void Awake()
        {
            spark = NorthUtil.Pixel(new Color32(255, 255, 255, 255), 1, 1);
            sfx = gameObject.AddComponent<AudioSource>(); sfx.playOnAwake = false;
            pop = NorthUtil.Tone("north_pop", 0.18f, new[] { 180f, 120f }, 0.4f);
            thanks = NorthUtil.Tone("north_thanks", 0.6f, new[] { 523f, 659f, 784f });
        }

        static int Block => GameManager.I != null ? Mathf.FloorToInt(GameManager.I.Clock.TotalHours / 4f) : 0;

        void Update()
        {
            string map = NorthUtil.Map;
            if (map != lastMap) { OnMapChanged(lastMap, map); lastMap = map; }
            if (map == null) return;
            UpdateFestival(map);
        }

        void OnMapChanged(string from, string to)
        {
            if (root != null) Destroy(root.gameObject);
            root = null; festival = null;
            ClearSparks();
            if (to == null) return;
            root = new GameObject("North Events").transform;
            var fest = NorthData.Get(to, "festival");
            if (fest.Length >= 2)
            {
                festival = new GameObject("Aurora Festival").transform;
                festival.SetParent(root, false);
                festival.position = NorthUtil.Cell(to, fest[0], fest[1]);
                var spot = NorthSpot.Make(festival, festival.position, () => "축제 생선 장수", FestivalShop, Interactable.Kind.Market);
                spot.gameObject.SetActive(false);
            }
        }

        // ---------------- 길 잃은 상인 (공용 LostTraveler) ----------------

        static void RegisterCommon()
        {
            var N = Reactions.Region.North;
            LostTraveler.Weather[N] = () => NorthAtmosphere.Shown >= NorthAtmosphere.Weather.Heavy;
            LostTraveler.Place[N] = map =>
            {
                var ev = NorthData.Get(map, "events");
                if (ev.Length < 2) return null;
                int pick = (Block % (ev.Length / 2)) * 2;
                return NorthUtil.Cell(map, ev[pick], ev[pick + 1]);
            };
            LostTraveler.Extras[N] = spot =>
            {
                var gm = GameManager.I;
                var mead = gm != null ? gm.ItemById("north_mead") : null;
                var list = new List<DialogueUI.Choice>();
                if (mead != null && gm.Player.cargo.Count(mead) > 0)
                    list.Add(new DialogueUI.Choice("꿀술 한 병 나눠 주기", () =>
                    {
                        if (!gm.Player.cargo.Remove(mead, 1)) return;
                        var amber = gm.ItemById("north_amber");
                        string gift;
                        if (amber != null && gm.Player.CanCarryWeight(amber.weight - mead.weight)) { gm.Player.cargo.Add(amber, 1, 0f); gift = "호박 1개"; }
                        else { gm.Player.money += 25; gift = "25냥"; }
                        Sfx.Play("success", .7f);
                        DialogueUI.Show("길 잃은 상인", $"아아, 몸이 녹네요… 이걸로 기운 내서 혼자 가 볼게요. 이건 고마움의 표시예요. ({gift})");
                        LostTraveler.Dismiss(spot);
                    }));
                return list;
            };
        }

        // ---------------- 오로라 축제 ----------------

        void UpdateFestival(string map)
        {
            bool on = festival != null && NorthAtmosphere.AuroraOut && NorthUtil.Night;
            if (festival != null)
            {
                var shop = festival.GetComponentInChildren<NorthSpot>(true);
                if (shop != null && shop.gameObject.activeSelf != on) shop.gameObject.SetActive(on);
            }
            if (on && Time.time >= nextBurst)
            {
                nextBurst = Time.time + Random.Range(0.6f, 1.4f);
                var c = festival.position + new Vector3(Random.Range(-7f, 7f), Random.Range(4f, 8f), 0f);
                var col = Colors[Random.Range(0, Colors.Length)];
                for (int k = 0; k < 18; k++)
                {
                    float a = k / 18f * Mathf.PI * 2f;
                    var go = new GameObject("Firework");
                    go.transform.position = c;
                    var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = spark; sr.sortingOrder = 36; sr.color = col;
                    go.transform.localScale = new Vector3(2f, 2f, 1f);
                    sparks.Add((sr, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * Random.Range(2f, 3.2f), Time.time, Random.Range(0.9f, 1.4f)));
                }
                if (!AudioManager.Muted) sfx.PlayOneShot(pop, 0.35f);
            }
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                var (sr, v, born, life) = sparks[i];
                float age = Time.time - born;
                if (sr == null || age > life) { if (sr != null) Destroy(sr.gameObject); sparks.RemoveAt(i); continue; }
                v.y -= 2.5f * Time.deltaTime;
                sparks[i] = (sr, v * (1f - Time.deltaTime * 1.5f), born, life);
                sr.transform.position += v * Time.deltaTime;
                var c = sr.color; c.a = 1f - age / life; sr.color = c;
            }
        }

        void ClearSparks()
        {
            foreach (var p in sparks) if (p.sr != null) Destroy(p.sr.gameObject);
            sparks.Clear();
        }

        static readonly string[] FishIds = { "north_cod", "north_char", "north_salmon" };

        /// <summary>
        /// 11/08 축제 장수가 한 밤(게임 날짜)에 사 주는 생선 수. 예전엔 끝없이 사 줘서, 피오르 장터에서 싸게 사 온 생선을
        /// 1.6배로 몽땅 되팔 수 있었다. 얼음 낚시로 한 밤에 잡을 만한 수만 받는다 (진행 메모 north:festfish:day · :n)
        /// </summary>
        const int FestivalFishMax = 10;

        static int FishLeftTonight(GameManager gm)
        {
            if (Mathf.RoundToInt(gm.GetMemo("north:festfish:day", -1f)) != gm.Clock.day)
            {
                gm.SetMemo("north:festfish:day", gm.Clock.day);
                gm.SetMemo("north:festfish:n", 0f);
            }
            return Mathf.Max(0, FestivalFishMax - Mathf.RoundToInt(gm.GetMemo("north:festfish:n")));
        }

        void FestivalShop()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            int left = FishLeftTonight(gm), total = 0, count = 0;
            foreach (var id in FishIds)
            {
                var it = gm.ItemById(id);
                if (it == null) continue;
                int n = Mathf.Min(gm.Player.cargo.Count(it), left - count);
                if (n <= 0) continue;
                count += n; total += Mathf.RoundToInt(it.basePrice * 1.6f) * n;
            }
            var choices = new List<DialogueUI.Choice>();
            if (count > 0) choices.Add(new DialogueUI.Choice($"생선 {count}마리 팔기 ({total}냥)", SellAllFish));
            choices.Add(new DialogueUI.Choice("오로라 꿀술 한 잔 (5냥)", DrinkMead));
            choices.Add(new DialogueUI.Choice("구경만 할게요", null));
            string taught = NorthSocial.Teach("dance");   // 축제에선 다들 춤을 춘다
            string talk = left > 0
                ? $"오로라 축제다! 오늘 밤엔 생선을 평소의 1.6배로 사 줄게. 얼음 낚시로 잡은 거 있으면 가져와! (오늘 밤 {left}마리까지)"
                : "오로라 축제다! 오늘 밤 생선은 넉넉히 샀어. 다음 축제에 또 가져와!";
            DialogueUI.Show("축제 생선 장수", talk + (taught != null ? "\n\n(광장 사람들을 따라 춤을 배웠다! R)" : ""), choices.ToArray());
        }

        void SellAllFish()
        {
            var gm = GameManager.I;
            int left = FishLeftTonight(gm), total = 0, sold = 0;
            foreach (var id in FishIds)
            {
                var it = gm.ItemById(id);
                if (it == null) continue;
                int n = Mathf.Min(gm.Player.cargo.Count(it), left - sold);
                if (n > 0 && gm.Player.cargo.Remove(it, n)) { total += Mathf.RoundToInt(it.basePrice * 1.6f) * n; sold += n; }
            }
            gm.SetMemo("north:festfish:n", gm.GetMemo("north:festfish:n") + sold);
            gm.Player.money += total;
            if (!AudioManager.Muted) sfx.PlayOneShot(thanks, 0.6f);
            Notify.Show($"축제 장수에게 생선을 팔았다 (+{total}냥)", 3f);
        }

        void DrinkMead()
        {
            var gm = GameManager.I;
            if (gm.Player.money < 5) { Notify.Show("돈이 모자라…", 2f); return; }
            gm.Player.money -= 5;
            gm.Player.stamina = 1f;
            Notify.Show("오로라 꿀술을 마셨다. 몸이 후끈! (달리기 체력 가득)", 3f);
        }
    }
}
