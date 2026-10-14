using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 임시 픽셀 아트를 코드로 그려 PNG로 저장한다. 실제 아트가 오면 같은 파일 이름으로 덮어쓰면
    /// 타일과 씬이 그대로 새 그림을 쓴다.
    /// </summary>
    public static class PlaceholderArt
    {
        // 파스텔 톤 팔레트 (아기자기한 느낌)
        static readonly Color32 Outline = Hex("3B2E2A");
        static readonly Color32 Grass = Hex("8CCB6E"), GrassLight = Hex("A6DA84"), GrassDark = Hex("74B65C");
        static readonly Color32 Dirt = Hex("D9B47F"), DirtDark = Hex("BE9562"), DirtLight = Hex("E8C996");
        static readonly Color32 Stone = Hex("CFC8B8"), StoneDark = Hex("ABA394");
        static readonly Color32 Leaf = Hex("4E9C5A"), LeafLight = Hex("6CBD67"), LeafDark = Hex("3C7F4A"), Trunk = Hex("8A5A3B");
        static readonly Color32 Water = Hex("6DB8DC"), WaterLight = Hex("A2D8EE"), WaterDark = Hex("5AA2C8");
        static readonly Color32 Roof = Hex("5D6E91"), RoofLight = Hex("7A8BAE"), RoofDark = Hex("47557A");
        static readonly Color32 Wall = Hex("F1E3C6"), Wood = Hex("9A6A45"), Paper = Hex("FBF6EA");
        static readonly Color32 Skin = Hex("F4CDA6"), Hat = Hex("2E2C36"), Cloth = Hex("F3EEE2"), ClothDark = Hex("D8D0BE");
        static readonly Color32 Pants = Hex("6E5C4C"), Jige = Hex("A0703F"), Red = Hex("D7705E"), Purple = Hex("8A6CB8");
        static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        public static void WriteAll(string dir)
        {
            Save(dir, "grass", 16, 16, Grass16);
            Save(dir, "tree", 16, 16, Tree16);
            Save(dir, "water", 16, 16, Water16);
            Save(dir, "roof", 16, 16, Roof16);
            Save(dir, "wall", 16, 16, Wall16);
            Save(dir, "road1", 16, 16, (x, y) => Road(x, y, 1));
            Save(dir, "road2", 16, 16, (x, y) => Road(x, y, 2));
            Save(dir, "road3", 16, 16, (x, y) => Road(x, y, 3));
            SaveCharacter(dir, "player_tall", Cloth, ClothDark, true);
            SaveCharacter(dir, "merchant_tall", Red, Hex("B85A4A"), false);
        }

        // ---------- 타일 ----------

        static Color32 Grass16(int x, int y)
        {
            int h = Hash(x, y);
            if (h % 17 == 0) return GrassLight;
            if (h % 13 == 0) return GrassDark;
            // 작은 풀잎 무늬
            if ((x == 3 && y == 4) || (x == 4 && y == 5) || (x == 11 && y == 10) || (x == 12 && y == 11)) return GrassDark;
            return Grass;
        }

        static Color32 Tree16(int x, int y)
        {
            float dx = x - 7.5f, dy = y - 8.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (y <= 3 && x >= 6 && x <= 9) return y == 0 ? Outline : Trunk;
            if (d < 6.2f)
            {
                if (d > 5.3f) return Outline;
                if (dx < -1 && dy > 1) return LeafLight;
                if (dx > 2 && dy < -1) return LeafDark;
                return Hash(x, y) % 11 == 0 ? LeafLight : Leaf;
            }
            return Grass16(x, y);
        }

        static Color32 Water16(int x, int y)
        {
            if ((y == 4 && x >= 2 && x <= 5) || (y == 11 && x >= 9 && x <= 12)) return WaterLight;
            if ((y == 3 && x >= 3 && x <= 4) || (y == 10 && x >= 10 && x <= 11)) return WaterDark;
            return Water;
        }

        static Color32 Roof16(int x, int y)
        {
            // 기와 줄무늬
            if (y == 0) return Outline;
            if (y % 4 == 0) return RoofDark;
            if (y % 4 == 3) return RoofLight;
            return x % 4 == 0 ? RoofDark : Roof;
        }

        static Color32 Wall16(int x, int y)
        {
            if (y == 0) return Outline;
            if (y == 15) return RoofDark; // 처마 그림자
            if (x == 0 || x == 15 || y == 1) return Wood;
            // 창호지 문
            if (x >= 5 && x <= 10 && y >= 2 && y <= 11)
            {
                if (x == 5 || x == 10 || y == 11 || x == 7 || y == 6) return Wood;
                return Paper;
            }
            return Wall;
        }

        static Color32 Road(int x, int y, int grade)
        {
            int h = Hash(x + grade * 31, y);
            switch (grade)
            {
                case 1: // 오솔길: 풀 사이로 흙이 드문드문
                {
                    bool dirt = (h % 3 != 0) && x > 2 && x < 13 && y > 2 && y < 13 || (h % 7 == 0);
                    if (!dirt) return Grass16(x, y);
                    return h % 9 == 0 ? DirtDark : Dirt;
                }
                case 2: // 길: 다져진 흙
                    if (h % 15 == 0) return DirtDark;
                    if (h % 19 == 0) return DirtLight;
                    return Dirt;
                default: // 큰길: 돌 포장
                    if (x % 8 == 0 || (y % 8 == 0)) return StoneDark;
                    return h % 13 == 0 ? StoneDark : Stone;
            }
        }

        // ---------- 캐릭터 16×32 ----------

        static void SaveCharacter(string dir, string name, Color32 cloth, Color32 clothDark, bool withJige)
        {
            var px = new Color32[16 * 32];
            for (int i = 0; i < px.Length; i++) px[i] = Clear;
            void P(int x, int y, Color32 c) { if (x >= 0 && x < 16 && y >= 0 && y < 32) px[y * 16 + x] = c; }
            void R(int x0, int y0, int x1, int y1, Color32 c) { for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) P(x, y, c); }

            if (withJige) // 지게 (등짐 틀)
            {
                R(3, 4, 3, 19, Jige); R(12, 4, 12, 19, Jige);
                R(3, 12, 12, 12, Jige);
                R(4, 13, 11, 17, Hex("C9A46B")); // 짐 보따리
            }
            R(6, 1, 7, 3, Pants); R(8, 1, 9, 3, Pants);          // 다리
            R(5, 4, 10, 13, cloth);                              // 몸
            R(5, 4, 10, 4, clothDark); R(5, 9, 10, 9, clothDark); // 옷 주름, 허리띠
            R(4, 7, 4, 11, cloth); R(11, 7, 11, 11, cloth);       // 팔
            P(4, 7, Skin); P(11, 7, Skin);                        // 손
            R(5, 14, 10, 19, Skin);                               // 얼굴 (머리가 큰 2.5등신)
            P(6, 16, Outline); P(9, 16, Outline);                 // 눈
            P(5, 15, Hex("F2A9A0")); P(10, 15, Hex("F2A9A0"));    // 볼
            if (withJige)
            {
                R(3, 20, 12, 20, Hat);                            // 갓 챙
                R(6, 21, 9, 24, Hat);                             // 갓 모자
            }
            else
            {
                R(5, 20, 10, 21, Purple);                         // 두건
                R(6, 22, 9, 22, Purple);
            }

            // 바깥 테두리
            var copy = (Color32[])px.Clone();
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 16; x++)
            {
                if (copy[y * 16 + x].a != 0) continue;
                bool edge = false;
                for (int k = 0; k < 4 && !edge; k++)
                {
                    int nx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                    if (nx >= 0 && nx < 16 && ny >= 0 && ny < 32 && copy[ny * 16 + nx].a != 0) edge = true;
                }
                if (edge) px[y * 16 + x] = Outline;
            }
            WritePng(dir, name, 16, 32, px);
        }

        // ---------- 공통 ----------

        static void Save(string dir, string name, int w, int h, Func<int, int, Color32> f)
        {
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = f(x, y);
            WritePng(dir, name, w, h, px);
        }

        static void WritePng(string dir, string name, int w, int h, Color32[] px)
        {
            string assetPath = $"{dir}/{name}.png";
            string full = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
            if (File.Exists(full)) return; // 이미 있으면 (실제 아트로 바꿨을 수 있으니) 건드리지 않음
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllBytes(full, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        static int Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                return Math.Abs(h ^ (h >> 16));
            }
        }

        static Color32 Hex(string s)
        {
            ColorUtility.TryParseHtmlString("#" + s, out var c);
            return c;
        }
    }
}
