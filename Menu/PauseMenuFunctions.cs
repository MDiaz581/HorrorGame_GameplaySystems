using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PauseMenuFunctions : MonoBehaviour
{

    public NetworkManager manager;

    void Start()
    {
        StartCoroutine(DelayedManagerSearch());
    }

    public IEnumerator DelayedManagerSearch()
    {
        yield return new WaitForSeconds(0.5f);
        manager = GameObject.FindWithTag("NetworkManager").GetComponent<NetworkManager>();
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game");
        Application.Quit();
    }

    public void SettingsMenu()
    {

    }

    public void Disconnect()
    {
         if (NetworkClient.isConnected)
            {
                manager.StopClient();
                if(NetworkServer.active)
                {
                    AudioManager.instance.StopAllSounds();
                    manager.StopHost();
                }
                
            }

            

    }
}
