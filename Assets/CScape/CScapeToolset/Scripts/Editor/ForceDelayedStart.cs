using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CSToolset;

public class ForceDelayedStart : MonoBehaviour {
    public bool force = false;
    public CSDepthToNormal component;
    public int counter = 0;

	// Use this for initialization
	void Start () {
        component.enabled = false;
    }
	
	// Update is called once per frame
	void Update () {
        if (force)
            component.enabled = true;
        counter++;
        if (counter > 10)
        force = true;
	}
}
