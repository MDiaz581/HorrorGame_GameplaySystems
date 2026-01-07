using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RenderFeatureActivator : MonoBehaviour
{

    //I realize now that this could have simply been done in the camera's built in culling system. I could have just disabled the layermask in the camera for a similar effect. EDIT: 7/13/25 this is now the case.
    //If searching for a reason why objects disappear after creating a new layer check the camera's culling feature. 
    //This script has to be called by another. Called in Monster Behavior. 
    public static RenderFeatureActivator renderFeatureActivator;
    public UniversalRendererData universalRendererData;
    
    //[SerializeField]
    //private LayerMask setLayerMask;
    //private LayerMask originalOpaqueLayerMask;
    //private LayerMask originalTransparentLayerMask;

    void Awake()
    {
        if (renderFeatureActivator == null)
        {
            renderFeatureActivator = this;
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates
        }

        /*
        // Ensure that we set all the layers back to active.
        if (universalRendererData != null)
        {
            //InitializeOriginalLayerMasks();
            //SaveOriginalRenderer();
        }
        else
        {
            Debug.LogError("Universal Renderer Data not found! Fatal Error: Doors may or may not disappear and never appear again!");
        }
        */
    }

    public void SetRenderFeatureActive(int FeatureIndex, bool isActive)
    {
        if (universalRendererData != null)
        {
            universalRendererData.rendererFeatures[FeatureIndex].SetActive(isActive);
        }
        else
        {
            Debug.LogError("No UniversalRendererData found! Cannot activate or disable features");
        }

    }
    

    /*
    void SaveOriginalRenderer()
    {
        if (universalRendererData != null)
        {
            originalOpaqueLayerMask = universalRendererData.opaqueLayerMask; // Store original
            originalTransparentLayerMask = universalRendererData.transparentLayerMask;
            //Debug.Log("Stored original URP Layer Mask: " + originalOpaqueLayerMask);
        }
        else
        {
            Debug.LogError("Could not find UniversalRendererData in URP Asset.");
        }
    }

    /* Defunct now using camera culling to create the same effect. prevents any need to change the layer mask which gets saved
    public void DisableLayer(string layerName)
    {
        if (universalRendererData == null) return;

        int layerToRemove = 1 << LayerMask.NameToLayer(layerName); // Convert layer name to bitmask

        universalRendererData.opaqueLayerMask &= ~layerToRemove; // Disable the layer in Opaque Mask
        universalRendererData.transparentLayerMask &= ~layerToRemove; // Disable the layer in Opaque Mask

    }

    public void EnableLayer(string layerName)
    {
        if (universalRendererData == null) return;

        int layerToRemove = 1 << LayerMask.NameToLayer(layerName); // Convert layer name to bitmask

        universalRendererData.opaqueLayerMask |= layerToRemove; // Disable the layer in Opaque Mask
        universalRendererData.transparentLayerMask |= layerToRemove; // Disable the layer in Opaque Mask
    }
    

    public void InitializeOriginalLayerMasks()
    {
        if (universalRendererData != null)
        {
            universalRendererData.opaqueLayerMask = setLayerMask; // Restore the original mask
            universalRendererData.transparentLayerMask = setLayerMask;
            //Debug.Log("Restored URP Layer Mask on Start");
        }

    }

    void OnApplicationQuit()
    {

        if (universalRendererData != null)
        {
            universalRendererData.opaqueLayerMask = originalOpaqueLayerMask; // Restore the original mask
            universalRendererData.transparentLayerMask = originalTransparentLayerMask;
            Debug.Log("Restored URP Layer Mask on Exit");
        }
    }
    */
}
