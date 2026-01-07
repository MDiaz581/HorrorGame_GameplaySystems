using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Unity.Mathematics;
using UnityEngine;

public class DoorBehavior : InteractableBehavior
{
    [SerializeField]
    [TextArea(2, 100)]
    private string DoorInformation = "Door Description: The doorbehavior script takes its pivot point to calculate the rotation. In order to modify the pivot point just move this script's object to the pivot location.";
    public bool b_reverseDireciton = false;
    public enum DoorState
    {
        Unlocked,
        Locked
    }
    
    [SyncVar]
    public DoorState doorState;

    public float swingSpeed = 150f;
    public float maxSwingAngle = 90f;
    [SyncVar]
    public float currentSwingAngle = 0f;
    //public float initialSwingAngle; // used to keep track of open/door close audio

    [SyncVar]
    private quaternion initialDoorRotation;
    public Transform framePosition;
    public Transform oppositePivot;
    public AudioClip sfx_doorOpen;
    public AudioClip sfx_doorClose;
    public AudioClip sfx_doorLocked;
    public AudioClip sfx_doorUnlocked;
    public AudioClip sfx_doorSlam;
    public AudioClip sfx_doorSlamLocked;

    [SyncVar]
    private bool isOpenSoundPlayed = false;
    [SyncVar]
    private bool isCloseSoundPlayed = true;

    private Coroutine slamCoroutine;

    public float slamSpeed = 5f;

    // Start is called before the first frame update
    void Start()
    {
        // Initialize the door's rotation at the start
        initialDoorRotation = transform.localRotation;
        if (isServer)
        {
            InitializePosition();
        }
        ChangeInteractSymbol();
    }

    private void ChangeInteractSymbol()
    {
        if (interactSymbol != null)
        {
            switch (doorState)
            {
                case DoorState.Unlocked:
                    interactSymbol.symbolState = InteractSymbol.SymbolState.Interact;
                    break;
                case DoorState.Locked:
                    interactSymbol.symbolState = InteractSymbol.SymbolState.Locked;
                    break;
                default:
                    break;
            }
            interactSymbol.SetSymbol();
        }

    }
    [Server]
    private void InitializePosition()
    {
        transform.localRotation = initialDoorRotation * Quaternion.Euler(0f, currentSwingAngle, 0f);
    }

    // Update is called once per frame
    void Update()
    {
        if (b_interacting && Mathf.Abs(lookXvalue) > 0f && !b_value && doorState == DoorState.Unlocked)
        {
            SwingDoor();
        }

    }

    [Command(requiresAuthority = false)]
    public override void CmdOnInteract(bool interactionState)
    {
        base.CmdOnInteract(interactionState);

        // Important not to check if the player is interacting
        CmdSlamDoorOpen();

        if (b_interacting)
        {
            //If player isn't sprinting
            if (!b_value)
            {
                RpcDoorSoundOneShot();
            }

            if (doorState == DoorState.Locked)
            {
                RpcUnlockDoor();
            }
        }
    }

    [ClientRpc]
    private void RpcUnlockDoor()
    {
        if (playerTransform.GetComponent<PlayerBehavior>().int_keys > 0)
        {
            --playerTransform.GetComponent<PlayerBehavior>().int_keys;
            doorState = DoorState.Unlocked;

            ChangeInteractSymbol();

            if (sfx_doorUnlocked == null) return;
            audioClip = sfx_doorUnlocked;
            AudioManager.instance.PlaySound(audioClip, transform.position, false, .5f);
        }

    }

    [Command(requiresAuthority = false)]
    public override void CmdOnHoldInteract(float value)
    {
        base.CmdOnHoldInteract(value);
        lookXvalue = value;
        RpcDoorSound();

    }

    [ClientRpc]
    private void RpcDoorSound()
    {
        if (doorState == DoorState.Unlocked)
        {
            // Play open sound when door starts moving from the closed position
            if (Mathf.Abs(currentSwingAngle) >= 3f && !isOpenSoundPlayed && b_interacting)
            {
                if (sfx_doorOpen != null)
                {
                    audioClip = sfx_doorOpen;
                    AudioManager.instance.PlaySound(audioClip, transform.position, false, .5f);
                }


                isOpenSoundPlayed = true; // Prevent repeated playing
                isCloseSoundPlayed = false; // Reset close sound flag
            }
            // Play close sound when door is moving back to closed position
            if (Mathf.Abs(currentSwingAngle) <= 3f && !isCloseSoundPlayed && b_interacting)
            {
                if (sfx_doorClose != null)
                {
                    audioClip = sfx_doorClose;
                    AudioManager.instance.PlaySound(audioClip, transform.position, false, .5f);
                }

                isCloseSoundPlayed = true; // Prevent repeated playing
                isOpenSoundPlayed = false; // Reset open sound flag
            }
        }
    }

