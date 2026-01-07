using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
#if FIZZY
using FizzySteamworks;
#endif
#if STEAMWORKS_NET
using Steamworks;
#endif

public class DuplicationDebugger : MonoBehaviour
{
    void Start()
    {
        DebugLogAll<NetworkManager>("NetworkManager");
#if FIZZY
        DebugLogAll<FizzySteamworksTransport>("FizzySteamworksTransport");
#endif
#if STEAMWORKS_NET
        var steamMgrs = GameObject.FindObjectsOfType<MonoBehaviour>(true);
        int count = 0;
        foreach (var mb in steamMgrs)
        {
            if (mb.GetType().Name.Contains("SteamManager") || mb.GetType().Name.Contains("Steamworks"))
            {
                Debug.Log($"Steam-like manager: {mb.GetType().Name} | name={mb.gameObject.name} | scene={mb.gameObject.scene.name} | id={mb.GetInstanceID()}");
                count++;
            }
        }
        Debug.Log($"Steam-like managers found: {count}");
#endif
    }

    void DebugLogAll<T>(string label) where T : MonoBehaviour
    {
        var arr = GameObject.FindObjectsOfType<T>(true);
        Debug.Log($"{label} count = {arr.Length}");
        for (int i = 0; i < arr.Length; i++)
        {
            var go = arr[i].gameObject;
            Debug.Log($"{label}[{i}] name={go.name} id={go.GetInstanceID()} active={go.activeInHierarchy} scene={go.scene.name}");
        }
    }
}
