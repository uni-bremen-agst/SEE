using SEE.Utils;
using UnityEngine;
using SEE.Net;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// This class illustrates how to implement a reversible action.
    /// It creates random objects when the left mouse button is pressed.
    /// </summary>
    class DummyAction : AbstractPlayerAction
    {
        /// <summary>
        /// Returns a new instance of <see cref="DummyAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DummyAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {            
            DummyAction result = new DummyAction(nextPrimitive);
            // Advance nextPrimitive by one but stay in the range of PrimitiveType
            // (and we do not want to create Planes because of their scaling factor
            // very different from other primitive objects).
            nextPrimitive++;
            if (nextPrimitive == PrimitiveType.Plane)
            {
                nextPrimitive = 0;
            }
            return result;
        }

        /// <summary>
        /// The kind of object to be created by the next new instance of this action.
        /// </summary>
        private static PrimitiveType nextPrimitive = PrimitiveType.Sphere;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="primitive">the kind of object to be created by this action</param>
        public DummyAction(PrimitiveType primitive)
        {
            this.primitive = primitive;
        }

        /// <summary>
        /// Returns a new instance of <see cref="DummyAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// The world-space position of the object created by this action.
        /// </summary>
        private Vector3 position;

        /// <summary>
        /// The kind of object created by this action.
        /// </summary>
        private PrimitiveType primitive = PrimitiveType.Sphere;

        /// <summary>
        /// The game object created by this action. This object is kept
        /// only to be able to remove it again for Undo(). The object can be
        /// re-created from the <see cref="position"/>.
        /// </summary>
        private GameObject createdObject;

        /// <summary>
        /// Will be called once when the action is started for the
        /// first time. Intended for intialization purposes.
        /// See <see cref="ReversibleAction.Awake"/>.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            // Could be initialized at the point of the declaration. This illustrates
            // only the use of Awake() here.
            createdObject = null;
            position = Vector3.zero;
        }

        /// <summary>
        /// Will be called after <see cref="Awake"/> and then again whenever the
        /// action is re-enabled (<see cref="Stop"/> was called before then).
        /// See <see cref="ReversibleAction.Start"/>.
        /// </summary>
        public override void Start()
        {
            // Intentionally left blank.
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
            if (Input.GetMouseButtonDown(0) && !Raycasting.IsMouseOverGUI())
            {
                Vector3 newPosition = Input.mousePosition;
                newPosition.z = 1.0f;
                newPosition = Camera.main.ScreenToWorldPoint(newPosition);
                position = newPosition;
                CreateObjectAt(newPosition);
<<<<<<< HEAD
                Debug.Log(newPosition.x);
                new DummyNetAction(newPosition).Execute();
=======
                hadAnEffect = true;
                return true;
            }
            else
            {
                return false;
>>>>>>> origin/master
            }
        }

        /// <summary>
        /// Creates a new sphere object at given <paramref name="position"/> and
        /// adds it to <see cref="createdObjects"/>.
        /// </summary>
        /// <param name="position">position in world-space at which to put the object</param>
<<<<<<< HEAD
        public void CreateObjectAt(Vector3 position)
        {
            GameObject newGameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            newGameObject.transform.position = position;
            newGameObject.transform.localScale = Vector3.one / 10.0f;
            createdObjects.Add(newGameObject);
=======
        private void CreateObjectAt(Vector3 position)
        {            
            GameObject newGameObject = GameObject.CreatePrimitive(primitive);
            if (newGameObject == null)
            {
                Debug.LogError($"Could not create an object of kind {primitive}.\n");
            }
            else
            {
                newGameObject.transform.position = position;
                newGameObject.transform.localScale = Vector3.one / 10.0f;
                createdObject = newGameObject;
            }
>>>>>>> origin/master
        }

        /// <summary>
        /// Will be called when another action is to be executed. This signals that
        /// the action is to be put on hold. No <see cref="Update"/> will occur
        /// while on hold. It may be re-enabled by <see cref="Start"/> again.
        /// </summary>
        public override void Stop()
        {
            // Nothing really needs to be done for this example action.
        }

        /// <summary>
        /// The undo operation which destroys all objects created by this action.
        /// See <see cref="ReversibleAction.Undo"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            Destroyer.DestroyGameObject(createdObject);
        }

        /// <summary>
        /// The redo operation which restores the state at the point in time when 
        /// <see cref="Undo"/> was called. It will re-generate all destroyed 
        /// game objects created by this action.
        /// See <see cref="ReversibleAction.Redo"/>.
        /// </summary>
        public override void Redo()
        {
            base.Redo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            CreateObjectAt(position);
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Dummy"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Dummy;
        }
    }
}
