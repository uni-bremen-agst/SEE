using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{
    /// <summary>
    /// This script is attached to the <b>SEENetViveControllerLeft</b> and the
    /// <b>SEENetViveControllerRight</b> prefabs. Those prefabs can be instantiated on start-up via
    /// <see cref="InstantiatePrefabAction"/>.
    ///
    /// If an object with the name <see cref="NameL"/> for <see cref="ViveControllerHand.Left"/> or
    /// the name <see cref="NameR"/> for <see cref="ViveControllerHand.Right"/> exists in the
    /// scene, this script copies the transform from that object into itself. Then, the copied
    /// transform is automatically synchonized by the <see cref="TransformView"/> attached to the
    /// object of this script. The mentioned prefabs contain a visual representation of a simple
    /// controller, which then can be seen by the other players.
    /// </summary>
    public class SEENetViveController : MonoBehaviour
    {
        /// <summary>
        /// Defines the possible controller hand side values.
        /// </summary>
        public enum ViveControllerHand
        {
            Left,
            Right
        }

        /// <summary>
        /// The name of the left controller <see cref="GameObject"/>.
        /// </summary>
        private const string NameL = "/Player Rig/Interaction Manager/VR Vive-style Controller (Left)";

        /// <summary>
        /// The name of the right controller <see cref="GameObject"/>.
        /// </summary>
        private const string NameR = "/Player Rig/Interaction Manager/VR Vive-style Controller (Right)";

        /// <summary>
        /// The side of the local controller to be visually synchronized.
        /// </summary>
        [SerializeField] private ViveControllerHand hand;

        /// <summary>
        /// The transform of the local controller to be visually synchronized.
        /// </summary>
        private Transform controllerTransform;

        /// <summary>
        /// Determines the transform to be synchronized.
        /// </summary>
        private void Start()
        {
            string name = hand == ViveControllerHand.Left ? NameL : NameR;
            GameObject controller = GameObject.Find(name);
            Assert.IsNotNull(controller, "Controller could not be found! Is it enabled? Name: " + name);
            controllerTransform = controller.transform;

            // FIXME Here, we may want to disable the visuals of the local controller, as we most likely already have a visual representation
        }

        /// <summary>
        /// Sets the position and rotation of this this object, so that it is synchronized via the
        /// network.
        /// </summary>
        private void Update()
        {
            transform.position = controllerTransform.position;
            transform.rotation = controllerTransform.rotation;
        }
    }
}
