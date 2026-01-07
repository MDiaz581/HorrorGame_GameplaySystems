using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Mirror;
using System;
using Unity.VisualScripting;

public class GetPlayerName : NetworkBehaviour
{
    [SyncVar] public string steamName;
    [SyncVar] public int idValue = -1;
    [SyncVar] public ulong mySteamID;

    public GameObject playerObject;

    public static event Action<GetPlayerName> sendPlayerName;

    public static event Action testSend;

    public void Awake()
    {
        playerObject = gameObject;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        //LobbyManager.instance.AddPlayerCard(this);        
    }


    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        if (isLocalPlayer)
        {
            Debug.LogError("Sending Info");
            string localName = SteamFriends.GetPersonaName();

            CSteamID steamID = SteamUser.GetSteamID();
            mySteamID = steamID.m_SteamID;

            CmdSendPlayerInfo(localName, playerObject);
            CmdSendSteamID(mySteamID);
        }
    }

    [Command]
    public void CmdSendPlayerInfo(string name, GameObject player)
    {
        steamName = name;
        idValue = player.GetComponent<NetworkIdentity>().connectionToClient.connectionId;  
        Debug.Log($"Steam Name Recieved, Client Name: {steamName} with {player}");
    }

    [Command]
    public void CmdSendSteamID(ulong steamId)
    {
        mySteamID = steamId;
    }

}
