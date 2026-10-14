using UnityEditor;

/// <summary>
/// 화투 아틀라스(Resources/Cards)는 칸 단위로 잘라 쓰므로 크기가 바뀌면 안 된다:
/// 2의 거듭제곱으로 늘리지 않고, 최대 크기를 넉넉히, 작게 보일 때를 위해 밉맵을 켠다.
/// </summary>
public class CardTextureImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/Cards/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.mipmapEnabled = true;
        importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
        importer.alphaIsTransparency = true;
        importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        importer.filterMode = UnityEngine.FilterMode.Trilinear;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.isReadable = false;
    }
}
