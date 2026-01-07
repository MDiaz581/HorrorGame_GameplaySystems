using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RenderPipeline = UnityEngine.Rendering.RenderPipelineManager;

public class PortalCamera : MonoBehaviour
{
    
    [SerializeField] private Camera mirrorCamera;
    public RenderTexture mirrorTexture;
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;

        // Create a render texture matching screen resolution
        mirrorTexture.width = Screen.width;
        mirrorTexture.height = Screen.height;

        // Set mirrorCamera to render to this texture
        mirrorCamera.targetTexture = mirrorTexture;
    }

    private void LateUpdate()
    {
        if (!mainCamera || !mirrorCamera) return;

        // Position and orient the mirror camera like a reflection of the main camera
        Vector3 mirrorNormal = transform.forward;
        Vector3 mirrorPos = transform.position;

        // Reflect camera position
        Vector3 cameraDir = mainCamera.transform.position - mirrorPos;
        Vector3 reflectedPos = Vector3.Reflect(cameraDir, mirrorNormal) + mirrorPos;

        mirrorCamera.transform.position = reflectedPos;

        // Reflect camera rotation
        Vector3 forward = Vector3.Reflect(mainCamera.transform.forward, mirrorNormal);
        Vector3 up = Vector3.Reflect(mainCamera.transform.up, mirrorNormal);
        mirrorCamera.transform.rotation = Quaternion.LookRotation(forward, up);

        // Optionally set oblique clipping plane
        Plane mirrorPlane = new Plane(-mirrorNormal, mirrorPos);
        Vector4 clipPlaneWorldSpace = new Vector4(mirrorPlane.normal.x, mirrorPlane.normal.y, mirrorPlane.normal.z, mirrorPlane.distance);
        Vector4 clipPlaneCameraSpace = Matrix4x4.Transpose(Matrix4x4.Inverse(mirrorCamera.worldToCameraMatrix)) * clipPlaneWorldSpace;
        mirrorCamera.projectionMatrix = mainCamera.CalculateObliqueMatrix(clipPlaneCameraSpace);

        // Render the mirrored view
        mirrorCamera.Render();
    }


/*
    [SerializeField]
    private GameObject[] portals = new GameObject[2];

    [SerializeField]
    private Camera portalCamera;

    [SerializeField]
    private int iterations = 2;

    public RenderTexture MirrorTexture;
    private RenderTexture tempTexture2;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;

        MirrorTexture.width = Screen.width;
        MirrorTexture.height = Screen.height;


    }

    private void OnEnable()
    {
        RenderPipeline.beginCameraRendering += UpdateCamera;
    }

    private void OnDisable()
    {
        RenderPipeline.beginCameraRendering -= UpdateCamera;
    }

    public void LateUpdate()
    {
        if (portals[0].GetComponent<Renderer>().isVisible)
        {
            portalCamera.targetTexture = MirrorTexture;
            for (int i = iterations - 1; i >= 0; --i)
            {
                //RenderCamera(portals[0], portals[1], i, SRC);
            }
        }
    }

    void UpdateCamera(ScriptableRenderContext SRC, Camera camera)
    {
        if (portals[0].GetComponent<Renderer>().isVisible)
        {
            portalCamera.targetTexture = MirrorTexture;
            for (int i = iterations - 1; i >= 0; --i)
            {
                RenderCamera(portals[0], portals[1], i, SRC);
            }
        }

    }

    private void RenderCamera(GameObject inPortal, GameObject outPortal, int iterationID, ScriptableRenderContext SRC)
    {
        Transform inTransform = inPortal.transform;
        Transform outTransform = outPortal.transform;

        Transform cameraTransform = portalCamera.transform;
        cameraTransform.position = transform.position;
        cameraTransform.rotation = transform.rotation;

        for (int i = 0; i <= iterationID; ++i)
        {
            // Position the camera behind the other portal.
            Vector3 relativePos = inTransform.InverseTransformPoint(cameraTransform.position);
            relativePos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativePos;
            cameraTransform.position = outTransform.TransformPoint(relativePos);

            // Rotate the camera to look through the other portal.
            Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * cameraTransform.rotation;
            relativeRot = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeRot;
            cameraTransform.rotation = outTransform.rotation * relativeRot;
        }

        // Set the camera's oblique view frustum.
        Plane p = new Plane(-outTransform.forward, outTransform.position);
        Vector4 clipPlaneWorldSpace = new Vector4(p.normal.x, p.normal.y, p.normal.z, p.distance);
        Vector4 clipPlaneCameraSpace =
            Matrix4x4.Transpose(Matrix4x4.Inverse(portalCamera.worldToCameraMatrix)) * clipPlaneWorldSpace;

        var newMatrix = mainCamera.CalculateObliqueMatrix(clipPlaneCameraSpace);
        portalCamera.projectionMatrix = newMatrix;

        // Render the camera to its render target.
#pragma warning disable 0618
        UniversalRenderPipeline.RenderSingleCamera(SRC, portalCamera);
#pragma warning restore 0618
    }
*/
}
