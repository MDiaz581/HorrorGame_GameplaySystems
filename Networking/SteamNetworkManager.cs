using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using Steamworks;
using TMPro;

public class SteamNetworkManager : NetworkManager
{

    [Header("Callbacks")]
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> joinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected CallResult<LobbyMatchList_t> lobbyMatchList;
    protected Callback<LobbyChatUpdate_t> lobbyChatUpdate;
    protected Callback<LobbyChatMsg_t> lobbyChatMsgCallback;

    [Header("Lobby Information")]
    private string userName = "Null";
    public string lobbyName;
    public enum LobbyType
    {
        Public,
        InviteOnly
    }
    public LobbyType lobbyType;
    private CSteamID hostLobbyID;
    private const int int_maxPlayers = 5;
    private int int_playerCount;


    [Header("UI Components")]
    public GameObject lobbyEntryPrefab;
    public Transform lobbyListParent;
    public TMP_Text emptyBrowserText;
    public GameObject lobbyUI;
    public GameObject lobbyListUI;
    public GameObject errorBoxUI; //Comes up during failure to join

    #region Events
    public static event Action<string> updateConsole;
    public static event Action LobbyCreated;
    public static event Action<CSteamID> SetLobbyID;
    public static event Action<CSteamID> PlayerJoined;
    public static event Action<CSteamID> PlayerLeft;
    #endregion


    public static event Action clientConnected;

    public override void Start()
    {
        base.Start();

        if (!SteamManager.Initialized) return;

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        //joinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
        //lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        //lobbyMatchList = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
        //lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        //lobbyChatMsgCallback = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMessage);


        userName = SteamFriends.GetPersonaName(); //Used for a default room name
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
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

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
            Debug.Log($"New Client Connected {NetworkClient.connection.connectionId}");

            NetworkClient.Ready();
        }

