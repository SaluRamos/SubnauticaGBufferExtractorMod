using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GBufferCapture
{

    [HarmonyPatch]
    public class DayNightPatch
    {

        [HarmonyPatch(typeof(DayNightCycle), "OnConsoleCommand_day")]
        [HarmonyPrefix]
        public static bool SetDayPatch(DayNightCycle __instance, NotificationCenter.Notification n)
        {
            Debug.LogWarning("setting day");
            DayNightCycle.main.timePassedAsDouble += 1200.0 - DayNightCycle.main.timePassed % 1200.0 + 600.0;
            return false;
        }

        [HarmonyPatch(typeof(DayNightCycle), "OnConsoleCommand_night")]
        [HarmonyPrefix]
        public static bool SetNightPatch(DayNightCycle __instance, NotificationCenter.Notification n)
        {
            Debug.LogWarning("setting night");
            DayNightCycle.main.timePassedAsDouble += 1200.0 - DayNightCycle.main.timePassed % 1200.0;
            return false;
        }

    }

}
