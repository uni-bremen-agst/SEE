using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.GO;
using UnityEngine;

namespace SEE.Controls.Architecture
{
    /// <summary>
    /// Implementation of <see cref="PenInteractionAction"/>.
    /// Shows a Tooltip when hovering over a game object that has a graph element reference attached.
    /// The tooltip contains the <see cref="Node.SourceName"/> for <see cref="Node"/>
    /// and the <see cref="Edge.source"/> and <see cref="Edge.target"/> source names for <see cref="Edge"/>
    /// </summary>
    public class ElementTooltip : PenInteractionAction
    {
        private void OnEnable()
        {
            interactionObject.OnPenEntered += OnEnter;
            interactionObject.OnPenExited += OnExit;
        }
        
        private void OnDisable()
        {
            interactionObject.OnPenEntered -= OnEnter;
            interactionObject.OnPenExited -= OnExit;
        }

        private void OnExit(GameObject initiator)
        {
            if (!interactionObject.controller.PrimaryHoveredObject || interactionObject.controller.PrimaryHoveredObject.CompareTag(Tags.Whiteboard))
            {
                interactionObject.controller.PointerTooltipUpdated?.Invoke(null);
            }
            
        }
        
        private void OnEnter(GameObject initiator)
        {
            if (initiator.CompareTag(Tags.Whiteboard))
            {
                interactionObject.controller.PointerTooltipUpdated?.Invoke(null);
            }
            else if (gameObject.TryGetNode(out Node node))
            {
                interactionObject.controller.PointerTooltipUpdated?.Invoke($"{node.SourceName}");
            }
            else if (gameObject.TryGetEdge(out Edge edge))
            {  
                interactionObject.controller.PointerTooltipUpdated?.Invoke($"{edge.Source.SourceName}][{edge.Target.SourceName}");
            }
        }
    }
}