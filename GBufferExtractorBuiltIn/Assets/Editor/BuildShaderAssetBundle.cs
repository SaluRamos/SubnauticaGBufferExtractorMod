using UnityEngine;
using UnityEditor;

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
            "Assets/Shaders/ReconstructNormal.shader"
        };

        BuildPipeline.BuildAssetBundles(DepthMapPost.assetBundleFolderPath, buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
        Debug.Log("AssetBundle criado em " + DepthMapPost.assetBundleFolderPath);
    }

}
