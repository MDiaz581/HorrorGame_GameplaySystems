using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using VLB;
using UnityEngine.Rendering;


public class PlayerBehavior : NetworkBehaviour
{
    #region Variables

    [Header("Vital References")]
    public Animator animator;
    //public NetworkAnimator netAnimator; //Not necessary as we don't rely on Network Animator to call any of the animations. Still on the player object to sync animations however.
    public SkinnedMeshRenderer playerMeshRenderer; // This is on a child object not on the same object as the script. Needs to be manually added.
    public GameObject playerUI; //Set as the main game object just in case so there's no opportunity for any other UI aspects to appear on any other screen other than the client.
    //public Material hideMat;
    public GameObject firstPersonHands;

    [Header("Vital Private References")]
    [HideInInspector]
    protected InputSystem_Enabler inputSystem;
    protected CharacterController characterController;
    private Vector3 controllerCenter;
    private float controllerHeight;
    private FirstPersonCamera FPC;
    private PauseBehavior pauseBehavior;
    public Vector3 inputSystemLookVector;

    [Header("Character Properties")]

    [Header("Player States")]
    [SyncVar]
    public bool b_isDead;
    [SyncVar]
    public bool b_canAttack = true;
    public bool b_interactPressed = false;
    public enum SpecialStates
    {
        Normal,
        Interacting,
        Paused
    }

    public SpecialStates specialState;

    public enum StatusEffects
    {
        Normal,
        Dead,
        Stunned,
        Slowed
    }
    public StatusEffects statusEffects;

    [Header("Inventory")]
    public int int_keys;
    public int int_fuses;

    [Header("Movement Properties")]
    public float walkSpeed;
    public float sprintSpeed;
    public float crouchSpeed;
    [Tooltip("This is for the lunge of an attack it'll gradually reduce to 0")]
    public float f_initialAttackMoveSpeed = 2f;
    private float f_storeAttackMoveSpeed;

    public bool b_ceilingOverhead; // Used to check if player is crouching and there's a ceiling above them.

    public bool b_crouched;

    [Header("Gravity")]
    public float gravity = -9.81f;   // Custom gravity value
    public float terminalVelocity = -50f;   // Max fall speed
    public float groundCheckDistance = 1f; // Ground check raycast distance
    public LayerMask groundMask;    // To define what counts as "ground"
    private float verticalVelocity; // Tracks vertical speed
    private bool isGrounded;
    //[HideInInspector]
    public bool b_playerPaused;

    [Header("Camera Properties")]
    public GameObject cameraRoot;
    public Transform standPosition;
    public Transform crouchPosition;

    [Header("Interaction Properties")]
    public Transform grabPoint;
    public float maxInteractionDistance = 2f;
    public LayerMask interactionLayerMask;
    private InteractableBehavior interactableBehavior;
    private Transform platform;
    private ElevatorBehavior movingPlatform;

    [Header("Flashlight and Lights")]
    [SerializeField] protected GameObject flashlightObject;
    [SerializeField] protected VolumetricLightBeamSD Lightbeam;
    [SyncVar(hook = nameof(OnFlashlightStateChanged))]
    private bool isFlashlightOn = false;

    private bool flashlightConsumed; // guard flag
    public Light lowLightVision;

    [Header("Sanity / Insanity")]
    [SyncVar]
    public float f_sanity = 100f;
    public float f_InsanityInfluence; // Affected by outside sources such as traps.
    private Coroutine loseSanity;

    [Header("Debug Variables")]
    [Tooltip("Gets set active as a visual indicator")]
    [SerializeField] protected GameObject testFlag;
    [Tooltip("Allows for local testing of playermodel animations")]
    [SerializeField] protected bool b_Debug_EnablePlayerModels = false;
    public event Action onInteract; // an Action other objects can see and get reference to.
    [Header("Cellphone")]
    public GameObject cellphoneObject; // The chat box
    private GameObject overlayCamObject;
    [SyncVar]
    public int int_playerID; // Since each player prefab is separate, their SyncVar values remain distinct.

