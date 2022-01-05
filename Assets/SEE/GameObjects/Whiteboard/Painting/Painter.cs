/*
MIT License 
Copyright(c) 2017 MarekMarchlewicz

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#if UNITY_ANDROID
#else
using UnityEngine;

namespace SEE.GO.Whiteboard
{
    [System.Obsolete("Experimental code. Do not use it. May be removed soon.")]
    public class Painter : MonoBehaviour
    {
        [SerializeField]
        private readonly PaintMode paintMode;

        [SerializeField]
        private readonly Transform paintingTransform; // FIXME: Never assigned.

        [SerializeField]
        private readonly float raycastLength = 0.01f;

        [SerializeField]
        private readonly Texture2D brush;

        [SerializeField]
        private readonly float spacing = 1f;

        private float currentAngle = 0f;
        private float lastAngle = 0f;

        private PaintReceiver paintReceiver;
        private Collider paintReceiverCollider;

        private readonly DraggableObject paintingObject;

        private Stamp stamp;

        private Color color;

        private Vector2? lastDrawPosition = null;

        public void Initialize(PaintReceiver newPaintReceiver)
        {
            stamp = new Stamp(brush);
            // FIXME: paintMode is never assigned
            stamp.mode = paintMode;

            paintReceiver = newPaintReceiver;
            paintReceiverCollider = newPaintReceiver.GetComponent<Collider>();
        }

        private void Update()
        {
            currentAngle = -transform.rotation.eulerAngles.z;

            Ray ray = new Ray(paintingTransform.position, paintingTransform.forward);
            RaycastHit hit;

            Debug.DrawRay(ray.origin, ray.direction * raycastLength);

            if (paintReceiverCollider.Raycast(ray, out hit, raycastLength))
            {
                if (lastDrawPosition.HasValue && lastDrawPosition.Value != hit.textureCoord)
                {
                    paintReceiver.DrawLine(stamp, lastDrawPosition.Value, hit.textureCoord, lastAngle, currentAngle, color, spacing);
                }
                else
                {
                    paintReceiver.CreateSplash(hit.textureCoord, stamp, color, currentAngle);
                }

                lastAngle = currentAngle;

                lastDrawPosition = hit.textureCoord;
            }
            else
            {
                lastDrawPosition = null;
            }
        }

        public void ChangeColour(Color newColor)
        {
            color = newColor;
        }

        public void SetRotation(float newAngle)
        {
            currentAngle = newAngle;
        }
    }

}
#endif