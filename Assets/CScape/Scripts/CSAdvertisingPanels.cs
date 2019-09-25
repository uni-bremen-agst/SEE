using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CScape;

namespace CScape
{
    public class CSAdvertisingPanels : MonoBehaviour
    {
#if UNITY_EDITOR
        public GameObject advPanelFrontPrefab;
        // Use this for initialization
        void Start()
        {

        }
        public void UpdateAdv()
        {
            BuildingModifier bm = gameObject.GetComponent<BuildingModifier>() as BuildingModifier;
            advPanelFrontPrefab.transform.position = gameObject.transform.position + new Vector3(0, 6, bm.buildingDepth * 3);
            // advPanelFrontPrefab.transform.localScale = new Vector3 (bm.buildingWidth * 3, 2f, bm.buildingDepth * 3);
        }
#endif
    }
}
