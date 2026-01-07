using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;


public class FirstPersonCamera : NetworkBehaviour
{

[Header("Cinemachine")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    [SerializeField] private GameObject CinemachineCameraTarget;
    [Tooltip("How far in degrees can you move the camera up")]
    [SerializeField] private float TopClamp = 90.0f;
    [Tooltip("How far in degrees can you move the camera down")]
    [SerializeField] private float BottomClamp = -90.0f;
    [Tooltip("Rotation speed of the character")]
    public float float_rotationSpeed = 1.0f;
    private float _cinemachineTargetPitch;
    private float float_rotationVelocity;
    private InputSystem_Enabler inputSystem;
    private const float _threshold = 0.01f;
    public bool IsCurrentDeviceMouse;
    private string settingsFilePath;
    public GameSettings gameSettings;
    public CinemachineVirtualCamera playerCamera;

    public bool b_paused = false;

    public void Awake()
    {

        playerCamera = GameObject.FindGameObjectWithTag("VirtualCamera").GetComponent<CinemachineVirtualCamera>();
        
        //Get access to the gamesettings to set the sensitivity.
        settingsFilePath = Path.Combine(Application.persistentDataPath, "GameSettings.json");
        if (File.Exists(settingsFilePath))
        {
            string json = File.ReadAllText(settingsFilePath);
            gameSettings = JsonUtility.FromJson<GameSettings>(json);            
        }
        
        gameSettings.int_fieldOfView = Mathf.Clamp(gameSettings.int_fieldOfView, 60, 90);
        playerCamera.m_Lens.FieldOfView = gameSettings.int_fieldOfView;
        float_rotationSpeed = gameSettings.float_mouseSensitivity;

       
    }
    
    public void OnEnable()
    {   
        //float_rotationSpeed = gameSettings.float_mouseSensitivity;
    }

    public override void OnStartAuthority()
    {
		if(isLocalPlayer)
		{
		virtualCamera = GameObject.Find("PlayerCam").GetComponent<CinemachineVirtualCamera>();
		inputSystem = GetComponent<InputSystem_Enabler>();

        if(gameSettings != null)
        {
            float_rotationSpeed = gameSettings.float_mouseSensitivity;
        }
        else 
        {
            Debug.LogWarning("Game Settings not found setting to default values");
            float_rotationSpeed = 1f;
        }
        
        if (inputSystem == null)
        {
            Debug.LogError("InputSystem_Enabler component not found.");
        }
        // Disable the camera by default
        enabled = false;
		}		
		//Debug.Log("Starting Authority");
        // Enable the camera and control script only for the local player
        virtualCamera.Follow = CinemachineCameraTarget.transform;
        enabled = true;
       
    }


    public void ChangeSensitivity(float sensitivity)
    {
        float_rotationSpeed = sensitivity;               
    }

    public void ChangeFOV(float FOV)
    {
        Debug.Log("wuh");
        playerCamera.m_Lens.FieldOfView = FOV;  
    }

    private void LateUpdate()
    {
        
        if (isLocalPlayer && !b_paused)
        {
            CameraRotation();
        }
    }

    private void CameraRotation()
    {
        if (inputSystem == null)
        {
            Debug.LogWarning("Input components not initialized.");
            return;
        }

        // If there is an input
        if (inputSystem.look.sqrMagnitude >= _threshold)
        {
            // Don't multiply mouse input by Time.deltaTime
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetPitch += inputSystem.look.y * float_rotationSpeed * deltaTimeMultiplier;
            float_rotationVelocity = inputSystem.look.x * float_rotationSpeed * deltaTimeMultiplier;

            // Clamp our pitch rotation
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Update Cinemachine camera target pitch
            CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

            // Rotate the player left and right
            transform.Rotate(Vector3.up * float_rotationVelocity);
        }
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
}

