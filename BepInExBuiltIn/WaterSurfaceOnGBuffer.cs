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
        private CommandBuffer cb;
        private Material waterSurfaceMat;
        private Camera cam;
        public WaterSurfacePatcherInfo patch;

        void Awake()
        {
            cam = GetComponent<Camera>();
            Shader waterSurfaceShader = Utils.LoadExternalShader("WaterSurface");
            if (cam == null || waterSurfaceShader == null)
            {
                Destroy(this);
                return;
            }
            waterSurfaceMat = new Material(waterSurfaceShader);
            cb = new CommandBuffer();
            cb.name = "Water G-Buffer Pre-Pass";
            cam.AddCommandBuffer(CameraEvent.BeforeGBuffer, cb);
            patch = new WaterSurfacePatcherInfo(cam, cb, waterSurfaceMat);
            WaterSurfacePatcher.AddPatch(patch);
        }

        void OnDestroy()
        {
            Debug.LogWarning("Destroying WaterGBufferInjector");
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

    }

    public class WaterSurfacePatcherInfo
    {

        public Camera cam;
        public CommandBuffer cb;
        public Material mat;
        public Mesh mesh;

        public WaterSurfacePatcherInfo(Camera cam, CommandBuffer cb, Material mat)
        {
            this.cam = cam;
            this.cb = cb;
            this.mat = mat;
        }

        public void Clear()
        {
            cb = null;
            mat = null;
            cam = null;
            mesh = null;
        }

    }

    [HarmonyPatch]
    public class WaterSurfacePatcher
    {

        private static List<WaterSurfacePatcherInfo> patchs = new List<WaterSurfacePatcherInfo>();
        private static FieldInfo patchMeshField;
        private static FieldInfo matricesQueueField;
        private static FieldInfo jobHandleField;

        public static void AddPatch(WaterSurfacePatcherInfo patch)
        {
            patchs.Add(patch);
            if (patchMeshField == null)
            {
                patchMeshField = AccessTools.Field(typeof(HeightFieldMesh), "patchMesh");
                matricesQueueField = AccessTools.Field(typeof(HeightFieldMesh), "matrices");
                jobHandleField = AccessTools.Field(typeof(HeightFieldMesh), "jobHandle");
            }
        }

        [HarmonyPatch(typeof(HeightFieldMesh), "FinalizeRender")]
        [HarmonyPrefix]
        public static void FinalizeRender_Prefix(HeightFieldMesh __instance)
        {
            foreach (var patch in patchs)
            {
                if (patch.cb == null || patchMeshField == null)
                {
                    return;
                }

                try
                {
                    JobHandle jobHandle = (JobHandle) jobHandleField.GetValue(__instance);
                    jobHandle.Complete();
                    patch.cb.Clear();

                    patch.mesh = patchMeshField.GetValue(__instance) as Mesh;
                    var waterMatricesQueue = (NativeQueue<float4x4>) matricesQueueField.GetValue(__instance);

                    if (patch.mesh == null || !waterMatricesQueue.IsCreated || waterMatricesQueue.Count == 0)
                    {
                        return;
                    }

                    WaterSurface waterSurface = WaterSurface.Get();
                    var normalsTex = (RenderTexture)AccessTools.Field(typeof(WaterSurface), "normalsTexture").GetValue(waterSurface);
                    var displacementGenerator = AccessTools.Field(typeof(WaterSurface), "displacementGenerator").GetValue(waterSurface);
                    var choppyScale = (float)AccessTools.Field(displacementGenerator.GetType(), "choppyScale").GetValue(displacementGenerator);
                    var waterLevel = waterSurface.transform.position.y + waterSurface.waterOffset;

                    patch.mat.SetTexture("_WaterDisplacementMap", waterSurface.GetDisplacementTexture());
                    patch.mat.SetTexture("_NormalsTex", normalsTex);
                    patch.mat.SetFloat("_WaterPatchLength", waterSurface.GetPatchLength());

                    NativeArray<float4x4> matricesNativeArray = waterMatricesQueue.ToArray(Allocator.Temp);
                    for (int i = 0; i < matricesNativeArray.Length; i++)
                    {
                        patch.cb.DrawMesh(patch.mesh, (Matrix4x4)matricesNativeArray[i], patch.mat, 0, 0);
                    }
                    matricesNativeArray.Dispose();
                    return;
                } 
                catch 
                {
                    Clear();
                    return;
                }
            }
        }

        public static void Clear()
        {
            foreach (var patch in patchs)
            {
                patch.Clear();
            }
            patchMeshField = null;
            matricesQueueField = null;
            jobHandleField = null;
            GBufferCapturePlugin.instance.ClearCB();
            patchs.Clear();
        }

    }

}