using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 10/30 룬돌 언덕 빈 곳 채우기 (기우 기획). 공용 코드는 고치지 않는다. 자리는 NorthLife.json (n4_build.py place_runehill).
    /// - 순례자 야영지 "pilgrims": 은빛 순록 전설을 따라온 순례자 셋. 아직 못 읽은 숨은 룬돌 하나를 귀띔한다 (NorthLegends.Hint).
    ///   일곱 개를 다 읽었으면 "밤에 고리 한가운데", 축복을 받았으면 축하.
    /// - 음유시인 공연 자리 "bard", "bardListeners": 모닥불 가에서 지금까지 찾은 전설 조각을 차례로 읊는다. 둘러앉은 마을 사람 셋.
    /// - 돌탑 언덕 "cairn"(쌓는 자리), "cairnTower"(내 돌탑), "cairnDeco"(남들이 쌓은 돌탑):
    ///   좌우로 흔들리는 돌을 Space로 떨어뜨려 쌓는 타이밍 놀이(따로 뜨는 화면). 어긋난 만큼 깎이고, 못 얹으면 끝. 층이 오를수록 빨라진다.
    ///   최고 층수는 언덕의 내 돌탑 높이로 남는다. 5층↑ 하루 한 번 순례자의 '소원'(오늘의 장터 소식), 처음 10층 칭호 '돌탑 장인'.
    /// - 저장: NorthSave "cairn:best", "cairn:wish"(날), "cairn:master", "bard:next"
    /// </summary>
    public class NorthRunehill : MonoBehaviour
    {
        const string MapId = "north_runehill";
        static NorthRunehill instance;

        Transform root, tower;
        string lastMap;
        readonly List<(SpriteRenderer sr, Sprite[] f)> idlers = new List<(SpriteRenderer, Sprite[])>();
        Sprite[] stoneSprites;
        Vector3 towerBase;
        AudioSource sfx;
        AudioClip tap, perfect, crash, chime;

        // 돌탑 놀이
        enum S { None, Play, Fall, Result }
        S state = S.None;
        class Stone { public float x, w; public int look; }
        readonly List<Stone> stack = new List<Stone>();
        float curX, curW, dir = 1f, speed, view, fallY, fallX, fallW, resultT;
        int startFrame, perfects;
        string resultText;
        const float FieldW = 180f, FieldH = 250f, StoneH = 11f, BaseW = 76f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthRunehill>() != null) return;
            var obj = new GameObject("NorthRunehill");
            DontDestroyOnLoad(obj);
            obj.AddComponent<NorthRunehill>();
        }

        void Awake()
        {
            instance = this;
            sfx = gameObject.AddComponent<AudioSource>(); sfx.playOnAwake = false;
            tap = NorthUtil.Tone("north_cairn_tap", 0.07f, new[] { 520f }, 0.3f);
            perfect = NorthUtil.Tone("north_cairn_perfect", 0.18f, new[] { 1046f, 1568f }, 0.3f);
            crash = NorthUtil.Tone("north_cairn_crash", 0.35f, new[] { 260f, 180f, 130f }, 0.3f);
            chime = NorthUtil.Tone("north_cairn_chime", 0.6f, new[] { 659f, 988f, 1319f }, 0.3f);
            stoneSprites = new[] { NorthUtil.Load("cairnstone0", new Vector2(0.5f, 0f)), NorthUtil.Load("cairnstone1", new Vector2(0.5f, 0f)), NorthUtil.Load("cairnstone2", new Vector2(0.5f, 0f)) };
        }

        void Play(AudioClip c, float v = 0.6f) { if (c != null && !AudioManager.Muted) sfx.PlayOneShot(c, v); }
        static int Today => GameManager.I != null && GameManager.I.Clock != null ? GameManager.I.Clock.day : 0;

        // ---------------- 자리 ----------------

        void Update()
        {
            string map = NorthUtil.Map;
            if (map != lastMap) { lastMap = map; Rebuild(map); }
            foreach (var it in idlers)
                if (it.sr != null && it.f != null && it.f.Length >= 2 && it.f[0] != null && it.f[1] != null) it.sr.sprite = it.f[(int)(Time.time * 1.5f) % 2];
            if (state != S.None) UpdateGame(Time.unscaledDeltaTime);
        }

        Sprite[] Frames(string prefix, string dir)
        {
            var a = new Sprite[2];
            for (int i = 0; i < 2; i++) a[i] = NorthUtil.Load($"{prefix}_{dir}{i}", new Vector2(0.5f, 0.1f));
            return a;
        }

        NorthSpot Person(Vector3 pos, string prefix, string dir, System.Func<string> label, System.Action act)
        {
            var s = NorthSpot.Make(root, pos, label, act, Interactable.Kind.Person);
            var f = Frames(prefix, dir);
            if (f[0] != null)
            {
                var sr = s.gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = f[0]; sr.sortingOrder = 10;
                idlers.Add((sr, f));
            }
            return s;
        }

        SpriteRenderer Deco(Vector3 pos, Sprite sp, int order = 9)
        {
            if (sp == null) return null;
            var go = new GameObject("Cairn");
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp; sr.sortingOrder = order;
            return sr;
        }

        void Rebuild(string map)
        {
            if (root != null) Destroy(root.gameObject);
            root = null; tower = null; idlers.Clear();
            if (map != MapId || MapRoot.Current == null) return;
            root = new GameObject("North Runehill").transform;
            var pil = NorthData.Get(map, "pilgrims");
            for (int i = 0; i + 1 < pil.Length && i / 2 < 3; i += 2)
            {
                int k = i / 2;
                Person(NorthUtil.Cell(map, pil[i], pil[i + 1]), "pilgrim" + k, k == 0 ? "u" : k == 1 ? "r" : "l",
                    () => k == 0 ? "룬돌 순례자들" : "", () => Pilgrim(k)).verb = "말 걸기";
            }
            var bard = NorthData.Get(map, "bard");
            if (bard.Length >= 2) Person(NorthUtil.Cell(map, bard[0], bard[1]), "bard", "l", () => "음유시인", Bard);
            var lis = NorthData.Get(map, "bardListeners");
            for (int i = 0; i + 1 < lis.Length && i / 2 < 3; i += 2)
            {
                int k = i / 2;
                Person(NorthUtil.Cell(map, lis[i], lis[i + 1]), "listener" + k, k == 2 ? "u" : "r", () => "", () => Listener(k));
            }
            var cairn = NorthData.Get(map, "cairn");
            if (cairn.Length >= 2)
            {
                var s = NorthSpot.Make(root, NorthUtil.Cell(map, cairn[0], cairn[1]), () => "돌탑 쌓기", AskCairn, Interactable.Kind.Board);
                s.verb = "돌 쌓기";
            }
            var deco = NorthData.Get(map, "cairnDeco");
            var decoSp = NorthUtil.Load("cairn_deco", new Vector2(0.5f, 0.1f));
            for (int i = 0; i + 1 < deco.Length; i += 2) Deco(NorthUtil.Cell(map, deco[i], deco[i + 1]), decoSp);
            var tw = NorthData.Get(map, "cairnTower");
            if (tw.Length >= 2) { towerBase = NorthUtil.Cell(map, tw[0], tw[1]) + new Vector3(0f, -0.4f, 0f); BuildTower(); }
        }

        /// <summary>언덕 위 내 돌탑: 최고 기록 층수만큼 (보이는 건 20층까지)</summary>
        void BuildTower()
        {
            if (root == null) return;
            if (tower != null) Destroy(tower.gameObject);
            int best = (int)NorthSave.Get("cairn:best", 0f);
            if (best <= 0) return;
            tower = new GameObject("My Cairn").transform;
            tower.SetParent(root, false);
            tower.position = towerBase;
            int n = Mathf.Min(best, 20);
            for (int i = 0; i < n; i++)
            {
                var sp = stoneSprites[i % stoneSprites.Length];
                if (sp == null) continue;
                var go = new GameObject("Stone");
                go.transform.SetParent(tower, false);
                float jitter = ((i * 37) % 5 - 2) * 0.03f;
                go.transform.localPosition = new Vector3(jitter, i * 0.2f, 0f);
                float sc = Mathf.Lerp(1.1f, 0.6f, n > 1 ? i / (float)(n - 1) : 0f);
                go.transform.localScale = new Vector3(sc, 1f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp; sr.sortingOrder = 9;
            }
        }

        // ---------------- 순례자·음유시인 ----------------

        static readonly string[] PilgrimName = { "순례자 할아버지 오드", "순례자 시리", "순례자 상인 베른" };

        void Pilgrim(int k)
        {
            int found = NorthLegends.Found;
            string text;
            if (NorthLegends.Blessed)
                text = k == 0 ? "자네가 고리를 깨웠다는 이야기를 들었네. 은빛 순록이 정말 내려왔다니… 이 늙은이도 평생의 소원을 이뤘구먼."
                     : k == 1 ? "오로라가 내려오던 밤, 저도 봤어요! 순례를 마치면 고향에 이 이야기를 꼭 전할 거예요."
                     : "전설이 진짜였다니. 장사꾼으로 오래 살았지만 이런 횡재는 처음이오.";
            else if (found >= NorthLegends.Total)
                text = "일곱 조각을 다 읽었다고? 그럼 밤이 되길 기다렸다가 고리 한가운데 서 보게. 우리도 그 밤을 보러 이 언덕까지 온 걸세.";
            else
            {
                int next = -1;
                // 순례자마다 다른 조각을 귀띔 (아직 못 읽은 것 중 k번째, 모자라면 첫 번째)
                var missing = new List<int>();
                for (int i = 1; i <= NorthLegends.Total; i++) if (!NorthSave.Has("rune:" + i)) missing.Add(i);
                next = missing[Mathf.Min(k, missing.Count - 1)];
                string where = NorthLegends.Hint(next);
                text = k == 0 ? $"은빛 순록의 전설을 따라 여기까지 왔다네. 젊었을 적에 이끼 낀 룬돌 하나를 본 적이 있지…\n「{where}」\n그 근처였을 걸세. 밤엔 은은하게 빛나니 찾기 쉬울 게야."
                     : k == 1 ? $"저는 지도에 룬돌 자리를 적어 다녀요. 아직 아무도 못 읽었다는 조각 하나는…\n「{where}」\n거기라고 들었어요!"
                     : $"장사하러 다니다 보면 별소리를 다 듣소. 이끼 낀 돌이 있다는 곳 하나 알려 드리리다.\n「{where}」\n공짜 소문이니 너무 믿진 마시오.";
                text += $"\n\n(읽은 전설 조각 {found}/{NorthLegends.Total})";
            }
            DialogueUI.Show(PilgrimName[Mathf.Clamp(k, 0, 2)], text);
        }

        void Bard()
        {
            int found = NorthLegends.Found;
            var known = new List<int>();
            for (int i = 1; i <= NorthLegends.Total; i++) if (NorthSave.Has("rune:" + i)) known.Add(i);
            if (known.Count == 0)
            {
                DialogueUI.Show("음유시인", "(류트를 고르며) 은빛 순록의 노래를 들려주고 싶지만… 가사가 다 흩어져 버렸다오.\n이 땅 곳곳 이끼 낀 룬돌에 한 대목씩 새겨져 있다지. 하나라도 읽어 오면 그 대목을 불러 드리리다.");
                return;
            }
            int n = (int)NorthSave.Get("bard:next", 0f) % known.Count;
            NorthSave.Set("bard:next", n + 1);
            int idx = known[n];
            Play(chime, 0.35f);
            string tail = found < NorthLegends.Total ? $"\n\n…다음 대목은 아직 아무도 모른다오. ({found}/{NorthLegends.Total})" : "\n\n…이것으로 노래는 끝이라오. 일곱 대목이 다 모였구려!";
            DialogueUI.Show("음유시인", $"(류트를 뜯으며) 은빛 순록의 노래, {idx}번째 대목…\n\n「{NorthLegends.Lore[idx]}」" + tail);
        }

        static readonly string[][] ListenerLines =
        {
            new[] { "쉿, 지금 좋은 대목이야!", "이 노래 들으려고 고기잡이도 쉬고 왔다고." },
            new[] { "어릴 적 할머니가 불러 주던 노래란다.", "모닥불 가에서 듣는 사가가 제일이지." },
            new[] { "귀가 쫑긋해지는 노래네요!", "다음 대목은 언제 나오려나…" },
        };
        void Listener(int k)
        {
            var lines = ListenerLines[Mathf.Clamp(k, 0, 2)];
            DialogueUI.Show(k == 0 ? "어부" : k == 1 ? "할머니" : "토끼 아이", lines[Random.Range(0, lines.Length)]);
        }

        // ---------------- 돌탑 쌓기 ----------------

        static void AskCairn()
        {
            var m = instance;
            if (m == null || m.state != S.None) return;
            int best = (int)NorthSave.Get("cairn:best", 0f);
            string rec = best > 0 ? $"\n(내 최고 기록: {best}층 — 언덕 위 내 돌탑)" : "";
            DialogueUI.Show("돌탑 언덕",
                "순례자들이 소원을 빌며 쌓은 돌탑들이 서 있다.\n흔들리는 돌을 Space로 떨어뜨려 쌓는다. 아래 돌에서 비어져 나온 만큼은 떨어져 나가고, 하나도 못 얹으면 무너진다.\n5층을 넘기면 소원을 빌 수 있다고 한다." + rec,
                new DialogueUI.Choice("돌을 쌓는다", () => m.StartGame()),
                new DialogueUI.Choice("구경만 한다", null));
        }

        void StartGame()
        {
            stack.Clear();
            stack.Add(new Stone { x = (FieldW - BaseW) / 2f, w = BaseW, look = 0 });
            curW = BaseW; curX = 0f; dir = 1f; speed = 70f; view = 0f; perfects = 0; resultText = null;
            state = S.Play;
            startFrame = Time.frameCount;
            UIState.Blocking = true;
        }

        int Floors => stack.Count - 1;

        void UpdateGame(float dt)
        {
            if (state == S.Play)
            {
                curX += dir * speed * dt;
                if (curX < 0f) { curX = 0f; dir = 1f; }
                if (curX + curW > FieldW) { curX = FieldW - curW; dir = -1f; }
                float wantView = Mathf.Max(0f, stack.Count * StoneH - FieldH * 0.55f);
                view = Mathf.Lerp(view, wantView, dt * 6f);
                if (Time.frameCount > startFrame + 1 && InputReader.CancelPressed()) { End(); return; }
                if (Time.frameCount > startFrame + 1 && InputReader.InteractPressed()) Drop();
            }
            else if (state == S.Fall)
            {
                fallY += 260f * dt;
                if (fallY > FieldH + 40f) End();
            }
            else if (state == S.Result)
            {
                resultT += dt;
                if (resultT > 0.3f && InputReader.InteractPressed()) Close();
            }
        }

        void Drop()
        {
            var top = stack[stack.Count - 1];
            float l = Mathf.Max(curX, top.x), r = Mathf.Min(curX + curW, top.x + top.w);
            float over = r - l;
            if (over < 2.5f)
            {
                state = S.Fall; fallY = 0f; fallX = curX; fallW = curW;
                Play(crash);
                return;
            }
            if (Mathf.Abs(curX - top.x) < 2f) { l = top.x; over = Mathf.Min(curW, top.w); perfects++; Play(perfect, 0.5f); }   // 딱 맞으면 깎이지 않음
            else Play(tap);
            stack.Add(new Stone { x = l, w = over, look = stack.Count % 3 });
            curW = over;
            curX = Random.value < 0.5f ? 0f : FieldW - curW; dir = curX <= 0f ? 1f : -1f;
            speed = Mathf.Min(230f, 70f + Floors * 11f);
        }

        void End()
        {
            state = S.Result; resultT = 0f;
            int floors = Floors;
            int best = (int)NorthSave.Get("cairn:best", 0f);
            var lines = new List<string> { $"<b>{floors}층</b>을 쌓았다" + (perfects > 0 ? $" (딱 맞춤 {perfects}번)" : "") };
            if (floors > best) { NorthSave.Set("cairn:best", floors); lines.Add(best > 0 ? "<color=#9fe0a0>최고 기록! 언덕 위 내 돌탑이 높아졌다</color>" : "<color=#9fe0a0>언덕에 내 돌탑이 섰다</color>"); }
            if (floors >= 10 && !NorthSave.Has("cairn:master"))
            {
                NorthSave.Set("cairn:master", 1f);
                lines.Add("<color=#ffd75a>칭호 '돌탑 장인'</color>");
            }
            if (floors >= 5)
            {
                if ((int)NorthSave.Get("cairn:wish", -1f) != Today)
                {
                    NorthSave.Set("cairn:wish", Today);
                    lines.Add("순례자들이 박수를 치며 함께 소원을 빌어 주었다.");
                    resultText = "순례자 할아버지 오드: 소원이 이루어지길! 답례로 오늘 장터 소식을 알려 주지.\n\n" + NorthDaily.DealNews();
                }
                else lines.Add("순례자들이 박수를 쳐 주었다. (소원은 하루에 한 번)");
            }
            else lines.Add(floors >= 3 ? "조금만 더! 5층을 넘기면 소원을 빌 수 있다." : "돌이 와르르 무너졌다…");
            lastResult = string.Join("\n", lines);
            Play(floors >= 5 ? chime : tap, 0.5f);
        }

        string lastResult;

        void Close()
        {
            state = S.None;
            UIState.Release();
            BuildTower();
            if (!string.IsNullOrEmpty(resultText)) DialogueUI.Show("돌탑 언덕", resultText);
        }

        // ---------------- 화면 ----------------

        void OnGUI()
        {
            if (state == S.None) return;
            UIStyle.Begin();
            GUI.depth = -20;
            float vw = UIStyle.VirtualWidth, vh = UIStyle.VirtualHeight;
            Fill(new Rect(0, 0, vw, vh), new Color(0f, 0f, 0.05f, 0.45f));
            var field = new Rect(vw / 2 - FieldW / 2, vh / 2 - FieldH / 2 + 8, FieldW, FieldH);
            Fill(new Rect(field.x - 4, field.y - 4, field.width + 8, field.height + 8), new Color(0.2f, 0.18f, 0.28f));
            // 하늘 → 눈 언덕
            Fill(field, new Color(0.62f, 0.72f, 0.86f));
            Fill(new Rect(field.x, field.y, field.width, field.height * 0.35f), new Color(0.48f, 0.58f, 0.78f));
            float groundY = field.yMax - 18f + view;
            if (groundY < field.yMax) Fill(new Rect(field.x, groundY, field.width, field.yMax - groundY), new Color(0.94f, 0.95f, 1f));
            // 쌓인 돌
            for (int i = 0; i < stack.Count; i++)
            {
                float y = groundY - (i + 1) * StoneH;
                if (y > field.yMax || y + StoneH < field.y) continue;
                DrawStone(new Rect(field.x + stack[i].x, y, stack[i].w, StoneH), stack[i].look, field);
            }
            if (state == S.Play)
            {
                float y = groundY - (stack.Count + 1) * StoneH - 6f;
                DrawStone(new Rect(field.x + curX, Mathf.Max(field.y + 2f, y), curW, StoneH), stack.Count % 3, field);
            }
            else if (state == S.Fall)
            {
                float y = groundY - (stack.Count + 1) * StoneH - 6f + fallY;
                DrawStone(new Rect(field.x + fallX, y, fallW, StoneH), stack.Count % 3, field);
            }
            GUI.Label(new Rect(field.x, field.y - 30, field.width, 16), $"<b>돌탑 쌓기</b>  {Floors}층", UIStyle.Rich);
            GUI.Label(new Rect(field.xMax + 12, field.y, 140, 70), "Space 떨어뜨리기\n딱 맞추면 안 깎임\nEsc 그만두기", UIStyle.Small);
            if (state == S.Result)
            {
                var box = new Rect(field.x + 4, field.center.y - 50, field.width - 8, 100);
                Fill(box, new Color(0.08f, 0.07f, 0.14f, 0.92f));
                GUI.Label(new Rect(box.x + 8, box.y + 6, box.width - 16, 76), lastResult, UIStyle.Wrap);
                GUI.Label(new Rect(box.x + 8, box.yMax - 18, box.width - 16, 14), "Space: 닫기", UIStyle.Small);
            }
            // 11/08 마우스: X = Esc(그만), 빈 곳 클릭 = Space(떨어뜨리기·닫기)
            if (CuteUI.CloseButton(field.xMax - 16f, field.y - 8f)) InputReader.Press(state == S.Result ? ' ' : '\x1b');
            InputReader.ClickAsSpace();
        }

        static void DrawStone(Rect r, int look, Rect clip)
        {
            if (r.yMax < clip.y || r.y > clip.yMax) return;
            Color body = look == 0 ? new Color(0.52f, 0.53f, 0.59f) : look == 1 ? new Color(0.45f, 0.49f, 0.52f) : new Color(0.57f, 0.54f, 0.50f);
            Fill(new Rect(r.x, r.y, r.width, r.height), new Color(0.23f, 0.24f, 0.31f));
            Fill(new Rect(r.x + 1, r.y + 1, Mathf.Max(0f, r.width - 2), r.height - 3), body);
            Fill(new Rect(r.x + 2, r.y + 1, Mathf.Max(0f, r.width - 4), 1), Color.Lerp(body, Color.white, 0.35f));
            Fill(new Rect(r.x + 2, r.y + 1, Mathf.Max(0f, r.width - 4), 2), new Color(0.97f, 0.98f, 1f, 0.6f));   // 눈 살짝
        }

        static Texture2D white;
        static void Fill(Rect r, Color c)
        {
            if (white == null) { white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply(); }
            var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, white); GUI.color = old;
        }
    }
}
