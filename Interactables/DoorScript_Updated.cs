using System.Collections;
using UnityEngine;
using Mirror;
using Unity.Mathematics;

public class DoorScript_Updated : InteractableBehavior
{

    //NOTE the angle the door is at within the model is very important, Why it's so important I'm not sure, but check the stall door compared to the wooden door. This script doesn't work 100% on the stall door.
    //In order to change this importance an empty gameobject is required, the empty gameobject holds everything including the box collider and excluding the mesh. have the mesh be a child angle appropriately and have the empty gameobject 
    //box collider match the child mesh. 
    public enum DoorState
    {
        Unlocked,
        Locked
    }
    [Header("Door Variables")]
    [SyncVar]
    public DoorState doorState;
    public float swingSpeed = 10f;
    public float maxSwingAngle = 90f;
    public float minSwingAngle = 0f;
    private float maxClampAngle;
    private float minClampAngle;
    private Quaternion inverseRotation;
    private Quaternion relativeRotation;
    private Quaternion minRotation;
    private Quaternion maxRotation;
    [SyncVar]
    private float f_doorAngle;
    [SyncVar]
    private quaternion initialDoorRotation;

    [Header("Sound Variables")]
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

    [Header("Door Slam")]
    public bool b_reverseDirection = false;
    public float slamSpeed = 5f;
    private Coroutine slamCoroutine;
    public bool b_enableDebug;

    private bool b_DoorMoving;


    #region Base Functions
    void Start()
    {
        //Quaternion trueAngle = transform.rotation * Quaternion.Euler(0, 90f, 0);
        inverseRotation = Quaternion.Inverse(transform.rotation); //Invert the current rotation.

        relativeRotation = (inverseRotation * transform.rotation) * Quaternion.Euler(0, 180, 0); //Multiply by the inversion to return 0. meaning no matter where it is positioned it'll = to 0 intitially, then multiply by 180 on the Y to set it to 180;
        //The reason why we set it to 180 is so that we have more wiggle room for the max and min clamp angle before it "wraps" and causes issues with the check.

        // Define clamp limits relative to the door’s resting position.
        minRotation = relativeRotation * Quaternion.Euler(0, -minSwingAngle, 0);
        maxRotation = relativeRotation * Quaternion.Euler(0, maxSwingAngle, 0);

        minClampAngle = NormalizeAngle((int)minRotation.eulerAngles.y);
        maxClampAngle = NormalizeAngle((int)maxRotation.eulerAngles.y);

        //Debug.Log($"Initial True Angle: {NormalizeAngle(relativeRotation.eulerAngles.y)} Min: {minClampAngle}, Max: {maxClampAngle}");

        initialDoorRotation = transform.localRotation;

    }

    // Update is called once per frame
    void Update()
    {
        if (b_interacting && !b_value && doorState == DoorState.Unlocked)
        {
            SwingDoor();
        }
    }
    #endregion

    #region Interactable Functions

    [Command(requiresAuthority = false)]
    public override void CmdOnInteract(bool interactionState)
    {
        base.CmdOnInteract(interactionState);
        //RpcOpenSound();

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

    [Command(requiresAuthority = false)]
    public override void CmdOnHoldInteract(float value)
    {
        base.CmdOnHoldInteract(value);
        lookXvalue = value;
        RpcDoorSound();
    }
    #endregion

    #region Swing Door
    public void SwingDoor()
    {
        //Calculated Direction
        Vector3 dir = (grabPointPosition - transform.position).normalized;
        dir.y = 0; //Zero out Y to prevent moving in the Y axis

        Quaternion lookRot = Quaternion.LookRotation(dir, Vector3.up); // Find the look rotation
        Quaternion relativeLookRotCheck = (inverseRotation * lookRot) * Quaternion.Euler(0, 90f, 0); // Apply relativity, this is for the clamp check;
        Quaternion finalRotation = lookRot * Quaternion.Euler(0, -90f, 0); // Offset so X is forward. This doesn't need relativity applied.

        //Get the current angle of the door. This is for the clamp check.
        Quaternion trueRotation = (inverseRotation * transform.rotation) * Quaternion.Euler(0, 180, 0);

        Quaternion doorRotation = (inverseRotation * transform.rotation);

        f_doorAngle = NormalizeAngle2(doorRotation.eulerAngles.y);
        //Normalize all values so they're always the same value across the board, no matter the direction its facing or if the degree of rotation exceeds 360. This is important for the if statement.
        //float normalizedLookRot = NormalizeAngle(lookRot.eulerAngles.y);
        float normalizedRelativeLookRot = NormalizeAngle(relativeLookRotCheck.eulerAngles.y);
        float normalizedTrueAngle = NormalizeAngle(trueRotation.eulerAngles.y);

        float normalizedFinalRotation = NormalizeAngle(finalRotation.eulerAngles.y);

        //If within the max and min angle or if the look rotation is within the angles as well. 
        if (normalizedTrueAngle <= maxClampAngle && normalizedTrueAngle >= minClampAngle || normalizedRelativeLookRot < maxClampAngle && normalizedRelativeLookRot > minClampAngle)
        {
            Quaternion localRotation = transform.localRotation;
            // Target rotation: slam door to the maximum swing angle.
            Quaternion targetRotation;
        
            if (normalizedRelativeLookRot > maxClampAngle) // If max clamp is exceeded default the target rotation to it's max clamp.
            {
                Debug.Log("exceeded limit");
                targetRotation = initialDoorRotation * Quaternion.Euler(0f, maxSwingAngle, 0f);
                transform.localRotation = Quaternion.Lerp(localRotation, targetRotation, Time.deltaTime * swingSpeed);
            }
            else if (normalizedRelativeLookRot < minClampAngle) // If min clamp is exceeded default the target rotation to it's min clamp.
            {   
                Debug.Log("exceeded limit");      
                targetRotation = initialDoorRotation * Quaternion.Euler(0f, -minSwingAngle, 0f);
                transform.localRotation = Quaternion.Lerp(localRotation, targetRotation, Time.deltaTime * swingSpeed);
            }
            else
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, finalRotation, Time.deltaTime * swingSpeed);
            }

        }

