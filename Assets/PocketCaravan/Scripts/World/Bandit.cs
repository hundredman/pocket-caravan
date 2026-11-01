using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 도적. 가까이 오면 쫓아오고, 잡히면 통행료를 내거나 가장 비싼 짐을 빼앗긴다.
    /// 짐을 실은 걸음보다 조금 빠르고 달리기보다 느리다: 달려서 따돌릴 수 있다.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public class Bandit : Interactable
    {
        public enum State { Lurk, Chase, Leave }

        public Vector3 home;
        public float sightRange = 6f;
        [Tooltip("쫓는 속도 (칸/초)")] public float chaseSpeed = 3.4f;
        public float giveUpRange = 10f;
        public CharacterAnimator anim;
        public SpriteRenderer alert;

        PlayerMotor motor;
        State state = State.Lurk;
        float stateTime, wanderTimer;
        Vector2 wanderDir;
        bool met;
        // 11/01(기우 유저테스트) 맵에 들어오자마자 잡히지 않게: 이 맵에 들어온 뒤 잠깐은 쫓지 않는다
        [Tooltip("맵에 들어온 뒤 이 시간(초) 동안은 쫓지 않음")] public float entryGrace = 3f;
        MapRoot myRoot; float enteredAt = -1f;
        // 11/01 통행료를 받거나 따돌려지면 그 자리에서 사라지지 않고 플레이어와 먼 출구로 걸어 나간다
        Vector3? leaveTarget; Vector3 lastPos; float stuckCheck, fadeOut; int pathAt;
        readonly System.Collections.Generic.List<Vector3> path = new System.Collections.Generic.List<Vector3>();   // 출구까지 걷는 칸 (발 위치 기준)

        public override string DisplayName => "도적";
        public override Kind Category => Kind.Danger;

        void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            blocksMovement = false;
            myRoot = GetComponentInParent<MapRoot>();
        }

        bool PlayerSettledHere()
        {
            var cur = MapRoot.Current;
            if (cur == null || (myRoot != null && cur != myRoot)) { enteredAt = -1f; return false; }
            if (enteredAt < 0f) enteredAt = Time.time;
            return Time.time - enteredAt >= entryGrace;
        }

        void Update()
        {
            var player = PlayerController.Instance;
            var gm = GameManager.I;
            if (player == null || gm == null) return;
            stateTime += Time.deltaTime;
            bool locked = UIState.Blocking || gm.IsTravelling;
            Vector2 to = player.transform.position - transform.position;
            float dist = to.magnitude;
            Vector2 dir = Vector2.zero;
            float speed = 2f;

            switch (state)
            {
                case State.Lurk:
                    wanderTimer -= Time.deltaTime;
                    if (wanderTimer <= 0f)
                    {
                        wanderTimer = Random.Range(1f, 2.5f);
                        Vector2 back = home - transform.position;
                        wanderDir = back.magnitude > 2f ? back.normalized : (Random.value < 0.4f ? Vector2.zero : Random.insideUnitCircle.normalized);
                    }
                    dir = wanderDir;
                    speed = 1.2f;
                    if (dist < sightRange && !locked && gm.Player.HasAnyCargo && PlayerSettledHere()) StartChase();
                    else if (dist >= sightRange) PlayerSettledHere();
                    break;
                case State.Chase:
                    dir = to.normalized;
                    speed = chaseSpeed;
                    if (alert != null) alert.enabled = stateTime < 1.2f;
                    if (dist < 0.75f && !locked) Catch();
                    else if (dist > giveUpRange || stateTime > 14f)
                    {
                        Notify.Show("도적을 따돌렸다!");
                        Sfx.Play("success");
                        Leave();
                    }
                    break;
                case State.Leave:
                    speed = 2.6f;
                    if (leaveTarget == null)
                    {
                        dir = (Vector2)(transform.position - player.transform.position).normalized;
                        speed = 3f;
                        if (stateTime > 3f) Destroy(gameObject);
                        break;
                    }
                    // 11/01 숲·바위에 막혀 서 있던 것: 출구까지 걸을 수 있는 칸을 따라 간다
                    Vector2 feet = motor.Feet;
                    while (pathAt < path.Count - 1 && ((Vector2)path[pathAt] - feet).magnitude < 0.3f) pathAt++;
                    Vector2 goal = path.Count > 0 ? (Vector2)path[pathAt] : (Vector2)leaveTarget.Value;
                    Vector2 toExit = goal - feet;
                    bool arrived = path.Count == 0 ? ((Vector2)leaveTarget.Value - feet).magnitude < 1.2f : pathAt >= path.Count - 1 && toExit.magnitude < 0.6f;
                    if (arrived || fadeOut > 0f)
                    {
                        fadeOut += Time.deltaTime;   // 출구에 닿으면 0.5초 동안 흐려지며 넘어간다
                        float a0 = Mathf.Clamp01(1f - fadeOut / 0.5f);
                        foreach (var sr in GetComponentsInChildren<SpriteRenderer>()) { var c = sr.color; c.a = a0; sr.color = c; }
                        if (a0 <= 0f) { Destroy(gameObject); return; }
                    }
                    // 그래도 1.5초 넘게 제자리면 길을 다시 찾는다
                    stuckCheck += Time.deltaTime;
                    if (stuckCheck >= 1.5f)
                    {
                        if ((transform.position - lastPos).magnitude < 0.3f) BuildPath(leaveTarget.Value);
                        lastPos = transform.position; stuckCheck = 0f;
                    }
                    dir = toExit.sqrMagnitude > 0.0001f ? toExit.normalized : Vector2.zero;
                    // 출구가 멀어 화면 밖으로 걸어 나가면 거기서 사라진다 (화면 밖에서 오래 남지 않게)
                    var cam = Camera.main;
                    if (cam != null && stateTime > 1f)
                    {
                        Vector3 vp = cam.WorldToViewportPoint(transform.position);
                        if (vp.x < -0.05f || vp.x > 1.05f || vp.y < -0.1f || vp.y > 1.1f) { Destroy(gameObject); return; }
                    }
                    // 너무 오래 걸리면 천천히 사라진다
                    if (stateTime > 25f)
                    {
                        float a = Mathf.Clamp01(1f - (stateTime - 25f));
                        foreach (var sr in GetComponentsInChildren<SpriteRenderer>()) { var c = sr.color; c.a = a; sr.color = c; }
                        if (a <= 0f) { Destroy(gameObject); return; }
                    }
                    break;
            }
            if (locked) dir = Vector2.zero;
            motor.Tick(dir, speed, Time.deltaTime);
            if (anim != null)
            {
                anim.facing = motor.Facing;
                anim.walking = motor.IsMoving;
                anim.framesPerSecond = 6f + motor.Velocity.magnitude * 1.2f;
            }
        }

        void StartChase()
        {
            state = State.Chase;
            stateTime = 0f;
            Sfx.Play("alert");
            AudioManager.SetDanger(true);
            Notify.Show("도적이다! Shift로 달려서 따돌리자.", 3f);
        }

        void Leave()
        {
            state = State.Leave;
            stateTime = 0f;
            leaveTarget = FarExit();
            path.Clear(); pathAt = 0;
            if (leaveTarget != null) BuildPath(leaveTarget.Value);
            lastPos = transform.position; stuckCheck = 0f; fadeOut = 0f;
            if (alert != null) alert.enabled = false;
            AudioManager.SetDanger(false);
            GameManager.I.BanditMet(MapRoot.Current != null ? MapRoot.Current.mapId : "");
        }

        /// <summary>11/01 나가는 출구: 플레이어 반대쪽(등 뒤로 가지 않는) 출구 중 도적에게 가장 가까운 곳 → 화면 안에서 걸어 나가는 게 보이게.
        /// 반대쪽 출구가 없으면 플레이어에게서 가장 먼 출구. 출구가 하나도 없으면 null (예전처럼 반대쪽으로 걷다 사라짐)</summary>
        Vector3? FarExit()
        {
            var player = PlayerController.Instance;
            if (player == null) return null;
            Vector2 away = (Vector2)(transform.position - player.transform.position);
            Vector3? near = null, far = null; float nearD = float.MaxValue, farD = -1f;
            foreach (var e in MapExit.All)
            {
                if (e == null) continue;
                if (myRoot != null && e.GetComponentInParent<MapRoot>() != myRoot) continue;
                Vector2 toE = (Vector2)(e.transform.position - transform.position);
                float fromPlayer = (e.transform.position - player.transform.position).sqrMagnitude;
                if (fromPlayer > farD) { farD = fromPlayer; far = e.transform.position; }
                // 플레이어 쪽으로 되돌아오지 않는 출구 (도적→출구 방향이 플레이어→도적 방향과 90도 안쪽)
                if (away.sqrMagnitude > 0.01f && Vector2.Dot(toE.normalized, away.normalized) < -0.2f) continue;
                if (toE.sqrMagnitude < nearD) { nearD = toE.sqrMagnitude; near = e.transform.position; }
            }
            return near ?? far;
        }

        /// <summary>11/01 도적 발밑 칸에서 출구 칸까지 걸을 수 있는 칸으로 길 찾기 (너비 우선, 8방향 아님 4방향). 못 찾으면 path 비움 → 곧장 걷기</summary>
        void BuildPath(Vector3 exitPos)
        {
            path.Clear(); pathAt = 0;
            var map = MapRoot.Current;
            if (map == null) return;
            var start = motor.Cell; var goal = map.WorldToCell(exitPos);
            var prev = new System.Collections.Generic.Dictionary<Vector3Int, Vector3Int>();
            var q = new System.Collections.Generic.Queue<Vector3Int>();
            prev[start] = start; q.Enqueue(start);
            Vector3Int[] d4 = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };
            Vector3Int? found = null;
            while (q.Count > 0 && prev.Count < 20000)
            {
                var c = q.Dequeue();
                if (Mathf.Abs(c.x - goal.x) + Mathf.Abs(c.y - goal.y) <= 1) { found = c; break; }
                foreach (var d in d4)
                {
                    var n = c + d;
                    if (prev.ContainsKey(n) || !map.IsWalkable(n, motor)) continue;
                    prev[n] = c; q.Enqueue(n);
                }
            }
            if (found == null) return;
            var cells = new System.Collections.Generic.List<Vector3Int>();
            for (var c = found.Value; c != start; c = prev[c]) cells.Add(c);
            cells.Reverse();
            foreach (var c in cells) path.Add(map.CellCenter(c));
            path.Add(map.CellCenter(goal));
        }

        public override void Interact(PlayerController player)
        {
            if (state != State.Leave) Catch();
        }

        void Catch()
        {
            if (met) return;
            met = true;
            var gm = GameManager.I;
            var p = gm.Player;
            int toll = Mathf.Max(10, Mathf.RoundToInt(p.money * 0.15f));
            string loot = gm.MostValuableCargoName();
            if (loot == null)
            {
                // 11/01(기우: 소금 덩이를 들고 있는데 "빈털터리"): 뺏을 수 없는 퀘스트 짐만 있을 때도 돈이 있으면 통행료를 요구한다.
                // 거절하면 통행료의 절반을 억지로 빼앗는다. 정말 돈도 없을 때만 빈털터리 대사.
                if (p.money < 10) { DialogueUI.Show("도적", "쳇, 빈털터리잖아. 썩 꺼져!", new DialogueUI.Choice("확인", Leave)); return; }
                int forced = Mathf.Min(p.money, Mathf.Max(5, toll / 2));
                var refuse = new DialogueUI.Choice("거절한다", () => { p.money -= forced; Sfx.Play("fail"); Notify.Show($"도적이 {forced}냥을 빼앗아 갔다.", 4f); Leave(); });
                var picks = p.money >= toll
                    ? new[] { new DialogueUI.Choice($"통행료 {toll}냥", () => { p.money -= toll; Sfx.Play("coin"); Notify.Show($"통행료 {toll}냥을 냈다."); Leave(); }), refuse }
                    : new[] { refuse };
                DialogueUI.Show("도적", $"그 짐은 우리한테 쓸모가 없겠군. 대신 이 길을 지나려면 통행료 {toll}냥을 내야지. 싫으면 주머니를 털어 가겠소.", picks);
                return;
            }
            var choices = p.money >= toll
                ? new[] { new DialogueUI.Choice($"통행료 {toll}냥", () => { p.money -= toll; Sfx.Play("coin"); Notify.Show($"통행료 {toll}냥을 냈다."); Leave(); }),
                          new DialogueUI.Choice("거절한다", () => { gm.StealMostValuable(); Leave(); }) }
                : new[] { new DialogueUI.Choice("거절한다", () => { gm.StealMostValuable(); Leave(); }) };
            DialogueUI.Show("도적", $"거기 짐 실은 양반, 멈추시오! 이 길을 지나려면 통행료 {toll}냥을 내야지. 싫으면 그 {Josa.Eul(loot)} 두고 가시오.", choices);
        }

        void OnDestroy()
        {
            if (state == State.Chase) AudioManager.SetDanger(false);
        }
    }
}
