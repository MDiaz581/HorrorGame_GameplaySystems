using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class AddNetworkManager : MonoBehaviour
{
    public GameObject networkManager;

    // Start is called before the first frame update
    void Start()
    {
        if (NetworkManager.singleton == null)
        {
            Debug.Log("No NetworkManager found, creating one");
            Instantiate(networkManager);
        }
    }
}
