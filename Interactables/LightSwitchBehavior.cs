using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class LightSwitchBehavior : InteractableBehavior
{

    public bool b_fuseOn = true;

    [Header("Lights and State")]
    public GameObject[] AttachedLights;

    [SyncVar(hook = "ToggleLight")]
    [SerializeField]
    private bool b_switchToggle = true;
    [SerializeField]
    private bool b_dualSwitch = false;
    private Animator animator;
    [Header("Sound")]
    public AudioClip sfx_switch;
    [Header("Material Information")]
    [Tooltip("The objects that have the renderer we're targeting. The light furniture models in this case.")]
    public List<GameObject> targetObject;
    [Tooltip("If object has multiple materials within the renderer, this specifies the exact material to adjust.")]
    public int int_materialId;
    [Tooltip("Material the object uses when the light is off.")]
    public Material[] offMaterials;
    [Tooltip("Material the object uses when the light is on.")]
    public Material[] onMaterials;

    void OnEnable()
    {
        FuseBoxBehavior.globalPowerToggle += PowerDown;
    }

    void OnDisable()
    {
        FuseBoxBehavior.globalPowerToggle -= PowerDown;
    }


    private void Awake()
    {
        animator = GetComponent<Animator>();

    }


    void Start()
    {
        // Initialize the SFX
        audioClip = sfx_switch;

        //Set the animation to the correct state.
        if (animator != null)
        {
            animator.SetBool("b_switchOn", b_switchToggle);
        }

        // Initialize the light state based on the current toggle value
        if (AttachedLights.Length > 0)
        {
            foreach (var light in AttachedLights)
            {
                if (light != null) // Check if the light object is not null
                {
                    // if it's connected fuse is on turn on or off.
                    if (b_fuseOn)
                    {
                        light.SetActive(b_switchToggle);
                        ChangeMaterial(b_switchToggle);
                    }
                    else
                    {
                        light.SetActive(false);
                        ChangeMaterial(false);
                    }
                }
            }
        }

    }

    [Command(requiresAuthority = false)] // Allow all clients to call this command
    public override void CmdOnInteract(bool interactionState)
    {
        base.CmdOnInteract(interactionState);
        // This runs on the server, so we toggle the value here
        if (b_interacting)
        {
            CmdPlaySound(transform.position, false, 0.5f);
            //CmdInvokeExtraEffects();
            b_switchToggle = !b_switchToggle; // This will trigger the SyncVar hook
        }
    }

    // SyncVar hook that will be triggered whenever b_switchToggle changes
    public void ToggleLight(bool oldState, bool newState)
    {
        if (animator != null)
        {
            animator.SetBool("b_switchOn", b_switchToggle);
        }

        if (AttachedLights.Length > 0 && b_fuseOn)
        {
            

            foreach (var light in AttachedLights)
            {
                if (light != null) // Check if the light object is not null
                {
                    if (!b_dualSwitch)
                    {
                        light.SetActive(newState);
                        ChangeMaterial(b_switchToggle);
                    }
                    else
                    {
                        light.SetActive(!light.activeSelf);
                        ChangeMaterial(light.activeSelf);
                    }
                    
                }
            }
        }
    }

    // This is how the fuses this light is connected to controls the light.
    // This does not change the b_switchToggle, so whatever state this light was in it'll set it back to.
    public void FuseToggle(bool toggle)
    {
        b_fuseOn = toggle;
        if (AttachedLights.Length > 0)
        {
            foreach (var light in AttachedLights)
            {
                if (light != null) // Check if the light object is not null
                {
                    if (b_fuseOn)
                    {
                        light.SetActive(b_switchToggle);
                        ChangeMaterial(b_switchToggle);
                    }
                    else
                    {
                        light.SetActive(false);
                        ChangeMaterial(false);
                    }
                }
            }
        }
    }

    private void PowerDown()
    {
        Debug.Log("Heard Loud and Clear");
        StartCoroutine(FlickerLights());
    }

    private IEnumerator FlickerLights()
    {
        float[] flickerTimings = { 0.2f, 0.2f, 0.2f, 1f }; // Flicker pattern timing

        for (int i = 0; i < flickerTimings.Length; i++)
        {
            bool isOn = i % 2 == 1; // Alternates true/false
            FuseToggle(isOn);
            yield return new WaitForSeconds(flickerTimings[i]);
        }

        // Ensure the light is off at the end
        FuseToggle(false);
    }

    //We removed the dependency of InteractableExtraEffects to have better control. Changing the on/off materials of the light should be controlled by light anyways.
    public void ChangeMaterial(bool materialON)
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
            if (materialON)
            {
                materials[int_materialId] = onMaterials[0];
            }
            else
            {
                materials[int_materialId] = offMaterials[0];
            }

            // Assign the modified materials array back to the renderer
            renderer.materials = materials;
        }
        //Debug.Log("Materials changed.");
    }

    //Simple Debug to show which lights this is connected to.
    public void OnDrawGizmosSelected()
    {
        if (AttachedLights.Length > 0)
        {
            // Loop through each light and check if it's null before using it
            foreach (var light in AttachedLights)
            {
                if (light != null) // Check if the light object is not null
                {
                    Gizmos.DrawLine(transform.position, light.transform.position);
                }
            }
        }
    }
}
