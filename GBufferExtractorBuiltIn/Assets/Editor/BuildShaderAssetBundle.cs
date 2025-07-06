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

        string[] shaderFiles = Directory.GetFiles("Assets/Shaders", "*.shader", SearchOption.AllDirectories);

        Debug.Log("Verificando shaders antes do build...");
        foreach (string path in shaderFiles)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader != null && ShaderUtil.ShaderHasError(shader))
            {
                Debug.LogError($"BUILD ABORTADO: O shader em '{path}' contém erros de compilação.");
                return;
            }
        }
        Debug.Log("Nenhum erro de importação encontrado. Iniciando o build...");

        AssetBundleBuild[] buildMap = new AssetBundleBuild[1];

        foreach (string file in shaderFiles)
        {
            Debug.Log($"compiling {file}");
        }

        buildMap[0].assetBundleName = "bundle";
        buildMap[0].assetNames = shaderFiles.Select(path => path.Replace('\\', '/'))
        .ToArray();

        Directory.CreateDirectory(DepthMapPost.assetBundleFolderPath);

        BuildAssetBundleOptions options = BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.ForceRebuildAssetBundle;

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(DepthMapPost.assetBundleFolderPath, buildMap, options, BuildTarget.StandaloneWindows64);
        if (manifest == null)
        {
            Debug.LogError("FAILED ASSET BUNDLE CREATION");
        }
        else
        {
            Debug.Log("AssetBundle criado em " + DepthMapPost.assetBundleFolderPath);
        }
    }

}