    public static event Action<PlayerBehavior> callTestButton;

    [HideInInspector]
    public bool b_inChat = false; // public due to phone phonebehavior having access to it.

    [SerializeField] private AudioClip sfx_PhoneVibration;

    #endregion

    #region Initialization
    public void OnEnable()
    {
        InputSystem_Enabler.onInteractPressed += InteractionCast;
        InputSystem_Enabler.onAbilityPressed += CmdAbilityCast;
        InputSystem_Enabler.onCrouchPressed += CrouchCast;
        InputSystem_Enabler.onTransformPressed += CmdTransformState;
        InputSystem_Enabler.onAttackPressed += Attack;
        InputSystem_Enabler.onTestButtonPressed += ActivateTestButton;

        TextChatBehavior.SendSoundToPlayers += CmdPlayPhoneSound; // I would have liked to put this on the phone, but the issue here is that the phone would then need to be active, since we disable it instead it's easier to do this.
    }

    public void OnDisable()
    {
        InputSystem_Enabler.onInteractPressed -= InteractionCast;
        InputSystem_Enabler.onAbilityPressed -= CmdAbilityCast;
        InputSystem_Enabler.onCrouchPressed -= CrouchCast;
        InputSystem_Enabler.onTransformPressed -= CmdTransformState;
        InputSystem_Enabler.onAttackPressed -= Attack;
        InputSystem_Enabler.onTestButtonPressed -= ActivateTestButton;

        TextChatBehavior.SendSoundToPlayers -= CmdPlayPhoneSound;
    }

    public override void OnStartServer()
    {
        int_playerID = connectionToClient.connectionId; // Set the unique ID on the server, this has to be done in OnStartServer as only the server has access to the client connection ID.
    }

    public override void OnStartLocalPlayer()
    {
        Debug.Log($"My Player ID: {int_playerID}");
    }


    // Start is called before the first frame update
    public override void OnStartAuthority()
    {
        if (playerMeshRenderer != null && isLocalPlayer && !b_Debug_EnablePlayerModels)
        {
            playerMeshRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly; // Force model to only cast shadow on local players.
            /*
            if (hideMat != null)
            {

                int i = 0;
                Material[] materials = playerMeshRenderer.materials;
                foreach (Material material in materials)
                {
                    materials[i] = hideMat;
                    i++;
                    //Debug.Log("Changing Material");
                }
                playerMeshRenderer.materials = materials;

            }
            else
            {
                /* This was good, but it didn't provide the player with a shadow when in physical form, to see your shadow I think is a nice subtle touch.
                So now the local player's material changes to a never depth tested model with shadows enabled. Now relegated to a fallback just in case.
                //
            playerMeshRenderer.enabled = false; //Remove the player mesh so it doesn't block the camera. 
            playerMeshRenderer = null; // Forcefully override the ability to appear on the local player screen.
        }
            */
        }

        //If we're a player hide any ability to see the trap highlight.
        if (gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            RenderFeatureActivator.renderFeatureActivator.SetRenderFeatureActive(0, false);
        }

        // If the layer is monster or ethereal add the ability to see the trap highlight.
        if (gameObject.layer == LayerMask.NameToLayer("Monster") || gameObject.layer == LayerMask.NameToLayer("Ethereal"))
        {
            RenderFeatureActivator.renderFeatureActivator.SetRenderFeatureActive(0, true);
        }

        if (firstPersonHands != null)
        {
            firstPersonHands.SetActive(true);
        }

        //serverStateManager = GameObject.Find("ServerManager").GetComponent<ServerStateManager>();
        if (lowLightVision != null)
        {
            lowLightVision.enabled = true;
        }
        PlayerInput playerInput = GetComponent<PlayerInput>();
        playerInput.enabled = true;
        inputSystem = GetComponent<InputSystem_Enabler>();
        characterController = GetComponent<CharacterController>();
        FPC = GetComponent<FirstPersonCamera>();
        pauseBehavior = GetComponent<PauseBehavior>();
        pauseBehavior.enabled = true;

        playerUI.SetActive(true);

        if (inputSystem == null)
        {
            Debug.LogError("InputSystem_Enabler component not found.");
        }
        if (characterController == null)
        {
            Debug.LogError("CharacterController not found");
        }

        if (characterController != null)
        {
            controllerCenter = characterController.center; // Set prefab defaults for the standing position;
            controllerHeight = characterController.height; // Set prefab defaults for the standing position;
        }

        cameraRoot.transform.position = standPosition.position;

        overlayCamObject = GameObject.Find("Overlay Camera"); // Find the overlayCamera
        overlayCamObject.transform.position = cameraRoot.transform.position;
        overlayCamObject.transform.SetParent(cameraRoot.transform);

        SetCursorState(true);

        f_storeAttackMoveSpeed = f_initialAttackMoveSpeed;
    }

