using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static UnityEngine.GUI;

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

            clipRenderTextureContainerType = typeof(WaterSurface).GetNestedType("ClipRenderTextureContainer", BindingFlags.NonPublic | BindingFlags.Static);
            clipTextureField = AccessTools.Field(clipRenderTextureContainerType, "clipTexture");
            worldToClipMatrixField = AccessTools.Field(typeof(WaterSurface), "worldToClipMatrix");
            normalsTextureField = AccessTools.Field(typeof(WaterSurface), "normalsTexture");
        }

        void OnDestroy()
        {
            if (cam != null && cb != null)
            {
                cam.RemoveCommandBuffer(CameraEvent.AfterGBuffer, cb);
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
            UpdateGBuffer();
        }

        private static System.Type clipRenderTextureContainerType;
        private static FieldInfo clipTextureField;
        private static FieldInfo worldToClipMatrixField;
        private static FieldInfo normalsTextureField;

        private void UpdateGBuffer()
        {
            cb.Clear();
            Mesh waterMesh = WaterSurfacePatcher.LastFrameMesh;
            Matrix4x4[] waterMatrices = WaterSurfacePatcher.LastFrameMatrices;
            WaterSurface waterSurface = WaterSurface.Get();
            if (waterMesh == null || waterMatrices == null || waterMatrices.Length == 0 || waterSurface == null)
            {
                return;
            }
            if (clipTextureField == null || worldToClipMatrixField == null || normalsTextureField == null)
            {
                Debug.Log("some field is null");
                return;
            }
            Matrix4x4 worldToClipMatrix = (Matrix4x4)worldToClipMatrixField.GetValue(waterSurface);
            RenderTexture clipTexture = (RenderTexture)clipTextureField.GetValue(null);
            waterSurfaceMat.SetMatrix("_WorldToClipMatrix", worldToClipMatrix);
            waterSurfaceMat.SetTexture("_ClipTexture", clipTexture);
            RenderTexture normalsTex = (RenderTexture) normalsTextureField.GetValue(waterSurface);
            waterSurfaceMat.SetTexture("_WaterDisplacementMap", waterSurface.GetDisplacementTexture());
            waterSurfaceMat.SetTexture("_NormalsTex", normalsTex);
            waterSurfaceMat.SetFloat("_WaterPatchLength", waterSurface.GetPatchLength());
            for (int i = 0; i < waterMatrices.Length; i++)
            {
                cb.DrawMesh(waterMesh, waterMatrices[i], waterSurfaceMat, 0, 0);
            }
        }

        private float uvScale = 100.0f;
        private float displacementScale = 0.01f;
        private Texture2D displacementReadTexture;

        public bool IsCameraAboveWater()
        {
            float waterHeightAtCamera = cam.transform.position.y;
            Matrix4x4[] waterMatrices = WaterSurfacePatcher.LastFrameMatrices;
            if (waterMatrices == null || waterMatrices.Length == 0)
            {
                return true; 
            }
            WaterSurface waterSurface = WaterSurface.Get();
            if (waterSurface == null) return true;
            Texture displacementRT = waterSurface.GetDisplacementTexture();
            if (displacementRT == null) return true;
            float waterBaseHeight = waterMatrices[0].GetColumn(3).y;
            float patchLength = waterSurface.GetPatchLength();
            if (patchLength == 0) return true;
            Vector3 cameraPos = cam.transform.position;
            Vector2 uv = new Vector2(cameraPos.x, cameraPos.z) * uvScale / patchLength;
            uv.x = uv.x - Mathf.Floor(uv.x);
            uv.y = uv.y - Mathf.Floor(uv.y);
            Color displacementColor = ReadPixelFromRenderTexture(displacementRT, uv);
            float verticalDisplacement = (displacementColor.g - 0.5f) * 2.0f;
            float displacementY = displacementColor.g;
            float finalDisplacement = displacementY * displacementScale;
            waterHeightAtCamera = waterBaseHeight + finalDisplacement;
            return cameraPos.y >= waterHeightAtCamera;
        }

        private Color ReadPixelFromRenderTexture(Texture rt, Vector2 uv)
        {
            if (displacementReadTexture == null || displacementReadTexture.width != rt.width || displacementReadTexture.height != rt.height)
            {
                if (displacementReadTexture != null) Destroy(displacementReadTexture);
                displacementReadTexture = new Texture2D(rt.width, rt.height, rt.graphicsFormat, TextureCreationFlags.None);
            }
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = (RenderTexture) rt;
            displacementReadTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            displacementReadTexture.Apply();
            RenderTexture.active = active;
            return displacementReadTexture.GetPixelBilinear(uv.x, uv.y);
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