using UnityEngine;

namespace SEE.Controls.Architecture
{
    /// <summary>
    /// Implementation of the <see cref="PenInteractionAction"/>.
    /// Shows an <see cref="Outline"/>
    /// around the game object when hovered or selected.
    /// </summary>
    public class ElementOutline : PenInteractionAction
    {
        private void OnEnable()
        {
            if (interactionObject != null)
            {
                interactionObject.OnPenEntered += OnEnter;
                interactionObject.OnPenExited += OnExit;
            }
        }

        private void OnDisable()
        {
            if (interactionObject != null)
            {
                interactionObject.OnPenEntered -= OnEnter;
                interactionObject.OnPenExited -= OnExit;
                interactionObject.OnPenSelected -= OnSelected;
                interactionObject.OnPenDeselected -= OnDeselected;
            }
        }

        private void OnDeselected(GameObject initiator)
        {
            
            if (TryGetComponent(out Outline outline))
            {
                DestroyImmediate(outline);
            }
        }

        private void OnExit(GameObject initiator)
        {
            
            if (TryGetComponent(out Outline outline))
            {
                DestroyImmediate(outline);
            }
        }

        private void OnEnter(GameObject initiator)
        {
            if (TryGetComponent(out Outline outline))
            {
                outline.SetColor(Color.black);
            }
            else
            {
                Outline.Create(gameObject, Color.black, 6f);
            }
        }

        private void OnSelected(GameObject initiator)
        {
            if (TryGetComponent(out Outline outline))
            {
                outline.SetColor(Color.black);
            }
            else
            {
                Outline.Create(gameObject, Color.black, 6f);
            }
        }
    }
}