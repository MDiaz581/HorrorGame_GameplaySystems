using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Legacy_DynamicRenderChange : MonoBehaviour
{
    public Material newOverrideMaterial; // The new material to assign
    public UniversalRendererData urpData;

    //Legacy code, this works to adjust the material within the Render objects feature, but doesn't get performed ingame. 
    //Could be good for certain sitations where I want the player to reset their game in order for settings to take effect. 
    //Otherwise urpData.rendererFeatures[index].SetActive(true/false); is good enough for most situations and does take into effect in realtime.
    private void Start()
    {
        //urpData.rendererFeatures[0].SetActive(false);

        // Find the Render Objects feature in the Renderer Features list
        for (int i = 0; i < urpData.rendererFeatures.Count; i++)
        {
            // Iterate through the Renderer Features
            foreach (var feature in urpData.rendererFeatures)
            {
                // Check if the feature's type name matches "RenderObjects"
                if (feature != null && feature.GetType().Name == "RenderObjects")
                {
                    Debug.Log("Found Render Objects Feature!");

                    // Access the "settings" property via Reflection
                    var settingsProperty = feature.GetType().GetField("settings");

                    if (settingsProperty != null)
                    {
                        // Get the settings object
                        var settings = settingsProperty.GetValue(feature);

                        // Access the "overrideMaterial" field within the settings
                        var overrideMaterialField = settings.GetType().GetField("overrideMaterial");

                        if (overrideMaterialField != null)
                        {
                            // Set the new material
                            overrideMaterialField.SetValue(settings, newOverrideMaterial);
                            Debug.Log($"Updated Render Objects Feature's material to {newOverrideMaterial.name}");
                        }
                        else
                        {
                            Debug.LogError("Could not find the 'overrideMaterial' field in Render Objects settings!");
                        }
                    }
                    else
                    {
                        Debug.LogError("Could not access the 'settings' property of the Render Objects feature!");
                    }

                    break;
                }
            }

        }

    }
}


