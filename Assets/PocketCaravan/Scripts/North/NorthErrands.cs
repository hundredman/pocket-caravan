using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 10/30 남은 빈 곳 채우기 (기우 기획). 공용 코드는 고치지 않는다. 자리는 NorthLife.json (n4_build.py place_extras).
    /// - 얼음 동굴 골짜기: 길 잃은 행상 군나르의 잃어버린 짐 3개 "gunnar", "gunnarPacks", "gunnarHint"(0 물가·1 나무 사이·2 바위 뒤).
    ///   말 걸면 찾아 주기 시작 → 군나르가 대략의 방향만 알려 줌(화살표 없음) → 짐에서 Space로 줍기 → 셋 다 찾으면 레이스 손수건 + 신용 +1.
    ///   한 번만: 끝나면 군나르는 떠나고 꺼진 모닥불 자국이 남는다.
    /// - 개울 나루: 나무꾼 엘크 "woodcutter", 장작 패는 그루터기 "woodBlock", 모닥불 쉼터 "woodFire".
    ///   거들기 → 타이밍 놀이(움직이는 표시가 초록 칸에 올 때 Space, 6번 중 3번) → 장작 한 아름 → 모닥불에 넣으면 그날은 불이 활활. 처음엔 신용 +1.
    /// - 북문 마을 경비 훈련장 "dummies": 짚 허수아비 그림(가끔 흔들림). 경비병 둘은 맵 talker(말만 함).
    /// - 저장: NorthSave "gunnar:start", "gunnar:pack:0~2", "gunnar:done", "wood:day", "wood:carry", "wood:fire", "wood:first"
    /// </summary>
    public class NorthErrands : MonoBehaviour
    {
        const string IceMap = "north_icecave", CreekMap = "north_c_creek", GateMap = "north_c_gatetown";
        const string GiftId = "venice_lace";

        Transform root;
        string lastMap;
        readonly List<(SpriteRenderer sr, Sprite[] f)> idlers = new List<(SpriteRenderer, Sprite[])>();
        readonly List<(SpriteRenderer sr, float phase)> dummies = new List<(SpriteRenderer, float)>();
        AudioSource sfx;
        AudioClip pick, chop, miss, done;

        // 군나르
        Vector3 gunnarPos; int[] packCells = new int[0], packHint = new int[0];
        readonly List<GameObject> packObjs = new List<GameObject>();
        // 장작
        Vector3 firePos; bool hasFire;
        SpriteRenderer bigFlame; Sprite[] flames;
        enum C { None, Play, Result }
        C chopState = C.None;
        float cursor, cursorDir = 1f, zoneX, zoneW = 22f, chopT;
        int hits, tries, startFrame;
        string chopResult;
        const float BarW = 160f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthErrands>() != null) return;
            var obj = new GameObject("NorthErrands");
            DontDestroyOnLoad(obj);
            obj.AddComponent<NorthErrands>();
        }

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>(); sfx.playOnAwake = false;
            pick = NorthUtil.Tone("north_err_pick", 0.25f, new[] { 784f, 1046f }, 0.3f);
            chop = NorthUtil.Tone("north_err_chop", 0.08f, new[] { 300f, 180f }, 0.35f);
            miss = NorthUtil.Tone("north_err_miss", 0.12f, new[] { 160f }, 0.25f);
            done = NorthUtil.Tone("north_err_done", 0.5f, new[] { 784f, 988f, 1175f }, 0.3f);
            flames = new[] { NorthUtil.Load("bigflame_0", new Vector2(0.5f, 0f)), NorthUtil.Load("bigflame_1", new Vector2(0.5f, 0f)) };
        }

        void Play(AudioClip c, float v = 0.5f) { if (c != null && !AudioManager.Muted) sfx.PlayOneShot(c, v); }
        static int Today => GameManager.I != null && GameManager.I.Clock != null ? GameManager.I.Clock.day : 0;

        void Update()
        {
            string map = NorthUtil.Map;
            if (map != lastMap) { lastMap = map; Rebuild(map); }
            if (chopState != C.None) UpdateChop(Time.unscaledDeltaTime);
            if (root == null) return;
            foreach (var it in idlers)
                if (it.sr != null && it.f[0] != null && it.f[1] != null) it.sr.sprite = it.f[(int)(Time.time * 1.5f) % 2];
            foreach (var d in dummies)
            {
                if (d.sr == null) continue;
                float t = Mathf.Repeat(Time.time + d.phase, 2.4f);   // 경비병이 찌를 때마다 살짝 흔들림
                d.sr.transform.localRotation = Quaternion.Euler(0f, 0f, t < 0.3f ? Mathf.Sin(t * 40f) * 6f : 0f);
            }
            if (bigFlame != null)
            {
                bigFlame.enabled = (int)NorthSave.Get("wood:fire", -1f) == Today;
                if (bigFlame.enabled && flames[0] != null && flames[1] != null) bigFlame.sprite = flames[(int)(Time.time * 6f) % 2];
            }
        }

        void Rebuild(string map)
        {
            if (root != null) Destroy(root.gameObject);
            root = null; idlers.Clear(); dummies.Clear(); packObjs.Clear(); bigFlame = null; hasFire = false;
            if (MapRoot.Current == null || (map != IceMap && map != CreekMap && map != GateMap)) return;
            root = new GameObject("North Errands").transform;
            if (map == IceMap) BuildGunnar(map);
            if (map == CreekMap) BuildWood(map);
            if (map == GateMap)
            {
                var dm = NorthData.Get(map, "dummies");
                var sp = NorthUtil.Load("dummy", new Vector2(0.5f, 0.05f));
                for (int i = 0; i + 1 < dm.Length; i += 2)
                {
                    var sr = Art(NorthUtil.Cell(map, dm[i], dm[i + 1]) + new Vector3(0f, -0.5f, 0f), sp, 9);
                    if (sr != null) dummies.Add((sr, i * 0.8f));
                }
            }
        }

        SpriteRenderer Art(Vector3 pos, Sprite sp, int order)
        {
            if (sp == null) return null;
            var go = new GameObject("Errand Art");
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sp; sr.sortingOrder = order;
            return sr;
        }

        NorthSpot Person(Vector3 pos, string prefix, string dir, System.Func<string> label, System.Action act)
        {
            var s = NorthSpot.Make(root, pos, label, act, Interactable.Kind.Person);
            var f = new Sprite[2];
            for (int j = 0; j < 2; j++) f[j] = NorthUtil.Load($"{prefix}_{dir}{j}", new Vector2(0.5f, 0.1f));
            if (f[0] != null) { var sr = s.gameObject.AddComponent<SpriteRenderer>(); sr.sprite = f[0]; sr.sortingOrder = 10; idlers.Add((sr, f)); }
            return s;
        }

        // ---------------- 군나르의 잃어버린 짐 ----------------

        static bool GStarted => NorthSave.Has("gunnar:start");
        static bool GDone => NorthSave.Has("gunnar:done");
        static bool Found(int i) => NorthSave.Has("gunnar:pack:" + i);
        int FoundCount { get { int n = 0; for (int i = 0; i < packCells.Length / 2; i++) if (Found(i)) n++; return n; } }

        void BuildGunnar(string map)
        {
            var g = NorthData.Get(map, "gunnar");
            if (g.Length < 2) return;
            gunnarPos = NorthUtil.Cell(map, g[0], g[1]);
            packCells = NorthData.Get(map, "gunnarPacks");
            packHint = NorthData.Get(map, "gunnarHint");
            if (GDone)
            {
                Art(gunnarPos + new Vector3(0f, -0.4f, 0f), NorthUtil.Load("ashes", new Vector2(0.5f, 0f)), 8);
                return;
            }
            Person(gunnarPos, "gunnar", "d", () => "길 잃은 행상 군나르", Gunnar);
            var packSp = NorthUtil.Load("lost_pack", new Vector2(0.5f, 0f));
            for (int i = 0; i + 1 < packCells.Length; i += 2)
            {
                int k = i / 2;
                if (Found(k)) { packObjs.Add(null); continue; }
                var pos = NorthUtil.Cell(map, packCells[i], packCells[i + 1]);
                var s = NorthSpot.Make(root, pos, () => "", () => Pack(k), Interactable.Kind.Person);
                s.verb = "짐 줍기";
                var art = new GameObject("Lost Pack");
                art.transform.SetParent(s.transform, false);
                art.transform.localPosition = new Vector3(0f, -0.5f, 0f);
                var sr = art.AddComponent<SpriteRenderer>(); sr.sprite = packSp; sr.sortingOrder = 9;
                packObjs.Add(s.gameObject);
            }
        }

        string Where(int k)
        {
            var map = NorthUtil.Map;
            Vector3 p = NorthUtil.Cell(map, packCells[k * 2], packCells[k * 2 + 1]);
            Vector3 d = p - gunnarPos;
            string ns = d.y > 4f ? "북" : d.y < -4f ? "남" : "";
            string ew = d.x > 4f ? "동" : d.x < -4f ? "서" : "";
            string dir = ns + ew;
            dir = dir.Length > 0 ? dir + "쪽" : "이 근처";
            string near = k < packHint.Length ? (packHint[k] == 0 ? "물가" : packHint[k] == 1 ? "나무 사이" : "바위 뒤") : "어딘가";
            float far = d.magnitude;
            string dist = far > 30f ? "꽤 멀리" : far > 18f ? "조금 떨어진" : "가까운";
            return $"{dir} {dist} {near}";
        }

        void Gunnar()
        {
            if (!GStarted)
            {
                DialogueUI.Show("길 잃은 행상 군나르",
                    "어젯밤 눈보라에 썰매가 뒤집혀서 짐을 몽땅 날려 먹었소… 이 골짜기 어딘가에 흩어졌을 텐데, 다리가 얼어서 찾으러 갈 수가 없구려.\n세 꾸러미만 찾아 주면 사례는 톡톡히 하리다.",
                    new DialogueUI.Choice("찾아 드릴게요", () => { NorthSave.Set("gunnar:start", 1f); DialogueUI.Show("길 잃은 행상 군나르", "고맙소! 바람이 이렇게 불었으니…\n" + Hints()); }),
                    new DialogueUI.Choice("다음에", null));
                return;
            }
            int n = FoundCount, total = packCells.Length / 2;
            if (n >= total) { Reward(); return; }
            DialogueUI.Show("길 잃은 행상 군나르", $"찾은 짐: {n}/{total}\n" + Hints());
        }

        string Hints()
        {
            var lines = new List<string>();
            for (int k = 0; k < packCells.Length / 2; k++)
                if (!Found(k)) lines.Add($"· {Where(k)}에 하나 굴러간 것 같소.");
            return string.Join("\n", lines) + "\n(눈에 반쯤 묻힌 상자나 자루를 찾아보자)";
        }

        void Pack(int k)
        {
            if (!GStarted) { Notify.Show("눈에 반쯤 묻힌 짐 꾸러미다. 누군가 잃어버린 것 같다.", 2.5f); return; }
            if (Found(k)) return;
            NorthSave.Set("gunnar:pack:" + k, 1f);
            if (k < packObjs.Count && packObjs[k] != null) Destroy(packObjs[k]);
            Play(pick, 0.6f);
            int n = FoundCount, total = packCells.Length / 2;
            Notify.Show(n >= total ? "짐을 다 찾았다! 군나르에게 돌아가자." : $"군나르의 짐을 찾았다 ({n}/{total})", 3f);
        }

        void Reward()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            var gift = gm.ItemById(GiftId);
            if (gift != null && !gm.Player.CanCarry(gift)) { DialogueUI.Show("길 잃은 행상 군나르", "짐을 다 찾아 왔구려! 그런데 자네 짐이 가득하군. 답례품 넣을 자리를 비우고 오시오."); return; }
            if (gift != null) gm.Player.cargo.Add(gift, 1, 0f);
            gm.Player.credit += 1;
            NorthSave.Set("gunnar:done", 1f);
            Play(done);
            DialogueUI.Show("길 잃은 행상 군나르", "세상에, 하나도 빠짐없이! 이건 남쪽 운하 도시에서 떼어 온 레이스 손수건이오. 귀한 거니 좋은 값에 파시오.\n이제 썰매를 다시 꾸려 길을 떠나야겠소. 언젠가 또 길에서 만납시다!"
                + $"\n({(gift != null ? gift.displayName + " 1개 · " : "")}신용 +1)");
            Rebuild(NorthUtil.Map);
        }

        // ---------------- 장작 패기 ----------------

        static bool Carrying => NorthSave.Get("wood:carry", 0f) > 0.5f;

        void BuildWood(string map)
        {
            var w = NorthData.Get(map, "woodcutter");
            if (w.Length >= 2) Person(NorthUtil.Cell(map, w[0], w[1]), "woodcutter", "u", () => "나무꾼 엘크", Woodcutter);
            var f = NorthData.Get(map, "woodFire");
            if (f.Length >= 2)
            {
                firePos = NorthUtil.Cell(map, f[0], f[1]) + new Vector3(-0.5f, 0.5f, 0f);
                hasFire = true;
                var s = NorthSpot.Make(root, firePos, () => "", FeedFire, Interactable.Kind.Person);
                s.verb = "장작 넣기";
                bigFlame = Art(firePos + new Vector3(0f, -0.35f, 0f), flames[0], 11);
            }
        }

        void Woodcutter()
        {
            if (Carrying) { DialogueUI.Show("나무꾼 엘크", "그 장작은 저기 모닥불에 넣어 주게. 불이 시들시들하거든."); return; }
            if ((int)NorthSave.Get("wood:day", -1f) == Today)
            {
                DialogueUI.Show("나무꾼 엘크", (int)NorthSave.Get("wood:fire", -1f) == Today ? "오늘은 불이 활활 타는구먼. 덕분에 손이 녹네." : "오늘 몫은 넉넉하다네. 고맙네!");
                return;
            }
            DialogueUI.Show("나무꾼 엘크", "이놈의 통나무가 얼어서 도끼가 튕겨 나가는구먼… 좀 거들어 주겠나?\n도끼가 초록 칸에 왔을 때 Space! 여섯 번 중에 세 번만 쪼개면 되네.",
                new DialogueUI.Choice("거들게요", StartChop), new DialogueUI.Choice("다음에", null));
        }

        void StartChop()
        {
            chopState = C.Play; hits = 0; tries = 0; cursor = 0f; cursorDir = 1f; chopResult = null;
            NewZone();
            startFrame = Time.frameCount;
            UIState.Blocking = true;
        }

        void NewZone() { zoneW = Mathf.Max(14f, 26f - hits * 4f); zoneX = Random.Range(10f, BarW - zoneW - 10f); }

        void UpdateChop(float dt)
        {
            if (chopState == C.Play)
            {
                cursor += cursorDir * (120f + hits * 30f) * dt;
                if (cursor < 0f) { cursor = 0f; cursorDir = 1f; }
                if (cursor > BarW) { cursor = BarW; cursorDir = -1f; }
                if (Time.frameCount <= startFrame + 1) return;
                if (InputReader.CancelPressed()) { EndChop(false); return; }
                if (InputReader.InteractPressed())
                {
                    tries++;
                    if (cursor >= zoneX && cursor <= zoneX + zoneW) { hits++; Play(chop, 0.7f); NewZone(); }
                    else Play(miss, 0.5f);
                    if (hits >= 3) EndChop(true);
                    else if (tries >= 6) EndChop(false);
                }
            }
            else if (chopState == C.Result)
            {
                chopT += dt;
                if (chopT > 0.3f && InputReader.InteractPressed())
                {
                    chopState = C.None; UIState.Release();
                }
            }
        }

        void EndChop(bool ok)
        {
            chopState = C.Result; chopT = 0f;
            if (ok)
            {
                NorthSave.Set("wood:day", Today); NorthSave.Set("wood:carry", 1f);
                chopResult = $"쩍! 쩍! 쩍! 통나무를 다 쪼갰다 ({hits}/{tries})\n장작 한 아름을 받았다.\n<color=#ffd75a>모닥불에 넣어 보자 (▼ 표시)</color>";
                Play(done);
            }
            else chopResult = "도끼가 자꾸 튕겨 나간다… 다시 해 보자.";
        }

        void FeedFire()
        {
            if (!Carrying)
            {
                bool hot = (int)NorthSave.Get("wood:fire", -1f) == Today;
                Notify.Show(hot ? "불이 활활 타오른다. 손발이 녹는다." : "불이 사그라들고 있다… 나무꾼 엘크를 도와 장작을 얻어 오자.", 2.5f);
                return;
            }
            NorthSave.Set("wood:fire", Today);
            NorthSave.Set("wood:carry", 0f);
            var gm = GameManager.I;
            string extra = "";
            if (!NorthSave.Has("wood:first") && gm != null) { NorthSave.Set("wood:first", 1f); gm.Player.credit += 1; extra = "\n(나무꾼 엘크: 고맙네! 자네 같은 사람이 있어 마을이 따뜻하지. · 신용 +1)"; }
            Play(done);
            DialogueUI.Show("모닥불 쉼터", "장작을 넣자 불길이 활활 솟아오른다! 둘러앉은 사람들이 손을 쬔다." + extra);
        }

        // ---------------- 화면 ----------------

        void OnGUI()
        {
            var cam = Camera.main;
            if (chopState != C.None) { DrawChop(); return; }
            if (root == null || cam == null || UIState.Blocking) return;
            UIStyle.Begin();
            if (hasFire && Carrying)
            {
                float bob = Mathf.Sin(Time.time * 5f) * 0.08f;
                var s = cam.WorldToScreenPoint(firePos + new Vector3(0f, 1.4f + bob, 0f));
                var p = new Vector2(s.x / UIStyle.Scale, (Screen.height - s.y) / UIStyle.Scale);
                UIStyle.Outlined(new Rect(p.x - 40, p.y - 22, 80, 12), "<color=#ffd75a>장작 넣기</color>", UIStyle.CenterRich);
                UIStyle.Outlined(new Rect(p.x - 10, p.y - 11, 20, 14), "<color=#ffd75a><b>▼</b></color>", UIStyle.CenterRich);
            }
        }

        void DrawChop()
        {
            UIStyle.Begin();
            GUI.depth = -20;
            float vw = UIStyle.VirtualWidth, vh = UIStyle.VirtualHeight;
            var box = new Rect(vw / 2 - 110, vh / 2 - 50, 220, 100);
            Fill(box, new Color(0.08f, 0.07f, 0.14f, 0.92f));
            GUI.Label(new Rect(box.x + 10, box.y + 6, 200, 16), $"<b>장작 패기</b>  쪼갠 통나무 {hits}/3 · 남은 도끼질 {6 - tries}", UIStyle.Rich);
            var bar = new Rect(box.x + 30, box.y + 40, BarW, 14);
            Fill(bar, new Color(0.35f, 0.25f, 0.18f));
            Fill(new Rect(bar.x + zoneX, bar.y, zoneW, bar.height), new Color(0.45f, 0.85f, 0.45f));
            Fill(new Rect(bar.x + cursor - 2, bar.y - 5, 4, bar.height + 10), new Color(0.95f, 0.95f, 1f));
            if (chopState == C.Result)
            {
                GUI.Label(new Rect(box.x + 10, box.y + 58, 200, 40), chopResult, UIStyle.Wrap);
                GUI.Label(new Rect(box.xMax - 70, box.yMax - 14, 70, 12), "Space: 닫기", UIStyle.Small);
            }
            else GUI.Label(new Rect(box.x + 10, box.yMax - 18, 200, 14), "초록 칸에서 Space·클릭 · Esc 그만두기", UIStyle.Small);
            // 11/08 마우스: X = Esc(그만), 빈 곳 클릭 = Space
            if (CuteUI.CloseButton(box.xMax - 16f, box.y - 8f)) InputReader.Press(chopState == C.Result ? ' ' : '\x1b');
            InputReader.ClickAsSpace();
        }

        static Texture2D white;
        static void Fill(Rect r, Color c)
        {
            if (white == null) { white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply(); }
            var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, white); GUI.color = old;
        }
    }
}
