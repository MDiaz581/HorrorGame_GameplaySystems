using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorBehavior : MonoBehaviour
{
    [Header("Elevator Object")]
    [Tooltip("The actual elevator object that will move when changing location")]
    public GameObject objectToMove;
    [Header("Movement Properties")]
    [Tooltip("The points the elevator will travel to in the order of the list. If button uses integer 0 it will travel to the first in the list, if it uses 2 it will travel to the 3rd on the list")]
    public Transform[] targets;
    public float elevatorSpeed = 1f;
    private Vector3 lastPosition;
    public Vector3 platformVelocity { get; private set; }// Expose velocity to player
    [Header("Button Information")]
    [Tooltip("Every Button object that this object will listen for.")]
    public ButtonBehavior[] buttonBehavior; //With this we can take and listen for multiple buttons each with different integer values
    [Tooltip("Material Button intially has.")]
    public Material initialMaterial; // The initial Material
    [Tooltip("Material Button will change to.")]
    public Material materialToChangeTo; // The material we want to change to.
    public Coroutine moveCoroutine;
    [Header("Teleport Point")]
    [Tooltip("Location Monster should teleport to. Should be in the center of elevator at least 1 unit from the ground.")]
    public Transform teleportPoint;
    [Header("Door information")]
    [Tooltip("Reference to every door elevator should have access to.")]
    public ElevatorDoor_OpenClose[] elevatorDoors;
    private int floorID = 1; // Keeps track of which floor the elevator is on.  
    [Header("Vital Flags")]
    public bool isMoving;
    [Header("Audio")]
    public AudioClip sfx_moveSound;
    private AudioSource audioSource;

    void OnEnable()
    {
        foreach (ButtonBehavior button in buttonBehavior)
        {
            if (button != null) // Ensure it's not null before subscribing
            {
                button.buttonAction += Action; // Subscribe to all the buttons that this object has access to. Added from the inspector.
            }
        }
    }

    void OnDisable()
    {
        foreach (ButtonBehavior button in buttonBehavior)
        {
            if (button != null) // Ensure it's not null before unsubscribing
            {
                button.buttonAction -= Action;
            }
        }
    }

    void Start()
    {
        lastPosition = objectToMove.transform.position;
        OpenDoors(); // This opens the door at the position of the floorID;
    }

    //This is invoked when a button is interacted with.
    void Action(int i, GameObject sender)
    {
        if (!isMoving)
        {
            //Debug.Log($"Sender was: {sender}");

            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
            }

            moveCoroutine = StartCoroutine(MoveToPosition(targets[i].position, elevatorSpeed, sender));

            //Change the value after we close the doors so we know which floor we're heading to.
            floorID = i + 1;

            Debug.Log("Moving to floor: " + (i + 1));


        }
    }


    //What to do when we reach the destination.
    void ReachedDestination(GameObject sender)
    {
        platformVelocity = Vector3.zero; // Ensure platform velocity is 0. Important for the player who's keeping track of this.       

        //Reset Material
        if (sender.GetComponent<ButtonBehavior>().rendererToTarget.Length > 0 && initialMaterial != null)
        {
            foreach (Renderer renderer in sender.GetComponent<ButtonBehavior>().rendererToTarget)
            {
                renderer.material = initialMaterial;
            }
        }

        OpenDoors(); // Open door at the current floor.

        if (audioSource != null)
        {
            AudioManager.instance.StopLoopingSound(audioSource);
        }

    }


    IEnumerator MoveToPosition(Vector3 destination, float speed, GameObject sender)
    {
        CloseDoors();

        //Change the material of the button to glow!
        if (!isMoving && sender.GetComponent<ButtonBehavior>().rendererToTarget.Length > 0 && materialToChangeTo != null)
        {
            foreach (Renderer renderer in sender.GetComponent<ButtonBehavior>().rendererToTarget)
            {
                renderer.material = materialToChangeTo;
            }
        }

        isMoving = true;     // This has to be activate as soon as the button is pressed to prevent wonky button spam issues.

        yield return new WaitForSeconds(2); // Wait for doors to close

        //Only play move noise as the elevator is moving.
        if (sfx_moveSound != null)
        {
            audioSource = AudioManager.instance.PlaySound(sfx_moveSound, teleportPoint.position, true, objectToMove.transform, 1f);
        }

        while (objectToMove.transform.position != destination)
        {

            //isMoving = true;
            objectToMove.transform.position = Vector3.MoveTowards(objectToMove.transform.position, destination, speed * Time.deltaTime);

            // Calculate velocity (movement per frame)
            platformVelocity = (objectToMove.transform.position - lastPosition) / Time.deltaTime;
            lastPosition = objectToMove.transform.position;
            yield return null; // Wait until the next frame
        }

        ReachedDestination(sender);

        yield return new WaitForSeconds(1.5f);
        isMoving = false; // Reset the condition.

        yield return null;
    }


    #region DoorHandlers

    void CloseDoors()
    {
        if (!isMoving)
        {
            foreach (ElevatorDoor_OpenClose elevatorDoor in elevatorDoors)
            {
                if (elevatorDoor.int_doorFloorID == 0 || elevatorDoor.int_doorFloorID == floorID)
                {
                    elevatorDoor.ElevatorDoorAnimation(1); // Close doors
                }
            }
        }
    }

    void OpenDoors()
    {
        foreach (ElevatorDoor_OpenClose elevatorDoor in elevatorDoors)
        {
            if (elevatorDoor.int_doorFloorID == 0 || elevatorDoor.int_doorFloorID == floorID)
            {
                elevatorDoor.ElevatorDoorAnimation(-1); // Open door
            }
        }
    }


    #endregion

}
