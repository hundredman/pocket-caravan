using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PocketCaravan;
using PocketCaravan.EditorTools;

namespace CaravanMovementLab.EditorTools
{
    public static class MovementLabBuilder
    {
        const string Root = "Assets/CaravanMovementLab";
        public const string ScenePath = Root + "/Scenes/MovementLab.unity";

        [MenuItem("Pocket Caravan/Tests/이동·운반 시험장 열기")]
        public static void Open()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
            else Build();
        }

        [MenuItem("Pocket Caravan/Tests/이동·운반 시험장 만들기")]
        public static void Build()
        {
            if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            { Debug.LogWarning("Play를 종료하고 현재 씬을 저장한 뒤 시험장을 만드세요."); return; }
            string jsonPath = Root + "/Maps/MovementLab.json";
            if (!File.Exists(jsonPath)) { Debug.LogError("Tools/MapGen/movement_lab.py를 먼저 실행하세요."); return; }
            Directory.CreateDirectory(Root + "/Generated");
            Directory.CreateDirectory(Root + "/Scenes");
            AssetDatabase.Refresh();
            MapJsonBuilder.BuildSingleMap(File.ReadAllText(jsonPath), ScenePath);
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/PocketCaravan/Resources/GameDatabase.asset");
            var lab = new GameObject("Movement test session").AddComponent<MovementLab>();
            lab.player = Object.FindAnyObjectByType<PlayerController>();
            lab.walk = db.vehicles.Find(v => v.id == "walk");
            lab.cart = db.vehicles.Find(v => v.id == "handcart");
            lab.cargoItem = db.items.Find(i => i.id == "ginseng"); // 무게 1인 기존 물건. 낙하/줍기도 본편 코드로 처리
            lab.cartSprite = Cart();
            lab.crateSprite = Crate();
            Flag("출발", new Vector3(5, 14, 0), new Color32(102, 178, 113, 255));
            Flag("도착", new Vector3(83, 14, 0), new Color32(228, 122, 80, 255));
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("이동·운반 시험장 생성 완료. Play 즉시 테스트 가능. 본편 빌드 씬/지도 색인은 변경하지 않음.");
        }

        static Sprite Cart()
        {
            var t = new Texture2D(24, 28, TextureFormat.RGBA32, false);
            t.SetPixels(new Color[24 * 28]);
            void Box(int x, int y, int w, int h, string hex)
            {
                ColorUtility.TryParseHtmlString(hex, out var color);
                for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++) t.SetPixel(xx, yy, color);
            }
            Box(5, 4, 14, 17, "#523e33"); Box(6, 5, 12, 15, "#ad8051");
            for (int x = 7; x < 18; x += 3) Box(x, 6, 1, 13, "#d2a66b");
            Box(1, 7, 4, 10, "#363237"); Box(19, 7, 4, 10, "#363237");
            Box(2, 9, 2, 6, "#6d625b"); Box(20, 9, 2, 6, "#6d625b");
            Box(6, 19, 2, 9, "#775237"); Box(16, 19, 2, 9, "#775237");
            Box(5, 4, 14, 2, "#e1b97e"); Box(5, 18, 14, 2, "#e1b97e");
            return Save(t, "handcart");
        }

        static Sprite Crate()
        {
            var t = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var colors = new Color[256];
            ColorUtility.TryParseHtmlString("#98724c", out var wood);
            ColorUtility.TryParseHtmlString("#e0b37a", out var rim);
            for (int y = 1; y < 15; y++) for (int x = 1; x < 15; x++)
                colors[y * 16 + x] = x < 3 || x > 12 || y < 3 || y > 12 || x == y ? rim : wood;
            t.SetPixels(colors);
            return Save(t, "crate");
        }

        static void Flag(string name, Vector3 position, Color color)
        {
            var t = new Texture2D(16, 24, TextureFormat.RGBA32, false);
            t.SetPixels(new Color[16 * 24]);
            for (int y = 1; y < 23; y++) { t.SetPixel(3, y, new Color32(94, 72, 51, 255)); t.SetPixel(4, y, new Color32(214, 178, 117, 255)); }
            for (int y = 14; y < 22; y++) for (int x = 5; x < 14; x++) t.SetPixel(x, y, color);
            var go = new GameObject(name + " 깃발").AddComponent<SpriteRenderer>();
            go.transform.position = position;
            go.sortingOrder = 8;
            go.sprite = Save(t, name == "출발" ? "start_flag" : "finish_flag");
        }

        static Sprite Save(Texture2D texture, string name)
        {
            texture.Apply();
            string path = Root + "/Generated/" + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spritePixelsPerUnit = 16;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
