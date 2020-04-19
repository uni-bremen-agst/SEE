using UnityEngine;

namespace SEE.Controls
{
    public class XRCameraAction : CameraAction
    {
        private float speed;

        private Vector3 direction;

        public void Update()
        {
            // The factor of speed depending on the height of the moving object.
            float heightFactor = Mathf.Pow(gameObject.transform.position.y, 2) * 0.01f + 1;
            if (heightFactor > 5)
            {
                heightFactor = 5;
            }

            Vector3 translation = direction * speed * heightFactor;
            gameObject.transform.Translate(translation);
        }

        public override void Look(bool activated)
        {
            // Nothing to be done.
        }

        public override void RotateToward(Vector3 direction)
        {
            // Nothing to be done.
        }

        public override void SetSpeed(float speed)
        {
            this.speed = speed;
        }

        public override void MoveToward(Vector3 direction)
        {
            this.direction = direction;
        }

        public override void SetBoost(float boost)
        {
            // FIXME: Currently not considered.
            throw new System.NotImplementedException();
        }
    }
}