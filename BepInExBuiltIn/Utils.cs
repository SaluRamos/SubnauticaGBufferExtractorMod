using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public static void DumpShaders()
        {
            foreach (var mat in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (mat.shader != null)
                {
                    Debug.LogWarning($"Material: {mat.name} usa shader: {mat.shader.name}");
                }
            }
        }

    }
}
