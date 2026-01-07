using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PhoneBehavior : MonoBehaviour
{

   [Header("Vital References")]
    private Camera overlayCamera; // This is the camera that brings up the overlay.
    public GameObject player;
    public Transform cameraRoot;
    private PlayerBehavior playerBehavior;
    private FirstPersonCamera playerCamera;

    [Header("Raycast Interactions")]
    public LayerMask overlayMask;

    [Header("Positional Offsets")]
    public Vector3 locationOffset;
    public float distanceOffset;

    public static event Action<bool> ToggleInputField;

    public void OnEnable()
    {
        InputSystem_Enabler.onPauseToggled += RemovePhone;
    }

    public void OnDisable()
    {
        InputSystem_Enabler.onPauseToggled -= RemovePhone;
    }

    private void Awake()
    {
        GameObject overlayCamObject = GameObject.Find("Overlay Camera");

        // Check if the GameObject exists
        if (!overlayCamObject)
        {
            Debug.LogError($"Fatal Error for Object: {gameObject.name} - 'Overlay Camera' GameObject not found! Disabling object to prevent crashes.");
            gameObject.SetActive(false);
            return;
        }

        // Check if the Camera component exists
        overlayCamera = overlayCamObject.GetComponent<Camera>();

        if (!overlayCamera)
        {
            Debug.LogError($"Fatal Error for Object: {gameObject.name} - Found 'Overlay Camera' but it has no Camera component! Disabling object.");
            gameObject.SetActive(false);
        }

        playerBehavior = player.GetComponent<PlayerBehavior>();
        playerCamera = player.GetComponent<FirstPersonCamera>();
    }

    #region Raycast
    public void RayCastFromOverlayCamera()
    {
        Ray ray = overlayCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Correct Debug.DrawRay: Using ray.origin and ray.direction * distance
        Debug.DrawRay(ray.origin, ray.direction * 1f, Color.yellow, 5f);

        if (Physics.Raycast(ray, out hit, 1f, overlayMask))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Overlay"))
            {
                Debug.Log("Hit Object on Overlay Layer: " + hit.collider.gameObject.name);

                PhoneButtons phoneButtons = hit.collider.gameObject.GetComponent<PhoneButtons>();

                // Grab the phone buttons component of the object. This is for the separate object from the one we initially interacted with.
                if (phoneButtons != null)
                {
                    Debug.Log("Hit Object on Overlay Layer: " + hit.collider.gameObject.name + " with PhoneButtons");
                    phoneButtons.OnButtonPress();
                }
            }
        }
        else
        {
            StartCoroutine(WaitToRemove());
        }
    }
    #endregion

    #region Bring Up Object 
    public void BringUpPhone()
    {
        playerCamera = player.GetComponent<FirstPersonCamera>(); // Grab the FPS Camera Script from the player

        playerCamera.enabled = false; // Disable the FPS Camera script.

        playerBehavior.onInteract += RayCastFromOverlayCamera; // Subscribe to the onInteract event called by the player behavior script

        playerBehavior.SetCursorState(false); // Unlock the cursor

        transform.gameObject.SetActive(true); // Finally set the overlay object to active

        overlayCamera.transform.position = cameraRoot.position; // Move the camera to the player to get the natural lighting of the environment

        ToggleInputField.Invoke(true);
    }
    #endregion

    #region Remove Object
    public void RemovePhone()
    {
        if (playerBehavior != null)
        {
            playerBehavior.onInteract -= RayCastFromOverlayCamera;

            playerBehavior.SetCursorState(true);

            playerBehavior.b_inChat = false;
        }
        if (playerCamera != null)
        {
            playerCamera.enabled = true;
        }

        ToggleInputField.Invoke(false);

        transform.gameObject.SetActive(false);
    }

    //This is to prevent the player from immediately raycasting again after attempting to leave the overlay object causing an infinite loop of pulling up the overlay again.
    IEnumerator WaitToRemove()
    {
        yield return new WaitForSeconds(.05f);
        RemovePhone();
    }
    #endregion
}
