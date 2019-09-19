using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CScape;

[RequireComponent(typeof(ReplaceRenderingCScape))]
[RequireComponent(typeof(FilterTest))]

public class Re_lightingManager : MonoBehaviour {
    int frame = 0;
	// Use this for initialization
	void Start () {
        frame = 0; 
        
	}
	
	// Update is called once per frame
	void Update () {
        if (frame > 0)
            gameObject.SetActive(false);
        frame++;
    }
}
