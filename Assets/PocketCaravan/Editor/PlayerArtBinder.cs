using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 1) 네 출신 데이터에 주인공 걷기 그림 16장을 연결한다 (PlayerAppearance가 맵마다 입힘).
    ///    그림 파일: Art/Characters/{이름}_tall_{d|u|l|r}{0~3}.png
    /// 2) 시작 화면 문명 목록 Resources/StartChoices.asset을 만든다 (카드 그림 Resources/StartScreen/start_*.png).
    /// </summary>
    public static class PlayerArtBinder
    {
        const string Root = "Assets/PocketCaravan";
        static readonly (string origin, string art)[] Table =
        {
            ("Data/Origins/Origin_Viking.asset", "north_reindeer"),
            ("Data/Origins/Origin_Venice.asset", "venice_cat"),
            ("Data/Origins/Origin_Arabia.asset", "arabia_player_fennec"),
            ("Data/Goryeo/Origin_Goryeo.asset", "tanuki"),
        };

        [MenuItem("Pocket Caravan/통합/주인공 그림 + 시작 화면 연결", priority = 61)]
        public static void BindAll()
        {
            int ok = 0;
            foreach (var (originPath, art) in Table)
            {
                var origin = AssetDatabase.LoadAssetAtPath<OriginData>($"{Root}/{originPath}");
                if (origin == null) { Debug.LogWarning($"[주인공 그림] 출신 없음: {originPath}"); continue; }
                var frames = new Sprite[16];
                const string dirs = "dulr";
                bool missing = false;
                for (int d = 0; d < 4; d++)
                    for (int f = 0; f < 4; f++)
                    {
                        var path = $"{Root}/Art/Characters/{art}_tall_{dirs[d]}{f}.png";
                        frames[d * 4 + f] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                        if (frames[d * 4 + f] == null) { Debug.LogWarning($"[주인공 그림] 그림 없음: {path}"); missing = true; }
                    }
                if (missing) continue;
                origin.playerFrames = frames;
                EditorUtility.SetDirty(origin);
                ok++;
                Debug.Log($"[주인공 그림] {origin.displayName} ← {art}");
            }
            Debug.Log($"[주인공 그림] {ok}/{Table.Length} 출신 연결 완료");
            BuildStartChoices();
            AssetDatabase.SaveAssets();
        }

        // 카드 순서 = 화면 2×2 순서 (왼위·오른위·왼아래·오른아래)
        static readonly (string origin, string region, string animal, string strength, string scene, string town, string preview)[] Starts =
        {
            ("Data/Origins/Origin_Viking.asset", "북유럽", "순록", "이동이 빠름 (길 없는 곳은 더 빠름)", "north_birka", "비르카", "StartScreen/start_north"),
            ("Data/Origins/Origin_Venice.asset", "베네치아", "흰 고양이", "다른 지역 물건을 팔아도 값이 덜 떨어짐", "venice_rialto", "리알토", "StartScreen/start_venice"),
            ("Data/Goryeo/Origin_Goryeo.asset", "고려", "너구리", "장부가 두 배 오래 감", "Naru", "예성 나루", "StartScreen/start_goryeo"),
            ("Data/Origins/Origin_Arabia.asset", "아라비아", "사막여우", "멀리 가져가 팔면 +5%", "arabia_ahsa", "알 아흐사", "StartScreen/start_arabia"),
        };

        static void BuildStartChoices()
        {
            const string path = Root + "/Resources/StartChoices.asset";
            var asset = AssetDatabase.LoadAssetAtPath<StartChoices>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<StartChoices>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.choices.Clear();
            foreach (var s in Starts)
            {
                var origin = AssetDatabase.LoadAssetAtPath<OriginData>($"{Root}/{s.origin}");
                if (origin == null) Debug.LogWarning($"[시작 화면] 출신 없음: {s.origin}");
                if (!System.Array.Exists(EditorBuildSettings.scenes, e => e.enabled && System.IO.Path.GetFileNameWithoutExtension(e.path) == s.scene))
                    Debug.LogWarning($"[시작 화면] 빌드 목록에 출발 씬 없음: {s.scene} (통합 > 전체 준비 먼저)");
                var tex = $"{Root}/Resources/{s.preview}.png";
                if (AssetImporter.GetAtPath(tex) is TextureImporter ti)
                {
                    if (ti.textureType != TextureImporterType.Default || ti.mipmapEnabled || ti.npotScale != TextureImporterNPOTScale.None
                        || ti.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        ti.textureType = TextureImporterType.Default; ti.mipmapEnabled = false;
                        ti.npotScale = TextureImporterNPOTScale.None; ti.textureCompression = TextureImporterCompression.Uncompressed;
                        ti.SaveAndReimport();
                    }
                }
                else Debug.LogWarning($"[시작 화면] 카드 그림 없음: {tex}");
                asset.choices.Add(new StartChoices.Choice
                {
                    region = s.region, origin = origin, animal = s.animal, strength = s.strength,
                    startScene = s.scene, startTown = s.town, startSpawn = SpawnPoint.StartId, preview = s.preview,
                });
            }
            EditorUtility.SetDirty(asset);
            Debug.Log($"[시작 화면] 문명 {asset.choices.Count}개 등록: {path}");
        }
    }
}
