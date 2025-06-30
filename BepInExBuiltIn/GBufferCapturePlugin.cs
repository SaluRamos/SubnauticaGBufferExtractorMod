using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using WaterMod;

namespace GBufferCapture {

    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class GBufferCapturePlugin : BaseUnityPlugin
    {

        public static string assetBundleFolderPath = "E:/UnityGBufferExtractorMod/BepInExBuiltIn/Shaders";
        public static string assetBundlePath = $"{assetBundleFolderPath}/bundle";
        public string captureFolder = "E:/EPE/data/game_gbuffers/bepinex"; //local onde as imagens serão salvas

        private bool isCapturing = false;
        private float timer = 0f;
        private float captureInterval = 0.5f; // 500 ms

        private Camera mainCam;

        private const string MyGUID = "com.Salu.GBufferCapture";
        private const string PluginName = "GBufferCapture";
        private const string VersionString = "1.0.0";
        private static readonly Harmony harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        public static string captureThreadSleepKey = "Capture Thread Sleep";
        public static ConfigEntry<float> captureThreadSleep;

        private void Awake()
        {
            captureThreadSleep = Config.Bind("General", captureThreadSleepKey, 1.0f, new ConfigDescription("Set time between captures in seconds", new AcceptableValueRange<float>(0.0f, 10.0f)));
            captureThreadSleep.SettingChanged += ConfigSettingChanged;

            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            harmony.PatchAll();
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            Log = Logger;
            if (GraphicsSettings.renderPipelineAsset == null)
            {
                Logger.LogInfo("Using Built-in Render Pipeline");
            }
            else
            {
                Logger.LogInfo("Using SRP: " + GraphicsSettings.renderPipelineAsset.GetType().Name);
            }
        }

        private void ConfigSettingChanged(object sender, System.EventArgs e)
        {
            SettingChangedEventArgs settingChangedEventArgs = e as SettingChangedEventArgs;
            if (settingChangedEventArgs == null)
            {
                return;
            }
            if (settingChangedEventArgs.ChangedSetting.Definition.Key == captureThreadSleepKey)
            {
                captureInterval = captureThreadSleep.Value;
            }
        }

        private float mapsRenderDistance = 130f;

        private CommandBuffer depthCB;
        private RenderTexture depthRT;
        private Shader depthShader;
        private Material depthMaterial;

        private void SetupDepth()
        {
            depthRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
            depthRT.Create();

            depthShader = LoadExternalShader(assetBundlePath, "DepthPost");
            depthMaterial = new Material(depthShader);
            depthMaterial.hideFlags = HideFlags.HideAndDontSave;
            depthMaterial.SetFloat("_DepthCutoff", mapsRenderDistance);

            mainCam.depthTextureMode = DepthTextureMode.Depth;

            depthCB = new CommandBuffer();
            depthCB.name = "Capture Depth";
            depthCB.Blit(BuiltinRenderTextureType.CameraTarget, depthRT, depthMaterial);
            mainCam.AddCommandBuffer(CameraEvent.AfterEverything, depthCB);
        }

        private CommandBuffer normalCB;
        private RenderTexture normalRT;
        private Shader normalShader;
        private Material normalMaterial;

        private void SetupNormal()
        {
            normalRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
            normalRT.Create();

            normalShader = LoadExternalShader(assetBundlePath, "NormalPost");
            normalMaterial = new Material(normalShader);
            normalMaterial.hideFlags = HideFlags.HideAndDontSave;
            normalMaterial.SetFloat("_DepthCutoff", mapsRenderDistance);

            normalCB = new CommandBuffer();
            normalCB.name = "Capture Normal";
            normalCB.Blit(BuiltinRenderTextureType.GBuffer2, normalRT, normalMaterial);
            mainCam.AddCommandBuffer(CameraEvent.AfterEverything, normalCB);
        }

        private WaterGBufferInjector injectorInstance;
        void SetupWaterSurfaceOnGBuffers()
        {
            if (injectorInstance != null)
            {
                return;
            }
            injectorInstance = mainCam.gameObject.AddComponent<WaterGBufferInjector>();
        }

        void OnGUI()
        {
            if (depthRT != null)
            {
                // Desenha a textura no canto superior esquerdo com 256 pixels de largura
                GUI.DrawTexture(new Rect(0, 0, 256, 256), depthRT, ScaleMode.ScaleToFit, false);
            }
            if (normalRT != null)
            {
                GUI.DrawTexture(new Rect(256, 0, 256, 256), normalRT, ScaleMode.ScaleToFit, false);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                mainCam = FindObjectOfType<WaterSurfaceOnCamera>()?.gameObject.GetComponent<Camera>();
                SetupWaterSurfaceOnGBuffers();
                SetupDepth();
                SetupNormal();
            }

             if (Input.GetKeyDown(KeyCode.F10))
            {
                isCapturing = !isCapturing;
                Debug.Log($"Captura de G-Buffer {(isCapturing ? "iniciada" : "parada")}");
            }

            if (isCapturing)
            {
                timer += Time.deltaTime;
                if (timer >= captureInterval)
                {
                    timer = 0f;
                    if (mainCam == null)
                    {
                        Logger.LogDebug("No camera selected");
                        return;
                    }
                    StartCoroutine(Capture());
                }
            }
        }

        private IEnumerator Capture()
        {
            isCapturing = false;
            string timestamp = $"{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}";
            yield return StartCoroutine(CaptureFinalFrameCoroutine($"{timestamp}_capture_main.jpg"));
            isCapturing = true;
        }

        public static Shader LoadExternalShader(string bundlePath, string shaderName)
        {
            var bundle = AssetBundle.LoadFromFile(bundlePath);

            if (bundle == null)
            {
                Debug.LogError("Falha ao carregar AssetBundle!");
            }

            Shader loadedShader = bundle.LoadAsset<Shader>(shaderName);

            if (loadedShader != null)
            {
                if (!loadedShader.isSupported)
                {
                    Debug.LogWarning(shaderName + " carregado, mas não suportado pela plataforma atual!");
                }
            }
            else
            {
                Debug.LogError(shaderName + " não encontrado no AssetBundle!");
            }

            bundle.Unload(false);
            return loadedShader;
        }

        private IEnumerator CaptureFinalFrameCoroutine(string fileName)
        {
            yield return new WaitForEndOfFrame();
            Texture2D screenShot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenShot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenShot.Apply();
            byte[] bytes = screenShot.EncodeToJPG(95); // 95 é a qualidade
            Destroy(screenShot);
            string fullPath = Path.Combine(captureFolder, fileName);
            try
            {
                File.WriteAllBytes(fullPath, bytes);
            }
            catch (IOException ex)
            {
                Debug.LogError($"Erro ao salvar o arquivo: {ex.Message}");
            }
        }

        private void SaveCameraImage(Camera cam, string timestamp)
        {
            if (cam == null)
            {
                return;
            }
            RenderTexture rtFull = RenderTexture.GetTemporary(cam.pixelWidth, cam.pixelHeight, 24);
            RenderTexture camTargetTex = cam.targetTexture;
            cam.targetTexture = rtFull;
            cam.Render();
            SaveJPG($"{timestamp}_capture_{cam.gameObject.name}.jpg", cam, rtFull, camTargetTex);
        }

        private void SaveJPG(string fileName, Camera cam, RenderTexture rtFull, RenderTexture camTargetTex)
        {
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
            try
            {
                File.WriteAllBytes(fullPath, bytes);
            }
            catch (IOException ex)
            {
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
