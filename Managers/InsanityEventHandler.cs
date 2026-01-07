
using UnityEngine;

public class InsanityEventHandler : MonoBehaviour
{
    // This script will listen for the player to declare is insane to send signals to this script to randomly cause different insanity effects.

    [Header("Essential Variables")]
    private Camera mainCam; // Every player has their own instance of the main camera so we can find where the client player is based on this position.

    [Header("Evil Eye")]
    public GameObject EvilEye;
    public float rayDistance = 15f;
    public LayerMask hitMask;

    [Header("False Monster")]
    public GameObject fakeMonster;

    [Header("Whisper")]
    public AudioClip sfx_whisper; // whisper sfx

    [Header("Door Knock")]
    public LayerMask doorLayer;
    public AudioClip sfx_doorKnock; // Search for door tag around vicinity of player, door then plays this sound.

    void OnEnable()
    {
        //Subscribe to a static event within the playerbehavior that will trigger when a sanity threshold is hit.
    }

    // Start is called before the first frame update
    void Start()
    {
        mainCam = Camera.main;

        CreateEvilEye();

    }

    // Update is called once per frame
    void Update()
    {

    }

    [ContextMenu("Test EvilEyes")]
    public void CreateEvilEye()
    {
        int maxEyeCount = UnityEngine.Random.Range(2, 7);
        for (int i = 0; i < maxEyeCount; i++)
        {
            Vector3 randomDirection = UnityEngine.Random.onUnitSphere; // Random direction in 3D
            Vector3 startPosition = mainCam.transform.position;

            if (Physics.Raycast(startPosition, randomDirection, out RaycastHit hit, rayDistance, hitMask))
            {
                //Debug.Log($"Ray {i + 1} hit {hit.collider.name} at {hit.point}");
                Debug.DrawRay(startPosition, randomDirection * hit.distance, Color.red, 7f);


                Quaternion rotation = Quaternion.LookRotation(hit.normal);

                Vector3 eyeOffset = hit.normal * .01f;
                GameObject evilEyeInstance = Instantiate(EvilEye, hit.point + eyeOffset, rotation);
                evilEyeInstance.transform.localScale = transform.localScale * Random.Range(1f, 2f);
                evilEyeInstance.transform.SetParent(hit.transform);
            }
            else
            {
                Debug.Log($"Ray {i + 1} missed.");
                Debug.DrawRay(startPosition, randomDirection * rayDistance, Color.yellow, 7f);
                i--;
            }
        }
    }

    [ContextMenu("Test Create FalseMonster")]
    //Instantiates an entity that looks like a monster, could be a model or just a shadow. Will spawn either behind the player and despawn when in camera view. 
    public void CreateFakeMonster()
    {
        Vector3 startPosition = mainCam.transform.position;
        // Get the horizontal direction behind the camera
        Vector3 castDirection = -mainCam.transform.forward;
        castDirection.y = 0; // Remove vertical influence
        castDirection.Normalize(); // Make it a unit vector again

        if (Physics.Raycast(startPosition, castDirection, out RaycastHit hit, rayDistance, hitMask))
        {
            float maxSpawnDistance = Vector3.Distance(startPosition, hit.point);

            float randomRange = Random.Range(1, maxSpawnDistance);

            Vector3 spawnPosition = startPosition + castDirection * randomRange;

            Vector3 directionToPlayer = startPosition - spawnPosition;
            directionToPlayer.y = 0; // Ignore vertical difference
            directionToPlayer.Normalize(); // Make it a unit vector

            Quaternion yOnlyRotation = Quaternion.LookRotation(directionToPlayer);

            Instantiate(fakeMonster, spawnPosition, yOnlyRotation);
        }
        else
        {
            float randomRange = Random.Range(1, 15);

            Vector3 spawnPosition = startPosition + castDirection * randomRange;

            Vector3 directionToPlayer = startPosition - spawnPosition;
            directionToPlayer.y = 0; // Ignore vertical difference
            directionToPlayer.Normalize(); // Make it a unit vector

            Quaternion yOnlyRotation = Quaternion.LookRotation(directionToPlayer);

            Instantiate(fakeMonster, spawnPosition, yOnlyRotation);
        }
    }

    [ContextMenu("Test Whisper")]
    public void PlayWhisper()
    {
        AudioManager.instance.PlaySound(sfx_whisper, mainCam.transform.position - mainCam.transform.forward, false, 0.3f);
    }

    [ContextMenu("Test Knock")]
    public void FindDoor()
    {
        Collider[] colliders = Physics.OverlapSphere(mainCam.transform.position, 15f, doorLayer);

        Transform closestDoor = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Door"))
            {
                float distance = Vector3.Distance(mainCam.transform.position, col.transform.position);

                if (distance < closestDistance) // it will always set closest distance to the first door it detects, then all future doors will have to compare against the previous door deemed closest.
                {
                    closestDistance = distance; // Change the closest distance so we can compare it to the other doors found.

                    closestDoor = col.transform;                    
                }
            }
        }

        if (closestDoor != null)
        {
            Vector3 closestDoorPosition = closestDoor.position;
            Debug.Log("Closest Wooden Door at: " + closestDoorPosition);

            AudioManager.instance.PlaySound(sfx_doorKnock, closestDoorPosition, false, 0.5f);
        }
        else
        {
            Debug.Log("No Wooden Doors found in range.");
        }

    }
}
