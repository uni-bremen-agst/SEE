using UnityEngine;

namespace SEE.Utils
{
    public static class PhysicsUtil
    {
        public static Vector3 ClampVelocity(Vector3 velocity, float maxVelocity)
        {
            Vector3 result = velocity;

            float sqrMag = velocity.sqrMagnitude;
            float sqrMaxVelocity = maxVelocity * maxVelocity;
            if (sqrMag > sqrMaxVelocity)
            {
                result /= Mathf.Sqrt(sqrMag) / maxVelocity;
            }

            return result;
        }

        public static Vector3 Friction(Vector3 velocity, float frictionCoefficient)
        {
            Vector3 normalForce = -velocity;
            Vector3 result = frictionCoefficient * normalForce;
            return result;
        }
    }
}
