using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 베네치아 날씨·밤 사건 (2026-10-27, 제안). 공용 코드는 고치지 않는다.
    /// 1) 아쿠아 알타(높은 물): 닷새마다(날짜 % 5 == 2) 아침 6~12시, 석호 다섯 섬 물가 칸에 물이 차오른다.
    ///    물 찬 칸을 지나면 걸음이 0.8배(공용 PlayerMotor.walkSpeed에 배율만 곱했다가 되돌린다). 물가에서 먼 길은 그대로.
    /// 2) 길 잃은 순례자: 안개·비가 오는 동안 본토 길 맵(습지·고갯길·돌다리·동문)의 자리(VeniceLife "lost")에 나타난다.
    ///    "따라오세요" → 맵을 넘어서도 뒤따라오고, 장터 있는 마을에 함께 들어가면 사례 40냥·신용 +1·포도주 1병. 날씨 한 번에 한 명.
    /// 3) 카니발 불꽃놀이: 카니발 밤 리알토 하늘에 불꽃이 터지고 가면 장수(VeniceMaskSeller)가 선다.
    /// 진행: VeniceSave ("lost:날:블록" 만남, "lost_follow" 동행 중).
    /// </summary>
    public class VeniceEvents : MonoBehaviour
    {
        public static bool AcquaAlta
        {
            get
            {
                var gm = GameManager.I;
                return gm != null && gm.Clock.day % 5 == 2 && gm.Clock.Hour >= 6 && gm.Clock.Hour < 12;
            }
        }
        public static bool LostWeather => VeniceAtmosphere.Now != VeniceAtmosphere.Weather.Clear;
        static string LostKey => GameManager.I != null ? $"lost:{GameManager.I.Clock.day}:{GameManager.I.Clock.Hour / 4}" : "lost:-";
        public static bool Following => VeniceSave.Get("lost_follow") > 0f;

        Transform root;
        System.Collections.Generic.HashSet<Vector3Int> floodCells = new System.Collections.Generic.HashSet<Vector3Int>();
        static VeniceEvents inst;

        /// <summary>이 칸이 아쿠아 알타 때 물이 차는 칸인가 (걷기 연출 VeniceStride가 쓴다)</summary>
        public static bool IsFlooded(Vector3Int cell) => inst != null && inst.floodCells.Contains(cell);
        SpriteRenderer follower;
        VeniceMaskSeller seller;
        float baseWalk = -1f;
        bool floodShown;
        string lastAlta;
        // 불꽃
        struct Spark { public Vector2 p, v; public Color c; public float born, life; }
        readonly System.Collections.Generic.List<Spark> sparks = new System.Collections.Generic.List<Spark>();
        float nextRocket;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (GameManager.I != null && GameManager.I.GetComponent<VeniceEvents>() == null)
                GameManager.I.gameObject.AddComponent<VeniceEvents>();
        }

        void OnEnable() { inst = this; SceneManager.sceneLoaded += OnLoaded; Build(); }
        void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; ShowFlood(false); if (root != null) Destroy(root.gameObject); }
        void OnLoaded(Scene s, LoadSceneMode m) => Build();

        void Build()
        {
            if (root != null) Destroy(root.gameObject);
            ShowFlood(false);
            root = null; follower = null; floodShown = false; floodCells.Clear();
            if (!VeniceUtil.InVenice || MapRoot.Current == null) return;
            var data = VeniceData.Get(VeniceUtil.Map);
            root = new GameObject("베네치아 사건").transform;
            if (data != null)
            {
                foreach (var p in VeniceData.Points(data.flood))
                    floodCells.Add(MapRoot.Current.WorldToCell(VeniceUtil.Cell(p.x, p.y)));
                if (data.lost != null && data.lost.Length >= 2 && LostWeather && !VeniceSave.Has(LostKey) && !Following)
                {
                    var go = new GameObject("길 잃은 순례자");
                    go.transform.SetParent(root, false);
                    go.transform.position = VeniceUtil.Cell(data.lost[0], data.lost[1]);
                    go.AddComponent<VeniceLostTraveler>();
                }
            }
            if (Following)
            {
                // 동행 중인 순례자가 새 맵에서도 뒤따른다. 장터 마을에 함께 들어오면 사례.
                if (data != null && data.market != null && data.market.Length >= 2) { Arrive(); }
                else MakeFollower();
            }
        }

        void MakeFollower()
        {
            var pc = PlayerController.Instance;
            follower = new GameObject("따라오는 순례자").AddComponent<SpriteRenderer>();
            follower.transform.SetParent(root, false);
            follower.sprite = VeniceUtil.Sprite("pilgrim");
            follower.sortingOrder = 10;
            if (pc != null) follower.transform.position = pc.transform.position + new Vector3(-.8f, 0f, 0f);
        }

        static void Arrive()
        {
            var gm = GameManager.I;
            VeniceSave.Set("lost_follow", 0f);
            gm.Player.money += 40;
            gm.Player.credit += 1;
            Sfx.Play("levelup", .7f);
            var wine = VeniceUtil.Item("venice_wine");
            string gift = wine != null && gm.Player.CanCarry(wine) ? "포도주 1병" : "";
            if (gift != "") gm.Player.cargo.Add(wine, 1);
            Notify.Show($"순례자를 마을까지 데려다주었다! 사례 40냥·신용 +1{(gift != "" ? "·" + gift : "")}", 5f);
        }

        /// <summary>순례자가 "따라오세요"를 들었을 때</summary>
        public void StartFollow(VeniceLostTraveler who)
        {
            VeniceSave.Set(LostKey, 1f);
            VeniceSave.Set("lost_follow", 1f);
            if (who != null) Destroy(who.gameObject);
            MakeFollower();
            Notify.Show("순례자가 뒤따라온다. 장터 있는 마을까지 데려다주자.", 4f);
        }

        public static void Met() => VeniceSave.Set(LostKey, 1f);

        void Update()
        {
            var pc = PlayerController.Instance;
            bool inVenice = VeniceUtil.InVenice;
            // 아쿠아 알타: 물 찬 칸 그림과 걸음 배율
            if (pc != null)
            {
                bool wet = inVenice && AcquaAlta && floodCells.Contains(pc.Motor.Cell);
                if (wet && baseWalk < 0f) { baseWalk = pc.Motor.walkSpeed; pc.Motor.walkSpeed = baseWalk * .8f; }   // 물에 들어설 때만 바꾸고
                else if (!wet && baseWalk >= 0f) { pc.Motor.walkSpeed = baseWalk; baseWalk = -1f; }               // 나오면 그대로 되돌린다
            }
            if (root == null) return;
            bool alta = AcquaAlta && floodCells.Count > 0;
            if (alta != floodShown && !GameManager.I.IsTravelling) ShowFlood(alta);   // 이동이 다 끝난 뒤에 만든다 (불러오는 중에 만든 그림은 안 보였다)
            var key = GameManager.I != null ? GameManager.I.Clock.day + ":" + alta : null;
            if (alta && key != lastAlta) { lastAlta = key; Notify.Show("아쿠아 알타! 물가 길에 물이 차올랐다 (걸음이 느려진다)", 4f); }
            if (!alta) lastAlta = null;
            // 카니발 밤에만 리알토 광장에 가면 장수
            bool carnival = VeniceAtmosphere.CarnivalNight && VeniceUtil.Map == "venice_rialto";
            if (carnival && seller == null)
            {
                var data = VeniceData.Get(VeniceUtil.Map);
                if (data != null && data.wander != null && data.wander.Length >= 2)
                {
                    seller = new GameObject("가면 장수").AddComponent<VeniceMaskSeller>();
                    seller.transform.SetParent(root, false);
                    seller.transform.position = VeniceUtil.Cell(data.wander[0] - 2, data.wander[1]);
                }
            }
            else if (!carnival && seller != null) { Destroy(seller.gameObject); seller = null; }
            // 따라오는 순례자
            if (follower != null && pc != null)
            {
                var target = pc.transform.position + new Vector3(-.9f, .1f, 0f);
                follower.transform.position = Vector3.Lerp(follower.transform.position, target, Time.deltaTime * 3f);
                follower.transform.localScale = new Vector3(1f, 1f + Mathf.Abs(Mathf.Sin(Time.time * 8f)) * .04f, 1f);
            }
        }

        // 물 찬 칸: 맵 Grid 아래에 런타임 타일맵 하나 (스프라이트 584개를 따로 만들면 안 그려지는 경우가 있었고 무겁다)
        UnityEngine.Tilemaps.Tilemap puddles;
        UnityEngine.Tilemaps.Tile puddleTile;

        void ShowFlood(bool on)
        {
            floodShown = on;
            if (puddles != null) Destroy(puddles.gameObject);
            puddles = null;
            if (!on || MapRoot.Current == null) return;
            if (puddleTile == null)
            {
                puddleTile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
                puddleTile.sprite = VeniceUtil.Sprite("puddle");
            }
            var go = new GameObject("아쿠아 알타 물");
            go.transform.SetParent(MapRoot.Current.transform, false);
            puddles = go.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            go.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>().sortingOrder = 2;
            puddles.color = new Color(.42f, .68f, .78f, 1f);          // 흰 반투명 그림(알파 0.6)에 물빛을 입힌다
            foreach (var c in floodCells) puddles.SetTile(c, puddleTile);
        }

        public int PuddleCount => puddles != null ? puddles.GetUsedTilesCount() > 0 ? floodCells.Count : 0 : 0;

        // ---------------- 카니발 불꽃 ----------------
        void OnGUI()
        {
            if (!(VeniceAtmosphere.CarnivalNight && VeniceUtil.Map == "venice_rialto") || UIState.Blocking) { sparks.Clear(); return; }
            UIStyle.Begin();
            GUI.depth = 6;
            float vw = UIStyle.VirtualWidth, vh = UIStyle.VirtualHeight, now = Time.time;
            if (Event.current.type == EventType.Repaint && now > nextRocket)
            {
                nextRocket = now + Random.Range(.6f, 1.4f);
                var center = new Vector2(Random.Range(vw * .15f, vw * .85f), Random.Range(vh * .1f, vh * .35f));
                var cols = new[] { new Color(1f, .4f, .45f), new Color(1f, .85f, .35f), new Color(.45f, .7f, 1f), new Color(.6f, 1f, .6f), new Color(1f, .6f, 1f) };
                var col = cols[Random.Range(0, cols.Length)];
                for (int i = 0; i < 26; i++)
                {
                    float ang = i / 26f * Mathf.PI * 2f;
                    sparks.Add(new Spark { p = center, v = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(28f, 40f), c = col, born = now, life = 1.3f });
                }
                Sfx.Play("bump", .25f);
            }
            var old = GUI.color;
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                var s = sparks[i];
                float age = now - s.born;
                if (age > s.life) { sparks.RemoveAt(i); continue; }
                var p = s.p + s.v * age + new Vector2(0f, 14f * age * age);           // 퍼지며 천천히 떨어진다
                GUI.color = new Color(s.c.r, s.c.g, s.c.b, 1f - age / s.life);
                GUI.DrawTexture(new Rect(Mathf.Round(p.x), Mathf.Round(p.y), 2f, 2f), Texture2D.whiteTexture);
            }
            GUI.color = old;
        }
    }
}
