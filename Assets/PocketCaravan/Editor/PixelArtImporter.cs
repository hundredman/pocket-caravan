using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// Assets/PocketCaravan/Art 아래 PNG를 처음 가져올 때 픽셀 아트 설정을 자동으로 적용한다.
    /// 16 PPU, Point 필터, 압축 없음. 이름에 "_tall"이 들어가면(16×32 캐릭터) 발밑 칸 가운데가 기준점.
    /// 처음 한 번만 적용하므로, 이후 Inspector에서 바꾼 값은 유지된다.
    /// </summary>
    public class PixelArtImporter : AssetPostprocessor
    {
        public const int PPU = 16;
        const string ArtRoot = "Assets/PocketCaravan/Art";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;
            if (!assetImporter.importSettingsMissing) return;

            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = PPU;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;

            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteExtrude = 0;
            if (System.IO.Path.GetFileNameWithoutExtension(assetPath).Contains("_tall"))
            {
                s.spriteAlignment = (int)SpriteAlignment.Custom;
                s.spritePivot = new Vector2(0.5f, 0.25f);
            }
            ti.SetTextureSettings(s);
        }
    }
}
