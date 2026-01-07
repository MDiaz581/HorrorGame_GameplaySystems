using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerManager : NetworkBehaviour
{
    public GameObject playerEntryPrefab;
    public Transform playerListParent; // The container (Vertical Layout Group recommended)

    public Dictionary<int, GameObject> PlayerEntry = new Dictionary<int, GameObject>();


    public void OnEnable()
    {
        //NewNetworkManager.serverClientConnect += CmdAddPlayer;
    }

    public void OnDisable()
    {
        //NewNetworkManager.serverClientConnect -= CmdAddPlayer;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    [Command]
    void CmdAddPlayer(NetworkConnectionToClient conn)
    {
        int ID = conn.connectionId;
        RPCAddPlayer(ID);
    }

    [ClientRpc]
    void RPCAddPlayer(int connID)
    {
        Debug.Log("adding Player");
        GameObject entryObj = Instantiate(playerEntryPrefab, playerListParent);
        var entryUI = entryObj.GetComponent<PlayerListCard>();

        // Get player's Steam name
        string playerName = $"{connID}";
        entryUI.SetPlayerName(playerName);

        //entryUI.connID = connID;

        PlayerEntry.Add(connID, entryObj);
    }
}

