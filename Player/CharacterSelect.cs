using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class CharacterSelect : NetworkBehaviour
{
    [SerializeField] private GameObject characterSelectDisplay = default;
    [SerializeField] private Character[] characters = default;
    private int currentCharacterIndex = default;
    private List<GameObject> characterInstances = new List<GameObject>();

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

}
