using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;

public class PlayerListManager : MonoBehaviour
{
    public GameObject playerEntryPrefab;
    public Transform playerListParent; // The container (Vertical Layout Group recommended)
    private Dictionary<CSteamID, GameObject> playerEntries = new Dictionary<CSteamID, GameObject>();
    private CSteamID currentLobbyID;
    public GameObject serverListMenu;
    public GameObject errorBoxMenu;
    public GameObject lobbyMenu;

    void OnEnable()
    {
        SteamConnector.SetLobbyID += GetCurrentLobbyID;
        SteamConnector.PlayerJoined += OnPlayerJoined;
        SteamConnector.PlayerLeft += OnPlayerLeft;
    }
    void OnDisable()
    {
        SteamConnector.SetLobbyID -= GetCurrentLobbyID;
        SteamConnector.PlayerJoined -= OnPlayerJoined;
        SteamConnector.PlayerLeft -= OnPlayerLeft;
    }

    public void GetCurrentLobbyID(CSteamID lobbyID)
    {
        currentLobbyID = lobbyID;

        // Clear old entries first (safe for lobby switch)
        foreach (var entry in playerEntries.Values)
        {
            Destroy(entry);
        }
        playerEntries.Clear();

        RefreshPlayerList();
    }

    public void OnPlayerJoined(CSteamID steamID)
    {
        if (playerEntries.ContainsKey(steamID))
            return; // Already added

        GameObject entryObj = Instantiate(playerEntryPrefab, playerListParent);
        var entryUI = entryObj.GetComponent<PlayerListCard>();

        // Get player's Steam name
        string playerName = SteamFriends.GetFriendPersonaName(steamID);
        entryUI.SetPlayerName(playerName);

        //entryUI.steamID = steamID;

        CSteamID hostID = SteamMatchmaking.GetLobbyOwner(currentLobbyID);

        CSteamID mySteamID = SteamUser.GetSteamID();
        bool amIHost = mySteamID == hostID;

        // Show kick button only if:
        // I am host AND this player is NOT me
        bool showKickButton = amIHost && (steamID != mySteamID);
        entryUI.kickButton.SetActive(showKickButton);
        entryUI.kickButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                Debug.LogError("Clicked for Kick");

                //KickPlayerFromLobby(entryUI.steamID);
            });

        playerEntries.Add(steamID, entryObj);
    }

    public void OnPlayerLeft(CSteamID steamID)
    {
        Debug.Log($"[PlayerLeft] Removing player with SteamID: {steamID}");
        if (playerEntries.TryGetValue(steamID, out GameObject entryObj))
        {
            Destroy(entryObj);
            playerEntries.Remove(steamID);
            Debug.Log($"[PlayerLeft] Removed player entry for: {steamID}");
        }
        else
        {
            Debug.LogWarning($"[PlayerLeft] Tried to remove missing player entry: {steamID}");
        }
    }

    private void RefreshPlayerList()
    {
        int numMembers = SteamMatchmaking.GetNumLobbyMembers(currentLobbyID);

        for (int i = 0; i < numMembers; i++)
        {
            CSteamID memberID = SteamMatchmaking.GetLobbyMemberByIndex(currentLobbyID, i);
            OnPlayerJoined(memberID);
        }
    }
}