    #endregion

    #region Update
    // Update is called once per frame
    public virtual void Update()
    {
        if (!isLocalPlayer) return;


        Gravity();
        ApplyPlatformMovement();

        if (b_inChat) return;

        if (!b_playerPaused && specialState != SpecialStates.Interacting)
        {
            CharacterMove();
        }

        // Allows player to come out of crouched areas standing up, without having to manually crouch again to stand.
        if (b_crouched && !inputSystem.b_crouch)
        {
            CrouchCast();
        }

        //Disable the volumetric light locally. Allows players to see beams, but keeps the annoying visual clutter off the player
        if (Lightbeam != null)
        {
            if (Lightbeam.enabled)
            {
                Lightbeam.enabled = false;
            }
        }

        HoldInteractionCast();
    }
    #endregion

    #region Movement & Gravity
    private void CharacterMove()
    {
        if (inputSystem.move != Vector2.zero)
        {
            float moveSpeed;

            if (b_canAttack)
            {
                //Crouching takes priority so if player is crouching do crouch speed, if they're not check if they're sprinting, if they are give them sprint speed otherwise give them walk speed.
                moveSpeed = inputSystem.b_crouch ? crouchSpeed : (inputSystem.b_sprint ? sprintSpeed : walkSpeed);

                f_initialAttackMoveSpeed = f_storeAttackMoveSpeed;
            }
            else
            {
                f_initialAttackMoveSpeed = Mathf.MoveTowards(f_initialAttackMoveSpeed, .1f, f_storeAttackMoveSpeed * Time.deltaTime);
                moveSpeed = f_initialAttackMoveSpeed;
            }

            // normalise input direction
            Vector3 inputDirection = new Vector3(inputSystem.move.x, 0.0f, inputSystem.move.y).normalized;

            if (inputSystem.move != Vector2.zero)
            {
                // move
                inputDirection = transform.right * inputSystem.move.x + transform.forward * inputSystem.move.y;
            }

            characterController.Move(inputDirection.normalized * moveSpeed * Time.deltaTime);

            // Update animator parameters
            UpdateAnimator(inputDirection, moveSpeed);
        }
        else
        {
            UpdateAnimator(Vector3.zero, 0f);
        }
    }

    private void UpdateAnimator(Vector3 inputDirection, float moveSpeed)
    {
        // Calculate the speed of movement (magnitude of inputDirection)
        float speed = inputDirection.magnitude * moveSpeed;

        if (animator == null) return;

        // Update Animator parameters
        animator.SetFloat("Speed", speed);
        animator.SetBool("IsCrouching", b_crouched);
        animator.SetBool("IsSprinting", inputSystem.b_sprint);
    }

    private bool GroundChecker(out RaycastHit hitInfo)
    {
        return Physics.Raycast(transform.position, Vector3.down, out hitInfo, groundCheckDistance, groundMask);
    }

