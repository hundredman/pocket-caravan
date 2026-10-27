using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 베네치아 런타임 도우미 (2026-10-26).
    /// - VeniceSave: 베네치아 작은 진행(오늘 한 미니게임, 찾은 문장 등)을 공용 GameManager 진행 메모에 "venice:" 키로 남긴다(저장 형식 그대로).
    /// - VeniceData: Resources/Venice/VeniceLife.json(Tools/MapGen/venice_life.py가 맵과 함께 만든다)에서 맵별 자리 목록을 읽는다.
    /// - VeniceUtil: 맵 JSON 칸(위가 0) → 월드 좌표, 그림 불러오기.
    /// </summary>
    public static class VeniceSave
    {
        public static float Get(string key, float fallback = 0f) => GameManager.I != null ? GameManager.I.GetMemo("venice:" + key, fallback) : fallback;
        public static bool Has(string key) => GameManager.I != null && GameManager.I.HasMemo("venice:" + key);
        public static void Set(string key, float value) { if (GameManager.I != null) GameManager.I.SetMemo("venice:" + key, value); }

        /// <summary>오늘 이미 했는지 (게임 날짜 기준)</summary>
        public static bool DoneToday(string key) => GameManager.I != null && (int)Get(key + "_day", -1) == GameManager.I.Clock.day;
        public static void MarkToday(string key) { if (GameManager.I != null) Set(key + "_day", GameManager.I.Clock.day); }
    }

    public static class VeniceData
    {
        [System.Serializable] public class MapLife
        {
            public string id;
            public int h;
            public int[] pigeons, lamps, smoke, gondolas, glass, net, workshop_glass, workshop_lace, market, lion, inspector, museum, feed;
            public int[] seats, wander, dock, flood, spawns, lost;     // 2026-10-27 벤치 앞·떠돌이 상인·곤돌라 나루·물 차오름·도착 자리
            public string[] spawn_ids;
            public bool gulls;
        }
        [System.Serializable] class File { public MapLife[] maps; }

        static Dictionary<string, MapLife> maps;

        public static MapLife Get(string mapId)
        {
            if (maps == null)
            {
                maps = new Dictionary<string, MapLife>();
                var text = Resources.Load<TextAsset>("Venice/VeniceLife");
                var file = text != null ? JsonUtility.FromJson<File>(text.text) : null;
                if (file?.maps != null) foreach (var m in file.maps) maps[m.id] = m;
            }
            return mapId != null && maps.TryGetValue(mapId, out var v) ? v : null;
        }

        public static int Height(string mapId) => Get(mapId)?.h ?? 0;

        /// <summary>[x,y,x,y,...] → 점 목록</summary>
        public static List<Vector2Int> Points(int[] flat)
        {
            var list = new List<Vector2Int>();
            for (int i = 0; flat != null && i + 1 < flat.Length; i += 2) list.Add(new Vector2Int(flat[i], flat[i + 1]));
            return list;
        }
    }

    public static class VeniceUtil
    {
        public static string Map => MapRoot.Current != null ? MapRoot.Current.mapId : null;
        public static bool InVenice => Map != null && Map.StartsWith("venice_");
        public static bool InMainland => Map != null && Map.StartsWith("venice_c_");

        /// <summary>맵 JSON 칸(위가 0) → 월드 좌표(칸 가운데)</summary>
        public static Vector3 Cell(int x, int y)
        {
            int h = VeniceData.Height(Map);
            return MapRoot.Current.CellCenter(new Vector3Int(x, h - 1 - y, 0));
        }

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        public static Sprite Sprite(string name)
        {
            if (cache.TryGetValue(name, out var s) && s != null) return s;
            return cache[name] = Resources.Load<Sprite>("Venice/Life/" + name);
        }

        static Material unlit;
        /// <summary>밤 어둠(전역 2D 빛)에 영향받지 않는 재질 (등불 빛)</summary>
        public static Material Unlit
        {
            get
            {
                if (unlit != null) return unlit;
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                return unlit = shader != null ? new Material(shader) : null;
            }
        }

        public static ItemData Item(string id) => GameManager.I != null ? GameManager.I.Db.items.Find(i => i != null && i.id == id) : null;

        /// <summary>물건을 짐에 넣는다. 실을 수 없으면 기준가만큼 돈으로 준다. 받은 내용을 글로 돌려준다.</summary>
        public static string Give(string itemId, int count)
        {
            var gm = GameManager.I;
            var item = Item(itemId);
            if (gm == null || item == null || count <= 0) return "";
            int carried = 0;
            for (int i = 0; i < count; i++)
                if (gm.Player.CanCarry(item)) { gm.Player.cargo.Add(item); carried++; }
            int money = Mathf.RoundToInt((count - carried) * item.basePrice);
            gm.Player.money += money;
            string text = carried > 0 ? $"{item.displayName} {carried}개를 짐에 실었다." : "";
            if (money > 0) text += (text.Length > 0 ? " " : "") + $"짐이 가득해 {money}냥으로 받았다.";
            return text;
        }
    }
}
