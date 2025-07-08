using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GBufferCapture
{
    internal class Utils
    {

        private static int chunkCounter = 0;
        private static List<GameObject> countedGameObjects = new List<GameObject>();

        //used to inspect terrain patches
        public static void InvestigateCameraCenterColliderObject()
        {
            Camera mainCam = UnityEngine.Object.FindObjectOfType<WaterSurfaceOnCamera>()?.gameObject.GetComponent<Camera>();
            Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 1000f))
            {
                GameObject hitObject = hit.collider.gameObject;
                var renderer = hitObject.GetComponent<Renderer>();
                string[] targetNames = { "Chunk(Clone)", "ChunkGrass(Clone)", "ChunkLayer(Clone)" };
                Dictionary<string, GameObject> closestByName = new Dictionary<string, GameObject>();
                Dictionary<string, float> closestDistances = new Dictionary<string, float>();
                foreach (var obj in GameObject.FindObjectsOfType<GameObject>())
                {
                    string name = obj.name;
                    if (System.Array.IndexOf(targetNames, name) == -1) continue;
                    float dist = Vector3.Distance(hitObject.transform.position, obj.transform.position);
                    if (!closestByName.ContainsKey(name) || dist < closestDistances[name])
                    {
                        closestByName[name] = obj;
                        closestDistances[name] = dist;
                    }
                }
                if (!countedGameObjects.Contains(hitObject))
                {
                    hitObject.name = hitObject.name + chunkCounter;
                    countedGameObjects.Add(hitObject);
                }
                foreach (var kvp in closestByName)
                {
                    GameObject target = kvp.Value;
                    if (!countedGameObjects.Contains(target))
                    {
                        target.name = target.name + chunkCounter;
                        countedGameObjects.Add(target);
                    }
                }
                chunkCounter++;
                Debug.Log($"Hit object: {hitObject.name} at position {hitObject.transform.position} and renamed with id {chunkCounter}");
            }
        }

        public static List<Material> uniqueMaterials = new List<Material>();

        public static void DumpUniqueMaterials()
        {
            Renderer[] allRenderers = Resources.FindObjectsOfTypeAll<Renderer>();
            foreach (Renderer renderer in allRenderers)
            {
                Material mat = renderer.sharedMaterial;
                if (!uniqueMaterials.Contains(mat))
                {
                    uniqueMaterials.Add(mat);
                    Debug.Log($"Material \"{mat.name}\" uses shader \"{mat.shader}\"");
                }
            }
        }

        public static void ReplaceShader(string originalShaderName, string newShaderName)
        {
            Shader newShader = LoadExternalShader(newShaderName);
            if (newShader == null)
            {
                Debug.LogError("Shader de substituição não carregado. Abortando.");
                return;
            }
            int materialsReplacedCount = 0;
            int renderersAffectedCount = 0;
            Renderer[] allRenderers = Resources.FindObjectsOfTypeAll<Renderer>();
            foreach (Renderer renderer in allRenderers)
            {
                Material[] currentMaterials = renderer.materials;
                bool materialsWereChanged = false;
                for (int i = 0; i < currentMaterials.Length; i++)
                {
                    Material mat = currentMaterials[i];
                    if (mat != null && mat.shader != null && mat.shader.name == originalShaderName)
                    {
                        string[] oldKeywords = mat.shaderKeywords;
                        mat.shader = newShader;
                        mat.shaderKeywords = oldKeywords;
                        materialsWereChanged = true;
                        materialsReplacedCount++;
                    }
                }
                if (materialsWereChanged)
                {
                    renderer.materials = currentMaterials;
                    renderersAffectedCount++;
                }
            }
            Debug.Log($"materials replaced: {materialsReplacedCount}");
            Debug.Log($"renderers replaced: {renderersAffectedCount}");
        }

        private static AssetBundle bundle;

        public static Shader LoadExternalShader(string shaderName)
        {
            if (bundle == null)
            { 
                bundle = AssetBundle.LoadFromFile(GBufferCapturePlugin.assetBundlePath);
                if (bundle == null)
                {
                    Debug.LogError("failed to load AssetBundle!");
                }
            }
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

    }
}
