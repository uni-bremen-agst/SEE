using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SEE.GO
{
    public class MouseOverNodeGameObject : MonoBehaviour
    {
        public string Text { get; set; }
        public Vector3 Position { get; set; }

        // Start is called before the first frame update
        void Start()
        {
            name = "MouseOver";
        }

        private void OnMouseOver()
        {
            NodeTooltip.ShowTooltip(Text, Position);
        }

        private void OnMouseExit()
        {
            NodeTooltip.HideToolTip();
        }
    }
}

