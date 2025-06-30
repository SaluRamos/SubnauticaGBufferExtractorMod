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
            "Assets/Shaders/Post/DepthPost.shader",
            "Assets/Shaders/Post/NormalPost.shader",
            "Assets/Shaders/Post/WaterSurface.shader",
            "Assets/Shaders/WaterSurface/UpdateNormals.shader", //usado no WaterSurface via WaterSurfaceBeforeGBufferOnCamera.cs
        };

        BuildPipeline.BuildAssetBundles(GBufferExtractor.assetBundleFolderPath, buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
        Debug.Log("AssetBundle criado em " + GBufferExtractor.assetBundleFolderPath);
    }

}
