using UnityEngine;

namespace SEE.Controls
{
    public class XRCameraAction : CameraAction
    {
        public void Update()
        {
            // The factor of speed depending on the height of the moving object.
            float heightFactor = Mathf.Pow(gameObject.transform.position.y, 2) * 0.01f + 1;
            if (heightFactor > 5)
            {
                heightFactor = 5;
            }
            Vector3 translation = directionDevice.Value * throttleDevice.Value * heightFactor;
            gameObject.transform.Translate(translation);
        }
    }
}