        clientConnected?.Invoke();

    }

    public override void OnClientDisconnect()
    {
        Debug.Log("Client Disconnect");
    }


    public void ClientInGame()
    {
        // This is directly taken from NetworkManagerHUD.cs
        if (NetworkClient.isConnected && !NetworkClient.ready)
        {
            // Sets client ready to ready
            NetworkClient.Ready();
            if (NetworkClient.localPlayer == null) NetworkClient.AddPlayer();

            lobbyUI.SetActive(false);
        }
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log("Adding Player");
        GameObject player = Instantiate(playerPrefab);
        player.name = $"{playerPrefab.name} [connId={conn.connectionId}]";
        NetworkServer.AddPlayerForConnection(conn, player);

        // Add players name and informationt to a list.
        // Interesting quirk, although the network manager is still active, it disappears during play. It may make it difficult to grab the list. Instead lets send it to the game manager. We can also make it a synclist.
        GameManager.gmInstance.AddPlayer(conn.connectionId);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (GameManager.gmInstance != null)
        {
            GameManager.gmInstance.RemovePlayer(conn.connectionId);
        }
        base.OnServerDisconnect(conn);
    }

    public override void OnServerChangeScene(string newSceneName)
    {
        base.OnServerChangeScene(newSceneName);
    }


    //Called By input field through onvaluechanged
    public void InputLobbyName(string inputText)
    {
        lobbyName = inputText;
    }
    public void InputLobbyType(int value)
    {
        lobbyType = (LobbyType)value;
        Debug.Log("Lobby Type now set to: " + lobbyType);
    }

    public void HostLobby()
    {
        //SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxPlayers);

        int_playerCount = 0;

        if (string.IsNullOrWhiteSpace(lobbyName))
        {
            lobbyName = $"{userName}'s Lobby";
        }

        switch (lobbyType)
        {
            case LobbyType.Public:
                SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, int_maxPlayers);
                Debug.Log("Starting Public Session");
                break;
            case LobbyType.InviteOnly:
                SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, int_maxPlayers);
                Debug.Log("Starting InviteOnly Session");
                break;
        }

        //SteamMatchmaking.SetLobbyType(lobbyID, ELobbyType.k_ELobbyTypePublic);
    }
    private void OnLobbyCreated(LobbyCreated_t result)
    {
        //Checks if the lobby creation was successful obtained from the LobbyCreated_t struct we named result. We check the m_eResult which is what holds that information and checks if it's equal to the appropriate enum in this case k_EResultOK
        if (result.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogWarning("Failed to create lobby.");
            return;
        }

        Debug.Log("Lobby created successfully!");

        updateConsole("Lobby created successfully!");

        string hostID = SteamUser.GetSteamID().ToString();

        //This is critical. It stores the host’s Steam ID in the lobby as a string value "HostAddress". That way, when a friend joins, they can read this info to know who to connect to.
        //SetLobbyData is what allows us to set the metadata of the steam lobby, like the Host address, Lobby name, Map, all stored within this lobby data.
        // (CSteamID)result.m_ulSteamIDLobby is basically Here's the Steam lobby we just created, now give me a Steam-friendly object for it.
        //"HostAddress" Can be named anything but this is what we're storing it tells clients who they should connect to. hostID is what we're storing under "HostAddress" which is the host's steamID set as a string.
        //Easiest way to do this is: SteamMatchmaking.SetLobbyData((CSteamID)result.m_ulSteamIDLobby, "HostAddress", hostID);
        //But I want more information for my lobby so:

        //These two lines are basically SteamMatchmaking.SetLobbyData((CSteamID)result.m_ulSteamIDLobby, "HostAddress", hostID);
        hostLobbyID = new CSteamID(result.m_ulSteamIDLobby);

        SteamMatchmaking.SetLobbyMemberLimit(hostLobbyID, int_maxPlayers);

        SteamMatchmaking.SetLobbyData(hostLobbyID, "HostAddress", hostID);

        //These are additional information we can store. anything within the last quotations can be changed with variables to match what we want it to be, ensuring we assign it to a string.
        SteamMatchmaking.SetLobbyData(hostLobbyID, "LobbyName", lobbyName);
        SteamMatchmaking.SetLobbyData(hostLobbyID, "Map", "Example");
        SteamMatchmaking.SetLobbyData(hostLobbyID, "GameMode", "Example");
        SteamMatchmaking.SetLobbyData(hostLobbyID, "MaxPlayerCount", $"{int_maxPlayers}");
        SteamMatchmaking.SetLobbyData(hostLobbyID, "PlayerCount", $"{int_playerCount}");

        SteamMatchmaking.SetLobbyData(hostLobbyID, "Game", "HorrorGame"); // Basic filter which is taken into account when refreshing lobby list. Playtesting specific just so players don't see and attempt to join other spacewar games.

        SteamMatchmaking.SetLobbyJoinable(hostLobbyID, true);

        StartCoroutine(WaitForPingLocationAndSetLobbyData());

        NewNetworkManager.singleton.StartHost(); // start Mirror host

        LobbyCreated?.Invoke();
    }

     #region Lobby List

    //This is called on button
    public void OnClickRefreshLobbyButton()
    {
        StartCoroutine(DelayedLobbyRefresh());
    }

    private void RefreshLobbyList()
    {
        foreach (Transform child in lobbyListParent)
            Destroy(child.gameObject);

        if (emptyBrowserText != null)
        {
            emptyBrowserText.text = "Searching for servers...";
        }

        SteamMatchmaking.AddRequestLobbyListResultCountFilter(50);
        SteamMatchmaking.AddRequestLobbyListStringFilter("Game", "HorrorGame", ELobbyComparison.k_ELobbyComparisonEqual); // prevents players from seeing anything without this string. Basically any game using appID 480
        SteamMatchmaking.AddRequestLobbyListNumericalFilter("PlayerCount", int_maxPlayers, ELobbyComparison.k_ELobbyComparisonLessThan); //Prevents players from seeing filled Lobbies.

        //Request all lobbies in this App ID.
        SteamAPICall_t handle = SteamMatchmaking.RequestLobbyList();

        lobbyMatchList.Set(handle); //This makes steam automatically call OnLobbyMatchList

        //Just incase rebuild the layout of the ServerList
        LayoutRebuilder.ForceRebuildLayoutImmediate(lobbyListParent.GetComponent<RectTransform>());
    }

    //When request LobbyList is made
    private void OnLobbyMatchList(LobbyMatchList_t result, bool failure)
    {
        if (failure || result.m_nLobbiesMatching == 0)
        {
            Debug.Log("No lobbies found.");
            if (emptyBrowserText != null)
            {
                emptyBrowserText.text = "No servers found...";
            }
            return;
        }

        if (emptyBrowserText != null)
        {
            emptyBrowserText.text = "";
        }

        for (int i = 0; i < result.m_nLobbiesMatching; i++)
        {
            //Grab the CSteamID of the lobby.
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);

            string name = SteamMatchmaking.GetLobbyData(lobbyID, "LobbyName");

            string maxPlayersCount = SteamMatchmaking.GetLobbyData(lobbyID, "MaxPlayerCount");
            string currentPlayers = SteamMatchmaking.GetLobbyData(lobbyID, "PlayerCount");
            string map = SteamMatchmaking.GetLobbyData(lobbyID, "Map");
            int estimatedPing = RetrievePingFromLobbyAsInt(lobbyID);

            //Instantiate the Server bar
            GameObject entryObj = Instantiate(lobbyEntryPrefab, lobbyListParent);
            //Grab its informational script and set the values
            entryObj.GetComponent<ServerBarInformation>().text_lobbyName.text = $"{name}";
            entryObj.GetComponent<ServerBarInformation>().text_playerCount.text = $"{currentPlayers} / {maxPlayersCount}";
            entryObj.GetComponent<ServerBarInformation>().text_ping.text = $"{estimatedPing}";
            //Grab its button component
            Button button = entryObj.GetComponent<Button>();
            //Grab its CSteamID might be redundant 
            CSteamID capturedID = lobbyID;
            //Set the button so it Joins the capturedID, enables the lobbyUI and disables the lobbyList;
            button.onClick.AddListener(() =>
            {
                SteamMatchmaking.JoinLobby(capturedID);
                lobbyUI.SetActive(true);
                lobbyListUI.SetActive(false);
            });

        }
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
                string pingLocStr = new string('\0', 256);
                SteamNetworkingUtils.ConvertPingLocationToString(ref pingLocation, out pingLocStr, 256);

                SteamMatchmaking.SetLobbyData(hostLobbyID, "PingLocation", pingLocStr.TrimEnd('\0'));

                yield break;
            }

            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

    }

    public int RetrievePingFromLobbyAsInt(CSteamID targetLobbyID)
    {
        string ping = SteamMatchmaking.GetLobbyData(targetLobbyID, "PingLocation");

        int estimatedPing = -1;

        if (!string.IsNullOrEmpty(ping))
        {
            SteamNetworkPingLocation_t remoteLocation;
            bool parsed = SteamNetworkingUtils.ParsePingLocationString(ping, out remoteLocation);
            //Debug.Log($"Parsing result: {parsed}");

            if (parsed)
            {
                estimatedPing = SteamNetworkingUtils.EstimatePingTimeFromLocalHost(ref remoteLocation);
                //Debug.Log($"Estimated ping: {estimatedPing}");
            }
            else
            {
                Debug.LogError("Failed to parse ping string");
            }
        }
        else
        {
            Debug.LogError("Ping string was null or empty");
        }

        return estimatedPing;
    }

    private IEnumerator DelayedLobbyRefresh()
    {
        //Debug.Log("Waiting for ping data to initialize...");

        SteamNetworkPingLocation_t pingLoc;

        float timeout = 5f;
        float elapsed = 0f;

        // Keep checking until we get a proper result (or timeout)
        while (elapsed < timeout)
        {
            if (SteamNetworkingUtils.GetLocalPingLocation(out pingLoc) > 0)
            {
                //Debug.Log("Ping location acquired. Requesting lobby list.");
                break;
            }

            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        if (elapsed >= timeout)
        {
            Debug.LogWarning("Timed out waiting for Steam ping region data. Requesting anyway...");
        }

        RefreshLobbyList();
    }

    #endregion

}
