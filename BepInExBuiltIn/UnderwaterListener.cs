﻿using HarmonyLib;
using UnityEngine;

namespace GBufferCapture
{

    [HarmonyPatch(typeof(UnderWaterTracker), nameof(UnderWaterTracker.UpdateWaterState))]
    public static class UnderWaterListener_Patch
    {

        private static bool state = false;

        public static bool IsUnderWater()
        {
            return state;
        }

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
                    Debug.LogWarning($"IsUnderwater = {newState}");
                    state = newState;
                    //GBufferCapturePlugin.instance.UpdateMapsRenderDistance(newState);
                }
            }
        }
    }

}