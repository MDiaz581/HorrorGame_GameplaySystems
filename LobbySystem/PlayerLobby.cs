using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public enum CharacterType { None, Survivor, Monster }

public class PlayerLobby : NetworkBehaviour
{
    [SyncVar] public CharacterType selectedCharacter = CharacterType.None;

    // Called when the player selects a character from the UI
    [Command]
    public void CmdSelectCharacter(CharacterType characterType)
    {
        selectedCharacter = characterType;
    }
}
