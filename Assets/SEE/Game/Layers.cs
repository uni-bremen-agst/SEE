using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// The Unity tags for the graph entities represented by the GameObjects.
    /// </summary>
    public static class Layers
    {
                /// <summary>
        /// Internal name of the layer for interactable SEE city objects.
        /// </summary>
        private readonly static string InteractableGraphElementsName = "InteractableGraphElements";

        /// <summary>
        /// Internal name of the layer for NOT interactable SEE city objects.
        /// </summary>
        private readonly static string NonInteractableGraphElementsName = "NonInteractableGraphElements";

        /// <summary>
        /// Backing field for the <see cref="InteractableGraphElements"/> property.
        /// </summary>
        private static int? _interactableGraphElements;

        /// <summary>
        /// Cached property index for the <see cref="InteractableGraphElementsName"/> layer.
        /// </summary>
        public static int InteractableGraphElements
        {
            get
            {
                _interactableGraphElements ??= LayerMask.NameToLayer(InteractableGraphElementsName);

                if (_interactableGraphElements < 0)
                {
                    Debug.LogError($"Layer does not exist: {InteractableGraphElementsName}");
                }

                return _interactableGraphElements.Value;
            }
        }

        /// <summary>
        /// Backing field for <see cref="InteractableGraphElementsLayerMask"/>.
        /// </summary>
        private static LayerMask? _interactableGraphElementsLayerMask = null;

        /// <summary>
        /// Cached layer mask for the <see cref="Layers.InteractableGraphElements"/> layer.
        /// </summary>
        public static LayerMask InteractableGraphElementsLayerMask
        {
            get
            {
                _interactableGraphElementsLayerMask ??= 1 << Layers.InteractableGraphElements;
                return _interactableGraphElementsLayerMask.Value;
            }
        }

        /// <summary>
        /// Backing field for the <see cref="NonInteractableGraphElements"/> property.
        /// </summary>
        private static int? _nonInteractableGraphElements;

        /// <summary>
        /// Cached property index for the <see cref="NonInteractableGraphElementsName"/> layer.
        /// </summary>
        public static int NonInteractableGraphElements
        {
            get
            {
                _nonInteractableGraphElements ??= LayerMask.NameToLayer(NonInteractableGraphElementsName);

                if (_nonInteractableGraphElements < 0)
                {
                    Debug.LogError($"Layer does not exist: {NonInteractableGraphElementsName}");
                }

                return _nonInteractableGraphElements.Value;
            }
        }
    }
}
