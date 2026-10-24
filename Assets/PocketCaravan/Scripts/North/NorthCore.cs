using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 북유럽 런타임 스크립트들이 같이 쓰는 도우미 (10/24).
    /// - NorthData: Resources/North/NorthLife.json에서 맵별 좌표 목록을 꺼낸다 ("secrets": [[x,y,n],...] 같은 숫자 목록을 int[]로).
    /// - NorthSave: 북유럽 진행(찾은 룬돌, 물고기 기록, 순록 먹이 등)을 저장 파일에 남긴다.
    ///   공용 저장 형식을 고치지 않으려고, 이미 저장되는 GameManager의 쿨다운 표(문자열 → 숫자)에 "north:" 키로 넣는다.
    ///   새 게임이면 표가 비어 있으니 진행도 자연히 처음부터.
    /// - NorthUtil: 공용 도우미 (칸 → 월드 좌표, 북유럽 맵 판별, 1픽셀 스프라이트, Space 누르고 있기)
    /// </summary>
    public static class NorthData
    {
        static Dictionary<string, Dictionary<string, int[]>> maps;
        static Dictionary<string, int> heights;

        static void Load()
        {
            if (maps != null) return;
            maps = new Dictionary<string, Dictionary<string, int[]>>();
            heights = new Dictionary<string, int>();
            var json = Resources.Load<TextAsset>("North/NorthLife");
            if (json == null) return;
            string text = json.text;
            int at = 0;
            while (true)
            {
                int i = text.IndexOf("\"mapId\"", at, StringComparison.Ordinal);
                if (i < 0) break;
                int q1 = text.IndexOf('"', text.IndexOf(':', i) + 1), q2 = text.IndexOf('"', q1 + 1);
                string id = text.Substring(q1 + 1, q2 - q1 - 1);
                int next = text.IndexOf("\"mapId\"", q2, StringComparison.Ordinal);
                string block = next < 0 ? text.Substring(i) : text.Substring(i, next - i);
                maps[id] = new Dictionary<string, int[]>();
                maps[id]["__block"] = null;
                blocks[id] = block;
                heights[id] = HeightOf(block);
                at = q2;
            }
        }

        static readonly Dictionary<string, string> blocks = new Dictionary<string, string>();

        static int HeightOf(string block)
        {
            int i = block.IndexOf("\"h\"", StringComparison.Ordinal);
            if (i < 0) return 0;
            i = block.IndexOf(':', i) + 1;
            int j = i;
            while (j < block.Length && (char.IsWhiteSpace(block[j]) || char.IsDigit(block[j]))) j++;
            int.TryParse(block.Substring(i, j - i).Trim(), out int v);
            return v;
        }

        /// <summary>맵의 숫자 목록 (없으면 빈 배열)</summary>
        public static int[] Get(string mapId, string key)
        {
            Load();
            if (mapId == null || !maps.TryGetValue(mapId, out var d)) return new int[0];
            if (d.TryGetValue(key, out var v) && v != null) return v;
            v = Ints(blocks[mapId], key);
            d[key] = v;
            return v;
        }

        public static int Height(string mapId) { Load(); return mapId != null && heights.TryGetValue(mapId, out var h) ? h : 0; }

        static int[] Ints(string block, string key)
        {
            int i = block.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
            if (i < 0) return new int[0];
            int a = block.IndexOf('[', i), depth = 0, b = a;
            if (a < 0) return new int[0];
            for (; b < block.Length; b++)
            {
                if (block[b] == '[') depth++;
                else if (block[b] == ']' && --depth == 0) break;
            }
            var nums = new List<int>();
            foreach (var part in block.Substring(a, b - a).Split(new[] { '[', ']', ',', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                if (int.TryParse(part, out int v)) nums.Add(v);
            return nums.ToArray();
        }
    }

    public static class NorthSave
    {
        static readonly FieldInfo field = typeof(GameManager).GetField("cooldowns", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly Dictionary<string, float> fallback = new Dictionary<string, float>();

        static Dictionary<string, float> Table
        {
            get
            {
                var gm = GameManager.I;
                if (gm != null && field != null && field.GetValue(gm) is Dictionary<string, float> d) return d;
                return fallback;   // 혹시 공용 코드가 바뀌어도 게임은 돌아가게 (이번 실행 동안만 기억)
            }
        }

        public static float Get(string key, float def = 0f) => Table.TryGetValue("north:" + key, out var v) ? v : def;
        public static bool Has(string key) => Table.ContainsKey("north:" + key);
        public static void Set(string key, float value) => Table["north:" + key] = value;
        public static float Add(string key, float delta) { float v = Get(key) + delta; Set(key, v); return v; }
    }

    public static class NorthUtil
    {
        public static string Map => MapRoot.Current != null ? MapRoot.Current.mapId : null;
        public static bool InNorth => Map != null && Map.StartsWith("north_");

        /// <summary>JSON 칸 (위가 0) → 월드 좌표 (칸 가운데)</summary>
        public static Vector3 Cell(string mapId, int x, int y) =>
            MapRoot.Current.CellCenter(new Vector3Int(x, NorthData.Height(mapId) - 1 - y, 0));

        public static Sprite Pixel(Color32 c, int w = 1, int h = 1)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = c;
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        public static Sprite Load(string name, Vector2 pivot)
        {
            var t = Resources.Load<Texture2D>("North/Minigame/" + name);
            if (t == null) return null;
            t.filterMode = FilterMode.Point;
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot, 16f);
        }

        public static bool SpaceHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && k.spaceKey.isPressed;
#else
            return Input.GetKey(KeyCode.Space);
#endif
        }

        public static AudioClip Tone(string name, float seconds, float[] notes, float volume = 0.35f)
        {
            const int rate = 22050; int n = (int)(rate * seconds);
            var data = new float[n]; int seg = Mathf.Max(1, n / notes.Length);
            for (int i = 0; i < n; i++)
            {
                int k = Mathf.Min(notes.Length - 1, i / seg);
                float t = (float)i / rate, local = (float)(i - k * seg) / seg;
                data[i] = Mathf.Sin(2f * Mathf.PI * notes[k] * t) * volume * (1f - local * 0.8f) * Mathf.Clamp01(i / 40f);
            }
            var clip = AudioClip.Create(name, n, 1, rate, false); clip.SetData(data, 0); return clip;
        }

        /// <summary>장터가 있는 북유럽 마을 맵 (길 잃은 상인을 데려다줄 곳)</summary>
        public static readonly HashSet<string> TownMaps = new HashSet<string>
            { "north_birka", "north_hunter", "north_naru", "north_laugar", "north_herd", "north_c_post" };

        public static bool Night => GameManager.I != null && GameManager.I.Clock.Daylight < 0.5f;
    }

    /// <summary>보이지 않는 상호작용 자리 (Space). 이름과 행동만 넘겨 쓴다.</summary>
    public class NorthSpot : Interactable
    {
        public Func<string> label;
        public Action action;
        public Kind kind = Kind.Board;
        public override string DisplayName => label != null ? label() : "";
        public override Kind Category => kind;
        public override void Interact(PlayerController player) => action?.Invoke();

        public static NorthSpot Make(Transform parent, Vector3 pos, Func<string> label, Action action, Kind kind = Kind.Board)
        {
            var go = new GameObject("North Spot");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var s = go.AddComponent<NorthSpot>();
            s.blocksMovement = false; s.label = label; s.action = action; s.kind = kind;
            return s;
        }
    }
}
