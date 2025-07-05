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
    public static class HoopfishOnGBuffer_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Creature __instance)
        {
            if (!(__instance is Hoopfish))
            {
                return;
            }
            Hoopfish hoopfish = __instance as Hoopfish;
            hoopfish.StartCoroutine(ApplyGbufferFix(hoopfish));
        }

        private static IEnumerator ApplyGbufferFix(Hoopfish hoopfishInstance)
        {
            yield return new WaitForEndOfFrame();
            SkinnedMeshRenderer renderer = hoopfishInstance.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (renderer == null)
            {
                Debug.LogError("Cant find SkinnedMeshRenderer of Hoopfish!");
                yield break;
            }
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
