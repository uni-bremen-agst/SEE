using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CScape;
using System;

namespace CScape { 
public class StreetGenerator : MonoBehaviour {

        public int divisionDepth = 1;
        public int divisionWidth = 1;
        public int sidewalkSizeDepth = 1;
        public int sidewalkSizewidth = 1;
        public GameObject[] childrenBuildings;
        public GameObject instance;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	public void UpdateMe () {
            for (int i = 0; i < childrenBuildings.Length - 1; i++)
            {
                if (childrenBuildings[i])
                DestroyImmediate(childrenBuildings[i]);
            }

                Array.Resize(ref childrenBuildings, divisionDepth*2 + divisionWidth*2);
		for (int i= 0; i < divisionDepth - 1; i++)
            {
                GameObject inst = Instantiate(instance, gameObject.transform);
                inst.transform.localPosition = new Vector3(inst.transform.localPosition.x + i * 3f, 0, inst.transform.localPosition.z);


            }
	}
}
}
