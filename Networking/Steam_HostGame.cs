using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using Steamworks;
using System;

public class Steam_HostGame : MonoBehaviour
{
    [Header("Callbacks")]
    private static bool callbackRegistered = false;
    protected Callback<LobbyCreated_t> lobbyCreated;

    [Header("Lobby Information")]
    private string userName = "Null";
    public string lobbyName;
    public enum LobbyType
    {
        Public,
        InviteOnly
    }
    public LobbyType lobbyType;
    public CSteamID hostLobbyID;
    private const int int_maxPlayers = 5;
    private int int_playerCount;

    private Coroutine SetPing;
    public static event Action LobbyCreated;
    public static event Action LobbyCreationFailed;
   

    #region Initialization
    void Start()
    {
        //netManager = GameObject.FindGameObjectWithTag("NetworkManager").GetComponent<NewNetworkManager>();
        if (!SteamManager.Initialized) return;

        if (!callbackRegistered)
        {
            lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        }
        userName = SteamFriends.GetPersonaName(); //Used for a default room name
    }

    void OnDestroy()
    {
        if (lobbyCreated != null)
        {
            lobbyCreated.Dispose();
            lobbyCreated = null;
        }
    }
    #endregion

    #region Create & Host Lobby
    public void HostLobby()
    {
        int_playerCount = 1;

        if (string.IsNullOrWhiteSpace(lobbyName))
        {
            lobbyName = $"{userName}'s Lobby";
        }

        switch (lobbyType)
        {
            case LobbyType.Public:
                SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, int_maxPlayers);
                //Debug.Log("Starting Public Session");
                break;
            case LobbyType.InviteOnly:
                SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, int_maxPlayers);
                //Debug.Log("Starting InviteOnly Session");
                break;
        }

    }

    //Called By input field through onvaluechanged
    public void InputLobbyName(string inputText)
    {
        lobbyName = inputText;
    }
    public void InputLobbyType(int value)
    {
        lobbyType = (LobbyType)value;
        //Debug.Log("Lobby Type now set to: " + lobbyType);
    }

    private void OnLobbyCreated(LobbyCreated_t result)
    {
        //Checks if the lobby creation was successful obtained from the LobbyCreated_t struct we named result. We check the m_eResult which is what holds that information and checks if it's equal to the appropriate enum in this case k_EResultOK
        if (result.m_eResult != EResult.k_EResultOK)
        {
            //updateConsole("Failed to create lobby.");
            LobbyCreationFailed?.Invoke(); //Send signal to Game that Lobby failed to create.
            Debug.LogWarning("Failed to create lobby.");
            return;
        }

        string hostID = SteamUser.GetSteamID().ToString();

        //This is critical. It stores the host’s Steam ID in the lobby as a string value "HostAddress". That way, when a friend joins, they can read this info to know who to connect to.
        //SetLobbyData is what allows us to set the metadata of the steam lobby, like the Host address, Lobby name, Map, all stored within this lobby data.
        // (CSteamID)result.m_ulSteamIDLobby is basically Here's the Steam lobby we just created, now give me a Steam-friendly object for it.
        //"HostAddress" Can be named anything but this is what we're storing it tells clients who they should connect to. hostID is what we're storing under "HostAddress" which is the host's steamID set as a string.
        //Easiest way to do this is: SteamMatchmaking.SetLobbyData((CSteamID)result.m_ulSteamIDLobby, "HostAddress", hostID);
        //But I want more information for my lobby so:

        //These two lines are basically SteamMatchmaking.SetLobbyData((CSteamID)result.m_ulSteamIDLobby, "HostAddress", hostID);
        hostLobbyID = new CSteamID(result.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyData(hostLobbyID, "HostAddress", hostID);

        SteamMatchmaking.SetLobbyMemberLimit(hostLobbyID, int_maxPlayers);

        //These are additional information we can store. anything within the last quotations can be changed with variables to match what we want it to be, ensuring we assign it to a string.
        SteamMatchmaking.SetLobbyData(hostLobbyID, "LobbyName", lobbyName);
        SteamMatchmaking.SetLobbyData(hostLobbyID, "Map", "Example");
        SteamMatchmaking.SetLobbyData(hostLobbyID, "GameMode", "Example");
        SteamMatchmaking.SetLobbyData(hostLobbyID, "MaxPlayerCount", $"{int_maxPlayers}");
        SteamMatchmaking.SetLobbyData(hostLobbyID, "PlayerCount", $"{int_playerCount}");

        SteamMatchmaking.SetLobbyData(hostLobbyID, "Game", "HorrorGame"); // Basic filter which is taken into account when refreshing lobby list. Playtesting specific just so players don't see and attempt to join other spacewar games.

        SteamMatchmaking.SetLobbyJoinable(hostLobbyID, true);

        NewNetworkManager.singleton.StartHost(); // start Mirror host

        if (SetPing == null)
            SetPing = StartCoroutine(WaitForPingLocationAndSetLobbyData());

        LobbyCreated?.Invoke();
    }
    #endregion

    #region Ping
    private IEnumerator WaitForPingLocationAndSetLobbyData()
    {

        SteamNetworkPingLocation_t pingLocation;

        float timeout = 10f;
        float elapsed = 0f;


        while (elapsed < timeout)
        {
            if (SteamNetworkingUtils.GetLocalPingLocation(out pingLocation) > 0)
            {
                //Debug.Log("Ping found");
                string pingLocStr = new string('\0', 256);
                SteamNetworkingUtils.ConvertPingLocationToString(ref pingLocation, out pingLocStr, 256);

                SteamMatchmaking.SetLobbyData(hostLobbyID, "PingLocation", pingLocStr.TrimEnd('\0'));

                yield return new WaitForSeconds(2f);
                NewNetworkManager.singleton.ServerChangeScene("LobbyScene"); //We're going to be changing the process, now we wait for the ping to initialize first before changing scene.

                yield break;
            }

            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

    }

    #endregion


}
