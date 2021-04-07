
using UnityEngine;

namespace SEE.Utils
{
    public class Shrink : MonoBehaviour
    {


        /// <summary>
        /// Calculates a ration between the current localScale and a targetscale of a given vector.
        /// </summary>
        /// <param name="transform">The transform attached to an object which shall be shrunk</param>
        /// <param name="targetscale"> the targetscale, the object is goint to be ShrinkAnObject to</param>
        /// <returns> a vector with the ratios of current - and targetscale </returns>
        public static Vector3 shrinkFactor(Transform transform, Vector3 targetScale)
        {        
            float xLocalScale = targetScale.x / transform.localScale.x;
            float yLocalScale = targetScale.y / transform.localScale.y;
            float zLocalScale = targetScale.z / transform.localScale.z;   
            Vector3 Shrinkfactor = new Vector3(xLocalScale, yLocalScale, zLocalScale);
            return Shrinkfactor;
        }
        /// <summary>
        /// Shrinks a given object to a certain targetscale. 
        /// The targetscale is a result of an amount of iterations and the specific <param name="shrinkFactor">.
        /// </summary>
        /// <param name="objectToShrink">The object which shall be shrunk</param>
        /// <param name="iterations">the amount of iterations that are proceeded while shrinking</param>
        ///  <param name="shrinkFactor"> the specific factor, the localscale of the given object is multiplied in order to ShrinkAnObject</param>
        /// <returns> a vector with the ratios of current - and targetscale </returns>
        public static void ShrinkAnObject(GameObject objectToShrink, float iterations, Vector3  shrinkFactor)
        {    
            while (iterations > 0 ) { 
                objectToShrink.transform.localScale = new Vector3(objectToShrink.transform.localScale.x * shrinkFactor.x,objectToShrink.transform.localScale.y * shrinkFactor.y, objectToShrink.transform.localScale.z * shrinkFactor.z);
                iterations--;
            }
        }

        /// <summary>
        /// Expands a given object to a certain targetscale. 
        /// The targetscale is a result of iterations and the specific <param name="shrinkFactor">.
        /// </summary>
        /// <param name="objectToExpand">The object which shall be expandedk</param>
        ///  <param name="shrinkFactor"> the specific factor, the localscale of the given object is multiplied in order to ShrinkAnObject</param>
        /// <returns> a vector with the ratios of current - and targetscale </returns>
        public static void Expand(GameObject objectToExpand, Vector3 shrinkFactor)
        {
                objectToExpand.transform.localScale = new Vector3(objectToExpand.transform.localScale.x / shrinkFactor.x, objectToExpand.transform.localScale.y / shrinkFactor.y, objectToExpand.transform.localScale.z / shrinkFactor.z);    
        }
    }
}