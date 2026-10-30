using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 북유럽 자동 점검 (2026-10-27). 메뉴 한 번으로 북유럽·중앙 북쪽이 축제에서 돌아갈 준비가 됐는지 본다.
    /// (베네치아 VeniceFeatureCheck, 아라비아 ArabiaChecks를 참고해 북유럽판으로 만듦)
    /// 1) 데이터: 마을 9곳·물건·퀘스트가 GameDatabase에 있는지, 공방 재료/가공품이 있고 가공품이 재료값보다 비싼지
    /// 2) 그림·소리: 공방 마당 그림, 시간대 음악, NorthLife.json
    /// 3) 맵: 지역 등록(north.json, center_north.json)의 맵·씬 파일, 출구가 있는 맵을 가리키는지
    /// 4) 걷기: 각 맵 첫 스폰에서 출구·장터·말 거는 사람·공방·나룻배 자리까지 걸어갈 수 있는지
    /// 읽기만 하고 아무것도 고치지 않는다. 결과는 콘솔과 _local/north_check.txt
    /// </summary>
    public static class NorthFeatureCheck
    {
        const string Root = "Assets/PocketCaravan";

        [Serializable] class RegionFile { public string regionId; public RegionMap[] maps; }
        [Serializable] class RegionMap { public string mapId, jsonPath, scenePath; }
        [Serializable] class MapFile { public string id; public int w, h; public int[] blocked; public MapObj[] objects; }
        [Serializable] class MapObj { public string kind, target, town, name, id; public int x, y, w, h; }
        [Serializable] class LifeFile { public LifeMap[] maps; }
        [Serializable] class LifeMap { public string mapId; public int[] craft, yard, ferry, market; }

        static readonly List<string> lines = new List<string>();
        static int pass, fail, warn;

        static void Ok(string s) { pass++; lines.Add("  O " + s); }
        static void Bad(string s) { fail++; lines.Add("  X " + s); }
        static void Warn(string s) { warn++; lines.Add("  ! " + s); }
        static void Check(bool ok, string s) { if (ok) Ok(s); else Bad(s); }
        static void Head(string s) { lines.Add(""); lines.Add("[" + s + "]"); }

        [MenuItem("Pocket Caravan/북유럽/자동 점검", priority = 40)]
        public static void Run()
        {
            lines.Clear(); pass = fail = warn = 0;
            lines.Add("북유럽 자동 점검 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            try
            {
                CheckData();
                CheckResources();
                CheckMaps();
            }
            catch (Exception e) { Bad("점검 중 오류: " + e.Message); }
            lines.Insert(1, $"통과 {pass} · 실패 {fail} · 주의 {warn}");
            string text = string.Join("\n", lines);
            try
            {
                string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "_local");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "north_check.txt"), text, Encoding.UTF8);
            }
            catch (Exception) { }
            if (fail > 0) Debug.LogWarning(text); else Debug.Log(text);
            EditorUtility.DisplayDialog("북유럽 자동 점검",
                $"통과 {pass} · 실패 {fail} · 주의 {warn}\n\n" + (fail > 0 ? "실패 항목은 콘솔이나 _local/north_check.txt를 보세요." : "모두 통과!"), "확인");
        }

        // ---------- 1. 데이터 ----------
        static readonly string[] Towns = { "north_birka", "north_hunter", "north_naru", "north_laugar", "north_herd", "north_post", "north_cwood", "north_ccreek", "north_cross" };
        static readonly string[] FairTowns = { "north_post", "north_cwood", "north_ccreek", "north_cross" };

        static void CheckData()
        {
            Head("데이터 (GameDatabase)");
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(Root + "/Resources/GameDatabase.asset");
            if (db == null) { Bad("GameDatabase가 없음"); return; }
            var items = new Dictionary<string, ItemData>();
            foreach (var i in db.items) if (i != null && !string.IsNullOrEmpty(i.id)) items[i.id] = i;
            var towns = new Dictionary<string, TownData>();
            foreach (var t in db.towns) if (t != null && !string.IsNullOrEmpty(t.id)) towns[t.id] = t;

            int missingTowns = 0;
            foreach (var id in Towns)
            {
                if (!towns.TryGetValue(id, out var t)) { Bad("마을 없음: " + id + " (테스트 준비를 먼저 실행)"); missingTowns++; continue; }
                if (t.stock == null || t.stock.Count == 0) Bad(t.displayName + " 장터에 파는 물건이 없음");
            }
            if (missingTowns == 0) Ok("마을 9곳 등록 (북유럽 5 + 중앙 북쪽 4), 장터 물건 있음");
            foreach (var id in FairTowns) if (!towns.ContainsKey(id)) Bad("큰 장 마을 없음: " + id);

            int northItems = 0, noIcon = 0;
            foreach (var kv in items)
            {
                if (!kv.Key.StartsWith("north_")) continue;
                northItems++;
                if (kv.Value.icon == null) noIcon++;
            }
            Check(northItems >= 17, $"북유럽 물건 {northItems}개 등록");
            if (noIcon > 0) Warn($"그림 없는 북유럽 물건 {noIcon}개 (Art/Items/북유럽 id.png)");

            int northQuests = 0;
            foreach (var q in db.quests) if (q != null && q.id != null && q.id.StartsWith("north_")) northQuests++;
            Check(northQuests >= 8, $"북유럽 퀘스트 {northQuests}개 등록");

            // 공방 조리법: NorthCraft의 Recipes를 그대로 읽는다 (같은 표를 두 군데 적지 않게)
            var field = typeof(NorthCraft).GetField("Recipes", BindingFlags.NonPublic | BindingFlags.Static);
            var recipes = field != null ? field.GetValue(null) as Array : null;
            if (recipes == null) { Bad("NorthCraft 공방 조리법을 못 읽음"); return; }
            foreach (var r in recipes)
            {
                var rt = r.GetType();
                string shop = rt.GetField("shop").GetValue(r) as string;
                string product = rt.GetField("product").GetValue(r) as string;
                var inputs = rt.GetField("inputs").GetValue(r) as string[];
                var counts = rt.GetField("counts").GetValue(r) as int[];
                int fee = (int)rt.GetField("fee").GetValue(r);
                if (!items.TryGetValue(product, out var prod)) { Bad(shop + ": 가공품 " + product + " 없음"); continue; }
                float cost = fee; bool ok = true;
                for (int k = 0; k < inputs.Length; k++)
                {
                    if (!items.TryGetValue(inputs[k], out var it)) { Bad(shop + ": 재료 " + inputs[k] + " 없음"); ok = false; continue; }
                    cost += it.basePrice * counts[k];
                }
                if (!ok) continue;
                bool sold = false;
                foreach (var id in FairTowns)
                    if (towns.TryGetValue(id, out var t) && t.stock != null)
                        foreach (var s in t.stock) if (s.item == prod) sold = true;
                Check(prod.basePrice > cost, $"{shop}: {prod.displayName} 기준가 {prod.basePrice:0} > 재료 {cost:0}");
                Check(sold, $"{shop}: {prod.displayName}을 중앙 마을 장터에서 받아 줌");
            }
        }

        // ---------- 2. 그림·소리 ----------
        static readonly string[] YardArt =
        {
            "yard_hjell_empty", "yard_hjell_fresh", "yard_hjell_dried", "yard_fur_full", "yard_fur_work",
            "yard_kettle_a0", "yard_kettle_a1", "yard_kettle_a2", "yard_kettle_b0", "yard_kettle_b1", "yard_kettle_b2",
            "yard_vat", "yard_herbs", "yard_skeps", "yard_tap_idle", "yard_tap0", "yard_tap1",
            "fair_stall", "fair_flags",
        };

        static void CheckResources()
        {
            Head("그림·소리");
            var miss = new List<string>();
            foreach (var n in YardArt) if (Resources.Load<Texture2D>("North/Minigame/" + n) == null) miss.Add(n);
            Check(miss.Count == 0, miss.Count == 0 ? $"공방 마당·큰 장 그림 {YardArt.Length}장" : "없는 그림: " + string.Join(", ", miss));
            miss.Clear();
            foreach (var n in new[] { "morning", "day", "evening", "night" })
                if (Resources.Load<AudioClip>("North/Audio/north_bgm_" + n) == null) miss.Add(n);
            Check(miss.Count == 0, miss.Count == 0 ? "시간대 음악 4곡" : "없는 음악: " + string.Join(", ", miss));
            Check(Resources.Load<TextAsset>("North/NorthLife") != null, "NorthLife.json (공방·나룻배·장터 자리)");
        }

        // ---------- 3·4. 맵과 걷기 ----------
        static void CheckMaps()
        {
            Head("맵 등록·출구");
            var all = new HashSet<string>();
            string regions = Path.Combine(Path.GetDirectoryName(Application.dataPath), Root, "World/Regions");
            if (Directory.Exists(regions))
                foreach (var f in Directory.GetFiles(regions, "*.json"))
                {
                    var rf = JsonUtility.FromJson<RegionFile>(File.ReadAllText(f));
                    if (rf != null && rf.maps != null) foreach (var m in rf.maps) if (m != null && m.mapId != null) all.Add(m.mapId);
                }

            var ours = new List<RegionMap>();
            foreach (var name in new[] { "north.json", "center_north.json" })
            {
                string path = Path.Combine(regions, name);
                if (!File.Exists(path)) { Bad("지역 등록 없음: " + name); continue; }
                var rf = JsonUtility.FromJson<RegionFile>(File.ReadAllText(path));
                if (rf?.maps != null) ours.AddRange(rf.maps);
            }
            Check(ours.Count == 18, $"북유럽 9맵 + 중앙 북쪽 9맵 = {ours.Count}맵 등록");

            var built = new HashSet<string>();
            foreach (var s in EditorBuildSettings.scenes) if (s.enabled) built.Add(s.path);

            var life = new Dictionary<string, LifeMap>();
            var lifeText = Resources.Load<TextAsset>("North/NorthLife");
            if (lifeText != null)
            {
                var lf = JsonUtility.FromJson<LifeFile>(lifeText.text);
                if (lf?.maps != null) foreach (var m in lf.maps) if (m?.mapId != null) life[m.mapId] = m;
            }

            int notBuilt = 0;
            var walkLines = new List<string>();
            foreach (var rm in ours)
            {
                string jsonFull = Path.Combine(Path.GetDirectoryName(Application.dataPath), rm.jsonPath);
                if (!File.Exists(jsonFull)) { Bad(rm.mapId + ": 맵 JSON 없음"); continue; }
                if (!File.Exists(Path.Combine(Path.GetDirectoryName(Application.dataPath), rm.scenePath))) Bad(rm.mapId + ": 씬 없음 (테스트 준비 다시)");
                if (!built.Contains(rm.scenePath)) notBuilt++;
                var map = JsonUtility.FromJson<MapFile>(File.ReadAllText(jsonFull));
                if (map == null || map.blocked == null || map.blocked.Length != map.w * map.h) { Bad(rm.mapId + ": 맵 JSON을 못 읽음"); continue; }
                foreach (var o in map.objects ?? new MapObj[0])
                    if (o.kind == "exit" && !all.Contains(o.target)) Bad($"{rm.mapId}: 출구({o.x},{o.y})가 없는 맵 {o.target}을 가리킴");
                life.TryGetValue(rm.mapId, out var lm);
                Walk(rm.mapId, map, lm, walkLines);
            }
            if (notBuilt > 0) Warn($"빌드 목록에 없는 씬 {notBuilt}개 (테스트 준비가 넣음, 통합 때 팀이 반영)");
            else Ok("18맵 모두 빌드 목록에 있음");

            Head("걷기 (첫 스폰에서 갈 수 있는지)");
            lines.AddRange(walkLines);
        }

        /// <summary>첫 스폰에서 사방으로 걸어 닿는 칸을 칠하고, 중요한 자리가 그 안(또는 2칸 안)에 있는지 본다.</summary>
        static void Walk(string mapId, MapFile m, LifeMap lm, List<string> outLines)
        {
            MapObj spawn = null;
            foreach (var o in m.objects ?? new MapObj[0]) if (o.kind == "spawn") { spawn = o; break; }
            if (spawn == null) { fail++; outLines.Add("  X " + mapId + ": 스폰 없음"); return; }
            var seen = new bool[m.w * m.h];
            var q = new Queue<int>();
            int sx = Mathf.Clamp(spawn.x, 0, m.w - 1), sy = Mathf.Clamp(spawn.y, 0, m.h - 1);
            seen[sy * m.w + sx] = true; q.Enqueue(sy * m.w + sx);
            while (q.Count > 0)
            {
                int c = q.Dequeue(), x = c % m.w, y = c / m.w;
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + (d == 0 ? 1 : d == 1 ? -1 : 0), ny = y + (d == 2 ? 1 : d == 3 ? -1 : 0);
                    if (nx < 0 || ny < 0 || nx >= m.w || ny >= m.h) continue;
                    int n = ny * m.w + nx;
                    if (seen[n] || m.blocked[n] != 0) continue;
                    seen[n] = true; q.Enqueue(n);
                }
            }

            var bad = new List<string>();
            int count = 0;
            void Need(string what, int x, int y, int w = 1, int h = 1)
            {
                count++;
                for (int yy = y - 2; yy <= y + h + 1; yy++)
                    for (int xx = x - 2; xx <= x + w + 1; xx++)
                        if (xx >= 0 && yy >= 0 && xx < m.w && yy < m.h && seen[yy * m.w + xx]) return;
                bad.Add($"{what}({x},{y})");
            }
            foreach (var o in m.objects ?? new MapObj[0])
            {
                if (o.kind == "exit") Need("출구→" + o.target, o.x, o.y, Mathf.Max(1, o.w), Mathf.Max(1, o.h));
                else if (o.kind == "market") Need("장터", o.x, o.y);
                else if (o.kind == "talker" || o.kind == "npc") Need(string.IsNullOrEmpty(o.name) ? "사람" : o.name, o.x, o.y);
                else if (o.kind == "board") Need("게시판", o.x, o.y);
            }
            if (lm != null)
            {
                if (lm.craft != null && lm.craft.Length >= 2) Need("공방 장인", lm.craft[0], lm.craft[1]);
                if (lm.ferry != null && lm.ferry.Length >= 2) Need("나룻배", lm.ferry[0], lm.ferry[1]);
            }
            if (bad.Count == 0) { pass++; outLines.Add($"  O {mapId}: {count}곳 모두 닿음"); }
            else { fail++; outLines.Add($"  X {mapId}: 못 가는 곳 " + string.Join(", ", bad)); }
        }
    }
}
