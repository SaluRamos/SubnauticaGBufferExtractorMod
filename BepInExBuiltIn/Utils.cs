using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GBufferCapture
{
    internal class Utils
    {

        //usado para inspecionar terrain patches que são terrivelmente desorganizados na cena
        public static void InvestigateCenterObject()
        {
            Camera mainCam = UnityEngine.Object.FindObjectOfType<WaterSurfaceOnCamera>()?.gameObject.GetComponent<Camera>();
            Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 1000f))
            {
                GameObject hitObject = hit.collider.gameObject;
                Debug.Log($"[Investigator] Raycast atingiu: '{hitObject.name}'");
                Debug.Log($"[Investigator] Layer: {LayerMask.LayerToName(hitObject.layer)}");
                Debug.Log($"[Investigator] Posição do objeto: {hitObject.transform.position}");
                Debug.Log($"[Investigator] Posição da colisão: {hit.point}");
                Debug.Log("--- Componentes no objeto ---");
                foreach (var component in hitObject.GetComponents<Component>())
                {
                    Debug.Log($"- {component.GetType().FullName}");
                }
                var renderer = hitObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Debug.Log($"[Investigator] O objeto possui um componente Renderer: {renderer.GetType().Name}");
                    Debug.Log($"[Investigator] Material: {renderer.material.name}, Shader: {renderer.material.shader.name}");
                }
                else
                {
                    Debug.Log("[Investigator] O objeto NÃO possui um componente Renderer padrão.");
                }
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
                hitObject.name = hitObject.name + "1234";
                Debug.Log("-----------------------------------");
                foreach (var kvp in closestByName)
                {
                    GameObject target = kvp.Value;
                    target.name = target.name + "1234";
                    float distance = closestDistances[kvp.Key];
                    Debug.Log($"[Investigator] Mais próximo: '{target.name}'");
                    Debug.Log($"[Investigator] Posição: {target.transform.position}");
                    Debug.Log($"[Investigator] Distância até o ponto de colisão: {distance}");
                    Debug.Log("Componentes:");
                    foreach (var comp in target.GetComponents<Component>())
                    {
                        Debug.Log($"- {comp.GetType().FullName}");
                    }
                    var rend = target.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        Debug.Log($"Renderer: {rend.GetType().Name}");
                        Debug.Log($"Material: {rend.material.name}, Shader: {rend.material.shader.name}");
                    }
                    else
                    {
                        Debug.Log("Sem Renderer.");
                    }
                    Debug.Log("-----------------------------------");
                }
                if (closestByName.Count == 0)
                {
                    Debug.Log("[Investigator] Nenhum dos objetos alvo foi encontrado.");
                }
            }
            else
            {
                Debug.Log("[Investigator] Raycast não atingiu nada.");
            }
        }

        //exibir todos os materiais e seus shaders na cena
        public static void DumpShaders()
        {
            foreach (var mat in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (mat.shader != null)
                {
                    Debug.LogWarning($"Material: {mat.name} uses shader: {mat.shader.name}");
                }
            }
        }

        public static void ReplaceShader(string name, string shaderName)
        {
            Shader replacer =LoadExternalShader(shaderName);
            int replacedAmount = 0;
            foreach (var mat in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (mat.shader != null && mat.shader.name == name)
                {
                    mat.shader = replacer;
                    replacedAmount++;
                }
            }
            Debug.Log($"total replaced: {replacedAmount}");
        }

        public static Shader LoadExternalShader(string shaderName)
        {
            var bundle = AssetBundle.LoadFromFile(GBufferCapturePlugin.assetBundlePath);

            if (bundle == null)
            { 
                Debug.LogError("Falha ao carregar AssetBundle!");
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
                Debug.LogError(shaderName + " não encontrado no AssetBundle!");
            }

            bundle.Unload(false);
            return loadedShader;
        }

        public static void SaveJPG(string fileName, RenderTexture rtFull, int quality=95)
        {
            string fullPath = System.IO.Path.Combine(GBufferCapturePlugin.captureFolder, fileName);
            int newWidth = rtFull.width / 2;
            int newHeight = rtFull.height / 2;
            RenderTexture rtHalf = RenderTexture.GetTemporary(newWidth, newHeight, 0);
            Graphics.Blit(rtFull, rtHalf);
            Texture2D screenShot = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
            RenderTexture.active = rtHalf;
            screenShot.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            screenShot.Apply();
            byte[] bytes = screenShot.EncodeToJPG(quality);
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
