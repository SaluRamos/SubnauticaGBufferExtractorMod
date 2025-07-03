using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class BuildShaderAssetBundle
{

    [MenuItem("Build/Build Shader AssetBundle")]
    public static void BuildBundle()
    {
        var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
        var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        clearMethod.Invoke(null, null);

        AssetBundleBuild[] buildMap = new AssetBundleBuild[1];

        string[] shaderFiles = Directory.GetFiles("Assets/Shaders", "*.shader", SearchOption.AllDirectories);

        foreach (string file in shaderFiles)
        {
            Debug.Log($"compiling {file}");
        }

        buildMap[0].assetBundleName = "bundle";
        buildMap[0].assetNames = shaderFiles.Select(path => path.Replace('\\', '/')).ToArray();

        Directory.CreateDirectory(DepthMapPost.assetBundleFolderPath);
        BuildPipeline.BuildAssetBundles(DepthMapPost.assetBundleFolderPath, buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
        Debug.Log("AssetBundle criado em " + DepthMapPost.assetBundleFolderPath);
    }

}
