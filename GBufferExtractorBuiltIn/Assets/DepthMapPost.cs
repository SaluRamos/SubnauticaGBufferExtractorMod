using UnityEngine;
using UnityEngine.Rendering;

public class DepthMapPost : MonoBehaviour
{

    public static string assetBundleFolderPath = "G:/Steam/steamapps/common/Subnautica/BepInEx/plugins/GBufferCapture/Shaders";
    public static string assetBundlePath = $"{assetBundleFolderPath}/bundle";

    public Texture2D tex;

    private Shader shader;
    private Material material;
    private Camera mainCam;

    private RenderTexture depthRT;
    private RenderTexture rt;
    private CommandBuffer cb;

    private float gbuffersMaxRenderDistance = 130.0f;

    void Start()
    {
        mainCam = GetComponent<Camera>();

        depthRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.RFloat);
        depthRT.Create();

        rt = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
        rt.Create();

        shader = LoadExternalShader(assetBundlePath, "DepthPost");
        if (shader == null)
        {
            Debug.LogError("Shader não definido");
            return;
        }
        material = new Material(shader);
        material.SetTexture("_MainTex", tex);
        material.hideFlags = HideFlags.HideAndDontSave;

        mainCam.depthTextureMode = DepthTextureMode.Depth;

        cb = new CommandBuffer();
        cb.name = "Depth to WorldPos";
        cb.Blit(BuiltinRenderTextureType.CameraTarget, rt, material);
        cb.Blit(BuiltinRenderTextureType.Depth, depthRT);
        mainCam.AddCommandBuffer(CameraEvent.AfterEverything, cb);
    }

    void Update()
    {
        if (cb != null)
        {
            cb.SetGlobalMatrix("_CameraProj", mainCam.projectionMatrix);
            cb.SetGlobalMatrix("CameraToWorld", mainCam.cameraToWorldMatrix);
            cb.SetGlobalFloat("_DepthCutoff", gbuffersMaxRenderDistance);
        }
    }

    void OnGUI()
    {
        if (rt != null)
        {
            GUI.DrawTexture(new Rect(0, 0, 256, 256), rt, ScaleMode.ScaleToFit, false);
            GUI.DrawTexture(new Rect(256, 0, 256, 256), depthRT, ScaleMode.ScaleToFit, false);
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
