using System;
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
        /// <param name="denominator">The vector by which the counter´s coordinated are divided by</param>
        /// <param name="counter"> The counter vector - which is divided by the denominator´s coordinates</param>
        /// <returns> The result of the component-wise division between the counter and denominator. </returns>
        public static Vector3 DivideVectors(Vector3 denominator, Vector3 counter)
        {        
            float xLocalScale = counter.x / denominator.x;
            float yLocalScale = counter.y / denominator.y;
            float zLocalScale = counter.z / denominator.z;
            return new Vector3(xLocalScale, yLocalScale, zLocalScale);
        }

        /// <summary>
        /// Exposes mathematically the coordinates of a given vector.
        /// </summary>
        /// <param name="vector">The given vector</param>
        /// <param name="exponent"> The exponent</param>
        /// <returns> The result vector of vector to the power of exponent</returns>
        public static Vector3 ExponentOfVectorCoordinates(Vector3 vector, float exponent)
        {
            vector.x = (float)Math.Pow(vector.x, exponent);
            vector.y = (float)Math.Pow(vector.y, exponent);
            vector.z = (float)Math.Pow(vector.z, exponent);
            return vector;
        }

        /// <summary>
        /// Multiplies two given vectors component-wise. 
        /// </summary>
        /// <param name="product">The first vector, in that case a multiplicand </param>
        ///  <paramref name="factor"> the second vector as a multiplicand</param>
        public static Vector3 VectorMultiplication(Vector3 product, Vector3 factor)
        {    
          return  new Vector3(product.x * factor.x,product.y * factor.y, product.z * factor.z);     
        }
    }
}