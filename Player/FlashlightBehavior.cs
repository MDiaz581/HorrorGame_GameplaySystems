using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashlightBehavior : MonoBehaviour
{

    [Header("Flashlight")]
    public Light flashlight;
    public float minDistance = 0f; // Minimum distance at which the flashlight intensity is 20
    public float maxDistance = 5f; // Maximum distance at which the flashlight intensity is 1
    public float maxIntensity;
    public float minIntensity;

    [Header("Low light vision")]
    public Light lowLight;
    public float maxLowLight = 3.5f;

    public float adjustmentTime;

    // Start is called before the first frame update
    void Start()
    {
        //flashlight = GetComponent<Light>();
        lowLight.intensity = 0f;
    }

    // Update is called once per frame
    void LateUpdate()
    {        
        ChangeIntensity(); 
    }

    void Update() 
    {
        DarkvisionAdjustment();
    }

    private void ChangeIntensity()
    {
        if(flashlight != null && flashlight.enabled)
        {
            RaycastHit hit;
 
            if (Physics.Raycast(transform.position, transform.forward, out hit))
            {
            float rayDistance = hit.distance;

            // Clamp the distance to be within the specified range
            rayDistance = Mathf.Clamp(rayDistance, minDistance, maxDistance);

            // Calculate the normalized distance (0 to 1)
            float normalizedDistance = (rayDistance - minDistance) / (maxDistance - minDistance);

            float quadraticDistance = Mathf.Pow(normalizedDistance, 2f);

            // Interpolate intensity from 20 to 1
            flashlight.intensity = Mathf.Lerp(minIntensity, maxIntensity, quadraticDistance);

            }
            else
            {
            // Optional: Set a default intensity when nothing is hit
            flashlight.intensity = maxIntensity;
            }
        }

    }

    private void DarkvisionAdjustment()
    {
        if(lowLight != null)
        {
            if(lowLight.enabled)
            {
                if(lowLight.intensity < maxLowLight)
                {
                    lowLight.intensity = Mathf.Lerp(lowLight.intensity, maxLowLight, adjustmentTime * Time.deltaTime);
                }                
            }
            else 
            {
                lowLight.intensity = 0f;
            }
        }
    }

}
