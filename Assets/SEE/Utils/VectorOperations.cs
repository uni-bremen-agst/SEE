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
        /// Calculates the ratio between two given vectors.
        /// </summary>
        /// <param name="denominator">The vector by which the counter's coordinates are divided by</param>
        /// <param name="counter">The counter vector - which is divided by the denominator's coordinates</param>
        /// <returns>The result of the component-wise division between the counter and denominator.</returns>
        public static Vector3 DivideVectors(Vector3 denominator, Vector3 counter)
        {
            float xLocalScale = counter.x / denominator.x;
            float yLocalScale = counter.y / denominator.y;
            float zLocalScale = counter.z / denominator.z;
            return new Vector3(xLocalScale, yLocalScale, zLocalScale);
        }

        /// <summary>
        /// Returns the component-wise exponentials of the given <paramref name="vector"/>.
        /// </summary>
        /// <param name="vector">The given vector</param>
        /// <param name="exponent">The exponent</param>
        /// <returns>The component-wise exponentials of the given <paramref name="vector"/></returns>
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
        /// <param name="left">The first vector, in that case the multiplicand </param>
        /// <param name="right">The second vector as a multiplicand</param>
        /// <returns>Component-wise multiplication of the two vectors</returns>
        public static Vector3 VectorMultiplication(Vector3 left, Vector3 right)
        {
            return new Vector3(left.x * right.x, left.y * right.y, left.z * right.z);
        }
    }
}