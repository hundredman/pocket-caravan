using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 10/30 북쪽 길목 마을 남쪽 대상 야영지 (기우 기획). 공용 코드는 고치지 않는다.
    /// - 자리: NorthLife.json "caravanMerchants" [x,y]×3 (길잡이·낙타 상인·유리 상인), "caravanDeco" [종류,x,맨아랫줄 y]… (n4_build.py place_caravan)
    ///   그림은 아라비아 팀 그림을 Resources/North/Minigame/caravan_* 로 복사해 쓴다 (원본은 건드리지 않음). 막힘은 생성기가 맵에 넣어 둠.
    ///   동물은 낙타 대신 국경에서 빌린 순록(North/Critters)과 짐 썰매(10/30 사용자).
    /// - 부탁 들어주기: 하루에 한 명씩 돌아가며(날 % 3) 북유럽 특산품을 부탁한다. 짐에 있으면 말 걸어 건네기.
    ///   길잡이 = 꿀술, 낙타 상인 = 털옷, 유리 상인 = 생선(대구·곤들매기·연어 중 하나)
    ///   보상: 다른 지역 물건 1(직물 꾸러미·향신료 자루·유리그릇) + 신용 +1. 세 명 모두 한 번씩 들어주면 칭호 '남쪽 손님의 벗'.
    /// - 부탁이 없는 날·사람은 다른 지역 이야기와 시세 소문.
    /// - 저장: NorthSave "caravan:day"(오늘 부탁 끝낸 날), "caravan:helped:0~2", "caravan:friend"
    /// </summary>
    public class NorthCaravan : MonoBehaviour
    {
        const string MapId = "north_c_crossing";

        static readonly string[] Names = { "대상 길잡이 사르", "남쪽 상인 하산", "유리 상인 루카" };
        static readonly string[][] Wants = { new[] { "north_mead", "north_herbmead" }, new[] { "north_furcoat" }, new[] { "north_cod", "north_char", "north_salmon" } };
        static readonly string[] Gifts = { "arabia_cloth", "arabia_spice", "venice_glass" };
        static readonly string[] Asks =
        {
            "남쪽 초원에서 여기까지 대상을 이끌고 왔소. 밤길이 이리 추울 줄이야… 꿀술 한 병이면 밤길이 덜 추울 텐데.",
            "으으, 얼어 죽겠어요! 이 털옷이라는 거, 어디서 구할 수 있나요? 하나만 구해 주시면 은혜는 꼭 갚을게요.",
            "운하 도시에서 왔습니다. 북쪽 바다 생선이 그렇게 맛있다던데… 한 마리만 먹어 보고 싶군요.",
        };
        static readonly string[] Where =
        {
            "(꿀술은 비르카 장터에서 흔하다. 라우가르 양조장의 약초 꿀술도 된다)",
            "(털옷은 모피 사냥꾼 마을 털옷 공방에서 모피 2장으로 만들 수 있다)",
            "(얼음 대구·곤들매기·황금 연어는 얼음 낚시로 잡거나 피오르 나루 장터에서 산다)",
        };
        static readonly string[] Thanks =
        {
            "오, 이 향! 고맙소. 이건 남쪽 장터에서 가져온 직물이오. 북쪽에선 귀할 거요.",
            "따뜻해요…! 정말 고마워요. 우리 고향 향신료예요. 북쪽 사람들은 이걸 무척 좋아하더라고요.",
            "이게 북쪽 바다의 맛이군요! 답례로 운하 도시 유리그릇을 드리지요. 깨지지 않게 조심하세요.",
        };
        static readonly string[][] Chat =
        {
            new[] { "한가운데 초원은 이 마을 남쪽 길 끝이오. 네 지역 길이 거기서 만난다오.", "베네치아 쪽으로 가려면 서남 솔숲길, 동쪽 교역 길목은 동남 물가길로 가시오." },
            new[] { "국경 마을에 낙타를 맡기고 순록을 빌렸어요. 낙타는 눈길을 못 걷거든요. 순록은 추위에도 끄떡없네요!", "북쪽은 다 좋은데 해가 너무 빨리 져요!" },
            new[] { "운하 도시에서는 배가 길이고 길이 배랍니다. 여긴 눈이 길이군요.", "유리는 불과 모래로 만들지요. 얼음처럼 투명하지만 녹지는 않는답니다." },
        };
        static readonly int[] DecoW = { 2, 2, 2, 2, 2, 2, 1, 1, 2, 2 };
        // 10/30 사용자: 천막·상자·천 걸이가 커서 좌판·사람을 가림 → 작게
        static readonly float[] DecoScale = { 0.72f, 0.72f, 0.8f, 0.8f, 1f, 1f, 1f, 1f, 0.85f, 1f };
        // 10/30 낙타 대신 국경에서 빌린 순록(순록 고원 그림 North/Critters)과 짐 썰매
        static readonly string[] DecoArt = { "caravan_tent_r", "caravan_tent_b", "caravan_crates", "caravan_rack", "Critters/reindeer_graze_0", "Critters/reindeer_0", "caravan_barrel", "caravan_jar", "caravan_cart", "cargo_h" };

        Transform root;
        string lastMap;
        readonly List<(SpriteRenderer sr, Sprite[] f)> idlers = new List<(SpriteRenderer, Sprite[])>();
        readonly NorthSpot[] spots = new NorthSpot[3];
        AudioSource sfx;
        AudioClip chime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthCaravan>() != null) return;
            var obj = new GameObject("NorthCaravan");
            DontDestroyOnLoad(obj);
            obj.AddComponent<NorthCaravan>();
        }

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>(); sfx.playOnAwake = false;
            chime = NorthUtil.Tone("north_caravan_thanks", 0.5f, new[] { 784f, 988f, 1175f }, 0.3f);
        }

        static int Today => GameManager.I != null && GameManager.I.Clock != null ? GameManager.I.Clock.day : 0;
        static int Requester => ((Today % 3) + 3) % 3;
        static bool DoneToday => (int)NorthSave.Get("caravan:day", -1f) == Today;
        public static bool Friend => NorthSave.Has("caravan:friend");

        void Update()
        {
            string map = NorthUtil.Map;
            if (map != lastMap) { lastMap = map; Rebuild(map); }
            foreach (var it in idlers)
                if (it.sr != null && it.f != null && it.f.Length >= 2 && it.f[0] != null && it.f[1] != null) it.sr.sprite = it.f[(int)(Time.time * 1.5f) % 2];
        }

        void Rebuild(string map)
        {
            if (root != null) Destroy(root.gameObject);
            root = null; idlers.Clear();
            for (int i = 0; i < 3; i++) spots[i] = null;
            if (map != MapId || MapRoot.Current == null) return;
            root = new GameObject("North Caravan").transform;
            var deco = NorthData.Get(map, "caravanDeco");
            for (int i = 0; i + 2 < deco.Length; i += 3)
            {
                int kind = deco[i];
                if (kind < 0 || kind >= DecoArt.Length) continue;
                var sp = LoadArt(DecoArt[kind]);
                if (sp == null) continue;
                var go = new GameObject(DecoArt[kind]);
                go.transform.SetParent(root, false);
                go.transform.position = NorthUtil.Cell(map, deco[i + 1], deco[i + 2]) + new Vector3((DecoW[kind] - 1) * 0.5f, -0.5f, 0f);
                go.transform.localScale = new Vector3(DecoScale[kind], DecoScale[kind], 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp; sr.sortingOrder = 9;
            }
            var mer = NorthData.Get(map, "caravanMerchants");
            for (int i = 0; i + 1 < mer.Length && i / 2 < 3; i += 2)
            {
                int k = i / 2;
                var s = NorthSpot.Make(root, NorthUtil.Cell(map, mer[i], mer[i + 1]), () => Label(k), () => Talk(k), Interactable.Kind.Person);
                // 10/30 사용자: 아라비아 캐릭터 그림(32×48)은 26픽셀=1칸으로 그린다(비교 그림 B안)
                var f = new Sprite[2];
                for (int j = 0; j < 2; j++) f[j] = LoadPerson($"caravan{k}_d{j}");
                if (f[0] != null)
                {
                    var sr = s.gameObject.AddComponent<SpriteRenderer>();
                    sr.sprite = f[0]; sr.sortingOrder = 10;
                    idlers.Add((sr, f));
                }
                spots[k] = s;
            }
        }

        /// <summary>사람 그림: 16×24는 16픽셀=1칸(북유럽 사람), 32×48(아라비아·베네치아 일부)은 26픽셀=1칸. 발끝 높이를 맞춘다</summary>
        static Sprite LoadPerson(string name)
        {
            var t = Resources.Load<Texture2D>("North/Minigame/" + name);
            if (t == null) return null;
            t.filterMode = FilterMode.Point;
            bool big = t.height > 30;
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, big ? (8f + 3.9f) / 48f : 0.1f), big ? 26f : 16f);   // 10/30 기우: B안(26픽셀=1칸, 토르보다 머리 하나쯤 큼)
        }

        static Sprite LoadArt(string name)
        {
            if (!name.StartsWith("Critters/")) return NorthUtil.Load(name, new Vector2(0.5f, 0f));
            var t = Resources.Load<Texture2D>("North/" + name);
            if (t == null) return null;
            t.filterMode = FilterMode.Point;
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0f), 16f);
        }

        /// <summary>이름표: 길잡이는 늘, 오늘 부탁하는 사람은 노란 이름(부탁 중). 나머지는 이름표 없이(겹치지 않게)</summary>
        string Label(int k)
        {
            bool asking = k == Requester && !DoneToday;
            if (spots[k] != null) spots[k].kind = asking ? Interactable.Kind.Quest : Interactable.Kind.Person;
            if (asking) return Names[k] + " (부탁)";
            return k == 0 ? Names[0] : "";
        }

        static ItemData Carried(int k)
        {
            var gm = GameManager.I;
            if (gm == null) return null;
            foreach (var id in Wants[k])
            {
                var it = gm.ItemById(id);
                if (it != null && gm.Player.cargo.Count(it) > 0) return it;
            }
            return null;
        }

        void Talk(int k)
        {
            var gm = GameManager.I;
            if (gm == null) return;
            if (k == Requester && !DoneToday)
            {
                var have = Carried(k);
                if (have != null)
                {
                    DialogueUI.Show(Names[k], Asks[k],
                        new DialogueUI.Choice($"{have.displayName} 건네주기", () => Give(k, have)),
                        new DialogueUI.Choice("다음에", null));
                }
                else DialogueUI.Show(Names[k], Asks[k] + "\n\n" + Where[k]);
                return;
            }
            var lines = Chat[k];
            string text = lines[Random.Range(0, lines.Length)];
            if (k == 0 && gm.Traders != null) text += "\n\n그리고 장사꾼끼리 하는 이야기인데… " + gm.Traders.PriceTip();
            if (DoneToday && k == Requester) text = "덕분에 오늘은 살 것 같소! 내일은 다른 친구가 부탁할지도 모르오.";
            else if (k != Requester && !DoneToday) text += $"\n\n(오늘은 {Names[Requester]}이(가) 뭔가 부탁하고 싶은 눈치다)";
            DialogueUI.Show(Names[k], text);
        }

        void Give(int k, ItemData have)
        {
            var gm = GameManager.I;
            if (gm == null || have == null || DoneToday) return;
            var gift = gm.ItemById(Gifts[k]);
            if (gift != null && !gm.Player.CanCarry(gift) && gift.weight > have.weight)
            {
                DialogueUI.Show(Names[k], "어이쿠, 짐이 가득하군요. 답례품 들어갈 자리를 좀 비우고 오세요.");
                return;
            }
            if (!gm.Player.cargo.Remove(have, 1)) return;
            NorthSave.Set("caravan:day", Today);
            NorthSave.Set("caravan:helped:" + k, 1f);
            gm.Player.credit += 1;
            string got = "";
            if (gift != null && gm.Player.CanCarry(gift)) { gm.Player.cargo.Add(gift, 1, 0f); got = $"{gift.displayName} 1개 · "; }
            string text = Thanks[k] + $"\n({got}신용 +1)";
            if (!Friend && NorthSave.Has("caravan:helped:0") && NorthSave.Has("caravan:helped:1") && NorthSave.Has("caravan:helped:2"))
            {
                NorthSave.Set("caravan:friend", 1f);
                text += "\n\n사르: 우리 셋 모두 신세를 졌구려. 오늘부터 당신은 '남쪽 손님의 벗'이오!\n(칭호 '남쪽 손님의 벗')";
            }
            if (!AudioManager.Muted) sfx.PlayOneShot(chime, 0.5f);
            DialogueUI.Show(Names[k], text);
        }

        void OnGUI()
        {
            if (root == null || UIState.Blocking || DoneToday) return;
            var s = spots[Requester];
            var cam = Camera.main;
            if (s == null || cam == null) return;
            UIStyle.Begin();
            var p = cam.WorldToScreenPoint(s.transform.position + new Vector3(0f, 1.9f + Mathf.Sin(Time.time * 4f) * 0.06f, 0f));
            var g = new Vector2(p.x / UIStyle.Scale, (Screen.height - p.y) / UIStyle.Scale);
            UIStyle.Outlined(new Rect(g.x - 10, g.y - 10, 20, 18), "<color=#ffd75a><b>!</b></color>", UIStyle.CenterRich);
        }
    }
}
