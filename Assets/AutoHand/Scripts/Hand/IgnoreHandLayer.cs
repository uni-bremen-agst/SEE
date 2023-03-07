using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>For the special use case of attaching something under the hand component and 
/// wanting to ignore the hand layer override operation</summary>
[DefaultExecutionOrder(-100000)]
public class IgnoreHandLayer : MonoBehaviour
{
    public bool includeChildren = true;
    int startLayer;

    void Awake(){
        startLayer = gameObject.layer;
        Invoke("LateStart", 0.1f);
    }

    void LateStart(){
        if(includeChildren)
            SetLayerRecursive(transform, startLayer);
        else
            transform.gameObject.layer = startLayer;
    }
    
    internal void SetLayerRecursive(Transform obj, int newLayer) {
        obj.gameObject.layer = newLayer;
        for (int i = 0; i < obj.childCount; i++)
            SetLayerRecursive(obj.GetChild(i), newLayer);
    }
}
