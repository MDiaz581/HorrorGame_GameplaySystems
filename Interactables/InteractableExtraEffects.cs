using System.Collections;
using System.Collections.Generic;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InteractableExtraEffects : NetworkBehaviour
{

    [Header("Objects to listen for")]
    [Tooltip("If we want multiple objects to affect this object we can add more sources here.")]
    public List<InteractableBehavior> interactableBehaviors = new List<InteractableBehavior>();

    [Header("Effects")]
    public bool canChangeMaterial;
    public bool canActivateLight;

    [Header("Lights")]
    public Light[] lights;

    [Header("Materials")]
    [Tooltip("Add objects for materials to be changed here.")]
    public List<GameObject> targetObject;

    public int int_materialId;

    [SyncVar(hook = nameof(OnToggle))]
    public bool b_state;
    public Material[] inputMaterials;
    public Material[] initialMaterials;
    public Material[] newMaterials;


    void Awake()
    {
        // Ensure that we add the interactable behavior this object should have.
        interactableBehaviors.Add(GetComponent<InteractableBehavior>());
    }


    void OnEnable()
    {
        foreach (InteractableBehavior interactableBehavior in interactableBehaviors)
        {
            if (interactableBehavior != null) // Ensure it's not null before subscribing
                interactableBehavior.extraAction += OnExtraAction;
        }
    }


    void OnDisable()
    {
        foreach (InteractableBehavior interactableBehavior in interactableBehaviors)
        {
            if (interactableBehavior != null) // Ensure it's not null before unsubscribing
                interactableBehavior.extraAction -= OnExtraAction;
        }
    }



    //NOTE THIS SCRIPT TRIGGERS TWICE WHEN PLAYING ONLINE FIGURE IT OUT WHEN LESS SLEEPY>>>>ZZZ
    //Update the issue was occuring because the command was being run in a clientRPC function.
    //This means all clients ran the command to invoke extra effects, CmdInvokeExtraEffects was added to
    //The command calls 

    // Start is called before the first frame update
    void Start()
    {
        // Failsafe: if there's nothing in the list add the base object as a potential candidate to affect.
        if (targetObject.Count <= 0)
        {
            targetObject.Add(transform.gameObject);
        }
    }

    public void ChangeMaterial()
    {
        foreach (var target in targetObject)
        {
            Renderer renderer = target.GetComponent<Renderer>();

            // Skip objects that don't have a renderer
            if (renderer == null)
            {
                Debug.LogWarning("Object has no Renderer component.");
                continue; // Skip to the next target in the list
            }

            // Get a copy of the materials array
            Material[] materials = renderer.materials;

            // Ensure that the material index is valid
            if (int_materialId < 0 || int_materialId >= materials.Length)
            {
                Debug.LogWarning("Material index out of bounds for object: " + target.name);
                continue;
            }

            // Swap materials based on state
            if (!b_state)
            {
                materials[int_materialId] = inputMaterials[0];
            }
            else
            {
                materials[int_materialId] = initialMaterials[0];
            }

            // Assign the modified materials array back to the renderer
            renderer.materials = materials;
        }

        // Toggle the state
        b_state = !b_state;

        Debug.Log("Materials changed.");
    }

    public void EnableLights()
    {
        foreach (Light light in lights)
        {
            light.enabled = true;
        }
    }
    public void DisableLights()
    {
        foreach (Light light in lights)
        {
            light.enabled = false;
        }
    }

    private void ToggleLight()
    {
        if (lights.Length >= 0)
        {

            foreach (Light light in lights)
            {

                light.enabled = !light.enabled;
            }
        }
        else
        {
            Debug.LogWarning("No lights found! Please check inspector and add lights!");
        }
    }

    private void OnExtraAction(InteractableExtraEffects target)
    {
        if (target == this)
        {
            if (canActivateLight)
            {
                ToggleLight();
            }
            if (canChangeMaterial)
            {
                ChangeMaterial();
            }
        }
    }

    private void OnToggle(bool oldState, bool newState)
    {

    }

}
