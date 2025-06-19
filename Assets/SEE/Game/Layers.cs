using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// The Unity tags for the graph entities represented by the GameObjects.
    /// </summary>
    public static class Layers
    {
        #region LayerNames

        /// <summary>
        /// Internal name of the built-in default layer.
        /// </summary>
        private readonly static string DefaultLayerName = "Default";

        /// <summary>
        /// Internal name of the layer for user-interactable objects.
        /// </summary>
        private readonly static string InteractableAuxiliaryObjectsLayerName = "InteractableAuxiliaryObjects";

        /// <summary>
        /// Internal name of the layer for inactive unser-interactable objects.
        /// </summary>
        private readonly static string NonInteractableAuxiliaryObjectsLayerName = "NonInteractableAuxiliaryObjects";

        /// <summary>
        /// Internal name of the layer for user-interactable SEE city graph objects.
        /// </summary>
        private readonly static string InteractableGraphObjectsLayerName = "InteractableGraphObjects";

        /// <summary>
        /// Internal name of the layer for inactive user-interactable SEE city graph objects.
        /// </summary>
        private readonly static string NonInteractableGraphObjectsLayerName = "NonInteractableGraphObjects";

        #endregion LayerNames

        #region LayersIDs

        /// <summary>
        /// Backing field for the <see cref="Default"/> property.
        /// </summary>
        private static int? defaultLayer;

        /// <summary>
        /// Cached property index for the <see cref="DefaultLayerName"/> layer.
        /// </summary>
        public static int Default
        {
            get
            {
                defaultLayer ??= LayerMask.NameToLayer(DefaultLayerName);

                if (defaultLayer < 0)
                {
                    Debug.LogError($"Layer does not exist: {DefaultLayerName}");
                }

                return defaultLayer.Value;
            }
        }

        /// <summary>
        /// Backing field for the <see cref="InteractableAuxiliaryObjects"/> property.
        /// </summary>
        private static int? interactableAuxiliaryObjects;

        /// <summary>
        /// Cached property index for the <see cref="InteractableAuxiliaryObjectsLayerName"/> layer.
        /// </summary>
        public static int InteractableAuxiliaryObjects
        {
            get
            {
                interactableAuxiliaryObjects ??= LayerMask.NameToLayer(InteractableAuxiliaryObjectsLayerName);

                if (interactableAuxiliaryObjects < 0)
                {
                    Debug.LogError($"Layer does not exist: {InteractableAuxiliaryObjectsLayerName}");
                }

                return interactableAuxiliaryObjects.Value;
            }
        }

        /// <summary>
        /// Backing field for the <see cref="NonInteractableAuxiliaryObjects"/> property.
        /// </summary>
        private static int? nonInteractableAuxiliaryObjects;

        /// <summary>
        /// Cached property index for the <see cref="NonInteractableAuxiliaryObjectsLayerName"/> layer.
        /// </summary>
        public static int NonInteractableAuxiliaryObjects
        {
            get
            {
                nonInteractableAuxiliaryObjects ??= LayerMask.NameToLayer(NonInteractableAuxiliaryObjectsLayerName);

                if (nonInteractableAuxiliaryObjects < 0)
                {
                    Debug.LogError($"Layer does not exist: {NonInteractableAuxiliaryObjectsLayerName}");
                }

                return nonInteractableAuxiliaryObjects.Value;
            }
        }

        /// <summary>
        /// Backing field for the <see cref="InteractableGraphObjects"/> property.
        /// </summary>
        private static int? interactableGraphObjects;

        /// <summary>
        /// Cached property index for the <see cref="InteractableGraphObjectsLayerName"/> layer.
        /// </summary>
        public static int InteractableGraphObjects
        {
            get
            {
                interactableGraphObjects ??= LayerMask.NameToLayer(InteractableGraphObjectsLayerName);

                if (interactableGraphObjects < 0)
                {
                    Debug.LogError($"Layer does not exist: {InteractableGraphObjectsLayerName}");
                }

                return interactableGraphObjects.Value;
            }
        }

        /// <summary>
        /// Backing field for the <see cref="NonInteractableGraphObjects"/> property.
        /// </summary>
        private static int? nonInteractableGraphObjects;

        /// <summary>
        /// Cached property index for the <see cref="NonInteractableGraphObjectsLayerName"/> layer.
        /// </summary>
        public static int NonInteractableGraphObjects
        {
            get
            {
                nonInteractableGraphObjects ??= LayerMask.NameToLayer(NonInteractableGraphObjectsLayerName);

                if (nonInteractableGraphObjects < 0)
                {
                    Debug.LogError($"Layer does not exist: {NonInteractableGraphObjectsLayerName}");
                }

                return nonInteractableGraphObjects.Value;
            }
        }

        #endregion LayersIDs

        #region LayerMasks

        /// <summary>
        /// Backing field for <see cref="InteractableObjectsLayerMask"/>.
        /// </summary>
        private static LayerMask? interactableObjectsLayerMask = null;

        /// <summary>
        /// Cached layer mask for the <see cref="InteractableAuxiliaryObjects"/> and <see cref="InteractableGraphObjects"/> layers.
        /// </summary>
        public static LayerMask InteractableObjectsLayerMask
        {
            get
            {
                interactableObjectsLayerMask ??= (1 << InteractableAuxiliaryObjects) | (1 << InteractableGraphObjects);
                return interactableObjectsLayerMask.Value;
            }
        }

        /// <summary>
        /// Backing field for <see cref="AnyInteractableObjectsLayerMask"/>.
        /// </summary>
        private static LayerMask? anyInteractableObjectsLayerMask = null;

        /// <summary>
        /// Cached layer mask for the <see cref="InteractableAuxiliaryObjects"/>, <see cref="NonInteractableAuxiliaryObjects"/>,
        /// <see cref="InteractableGraphObjects"/>, and <see cref="NonInteractableGraphObjects"/> layers.
        /// </summary>
        public static LayerMask AnyInteractableObjectsLayerMask
        {
            get
            {
                anyInteractableObjectsLayerMask ??=
                        (1 << InteractableAuxiliaryObjects)
                        | (1 << NonInteractableAuxiliaryObjects)
                        | (1 << InteractableGraphObjects)
                        | (1 << NonInteractableGraphObjects);
                return anyInteractableObjectsLayerMask.Value;
            }
        }

        /// <summary>
        /// Backing field for <see cref="AuxiliaryObjectsLayerMask"/>.
        /// </summary>
        private static LayerMask? auxiliaryObjectsLayerMask = null;

        /// <summary>
        /// Cached layer mask for the <see cref="InteractableAuxiliaryObjects"/> and <see cref="NonInteractableAuxiliaryObjects"/> layers.
        /// </summary>
        public static LayerMask AuxiliaryObjectsLayerMask
        {
            get
            {
                auxiliaryObjectsLayerMask ??= (1 << InteractableAuxiliaryObjects) | (1 << NonInteractableAuxiliaryObjects);
                return auxiliaryObjectsLayerMask.Value;
            }
        }

        /// <summary>
        /// Backing field for <see cref="InteractableAuxiliaryObjectsLayerMask"/>.
        /// </summary>
        private static LayerMask? nonInteractableAuxiliaryObjectsLayerMask = null;

        /// <summary>
        /// Cached layer mask for the <see cref="InteractableAuxiliaryObjects"/> layer.
        /// </summary>
        public static LayerMask InteractableAuxiliaryObjectsLayerMask
        {
            get
            {
                nonInteractableAuxiliaryObjectsLayerMask ??= 1 << InteractableAuxiliaryObjects;
                return nonInteractableAuxiliaryObjectsLayerMask.Value;
            }
        }

        /// <summary>
        /// Backing field for <see cref="GraphObjectsLayerMask"/>.
        /// </summary>
        private static LayerMask? graphObjectsLayerMask = null;

        /// <summary>
        /// Cached layer mask for the <see cref="InteractableGraphObjects"/> and <see cref="NonInteractableGraphObjects"/> layers.
        /// </summary>
        public static LayerMask GraphObjectsLayerMask
        {
            get
            {
                graphObjectsLayerMask ??= (1 << InteractableGraphObjects) | (1 << NonInteractableGraphObjects);
                return graphObjectsLayerMask.Value;
            }
        }

        /// <summary>
        /// Backing field for <see cref="InteractableGraphObjectsLayerMask"/>.
        /// </summary>
        private static LayerMask? interactableGraphObjectsLayerMask = null;

        /// <summary>
        /// Cached layer mask for the <see cref="InteractableGraphObjects"/> layer.
        /// </summary>
        public static LayerMask InteractableGraphObjectsLayerMask
        {
            get
            {
                interactableGraphObjectsLayerMask ??= 1 << InteractableGraphObjects;
                return interactableGraphObjectsLayerMask.Value;
            }
        }

        #endregion LayerMasks
    }
}
