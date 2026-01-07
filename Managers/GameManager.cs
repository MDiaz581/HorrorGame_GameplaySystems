using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public enum GamePhase
    {
        lobby,
        LoadTime,
        preGame,
        inGame
    }

    public static GameManager gmInstance { get; private set; }

    [SyncVar]
    public GamePhase currentPhase = GamePhase.lobby;

    [SyncVar]
    private float preRoundDuration = 10f;
    private float preRoundStartTime;

    public List<GameObject> humanPlayers = new List<GameObject>();

    public readonly SyncList<string> playerConnections = new SyncList<string>();



    private void Awake()
    {
        if (gmInstance == null)
        {
            gmInstance = this;
        }
        else
        {
            Destroy(gameObject); // Avoid duplicates          
        }
    }

    public void AddPlayer(int connectionId)
    {
        if (isServer) playerConnections.Add($"{connectionId}");
    }

    public void RemovePlayer(int connectionId)
    {
        if (isServer) playerConnections.Remove($"{connectionId}");
    }


    public override void OnStartServer()
    {
        StartPreRound();
    }

    [Server]
    public void StartPreRound()
    {
        //Debug.Log("Starting Preround??!");
        preRoundStartTime = Time.time;
        currentPhase = GamePhase.preGame;
        Invoke(nameof(StartGame), preRoundDuration);
    }

    private void StartGame()
    {
        currentPhase = GamePhase.inGame;
        Debug.Log("Game Started!");

        //FindandAddPlayers();

    }

    [Server]
    public void ServerProcessAttackedPlayer(GameObject playerObject)
    {
        Debug.LogWarning($"Server Parsing Info: Player {playerObject.name} was hit");
        RPCSendAttackInformation(playerObject);
    }

    [ClientRpc]
    public void RPCSendAttackInformation(GameObject playerObject)
    {
        Debug.LogWarning($"Clients Recieving Info: Player {playerObject.name} was hit");
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
