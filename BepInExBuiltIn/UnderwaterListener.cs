using HarmonyLib;
using UnityEngine;

namespace GBufferCapture
{

    [HarmonyPatch(typeof(UnderWaterTracker), nameof(UnderWaterTracker.UpdateWaterState))]
    public static class UnderWaterTracker_UpdateWaterState_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(UnderWaterTracker __instance, out bool __state)
        {
            __state = __instance.isUnderWater;
        }

        [HarmonyPostfix]
        public static void Postfix(UnderWaterTracker __instance, bool __state)
        {
            bool newState = __instance.isUnderWater;
            if (newState != __state)
            {
                if (GBufferCapturePlugin.instance != null)
                { 
                    GBufferCapturePlugin.instance.UpdateMapsRenderDistance(newState);
                }
            }
        }
    }

}
