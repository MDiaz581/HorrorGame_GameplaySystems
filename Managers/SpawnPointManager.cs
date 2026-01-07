using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SpawnPointManager : NetworkBehaviour
{

    public Transform[] spawnPoints;
    [SerializeField] private bool useRandomSpawn = true; // toggle in inspector

    // Dictionary to keep track of which player has which spawn point
    private Dictionary<GameObject, Transform> assignedPoints = new Dictionary<GameObject, Transform>();

    // Set of currently used spawn points
    private HashSet<Transform> usedPoints = new HashSet<Transform>();

    private void OnEnable()
    {
        NewNetworkManager.addingPlayer += SetSpawnPoint;
        NewNetworkManager.removingPlayer += ReleaseSpawnPoint;
    }

    private void OnDisable()
    {
        NewNetworkManager.addingPlayer -= SetSpawnPoint;
        NewNetworkManager.removingPlayer -= ReleaseSpawnPoint;
    }

    [Server]
    public void SetSpawnPoint(GameObject conn)
    {
        if (!isServer) return;
        Transform freePoint = null;

        // Collect all free points
        List<Transform> freePoints = new List<Transform>();
        foreach (var point in spawnPoints)
        {
            if (!usedPoints.Contains(point))
                freePoints.Add(point);
        }

        if (freePoints.Count > 0)
        {
            if (useRandomSpawn)
            {
                // Pick a random free one
                freePoint = freePoints[Random.Range(0, freePoints.Count)];
            }
            else
            {
                // Pick the first free one (sequential / deterministic)
                freePoint = freePoints[0];
            }
        }
        else
        {
            // Fallback — all taken, so just pick a random one
            freePoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Debug.LogWarning("All spawn points in use — assigning random.");
        }

        // Assign and track
        NetworkIdentity targetID = conn.GetComponent<NetworkIdentity>();

        SetTargetToSpawn(targetID.connectionToClient, freePoint.position);

        //conn.transform.position = freePoint.position; // puts in position
        assignedPoints[conn] = freePoint; //adds the transform point into the conn dictionary. 
        usedPoints.Add(freePoint); //adds that position to the used points.

        Debug.Log($"Player {conn} assigned to spawn point {freePoint.name}");
    }


    [TargetRpc]
    public void SetTargetToSpawn(NetworkConnectionToClient target, Vector3 spawnPosition)
    {
        // This runs on the client who owns this connection
        // Move the local player object
        if (NetworkClient.localPlayer != null)
        {
            NetworkClient.localPlayer.transform.position = spawnPosition;
            //Debug.Log($"[Client] Player moved to {spawnPosition}");
        }
        else
        {
            Debug.LogError("[Client] localPlayer was null!");
        }
    }

    [Server]
    public void ReleaseSpawnPoint(GameObject conn)
    {
        if (!isServer) return;

        if (assignedPoints.TryGetValue(conn, out var point))
        {
            usedPoints.Remove(point);
            assignedPoints.Remove(conn);
            Debug.Log($"Released spawn point {point.name} from player {conn}");
        }
    }
}
