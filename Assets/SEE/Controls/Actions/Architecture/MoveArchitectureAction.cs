using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.UI3D;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using Plane = UnityEngine.Plane;

namespace SEE.Controls.Actions.Architecture
{
    
    /// <summary>
    /// Action to move architecture graph elements. Implementation of <see cref="AbstractArchitectureAction"/>.
    /// </summary>
    public class MoveArchitectureAction : AbstractArchitectureAction
    {
        public override ArchitectureActionType GetActionType()
        {
            return ArchitectureActionType.Move;
        }
        
        /// <summary>
        /// Struct that holds the information about the dragged game node.
        /// </summary>
        private struct Hit
        {
            internal Hit(Transform hit)
            {
                root = SceneQueries.GetCityRootTransformUpwards(hit);
                transform = hit.transform;
                plane = new Plane(Vector3.up, root.position);
            }

            internal readonly Transform root;
            internal readonly Transform transform;
            internal readonly Plane plane;
        }
        
        /// <summary>
        /// <see cref="MoveGizmo"/> to visualize the dragged distance.
        /// </summary>
        private static readonly MoveGizmo gizomo = MoveGizmo.Create();

        /// <summary>
        ///  Struct that holds the state of this action.
        /// </summary>
        private struct ActionState
        {
            internal bool moving;
            internal Hit hit;
            internal Vector3 startPosition;
            internal float yLevel;
            internal Vector3 mouseOffset;
            internal Transform rootTransform;
            internal bool isWhiteboard;
        }

        /// <summary>
        /// The current action state
        /// </summary>
        private ActionState actionState;

        /// <summary>
        /// The input mapping for the architecture actions.
        /// </summary>
        private ArchitectureInputActions actions;

        /// <summary>
        /// Input action for polling the pen tip pressure value.
        /// </summary>
        private InputAction pressureAction;
        /// <summary>
        /// Input action for polling the pen position.
        /// </summary>
        private InputAction positionAction;
        
        
        
        /// <summary>
        /// Creates a new <see cref="AbstractArchitectureAction"/> for this type of action.
        /// </summary>
        /// <returns></returns>
        public static AbstractArchitectureAction NewInstance()
        {
            return new MoveArchitectureAction();
        }


        public override void Stop()
        {
            actions.Moving.ButtonPress.performed -= OnButtonPress;
            actions.Moving.ButtonRelease.performed -= OnButtonRelease;
            actions.Moving.Disable();
        }

        public override void Start()
        {
            actions.Moving.ButtonPress.performed += OnButtonPress;
            actions.Moving.ButtonRelease.performed += OnButtonRelease;
            actions.Moving.Enable();
        }

        public override void Awake()
        {
            actions = new ArchitectureInputActions();
            pressureAction = actions.Moving.Pressure;
            positionAction = actions.Moving.Position;

        }
        
        
        /// <summary>
        /// Event handler method for the ButtonRelease mapping from <see cref="ArchitectureInputActions.Moving"/>
        /// </summary>
        private void OnButtonRelease(InputAction.CallbackContext _)
        {
            actionState.moving = false;
            gizomo.gameObject.SetActive(false);
            // Only update node and edge relevant data if the dragged object is not the whiteboard
            if (!actionState.isWhiteboard && actionState.hit.transform != actionState.hit.root && actionState.rootTransform != null &&
                Raycasting.RaycastPlane(actionState.hit.plane, out Vector3 _, positionAction.ReadValue<Vector2>()))
            {
                GameNodeMover.FinalizePosition(actionState.hit.transform.gameObject, actionState.startPosition - actionState.mouseOffset);
                GameElementUpdater.UpdateEdgePoints(actionState.hit.transform.gameObject, true);
                GameElementUpdater.UpdateNodeStyles();
            }
            actionState.hit = new Hit();
        }
        
        
        /// <summary>
        /// Event handler method for the ButtonPress mapping from <see cref="ArchitectureInputActions.Moving"/>
        /// </summary>
        private void OnButtonPress(InputAction.CallbackContext _)
        {
            bool isTouching = pressureAction.ReadValue<float>() > 0f;
            actionState.isWhiteboard = false;
            GameObject obj = PenInteractionController.PrimaryHoveredObject;
            if (obj.TryGetEdge(out Edge edge)) return;
            //Check whether the currently hovered object is the whiteboard
            if (obj.CompareTag(Tags.Whiteboard))
            {
                actionState.isWhiteboard = true;
            }
            
            if (obj && isTouching)
            {
                actionState.rootTransform = SceneQueries.GetCityRootTransformUpwards(obj.transform);
                Assert.IsNotNull(actionState.rootTransform);
            }

            if (actionState.rootTransform && Raycasting.RaycastPlane(new Plane(Vector3.up, actionState.rootTransform.position),
                out Vector3 hitPoint, positionAction.ReadValue<Vector2>()))
            {
                actionState.moving = true;
                actionState.hit = new Hit(obj.transform);
                actionState.mouseOffset = actionState.hit.transform.position - hitPoint;
                actionState.startPosition = actionState.hit.transform.position;
                actionState.yLevel = obj.transform.position.y;
                gizomo.gameObject.SetActive(true);
            }
        }

        public override void Update()
        {
            bool isTouching = pressureAction.ReadValue<float>() > 0f;
            if (actionState.moving && isTouching &&
                Raycasting.RaycastPlane(actionState.hit.plane, out Vector3 point, positionAction.ReadValue<Vector2>()))
            {
                
                actionState.hit.transform.position = new Vector3(point.x + actionState.mouseOffset.x,
                    actionState.yLevel, point.z + actionState.mouseOffset.z);
                gizomo.SetPositions(actionState.startPosition - actionState.mouseOffset, actionState.hit.transform.position);
                //Do not try to update the edge points for the whiteboard object
                if (!actionState.isWhiteboard)
                {
                    GameElementUpdater.UpdateEdgePoints(actionState.hit.transform.gameObject);
                }
            }
        }
        
        
    }
}