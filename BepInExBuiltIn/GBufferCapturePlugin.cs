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

            albedoCB = new CommandBuffer();
            albedoCB.name = "Capture Albedo";
            albedoCB.Blit(BuiltinRenderTextureType.GBuffer0, albedoRT);
            mainCam.AddCommandBuffer(CameraEvent.AfterEverything, albedoCB);
        }

        private CommandBuffer specularCB;
        private RenderTexture specularRT;

        private void SetupSpecular()
        {
            specularRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
            specularRT.Create();

            specularCB = new CommandBuffer();
            specularCB.name = "Capture Albedo";
            specularCB.Blit(BuiltinRenderTextureType.GBuffer1, specularRT);
            mainCam.AddCommandBuffer(CameraEvent.AfterEverything, specularCB);
        }

        private CommandBuffer emissionCB;
        private RenderTexture emissionRT;

        private void SetupEmission()
        {
            emissionRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
            emissionRT.Create();

            emissionCB = new CommandBuffer();
            emissionCB.name = "Capture Albedo";
            emissionCB.Blit(BuiltinRenderTextureType.GBuffer3, emissionRT);
            mainCam.AddCommandBuffer(CameraEvent.AfterEverything, emissionCB);
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

        private CommandBuffer jellyRayCB;
        private Material opaqueOverrideMaterial;
        private List<SkinnedMeshRenderer> jellyRayRenderers = new List<SkinnedMeshRenderer>();

        void SetupJellyRayOnGBuffers()
        {
            jellyRayRenderers.Clear();

            // Encontra todos os GameObjects que possuem o componente de script "Jellyray".
            // Nota: O nome do tipo aqui deve ser exatamente o mesmo que aparece no UnityExplorer.
            // Se o tipo estiver em um namespace, seria algo como Namespace.Jellyray.
            // Vamos assumir que está no escopo global por enquanto.
            Jellyray[] allJellyRayScripts = FindObjectsOfType<Jellyray>();
            Log.LogInfo($"Found {allJellyRayScripts.Length} Jellyray script instances.");

            foreach (var jellyRayScript in allJellyRayScripts)
            {
                // A partir do objeto que tem o script, procuramos por um SkinnedMeshRenderer nos filhos.
                // O "true" em GetComponentInChildren(true) garante que ele procure também em objetos filhos inativos.
                var renderer = jellyRayScript.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (renderer != null)
                {
                    // O nome do objeto que tem o renderer é "Jelly_Ray_01", como vimos nas imagens.
                    // Isso confirma que estamos no caminho certo.
                    Log.LogInfo($"Found SkinnedMeshRenderer on child object '{renderer.gameObject.name}' of a Jellyray.");
                    jellyRayRenderers.Add(renderer);
                }
                else
                {
                    Log.LogWarning($"A Jellyray instance was found, but it did not have a SkinnedMeshRenderer in its children.");
                }
            }

            // Se não criamos nosso material de override ainda, crie-o.
            if (opaqueOverrideMaterial == null)
            {
                // O shader "Standard" é uma aposta segura para um material opaco genérico.
                // Ele vai preencher os G-Buffers com informações de albedo, normais, etc.
                Shader standardShader = Shader.Find("Standard");
                if (standardShader != null)
                {
                    opaqueOverrideMaterial = new Material(standardShader);
                    // Podemos definir uma cor base cinza para o albedo, para não ser totalmente preto.
                    opaqueOverrideMaterial.color = Color.grey;
                }
                else
                {
                    Log.LogError("Could not find the 'Standard' shader to create override material.");
                    return;
                }
            }

            // Limpa o CommandBuffer antigo antes de adicionar um novo.
            if (jellyRayCB != null && mainCam != null)
            {
                // Tenta remover, mesmo que falhe, não há problema.
                try { mainCam.RemoveCommandBuffer(CameraEvent.BeforeGBuffer, jellyRayCB); } catch { }
            }

            jellyRayCB = new CommandBuffer { name = "JellyRay GBuffer Workaround" };

            // Define nossos G-Buffers como os alvos da renderização.
            // É crucial que os RTs já tenham sido criados aqui.
            var renderTargets = new RenderTargetIdentifier[] { albedoRT, normalRT /*, specularRT, etc */ };
            jellyRayCB.SetRenderTarget(renderTargets, depthRT.depthBuffer);

            // Itera sobre a lista de renderers que encontramos
            foreach (var renderer in jellyRayRenderers)
            {
                // Verifica se o renderer ainda existe e está visível pela câmera
                if (renderer != null && renderer.isVisible)
                {
                    // Adiciona um comando para desenhar este renderer específico usando nosso material opaco.
                    // O segundo argumento (submesh index) é 0 para malhas simples.
                    // O terceiro argumento é o shader pass. -1 usa todos os passes compatíveis.
                    jellyRayCB.DrawRenderer(renderer, opaqueOverrideMaterial, 0, -1);
                }
            }

            // Adiciona o CommandBuffer para ser executado ANTES da renderização dos G-Buffers.
            mainCam.AddCommandBuffer(CameraEvent.BeforeGBuffer, jellyRayCB);
            Log.LogInfo($"JellyRay GBuffer workaround CommandBuffer (re)created for {jellyRayRenderers.Count} renderers.");
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
            if (emissionCB != null)
            {
                GUI.DrawTexture(new Rect(1024, 0, 256, 256), emissionRT, ScaleMode.ScaleToFit, false);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                mainCam = FindObjectOfType<WaterSurfaceOnCamera>()?.gameObject.GetComponent<Camera>();
                SetupWaterSurfaceOnGBuffers();
                //SetupJellyRayOnGBuffers();
                SetupDepth();
                SetupNormal();
                SetupAlbedo();
                SetupSpecular();
                SetupEmission();
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
