using UnityEditor;
using UnityEngine;

public class BuildShaderAssetBundle
{

    [MenuItem("Build/Build Shader AssetBundle")]
    public static void BuildBundle()
    {
        AssetBundleBuild[] buildMap = new AssetBundleBuild[2];

        buildMap[0].assetBundleName = "bundle";
        buildMap[0].assetNames = new string[] {
            "Assets/Shaders/DepthPost.shader",
            "Assets/Shaders/NormalPost.shader",
            "Assets/Shaders/WaterSurface.shader",
            "Assets/Shaders/Triplanar.shader"
        };

        BuildPipeline.BuildAssetBundles(GBufferExtractor.assetBundleFolderPath, buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
        Debug.Log("AssetBundle criado em " + GBufferExtractor.assetBundleFolderPath);
    }

}
