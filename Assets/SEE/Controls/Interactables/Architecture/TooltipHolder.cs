using SEE.DataModel.DG;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace SEE.Controls.Architecture
{
    public class TooltipHolder : MonoBehaviour
    {
        
        /// <summary>
        /// The displayed text
        /// </summary>
        public TMP_Text TooltipText;
        /// <summary>
        /// The child
        /// </summary>
        public Tooltip tooltip;
        public string TooltipMessage;
        public Vector2 offset = new Vector2(-50, 0);
        public PenInteractionController Controller;
        public Vector2 pointerPosition;
        public GameObject element;




        private void Start()
        {
            Assert.IsNotNull(TooltipText);
            Assert.IsNotNull(tooltip);

            Controller.PointerTooltipUpdated += (msg) =>
            {
                TooltipMessage = msg;
            };
        }

        
        
        private void LateUpdate()
        {
            
            pointerPosition = Pointer.current.position.ReadValue();
            transform.position = pointerPosition + offset;
            
            if (!string.IsNullOrEmpty(TooltipMessage))
            {
                Controller.Show(tooltip);
                TooltipText.text = TooltipMessage;
            }
            else
            {
                Controller.Hide(tooltip);
            }
        }
    }
}