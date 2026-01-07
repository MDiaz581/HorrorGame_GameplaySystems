using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using UnityEngine.SceneManagement;
using Mirror.FizzySteam;
using Unity.VisualScripting;

public class NewNetworkManager : NetworkManager
{
    public GameObject lobbyUI;
    public static event Action clientConnected;
    public static event Action<int> serverClientConnect;
    public static event Action<int> serverClientDisconnect;
    public static event Action serverShutdown;
    public static event Action<GameObject> addingPlayer;
    public static event Action<GameObject> removingPlayer;
    public static event Action clientDisconnect;
    public static event Action<string> OnServerSceneChangedEvent;



    public void OnEnable()
    {
        NextClient.NextClient_OnConnectionError += TestError;
    }
    public void OnDisable()
    {
        NextClient.NextClient_OnConnectionError -= TestError;
    }

    public void TestError(string errorStr)
    {
        Debug.LogWarning($"{errorStr}");
    }


    public override void OnStartServer()
    {
        Debug.Log("Server Started!");
        base.OnStartServer();
    }

    public override void OnStopServer()
    {
        Debug.Log("Server Stopped!");
        base.OnStopServer();

        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene != "MainMenu")
        {
            SceneManager.LoadScene("MainMenu");
        }
        //serverShutdown?.Invoke();
    }


    public override void OnStopClient()
    {
        base.OnStopClient();

        Debug.Log("Stopping Client Connection");

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != "MainMenu")
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    public override void OnClientConnect()
    {
        Debug.Log("Client Connect");

        if (NetworkClient.isConnected && !NetworkClient.ready)
        {
            NetworkClient.Ready();

            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != "MainMenu")
            {
                NetworkClient.AddPlayer(); // Remove this when testing main menu scene;
            }


            StartCoroutine(DelayReadyAction());
        }

        clientConnected?.Invoke();

    }

    private IEnumerator DelayReadyAction()
    {
        Debug.Log("Joining Game");
        yield return new WaitForSeconds(0.5f);
    }

    public override void OnClientDisconnect()
    {
        Debug.Log("Client Disconnect");
        //  SceneManager.LoadScene("MainMenu"); could work to send back to main menu but it causes steammanager to duplicate and unable to keep track of information correctly.
        //GetComponent<NetworkManagerHUD>().enabled = true;        
    }

    public void ClientInGame()
    {
        // This is directly taken from NetworkManagerHUD.cs
        if (NetworkClient.isConnected && !NetworkClient.ready)
        {
            // Sets client ready to ready
            NetworkClient.Ready();
            if (NetworkClient.localPlayer == null)

                /*
                When this is called:

                1.) It sends a AddPlayerMessage to the server.

                2.) The server receives that message and triggers: public override void OnServerAddPlayer(NetworkConnectionToClient conn)
                */
                NetworkClient.AddPlayer();

            lobbyUI.SetActive(false);
        }
    }


    //OnServerAddPlayer is a relatively simple function all it does is take the player prefab, instatiates it, then runs AddPlayerForConnection to add it to the game.
    //At its simplist this is the code:
    // GameObject player = Instantiate(playerPrefab);
    // player.name = $"{playerPrefab.name} [connId={conn.connectionId}]";
    // NetworkServer.AddPlayerForConnection(conn, player);
    //Using the same instantiation rules you can add spawn points with the instantiation overloads.

    /*
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);

        OnServerAddPlayer(conn);
    }
    */


    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log("Adding Player");
        GameObject player = Instantiate(playerPrefab);
        if (playerPrefab.GetComponent<GetPlayerName>() != null)
        {

        }
        player.name = $"{playerPrefab.name} [connId={conn.connectionId}]";

        NetworkServer.AddPlayerForConnection(conn, player);

        addingPlayer?.Invoke(player);

    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log($"Server: Client connected via Mirror, connection ID: {conn.connectionId}, address: {conn.address}"); //conn.address is the STEAM ID
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        removingPlayer?.Invoke(conn.identity.transform.gameObject);
        if (GameManager.gmInstance != null)
        {
            GameManager.gmInstance.RemovePlayer(conn.connectionId);
        }
        base.OnServerDisconnect(conn);
        Debug.Log($"Server: Client Disconnected via Mirror, connection ID: {conn.connectionId}, address: {conn.address}"); //conn.address is the STEAM ID
        clientDisconnect?.Invoke();
    }

    public override void OnServerChangeScene(string newSceneName)
    {
        base.OnServerChangeScene(newSceneName);

                Debug.Log($"Server scene changed to {newSceneName}");

        OnServerSceneChangedEvent?.Invoke(newSceneName);
        
    }


    public void ReplacePlayer(NetworkConnectionToClient conn, GameObject newPrefab)
    {
        GameObject oldPlayer = conn.identity.gameObject;

        NetworkServer.ReplacePlayerForConnection(conn, Instantiate(newPrefab), true);

        Destroy(oldPlayer, 0.1f);
    }

}
