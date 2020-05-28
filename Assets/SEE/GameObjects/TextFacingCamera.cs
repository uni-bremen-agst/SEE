using UnityEngine;
using TMPro;

namespace SEE.GO
{
    /// <summary>
    /// Allows to rotate a TextMeshPro component so that it always faces 
    /// the camera.
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class TextFacingCamera : FacingCamera
    {
        /// <summary>
        /// The TextMeshPro component to be rotated.
        /// </summary>
        private TextMeshPro text;

        /// <summary>
        /// Sets the TextMeshPro component.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            text = GetComponent<TextMeshPro>();
        }

        /// <summary>
        /// Rotates only the text.
        /// </summary>
        protected override void LookAtCamera()
        {
            text.transform.localRotation = Quaternion.LookRotation(text.transform.position - mainCamera.transform.position);
        }
    }
}
