using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using UnityEditor;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GBufferCapture 
{

    public enum SavingFormat
    { 
        PNG,
        JPG
    }

    public enum EmissionBaseTex
    { 
        SPECULAR,
        ALBEDO
    }

    public enum FocusMode
    { 
        FINAL_RENDER,
        DEPTH_MAP,
        LOCAL_NORMAL_MAP,
        WORLD_NORMAL_MAP,
        ALBEDO_MAP,
        SPECULAR_MAP,
        AO_MAP,
        EMISSION_MAP
    }

    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class GBufferCapturePlugin : BaseUnityPlugin
    {

        public static GBufferCapturePlugin instance { get; private set; }
        public static string assetBundlePath = Paths.PluginPath + "\\GBufferCapture\\Shaders\\bundle";
        public static string captureFolder = Paths.PluginPath + "\\GBufferCapture\\captures";

        private const string MyGUID = "com.Salu.GBufferCapture";
        private const string PluginName = "GBufferCapture";
        private const string VersionString = "1.0.0";
        private static readonly Harmony harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        public static ConfigEntry<float> gbuffersMaxRenderDistanceEntry;
        public static ConfigEntry<float> gbufferUnderwaterDistanceClipEntry;

        public static ConfigEntry<bool> gbuffersPreviewEnabledEntry;
        public static ConfigEntry<int> gbuffersPreviewSizeEntry;
        public static ConfigEntry<FocusMode> focusModeEntry;

        public static ConfigEntry<float> captureIntervalEntry;
        public static ConfigEntry<int> captureWidthEntry;
        public static ConfigEntry<int> captureHeightEntry;
        public static ConfigEntry<SavingFormat> savingFormatEntry;
        public static ConfigEntry<int> jpgQualityEntry;

        public static ConfigEntry<bool> saveDepthEntry;
        public static ConfigEntry<bool> saveWorldNormalEntry;
        public static ConfigEntry<bool> saveLocalNormalEntry;
        public static ConfigEntry<bool> saveAlbedoEntry;
        public static ConfigEntry<bool> saveFinalRenderEntry;
        public static ConfigEntry<bool> saveSpecularEntry;
        public static ConfigEntry<bool> saveAOEntry;
        public static ConfigEntry<bool> saveNoLightEntry;

        public static ConfigEntry<bool> removeScubaMaskEntry;
        public static ConfigEntry<bool> removeBreathBubblesEntry;
        public static ConfigEntry<bool> removeWaterParticlesEntry;
        public static ConfigEntry<bool> neverShowDebugGUIEntry;

        private void Awake()
        {
            instance = this;
            Directory.CreateDirectory(captureFolder);
            gbuffersMaxRenderDistanceEntry = Config.Bind("Rendering", "gbuffersMaxRenderDistance", 1000.0f, "Max depth distance in gbuffer");
            gbufferUnderwaterDistanceClipEntry = Config.Bind("Rendering", "gbufferUnderwaterDistanceClip", 0.12f, new ConfigDescription("max distance underwater", new AcceptableValueRange<float>(0f, 1f)));

            gbuffersPreviewEnabledEntry = Config.Bind("Gui", "gbuffersPreviewEnabled", true, "toggle gbuffers captures GUI");
            gbuffersPreviewSizeEntry = Config.Bind("Gui", "gbuffersPreviewSize", 256, new ConfigDescription("width of gbuffers preview", new AcceptableValueRange<int>(100, 768)));
            focusModeEntry = Config.Bind("Gui", "focusMode", FocusMode.FINAL_RENDER, "changes camera render");

            captureIntervalEntry = Config.Bind("Capture", "CaptureInterval", 1.0f, "Set time between captures in seconds");
            captureWidthEntry = Config.Bind("Capture", "CaptureWidth", 960, "Resize capture width");
            captureHeightEntry = Config.Bind("Capture", "CaptureHeight", 540, "Resize capture height");
            savingFormatEntry = Config.Bind("Capture", "SavingFormat", SavingFormat.JPG, "Define saving format extension");
            jpgQualityEntry = Config.Bind("Capture", "JPG Quality", 95, "jpg quality");

            saveDepthEntry = Config.Bind("Capture", "saveDepthMap", true, "toggle saving depth map, only updates when restarting mod core");
            saveWorldNormalEntry = Config.Bind("Capture", "saveWorldNormalMap", true, "toggle saving world normal map, only updates when restarting mod core");
            saveLocalNormalEntry = Config.Bind("Capture", "saveLocalNormalMap", true, "toggle saving local normal map, only updates when restarting mod core");
            saveAlbedoEntry = Config.Bind("Capture", "saveAlbedoMap", true, "toggle saving albedo map, only updates when restarting mod core");
            saveFinalRenderEntry = Config.Bind("Capture", "saveFinalRender", true, "toggle saving final render, only updates when restarting mod core");
            saveSpecularEntry = Config.Bind("Capture", "saveSpecularMap", true, "toggle saving specular map, only updates when restarting mod core");
            saveAOEntry = Config.Bind("Capture", "saveAmbientOcclusionMap", true, "toggle saving ambient occlusion map, only updates when restarting mod core");
            saveNoLightEntry = Config.Bind("Capture", "saveNoLightMap", true, "toggle saving before lighting, only updates when restarting mod core");

            removeScubaMaskEntry = Config.Bind("Screen Cleaner", "removeScubaMask", true, "toggle remove scuba mask");
            removeBreathBubblesEntry = Config.Bind("Screen Cleaner", "removeBreathBubbles", true, "toggle breath bubbles");
            removeWaterParticlesEntry = Config.Bind("Screen Cleaner", "removeWaterParticles", true, "toggle water particles");
            neverShowDebugGUIEntry = Config.Bind("Screen Cleaner", "neverShowDebugGUI", true, "hides game debug GUI that appears when F1 is pressed");

            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            harmony.PatchAll();
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            Log = Logger;

            CountTakenCaptures();
        }

        private void CountTakenCaptures()
        {
            var files = Directory.GetFiles(captureFolder);
            var uniqueFiles = new HashSet<string>();
            foreach (var file in files)
            {
                var fileName = System.IO.Path.GetFileName(file);
                var parts = fileName.Split('_');
                if (parts.Length >= 2)
                {
                    var name = parts[0] + "_" + parts[1];
                    uniqueFiles.Add(name);
                }
            }
            totalCaptures = uniqueFiles.Count;
        }

        private Camera mainCam;
        private Camera gbufferCam; //this camera is useful because injected CustomWaterSurface messes with the final render

        private CommandBuffer cb;
        private CommandBuffer mainCB;
        private CommandBuffer blightCB;

        private RenderTexture mainRT;
        private RenderTexture depthRT;
        private RenderTexture worldNormalRT;
        private RenderTexture localNormalRT;
        private RenderTexture albedoRT;
        private RenderTexture specularRT;
        private RenderTexture aoRT;
        private RenderTexture beforeLightRT;

        private Shader texControlDepthShader;
        private Material mcdMaterial; //monocromatic control depth
        private Shader monocromaticControlDepthShader;
        private Material tcdMaterial; //texture control depth

        private Camera CreateNewCam(string name, Camera copyFrom)
        {
            GameObject newCamObj = new GameObject(name);
            newCamObj.transform.SetParent(copyFrom.transform.parent);
            newCamObj.transform.position = copyFrom.transform.position;
            newCamObj.transform.rotation = copyFrom.transform.rotation;
            Camera newCam = newCamObj.AddComponent<Camera>();
            newCam.CopyFrom(copyFrom);
            newCam.depth = copyFrom.depth - 1;
            return newCam;
        }

        private void SetupRTs()
        {
            if (mainRT == null)
            {
                mainRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 24, RenderTextureFormat.ARGB32);
                mainRT.Create();
                depthRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
                depthRT.Create();
                worldNormalRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
                worldNormalRT.Create();
                localNormalRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
                localNormalRT.Create();
                albedoRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
                albedoRT.Create();
                specularRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
                specularRT.Create();
                aoRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
                aoRT.Create();
                beforeLightRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
                beforeLightRT.Create();
            }
        }

        private void SetupMaterials()
        {
            Utils.UnloadAssetBundle();

            monocromaticControlDepthShader = Utils.LoadExternalShader("MonocromaticFogController");
            mcdMaterial = new Material(monocromaticControlDepthShader);
            mcdMaterial.hideFlags = HideFlags.HideAndDontSave;

            texControlDepthShader = Utils.LoadExternalShader("TextureFogController");
            tcdMaterial = new Material(texControlDepthShader);
            tcdMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        private void SetupLocalNormalMap()
        {
            UwePostProcessingManager postProcessingManager = mainCam.GetComponent<UwePostProcessingManager>();
            PostProcessingBehaviour behaviour = postProcessingManager.behaviour;
            BuiltinDebugViewsComponent m_DebugViews = (BuiltinDebugViewsComponent)AccessTools.Field(typeof(PostProcessingBehaviour), "m_DebugViews").GetValue(behaviour);

            CommandBuffer localNormalCB = new CommandBuffer();
            localNormalCB.name = "NormalMapCB";

            Material normalMapMaterial = m_DebugViews.context.materialFactory.Get("Hidden/Post FX/Builtin Debug Views");
            normalMapMaterial.shaderKeywords = null;
            if (m_DebugViews.context.isGBufferAvailable)
            {
                normalMapMaterial.EnableKeyword("SOURCE_GBUFFER");
            }

            localNormalCB.Blit(null, localNormalRT, normalMapMaterial, 1);
            int localNormalTempRT = Shader.PropertyToID("_LocalNormalTempRT");
            localNormalCB.GetTemporaryRT(localNormalTempRT, localNormalRT.width, localNormalRT.height, 0, FilterMode.Bilinear, localNormalRT.format);
            localNormalCB.Blit(localNormalRT, localNormalTempRT, tcdMaterial);
            localNormalCB.Blit(localNormalTempRT, localNormalRT);
            localNormalCB.ReleaseTemporaryRT(localNormalTempRT);
            gbufferCam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, localNormalCB);
        }

        private void SetupAmbientOcclusion()
        {
            UwePostProcessingManager postProcessingManager = mainCam.GetComponent<UwePostProcessingManager>();
            PostProcessingBehaviour behaviour = postProcessingManager.behaviour;
            AmbientOcclusionComponent m_AmbientOcclusion = (AmbientOcclusionComponent)AccessTools.Field(typeof(PostProcessingBehaviour), "m_AmbientOcclusion").GetValue(behaviour);

            CommandBuffer aoCB = new CommandBuffer();
            aoCB.name = "Ambient Occlusion CB";

            AmbientOcclusionModel.Settings aoSettings = m_AmbientOcclusion.model.settings;
            Material aoMaterial1 = m_AmbientOcclusion.context.materialFactory.Get("Hidden/Post FX/Blit");
            Material aoMaterial2 = m_AmbientOcclusion.context.materialFactory.Get("Hidden/Post FX/Ambient Occlusion");
            aoMaterial2.shaderKeywords = null;
            aoMaterial2.SetFloat(Shader.PropertyToID("_Intensity"), aoSettings.intensity);
            aoMaterial2.SetFloat(Shader.PropertyToID("_Radius"), aoSettings.radius);
            aoMaterial2.SetFloat(Shader.PropertyToID("_Downsample"), aoSettings.downsampling ? 0.5f : 1f);
            aoMaterial2.SetInt(Shader.PropertyToID("_SampleCount"), (int)aoSettings.sampleCount);
            if (!m_AmbientOcclusion.context.isGBufferAvailable && RenderSettings.fog)
            {
                aoMaterial2.SetVector(Shader.PropertyToID("_FogParams"), new UnityEngine.Vector3(RenderSettings.fogDensity, RenderSettings.fogStartDistance, RenderSettings.fogEndDistance));
                switch (RenderSettings.fogMode)
                {
                    case FogMode.Linear:
                        aoMaterial2.EnableKeyword("FOG_LINEAR");
                        break;
                    case FogMode.Exponential:
                        aoMaterial2.EnableKeyword("FOG_EXP");
                        break;
                    case FogMode.ExponentialSquared:
                        aoMaterial2.EnableKeyword("FOG_EXP2");
                        break;
                }
            }
            else
            {
                aoMaterial2.EnableKeyword("FOG_OFF");
            }
            int width = m_AmbientOcclusion.context.width;
            int height = m_AmbientOcclusion.context.height;
            int num = ((!aoSettings.downsampling) ? 1 : 2);
            bool flag = DynamicResolution.IsEnabled();
            int occlusionTexture = Shader.PropertyToID("_OcclusionTexture1");
            if (flag)
            {
                aoCB.GetTemporaryRT(occlusionTexture, width / num, height / num, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, enableRandomWrite: false, RenderTextureMemoryless.None, useDynamicScale: true);
            }
            else
            {
                aoCB.GetTemporaryRT(occlusionTexture, width / num, height / num, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            }
            int occlusionSource = (int)AccessTools.Property(typeof(AmbientOcclusionComponent), "occlusionSource").GetValue(m_AmbientOcclusion, null);
            aoCB.Blit(null, occlusionTexture, aoMaterial2, (int)occlusionSource);
            int occlusionTexture2 = Shader.PropertyToID("_OcclusionTexture2");
            if (flag)
            {
                aoCB.GetTemporaryRT(occlusionTexture2, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, enableRandomWrite: false, RenderTextureMemoryless.None, useDynamicScale: true);
            }
            else
            {
                aoCB.GetTemporaryRT(occlusionTexture2, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            }
            aoCB.SetGlobalTexture(Shader.PropertyToID("_MainTex"), occlusionTexture);
            aoCB.Blit(occlusionTexture, occlusionTexture2, aoMaterial2, (occlusionSource == 2) ? 4 : 3);
            aoCB.ReleaseTemporaryRT(occlusionTexture);
            occlusionTexture = Shader.PropertyToID("_OcclusionTexture");
            if (flag)
            {
                aoCB.GetTemporaryRT(occlusionTexture, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, enableRandomWrite: false, RenderTextureMemoryless.None, useDynamicScale: true);
            }
            else
            {
                aoCB.GetTemporaryRT(occlusionTexture, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            }
            aoCB.SetGlobalTexture(Shader.PropertyToID("_MainTex"), occlusionTexture2);
            aoCB.Blit(occlusionTexture2, occlusionTexture, aoMaterial2, 5);
            aoCB.ReleaseTemporaryRT(occlusionTexture2);
            bool ambientOnlySupported = (bool)AccessTools.Property(typeof(AmbientOcclusionComponent), "ambientOnlySupported").GetValue(m_AmbientOcclusion, null);
            RenderTargetIdentifier[] m_MRT = AccessTools.Field(typeof(AmbientOcclusionComponent), "m_MRT").GetValue(m_AmbientOcclusion) as RenderTargetIdentifier[];
            aoCB.SetGlobalTexture(Shader.PropertyToID("_MainTex"), occlusionTexture);
            aoCB.Blit(occlusionTexture, aoRT, aoMaterial2, 8);
            aoCB.ReleaseTemporaryRT(occlusionTexture);
            int tempRT = Shader.PropertyToID("_TempRT");
            aoCB.GetTemporaryRT(tempRT, aoRT.descriptor);
            aoCB.Blit(aoRT, tempRT, tcdMaterial);
            aoCB.Blit(tempRT, aoRT);
            aoCB.ReleaseTemporaryRT(tempRT);
            gbufferCam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, aoCB);
        }

        private void SetupCB()
        {
            Debug.LogWarning("mod core starting");
            SetupRTs();
            SetupMaterials();

            gbufferCam.depthTextureMode = DepthTextureMode.Depth;
            mainCam.depthTextureMode = DepthTextureMode.Depth;

            if (saveLocalNormalEntry.Value)
            {
                SetupLocalNormalMap();
            }
            if (saveAOEntry.Value)
            { 
                SetupAmbientOcclusion();
            }

            cb = new CommandBuffer();
            if (saveDepthEntry.Value)
            { 
                cb.Blit(BuiltinRenderTextureType.CameraTarget, depthRT, mcdMaterial);
            }
            if (saveWorldNormalEntry.Value)
            { 
                cb.Blit(BuiltinRenderTextureType.GBuffer2, worldNormalRT, tcdMaterial);
            }
            if (saveAlbedoEntry.Value)
            { 
                cb.Blit(BuiltinRenderTextureType.GBuffer0, albedoRT, tcdMaterial);
            }
            if (saveSpecularEntry.Value)
            { 
                cb.Blit(BuiltinRenderTextureType.GBuffer1, specularRT, tcdMaterial);
            }
            gbufferCam.AddCommandBuffer(CameraEvent.AfterEverything, cb);

            if (saveNoLightEntry.Value)
            { 
                blightCB = new CommandBuffer();
                blightCB.Blit(BuiltinRenderTextureType.CameraTarget, beforeLightRT, tcdMaterial);
                mainCam.AddCommandBuffer(CameraEvent.BeforeLighting, blightCB);
            }

            if (saveFinalRenderEntry.Value)
            { 
                mainCB = new CommandBuffer();
                mainCB.Blit(BuiltinRenderTextureType.CameraTarget, mainRT);
                mainCam.AddCommandBuffer(CameraEvent.AfterEverything, mainCB);
            }

        }

        public void ClearCB()
        {
            Debug.LogWarning("mod core stopping");
            if (mainCam != null && mainCB != null)
            {
                mainCam.RemoveCommandBuffer(CameraEvent.AfterEverything, mainCB);
            }
            mainCam = null;

            if (gbufferCam != null && cb != null)
            {
                gbufferCam.RemoveCommandBuffer(CameraEvent.AfterEverything, cb);
                GameObject.DestroyImmediate(gbufferCam.gameObject);
            }
            gbufferCam = null;

            cb?.Release();
            cb = null;

            mainCB?.Release();
            mainCB = null;

            blightCB?.Release();
            blightCB = null;

            Utils.ToggleParts(!removeScubaMaskEntry.Value, !removeBreathBubblesEntry.Value, !removeWaterParticlesEntry.Value);

            if (Player.main != null)
            { 
                Destroy(Player.main.gameObject.GetComponent<BaseOnEmission>());
            }

            isCapturing = false;
            modCoreEnabled = false;
        }

        private GUIStyle labelStyle;

        void OnGUI()
        {
            switch (focusModeEntry.Value)
            { 
                case FocusMode.DEPTH_MAP:
                    GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), depthRT, ScaleMode.StretchToFill, false);
                    break;
                case FocusMode.LOCAL_NORMAL_MAP:
                    GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), localNormalRT, ScaleMode.StretchToFill, false);
                    break;
                case FocusMode.WORLD_NORMAL_MAP:
                    GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), worldNormalRT, ScaleMode.StretchToFill, false);
                    break;
                case FocusMode.ALBEDO_MAP:
                    GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), albedoRT, ScaleMode.StretchToFill, false);
                    break;
                case FocusMode.SPECULAR_MAP:
                    GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), specularRT, ScaleMode.StretchToFill, false);
                    break;
                case FocusMode.AO_MAP:
                    GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), aoRT, ScaleMode.StretchToFill, false);
                    break;
                case FocusMode.EMISSION_MAP:
                    GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), beforeLightRT, ScaleMode.StretchToFill, false);
                    break;
                default:
                    break;
            }
            if (cb != null && gbuffersPreviewEnabledEntry.Value && gbuffersPreviewSizeEntry.Value > 0)
            {
                int stackPos = 0;
                int previewWidth = gbuffersPreviewSizeEntry.Value;
                int previewHeight = (int)Math.Ceiling(gbuffersPreviewSizeEntry.Value * (9.0f / 16.0f));
                if (saveDepthEntry.Value)
                {
                    GUI.DrawTexture(new Rect(0, previewHeight * stackPos, previewWidth, previewHeight), depthRT, ScaleMode.StretchToFill, false);
                    stackPos++;
                }
                if (saveWorldNormalEntry.Value)
                {
                    GUI.DrawTexture(new Rect(0, previewHeight * stackPos, previewWidth, previewHeight), worldNormalRT, ScaleMode.StretchToFill, false);
                    stackPos++;
                }
                if (saveLocalNormalEntry.Value)
                {
                    GUI.DrawTexture(new Rect(0, previewHeight * stackPos, previewWidth, previewHeight), localNormalRT, ScaleMode.StretchToFill, false);
                    stackPos++;
                }
                if (saveAlbedoEntry.Value)
                {
                    GUI.DrawTexture(new Rect(0, previewHeight * stackPos, previewWidth, previewHeight), albedoRT, ScaleMode.StretchToFill, false);
                    stackPos++;
                }
                if (saveSpecularEntry.Value)
                {
                    GUI.DrawTexture(new Rect(0, previewHeight * stackPos, previewWidth, previewHeight), specularRT, ScaleMode.StretchToFill, false);
                    stackPos++;
                }
                if (saveAOEntry.Value)
                {
                    GUI.DrawTexture(new Rect(0, previewHeight * stackPos, previewWidth, previewHeight), aoRT, ScaleMode.StretchToFill, false);
                    stackPos++;
                }
                if (saveNoLightEntry.Value)
                {
                    GUI.DrawTexture(new Rect(0, previewHeight * stackPos, previewWidth, previewHeight), beforeLightRT, ScaleMode.StretchToFill, false);
                    stackPos++;
                }
            }
            string labelText = $"Mod Core {(modCoreEnabled ? "Enabled" : "Disabled")}\nCapture {(isCapturing ? "Enabled" : "Disabled")}\nTotal Captures: {totalCaptures}\nCapture Interval: {captureIntervalEntry.Value}s";
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.normal.textColor = Color.white;
                labelStyle.fontSize = 20;
                labelStyle.fontStyle = FontStyle.Bold;
            }
            GUI.Label(new Rect(10, 950, 300, 200), labelText, labelStyle);
        }

        void LateUpdate()
        {
            if (cb != null && waterGBufferInjector != null)
            {
                UnityEngine.Matrix4x4 worldToCameraMatrix = gbufferCam.worldToCameraMatrix;
                GameObject waterPlane = FindObjectOfType<WaterscapeVolume>().waterPlane;
                Transform transform = waterPlane.transform;
                UnityEngine.Plane plane = new UnityEngine.Plane(transform.up, transform.position);
                UnityEngine.Plane plane2 = worldToCameraMatrix.TransformPlane(plane);
                UnityEngine.Vector3 normal = plane2.normal;
                Shader.SetGlobalVector(ShaderPropertyID._UweVsWaterPlane, new UnityEngine.Vector4(normal.x, normal.y, normal.z, plane2.distance));

                cb.SetGlobalFloat("_DepthMaxDistance", gbuffersMaxRenderDistanceEntry.Value);
                cb.SetGlobalFloat("_DepthCutoffBelowWater", gbufferUnderwaterDistanceClipEntry.Value);
            }
            if ((mainCam == null || gbufferCam == null) && modCoreEnabled)
            {
                ClearCB();
            }
            if (neverShowDebugGUIEntry.Value)
            {
                TerrainDebugGUI[] array = UnityEngine.Object.FindObjectsOfType<TerrainDebugGUI>();
                foreach (TerrainDebugGUI obj in array)
                {
                    obj.enabled = false;
                }
            }
        }

        private static int totalCaptures = 0;
        private bool isCapturing = false;
        private float timer = 0f;
        private static bool modCoreEnabled = false;
        WaterGBufferInjector waterGBufferInjector;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F11))
            {
                if (modCoreEnabled)
                {
                    ClearCB();
                }
                else
                { 
                    mainCam = FindObjectOfType<WaterSurfaceOnCamera>()?.gameObject.GetComponent<Camera>();
                    if (mainCam != null)
                    {
                        Utils.ToggleParts(!removeScubaMaskEntry.Value, !removeBreathBubblesEntry.Value, !removeWaterParticlesEntry.Value);
                        gbufferCam = CreateNewCam("gBufferCam", mainCam);
                        waterGBufferInjector = gbufferCam.gameObject.AddComponent<WaterGBufferInjector>();
                        SetupCB();
                        gbufferCam.gameObject.AddComponent<DayNightPatch>();
                        if (Player.main != null)
                        { 
                            Player.main.gameObject.AddComponent<BaseOnEmission>();
                        }
                        modCoreEnabled = true;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.F10) && cb != null)
            {
                isCapturing = !isCapturing;
            }


            if (Input.GetKeyDown(KeyCode.F9))
            {
                SaveCaptures();
            }

            if (isCapturing && cb != null)
            {
                timer += Time.deltaTime;
                if (timer >= captureIntervalEntry.Value)
                {
                    timer = 0f;
                    SaveCaptures();
                }
            }
        }

        void SaveCaptures()
        {
            string timestamp = $"{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}";
            Action<string, RenderTexture, int, int> saveFunc;
            switch (savingFormatEntry.Value)
            {
                case SavingFormat.PNG:
                    saveFunc = (fileName, rt, w, h) => Utils.SaveTexture(fileName, rt, w, h, ".png", t => t.EncodeToPNG());
                    break;
                case SavingFormat.JPG:
                    saveFunc = (fileName, rt, w, h) => Utils.SaveTexture(fileName, rt, w, h, ".jpg", t => t.EncodeToJPG(jpgQualityEntry.Value));
                    break;
                default:
                    throw new NotSupportedException($"Unsupported saving type: {savingFormatEntry.Value}");
            }
            int captureWidth = captureWidthEntry.Value;
            int captureHeight = captureHeightEntry.Value;
            if (saveDepthEntry.Value) saveFunc($"{timestamp}_depth", depthRT, captureWidth, captureHeight);
            if (saveWorldNormalEntry.Value) saveFunc($"{timestamp}_world_normal", worldNormalRT, captureWidth, captureHeight);
            if (saveLocalNormalEntry.Value) saveFunc($"{timestamp}_local_normal", localNormalRT, captureWidth, captureHeight);
            if (saveAlbedoEntry.Value) saveFunc($"{timestamp}_albedo", albedoRT, captureWidth, captureHeight);
            if (saveFinalRenderEntry.Value) saveFunc($"{timestamp}_base", mainRT, captureWidth, captureHeight);
            if (saveSpecularEntry.Value) saveFunc($"{timestamp}_specular", specularRT, captureWidth, captureHeight);
            if (saveAOEntry.Value) saveFunc($"{timestamp}_ao", aoRT, captureWidth, captureHeight);
            if (saveNoLightEntry.Value) saveFunc($"{timestamp}_no_light", beforeLightRT, captureWidth, captureHeight);
            totalCaptures++;
        }
    
    }
}
