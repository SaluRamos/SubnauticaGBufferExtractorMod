using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static HandReticle;
using static VFXParticlesPool;

namespace GBufferCapture.creatures
{

    [HarmonyPatch(typeof(Creature), nameof(Creature.Start))]
    public static class CreaturesOnGBuffer_Patch
    {

        private static Material hoopFishMat;

        [HarmonyPostfix]
        public static void Postfix(Creature __instance)
        {
            
            __instance.StartCoroutine(ApplyGbufferFix(__instance));
        }


        private static IEnumerator ApplyGbufferFix(Creature creature)
        {
            yield return new WaitForEndOfFrame();

            if (hoopFishMat == null)
            {
                var task = CraftData.GetPrefabForTechTypeAsync(TechType.HoopfishSchool);
                yield return task;
                GameObject prefab = task.GetResult();
                if (prefab == null)
                {
                    Debug.Log("prefab not found");
                    yield break;
                }
                MeshRenderer[] hoopfishRenderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
                if (hoopfishRenderers.Length == 0)
                {
                    Debug.Log("no renderer in prefab");
                    yield break;
                }
                hoopFishMat = hoopfishRenderers[0].sharedMaterial;
            }

            //only apply to creatures that has real geometry
            if (creature.GetType() == typeof(Jellyray) ||
                creature.GetType() == typeof(GhostRay) ||
                creature.GetType() == typeof(Hoopfish) ||
                creature.GetType() == typeof(Bladderfish) ||
                creature.GetType() == typeof(SpineEel) ||
                creature.GetType() == typeof(GhostLeviathan) ||
                creature.GetType() == typeof(GhostLeviatanVoid))
            { 
                SkinnedMeshRenderer[] skinnedRenderers = creature.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var renderer in skinnedRenderers)
                {
                    foreach (var mat in renderer.sharedMaterials)
                    {
                        if (mat != null && mat.shader.name == "MarmosetUBER")
                        {
                            mat.renderQueue = 2000;
                            mat.SetOverrideTag("RenderType", "Opaque");
                            mat.SetFloat("_Mode", 0f);
                            mat.SetFloat("_ZWrite", 1f);
                            if (mat.HasProperty("_GlowColor")) mat.SetColor("_GlowColor", Color.black);
                            if (mat.HasProperty("_GlowStrength")) mat.SetFloat("_GlowStrength", 0f);
                            if (mat.HasProperty("_GlowStrengthNight")) mat.SetFloat("_GlowStrengthNight", 0f);
                        }
                    }
                }
            }

            //particles like HoopFish_02_School are pure Creatures and use MeshRenderer
            //if (creature.gameObject.name.Contains("HoopFish_02_School") ||
            //    creature.gameObject.name.Contains("BladderFishSchool"))
            //{ 
                MeshRenderer[] renderers = creature.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var renderer in renderers)
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] != null && materials[i].shader.name == "MarmosetUBER")
                        {
                            Material newMat = new Material(hoopFishMat);
                            foreach (string property in materials[i].GetTexturePropertyNames())
                            {
                                if (materials[i].HasProperty(property) && newMat.HasProperty(property))
                                {
                                    newMat.SetTexture(property, materials[i].GetTexture(property));
                                }
                            }
                            newMat.mainTextureOffset = materials[i].mainTextureOffset;
                            newMat.mainTextureScale = materials[i].mainTextureScale;
                            //if (newMat.IsKeywordEnabled("MARMO_EMISSION")) newMat.DisableKeyword("MARMO_EMISSION");
                            newMat.SetFloat("_EnableGlow", 0f);
                            if (newMat.HasProperty("_GlowColor")) newMat.SetColor("_GlowColor", Color.black);
                            if (newMat.HasProperty("_GlowStrength")) newMat.SetFloat("_GlowStrength", 0f);
                            if (newMat.HasProperty("_GlowStrengthNight")) newMat.SetFloat("_GlowStrengthNight", 0f);
                            if (newMat.HasProperty("_Illum")) newMat.SetTexture("_Illum", null);
                            materials[i] = newMat;
                        }
                    }
                    renderer.sharedMaterials = materials;
                }
            //}
        }

    }

}
