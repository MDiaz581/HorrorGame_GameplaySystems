using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class KeyItemBehavior : InteractableBehavior
{
    public enum KeyItemType
    {
        Key,
        Fuse
    }
    public KeyItemType keyItemType;

    public AudioClip sfx_pickup;

    [Command(requiresAuthority = false)]
    public override void CmdOnInteract(bool interactionState)
    {
        base.CmdOnInteract(interactionState);
        if (b_interacting)
        {
            RpcAddValue();
        }
    }

    [ClientRpc]
    private void RpcAddValue()
    {
        switch (keyItemType)
        {
            case KeyItemType.Key:
                ++playerTransform.GetComponent<PlayerBehavior>().int_keys;               
                break;
            case KeyItemType.Fuse:
                ++playerTransform.GetComponent<PlayerBehavior>().int_fuses;
                break;
            default:
                break;
        }
        if(sfx_pickup != null)
        {
            AudioManager.instance.PlaySound(sfx_pickup, transform.position, false, 0.5f);
        }
        gameObject.SetActive(false);
    }
}
