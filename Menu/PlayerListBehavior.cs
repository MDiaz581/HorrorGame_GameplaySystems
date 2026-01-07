using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class PlayerListBehavior : NetworkBehaviour
{
    public GameObject playerEntryPrefab; // Prefab with a Text component
    public Transform playerListContainer; // The parent UI element where entries will be added

    void Start()
    {
        if (GameManager.gmInstance != null)
        {
            
        }
    }
}
