using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GetInputAction : MonoBehaviour
{

    public InputSystem_Enabler inputSystem_Enabler;
    public void OnEnable()
    {
        InputSystem_Enabler.onInteractPressed += InputTest;
        InputSystem_Enabler.onAbilityPressed += InputTestBool;
        InputSystem_Enabler.onCrouchPressed += InputTest;
    }
    public void OnDisable()
    {
        InputSystem_Enabler.onInteractPressed -= InputTest;
        InputSystem_Enabler.onAbilityPressed -= InputTestBool;
        InputSystem_Enabler.onCrouchPressed -= InputTest;
    }

    public void InputTest()
    {
        Debug.Log("Testing Input");
    }


        public void InputTestBool(bool pressed)
    {
        Debug.Log("Testing Input state: " + pressed);
    }
}
