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
    public class DayNightPatch : MonoBehaviour
    {

        private double realTime;

        void Start()
        {
            DayNightCycle.main.debugFreeze = true;
            realTime = DayNightCycle.main.timePassedAsDouble;
            if (DayNightCycle.main != null)
            {
                DayNightCycle.main.SetDayNightTime(0.0f);
            }
            StartCoroutine(RestoreTime());
        }

        void Update()
        {
            DayNightCycle.main.timePassedAsDouble += DayNightCycle.main.deltaTime;
        }

        private System.Collections.IEnumerator RestoreTime()
        {
            yield return new WaitForSeconds(0.1f); // 100ms
            DayNightCycle.main.timePassedAsDouble = realTime;
            AccessTools.Method(typeof(DayNightCycle), "UpdateAtmosphere").Invoke(DayNightCycle.main, null);
            AccessTools.Field(typeof(DayNightCycle), "skipTimeMode").SetValue(DayNightCycle.main, false);
            //do not trigger DayNightCycle.main.dayNightCycleChangedEvent!
        }

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
