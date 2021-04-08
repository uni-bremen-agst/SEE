using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Contains simple mathematical vector operations .
    /// </summary>
    public class VectorOperations : MonoBehaviour
    {
        /// <summary>
        /// Calculates the ration between two given vectors.
        /// </summary>
        /// <param name="denominator">The vector by which the counter¥s coordinated are divided by</param>
        /// <param name="counter"> The counter vector - which is divided by the denominatorﬂs coordinates</param>
        /// <returns> The result of the component-wise division between the transform's local scale and the given targetScale. </returns>
        public static Vector3 DivideVectors(Vector3 denominator, Vector3 counter)
        {        
            float xLocalScale = counter.x / denominator.x;
            float yLocalScale = counter.y / denominator.y;
            float zLocalScale = counter.z / denominator.z;
            return new Vector3(xLocalScale, yLocalScale, zLocalScale);
        }

        /// <summary>
        /// Multiplies two given vectors pairwise. 
        /// The target scale is a result of an amount of iterations and the specific <paramref name="factor">.
        /// </summary>
        /// <param name="product">The object which shall be shrunk</param>
        /// <param name="iterations">How often the objectToShrink¥s scale shall be multiplied with the shrinkFactor</param>
        ///  <paramref name="factor"> the factor the localScale of the given objectToShrink is multiplied with</param>
        public static void VectorMultiplication(GameObject product, float iterations, Vector3 factor)
        {    
            while (iterations > 0 ) { 
                product.transform.localScale = new Vector3(product.transform.localScale.x * factor.x,product.transform.localScale.y * factor.y, product.transform.localScale.z * factor.z);
                iterations--;
            }
        }
    }
}