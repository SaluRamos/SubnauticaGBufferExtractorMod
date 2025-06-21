using System.IO;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GBufferExtractor : MonoBehaviour
{

    public static string assetBundleFolderPath = "E:/UnityGBufferExtractorMod/BepInEx/Shaders";
    private static string assetBundlePath = $"{assetBundleFolderPath}/bundle";
    private string captureFolder = "E:/EPE/data/game_gbuffers/bepinex"; //local onde as imagens serão salvas

    private bool isCapturing = false;
    private float timer = 0f;
    private float captureInterval = 0.5f; // 500 ms

    private bool loadedShaders = false;

    private Camera mainCam;

    private Camera worldNormalCamera;
    private Shader worldNormalShader;

    private Camera localNormalCamera;
    private Shader localNormalShader;

    private Camera depthCamera;
    private Shader depthShader;

    private Shader albedoShader;
    private Camera albedoCamera;

    private Shader segmentationShader;
    private Camera segmentationCamera;

    void Start() {
        LoadShaders();
    }

    void Update() {
        #if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame) {
        #else
            if (Input.GetKeyDown(KeyCode.F10)) {
        #endif
                isCapturing = !isCapturing;
                Debug.Log($"Captura de G-Buffer {(isCapturing ? "iniciada" : "parada")}");
            }

        if (isCapturing)
        {
            timer += Time.deltaTime;
            if (timer >= captureInterval)
            {
                timer = 0f;
                LoadShaders();
                SaveCameraImage(mainCam);
                // SaveCameraImage(worldNormalCamera);
                // SaveCameraImage(localNormalCamera);
                // SaveCameraImage(depthCamera);
                // SaveCameraImage(albedoCamera);
                SaveSegmentationCameraByLayer();
                SaveSegmentationCameraByTag();
            }
        }
    }

    private void LoadShaders() {
        if (!loadedShaders) {
            Directory.CreateDirectory(captureFolder);
            mainCam = Camera.main;
            LoadWorldNormalCamera(mainCam, false);
            LoadLocalNormalCamera(mainCam, false);
            LoadDepthCamera(mainCam, false);
            LoadAlbedoCamera(mainCam, false);
            LoadSegmentationCamera(mainCam, false);
            //GameObject.Destroy(mainCam);
            loadedShaders = true;
        }
    }

    private void LoadWorldNormalCamera(Camera mainCam, bool active) {
        if (worldNormalCamera == null) {
            worldNormalCamera = new GameObject("WorldNormalCamera").AddComponent<Camera>();
            worldNormalCamera.transform.SetParent(mainCam.transform.parent);
            worldNormalCamera.transform.position = mainCam.transform.position;
            worldNormalCamera.transform.rotation = mainCam.transform.rotation;
            worldNormalCamera.transform.localScale = mainCam.transform.localScale;
            worldNormalCamera.cullingMask = ~0;
            worldNormalCamera.fieldOfView = mainCam.fieldOfView;
            worldNormalCamera.nearClipPlane = mainCam.nearClipPlane;
            worldNormalCamera.farClipPlane = mainCam.farClipPlane;
            worldNormalCamera.depth = mainCam.depth;
            worldNormalCamera.clearFlags = CameraClearFlags.SolidColor;
            worldNormalCamera.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            worldNormalCamera.enabled = active;
            worldNormalShader = LoadExternalShader(assetBundlePath, "WorldNormalShader");
            if (!worldNormalShader)
            {
                Debug.Log("'World' não encontrado no bundle!");
            }
            worldNormalCamera.SetReplacementShader(worldNormalShader, "");
        }
    }

    private void LoadLocalNormalCamera(Camera mainCam, bool active) {
        if (localNormalShader == null)
        {
            localNormalCamera = new GameObject("LocalNormalCamera").AddComponent<Camera>();
            localNormalCamera.transform.SetParent(mainCam.transform.parent);
            localNormalCamera.transform.position = mainCam.transform.position;
            localNormalCamera.transform.rotation = mainCam.transform.rotation;
            localNormalCamera.transform.localScale = mainCam.transform.localScale;
            localNormalCamera.cullingMask = ~0;
            localNormalCamera.fieldOfView = mainCam.fieldOfView;
            localNormalCamera.nearClipPlane = mainCam.nearClipPlane;
            localNormalCamera.farClipPlane = mainCam.farClipPlane;
            localNormalCamera.depth = mainCam.depth;
            localNormalCamera.clearFlags = CameraClearFlags.SolidColor;
            localNormalCamera.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            localNormalCamera.enabled = active;
            localNormalShader = LoadExternalShader(assetBundlePath, "LocalNormalShader");
            if (!localNormalShader)
            {
                Debug.Log("'LocalNormalShader' não encontrado no bundle!");
            }
            localNormalCamera.SetReplacementShader(localNormalShader, "");
        }
    }

    private void LoadDepthCamera(Camera mainCam, bool active) {
        if (depthShader == null) {
            depthCamera = new GameObject("DepthCamera").AddComponent<Camera>();
            depthCamera.transform.SetParent(mainCam.transform.parent);
            depthCamera.transform.position = mainCam.transform.position;
            depthCamera.transform.rotation = mainCam.transform.rotation;
            depthCamera.transform.localScale = mainCam.transform.localScale;
            depthCamera.cullingMask = ~0;
            depthCamera.fieldOfView = mainCam.fieldOfView;
            depthCamera.nearClipPlane = mainCam.nearClipPlane;
            depthCamera.farClipPlane = mainCam.farClipPlane;
            depthCamera.depth = mainCam.depth;
            depthCamera.clearFlags = CameraClearFlags.SolidColor;
            depthCamera.backgroundColor = new Color(1f, 1f, 1f, 1.0f);
            depthCamera.enabled = active;
            depthShader = LoadExternalShader(assetBundlePath, "DepthShader");
            if (!depthShader)
            {
                Debug.Log("'DepthShader' não encontrado no bundle!");
            }
            depthCamera.SetReplacementShader(depthShader, "");
        }
    }

    private void LoadAlbedoCamera(Camera mainCam, bool active) {
        if (albedoShader == null) {
            albedoCamera = new GameObject("AlbedoCamera").AddComponent<Camera>();
            albedoCamera.transform.SetParent(mainCam.transform.parent);
            albedoCamera.transform.position = mainCam.transform.position;
            albedoCamera.transform.rotation = mainCam.transform.rotation;
            albedoCamera.transform.localScale = mainCam.transform.localScale;
            albedoCamera.cullingMask = ~0;
            albedoCamera.fieldOfView = mainCam.fieldOfView;
            albedoCamera.nearClipPlane = mainCam.nearClipPlane;
            albedoCamera.farClipPlane = mainCam.farClipPlane;
            albedoCamera.depth = mainCam.depth;
            albedoCamera.clearFlags = CameraClearFlags.SolidColor;
            albedoCamera.backgroundColor = new Color(0f, 0f, 0f, 1f);
            albedoCamera.enabled = active;
            albedoShader = LoadExternalShader(assetBundlePath, "AlbedoShader");
            if (!albedoShader)
            {
                Debug.Log("'albedoShader' não encontrado no bundle!");
            }
            albedoCamera.SetReplacementShader(albedoShader, "");
        }
    }

    private void LoadSegmentationCamera(Camera mainCam, bool active) {
        if (segmentationShader == null) {
            segmentationCamera = new GameObject("SegmentationCamera").AddComponent<Camera>();
            segmentationCamera.transform.SetParent(mainCam.transform.parent);
            segmentationCamera.transform.position = mainCam.transform.position;
            segmentationCamera.transform.rotation = mainCam.transform.rotation;
            segmentationCamera.transform.localScale = mainCam.transform.localScale;
            segmentationCamera.cullingMask = ~0;
            segmentationCamera.fieldOfView = mainCam.fieldOfView;
            segmentationCamera.nearClipPlane = mainCam.nearClipPlane;
            segmentationCamera.farClipPlane = mainCam.farClipPlane;
            segmentationCamera.depth = mainCam.depth;
            segmentationCamera.enabled = active;
            segmentationShader = LoadExternalShader(assetBundlePath, "SegmentationShader");
            if (!segmentationShader)
            {
                Debug.Log("'segmentationShader' não encontrado no bundle!");
            }
            segmentationCamera.SetReplacementShader(segmentationShader, "");
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

    private void SaveCameraImage(Camera cam) {
        if (cam == null) {
            return;
        }

        string fileName = $"capture_{cam.gameObject.name}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.jpg";
        string fullPath = Path.Combine(captureFolder, fileName);

        int newWidth = cam.pixelWidth / 2;
        int newHeight = cam.pixelHeight / 2;

        RenderTexture rtFull = RenderTexture.GetTemporary(cam.pixelWidth, cam.pixelHeight, 24);
        RenderTexture rtHalf = RenderTexture.GetTemporary(newWidth, newHeight, 0);

        RenderTexture prevActiveRT = RenderTexture.active;
        RenderTexture prevCameraRT = cam.targetTexture;

        cam.targetTexture = rtFull;
        cam.Render();

        Graphics.Blit(rtFull, rtHalf);

        Texture2D screenShot = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
        RenderTexture.active = rtHalf;
        screenShot.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        screenShot.Apply();

        byte[] bytes = screenShot.EncodeToJPG(95);
        try {
            File.WriteAllBytes(fullPath, bytes);
        } catch (IOException ex) {
            Debug.LogError($"Erro ao salvar o arquivo: {ex.Message}");
        }

        // Limpeza de recursos para evitar vazamento de memoria
        cam.targetTexture = prevCameraRT; 
        RenderTexture.active = prevActiveRT; 
        RenderTexture.ReleaseTemporary(rtFull);
        RenderTexture.ReleaseTemporary(rtHalf);
        Destroy(screenShot);

    }

    private static readonly int SegmentationIDProp = Shader.PropertyToID("_SegmentationID");

    private void SaveSegmentationCameraByLayer() {
        if (segmentationCamera == null || segmentationShader == null) {
            return;
        }

        var layerMappings = new Dictionary<string, int>();
        int segmentationIdCounter = 1; // ID 0 é o fundo
        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(layerName))
            {
                layerMappings.Add(layerName, segmentationIdCounter);
                segmentationIdCounter++;
            }
        }

        var allRenderers = FindObjectsOfType<Renderer>();
        var propBlock = new MaterialPropertyBlock();
        foreach (var renderer in allRenderers) {
            if (!renderer.isVisible) continue;
            string layerName = LayerMask.LayerToName(renderer.gameObject.layer);
            if (layerMappings.TryGetValue(layerName, out int segmentationId))
            {
                renderer.GetPropertyBlock(propBlock); // Pega o bloco atual (boa prática)
                propBlock.SetFloat(SegmentationIDProp, (float)segmentationId); // Define nosso ID
                renderer.SetPropertyBlock(propBlock); // Aplica o bloco de volta
            }
        }

        int newWidth = segmentationCamera.pixelWidth / 2;
        int newHeight = segmentationCamera.pixelHeight / 2;

        RenderTexture rtFull = RenderTexture.GetTemporary(segmentationCamera.pixelWidth, segmentationCamera.pixelHeight, 24);
        RenderTexture rtHalf = RenderTexture.GetTemporary(newWidth, newHeight, 0);

        RenderTexture prevActiveRT = RenderTexture.active;
        RenderTexture prevCameraRT = segmentationCamera.targetTexture;

        segmentationCamera.targetTexture = rtFull;
        segmentationCamera.clearFlags = CameraClearFlags.SolidColor;
        segmentationCamera.backgroundColor = new Color(0.2862f, 0.4941f, 0.6745f, 1f);
        segmentationCamera.cullingMask = ~0;
        segmentationCamera.Render();

        Graphics.Blit(rtFull, rtHalf);

        string fileName = $"capture_seg_by_layer_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.jpg";
        string fullPath = Path.Combine(captureFolder, fileName);
        Texture2D screenShot = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
        RenderTexture.active = rtHalf;
        screenShot.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        screenShot.Apply();
        byte[] bytes = screenShot.EncodeToJPG(95);

        try {
            File.WriteAllBytes(fullPath, bytes);
        } catch (IOException ex) {
            Debug.LogError($"Erro ao salvar o arquivo: {ex.Message}");
        }
        Destroy(screenShot);

        // Limpeza
        segmentationCamera.targetTexture = prevCameraRT;
        RenderTexture.active = prevActiveRT;
        RenderTexture.ReleaseTemporary(rtFull);
        RenderTexture.ReleaseTemporary(rtHalf);
    }

    private void SaveSegmentationCameraByTag() {
        if (segmentationCamera == null || segmentationShader == null) {
            return;
        }

        var allRenderers = FindObjectsOfType<Renderer>();
        var tagsInUse = new HashSet<string>();
        foreach (var renderer in allRenderers) {
            tagsInUse.Add(renderer.gameObject.tag);
        }

        var tagMappings = new Dictionary<string, int>();
        int segmentationIdCounter = 1; // ID 0 é o fundo
        foreach (var tagName in tagsInUse) {
            tagMappings.Add(tagName, segmentationIdCounter);
            segmentationIdCounter++;
        }

        var propBlock = new MaterialPropertyBlock();
        foreach (var renderer in allRenderers) {
            if (!renderer.isVisible) continue;
            string objectTag = renderer.gameObject.tag;
            if (tagMappings.TryGetValue(objectTag, out int segmentationId)) {
                renderer.GetPropertyBlock(propBlock);
                propBlock.SetFloat(SegmentationIDProp, (float)segmentationId);
                renderer.SetPropertyBlock(propBlock);
            }
        }

        int newWidth = segmentationCamera.pixelWidth / 2;
        int newHeight = segmentationCamera.pixelHeight / 2;

        RenderTexture rtFull = RenderTexture.GetTemporary(segmentationCamera.pixelWidth, segmentationCamera.pixelHeight, 24);
        RenderTexture rtHalf = RenderTexture.GetTemporary(newWidth, newHeight, 0);

        RenderTexture prevActiveRT = RenderTexture.active;
        RenderTexture prevCameraRT = segmentationCamera.targetTexture;

        segmentationCamera.targetTexture = rtFull;
        segmentationCamera.clearFlags = CameraClearFlags.SolidColor;
        segmentationCamera.cullingMask = ~0;
        segmentationCamera.Render();

        Graphics.Blit(rtFull, rtHalf);

        string fileName = $"capture_seg_by_tag_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.jpg";
        string fullPath = Path.Combine(captureFolder, fileName);
        Texture2D screenShot = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
        RenderTexture.active = rtHalf;
        screenShot.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        screenShot.Apply();
        byte[] bytes = screenShot.EncodeToJPG(95);

        try {
            File.WriteAllBytes(fullPath, bytes);
        } catch (IOException ex) {
            Debug.LogError($"Erro ao salvar o arquivo: {ex.Message}");
        }
        Destroy(screenShot);

        // Limpeza
        segmentationCamera.targetTexture = prevCameraRT;
        RenderTexture.active = prevActiveRT;
        RenderTexture.ReleaseTemporary(rtFull);
        RenderTexture.ReleaseTemporary(rtHalf);
    }

}
