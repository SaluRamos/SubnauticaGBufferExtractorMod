using UnityEngine;

[ExecuteInEditMode]
[DefaultExecutionOrder(99999)] 
public class DepthMapPost : MonoBehaviour
{

    private Shader shader;
    private Material material;

    void Start()
    {
        shader = LoadExternalShader("E:/UnityGBufferExtractorMod/BepInExBuiltIn/Shaders/bundle", "DepthPost");
        OnEnable();
    }

    void OnEnable()
    {
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
        if (shader != null && material == null)
        {
            material = new Material(shader);
            material.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (shader == null || material == null)
        {
            Graphics.Blit(source, destination);
            return;
        }
        // Ex: depthMaterial.SetFloat("_MaxDepth", 150.0f);
        Graphics.Blit(source, destination, material);
    }

    void OnDisable()
    {
        if (material != null)
        {
            DestroyImmediate(material);
        }
    }

    Shader LoadExternalShader(string bundlePath, string shaderName) {
        var bundle = AssetBundle.LoadFromFile(bundlePath);

        if (bundle == null)
        {
            Debug.LogError("Falha ao carregar AssetBundle!");
        }

        Shader loadedShader = bundle.LoadAsset<Shader>(shaderName);

        if (loadedShader != null) {
            if (!loadedShader.isSupported) {
                Debug.LogWarning(shaderName + " carregado, mas não suportado pela plataforma atual!");
            }
        } else {
            Debug.LogError(shaderName + " não encontrado no AssetBundle!");
        }

        bundle.Unload(false);
        return loadedShader;
    }

}
