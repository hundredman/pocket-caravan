using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 숨은 룬돌 7개와 은빛 순록의 전설 (10/24).
    /// 북유럽 곳곳(길에서 떨어진 숲 뒤·바위 뒤)에 이끼 낀 룬돌이 숨어 있고, 밤에는 은은히 빛난다 (NorthLife 불빛).
    /// 읽을 때마다 전설 조각 하나 (NorthSave "rune:n"). 일곱 개를 다 읽고 밤에 룬돌 언덕 고리 한가운데에 서면
    /// 고리가 깨어나 오로라가 내려온다 → 상금 300냥, 신용 +2, "은빛 순록의 축복" (얼음 낚시 황금 연어·썰매 상금에 보너스).
    /// 자리: NorthLife.json "secrets" [x, y, 번호], "ring" [x, y].
    /// </summary>
    public class NorthLegends : MonoBehaviour
    {
        public static readonly string[] Places = { "", "룬돌 언덕 오솔길 남쪽 숲 틈", "피오르 길 얼어붙은 호숫가", "라우가르 온천 마을 뒤편",
            "얼음 동굴 골짜기 폭포 옆", "피오르 나루 마을 북쪽 바닷가", "모피 사냥꾼 마을 남쪽 숲", "국경 고개 내리막 바위 뒤" };

        static readonly string[] Lore =
        {
            "",
            "처음 남쪽으로 길을 떠난 상인들은 눈 속에서 길을 잃었다. 그날 밤, 하늘에 초록빛 강이 흘렀다.",
            "얼어붙은 피오르 위로 은빛 순록 한 마리가 나타나, 눈 속에 길을 밟아 보였다.",
            "순록은 김이 오르는 샘가에서 지친 상인들을 쉬게 했다. 그 샘이 오늘의 라우가르다.",
            "얼음 동굴 깊은 곳에서 순록이 발을 구르자 호박이 빛났다. 상인들은 그것을 '얼어붙은 햇빛'이라 불렀다.",
            "바다로 나간 배들이 돌아올 수 있었던 건, 밤마다 하늘에 순록의 발자국이 떴기 때문이다.",
            "사냥꾼들은 은빛 순록만은 쫓지 않았다. 그를 쫓은 자는 숲에서 길을 잃고 돌아오지 못했다.",
            "고개 꼭대기에서 순록은 남쪽을 바라보다 사라졌다. 돌에 새겨진 마지막 말: '길은 나누면 넓어진다.'",
        };

        public const int Total = 7;
        public static int Found { get { int n = 0; for (int i = 1; i <= Total; i++) if (NorthSave.Has("rune:" + i)) n++; return n; } }
        public static bool Blessed => NorthSave.Has("blessed");

        Transform root;
        string lastMap;
        AudioSource sfx;
        AudioClip chime, choir;
        float ceremony = -1f;
        readonly List<(SpriteRenderer sr, Vector3 v, float born)> motes = new List<(SpriteRenderer, Vector3, float)>();
        Sprite mote;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthLegends>() != null) return;
            var go = new GameObject("NorthLegends");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthLegends>();
        }

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>(); sfx.playOnAwake = false;
            chime = NorthUtil.Tone("north_rune_chime", 0.9f, new[] { 659f, 988f, 1319f });
            choir = NorthUtil.Tone("north_rune_choir", 2.4f, new[] { 392f, 494f, 587f, 784f, 988f, 1175f }, 0.28f);
            mote = NorthUtil.Pixel(new Color32(150, 255, 210, 255), 2, 2);
        }

        void Update()
        {
            string map = NorthUtil.Map;
            if (map != lastMap) { lastMap = map; Rebuild(map); }
            UpdateCeremony();
        }

        void Rebuild(string map)
        {
            if (root != null) Destroy(root.gameObject);
            root = null;
            if (map == null || !map.StartsWith("north_")) return;
            var s = NorthData.Get(map, "secrets");
            var ring = NorthData.Get(map, "ring");
            if (s.Length == 0 && ring.Length < 2) return;
            root = new GameObject("North Legends").transform;
            for (int i = 0; i + 2 < s.Length; i += 3)
            {
                int idx = s[i + 2];
                NorthSpot.Make(root, NorthUtil.Cell(map, s[i], s[i + 1]),
                    () => NorthSave.Has("rune:" + idx) ? "읽은 룬돌" : "이끼 낀 룬돌",
                    () => Read(idx), Interactable.Kind.Quest);
            }
            if (ring.Length >= 2)
                NorthSpot.Make(root, NorthUtil.Cell(map, ring[0], ring[1]),
                    () => Blessed ? "룬돌 고리 (축복받음)" : "룬돌 고리의 중심", Ring, Interactable.Kind.Quest);
        }

        void Read(int idx)
        {
            if (idx < 1 || idx > Total) return;
            bool first = !NorthSave.Has("rune:" + idx);
            if (first) NorthSave.Set("rune:" + idx, 1f);
            if (!AudioManager.Muted) sfx.PlayOneShot(chime, 0.6f);
            int n = Found;
            string tail = first ? $"\n\n[전설 조각 {n}/{Total}]" + (n == Total ? "  — 일곱 조각이 모였다. 밤에 룬돌 언덕 고리로 가 보자." : "") : "";
            DialogueUI.Show($"룬돌 조각 {idx}", Lore[idx] + tail);
            if (first) Notify.Show($"전설 조각을 찾았다 ({n}/{Total})", 3f);
        }

        void Ring()
        {
            if (Blessed)
            {
                DialogueUI.Show("룬돌 고리", "고리는 조용히 빛나고 있다. 은빛 순록의 축복이 너와 함께한다.\n(얼음 낚시에서 황금 연어가 더 잘 잡히고, 썰매 언덕 상금이 늘었다)");
                return;
            }
            int n = Found;
            if (n < Total)
            {
                var missing = new List<string>();
                for (int i = 1; i <= Total; i++) if (!NorthSave.Has("rune:" + i)) missing.Add("· " + Places[i]);
                DialogueUI.Show("룬돌 고리", $"고리의 돌들이 무언가를 기다리는 듯하다. 지금까지 읽은 전설 조각: {n}/{Total}\n\n아직 못 읽은 조각이 있다는 곳:\n" + string.Join("\n", missing)
                    + "\n\n(숨은 룬돌은 밤에 은은히 빛난다)");
                return;
            }
            if (!NorthUtil.Night)
            {
                DialogueUI.Show("룬돌 고리", "일곱 조각이 모였다. 하지만 고리는 잠들어 있다. 오래된 말이 떠오른다: '고리는 별빛 아래에서만 깨어난다.'");
                return;
            }
            // 깨어나는 고리
            NorthSave.Set("blessed", 1f);
            var gm = GameManager.I;
            if (gm != null) { gm.Player.money += 300; gm.Player.credit += 2; }
            ceremony = 0f;
            if (!AudioManager.Muted) sfx.PlayOneShot(choir, 0.7f);
            DialogueUI.Show("은빛 순록의 축복",
                "일곱 조각을 읊자 룬돌 고리가 하나씩 빛나기 시작했다. 하늘의 초록 강이 고리 위로 내려오고, 은빛 순록이 그 안을 걸어 지나갔다.\n\n'길을 나누는 상인에게 복이 있으라.'\n\n"
                + "[상금 300냥 · 신용 +2 · 은빛 순록의 축복]\n(얼음 낚시 황금 연어 확률 증가, 썰매 언덕 상금 최대치 +20)");
            Notify.Show("은빛 순록의 축복을 받았다!", 4f);
        }

        /// <summary>깨어나는 고리: 4초 동안 초록 빛가루가 고리에서 하늘로 오른다</summary>
        void UpdateCeremony()
        {
            if (ceremony >= 0f && MapRoot.Current != null && PlayerController.Instance != null)
            {
                ceremony += Time.deltaTime;
                var c = PlayerController.Instance.transform.position;
                for (int k = 0; k < 3; k++)
                {
                    var go = new GameObject("Rune Mote");
                    float a = Random.Range(0f, Mathf.PI * 2f), r = Random.Range(1.5f, 5.5f);
                    go.transform.position = c + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.6f, 0f);
                    var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = mote; sr.sortingOrder = 37;
                    motes.Add((sr, new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(1.5f, 3f), 0f), Time.time));
                }
                if (ceremony > 4f) ceremony = -1f;
            }
            for (int i = motes.Count - 1; i >= 0; i--)
            {
                var (sr, v, born) = motes[i];
                float age = Time.time - born;
                if (sr == null || age > 2.5f) { if (sr != null) Destroy(sr.gameObject); motes.RemoveAt(i); continue; }
                sr.transform.position += v * Time.deltaTime;
                sr.color = new Color(0.6f + 0.4f * Mathf.Sin(age * 9f), 1f, 0.85f, 1f - age / 2.5f);
            }
        }

        void OnGUI()
        {
            if (ceremony < 0f) return;
            // 화면 전체가 잠깐 초록빛으로 물듦
            var old = GUI.color;
            GUI.color = new Color(0.4f, 1f, 0.75f, 0.25f * Mathf.Sin(Mathf.Clamp01(ceremony / 4f) * Mathf.PI));
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
