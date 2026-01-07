using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventProxy : MonoBehaviour
{
    [HideInInspector]
    public MonsterBehavior monsterBehavior; // Reference to the actual script

    private void Awake()
    {
        // Automatically find the script if not manually assigned
        if (monsterBehavior == null)
        {
            monsterBehavior = GetComponentInParent<MonsterBehavior>();
        }
    }

    public void ActivateHitbox(int activateInt)
    {
        if (monsterBehavior != null)
        {
            monsterBehavior.ActivateHitbox(activateInt); // Call the actual function
        }
        else
        {
            Debug.LogError("MonsterBehavior script not found!");
        }
    }

    public void BeginCooldown()
    {
        if (monsterBehavior != null)
        {
            monsterBehavior.BeginAttackCooldown(); // Call the actual function
        }
        else
        {
            Debug.LogError("MonsterBehavior script not found!");
        }
    }
}
