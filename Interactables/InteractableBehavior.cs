using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;

public class InteractableBehavior : NetworkBehaviour
{
    
    [Header("Player Information")]
    [SyncVar]
    public bool b_value;

    [HideInInspector]
    [SyncVar]  // Syncing the interaction state to all clients
    public bool b_interacting;

    [HideInInspector]
    [SyncVar]
    public float lookXvalue;

    //[HideInInspector]
    public Transform playerTransform;
    //[HideInInspector]
    public Vector3 grabPointPosition; //Has to be converted to a Vector3 as transforms cannot be synced unless it has a network identity even if child of an object with network identity.

    [Header("Audio")]
    private AudioSource audioLoop;

    [HideInInspector]
    public AudioClip audioClip;

    [Header("Extras")]
    public InteractableExtraEffects interactableExtraEffects;

    public event Action<InteractableExtraEffects> extraAction;

    public InteractSymbol interactSymbol;

#region Interaction Methods
    // Command to interact (executed on the server)
    [Command(requiresAuthority = false)]
    public virtual void CmdOnInteract(bool interactionState)
    {
        // Update interaction state and notify all clients
        b_interacting = interactionState;
        RpcOnInteract(interactionState);
    }

    // ClientRpc to update clients
    [ClientRpc]
    public void RpcOnInteract(bool newInteractionState)
    {
        // Update the interaction state on all clients
        b_interacting = newInteractionState;
        //Debug.Log("b_interacting = " + b_interacting);
    }

    [Command(requiresAuthority = false)]
    public virtual void CmdOnHoldInteract(float value)
    {
        lookXvalue = value;

        RpcOnHoldInteract(lookXvalue);
    }

    [ClientRpc]
    public void RpcOnHoldInteract(float newValue)
    {
        lookXvalue = newValue;

    }

    public virtual void OnOverlayInteract()
    {
        // This is for overlay objects to invoke commands. 
    }

#endregion

#region PlayerInformation
    [Command(requiresAuthority = false)]
    public void CmdGetPlayerTransform(Transform transform)
    {
        playerTransform = transform;
        RpcUpdatePlayerTransform(playerTransform);
    }

    [ClientRpc]
    public void RpcUpdatePlayerTransform(Transform newTransform)
    {
        playerTransform = newTransform;
    }

    [Command(requiresAuthority = false)]
    public void CmdGetBool(bool value)
    {
        b_value = value;
        RpcGetBool(b_value);
    }

    [ClientRpc]
    public void RpcGetBool(bool newValue)
    {
        b_value = newValue;
    }
    
    [Command(requiresAuthority = false)]
    public void CmdGetGrabPoint(Vector3 position)
    {
        grabPointPosition = position;
        RpcUpdateGrabPoint(grabPointPosition);
    }

    [ClientRpc]
    public void RpcUpdateGrabPoint(Vector3 newPosition)
    {
        grabPointPosition = newPosition;
    }
    
#endregion

    #region Sound Information
    /**********************************************************************
    This section is null and void unless used only to play a sound, either way
    calling the Audiomanager is much more flexible than calling a sound command. 
    **********************************************************************/

    // Command without optional parameter
    [Command(requiresAuthority = false)]
    public virtual void CmdPlaySound(Vector3 position, bool isLooping, float volumeAdjustment)
    {
        RpcPlaySound(position, isLooping, volumeAdjustment);
    }

    // Overloaded version of CmdPlaySound that defaults to volumeAdjustment = 1f
    [Command(requiresAuthority = false)]
    public virtual void CmdPlaySound(Vector3 position, bool isLooping)
    {
        CmdPlaySound(position, isLooping, 1f); // Call the full method with default value
    }

    // ClientRpc without optional parameter
    [ClientRpc]
    public void RpcPlaySound(Vector3 position, bool isLooping, float volumeAdjustment)
    {
        if (audioClip != null)
        {
            if (!isLooping)
            {

                AudioManager.instance.PlaySound(audioClip, position, isLooping, volumeAdjustment);


            }
            else
            {

                audioLoop = AudioManager.instance.PlaySound(audioClip, position, isLooping);
            }
        }
        else 
        {
            Debug.LogError("Audio Clip not Found");
        }
    }

    // Overloaded version of RpcPlaySound that defaults to volumeAdjustment = 1f
    [ClientRpc]
    public void RpcPlaySound(Vector3 position, bool isLooping)
    {
        RpcPlaySound(position, isLooping, 1f); // Call the full method with default value
    }

    [Command(requiresAuthority = false)]
    public virtual void CmdStopSound()
    {
        RpcStopSound();
    }

    [ClientRpc]
    public void RpcStopSound()
    {
        AudioManager.instance.StopLoopingSound(audioLoop);
    }

#endregion

#region Extra Effects
    [Command(requiresAuthority = false)]
    public virtual void CmdInvokeExtraEffects()
    {
        if (interactableExtraEffects != null)
        {
            RpcInvokeExtraEffects();
        }
    }

    [ClientRpc]
    public void RpcInvokeExtraEffects()
    {
        if (interactableExtraEffects != null)
        {
            extraAction?.Invoke(interactableExtraEffects);
        }
    }
#endregion
}
