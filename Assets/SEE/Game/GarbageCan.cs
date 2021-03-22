using System;
using UnityEngine;

namespace SEE.Game
{

    /// <summary>
    /// This script is attached to the garbage can and is responsible for interactions with the garbage can on clicks.
    /// </summary>
    [Obsolete("This class has no purpose and will be removed soon.")]
    public class GarbageCan : MonoBehaviour
    {
        /// <summary>
        /// A ray from the mouse position to the hovered object.
        /// FIXME: Not used.
        /// </summary>
        private Ray ray;

        /// <summary>
        /// The object which was hit by the ray.
        /// FIXME: Not used.
        /// </summary>
        private RaycastHit hit;

        /// <summary>
        /// The main camera of the scene.
        /// FIXME: Not used.
        /// </summary>
        private Camera main;

        void Start()
        {
            main = Camera.main;
        }

        void Update()
        {
            // Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef graphElementRef);
            // FIXME: In future - there should be an interaction with the garbage can possible.
            // A user could choose from all deleted Nodes maybe for undo not only the last operation, but also a specific action from history.
        }
    }
}