    protected virtual void Gravity()
    {
        // Check if the character is on the ground using a raycast
        RaycastHit hit;
        isGrounded = GroundChecker(out hit);

        // Reset vertical velocity if on the ground
        if (isGrounded)
        {
            if (verticalVelocity < 0)
            {
                verticalVelocity = -1f; // Small value to keep the character grounded
            }
        }

        // Apply gravity
        verticalVelocity += gravity * Time.deltaTime;

        // Clamp the vertical velocity to prevent exceeding terminal velocity
        verticalVelocity = Mathf.Max(verticalVelocity, terminalVelocity);

        // Move the character based on gravity
        Vector3 gravityMovement = new Vector3(0, verticalVelocity, 0);

        characterController.Move(gravityMovement * Time.deltaTime);
    }

    private void ApplyPlatformMovement()
    {
        RaycastHit hit;
        isGrounded = GroundChecker(out hit);

        // Check if on the ground first
        if (isGrounded)
        {
            // Check if its a moving platform
            if (hit.collider.CompareTag("MovingPlatform"))
            {
                if (platform == null)
                {
                    platform = hit.collider.transform;

                    // Look for Elevator Behavior component in order to get the moving platform's velocity.
                    if (platform.GetComponent<ElevatorBehavior>() == null)
                    {
                        //Debug.Log("Found on parent!");
                        movingPlatform = platform.transform.parent.GetComponent<ElevatorBehavior>(); // Check Parent first
                    }
                    else
                    {
                        movingPlatform = platform.GetComponent<ElevatorBehavior>(); // If its not in the parent check the actual object.
                    }

                }
            }
            else
            {
                platform = null;
                movingPlatform = null;
            }
        }
        else
        {
            platform = null;
            movingPlatform = null;
        }

        if (platform != null && movingPlatform != null)
        {

            //Debug.Log("Ready to move with platformVelocity! " + movingPlatform.platformVelocity); //Note, if elevator is jittery check if the groundcast is even hitting the floor. This debug should help determine that.

            // Apply the platform's velocity to the player
            if (movingPlatform.platformVelocity.y < 0) // if the velocity is downward, don't try to apply this velocity, just let us fall. The speed at which the elevator moves and our vertical velocity seem to be directly correlated.
            {
                //The speed of which the elevator moves downward has to be the same as our basic vertical velocity when we are grounded for a smooth appearance. This may Break things to revert back just
                //Remove the if statement and make the else statement "characterController.Move(movingPlatform.platformVelocity * Time.deltaTime);" the only thing.
                characterController.Move(new Vector3(movingPlatform.platformVelocity.x, 0, movingPlatform.platformVelocity.z) * Time.deltaTime);
            }
            else
            {
                characterController.Move(movingPlatform.platformVelocity * Time.deltaTime);
            }

        }
    }


    private bool CeilingChecker(out RaycastHit hitInfo)
    {
        return Physics.Raycast(transform.position, Vector3.up, out hitInfo, 1f, groundMask);
    }

    protected void CrouchCast()
    {
        if (!isLocalPlayer) return; // Safety Check to prevent other players from seeing this function be called.

        if (b_playerPaused || b_inChat) return;

        RaycastHit hit;
        b_ceilingOverhead = CeilingChecker(out hit);
        //Was thinking about putting this on the player animation and just parenting on the head but that could cause headbob issues with certain animations. 
        if (inputSystem.b_crouch && b_canAttack)
        {
            if (crouchPosition != null)
            {
                b_crouched = true;
                cameraRoot.transform.position = crouchPosition.position;
                characterController.height = 1;
                characterController.center = new Vector3(0, -0.5f, 0);
            }
        }
        else
        {
            if (standPosition != null && !b_ceilingOverhead)
            {
                b_crouched = false;
                cameraRoot.transform.position = standPosition.position;
                characterController.height = controllerHeight; // reset to default;
                characterController.center = controllerCenter;  // reset to default;
            }
        }
    }
    #endregion

