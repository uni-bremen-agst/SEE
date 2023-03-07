using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This is a script applied to tmp demo text because of a bug where the letters scales are not saved because tmp is not imported
public class TMPSizeProtector : MonoBehaviour{
    public float size;

    void Start(){
#if UNITY_EDITOR
        if(GetComponent<TMPro.TextMeshPro>() != null)
            GetComponent<TMPro.TextMeshPro>().fontSize = size;
#endif
    }
}
