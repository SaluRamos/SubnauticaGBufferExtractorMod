using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GBufferCapture
{
    
    //[HarmonyPatch(typeof(PrefabIdentifier), nameof(PrefabIdentifier.SetPrefabKey))]
    //public static class WorldOnGBuffer_Patch
    //{

    //    private static Material lostRiverGhostTreeLake;

    //    [HarmonyPostfix]
    //    public static void Patch(PrefabIdentifier __instance)
    //    {
    //        //lost river lake
    //        string key = (string) AccessTools.Field(typeof(PrefabIdentifier), "prefabKey").GetValue(__instance);
    //        if (lostRiverGhostTreeLake == null && key == "WorldEntities/Atmosphere/LostRiver/GhostTree/LostRiver_GhostTree_Lake.prefab")
    //        {
    //            Transform surface = __instance.transform.Find("Surface");
    //            if (surface != null)
    //            {
    //                lostRiverGhostTreeLake = surface.GetComponent<MeshRenderer>().sharedMaterial;
    //                lostRiverGhostTreeLake.renderQueue = 2000;

    //                lostRiverGhostTreeLake.SetOverrideTag("RenderType", "Opaque");

    //                // Desligue quaisquer sistemas de brilho ou transparência no MarmosetUBER como precaução
    //                if (lostRiverGhostTreeLake.IsKeywordEnabled("MARMO_EMISSION")) lostRiverGhostTreeLake.DisableKeyword("MARMO_EMISSION");
    //                if (lostRiverGhostTreeLake.HasProperty("_EnableGlow")) lostRiverGhostTreeLake.SetFloat("_EnableGlow", 0f);
    //                if (lostRiverGhostTreeLake.HasProperty("_Mode")) lostRiverGhostTreeLake.SetFloat("_Mode", 0f); // 0 = Opaque

    //                // 4. Copie as propriedades visuais do material original para o novo
    //                // Mapeando UWE/Particles/UBER -> MarmosetUBER
    //                if (lostRiverGhostTreeLake.HasProperty("_Color"))
    //                {
    //                    lostRiverGhostTreeLake.SetColor("_Color", lostRiverGhostTreeLake.GetColor("_Color"));
    //                }
    //                if (lostRiverGhostTreeLake.HasProperty("_MainTex"))
    //                {
    //                    lostRiverGhostTreeLake.SetTexture("_MainTex", lostRiverGhostTreeLake.GetTexture("_MainTex"));
    //                }
    //                if (lostRiverGhostTreeLake.HasProperty("_NormalMap"))
    //                {
    //                    lostRiverGhostTreeLake.SetTexture("_BumpMap", lostRiverGhostTreeLake.GetTexture("_NormalMap"));
    //                    lostRiverGhostTreeLake.EnableKeyword("MARMO_NORMALMAP");
    //                }
    //            }
    //            else
    //            {
    //                Debug.Log("no surface in LostRiver_GhostTree");
    //            }
    //        }
    //    }
    //}

}
