using HarmonyLib;
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
        private CommandBuffer waterPrePassCB;
        private Material waterGBufferMaterial;
        private Camera cam;

        void Awake()
        {
            cam = GetComponent<Camera>();
            Shader gbufferShader = Utils.LoadExternalShader("WaterSurface");
            if (cam == null || gbufferShader == null)
            {
                Destroy(this);
                return;
            }
            waterGBufferMaterial = new Material(gbufferShader);
            waterPrePassCB = new CommandBuffer { name = "Water G-Buffer Pre-Pass" };
            cam.AddCommandBuffer(CameraEvent.BeforeGBuffer, waterPrePassCB);
            WaterSurfacePatcher.Prepare(waterGBufferMaterial, waterPrePassCB);
        }

        void OnDestroy()
        {
            if (cam != null && waterPrePassCB != null)
            {
                cam.RemoveCommandBuffer(CameraEvent.BeforeGBuffer, waterPrePassCB);
            }
            if (waterPrePassCB != null)
            {
                waterPrePassCB.Dispose();
            }
            if (waterGBufferMaterial != null)
            {
                Destroy(waterGBufferMaterial);
            }
        }

    }

    [HarmonyPatch]
    public static class WaterSurfacePatcher
    {
        // Referências para o nosso sistema
        private static CommandBuffer waterPrePassCB;
        private static Material waterGBufferMaterial;

        // Referências para os campos do jogo
        private static FieldInfo patchMeshField;
        private static FieldInfo matricesQueueField;
        private static FieldInfo jobHandleField;

        public static void Prepare(Material gbufferMaterial, CommandBuffer cb)
        {
            waterGBufferMaterial = gbufferMaterial;
            waterPrePassCB = cb;

            patchMeshField = AccessTools.Field(typeof(HeightFieldMesh), "patchMesh");
            matricesQueueField = AccessTools.Field(typeof(HeightFieldMesh), "matrices");
            jobHandleField = AccessTools.Field(typeof(HeightFieldMesh), "jobHandle");
        }

        [HarmonyPatch(typeof(HeightFieldMesh), "FinalizeRender")]
        [HarmonyPrefix]
        public static void FinalizeRender_Prefix(HeightFieldMesh __instance)
        {
            if (waterPrePassCB == null || patchMeshField == null)
            {
                return;
            }

            try
            {
                JobHandle jobHandle = (JobHandle)jobHandleField.GetValue(__instance);
                jobHandle.Complete();
                waterPrePassCB.Clear();

                Mesh waterPatchMesh = patchMeshField.GetValue(__instance) as Mesh;
                var waterMatricesQueue = (NativeQueue<float4x4>)matricesQueueField.GetValue(__instance);

                if (waterPatchMesh == null || !waterMatricesQueue.IsCreated || waterMatricesQueue.Count == 0)
                {
                    return;
                }

                WaterSurface waterSurface = WaterSurface.Get();
                var normalsTex = (RenderTexture)AccessTools.Field(typeof(WaterSurface), "normalsTexture").GetValue(waterSurface);
                var displacementGenerator = AccessTools.Field(typeof(WaterSurface), "displacementGenerator").GetValue(waterSurface);
                var choppyScale = (float)AccessTools.Field(displacementGenerator.GetType(), "choppyScale").GetValue(displacementGenerator);
                var waterLevel = waterSurface.transform.position.y + waterSurface.waterOffset;

                waterGBufferMaterial.SetTexture("_WaterDisplacementMap", waterSurface.GetDisplacementTexture());
                waterGBufferMaterial.SetTexture("_NormalsTex", normalsTex);
                waterGBufferMaterial.SetFloat("_WaterPatchLength", waterSurface.GetPatchLength());

                NativeArray<float4x4> matricesNativeArray = waterMatricesQueue.ToArray(Allocator.Temp);
                for (int i = 0; i < matricesNativeArray.Length; i++)
                {
                    waterPrePassCB.DrawMesh(waterPatchMesh, (Matrix4x4)matricesNativeArray[i], waterGBufferMaterial, 0, 0);
                }
                matricesNativeArray.Dispose();

                return;
            } 
            catch 
            {
                Clear();
                GBufferCapturePlugin.instance.ClearCB();
                return;
            }

        }

        public static void Clear()
        {
            waterPrePassCB = null;
            waterGBufferMaterial = null;
            patchMeshField = null;
            matricesQueueField = null;
            jobHandleField = null;
        }

    }

}