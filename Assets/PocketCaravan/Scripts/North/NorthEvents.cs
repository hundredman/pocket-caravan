using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 북유럽 날씨·밤 이벤트 (10/24).
    /// 1) 눈보라에 길 잃은 상인: 함박눈·눈보라 때 마을이 아닌 북유럽 길 맵의 돌발 사건 자리에 나타난다 (날씨 한 번에 한 명).
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
        // 길 잃은 상인
        Transform merchant;
        SpriteRenderer merchantSr;
        bool escorting;
        Sprite[] mFrames;   // d0 d1 l0 l1 r0 r1 u0 u1
        float mAnim;
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
            if (FindFirstObjectByType<NorthEvents>() != null) return;
            var go = new GameObject("NorthEvents");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthEvents>();
        }

        void Awake()
        {
            var names = new[] { "d0", "d1", "l0", "l1", "r0", "r1", "u0", "u1" };
            mFrames = new Sprite[names.Length];
            for (int i = 0; i < names.Length; i++) mFrames[i] = NorthUtil.Load("merchant_" + names[i], new Vector2(0.5f, 0.1f));
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
            UpdateMerchant();
            UpdateFestival(map);
        }

        void OnMapChanged(string from, string to)
        {
            if (root != null) Destroy(root.gameObject);
            root = null; merchant = null; festival = null;
            ClearSparks();
            if (to == null) return;
            root = new GameObject("North Events").transform;
            if (escorting)
            {
                if (NorthUtil.TownMaps.Contains(to)) FinishEscort();
                else if (!to.StartsWith("north_")) { escorting = false; Notify.Show("길 잃은 상인: \"여기서부턴 혼자 갈게요. 고마웠어요!\"", 3f); }
                else SpawnMerchant(PlayerController.Instance != null ? PlayerController.Instance.transform.position + new Vector3(0f, 1f, 0f) : Vector3.zero);
            }
            else TrySpawnLost(to);
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

        // ---------------- 길 잃은 상인 ----------------

        void TrySpawnLost(string map)
        {
            if (!map.StartsWith("north_") || NorthUtil.TownMaps.Contains(map)) return;
            if (NorthAtmosphere.Shown < NorthAtmosphere.Weather.Heavy) return;
            if (NorthSave.Has("lost:" + Block)) return;
            var ev = NorthData.Get(map, "events");
            if (ev.Length < 2) return;
            int pick = (Block % (ev.Length / 2)) * 2;
            SpawnMerchant(NorthUtil.Cell(map, ev[pick], ev[pick + 1]));
        }

        void SpawnMerchant(Vector3 pos)
        {
            if (mFrames[0] == null) return;
            var go = new GameObject("Lost Merchant");
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            merchant = go.transform;
            merchantSr = go.AddComponent<SpriteRenderer>();
            merchantSr.sprite = mFrames[0]; merchantSr.sortingOrder = 10;
            var spot = go.AddComponent<NorthSpot>();
            spot.blocksMovement = false; spot.kind = Interactable.Kind.Person;
            spot.label = () => escorting ? "길 잃은 상인 (따라오는 중)" : "눈보라에 길 잃은 상인";
            spot.action = TalkMerchant;
        }

        void TalkMerchant()
        {
            if (escorting)
            {
                DialogueUI.Show("길 잃은 상인", "장터 불빛이 보이는 마을까지만 부탁해요. 이 눈보라에선 한 치 앞도 안 보여서…");
                return;
            }
            var gm = GameManager.I;
            var mead = gm != null ? gm.ItemById("north_mead") : null;
            var choices = new List<DialogueUI.Choice> { new DialogueUI.Choice("따라오세요 (마을까지 안내)", StartEscort) };
            if (mead != null && gm.Player.cargo.Count(mead) > 0) choices.Add(new DialogueUI.Choice("꿀술 한 병 나눠 주기", GiveMead));
            choices.Add(new DialogueUI.Choice("미안해요, 바빠서", null));
            DialogueUI.Show("눈보라에 길 잃은 상인", "으으, 추워… 눈보라에 길을 잃었어요. 가까운 마을까지만 데려다줄 수 있나요? 사례는 꼭 할게요.", choices.ToArray());
        }

        void StartEscort()
        {
            escorting = true;
            NorthSave.Set("lost:" + Block, 1f);
            Notify.Show("길 잃은 상인이 뒤따라온다. 장터 있는 마을까지 데려다주자.", 3f);
        }

        void GiveMead()
        {
            var gm = GameManager.I;
            var mead = gm.ItemById("north_mead");
            if (mead == null || !gm.Player.cargo.Remove(mead, 1)) return;
            NorthSave.Set("lost:" + Block, 1f);
            var amber = gm.ItemById("north_amber");
            string gift;
            if (amber != null && gm.Player.CanCarryWeight(amber.weight - mead.weight)) { gm.Player.cargo.Add(amber, 1, 0f); gift = "호박 1개"; }
            else { gm.Player.money += 25; gift = "25냥"; }
            if (!AudioManager.Muted) sfx.PlayOneShot(thanks, 0.6f);
            DialogueUI.Show("길 잃은 상인", $"아아, 몸이 녹네요… 이걸로 기운 내서 혼자 가 볼게요. 이건 고마움의 표시예요. ({gift})");
            if (merchant != null) Destroy(merchant.gameObject);
            merchant = null;
        }

        void FinishEscort()
        {
            escorting = false;
            var gm = GameManager.I;
            if (gm == null) return;
            gm.Player.money += 40; gm.Player.credit += 1;
            string extra = "";
            var fur = gm.ItemById("north_fur");
            if (fur != null && gm.Player.CanCarry(fur, 1)) { gm.Player.cargo.Add(fur, 1, 0f); extra = " · 모피 1장"; }
            if (!AudioManager.Muted) sfx.PlayOneShot(thanks, 0.6f);
            DialogueUI.Show("길 잃은 상인", $"살았다! 마을 불빛이에요. 정말 고마워요, 이 은혜는 장부에 꼭 적어 둘게요.\n(사례 40냥 · 신용 +1{extra})");
        }

        void UpdateMerchant()
        {
            if (merchant == null || merchantSr == null) return;
            mAnim += Time.deltaTime;
            Vector3 v = Vector3.zero;
            if (escorting && PlayerController.Instance != null)
            {
                var p = PlayerController.Instance.transform.position;
                var target = p - (Vector3)((Vector2)PlayerController.Instance.Motor.Facing * 1.1f);
                var before = merchant.position;
                if ((target - merchant.position).magnitude > 0.2f)
                    merchant.position = Vector3.MoveTowards(merchant.position, target, Time.deltaTime * Mathf.Max(3f, (target - merchant.position).magnitude * 3f));
                v = merchant.position - before;
            }
            int dir = Mathf.Abs(v.x) > Mathf.Abs(v.y) ? (v.x < 0 ? 1 : 2) : (v.y > 0 ? 3 : 0);
            bool walking = v.sqrMagnitude > 0.00001f;
            var f = mFrames[dir * 2 + (walking && Mathf.Repeat(mAnim, 0.4f) > 0.2f ? 1 : 0)];
            if (f != null) merchantSr.sprite = f;
            // 눈보라 속에서 떨고 있는 모습
            if (!escorting) merchantSr.transform.localScale = new Vector3(1f + Mathf.Sin(mAnim * 40f) * 0.02f, 1f, 1f);
            else merchantSr.transform.localScale = Vector3.one;
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

        void FestivalShop()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            int total = 0, count = 0;
            foreach (var id in FishIds)
            {
                var it = gm.ItemById(id);
                if (it == null) continue;
                int n = gm.Player.cargo.Count(it);
                count += n; total += Mathf.RoundToInt(it.basePrice * 1.6f) * n;
            }
            var choices = new List<DialogueUI.Choice>();
            if (count > 0) choices.Add(new DialogueUI.Choice($"생선 {count}마리 다 팔기 ({total}냥)", SellAllFish));
            choices.Add(new DialogueUI.Choice("오로라 꿀술 한 잔 (5냥)", DrinkMead));
            choices.Add(new DialogueUI.Choice("구경만 할게요", null));
            DialogueUI.Show("축제 생선 장수", "오로라 축제다! 오늘 밤엔 생선을 평소의 1.6배로 사 줄게. 얼음 낚시로 잡은 거 있으면 다 가져와!", choices.ToArray());
        }

        void SellAllFish()
        {
            var gm = GameManager.I;
            int total = 0;
            foreach (var id in FishIds)
            {
                var it = gm.ItemById(id);
                if (it == null) continue;
                int n = gm.Player.cargo.Count(it);
                if (n > 0 && gm.Player.cargo.Remove(it, n)) total += Mathf.RoundToInt(it.basePrice * 1.6f) * n;
            }
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
