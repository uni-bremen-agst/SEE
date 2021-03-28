using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Utils
{
    public class Shrink : MonoBehaviour
    {
        public static Vector3 shrinkFactor(Transform transform, Vector3 targetScale)
        {
            UnityEngine.Debug.Log(transform.localScale + " pre calc");

            float xLocalScale = targetScale.x / transform.localScale.x;
            float yLocalScale = targetScale.y / transform.localScale.y;
            float zLocalScale = targetScale.z / transform.localScale.z;   
            Vector3 Shrinkfactor = new Vector3(xLocalScale, yLocalScale, zLocalScale);

            return Shrinkfactor;
        }

        public static void shrink(GameObject objectToShrink, float iterations, Vector3  shrinkFactor)
        {
            
          
            while (iterations > 0 ) { 
                objectToShrink.transform.localScale = new Vector3(objectToShrink.transform.localScale.x * shrinkFactor.x,objectToShrink.transform.localScale.y * shrinkFactor.y, objectToShrink.transform.localScale.z * shrinkFactor.z);
                iterations--;
            }
        }

        public static void expand(GameObject objectToExpand, Vector3 shrinkFactor)
        {
            
                objectToExpand.transform.localScale = new Vector3(objectToExpand.transform.localScale.x / shrinkFactor.x, objectToExpand.transform.localScale.y / shrinkFactor.y, objectToExpand.transform.localScale.z / shrinkFactor.z);
               
        }
    }
}