using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakerBoxBehavior : MonoBehaviour
{
    [Header("Fuse State Information")]
    [Tooltip("Determines if this fuse is the Master Fuse.")]
    public bool b_MasterFuse;
    [Tooltip("Determines which fuses this master fuse has control over.")]
    public BreakerBoxBehavior[] breakerboxBehaviors; // This is for the master switch to control all other fuses.

    private bool b_MasterFuseOn; // Child fuses without this activated cannot activate unless master fuse is on.

    public enum FuseState
    {
        FuseOn,
        FuseOff,
        Empty
    }
    [Tooltip("Determines the state the fuse is in.")]
    public FuseState fuseState;
    [Tooltip("The fuse model.")]
    public GameObject fuseObject;
    [Tooltip("The button this object gets activated by. Normally it would be on the same object in the inspector.")]

    [Header("Button Information")]
    public ButtonBehavior buttonBehavior; // The button behavior this button activates with.

    [Header("Light Switches")]
    [Tooltip("Every light switch this fuse has control over.")]
    public LightSwitchBehavior[] lightSwitchBehaviors; // Light switches have control over every light so including every light also is redundant

    void OnEnable()
    {
        if (buttonBehavior != null) // Ensure it's not null before subscribing
        {
            buttonBehavior.buttonAction += Action; // Subscribe to all the buttons that this object has access to. Added from the inspector.
        }

    }

    void OnDisable()
    {
        if (buttonBehavior != null) // Ensure it's not null before unsubscribing
        {
            buttonBehavior.buttonAction -= Action;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //Initialize Fuse
        switch (fuseState)
        {
            // If it's empty turn off all lights connected to this script, if it's the master fuse turn off all fuses associated with this.
            case FuseState.Empty: 
                fuseObject.SetActive(false);
                ToggleFuseState(false);
                MasterSwitchOff();
                break;
            // if fuse is on Turn on
            case FuseState.FuseOn:
                ToggleFuseState(true);
                MasterSwitchOn();

                break;
            // if fuse is off Turn off
            case FuseState.FuseOff:
                ToggleFuseState(false);
                MasterSwitchOff();
                break;
        }
    }

    public void Action(int i, GameObject sender)
    {
        switch (fuseState)
        {
            // If it's empty and the player has a fuse, enable the fuse, and if it's the master fuse turn on the masterfuse.
            case FuseState.Empty:
                if (sender.GetComponent<InteractableBehavior>().playerTransform.GetComponent<PlayerBehavior>().int_fuses >= 1)
                {
                    --sender.GetComponent<InteractableBehavior>().playerTransform.GetComponent<PlayerBehavior>().int_fuses;
                    fuseObject.SetActive(true);
                    ToggleFuseState(true);
                    fuseState = FuseState.FuseOn;
                    if (b_MasterFuse)
                    {
                        b_MasterFuseOn = true;
                    }
                }
                break;
            // if fuse is on Turn off
            case FuseState.FuseOn:

                if (b_MasterFuseOn || b_MasterFuse)
                {
                    ToggleFuseState(false);
                    fuseState = FuseState.FuseOff;
                    MasterSwitchOff();
                }

                break;
            // if fuse is off Turn on
            case FuseState.FuseOff:
                if (b_MasterFuseOn || b_MasterFuse)
                {
                    ToggleFuseState(true);
                    fuseState = FuseState.FuseOn;
                    MasterSwitchOn();
                }
                break;
        }
    }

    // Toggles the lights
    public void ToggleFuseState(bool fuseState)
    {
        foreach (var lightswitch in lightSwitchBehaviors)
        {
            if (lightswitch != null)
            {
                lightswitch.FuseToggle(fuseState);
            }
        }
    }

    // How to proceed when the masterswitch is turned off. Turns off all fuses associated with the master.
    private void MasterSwitchOff()
    {
        if (b_MasterFuse)
        {
            b_MasterFuseOn = false;

            foreach (var fuse in breakerboxBehaviors)
            {
                if (fuse != null)
                {
                    if (fuse.fuseState != FuseState.Empty)
                    {
                        fuse.fuseState = FuseState.FuseOff;
                        fuse.ToggleFuseState(false);
                        fuse.b_MasterFuseOn = false;
                    }
                }
            }
        }
    }

    // How to proceed when the masterswitch is turned on. Unlike the off script this does not turn on all the fuses. The fuses must be individually turned back on.
    private void MasterSwitchOn()
    {
        if (b_MasterFuse)
        {
            b_MasterFuseOn = true;

            foreach (var fuse in breakerboxBehaviors)
            {
                if (fuse != null)
                {
                    fuse.b_MasterFuseOn = true;
                }
            }
        }
    }
}
