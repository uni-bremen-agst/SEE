using UnityEngine;

namespace SEE.Game.Evolution
{
    public class AdditionalBeamDetails : MonoBehaviour
    {

        /// <summary>
        /// Newly created nodes beam color
        /// </summary>
        public static Color newBeamColor;

        /// <summary>
        /// Newly created nodes beam color to be shown in inspector
        /// </summary>
        [Tooltip("Changes the color the beam of newly created nodes")]
        public Color NewBeamColor;

        /// <summary>
        /// Changed nodes beam color
        /// </summary>
        public static Color changedBeamColor;

        /// <summary>
        /// Changed nodes beam color to be shown in inspector
        /// </summary>
        [Tooltip("Changes the color of the beam of changed nodes")]
        public Color ChangedBeamColor;

        /// <summary>
        /// Deleted nodes beam color
        /// </summary>
        public static Color deletedBeamColor;

        /// <summary>
        /// Deleted nodes beam color to be shown in inspector
        /// </summary>
        [Tooltip("Sets the color the beam of deleted nodes will have")]
        public Color DeletedBeamColor;

        /// <summary>
        /// Dimensions of power beams
        /// </summary>
        public static Vector3 powerBeamDimensions;

        /// <summary>
        /// Dimensions of power beams to be shown in inspector
        /// </summary>
        [Tooltip("Sets the width/height of power beams")]
        public Vector3 PowerBeamDimensions;

        void Awake()
        {
            Debug.Log("Called awake. New beam color: " + NewBeamColor + " changed: " + ChangedBeamColor );
            newBeamColor = NewBeamColor;
            changedBeamColor = ChangedBeamColor;
            deletedBeamColor = DeletedBeamColor;
            powerBeamDimensions = PowerBeamDimensions;
        }
    }
}
