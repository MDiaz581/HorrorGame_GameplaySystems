using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FakeMonsterBehavior : MonoBehaviour
{
    public Camera mainCam;
    public Transform centerTarget;
    public LayerMask layerMask;
    // Start is called before the first frame update
    void Start()
    {   
        mainCam = Camera.main;
        //Align to the ground
        if(Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, 10f, layerMask))
        {
            transform.position = hit.point; 
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (mainCam == null || centerTarget == null) return;

        // Convert world position to viewport space
        Vector3 viewportPos = mainCam.WorldToViewportPoint(centerTarget.position);

        // Check if inside viewport (0 to 1 is visible)
        bool isVisible = viewportPos.z > 0 && 
                         viewportPos.x > 0 && viewportPos.x < 1 &&
                         viewportPos.y > 0 && viewportPos.y < 1;

        if (isVisible)
        {
            Debug.Log("Object is visible in the camera's viewport");

            // Calculate distance from center (0.5, 0.5 is center of screen)
            float distanceToCenter = Vector2.Distance(new Vector2(viewportPos.x, viewportPos.y), new Vector2(0.5f, 0.5f));

            Debug.Log($"Distance from center: {distanceToCenter}");

            if(distanceToCenter < 0.4f)
            {
                Destroy(this.gameObject);
            }
        }
    }
}
