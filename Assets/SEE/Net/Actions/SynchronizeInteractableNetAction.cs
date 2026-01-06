using SEE.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net.Actions
{

    /// <summary>
    /// Updates position, rotation and potentially local scale of an interactable
    /// object.
    ///
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractNetAction"/> before modifying this class!
    /// </summary>
    public class SynchronizeInteractableNetAction : AbstractNetAction
    {
        /// <summary>
        /// The id of the interactable.
        /// </summary>
        public string ID;

        /// <summary>
        /// The position of the interactable.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The rotation of the interactable.
        /// </summary>
        public Quaternion Rotation;

        /// <summary>
        /// The local scale of the interactable or null, if the
        /// local scale is not to be synchronized.
        /// </summary>
        public Vector3? LocalScale = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="interactable">The interactable to be synchronized.</param>
        /// <param name="syncLocalScale">Whether the local scale is to be synchronized.
        /// </param>
        public SynchronizeInteractableNetAction(InteractableObject interactable, bool syncLocalScale)
        {
            Assert.IsNotNull(interactable);

            ID = interactable.name;
            Position = interactable.transform.position;
            Rotation = interactable.transform.rotation;
            if (syncLocalScale)
            {
                LocalScale = interactable.transform.localScale;
            }
        }

        public override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Updates position, rotation and potentially local scale of the interactable
        /// object of given id.
        /// </summary>
        public override void ExecuteOnClient()
        {
            InteractableObject interactable = InteractableObject.Get(ID);
            if (interactable)
            {
                interactable.InteractableSynchronizer?.NotifyJustReceivedUpdate();
                interactable.transform.position = Position;
                interactable.transform.rotation = Rotation;
                if (LocalScale.HasValue)
                {
                    interactable.transform.localScale = LocalScale.Value;
                }
            }
        }
    }
}
