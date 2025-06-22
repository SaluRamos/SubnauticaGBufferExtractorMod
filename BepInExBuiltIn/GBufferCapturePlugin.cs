using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GBufferCapture {

    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class GBufferCapturePlugin : BaseUnityPlugin {

        public static string assetBundleFolderPath = "E:/UnityGBufferExtractorMod/BepInExBuiltIn/Shaders";
        private static string assetBundlePath = $"{assetBundleFolderPath}/bundle";
        private string captureFolder = "E:/EPE/data/game_gbuffers/bepinex"; //local onde as imagens serão salvas

        private bool isCapturing = false;
        private float timer = 0f;
        private float captureInterval = 0.5f; // 500 ms

        private string lastSceneName;
        private Camera lastMainCamera;
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
        private static readonly int SegmentationIDProp = Shader.PropertyToID("_SegmentationID");

        private Shader specularShader;
        private Camera specularCamera;

        private Shader glossinessShader;
        private Camera glossinessCamera;

        private Shader emissionShader;
        private Camera emissionCamera;

        private const string MyGUID = "com.Salu.GBufferCapture";
        private const string PluginName = "GBufferCapture";
        private const string VersionString = "1.0.0";
        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        public static string captureThreadSleepKey = "Capture Thread Sleep";
        public static ConfigEntry<float> captureThreadSleep;

        private Dictionary<string, ConfigEntry<bool>> cameraToggleEntries = new Dictionary<string, ConfigEntry<bool>>();
        private Dictionary<string, Action> cameraButtons = new Dictionary<string, Action>();
        private List<Camera> availableCameras = new List<Camera>();

        private void Awake() {
            captureThreadSleep = Config.Bind("General", captureThreadSleepKey, 1.0f, new ConfigDescription("Set time between captures in seconds", new AcceptableValueRange<float>(0.0f, 10.0f)));
            captureThreadSleep.SettingChanged += ConfigSettingChanged;

            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            Log = Logger;
            if (GraphicsSettings.renderPipelineAsset == null) {
                Logger.LogInfo("Using Built-in Render Pipeline");
            } else { 
                Logger.LogInfo("Using SRP: " + GraphicsSettings.renderPipelineAsset.GetType().Name);
            }
        }

        private void ConfigSettingChanged(object sender, System.EventArgs e) {
            SettingChangedEventArgs settingChangedEventArgs = e as SettingChangedEventArgs;
            if (settingChangedEventArgs == null) {
                return;
            }
            if (settingChangedEventArgs.ChangedSetting.Definition.Key == captureThreadSleepKey) {
                captureInterval = captureThreadSleep.Value;
            }
        }

        private void RefreshCameraList() {
            mainCam = null;
            ResetCustomCameras();
            availableCameras.Clear();
            availableCameras.AddRange(Camera.allCameras);
            cameraButtons.Clear();

            foreach (var cam in availableCameras) {
                string camName = cam.name;
                cameraButtons[camName] = () => {
                    mainCam = cam;
                    ResetCustomCameras();
                    Logger.LogInfo($"Câmera selecionada: {mainCam.name}");
                };
            }

            string sceneName = SceneManager.GetActiveScene().name;
            Debug.Log("actual scene name: " + sceneName);
            lastSceneName = sceneName;

            LogAllCameras();

            foreach (var entry in cameraToggleEntries.Values) { 
                Config.Remove(entry.Definition);
            }
            cameraToggleEntries.Clear();

            foreach (var entry in cameraButtons) {
                string camName = entry.Key;
                var configEntry = Config.Bind("Cameras", $"Select: {camName}", false, $"Click to define {camName} as mainCamera.");
                cameraToggleEntries[camName] = configEntry;

                configEntry.SettingChanged += (s, e) =>
                {
                    if (configEntry.Value)
                    {
                        cameraButtons[camName].Invoke();
                        configEntry.Value = false;
                    }
                };
            }
        }

        private void ResetCustomCameras() {
            GameObject.Destroy(worldNormalCamera);
            GameObject.Destroy(localNormalCamera);
            GameObject.Destroy(depthCamera);
            GameObject.Destroy(albedoCamera);
            GameObject.Destroy(segmentationCamera);
            GameObject.Destroy(specularCamera);
            GameObject.Destroy(glossinessCamera);
            GameObject.Destroy(emissionCamera);
            worldNormalCamera = null;
            localNormalCamera = null;
            depthCamera = null;
            albedoCamera = null;
            segmentationCamera = null;
            specularCamera = null;
            glossinessCamera = null;
            emissionCamera = null;
        }

        private void LogAllCameras() {
            Camera[] cameras = Camera.allCameras;
            Debug.Log("amount scene cams: " + cameras.Length);
            foreach (Camera cam in cameras) {
                Debug.Log($"Camera: {cam.name}, enabled: {cam.enabled}, depth: {cam.depth}, far clip plane: {cam.farClipPlane}, pos: {cam.transform.position}, rot: {cam.transform.rotation}");
            }
            // Logger.LogInfo("================ DUMP DA HIERARQUIA DA CENA ================");
            // DumpSceneHierarchy();
            // Logger.LogInfo("================ FIM DO DUMP DA HIERARQUIA ================");
            // foreach (var pair in GetTags()) {
            //     Debug.Log($"TAG {pair.Value} = {pair.Key}");
            // }
            // foreach (var pair in GetLayers()) {
            //     Debug.Log($"LAYER {pair.Value} = {pair.Key}");
            // }
        }

        private void DumpSceneHierarchy() {
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] rootObjects = activeScene.GetRootGameObjects();
            foreach (var rootObject in rootObjects) {
                DumpGameObjectRecursive(rootObject, "");
            }
        }

        private void DumpGameObjectRecursive(GameObject obj, string indent) {
            if (obj == null) return;
            if (obj.name != "ChunkGrass(Clone)" && obj.name != "ChunkLayer(Clone)" && obj.name != "Chunk(Clone)" && obj.name != "ChunkCollider(Clone)") {
                Logger.LogInfo($"{indent}+ {obj.name} (Tag: {obj.tag}, Layer: {LayerMask.LayerToName(obj.layer)}, Ativo: {obj.activeSelf})");
            }
            if (obj.tag != "Creature" && obj.name != "Base(Clone)") {
                foreach (Transform child in obj.transform) {
                    DumpGameObjectRecursive(child.gameObject, indent + "  ");
                }
            }
        }

        void Update() {
            #if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame) {
            #else
                if (Input.GetKeyDown(KeyCode.F12)) {
            #endif
                    LogAllCameras();
                }

            #if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null && Keyboard.current.f11Key.wasPressedThisFrame) {
            #else
                if (Input.GetKeyDown(KeyCode.F11)) {
            #endif
                    RefreshCameraList();
                }

            #if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame) {
            #else
                if (Input.GetKeyDown(KeyCode.F10)) {
            #endif
                    isCapturing = !isCapturing;
                    Debug.Log($"Captura de G-Buffer {(isCapturing ? "iniciada" : "parada")}");
                }

            if (isCapturing) {
                timer += Time.deltaTime;
                if (timer >= captureInterval) {
                    timer = 0f;
                    if (mainCam == null) {
                        Logger.LogDebug("No camera selected");
                        return;
                    }
                    if (lastSceneName != SceneManager.GetActiveScene().name){
                        Logger.LogDebug("You need to update scene cameras");
                        return;
                    }
                    StartCoroutine(Capture());
                }
            }
        }

        private IEnumerator Capture() {
            isCapturing = false;
            LoadShaders();
            string timestamp = $"{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}";
            yield return StartCoroutine(CaptureFinalFrameCoroutine($"{timestamp}_capture_main.jpg"));
            //SaveCameraImage(mainCam, timestamp);
            //SaveCameraImage(worldNormalCamera, timestamp);
            //SaveCameraImage(localNormalCamera, timestamp);
            SaveCameraImage(depthCamera, timestamp);
            //SaveCameraImage(albedoCamera, timestamp);
            //SaveSegmentationCameraByLayer(timestamp);
            //SaveSegmentationCameraByTag(timestamp);
            //SaveCameraImage(specularCamera, timestamp);
            //SaveCameraImage(glossinessCamera, timestamp);
            //SaveCameraImage(emissionCamera, timestamp);
            isCapturing = true;
        }

        private void LoadShaders() {
            if (lastMainCamera != mainCam && mainCam != null && lastSceneName == SceneManager.GetActiveScene().name) {
                Directory.CreateDirectory(captureFolder);
                LoadWorldNormalCamera(mainCam, false);
                LoadLocalNormalCamera(mainCam, false);
                LoadDepthCamera(mainCam, false);
                LoadAlbedoCamera(mainCam, false);
                LoadSegmentationCamera(mainCam, false);
                LoadSpecularCamera(mainCam, false);
                LoadGlossinessCamera(mainCam, false);
                LoadEmissionCamera(mainCam, false);
                // GameObject.Destroy(mainCam);
                lastMainCamera = mainCam;
            }
        }

        private void LoadWorldNormalCamera(Camera mainCam, bool active) {
            if (worldNormalCamera == null) {
                Debug.Log("Criando WorldNormalCamera");
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
            if (localNormalShader == null) {
                Debug.Log("Criando LocalNormalCamera");
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
            if (depthShader == null)
            {
                Debug.Log("Criando DepthCamera");
                // GameObject clonedCamObject = GameObject.Instantiate(mainCam.gameObject, mainCam.transform.parent);
                // clonedCamObject.name = "DepthCamera";
                // depthCamera = clonedCamObject.GetComponent<Camera>();
                // // depthCamera = new GameObject("DepthCamera").AddComponent<Camera>();
                // // depthCamera.transform.SetParent(mainCam.transform.parent, false);
                // // depthCamera.transform.localPosition = Vector3.zero;
                // // depthCamera.transform.localRotation = Quaternion.identity;
                // // depthCamera.transform.localScale = Vector3.one;
                // depthCamera.cullingMask = ~0;
                // depthCamera.fieldOfView = mainCam.fieldOfView;
                // depthCamera.nearClipPlane = mainCam.nearClipPlane;
                // depthCamera.farClipPlane = mainCam.farClipPlane;
                // depthCamera.depth = mainCam.depth;
                // depthCamera.clearFlags = CameraClearFlags.SolidColor;
                // depthCamera.backgroundColor = new Color(1f, 1f, 1f, 1.0f);
                // depthCamera.enabled = active;
                // depthShader = LoadExternalShader(assetBundlePath, "DepthShader");
                // if (!depthShader)
                // {
                //     Debug.Log("'DepthShader' não encontrado no bundle!");
                // }
                // // depthCamera.SetReplacementShader(depthShader, "");

                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = new Color(1f, 1f, 1f, 1.0f);
                depthShader = LoadExternalShader(assetBundlePath, "DepthShader");
                if (!depthShader)
                {
                    Debug.Log("'DepthShader' não encontrado no bundle!");
                }
                mainCam.SetReplacementShader(depthShader, "");
            }
        }

        private void LoadAlbedoCamera(Camera mainCam, bool active) {
            if (albedoShader == null) {
                Debug.Log("Criando AlbedoCamera");
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
                Debug.Log("Criando SegmentationCamera");
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

        private void LoadSpecularCamera(Camera mainCam, bool active) {
            if (specularShader == null) {
                Debug.Log("Criando SpecularCamera");
                specularCamera = new GameObject("SpecularCamera").AddComponent<Camera>();
                specularCamera.transform.SetParent(mainCam.transform.parent);
                specularCamera.transform.position = mainCam.transform.position;
                specularCamera.transform.rotation = mainCam.transform.rotation;
                specularCamera.transform.localScale = mainCam.transform.localScale;
                specularCamera.cullingMask = ~0;
                specularCamera.fieldOfView = mainCam.fieldOfView;
                specularCamera.nearClipPlane = mainCam.nearClipPlane;
                specularCamera.farClipPlane = mainCam.farClipPlane;
                specularCamera.depth = mainCam.depth;
                specularCamera.clearFlags = CameraClearFlags.SolidColor;
                specularCamera.backgroundColor = new Color(0f, 0f, 0f, 1f);
                specularCamera.enabled = active;
                specularShader = LoadExternalShader(assetBundlePath, "SpecularShader");
                if (!specularShader)
                {
                    Debug.Log("'SpecularShader' não encontrado no bundle!");
                }
                specularCamera.SetReplacementShader(specularShader, "");
            }
        }

        private void LoadGlossinessCamera(Camera mainCam, bool active) {
            if (glossinessShader == null) {
                Debug.Log("Criando GlossinessCamera");
                glossinessCamera = new GameObject("GlossinessCamera").AddComponent<Camera>();
                glossinessCamera.transform.SetParent(mainCam.transform.parent);
                glossinessCamera.transform.position = mainCam.transform.position;
                glossinessCamera.transform.rotation = mainCam.transform.rotation;
                glossinessCamera.transform.localScale = mainCam.transform.localScale;
                glossinessCamera.cullingMask = ~0;
                glossinessCamera.fieldOfView = mainCam.fieldOfView;
                glossinessCamera.nearClipPlane = mainCam.nearClipPlane;
                glossinessCamera.farClipPlane = mainCam.farClipPlane;
                glossinessCamera.depth = mainCam.depth;
                glossinessCamera.clearFlags = CameraClearFlags.SolidColor;
                glossinessCamera.backgroundColor = new Color(1f, 1f, 1f, 1f);
                glossinessCamera.enabled = active;
                glossinessShader = LoadExternalShader(assetBundlePath, "GlossinessShader");
                if (!glossinessShader)
                {
                    Debug.Log("'GlossinessShader' não encontrado no bundle!");
                }
                glossinessCamera.SetReplacementShader(glossinessShader, "");
            }
        }

        private void LoadEmissionCamera(Camera mainCam, bool active) {
            if (emissionShader == null) {
                Debug.Log("Criando EmissionCamera");
                emissionCamera = new GameObject("EmissionCamera").AddComponent<Camera>();
                emissionCamera.transform.SetParent(mainCam.transform.parent);
                emissionCamera.transform.position = mainCam.transform.position;
                emissionCamera.transform.rotation = mainCam.transform.rotation;
                emissionCamera.transform.localScale = mainCam.transform.localScale;
                emissionCamera.cullingMask = ~0;
                emissionCamera.fieldOfView = mainCam.fieldOfView;
                emissionCamera.nearClipPlane = mainCam.nearClipPlane;
                emissionCamera.farClipPlane = mainCam.farClipPlane;
                emissionCamera.depth = mainCam.depth;
                emissionCamera.clearFlags = CameraClearFlags.SolidColor;
                emissionCamera.backgroundColor = new Color(1f, 1f, 1f, 1f);
                emissionCamera.enabled = active;
                emissionShader = LoadExternalShader(assetBundlePath, "EmissionShader");
                if (!emissionShader)
                {
                    Debug.Log("'emissionShader' não encontrado no bundle!");
                }
                emissionCamera.SetReplacementShader(specularShader, "");
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

        private IEnumerator CaptureFinalFrameCoroutine(string fileName) {
            yield return new WaitForEndOfFrame();
            Texture2D screenShot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenShot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenShot.Apply();
            byte[] bytes = screenShot.EncodeToJPG(95); // 95 é a qualidade
            Destroy(screenShot);
            string fullPath = Path.Combine(captureFolder, fileName);
            try {
                File.WriteAllBytes(fullPath, bytes);
            } catch (IOException ex) {
                Debug.LogError($"Erro ao salvar o arquivo: {ex.Message}");
            }
        }

        private void SaveCameraImage(Camera cam, string timestamp) {
            if (cam == null) {
                return;
            }
            RenderTexture rtFull = RenderTexture.GetTemporary(cam.pixelWidth, cam.pixelHeight, 24);
            RenderTexture camTargetTex = cam.targetTexture;
            cam.targetTexture = rtFull;
            cam.Render();
            SaveJPG($"{timestamp}_capture_{cam.gameObject.name}.jpg", cam, rtFull, camTargetTex);
        }

        private void SaveSegmentationCameraByLayer(string timestamp) {
            if (segmentationCamera == null || segmentationShader == null) {
                return;
            }

            var layerMappings = GetLayers();

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

            RenderTexture rtFull = RenderTexture.GetTemporary(segmentationCamera.pixelWidth, segmentationCamera.pixelHeight, 24);
            RenderTexture camTargetTex = segmentationCamera.targetTexture;
            segmentationCamera.targetTexture = rtFull;
            segmentationCamera.clearFlags = CameraClearFlags.SolidColor;
            segmentationCamera.backgroundColor = new Color(0.2862f, 0.4941f, 0.6745f, 1f);
            segmentationCamera.cullingMask = ~0;
            segmentationCamera.Render();
            SaveJPG($"{timestamp}_capture_seg_by_layer.jpg", segmentationCamera, rtFull, camTargetTex);
        }

        private Dictionary<string, int> GetLayers() {
            var layerMappings = new Dictionary<string, int>();
            int segmentationIdCounter = 1; // ID 0 é o fundo
            for (int i = 0; i < 32; i++) {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName)) {
                    layerMappings.Add(layerName, segmentationIdCounter);
                    segmentationIdCounter++;
                }
            }
            Debug.Log($"Total de layers encontrados: {layerMappings.Count()}");
            return layerMappings;
        }

        private void SaveSegmentationCameraByTag(string timestamp) {
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

            RenderTexture rtFull = RenderTexture.GetTemporary(segmentationCamera.pixelWidth, segmentationCamera.pixelHeight, 24);
            RenderTexture camTargetTex = segmentationCamera.targetTexture;
            segmentationCamera.targetTexture = rtFull;
            segmentationCamera.clearFlags = CameraClearFlags.SolidColor;
            segmentationCamera.cullingMask = ~0;
            segmentationCamera.Render();
            SaveJPG($"{timestamp}_capture_seg_by_tag.jpg", segmentationCamera, rtFull, camTargetTex);
        }

        private Dictionary<string, int> GetTags() {
            var tagsCount = new Dictionary<string, int>();
            GameObject[] allGameObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject go in allGameObjects) {
                if (tagsCount.ContainsKey(go.tag))
                {
                    tagsCount[go.tag]++;
                }
                else
                {
                    tagsCount.Add(go.tag, 1);
                }
            }
            Debug.Log($"Total de tags encontrados: {tagsCount.Count()}");
            return tagsCount;
        }

        private void SaveJPG(string fileName, Camera cam, RenderTexture rtFull, RenderTexture camTargetTex) {
            string fullPath = Path.Combine(captureFolder, fileName);
            int newWidth = cam.pixelWidth / 2;
            int newHeight = cam.pixelHeight / 2;
            RenderTexture rtHalf = RenderTexture.GetTemporary(newWidth, newHeight, 0);
            RenderTexture prevActiveRT = RenderTexture.active;
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
            // Limpeza
            Destroy(screenShot);
            cam.targetTexture = camTargetTex;
            RenderTexture.active = prevActiveRT;
            RenderTexture.ReleaseTemporary(rtFull);
            RenderTexture.ReleaseTemporary(rtHalf);
        }

    }
}
