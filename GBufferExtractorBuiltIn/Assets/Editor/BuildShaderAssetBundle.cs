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
            "Assets/Shaders/WorldNormalShader.shader",
            "Assets/Shaders/LocalNormalShader.shader",
            "Assets/Shaders/DepthShader.shader",
            "Assets/Shaders/AlbedoShader.shader",
            "Assets/Shaders/SegmentationShader.shader",
            "Assets/Shaders/SpecularShader.shader",
            "Assets/Shaders/GlossinessShader.shader",
            "Assets/Shaders/EmissionShader.shader",
            "Assets/Shaders/Passthrough.shader"
        };

        BuildPipeline.BuildAssetBundles(GBufferExtractor.assetBundleFolderPath, buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
        Debug.Log("AssetBundle criado em " + GBufferExtractor.assetBundleFolderPath);
    }

}
