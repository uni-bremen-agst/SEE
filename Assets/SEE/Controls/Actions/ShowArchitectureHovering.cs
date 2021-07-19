namespace SEE.Controls.Actions
{
    /// <summary>
    /// Extension of the <see cref="ShowHovering"/> component.
    /// This variant uses a thicker outline to visually emphasize the graph elements.
    /// </summary>
    public class ShowArchitectureHovering : ShowHovering
    {
        protected override void On(InteractableObject interactableObject, bool isInitiator)
        {
            if (interactable.IsSelected || interactable.IsGrabbed) return;
            if (TryGetComponent(out Outline outline))
            {
                outline.SetColor(isInitiator ? LocalHoverColor : RemoteHoverColor);
            }
            else
            {
                Outline.Create(gameObject, isInitiator ? LocalHoverColor : RemoteHoverColor, 8f);
            }
        }

        

        

        
    }
}