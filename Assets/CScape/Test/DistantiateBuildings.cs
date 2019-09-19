using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CScape;


[ExecuteInEditMode]
public class DistantiateBuildings : MonoBehaviour {
    public bool runDistantiator = false;
    public int minDistance;
    public int maxDistance;
    public int randomSeed;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (runDistantiator)
        {
            Random.seed = 0;
            BuildingModifier[] bm = GetComponentsInChildren<BuildingModifier>();
            for (int i = 0; i < bm.Length; i++)
            {
                Random.seed = i;
                bm[i].buildingWidth = bm[i].buildingWidth - Random.Range(minDistance, maxDistance);
                bm[i].UpdateCity();
            }
        }
        runDistantiator = false;
	}
}
