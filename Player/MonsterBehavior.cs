using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class MonsterBehavior : PlayerBehavior
{

    //Turn Monsterbehavior into an instance so all traps can talk to it.
    MonsterBehavior monsterBehaviorInstance { get; set; }
    public enum MonsterState
    {
        Ethereal,
        Physical
    }

    [Header("Monster Properties")]
    public bool b_permanentPhysical = false;
    public MonsterState monsterState;

    [Header("Fear Meter")]
    public bool b_gainingFear; //This is necessary as a toggle to prevent the meter from rapidly gaining fear.
    public float f_fearValue = 0; // The amount by which the meter will be subtracted by. This is modified by the meter coroutines.
    public int int_maxFearValue = 100;
    [Tooltip("Time in which the meter adds value in seconds")]
    public float f_fearGainRate = 1f;
    public float f_fearDrainRate = 1f;
    public float f_fearDrainAmount = 10f;

    [Header("Transformation")]
    [Tooltip("How much fear you need in order to change state")]
    public float f_stateChangeRequirement = 50f;
    public float f_stateChangeCooldown = 5f;
    private bool b_canStateChange = true;


    [Header("Attack")]
    public float f_attackCooldown = 1f;

    [Header("Ability Variables")]
    public float f_abilityCooldown;
    public float f_abilityCost;
    public bool b_canUseAbility = true; // To enable and disable ability

    [Header("UI Components")]
    public TMP_Text tmp_TempMeter;

    [Header("Essential Components")]
    public GameObject attackHitbox;
    public GameObject monsterVisionPostProcess;
    //public RenderFeatureActivator RFA;

    //public GameObject[] players;

    //Coroutines
    private Coroutine gainCoroutine;
    private Coroutine drainCoroutine;

    [Header("Monster Debug")]
    public bool b_enableMassFearValue;

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        flashlightObject.SetActive(false);
        if (monsterVisionPostProcess != null)
        {
            monsterVisionPostProcess.SetActive(true);
        }

        if (isClient)
        {
            //Debug.Log("Is Client?");
        }

        //players = GameObject.FindGameObjectsWithTag("Player"); // Not necessary as we're now disabling players based on the camera culling rather than the gameobject itself.
    }

    protected virtual void Start()
    {
        if (b_enableMassFearValue)
        {
            int_maxFearValue = 1000;
            f_fearValue = int_maxFearValue;
        }


        if (b_gainingFear)
        {
            UpdateMeter();
            Invoke(nameof(StartGainingFear), 0f);
        }

        //Start off the round turning Ethereal. 
        if (!b_permanentPhysical)
        {
            CmdTurnEthereal();
            DisableObjects();
        }

    }

    public override void Update()
    {
        base.Update();

        ViewCast();
    }

    #region Extra Info


    // View cast updated to be Monster only. For players interactable objects will show through animation or sounds. 
    // View cast is to see what objects are interactable and what state the object is in, e.g., Locked / unlocked
    protected InteractSymbol currentSymbol = null;
    protected virtual void ViewCast()
    {
        // Define the start position and direction of the raycast
        Vector3 startPosition = Camera.main.transform.position;
        Vector3 direction = Camera.main.transform.forward;
        RaycastHit hit;

        if (Physics.Raycast(startPosition, direction, out hit, maxInteractionDistance, interactionLayerMask))
        {
            InteractableBehavior interactable = hit.collider.GetComponent<InteractableBehavior>();

            if (interactable != null && interactable.interactSymbol != null)
            {
                InteractSymbol iSymbol = interactable.interactSymbol;

                // If we're looking at a new symbol
                if (currentSymbol != iSymbol)
                {
                    // Deactivate the old symbol if it exists
                    if (currentSymbol != null)
                    {
                        currentSymbol.b_isActive = false;
                        currentSymbol.Activate();
                    }
                    // Activate the new one
                    currentSymbol = iSymbol;

                    // Clause to prevent players from seeing certain symbols.
                    if (gameObject.layer == LayerMask.NameToLayer("Ethereal") && hit.collider.gameObject.layer == LayerMask.NameToLayer("Door"))
                    {
                        return;
                    }
                    currentSymbol.b_isActive = true;
                    currentSymbol.Activate();


                }
            }
        }
        else if (currentSymbol != null)
        {
            // If we stopped looking at an object, deactivate the last known symbol
            currentSymbol.b_isActive = false;
            currentSymbol.Activate();
            currentSymbol = null;
        }
    }
    #endregion

    #region Fear Meter
    protected void StartGainingFear()
    {
        b_gainingFear = false;
        gainCoroutine = StartCoroutine(FearGainCoroutine());
    }

    protected void StartDrainingFear()
    {
        b_gainingFear = true;
        drainCoroutine = StartCoroutine(FearDrainCoroutine());
    }

    protected IEnumerator FearGainCoroutine()
    {
        while (monsterState == MonsterState.Ethereal || b_permanentPhysical)
        {
            yield return new WaitForSeconds(1f);

            if (f_fearValue < int_maxFearValue)
            {
                CmdModifyFear(1f * f_fearGainRate);
            }

            yield return null; // This is necessary as we want the meter to continue ticking but we just don't want to add value if the player has too much meter. So if a player casts an ability while at max it'll continue to restore that value.
        }
    }

    protected IEnumerator FearDrainCoroutine()
    {
        while (monsterState == MonsterState.Physical || !b_permanentPhysical)
        {
            yield return new WaitForSeconds(1f);
            CmdModifyFear(f_fearDrainAmount * -f_fearDrainRate);
            //UpdateMeter();
            // If we're out of meter change the state and stop this coroutine completely.
            if (f_fearValue <= 0f)
            {
                StateChange();
                UpdateMeter();
                yield break;
            }
        }
    }

    protected void UpdateMeter()
    {
        if (tmp_TempMeter != null)
        {
            tmp_TempMeter.text = $"{f_fearValue}" + $" {monsterState}";
        }
    }


    [Command]
    protected void CmdModifyFear(float value)
    {
        RpcModifyFear(value);
    }

    [ClientRpc]
    protected void RpcModifyFear(float value)
    {
        f_fearValue += value;

        f_fearValue = Mathf.Max(f_fearValue, 0f);

        if (f_fearValue > int_maxFearValue)
        {
            f_fearValue = int_maxFearValue;
        } 
        UpdateMeter();
    }

    #endregion

    #region State Change

    public override void CmdTransformState()
    {
        //Only do this if the monster isn't permanently physical.
        //Debug.LogWarning("Logging Change");
        if (!b_permanentPhysical && b_canStateChange)
        {
            StateChange();
        }
    }

    private IEnumerator TransformCooldown()
    {
        b_canStateChange = false;
        yield return new WaitForSeconds(f_stateChangeCooldown);
        b_canStateChange = true;
    }

    public void StateChange()
    {
        //if Ethereal and if you can pay the cost, turn physical and drain the meter.
        if (monsterState == MonsterState.Ethereal && f_fearValue >= f_stateChangeRequirement)
        {
            CmdTurnPhysical();
            monsterVisionPostProcess.SetActive(false);

            EnableObjects();

            Invoke(nameof(StartDrainingFear), 0f);
            StopCoroutine(gainCoroutine);
            monsterState = MonsterState.Physical;
            UpdateMeter();
        }

        //If we're physical turn back ethereal with no cost
        else if (monsterState == MonsterState.Physical)
        {
            CmdTurnEthereal();
            monsterVisionPostProcess.SetActive(true);
            DisableObjects();
            Invoke(nameof(StartGainingFear), 0f);
            StopCoroutine(drainCoroutine);
            monsterState = MonsterState.Ethereal;
            UpdateMeter();

            StartCoroutine(TransformCooldown());

        }
    }

    //Handle turning ethereal
    [Command]
    protected void CmdTurnEthereal()
    {
        Debug.LogWarning("This object is turning invisible!");
        RPCTurnEthereal();
    }

    [ClientRpc]
    protected void RPCTurnEthereal()
    {
        if (playerMeshRenderer != null)
        {
            playerMeshRenderer.enabled = false;
        }

        this.gameObject.layer = LayerMask.NameToLayer("Ethereal");
    }

    [Command]
    protected void CmdTurnPhysical()
    {
        RPCTurnPhysical();

    }

    [ClientRpc]
    protected void RPCTurnPhysical()
    {
        Debug.LogWarning("This object is turning physical");
        if (playerMeshRenderer != null)
        {
            playerMeshRenderer.enabled = true;
        }
        //GameManager.gmInstance.ServerProcessPhysicality(this);
        this.gameObject.layer = LayerMask.NameToLayer("Monster");
    }

    private void EnableObjects()
    {
        //This Enables the render layer for doors. Shadows are still active.
        /*
        if (RenderFeatureActivator.renderFeatureActivator != null)
        {
            RenderFeatureActivator.renderFeatureActivator.EnableLayer("Door");
        }
        */
        int layerToRemove = 1 << LayerMask.NameToLayer("Door");
        Camera.main.cullingMask |= layerToRemove;
        if (GameManager.gmInstance.humanPlayers.Count > 0)
        {
            foreach (var player in GameManager.gmInstance.humanPlayers)
            {
                player.SetActive(true);
            }
        }

        layerToRemove = 1 << LayerMask.NameToLayer("Player");
        Camera.main.cullingMask |= layerToRemove;

        /*
        if (players.Length > 0)
        {
            foreach (var player in players)
            {
                player.SetActive(true);
            }
        }
        */

    }

    private void DisableObjects()
    {
        //This disables the render layer for doors. Shadows are still active
        if (RenderFeatureActivator.renderFeatureActivator != null)
        {
            //RenderFeatureActivator.renderFeatureActivator.DisableLayer("Door");
        }

        int layerToRemove = 1 << LayerMask.NameToLayer("Door");
        Camera.main.cullingMask &= ~layerToRemove;

        if (GameManager.gmInstance.humanPlayers.Count > 0)
        {
            foreach (var player in GameManager.gmInstance.humanPlayers)
            {
                player.SetActive(false);
            }
        }

        layerToRemove = 1 << LayerMask.NameToLayer("Player");
        Camera.main.cullingMask &= ~layerToRemove;
        /*         
        if (players.Length > 0)
        {
            foreach (var player in players)
            {
                player.SetActive(false);
            }
        } 
        */
    }
    #endregion

    #region Ability
    public override void CmdAbilityCast(bool pressed)
    {
        //overrides can be overriden in a derived class so this can be changed. Nothing is used. Checks may be able to be done in here, but I've resorted to checking within each monster.
        //All monster ability programming will be done for each specific monster in their specific class, for now just add how to react if on cooldown.

    }

    //Call this to begin the cooldown.
    protected void BeginAbilityCooldown()
    {
        b_canUseAbility = false;
        CmdModifyFear(-f_abilityCost);
        UpdateMeter();
        StartCoroutine(AbilityCooldown(f_abilityCooldown));
    }

    // Call this coroutine whenever you need to begin the cooldown process.
    protected virtual IEnumerator AbilityCooldown(float coolDownTime)
    {
        yield return new WaitForSeconds(coolDownTime);
        b_canUseAbility = true; // Renable the ability to use ability.
    }
    #endregion

    #region Attack
    public override void Attack()
    {
        if (b_canAttack && isLocalPlayer && !inputSystem.b_crouch && monsterState == MonsterState.Physical)
        {
            b_canAttack = false;
            animator.SetTrigger("Attack_Trigger");
            animator.SetFloat("Speed", 0);
            firstPersonHands.GetComponent<Animator>().SetTrigger("Attack_Trigger");
            CmdAttack();
        }
    }

    [Command]
    public override void CmdAttack()
    {
        //Start animation and activate hurtbox.
        //AttackHitbox.SetActive(true);
        //Debug.Log($"{this.name} Sending Attack Info to clients");       
        RPCAttack();
    }

    [ClientRpc]
    public void RPCAttack()
    {
        if (!isLocalPlayer) // Prevent the local player from re-triggering
        {
            animator.SetTrigger("Attack_Trigger");
            animator.ResetTrigger("Attack_Trigger");
        }
    }
    public void ActivateHitbox(int activateInt)
    {
        switch (activateInt)
        {
            case 0:
                //CmdActivateHitbox(false);
                attackHitbox.SetActive(false);
                break;
            case 1:
                //CmdActivateHitbox(true);
                attackHitbox.SetActive(true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(activateInt), activateInt, "Invalid value must be 0 or 1");

        }
    }
    //Time after completing the animation to be able to attack again. Called by the animation event proxy using the attack animation event call.
    //This helps to synchronize the attack animations between players better as before there was a discrepancy between clients where the animation could not fire due to still being active on the non local player.
    //Now the cooldown will only begin when animation ends, also better for timings. 
    public void BeginAttackCooldown()
    {
        StartCoroutine(AttackCooldown(f_attackCooldown));
    }
    protected IEnumerator AttackCooldown(float cooldown)
    {
        // Wait for the cooldown to renable the ability to attack
        yield return new WaitForSeconds(cooldown);

        // Reset attack state
        b_canAttack = true;

        CrouchCast(); //Recheck if the player is attempting to crouch.
    }
    #endregion

    /* Commented out to test phone sound.
    public override void CmdPlayPhoneSound()
    {
        base.CmdPlayPhoneSound();
        // Make this empty to prevent monsters from having a vibrate
    }
    */
}
