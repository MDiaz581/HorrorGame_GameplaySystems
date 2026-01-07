using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PhoneButtons : MonoBehaviour
{

    public enum MenuButtonType
    {
        Chat,
        Contacts,
        Base
    }
    public static event Action<bool> ToggleInputField;

    public static event Action<int> MoveChatField;

    public static event Action<int> ToggleMenuLocation;

    public GameObject chatButtons;
    public GameObject contactButtons;

     public UnityEvent onPressed; // Assign UI button functions here in the Inspector

    public void OnButtonPress()
    {
        Debug.Log("buttonPressed");
        onPressed.Invoke();
    }

    public void DebugFunction(bool b_test)
    {       
        Debug.Log($"Test function fired with bool: {b_test}");
    }

    public void InputToggle(bool toggle)
    {
        ToggleInputField.Invoke(toggle);
    }

    public void MoveChatbox(int units)
    {
        MoveChatField.Invoke(units);
    }

    public void ToggleMenu(int roomID)
    {
        ToggleMenuLocation.Invoke(roomID);

        switch (roomID)
        {
            case (int)MenuButtonType.Chat:
                contactButtons.SetActive(false);
                chatButtons.SetActive(true);
                break;
            case (int)MenuButtonType.Contacts:
                chatButtons.SetActive(false);
                contactButtons.SetActive(true);
                break;
            case (int)MenuButtonType.Base:
                Debug.Log("Not Set yet");
                break;
            default:
                Debug.Log("Phone menu out of bounds");
                break;
        }
    }

}
