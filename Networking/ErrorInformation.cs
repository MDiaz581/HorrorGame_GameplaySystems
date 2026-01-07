using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror.FizzySteam;

public class ErrorInformation : MonoBehaviour
{
    public GameObject errorBox;
    public GameObject joinGameBox;
    public GameObject serverListMenu;
    public TMP_Text errorText;


    public void OnEnable()
    {
        NewNetworkManager.serverShutdown += SendServerShutdown;
        NextClient.NextClient_OnConnectionError += ServerTimeout;
        Steam_HostGame.LobbyCreationFailed += HostFailed;
    }
    public void OnDisable()
    {
        NewNetworkManager.serverShutdown -= SendServerShutdown;
        NextClient.NextClient_OnConnectionError -= ServerTimeout;
        Steam_HostGame.LobbyCreationFailed -= HostFailed;
    }
    private void ServerTimeout(string error)
    {
        serverListMenu.SetActive(false);
        joinGameBox.SetActive(false);
        errorBox.SetActive(true);
        errorText.text = error;
    }

    public void SendServerShutdown()
    {
        serverListMenu.SetActive(false);
        errorBox.SetActive(true);
        errorText.text = "Server shutdown...";
    }

    private void HostFailed()
    {
        serverListMenu.SetActive(false);
        joinGameBox.SetActive(false);
        errorBox.SetActive(true);
        errorText.text = "Failed to host Lobby";
    }
}
