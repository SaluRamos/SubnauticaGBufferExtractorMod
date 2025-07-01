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

        public static GBufferCapturePlugin instance { get; private set; }

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
            instance = this;
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

        private float gbuffersMaxRenderDistance = 130f;

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
            depthMaterial.SetFloat("_DepthCutoff", gbuffersMaxRenderDistance);

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
            normalMaterial.SetFloat("_DepthCutoff", gbuffersMaxRenderDistance);

            normalCB = new CommandBuffer();
            normalCB.name = "Capture Normal";
            normalCB.Blit(BuiltinRenderTextureType.GBuffer2, normalRT, normalMaterial);
            mainCam.AddCommandBuffer(CameraEvent.AfterEverything, normalCB);
        }

        private CommandBuffer albedoCB;
        private RenderTexture albedoRT;
        private Shader albedoShader;
        private Material albedoMaterial;

        private void SetupAlbedo()
        {
            albedoRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
            albedoRT.Create();

            albedoShader = LoadExternalShader(assetBundlePath, "NormalPost");
            albedoMaterial = new Material(albedoShader);
            albedoMaterial.hideFlags = HideFlags.HideAndDontSave;
            albedoMaterial.SetFloat("_DepthCutoff", gbuffersMaxRenderDistance);

            albedoCB = new CommandBuffer();
            albedoCB.name = "Capture Albedo";
            albedoCB.Blit(BuiltinRenderTextureType.GBuffer0, albedoRT, albedoMaterial);
            mainCam.AddCommandBuffer(CameraEvent.AfterEverything, albedoCB);
        }

        private CommandBuffer specularCB;
        private RenderTexture specularRT;
        private Shader specularShader;
        private Material specularMaterial;

        private void SetupSpecular()
        {
            specularRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
            specularRT.Create();

            specularShader = LoadExternalShader(assetBundlePath, "NormalPost");
            specularMaterial = new Material(specularShader);
            specularMaterial.hideFlags = HideFlags.HideAndDontSave;
            specularMaterial.SetFloat("_DepthCutoff", gbuffersMaxRenderDistance);

            specularCB = new CommandBuffer();
            specularCB.name = "Capture Specular";
            specularCB.Blit(BuiltinRenderTextureType.GBuffer1, specularRT, specularMaterial);
            mainCam.AddCommandBuffer(CameraEvent.AfterEverything, specularCB);
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

            if (depthCB != null)
            {
                depthCB.SetGlobalMatrix("_CameraProj", mainCam.projectionMatrix);
                depthCB.SetGlobalMatrix("CameraToWorld", mainCam.cameraToWorldMatrix);
                depthCB.SetGlobalFloat("_DepthCutoff", gbuffersMaxRenderDistance);

                normalCB.SetGlobalMatrix("_CameraProj", mainCam.projectionMatrix);
                normalCB.SetGlobalMatrix("CameraToWorld", mainCam.cameraToWorldMatrix);
                normalCB.SetGlobalFloat("_DepthCutoff", gbuffersMaxRenderDistance);

                specularCB.SetGlobalMatrix("_CameraProj", mainCam.projectionMatrix);
                specularCB.SetGlobalMatrix("CameraToWorld", mainCam.cameraToWorldMatrix);
                specularCB.SetGlobalFloat("_DepthCutoff", gbuffersMaxRenderDistance);

                albedoCB.SetGlobalMatrix("_CameraProj", mainCam.projectionMatrix);
                albedoCB.SetGlobalMatrix("CameraToWorld", mainCam.cameraToWorldMatrix);
                albedoCB.SetGlobalFloat("_DepthCutoff", gbuffersMaxRenderDistance);
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
                    //save RT's to captureFolder
                    //não é prioridade
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
