using SEE.Controls;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game
{

    /// <summary>
    /// This script is attached to the garbage can and is responsible for interactions with the garbage can on clicks.
    /// </summary>
    public class GarbageCan : MonoBehaviour
    {
        /// <summary>
        /// A ray from the mouse position to the hovered object
        /// </summary>
        private Ray ray;

        /// <summary>
        /// The object which was hitt by the ray.
        /// </summary>
        private RaycastHit hit;

        /// <summary>
        /// The main camera of the scene.
        /// </summary>
        private Camera main;

        // Start is called before the first frame update
        void Start()
        {
            main = Camera.main;
        }

        // Update is called once per frame
        void Update()
        {
        //    Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef graphElementRef);
        // Fixme: In future - there should be an interaction with the garbage can possible.
        // A user could choose from all deleted Nodes maybe for undo not only the last operation, but also a specific action from history.
        }
    }
}
