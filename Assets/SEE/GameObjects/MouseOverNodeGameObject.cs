using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SEE.GO
{
    public class MouseOverNodeGameObject : MonoBehaviour
    {
        /// <summary>
        /// Node Name
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Tooltip position. Never changing this makes it possible to use a Tooltip as Label on any position. Just position it in Editor. 
        /// </summary>
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