    #region Interaction
    public void InteractionCast()
    {

        if (inputSystem.b_interact && !b_playerPaused)
        {
            //Invoke this action. This is specifically for when we don't want to rely on grabbing the interactable behavior. Instead we just want to listen for a player input.
            onInteract?.Invoke();

            if (specialState == SpecialStates.Interacting) return;

            // Define the start position and direction of the raycast
            Vector3 startPosition = Camera.main.transform.position;
            Vector3 direction = Camera.main.transform.forward;
            // Draw the ray in the scene view
            Debug.DrawRay(startPosition, direction * maxInteractionDistance, Color.red, 1.0f);
            RaycastHit hit;
            if (Physics.Raycast(startPosition, direction, out hit, maxInteractionDistance, interactionLayerMask))
            {
                if (hit.collider != null)
                {
                    interactableBehavior = hit.collider.gameObject?.GetComponent<InteractableBehavior>();
                    if (interactableBehavior == null)
                    {
                        if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Default"))
                        {
                            Debug.LogWarning("Could not find InteractableBehavior");
                        }
                    }
                    else
                    {

                        grabPoint.position = hit.point + (transform.forward / 5);
                        interactableBehavior.CmdGetGrabPoint(grabPoint.position);

                        //interactableBehavior.b_interacting = inputSystem.b_action;

                        //Feed our transform into the interactable behavior
                        interactableBehavior.CmdGetPlayerTransform(transform);

                        //Feed our Call function and feed our input system bool into the interactable behavior
                        interactableBehavior.CmdOnInteract(inputSystem.b_interact);

                        // If we want to provide an additional bool we send it this way. This updates the objects bool b_value allowing us to convert whatever action to require an addition check.
                        // Here we feed the sprint value to the bool.
                        if (!inputSystem.b_crouch)
                        {
                            interactableBehavior.CmdGetBool(inputSystem.b_sprint);

                        }

                    }
                }
            }
        }
        else
        {
            //if not interacting clear variables.
            if (interactableBehavior != null)
            {
                interactableBehavior.CmdOnInteract(false);
                interactableBehavior.CmdGetPlayerTransform(null);
                //interactableBehavior.CmdGetGrabPoint(null);
                interactableBehavior = null;
            }
        }

    }


    //This is always checked in update;
    public void HoldInteractionCast()
    {
        if (inputSystem.b_interact)
        {
            // Grab the look vector
            inputSystemLookVector = new Vector2(inputSystem.look.x, inputSystem.look.y); // This allows us to access this across multiple scripts. Used for OverlayObjectBehavior

            if (specialState == SpecialStates.Interacting) return;
            //Verify we're targeting an interactable behavior
            if (interactableBehavior != null)
            {
                // Give it a value based on the look direction and the rotation speed of the camera. Useful for the door.
                interactableBehavior.CmdOnHoldInteract(inputSystem.look.x * FPC.float_rotationSpeed);

                //Constantly Give it the grab point;
                interactableBehavior.CmdGetGrabPoint(grabPoint.position);

                //If beyond max distance clear given variables.
                if (Vector3.Distance(interactableBehavior.transform.position, transform.position) > 2.5f)
                {
                    interactableBehavior.CmdOnInteract(false);
                    interactableBehavior.CmdGetPlayerTransform(null);
                    //interactableBehavior.CmdGetGrabPoint(null);
                    interactableBehavior = null;
                }
            }
        }
        else
        {
            if (inputSystemLookVector != Vector3.zero)
            {
                inputSystemLookVector = Vector2.zero;
            }

        }
    }


    #endregion

    #region Player Abilities

    [Command]
    public virtual void CmdAbilityCast(bool pressed)
    {
        if (pressed) // Checks if the button is down and not up. This allows for toggle.
        {
            isFlashlightOn = !isFlashlightOn;
            lowLightVision.enabled = !isFlashlightOn;
        }
    }

    [Command]
    public void CmdFlashlightToggle()
    {
        // Toggle the flashlight state on the server
        //isFlashlightOn = !isFlashlightOn;
    }

