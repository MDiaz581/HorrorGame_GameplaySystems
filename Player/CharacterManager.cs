using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CharacterManager : NetworkBehaviour
{
    [Header("Player Prefabs")]
    [SerializeField] private GameObject teamAPrefab; //Monster PREFAB
    [SerializeField] private GameObject teamBPrefab; //Human PREFAB

    public override void OnStartServer()
    {
        StartCoroutine(DelayedReplace());
    }

    private IEnumerator DelayedReplace()
    {
        yield return new WaitForSeconds(0.5f); // give Mirror time to spawn players
        ReplaceAllPlayers();
    }

    [Server]
    private void ReplaceAllPlayers()
    {
        Debug.LogWarning($"Attempting to replace Characters");
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn.identity == null) continue;

            GameObject oldPlayer = conn.identity.gameObject;

            // Decide team (for now: host = Team A, others = Team B)
            GameObject prefabToUse = conn.connectionId == 0 ? teamAPrefab : teamBPrefab;
            
            GameObject newPlayer = Instantiate(prefabToUse, oldPlayer.transform.position, oldPlayer.transform.rotation); // This is randomized when using the SpawnPointManager
            
            NetworkServer.ReplacePlayerForConnection(conn, newPlayer, true);
            Destroy(oldPlayer, 0.1f);

            Debug.Log($"[GameManager] Replaced player {conn.connectionId} with {prefabToUse.name}");
        }
    }
}
