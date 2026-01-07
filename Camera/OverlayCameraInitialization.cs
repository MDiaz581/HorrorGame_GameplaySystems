using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class OverlayCameraInitialization : MonoBehaviour
{
    void Awake()
    {
        UniversalAdditionalCameraData mainCamData = Camera.main.GetComponent<UniversalAdditionalCameraData>();
        if (mainCamData != null)
        {
            mainCamData.cameraStack.Add(gameObject.GetComponent<Camera>());
            Debug.Log("Overlay Camera added to Main Camera stack.");
        }
        else
        {
            Debug.LogError("Main Camera is not using URP!");
        }

    }
}

