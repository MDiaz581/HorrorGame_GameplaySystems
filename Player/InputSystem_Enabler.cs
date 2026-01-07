using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputSystem_Enabler : MonoBehaviour
{

	[Header("Character Input Values")]
	public Vector2 move;
	public Vector2 look;
	public bool b_jump;
	public bool b_sprint;

	public static event Action onPauseToggled;

	public static event Action onInteractPressed;

	public static event Action<bool> onAbilityPressed;

	public static event Action onCrouchPressed;

	public static event Action onTransformPressed;

	public static event Action onAttackPressed;

	public static event Action onTestButtonPressed;

	public bool b_interact;
	public bool b_pause;
	public bool b_flashlight;

	public bool b_ability;

	public bool b_crouch;

	public bool b_attacking;

	[Header("Movement Settings")]
	public bool analogMovement;

	[Header("Mouse Cursor Settings")]
	public bool cursorInputForLook = true;

	#region Input Receivers

	//This is where we get the input from the actionmap.
	// In order for the input values to be recieved they must first be pulled from the Input Action Asset by using On["Specific Action Name"](InputValue value) it HAS to be named On["Specific Action Name"] to get that specific action.
	// This is VITAL or the script won't be able to pull any information from the inputs. Next its a matter of setting that value to a usable variable, such as a float, bool, or vector.
	// To go about setting the value we need a new function which sets a public variable to the InputValue value. We feed the function the value so it constantly sets the usable variable to its value. 
	public void OnLook(InputValue value)
	{
		if (cursorInputForLook)
		{
			LookInput(value.Get<Vector2>());
		}
	}

	public void OnMove(InputValue value)
	{
		MoveInput(value.Get<Vector2>());
	}

	public void OnSprint(InputValue value)
	{
		b_sprint = !b_sprint;
	}

	
	public void OnFlashlight(InputValue value)
	{
		FlashlightInput(value.isPressed);
	}
	
	public void OnCrouch(InputValue value)
	{
		b_crouch = !b_crouch;
		CrouchInput(value.isPressed);
	}

	public void OnPause(InputValue value)
	{
		PauseInput(value.isPressed);
	}

	public void OnInteract(InputValue value)
	{
		InteractInput(value.isPressed);
	}

	public void OnInteract2(InputValue value)
	{
		InteractInput(value.isPressed);
	}

	public void OnTransform(InputValue value)
	{
		TransformInput(value.isPressed);
	}

	public void OnAttack(InputValue value)
	{
		b_attacking = !b_attacking;
		AttackInput(value.isPressed);
	}

	public void OnTestButton(InputValue value)
	{
		TestButtonInput(value.isPressed);
	}
	#endregion

	#region Event Calls
	public void MoveInput(Vector2 newMoveDirection)
	{
		move = newMoveDirection;
	}


	public void LookInput(Vector2 newLookDirection)
	{
		look = newLookDirection;
	}

	public void SprintInput(bool newSprintState)
	{
		b_sprint = newSprintState;
	}

	public void FlashlightInput(bool newFlashlightState)
	{
		b_flashlight = newFlashlightState;

		b_ability = newFlashlightState;

		if (onAbilityPressed != null)
		{
			onAbilityPressed?.Invoke(newFlashlightState);
		}
	}

	public void PauseInput(bool newPauseState)
	{
		b_pause = newPauseState;

		if (onPauseToggled != null)
		{
			onPauseToggled?.Invoke();
		}
	}

	public void InteractInput(bool newInteractState)
	{
		b_interact = newInteractState;


		if (onInteractPressed != null)
		{
			onInteractPressed?.Invoke();
		}
	}

	public void CrouchInput(bool newCrouchState)
	{

		if (onCrouchPressed != null)
		{
			onCrouchPressed?.Invoke();
		}
	}

	public void TransformInput(bool newTransformState)
	{
		if (onTransformPressed != null)
		{
			onTransformPressed?.Invoke();
		}
	}

	public void AttackInput(bool newAttackState)
	{
		if (onAttackPressed != null)
		{
			onAttackPressed?.Invoke();
		}
	}

	public void TestButtonInput(bool newTestState)
	{
		if (onTestButtonPressed != null)
		{
			onTestButtonPressed?.Invoke();
		}
	}

	#endregion
}
