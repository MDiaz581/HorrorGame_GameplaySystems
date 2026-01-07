using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ButtonBehavior : InteractableBehavior
{

    //Is there a way to do this but with overloads so if I can make this adjustable to anything?
    public event Action<int, GameObject> buttonAction; // Send an int value, but also send the gameobject that sent this value so we can affect it.

    [Tooltip("This is an int value that gets sent to be processed by other scripts.")]
    public int integerValue; // int value to send
    public Renderer[] rendererToTarget;

    [Tooltip("Prevents Button Spam")]
    public float f_buttonCooldown = 1f;
    private bool b_canActivate = true;
    public AudioClip sfx_buttonPressed;

    [Tooltip("Only vital for overlay objects to sound at proper location. Can be left null, if so it will default at position of button.")]
    public Transform soundPosition; // Vital for overlay objects to sound at proper location;

    private Coroutine buttonCooldown;


    private void OnEnable()
    {
        if(!b_canActivate)
        {
            buttonCooldown = StartCoroutine(ButtonCooldown());
        }
        
    }


    [Command(requiresAuthority = false)] // Allow all clients to call this command
    public override void CmdOnInteract(bool interactionState)
    {
        base.CmdOnInteract(interactionState);
        // This runs on the server, so we toggle the value here
        if (b_interacting)
        {
            if (interactableExtraEffects != null)
            {
                CmdInvokeExtraEffects();
            }

            RpcCallAction();
            //buttonAction?.Invoke(integerValue);
        }
    }

    [Command(requiresAuthority = false)]
    public override void OnOverlayInteract()
    {
        if (interactableExtraEffects != null)
        {
            CmdInvokeExtraEffects();
        }
        RpcCallAction();
    }


    [ClientRpc]
    public void RpcCallAction()
    {
        if (!b_canActivate) return;
        Debug.Log($"Sending: {integerValue}");
        buttonAction?.Invoke(integerValue, gameObject); // Invoke this on anything subscribe, the elevator for example. 
        buttonCooldown = StartCoroutine(ButtonCooldown()); // Causes an error in multiplayer as it cannot be called while other clients have this inactive. Overlay Objects cause this Inactive Error.

        if(sfx_buttonPressed != null)
        {
            //Check where the sound should come from. 
            if(soundPosition == null)
            {
                AudioManager.instance.PlaySound(sfx_buttonPressed, transform.position, false, 0.5f);
            }
            else 
            {
                AudioManager.instance.PlaySound(sfx_buttonPressed, soundPosition.position, false, 0.5f);
            }
            
        }

        
    }


    public IEnumerator ButtonCooldown()
    {
        b_canActivate = false;
        yield return new WaitForSeconds(f_buttonCooldown);
        b_canActivate = true;
    }
}
