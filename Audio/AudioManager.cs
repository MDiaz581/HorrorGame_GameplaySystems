using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance; //Global Reference. To call AudioManager on any script use AudioManager.instance.(Function calls) example: AudioManager.instance.PlaySound()
    public int poolSize = 10;
    public AudioSource audioSourcePrefab; // Prefab to instantiate.

    public float setVolume = 1; // Ability to control volume. Used for option settings.

    private Queue<AudioSource> audioSourcePool;

    private List<AudioSource> activeLoopingSources = new List<AudioSource>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            audioSourcePrefab.volume = setVolume;
            InitializePool();
        }
        else
        {
            Destroy(this);
        }
    }

    private void InitializePool()
    {
        audioSourcePool = new Queue<AudioSource>();
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource newAudioSource = Instantiate(audioSourcePrefab, transform);
            newAudioSource.gameObject.SetActive(false);
            audioSourcePool.Enqueue(newAudioSource);
        }
    }


    //If any sound is needed to be stopped assign this to a variable. AudioSource sourceSound; then assign this by doing sourceSound = PlaySound(...); Then apply StopLoopingSound(sourceSound);
    public AudioSource PlaySound(AudioClip clip, Vector3 position, bool isLooping, Transform parentObject, float volumeAdjustment = 1f)
    {
        // Updated catch to prevent players from sending or recieving null sound clips. Avoids the need for a catch in every script calling this function. which is a lot.
        if (clip == null)
        {
            // Get the caller's method name and script
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
            System.Reflection.MethodBase callerMethod = stackTrace.GetFrame(1).GetMethod();
            string callerClassName = callerMethod.DeclaringType != null ? callerMethod.DeclaringType.Name : "UnknownClass";
            string callerMethodName = callerMethod.Name;

            Debug.LogWarning($"PlaySound was called with a null AudioClip! Caller: {callerClassName}.{callerMethodName}");
            return null;
        }

        if (audioSourcePool.Count > 0)
        {
            AudioSource audioSource = audioSourcePool.Dequeue();
            audioSource.transform.position = position;

            if (parentObject != null)
            {
                audioSource.transform.SetParent(parentObject);
            }

            audioSource.volume = setVolume * volumeAdjustment;
            audioSource.clip = clip;
            audioSource.loop = isLooping;
            audioSource.gameObject.SetActive(true);
            audioSource.Play();

            if (isLooping)
            {
                activeLoopingSources.Add(audioSource); // Keep track of looping sounds
            }
            else
            {
                StartCoroutine(ReturnToPool(audioSource, clip.length));
            }
            return audioSource; // Return the AudioSource so it can be controlled
        }
        return null;
    }

    //Audio Source Overload, not requiring parent. Parenting an object is useful for the cases in which an object moves and must sound. e.g. Elevators, cars, etc.
    public AudioSource PlaySound(AudioClip clip, Vector3 position, bool isLooping, float volumeAdjustment = 1f)
    {
        return PlaySound(clip, position, isLooping, null, volumeAdjustment);
    }


    public void StopLoopingSound(AudioSource source)
    {
        if (activeLoopingSources.Contains(source))
        {
            source.Stop();
            source.transform.SetParent(transform); // Return the object to its rightful parent, the AudioManager.
            source.gameObject.SetActive(false);
            activeLoopingSources.Remove(source);
            audioSourcePool.Enqueue(source); // Return the AudioSource to the pool
        }
    }

    public void StopAllSounds()
    {
        // Stop and return all looping sounds
        foreach (AudioSource audioSource in activeLoopingSources)
        {
            audioSource.Stop();
            audioSource.gameObject.SetActive(false);
            audioSourcePool.Enqueue(audioSource);
        }

        // Clear the list of active looping sources
        activeLoopingSources.Clear();

        // Stop and return all currently active non-looping sounds
        foreach (AudioSource audioSource in audioSourcePool)
        {
            if (audioSource.gameObject.activeSelf) // Ensure that this is an active sound
            {
                audioSource.Stop();
                audioSource.gameObject.SetActive(false);
                audioSourcePool.Enqueue(audioSource); // Return it to the pool
            }
        }
    }

    //This allows the entire audio clip to play before disabling the audio source returning to the queue within the pool.
    private IEnumerator ReturnToPool(AudioSource audioSource, float delay)
    {
        yield return new WaitForSeconds(delay);
        audioSource.transform.SetParent(transform); // Return the object to its rightful parent, the AudioManager.
        audioSource.gameObject.SetActive(false);
        audioSourcePool.Enqueue(audioSource);
    }

}
