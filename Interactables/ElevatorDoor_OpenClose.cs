using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorDoor_OpenClose : MonoBehaviour
{
    [Tooltip("Which floor is this door on. If door ID is 0 it will always close or open no matter what floor. Reserve for interior elevator door.")]
    public int int_doorFloorID;
    public Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }


    public void ElevatorDoorAnimation(float speed)
    {
        //Debug.LogWarning(nameof(gameObject) + " is moving with ID: " + int_doorFloorID);
        
        animator.SetFloat("DoorSpeed", speed);

        if (speed < 0) // If reversing, make sure it starts at the end
        {
            animator.CrossFade("Open/CloseDoor", 0f, 0, 1f); // Start at the end of the animation
        }
        else // If playing normally, start from the beginning
        {
            animator.CrossFade("Open/CloseDoor", 0f, 0, 0f);
        }
    }

}
