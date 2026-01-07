using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInitialization : NetworkBehaviour
{
    public bool b_gameStart = false;
    private InputSystem_Enabler inputSystem;
    private CharacterController characterController;
    private PauseBehavior pauseBehavior;
    private FirstPersonCamera firstPersonCamera;
    private PlayerBehavior playerBehavior;
    public GameObject lowLight;

    public override void OnStartAuthority()
    {
        //if (!b_gameStart) return;
        playerBehavior = GetComponent<PlayerBehavior>();
        playerBehavior.enabled = true;
        
        PlayerInput playerInput = GetComponent<PlayerInput>();
        playerInput.enabled = true;

        inputSystem = GetComponent<InputSystem_Enabler>();
        inputSystem.enabled = true;

        characterController = GetComponent<CharacterController>();
        characterController.enabled = true;

        pauseBehavior = GetComponent<PauseBehavior>();
        pauseBehavior.enabled = true;      

        firstPersonCamera = GetComponent<FirstPersonCamera>();
        firstPersonCamera.enabled = true;

        lowLight.SetActive(true);  
    }

}
