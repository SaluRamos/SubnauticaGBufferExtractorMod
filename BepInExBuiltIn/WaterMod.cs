using GBufferCapture;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace WaterMod
{
    // Este componente é adicionado dinamicamente à câmera.
    public class WaterGBufferInjector : MonoBehaviour
    {
        private CommandBuffer waterPrePassCB;
        private Material waterGBufferMaterial;
        private Camera cam;

        void Awake()
        {
            cam = GetComponent<Camera>();
            Shader gbufferShader = GBufferCapturePlugin.LoadExternalShader(GBufferCapturePlugin.assetBundlePath, "WaterSurface");

            if (cam == null || gbufferShader == null)
            {
                Debug.LogError("[WaterMod] Falha na inicialização do Injetor (Câmera ou Shader nulo).");
                Destroy(this);
                return;
            }

            waterGBufferMaterial = new Material(gbufferShader);
            waterPrePassCB = new CommandBuffer { name = "Water G-Buffer Pre-Pass" };
            cam.AddCommandBuffer(CameraEvent.BeforeGBuffer, waterPrePassCB);

            // Passa os objetos necessários para o Patcher estático
            WaterSurfacePatcher.Prepare(waterGBufferMaterial, waterPrePassCB);

            Debug.Log("[WaterMod] Injetor e Patcher preparados.");
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
            Debug.Log("[WaterMod] WaterGBufferInjector e seus recursos foram limpos.");
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
        // Agora não precisamos mais retornar um bool, pois não vamos pular o original
        public static void FillGBufferWithWater_Prefix(HeightFieldMesh __instance)
        {
            // Se nosso sistema não estiver pronto, não fazemos nada.
            if (waterPrePassCB == null || patchMeshField == null) return;

            // 1. Força a conclusão do Job
            JobHandle jobHandle = (JobHandle)jobHandleField.GetValue(__instance);
            jobHandle.Complete();

            // 2. Limpa nosso CommandBuffer
            waterPrePassCB.Clear();

            // 3. Pega os dados
            Mesh waterPatchMesh = patchMeshField.GetValue(__instance) as Mesh;
            var waterMatricesQueue = (NativeQueue<float4x4>)matricesQueueField.GetValue(__instance);

            if (waterPatchMesh == null || !waterMatricesQueue.IsCreated || waterMatricesQueue.Count == 0)
            {
                return; // Não há nada para desenhar neste frame
            }

            WaterSurface waterSurface = WaterSurface.Get();
            var normalsTex = (RenderTexture)AccessTools.Field(typeof(WaterSurface), "normalsTexture").GetValue(waterSurface);
            var displacementGenerator = AccessTools.Field(typeof(WaterSurface), "displacementGenerator").GetValue(waterSurface);
            var choppyScale = (float)AccessTools.Field(displacementGenerator.GetType(), "choppyScale").GetValue(displacementGenerator);
            var waterLevel = waterSurface.transform.position.y + waterSurface.waterOffset;

            // 4. Configura nosso material G-Buffer (o mesmo de antes)
            waterGBufferMaterial.SetTexture("_WaterDisplacementMap", waterSurface.GetDisplacementTexture());
            waterGBufferMaterial.SetTexture("_NormalsTex", normalsTex);
            waterGBufferMaterial.SetFloat("_WaterPatchLength", waterSurface.GetPatchLength());

            // 5. Preenche nosso CommandBuffer com os comandos de desenho
            NativeArray<float4x4> matricesNativeArray = waterMatricesQueue.ToArray(Allocator.Temp);
            for (int i = 0; i < matricesNativeArray.Length; i++)
            {
                // Usamos a malha e a matriz do jogo, MAS o NOSSO material G-Buffer
                waterPrePassCB.DrawMesh(waterPatchMesh, (Matrix4x4)matricesNativeArray[i], waterGBufferMaterial, 0, 0);
            }
            matricesNativeArray.Dispose();

            // 6. NÃO retornamos false. O método original vai rodar normalmente,
            // desenhando a água visualmente. O Z-buffer que nosso passe escreveu
            // vai garantir que a água visual seja desenhada corretamente sobre
            // os objetos que estão embaixo dela, e que objetos na frente dela
            // sejam desenhados corretamente.
        }
    }

}