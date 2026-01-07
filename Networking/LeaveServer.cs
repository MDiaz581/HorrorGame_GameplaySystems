using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using Steamworks;
using Mirror.FizzySteam;

public class LeaveServer : NetworkBehaviour
{
    [Header("Callbacks")]
    public CSteamID hostLobbyID;

    public Button leaveButton;

    //public NewNetworkManager netManager; we'll need to make another network identity object to solely send info to players

    public void Awake()
    {
        hostLobbyID = Steam_JoinRoom.CurrentLobbyID;
    }

    public void OnLeaveButtonClicked()
    {
        //CmdTest();


        if (isServer)
        {
            if (SteamMatchmaking.GetLobbyOwner(hostLobbyID) == SteamUser.GetSteamID())
            {
                SteamMatchmaking.SetLobbyJoinable(hostLobbyID, false); // Prevents new joins
                Debug.LogError("SteamLobby Set joinable to: false");
            }

            StartCoroutine(DelayedHostDisconnect()); // Delay the host disconnect to appropriately inform users and change visiblity settings on the server.

            //NewNetworkManager.singleton.StopHost();
            //NewNetworkManager.singleton.StopServer();
            //RpcTest();
        }
        else
        {
            NewNetworkManager.singleton.StopClient();
        }

        SteamMatchmaking.LeaveLobby(hostLobbyID);


        /*
        // Now leave the Steam lobby
        if (hostLobbyID.m_SteamID != 0)
        {
            SteamMatchmaking.LeaveLobby(hostLobbyID);
        }




        NewNetworkManager.singleton.StopClient();

        if (isServer)
        {
            Debug.Log("We are the server shutting down");
            NewNetworkManager.singleton.StopServer();

        }
        */
    }

    IEnumerator DelayedHostDisconnect()
    {
        yield return new WaitForSeconds(3f);

        NewNetworkManager.singleton.StopHost();

    }

    #region Leave Lobby
    public void LeaveLobby()
    {
        hostLobbyID = Steam_JoinRoom.CurrentLobbyID;

        if (isServer)
        {
            Debug.Log("I'm the server");
        }

        if (NetworkServer.active)
        {
            //networkManager.RpcServerShuttingDown(); apparently has to be declared in the base network manager
            RpcHostLeave();
        }
        //If host
        if (SteamMatchmaking.GetLobbyOwner(hostLobbyID) == SteamUser.GetSteamID())
        {
            SteamMatchmaking.SetLobbyJoinable(hostLobbyID, false); // Prevents new joins
            Debug.LogError("SteamLobby Set joinable to: false");
        }
        else
        {
            Debug.Log("Not Host ignore above");
        }
        // Tell Mirror to clean up networking first
        if (NetworkServer.active && NetworkClient.isConnected)
            NewNetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NewNetworkManager.singleton.StopClient();
        else if (NetworkServer.active)
            NewNetworkManager.singleton.StopServer();

        // Now leave the Steam lobby
        if (hostLobbyID.m_SteamID != 0)
            SteamMatchmaking.LeaveLobby(hostLobbyID);
    }

    [Command(requiresAuthority = false)]
    public void CmdTest()
    {
        Debug.Log("Sending Test Command");
    }

    [ClientRpc]
    public void RpcTest()
    {
        Debug.Log("Host is leaving, Disconnecting");
        NewNetworkManager.singleton.StopClient();
    }

    [Command(requiresAuthority = false)]
    public void CmdLeaveServer(NetworkConnectionToClient sender = null)
    {
        Debug.Log("Server: Received CmdLeaveServer");

        // If sender is the host
        if (sender == null || sender.identity == null || sender.identity.isServer)
        {
            Debug.Log("Server: Host is leaving, notifying clients");

            if (SteamMatchmaking.GetLobbyOwner(hostLobbyID) == SteamUser.GetSteamID())
            {
                SteamMatchmaking.SetLobbyJoinable(hostLobbyID, false); // Prevents new joins
                Debug.LogError("SteamLobby Set joinable to: false");
            }
            RpcHostLeave();
        }
        else
        {
            // Non-host client leaves
            Debug.Log($"Server: Client {sender.connectionId} is leaving");

            sender.Disconnect(); // Disconnect the client
        }
    }

    [ClientRpc]
    private void RpcHostLeave()
    {
        Debug.Log("Client: Received RpcHostLeave");
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            Debug.Log("Host: Stopping host");
            NewNetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            Debug.Log("Client: Stopping client");
            NewNetworkManager.singleton.StopClient();
        }
        Debug.Log("Returning to lobby");
        // Update UI or load lobby scene here if needed
    }

    #endregion


}
