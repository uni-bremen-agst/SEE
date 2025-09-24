using SEE.Game.Operator;
using SEE.GO;
using UnityEngine;

namespace SEE.Controls.Interactables
{
    internal sealed class InteractableGraphElement : InteractableObject
    {
        /// <summary>
        /// The graph element this interactable object is attached to.
        /// </summary>
        public GraphElementRef GraphElemRef { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            GraphElemRef = GetComponent<GraphElementRef>();
            hitColor = gameObject.IsNode() ? nodeHitColor : edgeHitColor;
        }

        /// <summary>
        /// The color of the laser pointer when it is hovering over a node.
        /// </summary>
        private static Color nodeHitColor = Color.green;

        /// <summary>
        /// The color of the laser pointer when it is hovering over an edge.
        /// </summary>
        private static Color edgeHitColor = Color.blue;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GraphElemRef = null;
        }

        /// <summary>
        /// Start blinking indefinitely.
        /// </summary>
        protected override void Highlight()
        {
            GraphElementOperator op = gameObject.Operator();
            op.Blink(-1);
            if (op is EdgeOperator eop)
            {
                eop.AnimateDataFlow(true);
            }
        }

        /// <summary>
        /// Stop blinking.
        /// </summary>
        protected override void Unhighlight()
        {
            GraphElementOperator op = gameObject.Operator();
            op.Blink(0);
            if (op is EdgeOperator eop)
            {
                eop.AnimateDataFlow(false);
            }
        }
    }
}
