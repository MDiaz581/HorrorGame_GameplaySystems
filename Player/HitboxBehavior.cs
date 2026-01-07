using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HitboxBehavior : MonoBehaviour
{
    [Tooltip("Reference to the base object, so we can ensure that the box does not hit itself.")]
    public GameObject baseObject;
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"Hitbox triggered by: {other.name}");
        if (other.GameObject() != baseObject)
        {
            // Example: Check for a specific tag
            if (other.CompareTag("Monster"))
            {
                Debug.Log("Hit a Player!");

                //Send Info to Game manager to parse the info across all clients. 
                // Add your logic here (e.g., deal damage)
                other.gameObject.GetComponent<PlayerBehavior>()?.TakeDamage();

                this.GameObject().SetActive(false);
            }
        }
    }

}
