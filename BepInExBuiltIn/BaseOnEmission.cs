using HarmonyLib;
using mset;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;

namespace GBufferCapture
{

    public class BaseOnEmission : MonoBehaviour
    {

        public static BaseOnEmission main;
        private bool lightStatus;
        private Light playerLight;

        void OnDestroy()
        {
            if (playerLight != null)
            {
                Destroy(playerLight);
            }
            if (Player.main != null)
            {
                Player.main.currentSubChangedEvent.RemoveHandler(this, OnSubChanged);
            }
            hasSkyValues = false;
            RestoreAllSubRootLights();
            BaseOnEmission.main = null;
        }

        void Start()
        {
            if (BaseOnEmission.main == null)
            {
                BaseOnEmission.main = this;
            }
            Camera mainCam = UnityEngine.Object.FindObjectOfType<WaterSurfaceOnCamera>()?.gameObject.GetComponent<Camera>();
            playerLight = mainCam.gameObject.AddComponent<Light>();
            playerLight.type = LightType.Spot;
            playerLight.range = 200f;
            playerLight.intensity = 1f;
            playerLight.spotAngle = 179f;
            OnSubChanged(null);
            Player.main.currentSubChangedEvent.AddHandler(this, OnSubChanged);
            SubscribeToPowerRelay();
            UpdateAllSubRootLights();
        }

        ////plan B
        //void LateUpdate()
        //{
        //    bool isInside = Player.main.IsInside();
        //    bool isPowered = Player.main.CanBreathe();
        //    UpdateLights(isInside && isPowered);
        //}

        private void SubscribeToPowerRelay()
        {
            PowerRelay[] allPowerRelays = UnityEngine.Object.FindObjectsOfType<PowerRelay>();
            foreach (var powerRelay in allPowerRelays)
            {
                powerRelay.powerDownEvent.AddHandler(powerRelay, OnPowerRelayChanged);
                powerRelay.powerUpEvent.AddHandler(powerRelay, OnPowerRelayChanged);
            }
        }

        private void RestoreAllSubRootLights()
        {
            SubRoot[] allSubRoots = UnityEngine.Object.FindObjectsOfType<SubRoot>();
            foreach (SubRoot subRoot in allSubRoots)
            {
                foreach (LightingController.MultiStatesSky sky in subRoot.lightControl.skies)
                {
                    RestoreStructureLights(sky);
                }
            }
            EscapePod newEscapePod = EscapePod.main;
            if (newEscapePod != null)
            {
                foreach (LightingController.MultiStatesSky sky in newEscapePod.lightingController.skies)
                {
                    RestoreStructureLights(sky);
                }
            }
        }

        private void UpdateAllSubRootLights()
        {
            SubRoot[] allSubRoots = UnityEngine.Object.FindObjectsOfType<SubRoot>();
            foreach (SubRoot subRoot in allSubRoots)
            {
                foreach (LightingController.MultiStatesSky sky in subRoot.lightControl.skies)
                {
                    DisableStructureLights(sky);
                }
            }
            EscapePod newEscapePod = EscapePod.main;
            if (newEscapePod != null)
            {
                foreach (LightingController.MultiStatesSky sky in newEscapePod.lightingController.skies)
                {
                    DisableStructureLights(sky);
                }
            }
        }

        //works for base and cyclops
        public void OnSubChanged(SubRoot newSub)
        {
            Debug.LogWarning("SubChanged");
            bool isInside = (Player.main.currentSub != null || Player.main.escapePod.value);
            bool hasEnergy = false;
            if (isInside)
            {
                hasEnergy = (bool) AccessTools.Field(typeof(PowerRelay), "isPowered").GetValue(Player.main.currentSub.powerRelay);
            }
            UpdateLights(isInside && hasEnergy);
        }

        //works for everyone
        public void OnPowerRelayChanged(PowerRelay powerRelay)
        {
            Debug.LogWarning("PowerRelay Trigger");
            bool isPowered = (bool)AccessTools.Field(typeof(PowerRelay), "isPowered").GetValue(powerRelay);
            bool isInside = false;
            //player must be inside subroot or escapePod
            if (powerRelay.gameObject.GetComponent<EscapePod>() != null) //inside escapePod
            {
                isInside = Player.main.escapePod.value;
                Debug.LogWarning("inside escapePod");
            }
            else if (powerRelay.GetType() == typeof(BasePowerRelay)) //inside base
            {
                isInside = ((BasePowerRelay)powerRelay).subRoot.playerInside;
                Debug.LogWarning("inside base");
            }
            else //inside submarine
            {
                SubRoot subRoot = powerRelay.gameObject.GetComponent<SubRoot>();
                if (subRoot != null)
                {
                    isInside = subRoot.playerInside;
                    Debug.LogWarning("inside submarine");
                }
            }
            Debug.LogWarning($"PowerRelay, isPowered {isPowered}, isInside {isInside}");
            if (isInside)
            {
                BaseOnEmission.main?.UpdateLights(isPowered);
            }
        }

