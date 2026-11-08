using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 모래바람 오아시스(arabia_central_dunes) 살아 있는 연출 (2026-11-08 기우, 팀 브레인스토밍 1·2·4·6).
    /// 1) 대상 행렬: 낙타 넷 + 길잡이가 북동쪽 빈 모래밭(y=5 줄, x 118~164)을 동서로 천천히 오간다 (밤엔 느리게, 그림만·막지 않음). 와디 줄은 바위가 있어 옮김(11/08).
    /// 2) 밤 모닥불: 오아시스 천막 옆에 밤(19~5시)에만 불(고려 평상 불 그림 4장)과 빛. 그 옆 양탄자에 앉으면 별자리 이야기 한 줄.
    /// 4) 신기루: 낮·맑을 때 멀리 모래밭에 오아시스가 하나 더 보이다가 6칸 안으로 다가가면 사라진다. 하루 한 번, 자리는 날짜 해시.
    /// 6) 옛 길잡이 탑: 탑 앞에서 Space → "탑에서 보니 길이 보인다" + 지역 지도가 열린다.
    /// 좌표는 Maps/arabia_central_dunes.json 기준(위가 0) — 씬은 위아래 뒤집힘(RoadGrid.height-1-y).
    /// </summary>
    public class ArabiaDunesLife : MonoBehaviour
    {
        const string Map = "arabia_central_dunes";
        static readonly Vector2Int Tent = new Vector2Int(118, 24);      // 줄무늬 천막 (월드 118,136)
        static readonly Vector2Int Tower = new Vector2Int(124, 16);     // 옛 길잡이 탑 (월드 124,128), 2×? 건물
        // 와디(마른 강바닥): wadi_y(x) = 125 + 2.6 sin(x/13) + 1.3 sin(x/5.3+2) (월드) → 로컬 y-112. 신기루 자리 고를 때만 피하려고 씀
        static int WadiY(int x) => 125 + Mathf.RoundToInt(2.6f * Mathf.Sin(x / 13f) + 1.3f * Mathf.Sin(x / 5.3f + 2f)) - 112;
        const int CaravanY = 5, CaravanX0 = 118, CaravanX1 = 164;   // 북동쪽 빈 모래밭 (JSON 로컬 좌표, 전부 걸을 수 있는 줄)

        Transform root; RoadGrid road; MapRoot map;
        // 행렬
        readonly List<Transform> caravan = new List<Transform>();
        float caravanX = 20f, caravanDir = 1f;
        // 모닥불
        Transform fire; SpriteRenderer fireSr, fireGlow; Sprite[] fireFrames; bool fireOn;
        // 신기루
        Transform mirage; SpriteRenderer mirageSr; Vector3 miragePos; bool mirageGone;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<ArabiaDunesLife>() != null) return;
            var go = new GameObject("ArabiaDunesLife"); DontDestroyOnLoad(go); go.AddComponent<ArabiaDunesLife>();
        }
        void OnEnable() { SceneManager.sceneLoaded += OnLoaded; Setup(); }
        void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; Teardown(); }
        void OnLoaded(Scene s, LoadSceneMode m) => Setup();

        Vector3 Cell(int x, int y) => map.CellCenter(new Vector3Int(x, road.height - 1 - y, 0));
        static Sprite Art(string n) => Resources.Load<Sprite>("Arabia/Fair/" + n) ?? Resources.Load<Sprite>("Arabia/Pastimes/" + n);

        void Teardown() { if (root != null) Destroy(root.gameObject); root = null; caravan.Clear(); fire = null; mirage = null; }

        void Setup()
        {
            Teardown();
            map = MapRoot.Current;
            if (map == null || map.previewOnly) return;
            road = map.GetComponent<RoadGrid>(); if (road == null) return;
            if (map.mapId == "arabia_central_canyon") { SetupCanyon(); return; }
            if (map.mapId != Map) return;
            root = new GameObject("모래바람 오아시스 연출").transform;

            // 1) 대상 행렬 (낙타 4 + 길잡이 1)
            for (int i = 0; i < 5; i++)
            {
                var go = new GameObject(i == 0 ? "대상 길잡이" : "대상 낙타"); go.transform.SetParent(root, false);
                var sr = go.AddComponent<SpriteRenderer>(); sr.sortingOrder = 9;
                sr.sprite = i == 0 ? (Art("bedouin_r0") ?? Art("animal_camel")) : Art("animal_camel");
                go.transform.localScale = Vector3.one * (i == 0 ? 1f : 0.45f);   // 낙타 그림(48px)은 사람보다 커서 1.3칸쯤으로
                caravan.Add(go.transform);
            }
            caravanX = Random.Range(CaravanX0 + 6f, CaravanX1 - 6f); caravanDir = Random.value < .5f ? 1f : -1f;

            // 2) 모닥불 (천막 왼쪽 아래)
            // 고려 평상 모닥불 그림은 16px 조각 넷을 2×2로 붙인 한 장 (0=왼위 1=오른위 2=왼아래 3=오른아래). 프레임이 아니라 조각이다
            fireFrames = new Sprite[4]; for (int i = 0; i < 4; i++) fireFrames[i] = Resources.Load<Sprite>("Goryeo/Seats/Fire/" + i);
            fire = new GameObject("모닥불").transform; fire.SetParent(root, false); fire.position = Cell(Tent.x + 4, Tent.y + 2);   // 천막 오른쪽 모래 위
            for (int i = 0; i < 4; i++)
            {
                var part = new GameObject("불 조각"); part.transform.SetParent(fire, false);
                part.transform.localPosition = new Vector3(i % 2 == 0 ? 0f : 1f, i < 2 ? 0f : -1f, 0f);
                var fsr = part.AddComponent<SpriteRenderer>(); fsr.sprite = fireFrames[i]; fsr.sortingOrder = 10;
            }
            fireSr = null;
            var g = new GameObject("불빛"); g.transform.SetParent(fire, false); g.transform.localPosition = new Vector3(.5f, -.3f, 0f); g.transform.localScale = Vector3.one * 4.5f;
            fireGlow = g.AddComponent<SpriteRenderer>(); fireGlow.sprite = Art("glow"); fireGlow.sortingOrder = 8;
            var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"); if (sh != null) fireGlow.sharedMaterial = new Material(sh);
            fire.gameObject.SetActive(false); fireOn = false;

            // 4) 신기루: 오늘 자리 (모래밭 어딘가, 오아시스·길에서 떨어진 곳)
            int day = GameManager.I != null ? GameManager.I.Clock.day : 0;
            var rng = new System.Random(day * 7919 + 1108);
            int mx = 60, my = 8;   // 못 고르면 북서쪽 빈 모래밭
            for (int tries = 0; tries < 60; tries++)
            {
                int tx = rng.Next(20, 160), ty = rng.Next(4, 40);
                var c = new Vector3Int(tx, road.height - 1 - ty, 0);
                if (!map.IsWalkable(c)) continue;
                if (Mathf.Abs(tx - 113) < 26 && Mathf.Abs(ty - 29) < 14) continue;      // 진짜 오아시스 근처는 안 됨
                if (Mathf.Abs(tx - 46) < 14 && Mathf.Abs(ty - 35) < 8) continue;        // 숨은 샘 근처도
                if (Mathf.Abs(ty - WadiY(tx)) < 4) continue;                             // 와디(강바닥) 위도
                mx = tx; my = ty; break;
            }
            miragePos = Cell(mx, my);
            mirage = new GameObject("신기루").transform; mirage.SetParent(root, false); mirage.position = miragePos;
            mirageSr = mirage.gameObject.AddComponent<SpriteRenderer>(); mirageSr.sprite = Art("nature_oasisPond"); mirageSr.sortingOrder = 2;
            var palm = new GameObject("신기루 야자"); palm.transform.SetParent(mirage, false); palm.transform.localPosition = new Vector3(1.4f, 1.2f, 0f);
            var psr = palm.AddComponent<SpriteRenderer>(); psr.sprite = Art("nature_datePalm"); psr.sortingOrder = 3;
            mirageGone = GameManager.I != null && Mathf.RoundToInt(GameManager.I.GetMemo("arabia:mirage:day", -1f)) == day;
            mirage.gameObject.SetActive(false);

            // 6) 탑: 탑 아래 칸에 말 걸 수 있는 자리
            PlaceLookout(Tower.x + 1, Tower.y + 4, "옛 길잡이 탑에 오르기", "탑에서 보니 길이 보인다. 북쪽이 장터, 서쪽이 오아시스.");
        }

        /// <summary>전망대 자리 (탑·아치): 아래 칸이 막혀 있으면 한 칸씩 내려가며 찾는다</summary>
        void PlaceLookout(int x, int y, string label, string line)
        {
            var tcell = new Vector3Int(x, road.height - 1 - y, 0);
            for (int dy = 0; dy < 4 && !map.IsWalkable(tcell); dy++) tcell.y -= 1;
            if (!map.IsWalkable(tcell) || Interactable.At(tcell) != null) return;
            var t = new GameObject(label); t.transform.SetParent(root, false); t.transform.position = map.CellCenter(tcell);
            var spot = t.AddComponent<TowerSpot>(); spot.label = label; spot.line = line;
        }

        /// <summary>붉은 바위 협곡: 아치 전망대만 (11/08). 나머지 연출은 오아시스 맵 전용</summary>
        void SetupCanyon()
        {
            root = new GameObject("붉은 바위 협곡 연출").transform;
            PlaceLookout(29, 25, "붉은 바위 아치에 오르기", "아치 위에서 보니 협곡이 한눈에 들어온다. 동쪽 끝이 마을, 아래가 좁은 아랫길.");
        }

        void Update()
        {
            if (root == null || GameManager.I == null || caravan.Count == 0) return;   // 협곡 맵은 전망대만이라 할 게 없다
            float h = Mathf.Repeat(GameManager.I.Clock.TotalHours, 24f);
            bool night = h >= 19f || h < 5f;
            float dt = Time.deltaTime;

            // 1) 행렬: 낮에 와디를 따라, 밤엔 오아시스 근처에 멈춤
            // 밤엔 조금 느리게 (11/08 밤에도 움직이게)
            caravanX += caravanDir * dt * (night ? 0.6f : 0.9f);
            if (caravanX > CaravanX1) caravanDir = -1f; if (caravanX < CaravanX0) caravanDir = 1f;
            for (int i = 0; i < caravan.Count; i++)
            {
                float x = caravanX - caravanDir * i * 1.1f;
                int xi = Mathf.Clamp(Mathf.RoundToInt(x), 0, road.width - 1);
                var p = Cell(xi, CaravanY) + new Vector3(x - xi, .3f + Mathf.Abs(Mathf.Sin(Time.time * 5f + i)) * .08f, 0f);
                caravan[i].position = p;
                var sr = caravan[i].GetComponent<SpriteRenderer>(); if (sr != null) sr.flipX = i == 0 ? caravanDir < 0f : caravanDir > 0f;   // 낙타 그림은 원래 왼쪽을 봄, 길잡이(r0)는 오른쪽
            }

            // 2) 모닥불
            if (fire != null)
            {
                if (night != fireOn) { fireOn = night; fire.gameObject.SetActive(night); }
                if (night && fireGlow != null) fireGlow.color = new Color(1f, .7f, .35f, .5f + .12f * Mathf.Sin(Time.time * 7f));   // 불빛만 일렁임
            }

            // 4) 신기루: 낮·모래바람 아닐 때, 멀리서만
            if (mirage != null)
            {
                var pc = PlayerController.Instance;
                bool show = !night && !mirageGone && pc != null && h >= 9f && h < 17f;
                if (show)
                {
                    float d = Vector3.Distance(pc.transform.position, miragePos);
                    if (d < 6f) { mirageGone = true; GameManager.I.SetMemo("arabia:mirage:day", GameManager.I.Clock.day); Notify.Show("……신기루였다. 물은 없다.", 3f); show = false; }
                    else
                    {
                        float a = Mathf.Clamp01((d - 6f) / 10f) * (.55f + .1f * Mathf.Sin(Time.time * 1.3f));
                        mirageSr.color = new Color(1f, 1f, 1f, a);
                        var psr = mirage.GetChild(0).GetComponent<SpriteRenderer>(); if (psr != null) psr.color = new Color(1f, 1f, 1f, a);
                        mirage.position = miragePos + new Vector3(Mathf.Sin(Time.time * 2f) * .06f, 0f, 0f);   // 아지랑이
                    }
                }
                if (mirage.gameObject.activeSelf != show) mirage.gameObject.SetActive(show);
            }
        }

        /// <summary>붉은 바위 협곡 해질녘(16~20시) 붉은 빛 (11/08). 다른 UI보다 뒤(GUI.depth 높게)에 화면 전체를 살짝 물들인다</summary>
        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || MapRoot.Current == null || MapRoot.Current.mapId != "arabia_central_canyon" || GameManager.I == null || GameManager.I.IsTravelling) return;
            float h = Mathf.Repeat(GameManager.I.Clock.TotalHours, 24f);
            float a = h < 16f || h >= 20f ? 0f : h < 18f ? Mathf.InverseLerp(16f, 18f, h) : Mathf.InverseLerp(20f, 18.5f, h);
            if (a <= 0.01f) return;
            GUI.depth = 50;
            var old = GUI.color; GUI.color = new Color(1f, .38f, .18f, .22f * Mathf.Clamp01(a));
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = old;
        }

        /// <summary>모닥불 옆에 앉았을 때 ArabiaSeat가 부른다: 밤이면 별자리 이야기 한 줄</summary>
        public static string NightStory()
        {
            if (MapRoot.Current == null || MapRoot.Current.mapId != Map || GameManager.I == null) return null;
            float h = Mathf.Repeat(GameManager.I.Clock.TotalHours, 24f);
            if (!(h >= 19f || h < 5f)) return null;
            string[] lines =
            {
                "길잡이 바심: \"저 밝은 별이 북쪽 별이야. 저걸 등지고 걸으면 사나로 가지.\"",
                "길잡이 바심: \"낙타자리라고 부르는 별들이야. 꼬리 끝이 동쪽 협곡을 가리켜.\"",
                "길잡이 바심: \"별이 흐려지면 모래바람이 온다는 뜻이야. 오늘 밤은 맑군.\"",
                "길잡이 바심: \"옛날엔 저 탑 꼭대기에 불을 피워 길을 알렸대. 지금은 별이 대신하지.\"",
            };
            return lines[GameManager.I.Clock.day % lines.Length];
        }

        /// <summary>옛 길잡이 탑 — 올라가서 보기 (지역 지도 열기)</summary>
        class TowerSpot : Interactable
        {
            public string label = "오르기", line = "";
            public override string DisplayName => label;
            public override Kind Category => Kind.Board;
            void Awake() => blocksMovement = false;
            public override void Interact(PlayerController player)
            {
                if (ArabiaMapUI.OpenFromTower() && !string.IsNullOrEmpty(line)) Notify.Show(line, 3.5f);
            }
        }
    }
}
