using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorSync : MonoBehaviour
{
    public GameObject syncObject;
    public bool b_invert;

    // Update is called once per frame
    void Update()
    {
        if (Quaternion.Inverse(syncObject.transform.rotation) != Quaternion.Inverse(transform.rotation))
        {
            transform.rotation = Quaternion.Inverse(syncObject.transform.rotation);  
        }
              
    }
}