        //Debug to ensure all angles align.
        Debug.Log($"True Angle:{normalizedTrueAngle}, RelativeLookRot Angle: {normalizedRelativeLookRot} Normalized Final Rotation: {normalizedFinalRotation}");
        
    }

    float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    float NormalizeAngle2(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }
    #endregion

    #region Door Slam
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
            if (Mathf.Abs(f_doorAngle) < 70 && sfx_doorSlam != null)
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
        Debug.Log("Starting Coroutine");
        // Record the starting rotation.
        Quaternion localRotation = transform.localRotation;

        // Target rotation: slam door to the maximum swing angle.
        Quaternion targetRotation;

        if (!b_reverseDirection)
        {
            targetRotation = initialDoorRotation * Quaternion.Euler(0f, maxSwingAngle, 0f);
        }
        else
        {
            targetRotation = initialDoorRotation * Quaternion.Euler(0f, -minSwingAngle, 0f);
        }

        float elapsedTime = 0f;

        // Lerp the rotation over time until the door is fully open.
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * slamSpeed;

            // Smoothly interpolate between initial and target rotation.
            transform.localRotation = Quaternion.Lerp(localRotation, targetRotation, elapsedTime);

            yield return null; // Wait for the next frame.
        }

        // Ensure the door reaches the exact target rotation.
        transform.localRotation = targetRotation;

        if (!b_reverseDirection)
        {
            f_doorAngle = maxSwingAngle;
        }
        else
        {
            f_doorAngle = -maxSwingAngle;
        }
        isCloseSoundPlayed = false;
    }
    #endregion

    #region  Door Unlock
    [ClientRpc]
    private void RpcUnlockDoor()
    {
        if (playerTransform.GetComponent<PlayerBehavior>().int_keys > 0)
        {
            --playerTransform.GetComponent<PlayerBehavior>().int_keys;
            doorState = DoorState.Unlocked;

            //ChangeInteractSymbol();

            if (sfx_doorUnlocked == null) return;
            audioClip = sfx_doorUnlocked;
            AudioManager.instance.PlaySound(audioClip, transform.position, false, .5f);

            Debug.Log("Unlocked");
        }
    }
    #endregion

    #region Sound
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
                Debug.Log("Locked");
            }
        }
    }

    [ClientRpc]
    private void RpcOpenSound()
    {
        if (Mathf.Abs(f_doorAngle) <= 5f && !isOpenSoundPlayed && b_interacting)
        {
            if (sfx_doorOpen != null)
            {
                audioClip = sfx_doorOpen;
                AudioManager.instance.PlaySound(audioClip, transform.position, false, .5f);
            }

            isOpenSoundPlayed = true; // Prevent repeated playing
            isCloseSoundPlayed = false; // Reset close sound flag
        }
    }


    [ClientRpc]
    private void RpcDoorSound()
    {
        if (doorState == DoorState.Unlocked)
        {
            // Play open sound when door starts moving from the closed position
            if (Mathf.Abs(f_doorAngle) >= 5f && !isOpenSoundPlayed && b_interacting)
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
            if (Mathf.Abs(f_doorAngle) <= 1f && !isCloseSoundPlayed && b_interacting)
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
    #endregion
}
