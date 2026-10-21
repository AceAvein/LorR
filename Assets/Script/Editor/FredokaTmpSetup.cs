#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

public class FredokaTmpSetup : AssetPostprocessor
{
    static readonly string[] FontPaths =
    {
        "Assets/Fonts/Fredoka/FredokaOne-Regular.ttf",
        "Assets/Fonts/Fredoka/Fredoka-Bold.ttf"
    };

    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        for (int i = 0; i < importedAssets.Length; i++)
        {
            for (int f = 0; f < FontPaths.Length; f++)
            {
                if (importedAssets[i] == FontPaths[f])
                    CreateSdf(FontPaths[f]);
            }
        }
    }

    [InitializeOnLoadMethod]
    static void EnsureExisting()
    {
        EditorApplication.delayCall += () =>
        {
            for (int i = 0; i < FontPaths.Length; i++)
                CreateSdf(FontPaths[i]);
        };
    }

    static void CreateSdf(string fontPath)
    {
        string sdfPath = fontPath.Replace(".ttf", " SDF.asset");
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(sdfPath) != null)
            return;

        Font font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (font == null)
            return;

        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font);
        if (asset == null)
            return;

        AssetDatabase.CreateAsset(asset, sdfPath);
        if (asset.material != null)
            AssetDatabase.AddObjectToAsset(asset.material, asset);
        if (asset.atlasTextures != null)
        {
            for (int i = 0; i < asset.atlasTextures.Length; i++)
            {
                if (asset.atlasTextures[i] != null)
                    AssetDatabase.AddObjectToAsset(asset.atlasTextures[i], asset);
            }
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }
}
#endif
