using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FuseBoxBehavior : MonoBehaviour
{
    public static event Action globalPowerToggle;

    public ButtonBehavior[] buttonBehaviors;

    public bool b_hasFuse;

    void OnEnable()
    {
        foreach (ButtonBehavior button in buttonBehaviors)
        {
            if (button != null) // Ensure it's not null before subscribing
            {
                button.buttonAction += Action; // Subscribe to all the buttons that this object has access to. Added from the inspector.
            }
        }
    }

    void OnDisable()
    {
        foreach (ButtonBehavior button in buttonBehaviors)
        {
            if (button != null) // Ensure it's not null before unsubscribing
            {
                button.buttonAction -= Action;
            }
        }
    }


    private void Action(int info, GameObject sender)
    {
        SendTogglePower();
    }

    private void SendTogglePower()
    {
        Debug.Log("Sending Info to all lights that can hear");
        globalPowerToggle?.Invoke();
    }
}
