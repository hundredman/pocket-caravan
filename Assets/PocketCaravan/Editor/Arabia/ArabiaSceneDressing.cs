using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 공용 빌더(MapJsonBuilder)가 만든 아라비아 씬에 아라비아 전용 설정을 덧입힌다. 다른 지역 씬은 열지 않는다.
    /// - 주인공은 공용 주인공(너구리)을 그대로 쓴다 (사용자 요청 2026-10-22).
    /// - 아라비아 캐릭터 그림(32×40px)은 PPU 32로 가져와 주인공(16×24px, PPU 16)과 같은 크기로 보이게 한다.
    /// - 장터 상인: 공용 빌더가 고정한 고려 상인 그림 → 아라비아 상인.
    /// - 마을 사람·이야기꾼에 ArabiaWanderer(근처 거닐기 + 4방향 걷기)를 붙인다.
    /// NPC·주민·이야기꾼 그림 이름은 World/Regions/arabia.json과 맵 원본에서 지정한다.
    /// </summary>
    public static class ArabiaSceneDressing
    {
        public const string MerchantSprite = "arabia_npc_merchant_tall";
        const string CharacterDir = ArabiaRegionSetup.Root + "/Art/Characters";

        /// <summary>arabia_*_tall_*.png: PPU 32, 발이 칸 중심 약간 아래(그림자 위치)에 오도록 기준점 지정.</summary>
        public static void EnsureCharacterImport()
        {
            foreach (var guid in AssetDatabase.FindAssets("arabia_ t:Texture2D", new[] { CharacterDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!System.IO.Path.GetFileName(path).StartsWith("arabia_")) continue;
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                var s = new TextureImporterSettings();
                ti.ReadTextureSettings(s);
                var pivot = new Vector2(0.5f, 20f / 48f);   // 캔버스 아래 8px 여백 + 발 아래 0.375칸
                if (Mathf.Approximately(ti.spritePixelsPerUnit, 32f) && s.spriteAlignment == (int)SpriteAlignment.Custom && s.spritePivot == pivot) continue;
                s.spritePixelsPerUnit = 32;   // SetTextureSettings가 PPU도 덮어쓰므로 설정 묶음 쪽에 넣는다
                s.spriteAlignment = (int)SpriteAlignment.Custom;
                s.spritePivot = pivot;
                ti.SetTextureSettings(s);
                ti.SaveAndReimport();
            }
        }

        public static void Apply(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (var market in Object.FindObjectsByType<TownMarket>(FindObjectsSortMode.None))
            {
                var anim = market.GetComponent<CharacterAnimator>();
                var frames = LoadFrames(MerchantSprite);
                if (anim == null || frames[0] == null) { Debug.LogWarning($"[아라비아] 상인 그림 없음: {MerchantSprite}"); continue; }
                anim.frames = frames;
                anim.GetComponent<SpriteRenderer>().sprite = frames[0];
                EditorUtility.SetDirty(anim);
            }
            // 마을 사람·이야기꾼(TalkNpc + 캐릭터 그림)은 제자리 근처를 거닌다. 의뢰 NPC·상인은 제자리.
            foreach (var talk in Object.FindObjectsByType<TalkNpc>(FindObjectsSortMode.None))
            {
                if (talk.GetComponent<CharacterAnimator>() == null || talk.GetComponent<ArabiaWanderer>() != null) continue;
                // 2026-11-03 통합: 여관 주인(공용 Inn)은 건물 문 앞에 서 있어야 찾기 쉽다 → 거닐지 않는다
                if (talk.displayName != null && (talk.displayName.Contains("여관") || talk.displayName.Contains("숙소") || talk.displayName.Contains("객주"))) continue;
                talk.gameObject.AddComponent<ArabiaWanderer>();
                EditorUtility.SetDirty(talk.gameObject);
            }
            // 분위기 연출 (발자국·모래바람·동물): 씬마다 하나 (ArabiaAmbience, 표시 전용)
            if (Object.FindAnyObjectByType<ArabiaAmbience>() == null)
            {
                new GameObject("arabia_ambience").AddComponent<ArabiaAmbience>();
            }
            // 맵 원본의 아라비아 전용 물체 (2026-10-27): 순찰 경비병 · 밤 불빛 자리 · 관람차/분수 연출 자리
            var mapRoot = Object.FindAnyObjectByType<MapRoot>();
            if (mapRoot != null) DressFromJson(mapRoot);
            var cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null) { cam.backgroundColor = new Color32(0xD9, 0xB9, 0x82, 0xFF); EditorUtility.SetDirty(cam); }   // 맵 밖 모래색
            EditorSceneManager.SaveScene(scene);
        }

        [System.Serializable] class JObj { public string kind, name, character, id; public int x, y, variant; public float fx, fy, radius; public int[] path; public string[] lines; }
        [System.Serializable] class JMap { public string id; public int w, h; public JObj[] objects; }

        static void DressFromJson(MapRoot root)
        {
            var old = GameObject.Find("arabia_dressing");
            if (old != null) Object.DestroyImmediate(old);
            string path = $"{ArabiaRegionSetup.Root}/Maps/{root.mapId}.json";
            if (!System.IO.File.Exists(path)) return;
            var f = JsonUtility.FromJson<JMap>(System.IO.File.ReadAllText(path));
            var grid = root.GetComponent<Grid>() ?? Object.FindAnyObjectByType<Grid>();
            if (f == null || f.objects == null || grid == null) return;
            var parent = new GameObject("arabia_dressing").transform;
            Vector3 origin = grid.CellToWorld(Vector3Int.zero);
            float cs = grid.cellSize.x;
            Vector3 W(float X, float Y) => new Vector3(origin.x + X * cs, origin.y + (f.h - Y) * cs, 0f);      // 칸 단위 실수 좌표(왼쪽 위 기준) → 월드
            Vector3 C(int x, int y) => grid.GetCellCenterWorld(new Vector3Int(x, f.h - 1 - y, 0));
            var shadow = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArabiaRegionSetup.Root}/Art/Characters/shadow.png");
            int guards = 0, lights = 0;
            foreach (var o in f.objects)
            {
                if (o.kind == "arabia_guard")
                {
                    var frames = LoadFrames(o.character);
                    if (frames[0] == null) { Debug.LogWarning($"[아라비아] 경비병 그림 없음: {o.character}"); continue; }
                    var go = new GameObject(o.name);
                    go.transform.SetParent(parent);
                    go.transform.position = C(o.x, o.y);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = 10; sr.sprite = frames[0];
                    go.AddComponent<CharacterAnimator>().frames = frames;
                    var sh = new GameObject("Shadow");
                    sh.transform.SetParent(go.transform, false);
                    sh.transform.localPosition = new Vector3(0f, -0.36f, 0f);
                    var ssr = sh.AddComponent<SpriteRenderer>(); ssr.sprite = shadow; ssr.sortingOrder = 9;
                    var tk = go.AddComponent<TalkNpc>();
                    tk.displayName = o.name; tk.lines = o.lines;
                    var pt = go.AddComponent<ArabiaPatrol>();
                    if (o.path != null)
                        for (int k = 0; k + 1 < o.path.Length; k += 2) pt.path.Add(C(o.path[k], o.path[k + 1]));
                    guards++;
                }
                else if (o.kind == "arabia_light" || o.kind == "arabia_fx")
                {
                    var go = new GameObject(o.kind == "arabia_light" ? "night_light_spot" : "fx_" + o.id);
                    go.transform.SetParent(parent);
                    go.transform.position = W(o.fx, o.fy);
                    var mk = go.AddComponent<ArabiaFxMarker>();
                    mk.kind = o.kind == "arabia_light" ? "light" : o.id;
                    mk.radius = o.radius; mk.colorName = o.id; mk.flicker = o.variant == 1;
                    if (o.kind == "arabia_light") lights++;
                }
            }
            Debug.Log($"[아라비아] {root.mapId}: 경비병 {guards} · 밤 불빛 {lights}");
        }

        static Sprite[] LoadFrames(string character)
        {
            var frames = new Sprite[16];
            const string dirs = "dulr";
            for (int d = 0; d < 4; d++)
                for (int f = 0; f < 4; f++)
                    frames[d * 4 + f] = AssetDatabase.LoadAssetAtPath<Sprite>($"{CharacterDir}/{character}_{dirs[d]}{f}.png");
            return frames;
        }
    }
}
