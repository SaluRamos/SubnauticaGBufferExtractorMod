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

        private void SetupNormal()
        {
            normalRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
            normalRT.Create();

            normalCB = new CommandBuffer();
            normalCB.name = "Capture Normal";
            normalCB.Blit(BuiltinRenderTextureType.GBuffer2, normalRT);
            mainCam.AddCommandBuffer(CameraEvent.AfterGBuffer, normalCB);
        }

        private CommandBuffer albedoCB;
        private RenderTexture albedoRT;

        private void SetupAlbedo()
        {
            albedoRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
            albedoRT.Create();

            albedoCB = new CommandBuffer();
            albedoCB.name = "Capture Albedo";
            albedoCB.Blit(BuiltinRenderTextureType.GBuffer0, albedoRT);
            mainCam.AddCommandBuffer(CameraEvent.AfterGBuffer, albedoCB);
        }

        private CommandBuffer specularCB;
        private RenderTexture specularRT;

        private void SetupSpecular()
        {
            specularRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
            specularRT.Create();

            specularCB = new CommandBuffer();
            specularCB.name = "Capture Specular";
            specularCB.Blit(BuiltinRenderTextureType.GBuffer1, specularRT);
            mainCam.AddCommandBuffer(CameraEvent.AfterGBuffer, specularCB);
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
            if (depthCB != null)
            {
                GUI.DrawTexture(new Rect(0, 0, 256, 256), depthRT, ScaleMode.ScaleToFit, false);
            }
            if (normalCB != null)
            {
                GUI.DrawTexture(new Rect(256, 0, 256, 256), normalRT, ScaleMode.ScaleToFit, false);
            }
            if (albedoCB != null)
            {
                GUI.DrawTexture(new Rect(512, 0, 256, 256), albedoRT, ScaleMode.ScaleToFit, false);
            }
            if (specularCB != null)
            {
                GUI.DrawTexture(new Rect(768, 0, 256, 256), specularRT, ScaleMode.ScaleToFit, false);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F11))
            {
                mainCam = FindObjectOfType<WaterSurfaceOnCamera>()?.gameObject.GetComponent<Camera>();
                SetupDepth();
                SetupNormal();
                SetupAlbedo();
                SetupSpecular();
                SetupWaterSurfaceOnGBuffers();
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
                    //save as RT to captureFolder
                }
            }
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
