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
            "Assets/Shaders/ShaderReplacement/UnlitAlbedo.shader",
            "Assets/Shaders/Post/DepthPost.shader",
            "Assets/Shaders/ShaderInterception/Triplanar.shader",
            "Assets/Shaders/ShaderInterception/TriplanarWithCapping.shader"
        };

        BuildPipeline.BuildAssetBundles(GBufferExtractor.assetBundleFolderPath, buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
        Debug.Log("AssetBundle criado em " + GBufferExtractor.assetBundleFolderPath);
    }

}
