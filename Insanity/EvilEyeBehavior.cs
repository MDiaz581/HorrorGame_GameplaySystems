using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvilEyeBehavior : MonoBehaviour
{
    private Animator animator;   
    public Transform eyeTransform; // Has to be separate from the actual eyeball itself which holds the animations.
    [Tooltip("Time in seconds the eye stays before being destroyed.")]
    public float f_lifeSpan = 60;
    private Camera mainCam;
    public Vector3 rotationOffset = Vector3.zero; // Adjust in the Inspector
    private Coroutine _LookAtCamera;
    public AudioClip sfx_eyeSpawn;
    private bool bool_EyeActive;


    // Start is called before the first frame update
    void Start()
    {
        mainCam = Camera.main;
        animator = GetComponent<Animator>();
        StartCoroutine(SpawnSound()); // prevents sound spam and sounds better
        StartCoroutine(LifeSpan());
    }

    void Update()
    {
        if (Vector3.Distance(transform.position, mainCam.transform.position) > 20f)
        {
            if (_LookAtCamera != null)
            {
                StopCoroutine(_LookAtCamera);
            }
            
            Destroy(gameObject);
        }
        //This is when active
        if (Vector3.Distance(transform.position, mainCam.transform.position) <= 6f && !bool_EyeActive)
        {

            bool_EyeActive = true;

            if (animator != null) animator.SetBool("bool_Active", bool_EyeActive);

            _LookAtCamera = StartCoroutine(LookAtCamera());
        }
        //This is when idle / searching
        else if (Vector3.Distance(transform.position, mainCam.transform.position) > 6f && bool_EyeActive)
        {

            bool_EyeActive = false;
            eyeTransform.localRotation = new Quaternion(0, 0, 0, 0); // Reset the rotation
            if (animator != null) animator.SetBool("bool_Active", bool_EyeActive);
            StopCoroutine(_LookAtCamera);
            //Reference animator to play the active
        }
    }

    private IEnumerator LookAtCamera()
    {
        while (bool_EyeActive)
        {
            eyeTransform.LookAt(Camera.main.transform);
            eyeTransform.Rotate(rotationOffset); // Apply the offset to correct the orientation
            yield return new WaitForSeconds(.25f);
        }
    }

    private IEnumerator SpawnSound()
    {
        yield return new WaitForSeconds(Random.Range(.5f, 1f));

        AudioManager.instance.PlaySound(sfx_eyeSpawn, transform.position, false);
    }

    private IEnumerator LifeSpan()
    {
        yield return new WaitForSeconds(f_lifeSpan);
        Destroy(gameObject);
    }
}
