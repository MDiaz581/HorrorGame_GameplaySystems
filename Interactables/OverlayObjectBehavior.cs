using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class OverlayObjectBehavior : InteractableBehavior
{
    [Header("Vital References")]
    public GameObject objectToBringUp;
    public Camera overlayCamera; // This is the camera that brings up the overlay.
    private GameObject player;
    private PlayerBehavior playerBehavior;
    private FirstPersonCamera playerCamera;

    [Header("Raycast Interactions")]
    public LayerMask overlayMask;

    [Header("Flags")]
    private bool b_ObjectActive;

    [Header("Positional Offsets")]
    public Vector3 locationOffset;
    public float distanceOffset;

    [Header("Object Rotation")]
    [Tooltip("Whether or not the player should be able to rotate this object")]
    public bool b_canBeRotated;
    public float rotationSpeed = 5f;
    public Transform modelPoint;
    [Tooltip("This only affects the intial rotation of the model, therefore requires modelPoint")]
    public Vector3 rotationOffset;
    private Quaternion currentRotation = Quaternion.identity;


    public void OnEnable()
    {
        InputSystem_Enabler.onPauseToggled += RemoveObject;
    }

    public void OnDisable()
    {
        InputSystem_Enabler.onPauseToggled -= RemoveObject;
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
    }

    public void Update()
    {
        if (b_ObjectActive)
        {
            // This allows us to change the position of the object compared to the overlay camera in realtime so we don't have to constantly adjust.
            objectToBringUp.transform.position = overlayCamera.transform.TransformPoint(locationOffset + Vector3.forward * distanceOffset);  // Set the position of the overlay object.

            //Object Rotation
            SpinObject();

            // Detect if we're recieving that input
            if(playerBehavior.inputSystemLookVector == Vector3.zero)
            {
                DetectScreenEdge();
            }
        }
    }

    public override void CmdOnInteract(bool interactionState)
    {
        base.CmdOnInteract(interactionState);

        // If interacted with object
        if (b_interacting)
        {
            if (!b_ObjectActive)
            {
                BringUpObject();
            }
            else
            {
                RayCastFromOverlayCamera();
            }
        }
    }

    #region Raycast
    public void RayCastFromOverlayCamera()
    {
        Ray ray = overlayCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Correct Debug.DrawRay: Using ray.origin and ray.direction * distance
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.cyan, 5f);

        if (Physics.Raycast(ray, out hit, 100f, overlayMask))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Overlay"))
            {
                Debug.Log("Hit Object on Overlay Layer: " + hit.collider.gameObject.name);

                // Grab the interactable Behavior component of the overlay object. This is for the separate object from the one we initially interacted with.
                if (hit.collider.gameObject.GetComponent<InteractableBehavior>() != null)
                {
                    Debug.Log("Hit Object on Overlay Layer: " + hit.collider.gameObject.name + " with Interactable Behavior!");
                    hit.collider.gameObject.GetComponent<InteractableBehavior>()?.Invoke(nameof(OnOverlayInteract), 0f);
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
    public void BringUpObject()
    {
        b_ObjectActive = true;

        player = playerTransform.gameObject; // Grab the player

        playerCamera = player.GetComponent<FirstPersonCamera>(); // Grab the FPS Camera Script from the player

        playerCamera.enabled = false; // Disable the FPS Camera script.

        playerBehavior = player.GetComponent<PlayerBehavior>(); // Get the PlayerBehavior Script from the player

        playerBehavior.onInteract += RayCastFromOverlayCamera; // Subscribe to the onInteract event called by the player behavior script

        playerBehavior.specialState = PlayerBehavior.SpecialStates.Interacting; // Set the player's state to interacting this will disable certain features from the player

        playerBehavior.SetCursorState(false); // Unlock the cursor

        objectToBringUp.SetActive(true); // Finally set the overlay object to active

        overlayCamera.transform.position = playerTransform.position + new Vector3(0, .5f, 0); // Move the camera to the player to get the natural lighting of the environment

        objectToBringUp.transform.position = overlayCamera.transform.TransformPoint(locationOffset + Vector3.forward * distanceOffset);  // Set the position of the overlay object.

        currentRotation = Quaternion.identity; //Reset Rotation

        objectToBringUp.transform.rotation = overlayCamera.transform.rotation;

        if (modelPoint != null)
        {
            modelPoint.transform.rotation = overlayCamera.transform.rotation * Quaternion.Euler(rotationOffset);
        }

    }
    #endregion

    #region Remove Object
    public void RemoveObject()
    {
        if (playerBehavior != null)
        {
            playerBehavior.onInteract -= RayCastFromOverlayCamera;
            playerBehavior.specialState = PlayerBehavior.SpecialStates.Normal;
            playerBehavior.SetCursorState(true);
        }
        if (playerCamera != null)
        {
            playerCamera.enabled = true;
        }

        b_ObjectActive = false;
        objectToBringUp.SetActive(false);

        // Only nullify variables AFTER we are done using them
        player = null;
        playerBehavior = null;
        playerCamera = null;

    }

    //This is to prevent the player from immediately raycasting again after attempting to leave the overlay object causing an infinite loop of pulling up the overlay again.
    IEnumerator WaitToRemove()
    {
        yield return new WaitForSeconds(.05f);
        RemoveObject();
    }
    #endregion

    #region Object Rotation
    private void SpinObject()
    {
        if (b_canBeRotated && playerBehavior.inputSystemLookVector != Vector3.zero)
        {
            Vector2 lookVector = playerBehavior.inputSystemLookVector;

            // Convert 2D look vector into a 3D rotation (assuming Yaw (Y) and Pitch (X))
            Quaternion lookRotation = Quaternion.Euler(-lookVector.y * rotationSpeed, -lookVector.x * rotationSpeed, 0);

            // Accumulate the rotation
            currentRotation *= lookRotation;

            // Apply the rotation relative to the overlay camera
            objectToBringUp.transform.rotation = overlayCamera.transform.rotation * currentRotation;
        }
    }
    #endregion

    #region EdgeLookup

    private void DetectScreenEdge()
    {
        float edgeThreshold = 2f; // Pixels from the edge to trigger
        Vector3 mousePos = Input.mousePosition;

        bool isAtEdge =
            mousePos.x <= edgeThreshold ||
            mousePos.x >= Screen.width - edgeThreshold ||
            mousePos.y <= edgeThreshold ||
            mousePos.y >= Screen.height - edgeThreshold;

        if (isAtEdge)
        {
            Debug.Log("Mouse is at the edge of the screen!");
            RemoveObject();
        }
    }
    #endregion
}
