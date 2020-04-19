using UnityEngine;

namespace SEE.Controls
{
    public abstract class CameraAction : MonoBehaviour
    {
        public abstract void SetSpeed(float speed);
        public abstract void MoveToward(Vector3 direction);
        public abstract void SetBoost(float boost);

        public abstract void Look(bool activated);
        public abstract void RotateToward(Vector3 direction);
    }
}