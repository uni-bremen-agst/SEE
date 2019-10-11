using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CScape;

public class PrefabRuntimeSupport : MonoBehaviour {
    CityRandomizer crandom;
    public bool regenerateOnLoad;
	// Use this for initialization
	void Start () {
        if (regenerateOnLoad)
        {
            crandom = gameObject.GetComponent<CityRandomizer>();
            crandom.Refresh();
            crandom.StripScripts();
        }

    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
