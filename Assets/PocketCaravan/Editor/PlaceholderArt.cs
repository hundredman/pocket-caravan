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

        public const string PlayerSprite = "player_tanuki_tall";
        public const string MerchantSprite = "merchant_rabbit_tall";
        public const string GaekjuSprite = "npc_bear_tall";
        public const string ChiefSprite = "npc_dog_tall";
        public const string MarkerAvailable = "marker_available";
        public const string MarkerTurnIn = "marker_turnin";
        public const string MarkerProgress = "marker_progress";

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
            SaveTanuki(dir, PlayerSprite);
            SaveRabbit(dir, MerchantSprite);
            // 객주: 푸른 옷의 곰
            SaveAnimal(dir, GaekjuSprite, Hex("9C6B4A"), Hex("6B4630"), Hex("E8C9A0"), Ears.Round, Hex("4F6FA6"), false, false);
            // 촌장: 갓 쓰고 흰 수염 난 늙은 개
            SaveAnimal(dir, ChiefSprite, Hex("C9B49A"), Hex("8E7A62"), Hex("F4EDE2"), Ears.Floppy, Hex("8C7A9E"), true, true);
            SaveMarker(dir, MarkerAvailable, '!', Hex("FFD54A"));
            SaveMarker(dir, MarkerTurnIn, '?', Hex("FFD54A"));
            SaveMarker(dir, MarkerProgress, '?', Hex("B8B8B8"));
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

        // ---------- 캐릭터 16×32 (머리가 큰 동물) ----------

        class Canvas16x32
        {
            public readonly Color32[] px = new Color32[16 * 32];
            public Canvas16x32() { for (int i = 0; i < px.Length; i++) px[i] = Clear; }
            public void P(int x, int y, Color32 c) { if (x >= 0 && x < 16 && y >= 0 && y < 32) px[y * 16 + x] = c; }
            public void R(int x0, int y0, int x1, int y1, Color32 c) { for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) P(x, y, c); }

            /// <summary>모서리를 깎은 사각형 (둥근 머리)</summary>
            public void Round(int x0, int y0, int x1, int y1, Color32 c)
            {
                R(x0, y0, x1, y1, c);
                P(x0, y0, Clear); P(x1, y0, Clear); P(x0, y1, Clear); P(x1, y1, Clear);
            }

            public void Outline(Color32 line)
            {
                var copy = (Color32[])px.Clone();
                for (int y = 0; y < 32; y++)
                for (int x = 0; x < 16; x++)
                {
                    if (copy[y * 16 + x].a != 0) continue;
                    for (int k = 0; k < 4; k++)
                    {
                        int nx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                        if (nx >= 0 && nx < 16 && ny >= 0 && ny < 32 && copy[ny * 16 + nx].a != 0) { px[y * 16 + x] = line; break; }
                    }
                }
            }
        }

        /// <summary>고려 행상: 갓 쓰고 지게 진 너구리</summary>
        static void SaveTanuki(string dir, string name)
        {
            var fur = Hex("A58263"); var furDark = Hex("6E5440"); var mask = Hex("4A3A2E"); var cream = Hex("F2E3C8");
            var c = new Canvas16x32();
            // 지게 (등 뒤)
            c.R(2, 3, 2, 18, Jige); c.R(13, 3, 13, 18, Jige); c.R(2, 9, 13, 9, Jige);
            c.R(3, 10, 12, 13, Hex("C9A46B"));                    // 짐 보따리
            // 꼬리
            c.R(11, 2, 13, 4, fur); c.R(13, 2, 14, 3, furDark);
            // 발과 몸
            c.R(5, 0, 6, 1, furDark); c.R(9, 0, 10, 1, furDark);
            c.R(4, 2, 11, 9, Cloth);                               // 흰 도포
            c.R(4, 2, 11, 2, ClothDark); c.R(4, 6, 11, 6, Hex("7A5C8E")); // 옷자락, 허리띠
            c.R(3, 5, 3, 8, Cloth); c.R(12, 5, 12, 8, Cloth);     // 팔
            c.P(3, 5, fur); c.P(12, 5, fur);                       // 손
            // 머리 (크게)
            c.Round(3, 10, 12, 20, fur);
            c.R(4, 14, 11, 16, mask);                              // 너구리 눈가 무늬
            c.P(5, 15, Outline); c.P(10, 15, Outline);             // 눈
            c.P(5, 16, Hex("FFFFFF")); c.P(10, 16, Hex("FFFFFF")); // 눈 반짝
            c.R(6, 11, 9, 13, cream);                              // 주둥이
            c.R(7, 13, 8, 13, Outline);                            // 코
            c.P(4, 12, Hex("F2A9A0")); c.P(11, 12, Hex("F2A9A0")); // 볼
            // 귀 (갓 옆으로 살짝)
            c.R(3, 20, 4, 21, furDark); c.R(11, 20, 12, 21, furDark);
            // 갓
            c.R(1, 22, 14, 22, Hat);
            c.R(5, 23, 10, 26, Hat);
            c.R(5, 23, 10, 23, Hex("4A4858"));
            c.Outline(Outline);
            WritePng(dir, name, 16, 32, c.px);
        }

        /// <summary>장터 상인: 앞치마 두른 토끼</summary>
        static void SaveRabbit(string dir, string name)
        {
            var fur = Hex("F4EEE6"); var furShade = Hex("DCD2C6"); var pink = Hex("F2A9B4");
            var c = new Canvas16x32();
            c.R(5, 0, 6, 1, furShade); c.R(9, 0, 10, 1, furShade); // 발
            c.R(4, 2, 11, 9, Red);                                 // 옷
            c.R(6, 2, 9, 7, Hex("F3E3C0"));                        // 앞치마
            c.R(3, 5, 3, 8, Red); c.R(12, 5, 12, 8, Red);
            c.P(3, 5, fur); c.P(12, 5, fur);
            c.Round(3, 10, 12, 19, fur);                           // 머리
            c.R(3, 10, 12, 10, furShade);
            c.P(5, 14, Outline); c.P(10, 14, Outline);             // 눈
            c.P(5, 15, Hex("FFFFFF")); c.P(10, 15, Hex("FFFFFF"));
            c.R(7, 12, 8, 12, pink);                               // 코
            c.P(4, 12, pink); c.P(11, 12, pink);                   // 볼
            // 긴 귀
            c.R(4, 20, 6, 29, fur); c.R(9, 20, 11, 29, fur);
            c.R(5, 21, 5, 28, pink); c.R(10, 21, 10, 28, pink);
            c.P(4, 29, Clear); c.P(6, 29, Clear); c.P(9, 29, Clear); c.P(11, 29, Clear);
            c.Outline(Outline);
            WritePng(dir, name, 16, 32, c.px);
        }

        enum Ears { Round, Floppy }

        /// <summary>NPC용 동물. 머리 색, 귀 모양, 옷 색, 갓·수염만 바꿔 여러 마을 사람을 만든다.</summary>
        static void SaveAnimal(string dir, string name, Color32 fur, Color32 furDark, Color32 muzzle, Ears ears, Color32 cloth, bool gat, bool beard)
        {
            var c = new Canvas16x32();
            c.R(5, 0, 6, 1, furDark); c.R(9, 0, 10, 1, furDark);
            c.R(4, 2, 11, 9, cloth);
            c.R(4, 6, 11, 6, Hex("E8D8B0"));                       // 허리띠
            c.R(3, 5, 3, 8, cloth); c.R(12, 5, 12, 8, cloth);
            c.P(3, 5, fur); c.P(12, 5, fur);
            c.Round(3, 10, 12, 20, fur);
            c.R(6, 11, 9, 13, muzzle);
            c.R(7, 13, 8, 13, Outline);                            // 코
            c.P(5, 15, Outline); c.P(10, 15, Outline);             // 눈
            c.P(5, 16, Hex("FFFFFF")); c.P(10, 16, Hex("FFFFFF"));
            if (beard) { c.R(6, 9, 9, 10, Hex("FFFFFF")); c.P(7, 8, Hex("FFFFFF")); c.P(8, 8, Hex("FFFFFF")); }
            if (ears == Ears.Round) { c.R(3, 20, 5, 22, furDark); c.R(10, 20, 12, 22, furDark); c.P(4, 21, muzzle); c.P(11, 21, muzzle); }
            else { c.R(2, 15, 3, 20, furDark); c.R(12, 15, 13, 20, furDark); }  // 늘어진 귀
            if (gat) { c.R(1, 22, 14, 22, Hat); c.R(5, 23, 10, 26, Hat); c.R(5, 23, 10, 23, Hex("4A4858")); }
            c.Outline(Outline);
            WritePng(dir, name, 16, 32, c.px);
        }

        /// <summary>퀘스트 표시 (!, ?). 16×16, 아래쪽 기준.</summary>
        static void SaveMarker(string dir, string name, char ch, Color32 color)
        {
            var px = new Color32[16 * 16];
            for (int i = 0; i < px.Length; i++) px[i] = Clear;
            void P(int x, int y) { if (x >= 0 && x < 16 && y >= 0 && y < 16) px[y * 16 + x] = color; }
            if (ch == '!')
            {
                for (int y = 6; y <= 14; y++) { P(7, y); P(8, y); }
                for (int y = 2; y <= 3; y++) { P(7, y); P(8, y); }
            }
            else
            {
                // ? 모양
                for (int x = 5; x <= 10; x++) { P(x, 14); P(x, 13); }
                for (int y = 10; y <= 12; y++) { P(10, y); P(11, y); P(4, y + 1); P(5, y + 1); }
                for (int x = 7; x <= 9; x++) P(x, 9);
                for (int y = 6; y <= 8; y++) { P(7, y); P(8, y); }
                for (int y = 2; y <= 3; y++) { P(7, y); P(8, y); }
            }
            // 테두리
            var copy = (Color32[])px.Clone();
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                if (copy[y * 16 + x].a != 0) continue;
                for (int k = 0; k < 8; k++)
                {
                    int nx = x + new[] { 1, -1, 0, 0, 1, 1, -1, -1 }[k], ny = y + new[] { 0, 0, 1, -1, 1, -1, 1, -1 }[k];
                    if (nx >= 0 && nx < 16 && ny >= 0 && ny < 16 && copy[ny * 16 + nx].a != 0) { px[y * 16 + x] = Outline; break; }
                }
            }
            WritePng(dir, name, 16, 16, px);
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
