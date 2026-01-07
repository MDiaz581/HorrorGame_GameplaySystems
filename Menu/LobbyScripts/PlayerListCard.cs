using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Steamworks;
using Mirror;

public class PlayerListCard : NetworkBehaviour
{
    public TMP_Text text_playerName;

    [SyncVar(hook = nameof(OnNameChanged))] public string steamName;

    //public CSteamID steamID;

    [SyncVar] public int connId = -1;

    [SyncVar] public GameObject playerGameObject;

    public GameObject kickButton;

    public void OnEnable()
    {
        //steamName = name;
        text_playerName.text = steamName;
    }

    public void SetPlayerName(string name)
    {
        text_playerName.text = name;
    }

    void OnNameChanged(string _, string newValue)
    {
        if (text_playerName != null)
        {
            text_playerName.text = newValue;
        }
    }

    public void SetInfo(string name, GameObject player)
    {
        steamName = name;
        text_playerName.text = steamName;
        playerGameObject = player;

        if (NetworkServer.active)
        {
          connId = player.GetComponent<NetworkIdentity>().connectionToClient.connectionId;  
        }
        
        //Debug.Log($"Converting player to player ID: {connId}");
    }

    private void Start()
    {
        if (NetworkServer.active && connId != 0)
        {
            kickButton.SetActive(true);
        }
    }

    [Server]
    public void KickPlayer()
    {

        if (NetworkServer.active)
        {

            NetworkIdentity kickTargetIdentity = playerGameObject.GetComponent<NetworkIdentity>();
            //TargetKickPlayer(kickTargetIdentity.connectionToClient); not necessary.

            kickTargetIdentity.connectionToClient.Disconnect();
        }
    }


    [TargetRpc]
    void TargetKickPlayer(NetworkConnectionToClient target)
    {
        target.Disconnect();

    }
}
