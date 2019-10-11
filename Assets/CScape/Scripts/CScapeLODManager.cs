#if UNITY_EDITOR
using System.Collections;

using System.Collections.Generic;
using UnityEngine;
using CScape;
using System.Linq;

namespace CScape {
    public class CScapeLODManager : MonoBehaviour
    {

        CityRandomizer CSRandomizerComponent;
        public GameObject csDetailsObject;
        public int polesDistance = 15;
        public int lightsDistance = 25;
        public int treeDistance = 15;
        public float rooftopsCullingSize = 0.7f;
        public int rooftopDensity;
        public float advertsCullingSize = 0.7f;
        public int advertsDensity;
        public bool useRooftops = true;

        // Use this for initialization
        public void UpdateLightpoleLods()
        {

            CSRandomizerComponent = gameObject.GetComponent<CityRandomizer>();
            csDetailsObject = CSRandomizerComponent.streetDetails;
            foreach (Transform go in csDetailsObject.transform.Cast<Transform>().Reverse())
            {
                CSInstantiator poles = go.GetComponent(typeof(CSInstantiator)) as CSInstantiator;
                poles.offsetX = polesDistance;
                poles.offsetZ = polesDistance;
                poles.UpdateElements();

            }

        }

        public void UpdateLightsLods()
        {

            CSRandomizerComponent = gameObject.GetComponent<CityRandomizer>();
            csDetailsObject = CSRandomizerComponent.streetLights;
            foreach (Transform go in csDetailsObject.transform.Cast<Transform>().Reverse())
            {
                CSInstantiatorLights poles = go.GetComponent(typeof(CSInstantiatorLights)) as CSInstantiatorLights;
                poles.offsetX = lightsDistance;
                poles.offsetZ = lightsDistance;
                poles.UpdateElements();

            }

        }
        public void UpdateTreesLods()
        {

            CSRandomizerComponent = gameObject.GetComponent<CityRandomizer>();
            csDetailsObject = CSRandomizerComponent.foliage;
            foreach (Transform go in csDetailsObject.transform.Cast<Transform>().Reverse())
            {
                CSFoliageInstantiator poles = go.GetComponent(typeof(CSFoliageInstantiator)) as CSFoliageInstantiator;
                poles.offsetX = treeDistance;
                poles.offsetZ = treeDistance;
                poles.UpdateElements();

            }



        }

        public void UpdateRooftopCulling()
        {

            CSRandomizerComponent = gameObject.GetComponent<CityRandomizer>();
            csDetailsObject = CSRandomizerComponent.buildings;
            foreach (Transform go in csDetailsObject.transform.Cast<Transform>().Reverse())
            {
                CSRooftops rooftops = go.GetComponent(typeof(CSRooftops)) as CSRooftops;
                if (rooftops != null)
                {
                    rooftops.instancesX = rooftopDensity;
                    rooftops.lodDistance = rooftopsCullingSize;
                    rooftops.animateLodFade = true;
                    rooftops.UpdateElements();
                }
            }

        }

        public void UpdateAdvertisingCulling()
        {

            CSRandomizerComponent = gameObject.GetComponent<CityRandomizer>();
            csDetailsObject = CSRandomizerComponent.buildings;
            foreach (Transform go in csDetailsObject.transform.Cast<Transform>().Reverse())
            {
                CSAdvertising rooftops = go.GetComponent(typeof(CSAdvertising)) as CSAdvertising;
                if (rooftops != null)
                {
                    rooftops.instancesX = advertsDensity;
                    rooftops.lodDistance = advertsCullingSize;
                    rooftops.useAdvertising = useRooftops;
                    rooftops.animateLodFade = true;
                    rooftops.DeleteSolution();
                    rooftops.UpdateElements();
                }



            }

            foreach (Transform go in csDetailsObject.transform.Cast<Transform>().Reverse())
            {
                BuildingModifier bm = go.GetComponent(typeof(BuildingModifier)) as BuildingModifier;
                if (bm != null)
                {
                    bm.useAdvertising = useRooftops;
                 //   bm.AwakeCity();
                    bm.UpdateCity();

                }
            }

        }

    }
}
#endif