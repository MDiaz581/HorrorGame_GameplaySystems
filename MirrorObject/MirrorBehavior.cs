using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MirrorBehavior : MonoBehaviour
{
    public Camera mainCamera; // Reference to the main camera
    public Camera mirrorCamera;
    public RenderTexture renderTexture;

    public int textureWidth = 1024;
    public int textureHeight = 1024;
    public float clipPlaneOffset = 0.07f;

    private Transform mirrorTransform;

    void Start()
    {
        
        mirrorTransform = transform;

        if (!mainCamera)
            mainCamera = Camera.main;

        //renderTexture.width = textureWidth;
        //renderTexture.height = textureHeight;
        //renderTexture.depth = 24;

    }

    void LateUpdate()
    {
        if (!mirrorCamera || !mainCamera)
            return;

        // Reflect main camera position
        Vector3 mirrorNormal = mirrorTransform.forward;
        Vector3 camPos = mainCamera.transform.position;
        Vector3 reflectedPos = ReflectPosition(camPos, mirrorTransform.position, mirrorNormal);

        // Reflect main camera direction
        Vector3 camDir = mainCamera.transform.forward;
        Vector3 reflectedDir = ReflectDirection(camDir, mirrorNormal);

        mirrorCamera.transform.position = reflectedPos;
        mirrorCamera.transform.rotation = Quaternion.LookRotation(reflectedDir, Vector3.up);

        // Match settings
        mirrorCamera.fieldOfView = mainCamera.fieldOfView;
        mirrorCamera.aspect = mainCamera.aspect;
        mirrorCamera.nearClipPlane = mainCamera.nearClipPlane;
        mirrorCamera.farClipPlane = mainCamera.farClipPlane;

        // Oblique clip plane
        Vector4 clipPlane = CameraSpacePlane(mirrorCamera, mirrorTransform.position, mirrorNormal, 1.0f);
        mirrorCamera.projectionMatrix = mainCamera.CalculateObliqueMatrix(clipPlane);

        mirrorCamera.Render();
    }

    private Vector3 ReflectPosition(Vector3 position, Vector3 planeOrigin, Vector3 planeNormal)
    {
        Vector3 toPlane = position - planeOrigin;
        float distance = Vector3.Dot(toPlane, planeNormal);
        return position - 2f * distance * planeNormal;
    }

    private Vector3 ReflectDirection(Vector3 direction, Vector3 normal)
    {
        return direction - 2f * Vector3.Dot(direction, normal) * normal;
    }

    private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * clipPlaneOffset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cPos = m.MultiplyPoint(offsetPos);
        Vector3 cNormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cNormal.x, cNormal.y, cNormal.z, -Vector3.Dot(cPos, cNormal));
    }
}
