using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 아라비아 도적 — 사막 약탈자 (2026-11-18). 다른 지역처럼 짐을 들고 지나가면 나타나 통행료를 노린다.
    /// - 자리: 아라비아 사막 길 맵 세 곳에 더한다 (마을·한가운데 초원·만국 장터는 피함). 붉은 바위 협곡 자리(BanditPlacement)는 그대로 쓴다.
    /// - 모습: 남색 머리천 + 검은 머리끈 + 얼굴 가리개 + 검붉은 옷 + 창 (Resources/Arabia/Bandit/raider_{d,u,l,r}{0~3}).
    /// - 움직임: 숨은 자리 둘레에서 몇 걸음 서성이다 멈춰 두리번거린다 (사방으로 제멋대로 미끄러지지 않게).
    ///   짐 든 플레이어가 가까이 오면 모래를 튀기며 뛰쳐나와 쫓는다. 쫓기·통행료·흥정·떠나기는 공용 Bandit 그대로.
    /// 공용 Bandit·BanditSpot·BanditPlacement 코드는 바꾸지 않는다: 숨어 있는 동안만 Bandit을 잠시 끄고, 뛰쳐나올 때 다시 켠다.
    /// </summary>
    public static class ArabiaBandits
    {
        struct Spot { public string map; public Vector2 at; }

        /// <summary>새로 두는 자리 (Unity 좌표, 길 위, 출구에서 12칸 넘게, 사람·놀거리에서 떨어진 곳)</summary>
        static readonly Spot[] Spots =
        {
            new Spot { map = "arabia_farmroad", at = new Vector2(39.5f, 23.5f) },        // 나즈드 대상로: 알 아흐사 ↔ 제다 큰길 가운데
            new Spot { map = "arabia_shortcut", at = new Vector2(41.5f, 17.5f) },        // 룹알할리 빈 사막: 지름길은 빠르지만 위험
            new Spot { map = "arabia_central_dunes", at = new Vector2(105.5f, 31.5f) },  // 모래바람 오아시스: 바위 무리 옆 길
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindFirstObjectByType<Runner>() != null) return;
            var go = new GameObject("ArabiaBandits");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
        }

        class Runner : MonoBehaviour
        {
            void OnEnable() => SceneManager.sceneLoaded += OnLoaded;
            void OnDisable() => SceneManager.sceneLoaded -= OnLoaded;
            void OnLoaded(Scene s, LoadSceneMode m) => StartCoroutine(Setup(s));

            IEnumerator Setup(Scene scene)
            {
                yield return null;                       // 공용 BanditPlacement가 먼저 자리를 놓게 한 프레임 기다린다
                MapRoot root = null;
                foreach (var go in scene.GetRootGameObjects()) { root = go.GetComponentInChildren<MapRoot>(true); if (root != null) break; }
                if (root == null || root.mapId == null || !root.mapId.StartsWith("arabia_")) yield break;

                foreach (var spot in Spots)
                {
                    if (spot.map != root.mapId) continue;
                    if (root.GetComponentInChildren<BanditSpot>(true) != null) break;   // 이미 있으면 그대로
                    var prefab = Resources.Load<GameObject>("Bandit/Bandit_north");
                    if (prefab == null) break;
                    var pos = new Vector3(spot.at.x, spot.at.y, 0.5f);
                    var bandit = Instantiate(prefab, pos, Quaternion.identity, root.transform);
                    bandit.name = "도적 (사막 약탈자)";
                    bandit.SetActive(false);
                    bandit.AddComponent<ArabiaRaider>();
                    var holder = new GameObject("도적 출몰 자리");
                    holder.transform.SetParent(root.transform, false);
                    holder.transform.position = pos;
                    var bs = holder.AddComponent<BanditSpot>();
                    bs.prefabAnimator = bandit.GetComponent<CharacterAnimator>();
                }
                // 아라비아 맵의 모든 도적(협곡 포함)을 사막 약탈자로
                foreach (var b in root.GetComponentsInChildren<Bandit>(true))
                    if (b.GetComponent<ArabiaRaider>() == null) b.gameObject.AddComponent<ArabiaRaider>();
            }
        }
    }

    /// <summary>사막 약탈자 모습과 숨어 있을 때의 움직임. 뛰쳐나온 뒤엔 공용 Bandit이 맡는다.</summary>
    [RequireComponent(typeof(Bandit))]
    public class ArabiaRaider : MonoBehaviour
    {
        enum Phase { Warmup, Ambush, Reveal, Free }

        Bandit bandit;
        PlayerMotor motor;
        CharacterAnimator anim;
        Phase phase = Phase.Warmup;
        Vector3 home, target, revealBase;
        bool moving;
        float wait, lookT, readyAt, revealT, moveT;
        int warm;
        Vector2Int look = Vector2Int.down;
        static Sprite[] frames;
        static Sprite grain;

        void Start()
        {
            bandit = GetComponent<Bandit>();
            motor = GetComponent<PlayerMotor>();
            anim = bandit.anim != null ? bandit.anim : GetComponent<CharacterAnimator>();
            ApplyLook();
            home = bandit.home != Vector3.zero ? bandit.home : transform.position;
            readyAt = Time.time + 3.5f;                  // 맵에 들어오자마자 덮치지 않게 (공용 entryGrace 3초보다 조금 길게)
            wait = Random.Range(0.5f, 2f);
        }

        void ApplyLook()
        {
            if (anim == null) return;
            if (frames == null)
            {
                var f = new Sprite[16]; string dirs = "dulr";
                for (int d = 0; d < 4; d++)
                    for (int i = 0; i < 4; i++)
                    {
                        f[d * 4 + i] = Resources.Load<Sprite>($"Arabia/Bandit/raider_{dirs[d]}{i}");
                        if (f[d * 4 + i] == null) return;   // 그림이 없으면 공용 모습 그대로
                    }
                frames = f;
            }
            anim.frames = frames;
            var sr = anim.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = frames[0];
        }

        bool Locked => UIState.Blocking || (GameManager.I != null && GameManager.I.IsTravelling);
        bool MyMap { get { var r = GetComponentInParent<MapRoot>(); return r == null || r == MapRoot.Current; } }

        // 11/19: 아라비아 시작 튜토리얼(ArabiaIntro) 동안은 도적이 숨어 있다 (보이지도, 쫓지도 않는다).
        //   튜토리얼이 끝나는 순간 그 자리에서 모래 먼지와 함께 나타나고, 몇 초 뒤부터 평소처럼 매복한다
        bool hidden;

        void SetHidden(bool h)
        {
            hidden = h;
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = !h;
            foreach (var c in GetComponentsInChildren<Collider2D>(true)) c.enabled = !h;
            if (h) { bandit.enabled = false; moving = false; return; }
            if (phase == Phase.Reveal) phase = Phase.Ambush;
            if (phase == Phase.Warmup) { warm = 0; bandit.enabled = true; }
            else bandit.enabled = phase == Phase.Free;
            readyAt = Time.time + 3.5f;
            Puff(8);
        }

        void Update()
        {
            var pc = PlayerController.Instance; var gm = GameManager.I;
            if (bandit == null) return;
            bool hold = ArabiaIntro.Active;
            if (hold != hidden) SetHidden(hold);
            if (hidden) { if (bandit.enabled) bandit.enabled = false; TickDust(Time.deltaTime); return; }
            if (pc == null || gm == null || phase == Phase.Free) { TickDust(Time.deltaTime); return; }
            switch (phase)
            {
                case Phase.Warmup:
                    // 공용 Bandit이 두어 프레임 돌며 '맵에 들어온 때'를 기록하게 둔 뒤 끈다 (Update 순서가 정해져 있지 않아서)
                    if (++warm < 3) return;
                    bandit.enabled = false; phase = Phase.Ambush;
                    return;
                case Phase.Ambush: Ambush(pc, gm); break;
                case Phase.Reveal: Reveal(pc); break;
            }
            TickDust(Time.deltaTime);
        }

        void Ambush(PlayerController pc, GameManager gm)
        {
            float dt = Time.deltaTime;
            Vector2 to = pc.transform.position - transform.position;
            if (Locked) { motor.Tick(Vector2.zero, 1f, dt); Animate(false); return; }
            // 짐 든 플레이어가 다가오면 뛰쳐나온다
            if (to.magnitude < bandit.sightRange && gm.Player.HasAnyCargo && Time.time >= readyAt && MyMap)
            {
                phase = Phase.Reveal; revealT = 0f; revealBase = transform.position;
                look = Dir4(to); Puff(8);
                return;
            }
            if (moving)
            {
                moveT += dt;
                Vector2 d = target - transform.position;
                if (d.magnitude < 0.12f || moveT > 3f) { moving = false; wait = Random.Range(1.4f, 3.8f); motor.Tick(Vector2.zero, 1f, dt); }
                else motor.Tick(Card(d), 1.1f, dt);        // 가로·세로로 걷는다 (비스듬히 미끄러지지 않게)
            }
            else
            {
                motor.Tick(Vector2.zero, 1f, dt);
                wait -= dt; lookT -= dt;
                if (lookT <= 0f)
                {
                    lookT = Random.Range(0.8f, 1.8f);
                    // 두리번: 플레이어가 보이는 거리면 그쪽을 노려보고, 아니면 길 양쪽을 살핀다
                    look = to.magnitude < bandit.sightRange + 4f ? Dir4(to)
                         : Random.value < 0.5f ? (Random.value < 0.5f ? Vector2Int.left : Vector2Int.right) : (Random.value < 0.5f ? Vector2Int.down : Vector2Int.up);
                }
                if (wait <= 0f)
                {
                    // 숨은 자리 둘레 1.6칸 안에서 몇 걸음 (가로가 더 잦게)
                    Vector2 off = Random.value < 0.7f ? new Vector2(Random.Range(-1.6f, 1.6f), 0f) : new Vector2(0f, Random.Range(-1f, 1f));
                    var t = home + (Vector3)off;
                    var map = MapRoot.Current;
                    if (map == null || map.IsWalkable(map.WorldToCell(t), motor)) { target = t; moving = true; moveT = 0f; }
                    else wait = 0.8f;
                }
            }
            Animate(motor.IsMoving);
        }

        void Reveal(PlayerController pc)
        {
            // 0.45초: 모래를 튀기며 펄쩍 뛰어오른 뒤 쫓기 시작 (공용 Bandit이 알림·소리·추격을 맡는다)
            revealT += Time.deltaTime;
            float k = Mathf.Clamp01(revealT / 0.45f);
            float hop = Mathf.Sin(k * Mathf.PI) * 0.35f;
            transform.position = revealBase + new Vector3(0f, Mathf.Round(hop * 16f) / 16f, 0f);
            look = Dir4(pc.transform.position - transform.position);
            Animate(false);
            if (k >= 1f)
            {
                transform.position = revealBase;
                Puff(5);
                phase = Phase.Free;
                bandit.enabled = true;                    // 다음 프레임부터 공용 Bandit이 바로 쫓는다
            }
        }

        void Animate(bool walking)
        {
            if (anim == null) return;
            anim.walking = walking;
            anim.facing = walking ? motor.Facing : look;
            anim.framesPerSecond = 5f;
        }

        static Vector2 Card(Vector2 d) => Mathf.Abs(d.x) >= Mathf.Abs(d.y) ? new Vector2(Mathf.Sign(d.x), 0f) : new Vector2(0f, Mathf.Sign(d.y));
        static Vector2Int Dir4(Vector2 d) => Mathf.Abs(d.x) >= Mathf.Abs(d.y) ? new Vector2Int(d.x < 0 ? -1 : 1, 0) : new Vector2Int(0, d.y < 0 ? -1 : 1);

        // ---- 모래 먼지
        class Grain { public SpriteRenderer sr; public Vector3 vel; public float life; }
        readonly List<Grain> grains = new List<Grain>();

        void Puff(int n)
        {
            if (grain == null)
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                tex.SetPixels32(new[] { new Color32(236, 214, 168, 255), new Color32(236, 214, 168, 255), new Color32(214, 186, 134, 255), new Color32(236, 214, 168, 255) });
                tex.Apply();
                grain = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 16f);
            }
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("raider_dust");
                go.transform.position = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.45f, -0.3f), 0f);
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = grain; sr.sortingOrder = 9;
                grains.Add(new Grain { sr = sr, vel = new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(0.4f, 1.1f), 0f), life = Random.Range(0.35f, 0.6f) });
            }
        }

        void TickDust(float dt)
        {
            for (int i = grains.Count - 1; i >= 0; i--)
            {
                var g = grains[i];
                g.life -= dt;
                if (g.sr == null) { grains.RemoveAt(i); continue; }
                g.vel.y -= 3f * dt;
                g.sr.transform.position += g.vel * dt;
                var c = g.sr.color; c.a = Mathf.Clamp01(g.life / 0.3f); g.sr.color = c;
                if (g.life <= 0f) { Destroy(g.sr.gameObject); grains.RemoveAt(i); }
            }
        }

        void LateUpdate()
        {
            if (phase == Phase.Free && grains.Count > 0) TickDust(Time.deltaTime);   // 뛰쳐나온 뒤에도 먼지는 마저 사라지게
        }

        void OnDestroy()
        {
            foreach (var g in grains) if (g.sr != null) Destroy(g.sr.gameObject);
        }
    }
}
