using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using Steamworks;
using System;

public class Steam_HostInformation : MonoBehaviour
{

    /* 
    This script is specifically for managing information after hosting a server.
    */
    [Header("Callbacks")]
    private bool callbackRegistered = false; //Extra catch to ensure callback isn't registered twice

    protected Callback<LobbyChatUpdate_t> lobbyChatUpdate;
    //protected Callback<LobbyChatMsg_t> lobbyChatMsgCallback;

    #region Events
    public static event Action<string> updateConsole;
    public static event Action<CSteamID> PlayerJoined;
    public static event Action<CSteamID> PlayerLeft;

    #endregion

    #region Initialization
    void Start()
    {
        if (!SteamManager.Initialized) return;

            if (!callbackRegistered)
        {
        lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        //lobbyChatMsgCallback = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMessage);
        }
    }

        void OnDestroy() //Cleanup callback methods
    {
        if (lobbyChatUpdate != null)
        {
            lobbyChatUpdate.Dispose();
            lobbyChatUpdate = null;
        }

        /*
        if (lobbyChatMsgCallback != null)
        {
            lobbyChatMsgCallback.Dispose();
            lobbyChatMsgCallback = null;
        }
        */
    }
    #endregion


    // When the player joins they technically join the chat it's the same as joining the lobby.
    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        CSteamID currentLobbyID = (CSteamID)callback.m_ulSteamIDLobby;

        CSteamID userChanged = (CSteamID)callback.m_ulSteamIDUserChanged; // The user that has changed.

        EChatMemberStateChange stateChange = (EChatMemberStateChange)callback.m_rgfChatMemberStateChange;

        CSteamID hostID = SteamMatchmaking.GetLobbyOwner(currentLobbyID);

        //If player has entered the room 
        if (stateChange.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeEntered))
        {
            ModifyPlayerCount(currentLobbyID, 1);

            Debug.Log($"Steam user: {userChanged} has entered");

            PlayerJoined?.Invoke(userChanged);
        }
        //Else if the player has either left or disconnect. 
        else if (stateChange.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeLeft) ||
                 stateChange.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeDisconnected) || stateChange.HasFlag(ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None))
        {
            if (userChanged == hostID)
            {
                Debug.LogWarning("Host has left the lobby!");

                // Optional: Notify players, then leave
                updateConsole("The host has left. You are being returned to the main menu.");

                // Load main menu scene or disconnect from Mirror
                NetworkManager.singleton.StopClient();

                return;
            }

            ModifyPlayerCount(currentLobbyID, -1);
            PlayerLeft?.Invoke(userChanged);
            Debug.LogError("Player has Left: " + SteamFriends.GetFriendPersonaName(userChanged));
        }
    }

/*
    private void OnLobbyChatMessage(LobbyChatMsg_t callback)
    {
        CSteamID currentLobbyID = (CSteamID)callback.m_ulSteamIDLobby;

        CSteamID sender = (CSteamID)callback.m_ulSteamIDUser;
        byte[] data = new byte[4096];
        EChatEntryType chatEntryType;
        int dataSize = SteamMatchmaking.GetLobbyChatEntry(
            (CSteamID)callback.m_ulSteamIDLobby,
            (int)callback.m_iChatID,
            out CSteamID chatter,
            data,
            data.Length,
            out chatEntryType
        );

        string message = System.Text.Encoding.UTF8.GetString(data, 0, dataSize);

        if (message.StartsWith("kick:"))
        {
            ulong targetSteamID = ulong.Parse(message.Substring(5));
            if (SteamUser.GetSteamID().m_SteamID == targetSteamID)
            {
                string leavingMsg = $"leaving:{SteamUser.GetSteamID().m_SteamID}";
                byte[] leavingBytes = System.Text.Encoding.UTF8.GetBytes(leavingMsg + '\0');
                SteamMatchmaking.SendLobbyChatMsg(currentLobbyID, leavingBytes, leavingBytes.Length);

                updateConsole("You have been kicked from the lobby.");

                //Debug.Log();

                // Optional: Return to Main Menu scene
            }
        }

        if (message.StartsWith("leaving:"))
        {
            ulong leavingID = ulong.Parse(message.Substring(8));
            PlayerLeft.Invoke(new CSteamID(leavingID));
        }
    }
*/
    public void ModifyPlayerCount(CSteamID lobbyID, int value)
    {
        string playerCountStr = SteamMatchmaking.GetLobbyData(lobbyID, "PlayerCount");

        Debug.Log($"Player read as: {playerCountStr}");

        int playerCount = 0;

        int.TryParse(playerCountStr, out playerCount);

        Debug.Log($"Player Counts should be: {playerCount}");

        playerCount = playerCount + value;

        Debug.Log($"Player Counts added now: {playerCount}");
        SteamMatchmaking.SetLobbyData(lobbyID, "PlayerCount", $"{playerCount}");
    }
}
