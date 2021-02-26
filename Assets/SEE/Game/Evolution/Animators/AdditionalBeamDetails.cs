using UnityEngine;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// Used for customizing power beams through the inspector
    /// </summary>
    public class AdditionalBeamDetails : MonoBehaviour
    {

        /// <summary>
        /// Color given to power beams of newly added nodes, inherits from inspector (default is grey)
        /// </summary>
        public static Color newBeamColor;

        /// <summary>
        /// Color for power beams of newly added nodes, can be set in inspector
        /// </summary>
        [Tooltip("Changes the color the beam of newly created nodes")]
        public Color InspectorNewBeamColor;

        /// <summary>
        /// Changed nodes beam color, inherits from inspector variable
        /// </summary>
        public static Color changedBeamColor;

        /// <summary>
        /// Beam color of changed nodes. Can be picked in inspector.
        /// </summary>
        [Tooltip("Changes the color of the beam of changed nodes")]
        public Color InspectorChangedBeamColor;

        /// <summary>
        /// Deleted nodes beam color, inherits from inspector variable
        /// </summary>
        public static Color deletedBeamColor;

        /// <summary>
        /// Beam color of deleted nodes. Can be picked in inspector.
        /// </summary>
        [Tooltip("Sets the color the beam of deleted nodes will have")]
        public Color InspectorDeletedBeamColor;

        /// <summary>
        /// Dimensions of power beams, inherits from inspector variable
        /// </summary>
        public static Vector3 powerBeamDimensions;

        /// <summary>
        /// Dimensions of power beams to be shown in inspector
        /// </summary>
        [Tooltip("Sets the width/height of power beams")]
        public Vector3 InspectorPowerBeamDimensions;

        private void Awake()
        {
            newBeamColor = InspectorNewBeamColor;
            changedBeamColor = InspectorChangedBeamColor;
            deletedBeamColor = InspectorDeletedBeamColor;
            powerBeamDimensions = InspectorPowerBeamDimensions;
            float beamX = 0.02f;
            float beamY = 3f;
            float beamZ = 0.02f;
            if (powerBeamDimensions.x <= beamX)
            {
                powerBeamDimensions = new Vector3(beamX, powerBeamDimensions.y, powerBeamDimensions.z);
            }
            if (powerBeamDimensions.y <= 0)
            {
                powerBeamDimensions = new Vector3(powerBeamDimensions.x, beamY, powerBeamDimensions.z);
            }
            if (powerBeamDimensions.z <= beamZ)
            {
                powerBeamDimensions = new Vector3(powerBeamDimensions.x, powerBeamDimensions.y, beamZ);
            }
        }
    }
}
