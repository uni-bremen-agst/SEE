using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// This class illustrates how to implement a reversible action.
    /// </summary>
    class DummyAction : AbstractPlayerAction
    {
        /// <summary>
        /// Returns a new instance of <see cref="DummyAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DummyAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new DummyAction();
        }

        /// <summary>
        /// The list of world-space positions of the objects created by this action.
        /// </summary>
        private IList<Vector3> positions;

        /// <summary>
        /// The list of game objects created by this action. These objects are kept
        /// only to be able to remove them again for Undo(). The objects can be
        /// re-created from the <see cref="positions"/>.
        /// </summary>
        private IList<GameObject> createdObjects;

        /// <summary>
        /// Will be called once when the action is started to be executing for the
        /// first time. Intended for intialization purposes.
        /// See <see cref="ReversibleAction.Awake"/>.
        /// </summary>
        public override void Awake()
        {
            // Could be initialized at the point of the declaration. This illustrates
            // only the use of Awake() here.
            createdObjects = new List<GameObject>();
            positions = new List<Vector3>();
        }

        /// <summary>
        /// Will be called after <see cref="Awake"/> and then again whenever the
        /// action is re-enabled (<see cref="Stop"/> was called before then).
        /// See <see cref="ReversibleAction.Start"/>.
        /// </summary>
        public override void Start()
        {
            Debug.Log($"Action started/resumed: Created {createdObjects.Count} many objects so far.\n");
        }

        /// <summary>
        /// Will be called upon every frame when this action is being executed.
        /// Creates a new object at the position of the mouse if the left mouse 
        /// button was pressed.
        /// See <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 newPosition = Input.mousePosition;
                newPosition.z = 1.0f;
                newPosition = Camera.main.ScreenToWorldPoint(newPosition);
                positions.Add(newPosition);
                CreateObjectAt(newPosition);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a new sphere object at given <paramref name="position"/> and
        /// adds it to <see cref="createdObjects"/>.
        /// </summary>
        /// <param name="position">position in world-space at which to put the object</param>
        private void CreateObjectAt(Vector3 position)
        {
            GameObject newGameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            newGameObject.transform.position = position;
            newGameObject.transform.localScale = Vector3.one / 10.0f;
            createdObjects.Add(newGameObject);
        }

        /// <summary>
        /// Will be called when another action is to be executed. This signals that
        /// the action is to be put on hold. No <see cref="Update"/> will occur
        /// while on hold. It may be re-enabled by <see cref="Start"/> again.
        /// </summary>
        public override void Stop()
        {
            // Nothing really needs to be done for this example action.
            Debug.Log($"Action stopped: Created {createdObjects.Count} many objects so far.\n");
        }

        /// <summary>
        /// The undo operation which destroys all objects created by this action.
        /// See <see cref="ReversibleAction.Undo"/>.
        /// </summary>
        public override void Undo()
        {
            foreach (GameObject gameObject in createdObjects)
            {
                Destroyer.DestroyGameObject(gameObject);
            }
        }

        /// <summary>
        /// The redo operation which restores the state at the point in time when 
        /// <see cref="Undo"/> was called. It will re-generate all destroyed 
        /// game objects created by this action.
        /// See <see cref="ReversibleAction.Redo"/>.
        /// </summary>
        public override void Redo()
        {
            foreach (Vector3 position in positions)
            {
                CreateObjectAt(position);
            }
        }
    }
}
