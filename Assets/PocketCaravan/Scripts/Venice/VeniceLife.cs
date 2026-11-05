using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 베네치아·본토 서부(venice_*) 맵을 살아 있게 하는 연출 (2026-10-26). 보이기만 한다: 충돌·저장·경제와 무관, 공용 코드 수정 없음.
    /// - 광장 비둘기: 모이를 쪼다가 가까이 가면 날아오르고, 조금 뒤 돌아온다.
    /// - 갈매기: 물이 넓은 맵에서 화면을 가로질러 난다 (날갯짓).
    /// - 곤돌라: 곧은 뱃길을 천천히 오간다.
    /// - 굴뚝 연기: 공방·가마·집 지붕에서 연기가 피어오른다.
    /// - 가로등: 포장길 건물 옆. 저녁·밤에는 따뜻한 빛이 켜진다(밤 어둠에 묻히지 않게 빛만 무광 재질).
    /// - 미니게임 자리를 놓는다: 무라노 유리 불기(VeniceGlassSpot), 부라노 그물 던지기(VeniceNetSpot).
    /// 자리: Resources/Venice/VeniceLife.json (Tools/MapGen/venice_life.py). 그림: Resources/Venice/Life (Tools/ArtGen/venice_life_art.py).
    /// </summary>
    public class VeniceLife : MonoBehaviour
    {
        class Pigeon { public SpriteRenderer r; public Vector3 home; public float flyT = -1f, back, next; public int frame; }
        class Gull { public SpriteRenderer r; public float speed, y, phase; public bool active; public float wait; }
        class Boat { public SpriteRenderer r; public Vector3 a, b; public float t, speed; public int dir = 1; }
        class Puff { public SpriteRenderer r; public Vector3 from; public float age; }
        class Smoke { public Vector3 at; public float next; }
        class Lamp { public SpriteRenderer glow; }

        Transform root;
        static Vector3 attractAt;
        static float attractUntil;

        /// <summary>광장 비둘기를 한동안 이 자리로 모은다 (모이 주기·리액션)</summary>
        public static void Attract(Vector3 at, float seconds) { attractAt = at; attractUntil = Time.time + seconds; }
        readonly List<Pigeon> pigeons = new List<Pigeon>();
        readonly List<Gull> gulls = new List<Gull>();
        readonly List<Boat> boats = new List<Boat>();
        readonly List<Smoke> smokes = new List<Smoke>();
        readonly List<Puff> puffs = new List<Puff>();
        readonly List<Lamp> lamps = new List<Lamp>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (GameManager.I != null && GameManager.I.GetComponent<VeniceLife>() == null)
                GameManager.I.gameObject.AddComponent<VeniceLife>();
        }

        void OnEnable() { SceneManager.sceneLoaded += OnLoaded; Build(); }
        void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; Clear(); }
        void OnLoaded(Scene s, LoadSceneMode m) => Build();

        void Clear()
        {
            if (root != null) Destroy(root.gameObject);
            root = null;
            pigeons.Clear(); gulls.Clear(); boats.Clear(); smokes.Clear(); puffs.Clear(); lamps.Clear();
        }

        SpriteRenderer Make(string sprite, Vector3 at, int order, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = at;
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = VeniceUtil.Sprite(sprite);
            r.sortingOrder = order;
            return r;
        }

        void Build()
        {
            Clear();
            if (!VeniceUtil.InVenice || MapRoot.Current == null) return;
            var data = VeniceData.Get(VeniceUtil.Map);
            if (data == null) return;
            root = new GameObject("베네치아 생활 연출").transform;
            var rng = new System.Random(VeniceUtil.Map.GetHashCode());
            foreach (var p in VeniceData.Points(data.pigeons))
            {
                var pos = VeniceUtil.Cell(p.x, p.y) + new Vector3((float)rng.NextDouble() * .6f - .3f, (float)rng.NextDouble() * .4f - .3f, 0f);
                pigeons.Add(new Pigeon { r = Make("pigeon_0", pos, 10, "비둘기"), home = pos, next = (float)rng.NextDouble() * 2f });
            }
            var lane = data.gondolas;
            for (int i = 0; lane != null && i + 3 < lane.Length; i += 4)
            {
                var a = VeniceUtil.Cell(lane[i], lane[i + 1]);
                var b = VeniceUtil.Cell(lane[i + 2], lane[i + 3]);
                boats.Add(new Boat { r = Make("gondola", a, 4, "곤돌라"), a = a, b = b, t = (float)rng.NextDouble(), speed = 1.1f + (float)rng.NextDouble() * .4f });
            }
            foreach (var p in VeniceData.Points(data.smoke))
                smokes.Add(new Smoke { at = VeniceUtil.Cell(p.x, p.y) + new Vector3(0f, .6f, 0f), next = (float)rng.NextDouble() });
            foreach (var p in VeniceData.Points(data.lamps))
            {
                var pos = VeniceUtil.Cell(p.x, p.y) + new Vector3(0f, .1f, 0f);
                Make("lantern", pos, 9, "가로등");
                var glow = Make("glow", pos + new Vector3(0f, .55f, 0f), 19, "가로등 빛");
                if (VeniceUtil.Unlit != null) glow.sharedMaterial = VeniceUtil.Unlit;
                lamps.Add(new Lamp { glow = glow });
            }
            // 미니게임 자리 (무라노 가마 앞 / 부라노 잔교 끝)
            if (data.glass != null && data.glass.Length >= 2) Spot<VeniceGlassSpot>(data.glass, "유리 가마 작업대");
            if (data.net != null && data.net.Length >= 2) Spot<VeniceNetSpot>(data.net, "그물 던지는 잔교");
            // 특산 공방 (무라노 유리 장식 / 부라노 레이스)
            if (data.workshop_glass != null && data.workshop_glass.Length >= 2) Spot<VeniceWorkshop>(data.workshop_glass, "무라노 장식 공방").recipe = VeniceWorkshop.Recipe.GlassArt;
            if (data.workshop_lace != null && data.workshop_lace.Length >= 2) Spot<VeniceWorkshop>(data.workshop_lace, "부라노 레이스 공방").recipe = VeniceWorkshop.Recipe.Lace;
            // Q3 정직 시험: 퀘스트를 하는 동안만 키오자 창고 옆에 부두 검수원
            if (data.inspector != null && data.inspector.Length >= 2 && VeniceHonesty.Active)
                Spot<VeniceHonesty>(data.inspector, "부두 검수원");
            // 리알토: 비둘기 모이 주머니, 상인회관 전시실
            if (data.feed != null && data.feed.Length >= 2) Spot<VeniceFeed>(data.feed, "비둘기 모이 주머니");
            // 2026-11-05 전시실·사자 문장은 공용 Museum·Collectibles (자리만 여기서, 규칙은 VeniceLegacy 등록)
            if (data.museum != null && data.museum.Length >= 2) Spot<Museum.Spot>(data.museum, "상인회관 전시실").region = Reactions.Region.Venice;
            if (data.lion != null && data.lion.Length >= 2)
            {
                var lion = Spot<Collectibles.Spot>(data.lion, "사자 문장");
                lion.region = Reactions.Region.Venice; lion.index = System.Array.IndexOf(VeniceLegacy.LionMaps, VeniceUtil.Map);
                lion.Dress(VeniceUtil.Sprite("lion"), VeniceUtil.Sprite("glow"));
            }
            if (data.gulls)
                for (int i = 0; i < 3; i++)
                    gulls.Add(new Gull { r = Make("gull_0", Vector3.zero, 22, "갈매기"), wait = i * 3f + (float)rng.NextDouble() * 2f });
        }

        T Spot<T>(int[] xy, string name) where T : Interactable
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = VeniceUtil.Cell(xy[0], xy[1]);
            return go.AddComponent<T>();
        }

        void Update()
        {
            if (root == null) return;
            float dt = Time.deltaTime, time = Time.time;
            var player = PlayerController.Instance;
            Vector3 me = player != null ? player.transform.position : new Vector3(9999, 9999, 0);

            // 비둘기: 쪼기·서 있기, 가까이 오면 날아오른다
            foreach (var p in pigeons)
            {
                if (p.flyT >= 0f)
                {
                    p.flyT += dt;
                    p.r.sprite = VeniceUtil.Sprite("pigeon_2");
                    p.r.transform.position = p.home + new Vector3(p.flyT * 2.2f, p.flyT * 2.6f, 0f);
                    p.r.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1.4f - p.flyT));
                    if (p.flyT > 1.5f) { p.flyT = -1f; p.back = time + 6f + Random.value * 4f; p.r.enabled = false; }
                    continue;
                }
                if (!p.r.enabled)
                {
                    if (time < p.back || (me - p.home).sqrMagnitude < 9f) continue;
                    p.r.enabled = true; p.r.color = Color.white; p.r.transform.position = p.home;
                }
                if (time < attractUntil)
                {
                    // 모이·리액션: 부른 자리 둘레로 종종걸음 (놀라 날아가지 않는다)
                    var goal = attractAt + new Vector3(Mathf.Cos(p.home.x * 3f) * .9f, Mathf.Sin(p.home.y * 3f) * .5f - .3f, 0f);
                    if ((goal - p.home).sqrMagnitude < 64f)
                    {
                        var pos = Vector3.MoveTowards(p.r.transform.position, goal, dt * 2.2f);
                        p.r.flipX = pos.x < p.r.transform.position.x;
                        p.r.transform.position = pos;
                        p.r.sprite = VeniceUtil.Sprite(((int)(time * 6f + p.home.x)) % 2 == 0 ? "pigeon_1" : "pigeon_0");
                        continue;
                    }
                }
                else if ((p.r.transform.position - p.home).sqrMagnitude > .01f)
                {
                    p.r.transform.position = Vector3.MoveTowards(p.r.transform.position, p.home, dt * 1.5f);   // 제자리로 돌아간다
                    continue;
                }
                if ((me - p.home).sqrMagnitude < 4f) { p.flyT = 0f; continue; }
                if (time >= p.next)
                {
                    p.frame = Random.value < .5f ? 1 : 0;
                    p.r.sprite = VeniceUtil.Sprite("pigeon_" + p.frame);
                    p.r.flipX = Random.value < .5f;
                    p.next = time + .3f + Random.value * 1.2f;
                }
            }

            // 곤돌라: 뱃길을 오간다 (방향 따라 그림 뒤집기, 살짝 출렁임)
            foreach (var b in boats)
            {
                float len = Mathf.Max(1f, Vector3.Distance(b.a, b.b));
                b.t += b.dir * b.speed * dt / len;
                if (b.t > 1f) { b.t = 1f; b.dir = -1; }
                if (b.t < 0f) { b.t = 0f; b.dir = 1; }
                var pos = Vector3.Lerp(b.a, b.b, b.t);
                pos.y += Mathf.Round(Mathf.Sin(time * 2f + len) * 1f) / 16f;
                b.r.transform.position = pos;
                b.r.flipX = b.dir < 0;
            }

            // 굴뚝 연기
            foreach (var s in smokes)
            {
                if (time < s.next) continue;
                s.next = time + .7f + Random.value * .5f;
                puffs.Add(new Puff { r = Make("puff", s.at, 21, "연기"), from = s.at });
            }
            for (int i = puffs.Count - 1; i >= 0; i--)
            {
                var f = puffs[i];
                f.age += dt;
                f.r.transform.position = f.from + new Vector3(Mathf.Sin(f.age * 2f) * .15f + f.age * .2f, f.age * .8f, 0f);
                f.r.transform.localScale = Vector3.one * (1f + f.age * .6f);
                f.r.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - f.age / 2.2f));
                if (f.age > 2.2f) { Destroy(f.r.gameObject); puffs.RemoveAt(i); }
            }

            // 가로등 빛: 저녁·밤에만 (조금 깜박임)
            float day = GameManager.I != null ? GameManager.I.Clock.Daylight : 1f;
            float night = Mathf.Clamp01((1f - day) * 1.6f);
            foreach (var l in lamps)
            {
                l.glow.enabled = night > .02f;
                l.glow.color = new Color(1f, 1f, 1f, night * (.85f + Mathf.Sin(time * 7f + l.glow.transform.position.x) * .08f));
            }

            // 갈매기: 카메라 화면을 가로질러 난다
            var cam = Camera.main;
            if (cam != null)
            {
                float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
                var c = cam.transform.position;
                foreach (var g in gulls)
                {
                    if (!g.active)
                    {
                        g.r.enabled = false;
                        g.wait -= dt;
                        if (g.wait > 0f) continue;
                        g.active = true;
                        g.speed = (Random.value < .5f ? -1f : 1f) * (2.2f + Random.value);
                        g.y = c.y + (Random.value * 1.4f - .7f) * halfH;
                        g.phase = Random.value * 6f;
                        g.r.transform.position = new Vector3(c.x - Mathf.Sign(g.speed) * (halfW + 1f), g.y, 0f);
                    }
                    g.r.enabled = true;
                    var pos = g.r.transform.position;
                    pos.x += g.speed * dt;
                    pos.y = g.y + Mathf.Sin(time * 1.5f + g.phase) * .4f;
                    g.r.transform.position = pos;
                    g.r.flipX = g.speed < 0f;
                    g.r.sprite = VeniceUtil.Sprite(((int)(time * 5f + g.phase)) % 2 == 0 ? "gull_0" : "gull_1");
                    if (Mathf.Abs(pos.x - c.x) > halfW + 2f) { g.active = false; g.wait = 4f + Random.value * 6f; }
                }
            }
        }
    }
}
