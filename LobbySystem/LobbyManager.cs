using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;
using TMPro;

public class LobbyManager : NetworkBehaviour
{
    public GameObject playerCard;
    public GameObject testPrefab;
    public PlayerListCard playerCardPrefab;
    public Transform playerCardParent;
    public List<GameObject> playerList = new List<GameObject>();
    public List<GameObject> playerDataList = new List<GameObject>();
    public TMP_Text consoleText;


    [SerializeField] private GameObject realPlayerPrefab;



    public void OnEnable()
    {
        NewNetworkManager.addingPlayer += PlayerJoined;
        NewNetworkManager.clientDisconnect += PlayerLeft;
        Steam_HostInformation.PlayerJoined += ServerOnPlayerJoined;
    }

    public void OnDisable()
    {
        NewNetworkManager.addingPlayer -= PlayerJoined;
        NewNetworkManager.clientDisconnect -= PlayerLeft;
        Steam_HostInformation.PlayerJoined -= ServerOnPlayerJoined;
    }

    //When the player joins we get direct reference of the player through the network manager, with that we can add it to a list.
    //This is only called on the server.
    [Server]
    public void PlayerJoined(GameObject player)
    {
        playerList.Add(player); //Add the player object to the list, this game object we recieve from the function mantains all the information from GetPlayerName.cs and the NetworkIdentity. (used in the player list card to kick)
        StartCoroutine(DelayPlayerList()); //Start the coroutine
    }

    //When a player leaves just recreate the player list with the new information.
    [Server]
    public void PlayerLeft()
    {
        StartCoroutine(DelayPlayerList());
    }

    //Delays the action to allow all information to be initialized
    public IEnumerator DelayPlayerList()
    {
        yield return new WaitForSeconds(0.5f);
        ServerCreatePlayerlist(); //Create the playerlist
    }


    //This creates the player list it runs a client RPC for each player it has and feeds said client RPC the information of the player.
    //Because this is server side specific it only utilizes information the server has not what the client has.
    [Server]
    public void ServerCreatePlayerlist()
    {
        RpcDeleteCards();
        Debug.Log($"Server Button Clicked");
        for (int i = 0; i < playerList.Count; i++)
        {
            if (playerList[i] == null)
            {
                playerList.RemoveAt(i);
                i--; // step back so we don’t skip the next element
                continue;
            }

            RpcRecieved(i + 1, playerList[i]); // send correct sequence
        }
    }

    [ClientRpc]
    public void RpcRecieved(int i, GameObject player) //Every client creates an instantiated player card with the information fed by the server.
    {
        GetPlayerName getPlayerInformation = player.GetComponent<GetPlayerName>(); //Grab the player's information from their informational script.

        GameObject customCard = Instantiate(playerCard, playerCardParent); //Instatiate the card, since this is all client side no need for syncing.

        customCard.GetComponent<PlayerListCard>().SetInfo(getPlayerInformation.steamName, getPlayerInformation.playerObject); //Set the card's information

        customCard.transform.SetParent(playerCardParent); //Ensures the card's parent is the context box.

        playerDataList.Add(customCard); // Add to a holder data list so we can delete the player card when needed;

        //consoleText.text = $"Creating: {i} for {player}";
        //Debug.Log($"Creating: {i} for {player}");
    }

    //This runs before anything we just ensure we clean up the player list before creating a new one.
    [ClientRpc]
    public void RpcDeleteCards()
    {
        //If there's already anything already made delete it.
        foreach (var player in playerDataList)
        {
            Destroy(player);
        }
        playerDataList.Clear(); //then clear the list
    }



    public CSteamID SteamID(CSteamID steamID)
    {
        Debug.LogWarning("Player Joined " + SteamFriends.GetFriendPersonaName(steamID));
        return steamID;
    }


    [Server]
    public void ServerOnPlayerJoined(CSteamID steamID)
    {

        if (!isServer) return;
        RpcOnPlayerJoined(steamID);
    }

    [ClientRpc]
    public void RpcOnPlayerJoined(CSteamID steamID)
    {
        Debug.LogWarning("Player Joined " + SteamFriends.GetFriendPersonaName(steamID));
    }



    public void StartGame()
    {
        if (isServer)
        {
            NetworkManager.singleton.ServerChangeScene("School"); // Load the main game scene
        }
    }


}
