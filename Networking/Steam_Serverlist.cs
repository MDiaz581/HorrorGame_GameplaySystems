using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;


public class Steam_Serverlist : MonoBehaviour
{
    [Header("Callbacks")]
    protected CallResult<LobbyMatchList_t> lobbyMatchList;

    private const int int_maxPlayers = 5;


    [Header("UI Components")]
    public GameObject lobbyEntryPrefab;
    public Transform lobbyListParent;
    public TMP_Text emptyBrowserText;
    public GameObject joiningUI;
    public GameObject lobbyListUI;


    #region Initialization
    void Start()
    {
        if (!SteamManager.Initialized) return;
        lobbyMatchList = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
    }
    #endregion



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
        SteamMatchmaking.AddRequestLobbyListNumericalFilter("PlayerCount", 0, ELobbyComparison.k_ELobbyComparisonGreaterThan); //Prevents players from seeing empty and most likely null lobbies.

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
                SteamMatchmaking.JoinLobby(capturedID); // Request to join the lobby, read join lobby for more info.
                joiningUI.SetActive(true);
                lobbyListUI.SetActive(false);
            });
        }
    }
    #endregion

    #region Ping

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
