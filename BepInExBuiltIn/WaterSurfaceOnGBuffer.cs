using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace GBufferCapture
{

    [RequireComponent(typeof(Camera))]
    public class WaterGBufferInjector : MonoBehaviour
    {

        public CommandBuffer cb;
        public Material waterSurfaceMat;
        private Camera cam;

        void Awake()
        {
            cam = GetComponent<Camera>();

            Shader waterSurfaceShader = Utils.LoadExternalShader("WaterSurface");
            waterSurfaceMat = new Material(waterSurfaceShader);

            cb = new CommandBuffer();
            cb.name = "Water On GBuffer";
            cam.AddCommandBuffer(CameraEvent.BeforeGBuffer, cb);

            WaterSurfacePatcher.Initialize();
        }

        void OnDestroy()
        {
            if (cam != null && cb != null)
            {
                cam.RemoveCommandBuffer(CameraEvent.BeforeGBuffer, cb);
            }
            if (cb != null)
            {
                cb.Dispose();
            }
            if (waterSurfaceMat != null)
            {
                Destroy(waterSurfaceMat);
            }
        }

        void LateUpdate()
        {
            cb.Clear();
            Mesh waterMesh = WaterSurfacePatcher.LastFrameMesh;
            Matrix4x4[] waterMatrices = WaterSurfacePatcher.LastFrameMatrices;
            if (waterMesh == null || waterMatrices == null || waterMatrices.Length == 0)
            {
                return;
            }
            WaterSurface waterSurface = WaterSurface.Get();
            var normalsTex = (RenderTexture)AccessTools.Field(typeof(WaterSurface), "normalsTexture").GetValue(waterSurface);
            waterSurfaceMat.SetTexture("_WaterDisplacementMap", waterSurface.GetDisplacementTexture());
            waterSurfaceMat.SetTexture("_NormalsTex", normalsTex);
            waterSurfaceMat.SetFloat("_WaterPatchLength", waterSurface.GetPatchLength());
            for (int i = 0; i < waterMatrices.Length; i++)
            {
                cb.DrawMesh(waterMesh, waterMatrices[i], waterSurfaceMat, 0, 0);
            }
        }

    }
 
    [HarmonyPatch]
    public class WaterSurfacePatcher
    {

        public static Mesh LastFrameMesh { get; private set; }
        public static Matrix4x4[] LastFrameMatrices { get; private set; }

        private static FieldInfo patchMeshField;
        private static FieldInfo matricesQueueField;
        private static FieldInfo jobHandleField;
        private static bool isInitialized = false;

        public static void Initialize()
        {
            if (!isInitialized)
            {
                patchMeshField = AccessTools.Field(typeof(HeightFieldMesh), "patchMesh");
                matricesQueueField = AccessTools.Field(typeof(HeightFieldMesh), "matrices");
                jobHandleField = AccessTools.Field(typeof(HeightFieldMesh), "jobHandle");
                isInitialized = true;
            }
        }

        [HarmonyPatch(typeof(HeightFieldMesh), "FinalizeRender")]
        [HarmonyPrefix]
        public static void FinalizeRender_Prefix(HeightFieldMesh __instance)
        {
            if (!isInitialized)
            {
                return;
            }

            try
            {
                JobHandle jobHandle = (JobHandle) jobHandleField.GetValue(__instance);
                jobHandle.Complete();

                Mesh currentMesh = patchMeshField.GetValue(__instance) as Mesh;
                var waterMatricesQueue = (NativeQueue<float4x4>) matricesQueueField.GetValue(__instance);

                if (currentMesh == null || !waterMatricesQueue.IsCreated || waterMatricesQueue.Count == 0)
                {
                    LastFrameMesh = null;
                    LastFrameMatrices = null;
                    return;
                }

                LastFrameMesh = currentMesh;
                NativeArray<float4x4> matricesNativeArray = waterMatricesQueue.ToArray(Allocator.Temp);
                if (LastFrameMatrices == null || LastFrameMatrices.Length != matricesNativeArray.Length)
                {
                    LastFrameMatrices = new Matrix4x4[matricesNativeArray.Length];
                }
                for (int i = 0; i < matricesNativeArray.Length; i++)
                {
                    LastFrameMatrices[i] = matricesNativeArray[i];
                }

                matricesNativeArray.Dispose();
            } 
            catch 
            {
                Clear();
            }
        }

        [HarmonyPatch(typeof(HeightFieldMesh), "BeginRender")]
        class DisableCullingPatch
        {
            private static FieldInfo waterSurfaceHfmField;

            static bool Prepare()
            {
                waterSurfaceHfmField = AccessTools.Field(typeof(WaterSurface), "surfaceMesh");
                if (waterSurfaceHfmField == null)
                {
                    Debug.LogError("WaterSurfacePatcher: Não foi possível encontrar o campo 'hfm' em WaterSurface. O patch de culling será desativado.");
                    return false;
                }
                return true;
            }

            static void Prefix(HeightFieldMesh __instance, out bool __state)
            {
                __state = __instance.frustumCull;
                WaterSurface waterSurface = WaterSurface.Get();
                if (waterSurface == null) return;
                if (__instance == (HeightFieldMesh)waterSurfaceHfmField.GetValue(waterSurface))
                {
                    __state = __instance.frustumCull;
                    __instance.frustumCull = false;
                }
            }

            static void Postfix(HeightFieldMesh __instance, bool __state)
            {
                 __instance.frustumCull = __state;
            }
        }

        public static void Clear()
        {
            LastFrameMesh = null;
            LastFrameMatrices = null;
            patchMeshField = null;
            matricesQueueField = null;
            jobHandleField = null;
            isInitialized = false;
            GBufferCapturePlugin.instance.ClearCB();
        }

    }

}