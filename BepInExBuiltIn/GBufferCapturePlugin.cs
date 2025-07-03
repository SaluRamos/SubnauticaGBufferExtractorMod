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
using static RadicalLibrary.Spline;

namespace GBufferCapture 
{

    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class GBufferCapturePlugin : BaseUnityPlugin
    {

        public static GBufferCapturePlugin instance { get; private set; }

        public static string assetBundlePath = Paths.PluginPath + "\\GBufferCapture\\Shaders\\bundle";
        public static string captureFolder = Paths.PluginPath + "\\GBufferCapture\\captures";

        private bool isCapturing = false;
        private float timer = 0f;
        private float captureInterval => captureThreadSleep.Value; // 1 second

        private Camera mainCam;
        private Camera gbufferCam;

        private const string MyGUID = "com.Salu.GBufferCapture";
        private const string PluginName = "GBufferCapture";
        private const string VersionString = "1.0.0";
        private static readonly Harmony harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        public static ConfigEntry<float> captureThreadSleep;
        public static ConfigEntry<float> gbuffersMaxRenderDistanceEntry;
        public static ConfigEntry<float> depthControlWaterLevelToleranceEntry;
        public static ConfigEntry<bool> seeOnGUIEntry;
        public static ConfigEntry<bool> fogEntry;

        private void Awake()
        {
            instance = this;
            Directory.CreateDirectory(captureFolder);
            captureThreadSleep = Config.Bind("General", "CaptureThreadSleep", 1.0f, new ConfigDescription("Set time between captures in seconds", new AcceptableValueRange<float>(0.0f, 10.0f)));
            gbuffersMaxRenderDistanceEntry = Config.Bind("General", "GBufferMaxRenderDistanceUnderwater", 120.0f, "Max saw distance by gbuffers underwater, upperwater default is 1000.0f");
            depthControlWaterLevelToleranceEntry = Config.Bind("General", "DepthControlWaterLevelTolerance", 100.0f, "the mod shaders converts depthmap to worldPos and may fail when you move camera too fast (doesnt know why exactly), increase this value to reduce/remove this errors effect in captured images");
            seeOnGUIEntry = Config.Bind("General", "seeOnGUI", true, "enable/disable mod OnGUI");
            fogEntry = Config.Bind("General", "Fog", true, "enable/disable fog without affecting captures");

            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            harmony.PatchAll();
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            Log = Logger;
        }

        public static float gbuffersMaxRenderDistance => gbuffersMaxRenderDistanceEntry.Value;

        private CommandBuffer cb;
        private RenderTexture mainCamTargetTextureRT;
        private RenderTexture mainRT;
        private RenderTexture depthRT;
        private RenderTexture normalRT;
        private RenderTexture albedoRT;
        private RenderTexture emissionRT;
        private RenderTexture idRT;

        private Shader midShader;
        private Material midMaterial;
        private Shader texControlDepthShader;
        private Material mcdMaterial; //monocromatic control depth
        private Shader monocromaticControlDepthShader;
        private Material tcdMaterial; //texture control depth
        private Shader emissionShader;
        private Material emissionMat;

        public static bool lastFog;


        private void SetupCB()
        {
            GameObject gbufferCamObj = new GameObject("GBufferCam");
            gbufferCamObj.transform.SetParent(mainCam.transform.parent);
            gbufferCamObj.transform.position = mainCam.transform.position;
            gbufferCamObj.transform.rotation = mainCam.transform.rotation;
            gbufferCam = gbufferCamObj.AddComponent<Camera>();
            gbufferCam.CopyFrom(mainCam);
            gbufferCam.depth = mainCam.depth - 1;

            mainRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 24, RenderTextureFormat.ARGB32);
            mainRT.Create();
            depthRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
            depthRT.Create();
            normalRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
            normalRT.Create();
            albedoRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
            albedoRT.Create();
            emissionRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGB32);
            emissionRT.Create();
            idRT = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGB32);
            idRT.Create();

            monocromaticControlDepthShader = Utils.LoadExternalShader("DepthPost");
            mcdMaterial = new Material(monocromaticControlDepthShader);
            mcdMaterial.hideFlags = HideFlags.HideAndDontSave;

            texControlDepthShader = Utils.LoadExternalShader("NormalPost");
            tcdMaterial = new Material(texControlDepthShader);
            tcdMaterial.hideFlags = HideFlags.HideAndDontSave;

            emissionShader = Utils.LoadExternalShader("EmissionMap");
            emissionMat = new Material(emissionShader);
            emissionMat.hideFlags = HideFlags.HideAndDontSave;

            midShader = Utils.LoadExternalShader("MaterialID");
            midMaterial = new Material(midShader);
            midMaterial.hideFlags = HideFlags.HideAndDontSave;

            gbufferCam.depthTextureMode = DepthTextureMode.Depth;

            cb = new CommandBuffer();
            cb.name = "GBuffer Capture Command Buffer";

            //código base para shaderID e emissionMap
            //var renderers = FindObjectsOfType<Renderer>();

            //cb.SetRenderTarget(idRT);
            //cb.ClearRenderTarget(true, true, Color.black);
            //var props = new MaterialPropertyBlock();
            //foreach (var rend in renderers)
            //{
            //    if (rend.sharedMaterial == null)
            //    { 
            //        Debug.Log($"pulando renderer {rend}");
            //        continue;
            //    }
            //    if (rend is ParticleSystemRenderer)
            //    { 
            //        continue;
            //    }
            //    props.Clear();
            //    int matID = rend.sharedMaterial.GetInstanceID();
            //    props.SetFloat("_MaterialID", matID);
            //    rend.SetPropertyBlock(props);
            //    cb.DrawRenderer(rend, midMaterial);
            //}

            //cb.SetRenderTarget(emissionRT);
            //cb.ClearRenderTarget(true, true, Color.black);
            //foreach (var rend in renderers)
            //{
                // uma das idéias para capturar o emissionMap é obter todos os gameObjects que contem Light e iterar sobre objetos parentes com mesh, pois estes serão a fonte de luz, a esses objetos se aplica o material
            //    if (rend.sharedMaterial == null)
            //    {
            //        Debug.Log($"pulando renderer {rend}");
            //        continue;
            //    }
            //    if (rend is ParticleSystemRenderer)
            //    {
            //        continue;
            //    }
            //    var mat = rend.sharedMaterial;
            //    if (!mat.HasProperty("_EmissionMap"))
            //    { 
            //        cb.DrawRenderer(rend, emissionMat);
            //    }
            //}

            cb.Blit(BuiltinRenderTextureType.CameraTarget, depthRT, mcdMaterial);
            cb.Blit(BuiltinRenderTextureType.GBuffer2, normalRT, tcdMaterial);
            cb.Blit(BuiltinRenderTextureType.GBuffer0, albedoRT, tcdMaterial);
            gbufferCam.AddCommandBuffer(CameraEvent.AfterEverything, cb);
        }

        private WaterGBufferInjector injectorInstance;

        void SetupWaterSurfaceOnGBuffers()
        {
            if (injectorInstance != null)
            {
                return;
            }
            injectorInstance = gbufferCam.gameObject.AddComponent<WaterGBufferInjector>();
        }

        void OnGUI()
        {
            if (cb != null && seeOnGUIEntry.Value)
            {
                GUI.DrawTexture(new Rect(0, 0, 256, 144), depthRT, ScaleMode.StretchToFill, false);
                GUI.DrawTexture(new Rect(0, 144, 256, 144), normalRT, ScaleMode.StretchToFill, false);
                GUI.DrawTexture(new Rect(0, 288, 256, 144), albedoRT, ScaleMode.StretchToFill, false);
                if (!fogEntry.Value)
                { 
                    GUI.DrawTexture(new Rect(0, 432, 256, 144), mainRT, ScaleMode.StretchToFill, false);
                }
            }
        }

        private float depthControlWaterLevel => depthControlWaterLevelToleranceEntry.Value;

        void LateUpdate()
        {
            if (cb != null)
            {
                //isso seria usado no autodepth shader
                //cb.SetGlobalMatrix("_CameraInvProj", mainCam.projectionMatrix.inverse);
                //Matrix4x4 worldToCameraMatrix = mainCam.worldToCameraMatrix;
                //Transform transform = FindObjectOfType<WaterscapeVolume>().waterPlane.transform;
                //Plane plane = new Plane(transform.up, transform.position);
                //Plane plane2 = worldToCameraMatrix.TransformPlane(plane);
                //Vector3 normal = plane2.normal;
                //cb.SetGlobalVector("_UweVsWaterPlane", new Vector4(normal.x, normal.y, normal.z, plane2.distance));

                cb.SetGlobalMatrix("_CameraProj", mainCam.projectionMatrix);
                cb.SetGlobalMatrix("CameraToWorld", mainCam.cameraToWorldMatrix);
                cb.SetGlobalFloat("_DepthCutoff", gbuffersMaxRenderDistance);
                if (UnderWaterListener_Patch.IsUnderWater())
                {
                    cb.SetGlobalFloat("_WaterLevel", depthControlWaterLevel);
                }
                else
                {
                    cb.SetGlobalFloat("_WaterLevel", -depthControlWaterLevel);
                }
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F11))
            {
                mainCam = FindObjectOfType<WaterSurfaceOnCamera>()?.gameObject.GetComponent<Camera>();
                mainCamTargetTextureRT = mainCam.targetTexture;
                SetupCB();
                SetupWaterSurfaceOnGBuffers();
            }

            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                Utils.DumpShaders();
            }

            if (lastFog != fogEntry.Value && cb != null)
            {
                if (!fogEntry.Value)
                {
                    mainCam.targetTexture = mainRT;
                }
                else
                {
                    mainCam.targetTexture = mainCamTargetTextureRT;
                }
                lastFog = fogEntry.Value;
            }
            
            if (cb == null)
            { 
                lastFog = !fogEntry.Value;
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                isCapturing = !isCapturing;
                Debug.Log($"G-Buffer capture {(isCapturing ? "started" : "stopped")}");
            }

            if (isCapturing && cb != null)
            {
                timer += Time.deltaTime;
                if (timer >= captureInterval)
                {
                    timer = 0f;
                    string timestamp = $"{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}";
                    if (fogEntry.Value)
                    {
                        mainCam.targetTexture = mainRT;
                        mainCam.Render();
                        mainCam.targetTexture = mainCamTargetTextureRT;
                    }
                    Utils.SaveJPG($"{timestamp}_base.png", mainRT);
                    Utils.SaveJPG($"{timestamp}_depth.png", depthRT);
                    Utils.SaveJPG($"{timestamp}_normal.png", normalRT);
                    Utils.SaveJPG($"{timestamp}_albedo.png", albedoRT);
                }
            }
        }

    }
}
