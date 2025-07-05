using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GBufferCapture
{

    [HarmonyPatch(typeof(Creature), nameof(Creature.Start))]
    public static class GhostRayOnGBuffer_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Creature __instance)
        {
            if (!(__instance is GhostRay))
            {
                return;
            }
            GhostRay ghostray = __instance as GhostRay;
            ghostray.StartCoroutine(ApplyGbufferFix(ghostray));
        }

        private static IEnumerator ApplyGbufferFix(GhostRay ghostrayInstance)
        {
            yield return new WaitForEndOfFrame();
            SkinnedMeshRenderer[] renderers = ghostrayInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogError("ERRO CRÍTICO: Não foi possível encontrar o SkinnedMeshRenderer da GhostRay! O patch não funcionará.");
                yield break;
            }
            foreach (var renderer in renderers)
            { 
                foreach (var mat in renderer.materials)
                {
                    if (mat != null && mat.shader.name == "MarmosetUBER")
                    {
                        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                        mat.SetOverrideTag("RenderType", "Opaque");
                        mat.SetFloat("_Mode", 0f);
                        mat.SetFloat("_ZWrite", 1f);
                    }
                }
            }
        }
    }

}