    [ClientRpc]
    private void RpcDoorSoundOneShot()
    {
        //Check if door is locked, player is not sprinting, and player has no keys.
        if (doorState == DoorState.Locked && !b_value && playerTransform.GetComponent<PlayerBehavior>().int_keys == 0)
        {
            if (sfx_doorLocked != null)
            {
                audioClip = sfx_doorLocked;
                AudioManager.instance.PlaySound(audioClip, transform.position, false, .5f);
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdSlamDoorOpen()
    {
        //Check for the fed bool from RpcGetBool.
        if (b_value)
        {
            RpcSlamDoorOpen();
        }
    }

    [ClientRpc]
    public void RpcSlamDoorOpen()
    {
        if (doorState == DoorState.Unlocked)
        {
            if (Mathf.Abs(currentSwingAngle) < 70 && sfx_doorSlam != null)
            {
                audioClip = sfx_doorSlam;
                AudioManager.instance.PlaySound(audioClip, transform.position, false);
            }
            // Stop any ongoing slam animation to avoid conflicts.
            if (slamCoroutine != null)
            {
                StopCoroutine(slamCoroutine);
            }
            isOpenSoundPlayed = true;

            // Start the slam animation coroutine.
            slamCoroutine = StartCoroutine(SlamOpenRoutine());
        }
        else if (doorState == DoorState.Locked)
        {
            if (sfx_doorSlamLocked)
            {
                audioClip = sfx_doorSlamLocked;
                AudioManager.instance.PlaySound(audioClip, transform.position, false);
            }
        }
        b_value = false;
    }

    // Coroutine to smoothly rotate the door using Lerp.
    private IEnumerator SlamOpenRoutine()
    {
        // Record the starting rotation.
        Quaternion initialRotation = transform.localRotation;

        // Target rotation: slam door to the maximum swing angle.

        Quaternion targetRotation;

        if (!b_reverseDireciton)
        {
            targetRotation = initialDoorRotation * Quaternion.Euler(0f, maxSwingAngle, 0f);
        }
        else
        {
            targetRotation = initialDoorRotation * Quaternion.Euler(0f, -maxSwingAngle, 0f);
        }


        float elapsedTime = 0f;

        // Lerp the rotation over time until the door is fully open.
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * slamSpeed;

            // Smoothly interpolate between initial and target rotation.
            transform.localRotation = Quaternion.Lerp(initialRotation, targetRotation, elapsedTime);

            yield return null; // Wait for the next frame.
        }

        // Ensure the door reaches the exact target rotation.
        transform.localRotation = targetRotation;

        if (!b_reverseDireciton)
        {
            currentSwingAngle = maxSwingAngle;
        }
        else
        {
            currentSwingAngle = -maxSwingAngle;
        }


        isCloseSoundPlayed = false;
    }


    // Maybe better to create an object in space that the door attempts to swing towards rather than creating this logic based swinging direction. 
    private void SwingDoor()
    {
        //Right now the player position is constantly updated due to interactable behavior we should only take it once and not updated it.
        // Get player's position relative to the door's position
        Vector3 playerPosition = playerTransform.position;
        Vector3 doorToPlayer = playerPosition - transform.position;
        Vector3 pivotToPlayer = playerPosition - oppositePivot.position;
        // Get door's forward and right directions (to determine sides)
        Vector3 doorForward = framePosition.forward;
        Vector3 doorRight = framePosition.right;
        Vector3 oppositePivotRight = oppositePivot.right;

        // Determine if the player is behind the door
        bool isPlayerBehindDoor = Vector3.Dot(doorToPlayer, doorForward) < 0;

        // Determine if the player is on the right side or left side of the door
        bool isPlayerOnRightSide = Vector3.Dot(doorToPlayer, doorRight) > 0;

        bool isPlayerOnOppositePivot = Vector3.Dot(pivotToPlayer, oppositePivotRight) > 0;

        // Set swing direction based on both front/behind and side information
        float swingAmount = lookXvalue * swingSpeed * Time.deltaTime;

        //if the hinge is the center point and the door is running downwards this is as follows
        //Top Right Quadrant
        if (!isPlayerBehindDoor && !isPlayerOnRightSide)
        {
            if (b_reverseDireciton)
            {
                swingAmount = -swingAmount;
            }
        }
        //Bottom Right Quadrant
        if (!isPlayerBehindDoor && isPlayerOnRightSide)
        {
            if (isPlayerOnOppositePivot && !b_reverseDireciton)
            {
                Debug.Log("Pivot");
                swingAmount = -swingAmount;
            }
        }
        //Top Left Quadrant
        if (isPlayerBehindDoor && !isPlayerOnRightSide)
        {
            if (b_reverseDireciton)
            {
                swingAmount = -swingAmount;
            }
        }
        //Bottom Left Quadrant
        if (isPlayerBehindDoor && isPlayerOnRightSide && !b_reverseDireciton)
        {
            swingAmount = -swingAmount;
        }

        // Accumulate the swing angle and clamp it within limits
        if (b_reverseDireciton)
        {
            currentSwingAngle = Mathf.Clamp(currentSwingAngle + swingAmount, -maxSwingAngle, 0);
        }
        else
        {
            currentSwingAngle = Mathf.Clamp(currentSwingAngle + swingAmount, 0, maxSwingAngle);
        }


        // Apply the rotation to the door
        transform.localRotation = initialDoorRotation * Quaternion.Euler(0f, currentSwingAngle, 0f);
    }

}
