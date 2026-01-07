using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostGirlBehavior : MonsterBehavior
{

    [Header("Vital References")]
    [Tooltip("Model to use to show the player where they'll end up.")]
    public GameObject teleportHighlight;
    public float maxTeleportDistance;
    [Tooltip("The layers the monster won't be able to teleport through.")]
    public LayerMask collidedMask;
    public LayerMask triggeredTrapMask;

    public enum TeleportState
    {
        NotLooking,
        Looking,
        Teleported
    }

    public TeleportState teleportState;
    private bool b_lookingAtTrap;
    private Transform storedTriggeredInfo;
    public Vector3 storedPlayerPosition;
    //public Vector3 offset; not used could be used to change where the current offset values are, but considering the Ghost girl shouldn't really change this doesn't seem necessary. Maybe revisit.
    private Vector3 teleportDestination; // Store the location to use in Teleport()
    private bool b_castingTeleportation = false;
    private Coroutine teleportCoroutine;
    private Transform specialTeleportPoint; // This is specifically if we want the ghost to be able to teleport to unique locations such as inside the elevator.
    private bool b_abilityCrouchCasted;

    private float f_directionalVelocity;
    private Vector3 v3_hitPointStored;
    private Vector3 v3_storedDirection;
    private bool onWall;

    [Header("Debug Variables")]
    [SerializeField]
    private bool b_DebugShowTeleportCalculations;
    [SerializeField]
    private float f_DebugTeleportCalculationTime = 0.1f;

    public override void Update()
    {
        //Disable everything if player has teleported to a triggered trap, including gravity.
        if (teleportState != TeleportState.Teleported)
        {
            base.Update();
        }
        if (!inputSystem.b_crouch && b_abilityCrouchCasted)
        {
            b_abilityCrouchCasted = false;
        }
    }

    public override void CmdAbilityCast(bool pressed)
    {
        if (b_inChat) return;
        if (b_canUseAbility && f_fearValue >= f_abilityCost)
        {
            //Cancel Teleport Ability;
            if (b_playerPaused)
            {
                if (teleportCoroutine != null)
                {
                    StopCoroutine(teleportCoroutine);
                }
                if (teleportHighlight != null)
                {
                    teleportHighlight.SetActive(false);
                }
                return;
            }
            //base.CmdAbilityCast();
            Debug.Log("Casting Ability!");
            teleportCoroutine = StartCoroutine(Ability());
        }
    }

    private void CalculateTeleportPosition()
    {
        // Define the start position and direction of the raycast
        Vector3 startPosition = Camera.main.transform.position;
        Vector3 direction = Camera.main.transform.forward;
        Vector3 endPosition = startPosition + direction * maxTeleportDistance;

        // Raycast Variables.
        RaycastHit hit;
        RaycastHit triggeredTrapHit;

        #region Basic Teleport
        // If we hit something change its end point to be the hit point + an offset.
        if (Physics.Raycast(startPosition, direction, out hit, maxTeleportDistance, collidedMask))
        {
            // Subtract the hit point by the direction to get the opposite direct, then set it at a slight offset.
            if (!hit.collider.CompareTag("MovingPlatform"))
            {
                endPosition = hit.point - direction * 0.3f;

                // Remove reference to the special TP point, so we don't rotate in its direction later.
                if (specialTeleportPoint != null)
                {
                    specialTeleportPoint = null;
                }
            }
            else // Unique Ability to teleport within elevators. 
            {
                ElevatorBehavior elevatorBehavior = hit.collider.GetComponentInParent<ElevatorBehavior>();
                if (elevatorBehavior != null && elevatorBehavior.teleportPoint != null)
                {
                    specialTeleportPoint = elevatorBehavior.teleportPoint;
                    endPosition = specialTeleportPoint.position;
                }
                else
                {
                    Debug.LogWarning("Can't Find Teleport Point Teleporting like Normal");
                    endPosition = hit.point - direction * 0.3f;
                }
            }
        }
        // If we don't hit anything do this then continue forward.
        else
        {
            // Remove reference to the special TP point, so we don't rotate in its direction later.
            if (specialTeleportPoint != null)
            {
                //specialTeleportPoint = null;
            }
        }
        #endregion

        // Overrides above if true
        #region Teleport to Trap
        if (monsterState == MonsterState.Ethereal)
        {
            if (Physics.Raycast(startPosition, direction, out triggeredTrapHit, 100f, triggeredTrapMask)) // This is a much bigger raycast so that the player can teleport across the map to triggered traps.
            {
                if (((1 << triggeredTrapHit.collider.gameObject.layer) & triggeredTrapMask) != 0)
                {
                    teleportState = TeleportState.Looking;

                    storedTriggeredInfo = triggeredTrapHit.transform;
                    endPosition = storedTriggeredInfo.position - storedTriggeredInfo.forward * 0.8f;

                    storedPlayerPosition = transform.position;
                }
            }
            else
            {
                //storedPlayerTransform = null;
                storedTriggeredInfo = null;

                if (teleportState != TeleportState.Teleported)
                {
                    teleportState = TeleportState.NotLooking;
                }

            }
        }
        #endregion

        // Check the floors and the ceilings around the teleport point.
        RaycastHit groundHit, ceilingHit, ceilingGroundSearch, wallHit; // Raycasts for ground and ceiling checks
        // Check slightly above ground
        bool hasGround = Physics.Raycast(endPosition + new Vector3(0, 0.1f, 0), Vector3.down, out groundHit, 15f);
        // Check slightly below ground
        bool hasCeiling = Physics.Raycast(endPosition - new Vector3(0, 0.1f, 0), Vector3.up, out ceilingHit, 15f);
        bool crouchVerifyGround = Physics.Raycast(endPosition + new Vector3(0, 0.1f, 0), Vector3.down, out wallHit, 1f);

        Vector3 finalPosition = endPosition; // Default to the endpoint

        teleportDestination = transform.position; // Default teleport destination to itself. This is in case it hasn't been set already. Preventing teleporting to world origin or previously stored areas.

        // DEBUG
        if (b_DebugShowTeleportCalculations)
        {
            Debug.DrawLine(startPosition, endPosition, Color.yellow, f_DebugTeleportCalculationTime); // Initial Raycast
            Debug.DrawRay(endPosition, Vector3.down * 15f, Color.green, f_DebugTeleportCalculationTime); // Ground check debug
            Debug.DrawRay(endPosition, Vector3.up * 15f, Color.blue, f_DebugTeleportCalculationTime);  // Ceiling check debug
            Debug.DrawLine(startPosition, finalPosition, Color.magenta, f_DebugTeleportCalculationTime); // Draw where the final location will be. 
        }

        if (b_crouched && Physics.Raycast(startPosition, direction, out wallHit, maxTeleportDistance, collidedMask) && !crouchVerifyGround)
        {
            v3_hitPointStored = wallHit.point;
            finalPosition = wallHit.point + (wallHit.normal * 0.4f);

        }
        else
        {
            #region TPVerticalStateCheck   
            //  Check if it's an acceptable room 
            if (hasGround && hasCeiling)
            {
                // Prevent teleportation if room is too small.
                if (Vector3.Distance(groundHit.point, ceilingHit.point) < 1.75f)
                {
                    Debug.LogWarning("Found both ground and ceiling! Room too small, No teleport: " + Vector3.Distance(groundHit.point, ceilingHit.point));
                    return; // Stop here and just maintain finalPosition as the end point. 
                }
                else
                {
                    finalPosition = groundHit.point + new Vector3(0, 0.75f, 0);
                }
            }
            else if (hasGround)
            {
                Debug.Log("Found Ground, Teleporting Above Ground");
                finalPosition = groundHit.point + new Vector3(0, 0.75f, 0); // Move slightly up
            }
            else if (hasCeiling)
            {
                Debug.Log("Found Ceiling, Teleporting Below Ceiling");
                //Final Search to determine where the ground is compared to the ceiling, so that we can put the player on the floor rather than floating in the air if using the ceiling as a teleport point. 
                if (Physics.Raycast(ceilingHit.point, Vector3.down, out ceilingGroundSearch, 25f, collidedMask))
                {
                    finalPosition = ceilingGroundSearch.point + new Vector3(0, 0.75f, 0);

                    if (b_DebugShowTeleportCalculations)
                    {
                        Debug.DrawLine(ceilingHit.point, finalPosition, Color.black, f_DebugTeleportCalculationTime);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Found No ground, refusing to teleport!");
                return; // Prevent player from teleporting over a void. No real reason to check for but just in case.
            }
        }
        #endregion

        // Activate Highlight
        if (teleportHighlight != null)
        {
            if (b_castingTeleportation)
            {
                teleportHighlight.SetActive(true);
                teleportHighlight.transform.position = finalPosition - new Vector3(0, 0.75f, 0); //show where player will teleport with a model
            }
            else
            {
                //Redundant + doesn't fire. 
                teleportHighlight.SetActive(false);
            }

        }
        teleportDestination = finalPosition; // Teleport to the final calculated position;
    }

    private IEnumerator Ability()
    {
        //While the button is pressed
        while (inputSystem.b_ability)
        {
            b_castingTeleportation = true;
            CalculateTeleportPosition();
            yield return new WaitForSeconds(0.05f);
            yield return null;
        }
        //If the button is let go check if we were casting teleportation
        if (b_castingTeleportation)
        {
            //Disable it
            b_castingTeleportation = false;
            //Then cast teleport
            Teleport();
        }

    }

    // Apply teleportation
    private void Teleport()
    {
        //If we want a cleaner teleport, we could turn this playermodel off for all clients, then turn it back on if it's etherael.
        BeginAbilityCooldown();
        characterController.enabled = false; // Disable to manually change position
        transform.position = teleportDestination;

        //If looking at a triggered trap.
        if (teleportState == TeleportState.Looking)
        {
            transform.rotation = new Quaternion(0, storedTriggeredInfo.rotation.y, 0, 0) * Quaternion.Euler(0, 180, 0);
            storedTriggeredInfo = null;

            CmdModifyFear(10); // Return cost of changing state.
            StartCoroutine(ReturnToPosition());

        }
        if (specialTeleportPoint != null)
        {
            transform.rotation = new Quaternion(0, specialTeleportPoint.rotation.y, 0, 0);
            specialTeleportPoint = null;
        }
        if (teleportHighlight != null)
        {
            teleportHighlight.SetActive(false);
        }

        if (b_crouched)
        {
            b_abilityCrouchCasted = true;
            v3_storedDirection = (v3_hitPointStored - transform.position).normalized;
        }

        characterController.enabled = true; // Re-enable CharacterController

    }

    private IEnumerator ReturnToPosition()
    {
        yield return new WaitForEndOfFrame();
        teleportState = TeleportState.Teleported;

        yield return new WaitForSeconds(0.05f);


        // This should be done at no cost Figure it out DUMMY
        if (monsterState == MonsterState.Ethereal)
        {
            CmdTransformState();
        }

        yield return new WaitForSeconds(2f);

        if (monsterState == MonsterState.Physical)
        {
            CmdTransformState();
        }

        teleportState = TeleportState.NotLooking;

        characterController.enabled = false; // Disable to manually change position
        transform.position = storedPlayerPosition;
        characterController.enabled = true; // Re-enable CharacterController
    }

    private bool DirectionChecker(out RaycastHit hitInfo)
    {
        Debug.DrawRay(transform.position - new Vector3(0, 0.5f, 0), v3_storedDirection, Color.red);
        return Physics.Raycast(transform.position, v3_storedDirection, out hitInfo, 0.5f, groundMask);
    }


    protected override void Gravity()
    {
        if (b_abilityCrouchCasted && inputSystem.b_crouch)
        {
            // Check if the character is on the ground using a raycast
            RaycastHit hit;
            onWall = DirectionChecker(out hit);

            // Reset vertical velocity if on the ground
            if (onWall)
            {
                if (f_directionalVelocity < 0)
                {
                    f_directionalVelocity = -1f; // Small value to keep the character grounded
                }
            }
            else
            {
                b_abilityCrouchCasted = false;
            }

            // Apply gravity
            f_directionalVelocity -= gravity * Time.deltaTime;

            // Clamp the vertical velocity to prevent exceeding terminal velocity
            f_directionalVelocity = Mathf.Min(f_directionalVelocity, terminalVelocity);

            // Move the character based on gravity
            Vector3 gravityMovement = v3_storedDirection;

            characterController.Move(gravityMovement * Time.deltaTime);

        }
        else
        {
        
        base.Gravity();
                
        }

    }
}
