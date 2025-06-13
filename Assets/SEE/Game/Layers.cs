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
        private static int? _default;

        /// <summary>
        /// Cached property index for the <see cref="DefaultLayerName"/> layer.
        /// </summary>
        public static int Default
        {
            get
            {
                _default ??= LayerMask.NameToLayer(DefaultLayerName);

                if (_default < 0)
                {
                    Debug.LogError($"Layer does not exist: {DefaultLayerName}");
                }

                return _default.Value;
            }
        }

        /// <summary>
        /// Backing field for the <see cref="InteractableAuxiliaryObjects"/> property.
        /// </summary>
        private static int? _interactableAuxiliaryObjects;

        /// <summary>
        /// Cached property index for the <see cref="InteractableAuxiliaryObjectsLayerName"/> layer.
        /// </summary>
        public static int InteractableAuxiliaryObjects
        {
            get
            {
                _interactableAuxiliaryObjects ??= LayerMask.NameToLayer(InteractableAuxiliaryObjectsLayerName);

                if (_interactableAuxiliaryObjects < 0)
                {
                    Debug.LogError($"Layer does not exist: {InteractableAuxiliaryObjectsLayerName}");
                }

                return _interactableAuxiliaryObjects.Value;
            }
        }

        /// <summary>
        /// Backing field for the <see cref="NonInteractableAuxiliaryObjects"/> property.
        /// </summary>
        private static int? _nonInteractableAuxiliaryObjects;

        /// <summary>
        /// Cached property index for the <see cref="NonInteractableAuxiliaryObjectsLayerName"/> layer.
        /// </summary>
        public static int NonInteractableAuxiliaryObjects
        {
            get
            {
                _nonInteractableAuxiliaryObjects ??= LayerMask.NameToLayer(NonInteractableAuxiliaryObjectsLayerName);

                if (_nonInteractableAuxiliaryObjects < 0)
                {
                    Debug.LogError($"Layer does not exist: {NonInteractableAuxiliaryObjectsLayerName}");
                }

                return _nonInteractableAuxiliaryObjects.Value;
            }
        }

        /// <summary>
        /// Backing field for the <see cref="InteractableGraphObjects"/> property.
        /// </summary>
        private static int? _interactableGraphObjects;

        /// <summary>
        /// Cached property index for the <see cref="InteractableGraphObjectsLayerName"/> layer.
        /// </summary>
        public static int InteractableGraphObjects
        {
            get
            {
                _interactableGraphObjects ??= LayerMask.NameToLayer(InteractableGraphObjectsLayerName);

                if (_interactableGraphObjects < 0)
                {
                    Debug.LogError($"Layer does not exist: {InteractableGraphObjectsLayerName}");
                }

                return _interactableGraphObjects.Value;
            }
        }

        /// <summary>
        /// Backing field for the <see cref="NonInteractableGraphObjects"/> property.
        /// </summary>
        private static int? _nonInteractableGraphObjects;

        /// <summary>
        /// Cached property index for the <see cref="NonInteractableGraphObjectsLayerName"/> layer.
        /// </summary>
        public static int NonInteractableGraphObjects
        {
            get
            {
                _nonInteractableGraphObjects ??= LayerMask.NameToLayer(NonInteractableGraphObjectsLayerName);

                if (_nonInteractableGraphObjects < 0)
                {
                    Debug.LogError($"Layer does not exist: {NonInteractableGraphObjectsLayerName}");
                }

                return _nonInteractableGraphObjects.Value;
            }
        }

        #endregion LayersIDs

        #region LayerMasks

        /// <summary>
        /// Backing field for <see cref="InteractableObjectsLayerMask"/>.
        /// </summary>
        private static LayerMask? _interactableObjectsLayerMask = null;

        /// <summary>
        /// Cached layer mask for the <see cref="InteractableAuxiliaryObjects"/> and <see cref="InteractableGraphObjects"/> layers.
        /// </summary>
        public static LayerMask InteractableObjectsLayerMask
        {
            get
            {
                _interactableObjectsLayerMask ??= (1 << InteractableAuxiliaryObjects) | (1 << InteractableGraphObjects);
                return _interactableObjectsLayerMask.Value;
            }
        }

        /// <summary>
        /// Backing field for <see cref="AnyInteractableObjectsLayerMask"/>.
        /// </summary>
        private static LayerMask? _anyInteractableObjectsLayerMask = null;

        /// <summary>
        /// Cached layer mask for the <see cref="InteractableAuxiliaryObjects"/>, <see cref="NonInteractableAuxiliaryObjects"/>,
        /// <see cref="InteractableGraphObjects"/>, and <see cref="NonInteractableGraphObjects"/> layers.
        /// </summary>
        public static LayerMask AnyInteractableObjectsLayerMask
        {
            get
            {
                _anyInteractableObjectsLayerMask ??=
                        (1 << InteractableAuxiliaryObjects)
                        | (1 << NonInteractableAuxiliaryObjects)
                        | (1 << InteractableGraphObjects)
                        | (1 << NonInteractableGraphObjects);
                return _anyInteractableObjectsLayerMask.Value;
            }
        }

        /// <summary>
        /// Backing field for <see cref="AuxiliaryObjectsLayerMask"/>.
        /// </summary>
        private static LayerMask? _auxiliaryObjectsLayerMask = null;

        /// <summary>
        /// Cached layer mask for the <see cref="InteractableAuxiliaryObjects"/> and <see cref="NonInteractableAuxiliaryObjects"/> layers.
        /// </summary>
        public static LayerMask AuxiliaryObjectsLayerMask
        {
            get
            {
                _auxiliaryObjectsLayerMask ??= (1 << InteractableAuxiliaryObjects) | (1 << NonInteractableAuxiliaryObjects);
                return _auxiliaryObjectsLayerMask.Value;
            }
        }

        /// <summary>
        /// Backing field for <see cref="InteractableAuxiliaryObjectsLayerMask"/>.
        /// </summary>
        private static LayerMask? _nonInteractableAuxiliaryObjectsLayerMask = null;

        /// <summary>
        /// Cached layer mask for the <see cref="InteractableAuxiliaryObjects"/> layer.
        /// </summary>
        public static LayerMask InteractableAuxiliaryObjectsLayerMask
        {
            get
            {
                _nonInteractableAuxiliaryObjectsLayerMask ??= 1 << InteractableAuxiliaryObjects;
                return _nonInteractableAuxiliaryObjectsLayerMask.Value;
            }
        }

        /// <summary>
        /// Backing field for <see cref="GraphObjectsLayerMask"/>.
        /// </summary>
        private static LayerMask? _graphObjectsLayerMask = null;

        /// <summary>
        /// Cached layer mask for the <see cref="InteractableGraphObjects"/> and <see cref="NonInteractableGraphObjects"/> layers.
        /// </summary>
        public static LayerMask GraphObjectsLayerMask
        {
            get
            {
                _graphObjectsLayerMask ??= (1 << InteractableGraphObjects) | (1 << NonInteractableGraphObjects);
                return _graphObjectsLayerMask.Value;
            }
        }

        /// <summary>
        /// Backing field for <see cref="InteractableGraphObjectsLayerMask"/>.
        /// </summary>
        private static LayerMask? _interactableGraphObjectsLayerMask = null;

        /// <summary>
        /// Cached layer mask for the <see cref="InteractableGraphObjects"/> layer.
        /// </summary>
        public static LayerMask InteractableGraphObjectsLayerMask
        {
            get
            {
                _interactableGraphObjectsLayerMask ??= 1 << InteractableGraphObjects;
                return _interactableGraphObjectsLayerMask.Value;
            }
        }

        #endregion LayerMasks
    }
}
