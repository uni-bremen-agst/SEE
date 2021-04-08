using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Contains methods to shrink or expand an object´s localScale.
    /// </summary>
    public class Shrink : MonoBehaviour
    {
        /// <summary>
        /// Calculates the ration between the current localScale and a target scale of a given vector.
        /// </summary>
        /// <param name="transform">The transform attached to an object which shall be shrunk</param>
        /// <param name="targetscale"> the targetscale, the object is going to be shrunk to</param>
        /// <returns> The result of the component-wise division between the transform's local scale and the given targetScale. </returns>
        public static Vector3 shrinkFactor(Transform transform, Vector3 targetScale)
        {        
            float xLocalScale = targetScale.x / transform.localScale.x;
            float yLocalScale = targetScale.y / transform.localScale.y;
            float zLocalScale = targetScale.z / transform.localScale.z;
            return new Vector3(xLocalScale, yLocalScale, zLocalScale);
        }

        /// <summary>
        /// Shrinks a given object to a certain targetscale. 
        /// The target scale is a result of an amount of iterations and the specific <paramref name="shrinkFactor">.
        /// </summary>
        /// <param name="objectToShrink">The object which shall be shrunk</param>
        /// <param name="iterations">How often the objectToShrink´s scale shall be multiplied with the shrinkFactor</param>
        ///  <paramref name="shrinkFactor"> the factor the localScale of the given objectToShrink is multiplied with</param>
        public static void ShrinkAnObject(GameObject objectToShrink, float iterations, Vector3 shrinkFactor)
        {    
            while (iterations > 0 ) { 
                objectToShrink.transform.localScale = new Vector3(objectToShrink.transform.localScale.x * shrinkFactor.x,objectToShrink.transform.localScale.y * shrinkFactor.y, objectToShrink.transform.localScale.z * shrinkFactor.z);
                iterations--;
            }
        }

        /// <summary>
        /// Expands a given object to a given targetscale. 
        /// The targetscale is a result of iterations and the specific <paramref name="shrinkFactor">.
        /// </summary>
        /// <param name="objectToExpand">The object which shall be expanded</param>
        ///  <param name="shrinkFactor"> the specific factor, the localscale of the given object is multiplied with</param>
        public static void Expand(GameObject objectToExpand, Vector3 shrinkFactor)
        {
                objectToExpand.transform.localScale = new Vector3(objectToExpand.transform.localScale.x / shrinkFactor.x, objectToExpand.transform.localScale.y / shrinkFactor.y, objectToExpand.transform.localScale.z / shrinkFactor.z);    
        }
    }
}