using UnityEngine;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// Used for customizing power beams through the inspector.
    /// </summary>
    public class AdditionalBeamDetails : MonoBehaviour
    {
        /// FIXME: Why are these settings static? If there are multiple
        /// evolution cities that have a <see cref="AdditionalBeamDetails"/>
        /// component attached to it, the user could make modifications
        /// to each in which case the color changes of one evolution city
        /// overrides the changes of the other evolution city.
        
        /// <summary>
        /// Color given to power beams of newly added nodes, inherits from inspector (default is grey)
        /// </summary>
        public static Color newBeamColor;

        /// <summary>
        /// Color for power beams of newly added nodes, can be set in inspector
        /// </summary>
        [Tooltip("Color of beams for added nodes.")]
        public Color AdditionColor = Color.green;

        /// <summary>
        /// Changed nodes beam color, inherits from inspector variable
        /// </summary>
        public static Color changedBeamColor;

        /// <summary>
        /// Beam color of changed nodes. Can be picked in inspector.
        /// </summary>
        [Tooltip("Color of beams for changed nodes")]
        public Color ChangeColor = Color.yellow;

        /// <summary>
        /// Deleted nodes beam color, inherits from inspector variable
        /// </summary>
        public static Color deletedBeamColor;

        /// <summary>
        /// Beam color of deleted nodes. Can be picked in inspector.
        /// </summary>
        [Tooltip("Color of beams for deleted node.")]
        public Color DeletionColor = Color.red;

        /// <summary>
        /// Dimensions of power beams, inherits from inspector variable
        /// </summary>
        public static Vector3 powerBeamDimensions;

        /// <summary>
        /// Dimensions of power beams to be shown in inspector
        /// </summary>
        [Tooltip("Scale")]
        public Vector3 Scale = new Vector3(0.02f, 0.3f, 0.02f);

        /// <summary>
        /// The minimal scale for the power beams.
        /// </summary>
        private readonly Vector3 minimalScale = Vector3.zero;

        private void Awake()
        {
            newBeamColor = AdditionColor;
            changedBeamColor = ChangeColor;
            deletedBeamColor = DeletionColor;
            powerBeamDimensions = Scale;
            powerBeamDimensions.x = Mathf.Max(Scale.x, minimalScale.x);
            powerBeamDimensions.y = Mathf.Max(Scale.y, minimalScale.y);
            powerBeamDimensions.z = Mathf.Max(Scale.z, minimalScale.z);
        }
    }
}