        public void UpdateLights(bool newLightStatus)
        {
            if (newLightStatus == lightStatus)
            {
                return;
            }
            lightStatus = newLightStatus;
            playerLight.enabled = newLightStatus;
            Debug.LogWarning($"playerLight is {newLightStatus}");
            if (newLightStatus)
            {
                SubRoot newSub = Player.main.currentSub;
                if (newSub != null)
                {
                    foreach (LightingController.MultiStatesSky sky in newSub.lightControl.skies)
                    {
                        DisableStructureLights(sky);
                    }
                }
            }
        }

        private void DisableStructureLights(LightingController.MultiStatesSky sky)
        {
            if (!hasSkyValues)
            {
                hasSkyValues = true;
                masterIntensities = sky.masterIntensities;
                diffIntensities = sky.diffIntensities;
                specIntensities = sky.specIntensities;
                startMasterIntensity = (float) AccessTools.Field(typeof(LightingController.MultiStatesSky), "startMasterIntensity").GetValue(sky);
                startDiffuseIntensity = (float)AccessTools.Field(typeof(LightingController.MultiStatesSky), "startDiffuseIntensity").GetValue(sky);
                startSpecIntensity = (float)AccessTools.Field(typeof(LightingController.MultiStatesSky), "startSpecIntensity").GetValue(sky);
                MasterIntensity = sky.sky.MasterIntensity;
                DiffIntensity = sky.sky.DiffIntensity;
                SpecIntensity = sky.sky.SpecIntensity;
            }
            sky.masterIntensities = new float[3] { 0.1f, 0.1f, 0.1f };
            sky.diffIntensities = new float[3] { 0.1f, 0.1f, 0.1f };
            sky.specIntensities = new float[3] { 0.1f, 0.1f, 0.1f };
            AccessTools.Field(typeof(LightingController.MultiStatesSky), "startMasterIntensity").SetValue(sky, 0.1f);
            AccessTools.Field(typeof(LightingController.MultiStatesSky), "startDiffuseIntensity").SetValue(sky, 0.1f);
            AccessTools.Field(typeof(LightingController.MultiStatesSky), "startSpecIntensity").SetValue(sky, 0.1f);
            sky.sky.MasterIntensity = 0.1f;
            sky.sky.DiffIntensity = 0.1f;
            sky.sky.SpecIntensity = 0.1f;
        }

        private bool hasSkyValues = false;
        private float[] masterIntensities;
        private float[] diffIntensities;
        private float[] specIntensities;
        private float startMasterIntensity;
        private float startDiffuseIntensity;
        private float startSpecIntensity;
        private float MasterIntensity;
        private float DiffIntensity;
        private float SpecIntensity;

        private void RestoreStructureLights(LightingController.MultiStatesSky sky)
        {
            try
            {
                sky.masterIntensities = masterIntensities;
                sky.diffIntensities = diffIntensities;
                sky.specIntensities = specIntensities;
                AccessTools.Field(typeof(LightingController.MultiStatesSky), "startMasterIntensity").SetValue(sky, startMasterIntensity);
                AccessTools.Field(typeof(LightingController.MultiStatesSky), "startDiffuseIntensity").SetValue(sky, startDiffuseIntensity);
                AccessTools.Field(typeof(LightingController.MultiStatesSky), "startSpecIntensity").SetValue(sky, startSpecIntensity);
                sky.sky.MasterIntensity = MasterIntensity;
                sky.sky.DiffIntensity = DiffIntensity;
                sky.sky.SpecIntensity = SpecIntensity;
            }
            catch (Exception ex)
            {
                //probably changed to menu scene
            }
        }

    }

    [HarmonyPatch]
    public class BaseOnEmissionPatch
    {

        [HarmonyPatch(typeof(EnterExitHelper), nameof(EnterExitHelper.Enter))]
        [HarmonyPostfix]
        public static void OnEnterEscapePod(GameObject gameObject, Player player, bool isForEscapePod, bool setCurrentSubForced)
        {
            if (isForEscapePod && BaseOnEmission.main != null)
            {
                bool isPowered = (bool)AccessTools.Field(typeof(PowerRelay), "isPowered").GetValue(EscapePod.main.gameObject.GetComponent<PowerRelay>());
                BaseOnEmission.main.UpdateLights(isPowered);
            }
        }

        [HarmonyPatch(typeof(EnterExitHelper), nameof(EnterExitHelper.Exit))]
        [HarmonyPostfix]
        public static void OnExitEscapePod(Transform transform, Player player, bool isForEscapePod, bool isForWaterPark)
        {
            if (isForEscapePod && BaseOnEmission.main != null)
            {
                BaseOnEmission.main.UpdateLights(false);
            }
        }

    }

}