    private void OnFlashlightStateChanged(bool oldState, bool newState)
    {
        // Update the flashlight state on all clients
        flashlightObject.SetActive(newState);

        if (Lightbeam != null)
        {
            //Reveal the volumetric light to all players, but disable it locally in update.
            Lightbeam.enabled = isFlashlightOn;
        }

    }

    //This is for the monster but has to be created here. Left blank as for the player it has no function.
    [Command]
    public virtual void CmdTransformState()
    {

    }

    public virtual void Attack()
    {

    }

    [Command]
    public virtual void CmdAttack()
    {

    }

    #region Damage
    public void TakeDamage()
    {
        if (!isServer)
        {
            CmdTakeDamage();
        }
        else
        {
            ProcessDamage();
        }
    }

    [Server]
    public virtual void ProcessDamage()
    {
        Debug.Log($"{this.name} took damage."); // Server understands player took damage

        b_isDead = true; //This is sync'd across all players therefore is technically not needed to be sent through client RPC.

        //Server Processes all critical game information such as the players health or state.
        // Optional: Notify all clients about the damage (e.g., play a hit animation)

        RpcPlayerTookDamage(); // Notifies all players that player took damage.
    }

    [ClientRpc]
    private void RpcPlayerTookDamage()
    {
        // Trigger any client-specific effects (like playing animations) keeps the server from having to process all this information.
        Debug.Log($"{this.name} took damage! Play hit effect.");
        //testFlag?.SetActive(true); Both Players are aware who takes damage
    }

    [Command]
    public virtual void CmdTakeDamage()
    {
        ProcessDamage();
        //Play death animation, Disable features, enable Spectator mode, send your Death to the Game Manager.

    }
    #endregion

    #endregion

    #region Sanity meter
    //These are player specific effects that occur when you have too little sanity.

    public float f_totalDrainAmount()
    {
        return 1f + f_InsanityInfluence;
    }

    public void LoseSanity()
    {
        float drainAmount = f_totalDrainAmount();
        loseSanity = StartCoroutine(DrainSanity(drainAmount));
    }

    public IEnumerator DrainSanity(float f_drainAmount)
    {
        yield return new WaitForSeconds(1f);
    }

    #endregion

    #region Cursor Lock Functions
    public void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.Confined;

        Cursor.visible = !newState;

        if (specialState == SpecialStates.Interacting)
        {
            Cursor.lockState = CursorLockMode.Confined;

        }
        //Debug.Log("Bool state is " + newState + " Mouse lock is now " + Cursor.lockState + "Cursor Visibility is now " + Cursor.visible);
    }


    private void OnApplicationFocus(bool focusStatus)
    {
        if (!b_playerPaused || !b_inChat || specialState != SpecialStates.Interacting)
        {
            SetCursorState(true);
        }

    }

    #endregion

    public virtual void ActivateTestButton()
    {
        Debug.Log("Pressed TestButton");

        if (cellphoneObject == null) return;
        if (cellphoneObject.GetComponent<PhoneBehavior>() == null) return;
        if (!b_playerPaused)
        {
            SetCursorState(b_inChat);

            b_inChat = !b_inChat;

            cellphoneObject.SetActive(b_inChat);

            if (b_inChat)
            {
                cellphoneObject.GetComponent<PhoneBehavior>().BringUpPhone();
                Debug.Log("On");

                InputSystem_Enabler.onTransformPressed -= CmdTransformState;
            }
            else
            {
                cellphoneObject.GetComponent<PhoneBehavior>().RemovePhone();
                Debug.Log("Off");
                InputSystem_Enabler.onTransformPressed += CmdTransformState;
            }

            if (callTestButton != null)
            {
                callTestButton.Invoke(this); //Send the signal to the TextChatBehavior 
            }
        }

    }

    [Command]
    public virtual void CmdPlayPhoneSound()
    {
        RpcPlayPhoneSound();
    }

    [ClientRpc]
    public virtual void RpcPlayPhoneSound()
    {
        Debug.Log("Playing sound here at " + int_playerID);

        AudioManager.instance.PlaySound(sfx_PhoneVibration, transform.position, false);

    }

}
