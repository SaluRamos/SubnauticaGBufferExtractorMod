using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GBufferCapture
{
    internal class Utils
    {

        private static AssetBundle bundle;

        private static void EnsureBundleIsLoaded()
        {
            if (bundle == null)
            {
                bundle = AssetBundle.LoadFromFile(GBufferCapturePlugin.assetBundlePath);
                if (bundle == null)
                {
                    Debug.LogError("failed to load AssetBundle!");
                }
            }
        }

        public static Shader LoadExternalShader(string shaderName)
        {
            EnsureBundleIsLoaded();
            Shader loadedShader = bundle.LoadAsset<Shader>(shaderName);
            if (loadedShader != null)
            {
                if (!loadedShader.isSupported)
                {
                    Debug.LogWarning(shaderName + " loaded, but not supported!");
                }
            }
            else
            {
                Debug.LogError(shaderName + " not found on AssetBundle!");
            }
            bundle.Unload(false);
            return loadedShader;
        }

        public static ComputeShader LoadExternalComputeShader(string shaderName)
        {
            EnsureBundleIsLoaded();
            if (bundle == null) return null;

            ComputeShader loadedShader = bundle.LoadAsset<ComputeShader>(shaderName);
            if (loadedShader == null)
            {
                Debug.LogError($"Utils: ComputeShader '{shaderName}' não encontrado no AssetBundle!");
                return null;
            }
            return loadedShader;
        }

        public static void UnloadAssetBundle()
        {
            if (bundle != null)
            {
                bundle.Unload(true);
                bundle = null;
            }
        }

        public static void SaveTexture(string fileName, RenderTexture rtFull, int newWidth, int newHeight, string extension, Func<Texture2D, byte[]> encoder)
        {
            fileName = Path.GetFileNameWithoutExtension(fileName);
            string fullPath = Path.Combine(GBufferCapturePlugin.captureFolder, fileName + extension);
            RenderTexture rtHalf = RenderTexture.GetTemporary(newWidth, newHeight, 0);
            Graphics.Blit(rtFull, rtHalf);
            Texture2D screenShot = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
            RenderTexture.active = rtHalf;
            screenShot.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            screenShot.Apply();

            byte[] bytes = encoder(screenShot);
            try
            {
                File.WriteAllBytes(fullPath, bytes);
            }
            catch (IOException ex)
            {
                Debug.LogError($"Error in saving file: {ex.Message}");
            }

            UnityEngine.Object.Destroy(screenShot);
            RenderTexture.ReleaseTemporary(rtHalf);
        }

        public static void ToggleScubaMask(bool active)
        {
            //most screen trash uses a component called "HideForScreenshots"
            Transform player = Player.main?.transform;
            if (player == null)
            {
                return;
            }
            Transform scubaMask = player.Find("camPivot/camRoot/camOffset/pdaCamPivot/SpawnPlayerMask");
            if (scubaMask == null)
            {
                return;
            }
            scubaMask.gameObject.SetActive(active);
        }

        public static void TogglePlayerBreathBubbles(bool active)
        {
            PlayerBreathBubbles[] bubbles = UnityEngine.Object.FindObjectsOfType<PlayerBreathBubbles>();
            foreach (PlayerBreathBubbles bubbleController in bubbles)
            {
                bubbleController.enabled = active;
            }
        }

        public static void ToggleWaterParticlesSpawner(bool active)
        {
            Transform player = Player.main?.transform;
            if (player == null)
            {
                return;
            }
            Transform waterParticles = player.Find("camPivot/camRoot/camOffset/pdaCamPivot/SpawnPlayerFX/PlayerFX(Clone)/WaterParticlesSpawner");
            if (waterParticles == null)
            {
                Debug.LogError("WaterParticlesSpawner not found");
                return;
            }
            waterParticles.gameObject.SetActive(active);
        }

        public static void ToggleParts(bool scubamask, bool breathBubbles, bool waterParticles)
        {
            try
            {
                ToggleScubaMask(scubamask);
                TogglePlayerBreathBubbles(breathBubbles);
                ToggleWaterParticlesSpawner(waterParticles);
            }
            catch (Exception e)
            {
                //NullReferenceException, probably changed to menu scene
            }
        }

    }
}
