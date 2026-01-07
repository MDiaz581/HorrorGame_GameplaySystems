using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;



public class Steam_JoinRoom : MonoBehaviour
{

    public static CSteamID CurrentLobbyID { get; private set; }

    [Header("Callbacks")]

    private static bool callbackRegistered = false; //Extra catch to ensure callback isn't registered twice
    protected Callback<GameLobbyJoinRequested_t> joinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;


    #region Initialization
    void Start()
    {
        if (!SteamManager.Initialized) return;

        if (!callbackRegistered)
        {
            joinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
            lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        }

    }

    void OnDestroy() //Cleanup callback methods
    {
        if (joinRequested != null)
        {
            joinRequested.Dispose();
            joinRequested = null;
        }

        if (lobbyEntered != null)
        {
            lobbyEntered.Dispose();
            lobbyEntered = null;
        }
    }
    #endregion

    #region Connecting to Lobby
    //This command tells Steam to join the specified lobby. Once you've joined successfully, another callback (OnLobbyEntered) will be triggered where you handle things like connecting to the host and syncing up your game state.
    //result.m_steamIDLobby is the CSteamID of the lobby your friend invited you to. It represents a specific lobby hosted by someone using Steam's networking. 
    //Created within OnLobbyCreated using CSteamID lobbyID = new CSteamID(result.m_ulSteamIDLobby);
    private void OnLobbyJoinRequested(GameLobbyJoinRequested_t result)
    {
        CSteamID lobbyID = result.m_steamIDLobby;

        // Get the lobby's metadata
        string playerCountStr = SteamMatchmaking.GetLobbyData(lobbyID, "PlayerCount");
        string maxPlayerCountStr = SteamMatchmaking.GetLobbyData(lobbyID, "MaxPlayerCount");

        int playerCount = 0;
        int maxPlayers = 0;

        int.TryParse(playerCountStr, out playerCount);
        int.TryParse(maxPlayerCountStr, out maxPlayers);

        if (playerCount < maxPlayers)
        {
            Debug.Log($"Joining lobby {lobbyID}, {playerCount}/{maxPlayers} players.");
            SteamMatchmaking.JoinLobby(lobbyID);
        }
        else
        {
            Debug.Log("Failed to connect, Server at Max Players");
            // TODO: show this in your UI instead of just Debug.Log
        }
    }

    private void OnLobbyEntered(LobbyEnter_t result)
    {

        Debug.Log("Lobby Entered");

        //This fetches custom data stored in the lobby — in our case, the "HostAddress" that was set when the lobby was created:
        string hostAddress = SteamMatchmaking.GetLobbyData((CSteamID)result.m_ulSteamIDLobby, "HostAddress");

        CurrentLobbyID = (CSteamID)result.m_ulSteamIDLobby;

        //Debug.Log($"Currently in: {CurrentLobbyID}");

        //This tells Mirror: Hey, the IP (or SteamID in this case) of the server you should connect to is right here.
        //Even though Mirror expects an IP string normally, when you're using FizzySteamworks, it hijacks the system — this string becomes the SteamID, and FizzySteamworks uses it to connect over Steam P2P.
        NewNetworkManager.singleton.networkAddress = hostAddress;

        NewNetworkManager.singleton.StartClient(); // start Mirror client

        SteamFriends.SetRichPresence("status", "In Game");

        //Update Player Count
        //int_playerCount++;
        //SteamMatchmaking.SetLobbyData(hostLobbyID, "PlayerCount", $"{int_playerCount}");
        //SetLobbyID?.Invoke((CSteamID)result.m_ulSteamIDLobby); // This is all local information its never sent to the server itself.
    }
    #endregion
}
