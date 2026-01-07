using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class TrapBehavior : InteractableBehavior
{
    public enum Traptype
    {
        [Tooltip("Trap only sounds once and deactivates, can be reactivated.")]
        SingleFire,
        [Tooltip("Trap sound loops until deactivated, can be reactivated.")]
        Constant,
        [Tooltip("Trap on sounds onces and deactivates, CANNOT be reactivated.")]
        Oneshot
    }

    [Header("Trap Properties")]
    public Traptype traptype;

    [Tooltip("How close a player has to be to activate trap.")]
    [SerializeField]
    private float f_trapRadius = 2f;

    [Header("Insanity Variables")]

    [Tooltip("How close a player has to be to be affected by fear and insanity.")]
    [SerializeField]
    private float f_trapEffectRadius = 8f;

    [Tooltip("How much insanity this object causes. If the trap type is constant this will be divided by 10")]
    [SerializeField]
    private float f_effectInsanityInfluence = 1f;

    [Tooltip("List of objects being affected")]
    public List<GameObject> influencedObjects = new List<GameObject>();

    [Tooltip("What activates and gets affected by this trap.")]
    [SerializeField]
    private LayerMask triggerLayerMask;

    [Tooltip("Cooldown before trap can be reactivated.")]
    [SerializeField]
    public float f_cooldown = 10f;

    [Header("Flags. (Don't directly modify unless debugging)")]
    [SyncVar]
    [Tooltip("Trap is on and able to be fired.")]
    public bool b_trapEnabled = false;

    [Tooltip("Trap can be fired, used for One shots that can't be fired again.")]
    [SyncVar]
    public bool b_canEnable = true;

    [SyncVar]
    [Tooltip("Trap is currently firing")]
    public bool b_trapIsActive = false;

    [SyncVar]
    [Tooltip("Allow all to activate the trap. (Meaning all players.)")]
    public bool b_allowAll;

    [Header("Trap Audio")]
    public AudioClip sfx_trapSound;

    private AudioSource loopedAudio; // Used to disabled any looping audio

    [Tooltip("How loud the trap will sound. High values louder, lower values quieter")]
    [SerializeField]
    private float f_volumeOverride = 1f;

    [SyncVar]
    private bool b_canSound = true;


    void Start()
    {
        // Not necessary. I think, this was when I was testing an idea to have audio be influenced by the parent class. Didn't pan out, so its not necessary. 
        // Its easy enough to call the instance of the audio manager.
        audioClip = sfx_trapSound;


        if (interactSymbol != null)
        {
            interactSymbol.symbolState = InteractSymbol.SymbolState.Trap;
            interactSymbol.SetSymbol();
        }

    }


    // Update is called once per frame
    void Update()
    {
        // Always search for players nearby if trap is enabled.
        if (b_trapEnabled && traptype != Traptype.Constant)
        {
            CmdCreateDetectionRadius();
        }
    }

    #region DetectionRadius
    [Command(requiresAuthority = false)]
    public void CmdCreateDetectionRadius()
    {
        RpcCreateDetectionRadius();
    }

    [ClientRpc]
    public void RpcCreateDetectionRadius()
    {
        Collider[] collisionObject = Physics.OverlapSphere(transform.position, f_trapRadius, triggerLayerMask);

        if (collisionObject.Length > 0)
        {
            CmdSendTrapFire();
        }
    }
    #endregion

    #region Effect Radius
    //This grabs players within this sphere and allows us to manipulate them in AffectPlayer()

    // This is technically synchronized across all players due to RpcTrapFire() All players have this radius active, and all players then if within this radius will be influenced by affect player.
    // Point to learn. Not everything has to be a Command into a client RPC.
    // This value change is synchronized however within the player as a [SyncVar]
    public void CreateEffectRadius()
    {
        Collider[] collisionObject = Physics.OverlapSphere(transform.position, f_trapEffectRadius, triggerLayerMask);

        if (collisionObject.Length == 0)
        {
            influencedObjects.Clear();
        }

        foreach (Collider col in collisionObject)
        {
            if (!influencedObjects.Contains(col.gameObject))
            {
                influencedObjects.Add(col.gameObject);
            }
        }
    }
    #endregion

    #region OnInteract
    public override void CmdOnInteract(bool interactionState)
    {
        base.CmdOnInteract(interactionState);

        if (b_trapEnabled == false && b_interacting == true && b_canEnable == true)
        {
            if (b_allowAll || playerTransform.CompareTag("Monster"))
            {

                TrapToggle();
                if (traptype == Traptype.Constant)
                {

                    CmdSendTrapFire();
                }
            }

        }

        // Here's how to react if the trap type is constant and is currently Active
        if (b_trapIsActive && traptype == Traptype.Constant)
        {
            CmdDisableActiveTrap();
        }

    }

    //This is used when a separate object is used to activate this trap. This could be called from a separate script or separate object.
    public void ExternalInteract()
    {

    }

    public void ExternalDisable()
    {
        if (b_trapIsActive && traptype == Traptype.Constant)
        {
            CmdDisableActiveTrap();
        }
    }
    #endregion

    #region Trap Fire
    //How to react when the trap is fired. 

    [Command(requiresAuthority = false)]
    public void CmdSendTrapFire()
    {
        RpcTrapFire();
        CmdInvokeExtraEffects();
    }


    [ClientRpc]
    public void RpcTrapFire()
    {

        if (traptype == Traptype.SingleFire && b_trapEnabled && b_canSound)
        {
            //Play Sound.
            AudioManager.instance.PlaySound(sfx_trapSound, transform.position, false, f_volumeOverride);

            // Disable its ability to sound again.
            b_canSound = false;

            //Create the radius instantly once.
            //This works and syncs across all players.
            //CreateEffectRadius();

            StartCoroutine(CheckForObjects());

            // Begin a cooldown.
            StartCoroutine(TrapCooldown(f_cooldown));

            ChangeLayerType();

            // Disable the trap.
            b_trapEnabled = false;

            if (interactSymbol != null)
            {
                interactSymbol.symbolState = InteractSymbol.SymbolState.Trap;
                interactSymbol.SetSymbol();
            }
        }

        if (traptype == Traptype.Constant && b_canSound)
        {
            //Play Sound.
            loopedAudio = AudioManager.instance.PlaySound(sfx_trapSound, transform.position, true, f_volumeOverride);

            // Disable its ability to sound again.
            b_canSound = false;

            // Disable the trap.
            b_trapEnabled = false;

            // Show that the trap is active, as in constantly turned on
            b_trapIsActive = true;
            // Due to order of operations on when bool flags are called each Start Coroutine has to be set separately.
            StartCoroutine(CheckForObjects());

            if (interactSymbol != null)
            {
                interactSymbol.symbolState = InteractSymbol.SymbolState.TrapActive;
                interactSymbol.SetSymbol();
            }
        }

        if (traptype == Traptype.Oneshot && b_trapEnabled && b_canSound)
        {
            //Play Sound.
            AudioManager.instance.PlaySound(sfx_trapSound, transform.position, false, f_volumeOverride);

            //CreateEffectRadius();

            StartCoroutine(CheckForObjects());

            ChangeLayerType();

            // Disable its ability to sound again.
            b_canSound = false;

            // Disable the trap.
            b_trapEnabled = false;

            // Disable traps ability to be renabled.
            b_canEnable = false;

            //Disables the symbol so player knows they can't reactivate it.
            if (interactSymbol != null)
            {
                interactSymbol.gameObject.SetActive(false);
            }
        }
        //
    }

    private IEnumerator CheckForObjects()
    {
        //This is for constants
        while (b_trapIsActive)
        {
            yield return new WaitForSeconds(1f);
            CreateEffectRadius();

            // Due this one firing constantly we multiply it by a decimal to reduce the impact on sanity.
            AffectPlayer(0.3f);

        }

        //This is for SingleFires and Oneshots
        if (traptype != Traptype.Constant && b_trapEnabled)
        {
            //Create the radius once, just to see whats around it.
            CreateEffectRadius();

            AffectPlayer(5f);
        }
        // Reset the List
        influencedObjects.Clear();

    }


    IEnumerator TrapCooldown(float delay)
    {
        yield return new WaitForSeconds(delay);
        b_canSound = true;
        b_canEnable = true;
    }

    private void ChangeLayerType()
    {
        gameObject.layer = LayerMask.NameToLayer("TriggeredTrap");
        StartCoroutine(RevertLayerType());
    }

    IEnumerator RevertLayerType()
    {
        yield return new WaitForSeconds(3f);
        gameObject.layer = LayerMask.NameToLayer("Interactable");

    }

    #endregion

    #region Affect Player

    private void AffectPlayer(float f_InfluenceMultiplier)
    {
        //If players are within the radius
        if (influencedObjects.Count > 0)
        {
            foreach (GameObject player in influencedObjects)
            {
                if (player.GetComponent<PlayerBehavior>() != null)
                {
                    player.GetComponent<PlayerBehavior>().f_sanity -= f_effectInsanityInfluence * f_InfluenceMultiplier;
                }
            }

        }

    }

    #endregion

    #region Disable Active
    // If the trap type is constant then this is how it's disabled.
    [Command(requiresAuthority = false)]
    public void CmdDisableActiveTrap()
    {
        RpcDisableActiveTrap();
        CmdInvokeExtraEffects();
    }

    //For all clients
    [ClientRpc]
    public void RpcDisableActiveTrap()
    {
        if (interactSymbol != null)
        {
            interactSymbol.symbolState = InteractSymbol.SymbolState.Trap;

            interactSymbol.SetSymbol();
        }
        // Stop looping the sound
        AudioManager.instance.StopLoopingSound(loopedAudio);

        //disable the trap
        b_trapIsActive = false;

        // Begin a cooldown
        StartCoroutine(TrapCooldown(f_cooldown));

        if (interactSymbol != null)
        {
            interactSymbol.symbolState = InteractSymbol.SymbolState.Trap;

            interactSymbol.SetSymbol();
        }

    }
    #endregion

    #region Toggle
    [Command(requiresAuthority = false)]
    public void TrapToggle()
    {

        if (b_trapEnabled)
        {
            //Debug.Log("Toggling false");
            b_trapEnabled = false;

            if (interactSymbol != null)
            {
                interactSymbol.symbolState = InteractSymbol.SymbolState.Trap;
                interactSymbol.SetSymbol();
            }

        }
        else
        {
            Debug.Log("Toggling true");
            b_trapEnabled = true;
            b_canEnable = false;

            if (interactSymbol != null)
            {
                interactSymbol.symbolState = InteractSymbol.SymbolState.TrapSet;
                interactSymbol.SetSymbol();
            }
        }
    }
    #endregion

    #region Bool Change
    private void TrapEnabled(bool oldState, bool newState)
    {
        b_trapEnabled = newState;

    }

    private void TrapActive(bool oldState, bool newState)
    {
        b_trapIsActive = newState;
    }
    #endregion

    #region Debug
    void OnDrawGizmosSelected()
    {
        // Display the explosion radius when selected
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, f_trapRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, f_trapEffectRadius);
    }
    #endregion
}
