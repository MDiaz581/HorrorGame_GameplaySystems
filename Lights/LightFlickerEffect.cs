using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightFlickerEffect : MonoBehaviour
{
    public List<Light> lights;  // Assign your lights in the inspector or dynamically
    public float minIntensity = 0.5f;
    public float maxIntensity = 1.5f;
    public float flickerSpeed = 0.1f;

    void Update()
    {
        foreach (Light light in lights)
        {
            if (light != null)
            {
                float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0.0f);
                light.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);

                foreach (Transform child in light.transform)
                {
                    Light childLight = child.GetComponent<Light>();
                    if (childLight != null && childLight.type == LightType.Spot)
                    {
                        childLight.intensity = light.intensity;
                    }
                }
            }
        }
    }
}
