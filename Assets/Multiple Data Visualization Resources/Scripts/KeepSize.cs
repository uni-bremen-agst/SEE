using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeepSize : MonoBehaviour
{
    public Transform targetPosition;

    // Update is called once per frame
    void Update()
    {
        transform.position = targetPosition.position;
    }
}
