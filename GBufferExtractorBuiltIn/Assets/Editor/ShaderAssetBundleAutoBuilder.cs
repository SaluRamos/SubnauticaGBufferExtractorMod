using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public class ShaderAssetBundleAutoBuilder : AssetPostprocessor
{
    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        bool shaderChanged = importedAssets.Any(path => path.EndsWith(".shader")) ||
                             deletedAssets.Any(path => path.EndsWith(".shader")) ||
                             movedAssets.Any(path => path.EndsWith(".shader")) ||
                             movedFromAssetPaths.Any(path => path.EndsWith(".shader"));

        if (shaderChanged)
        {
            Debug.Log("[AutoBuild] Shader modificado, recompilando AssetBundle...");
            BuildShaderAssetBundle.BuildBundle();
        }
    }
}
