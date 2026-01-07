using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class KeypadBehavior : NetworkBehaviour
{

    public ButtonBehavior[] buttonBehaviors;
    public int int_buttontracker;

    [SyncVar]
    public int int_combination;

    public int int_1;
    public int int_2;
    public int int_3;
    public int int_4;

    public Renderer[] overlayKeypadLightRenderers;
    public Renderer[] keypadLightRenderers;

    public Material initialMaterial;
    public Material greenMaterial;
    public Material redMaterial;

    private bool b_unlocked;
    private bool b_active;

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

    void Action(int number, GameObject button)
    {
        if (b_unlocked || !b_active) return;

        overlayKeypadLightRenderers[int_buttontracker].material = greenMaterial;
        keypadLightRenderers[int_buttontracker].material = greenMaterial;


        switch (int_buttontracker)
        {
            case 0:
                int_1 = number;

                ++int_buttontracker;
                break;

            case 1:
                int_2 = number;

                ++int_buttontracker;
                break;

            case 2:
                int_3 = number;

                ++int_buttontracker;
                break;

            case 3:
                int_4 = number;

                CompareAndResetNumbers();
                break;

            default:

                int_buttontracker = 0;
                break;
        }

    }


    void Awake()
    {
        b_active = true;

        int digit1 = Random.Range(1, 10); // 1-9
        int digit2 = Random.Range(1, 10); // 1-9
        int digit3 = Random.Range(1, 10); // 1-9
        int digit4 = Random.Range(1, 10); // 1-9

        int_combination = (digit1 * 1000) + (digit2 * 100) + (digit3 * 10) + digit4;

        Debug.Log($"Generated Keypad Code: {int_combination}");
    }

    public void CompareAndResetNumbers()
    {
        int inputValue = int_1 * 1000 + int_2 * 100 + int_3 * 10 + int_4;

        if (inputValue == int_combination)
        {
            Debug.Log("Correct Code Entered!");

            b_unlocked = true;
        }
        else
        {
            foreach (Renderer renderer in overlayKeypadLightRenderers)
            {
                renderer.material = redMaterial;                
            }
            foreach (Renderer renderer in keypadLightRenderers)
            {
                renderer.material = redMaterial;                
            }
            
            StartCoroutine(RetryCooldown());
        }

        
        int_buttontracker = 0;
        int_1 = 0;
        int_2 = 0;
        int_3 = 0;
        int_4 = 0;
        
    }

    IEnumerator RetryCooldown()
    {
        b_active = false;

        yield return new WaitForSeconds(2f);

        foreach (Renderer renderer in overlayKeypadLightRenderers)
        {
            renderer.material = initialMaterial;
        }
        foreach (Renderer renderer in keypadLightRenderers)
        {
            renderer.material = initialMaterial;
        }

        b_active = true;
    }


    // Update is called once per frame
    void Update()
    {

    }
}
