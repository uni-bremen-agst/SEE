using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game.GestureRecognition;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using Plane = UnityEngine.Plane;

namespace SEE.Controls.Actions.Architecture
{
    /// <summary>
    /// Action to create new architecture graph elements. Implementation of <see cref="AbstractArchitectureAction"/>.
    /// </summary>
    public class DrawArchitectureAction : AbstractArchitectureAction
    {
        public override ArchitectureActionType GetActionType()
        {
            return ArchitectureActionType.Draw;
        }

        /// <summary>
        /// The relative resources path to the gesture prefab object.
        /// </summary>
        private const string GESTURE_PATH_PREFAB_PATH = "Prefabs/Architecture/StrokeGesture";
        
        
        /// <summary>
        /// Struct that holds the state of this action.
        /// </summary>
        private struct ActionState
        {
            internal GameObject pathInstance;
            internal bool isDrawing;
            internal GameObject parentObject;
            internal GameObject sourceNode;
            internal GameObject targetNode;
        }

        /// <summary>
        /// The current action state.
        /// </summary>
        private ActionState actionState;

        /// <summary>
        /// The prefab game object used to render the drawn gesture path.
        /// We assume that this prefab always have an <see cref="TrailRenderer"/> component attached.
        /// </summary>
        private GameObject pathPrefab;

        /// <summary>
        /// The offset used to place the stroke path on top of the node
        /// </summary>
        private Vector3 HeightOffset = new Vector3(0f, 0.001f, 0f);

        /// <summary>
        /// The input actions mapping for the architecture action.
        /// </summary>
        private ArchitectureInputActions actions;

        /// <summary>
        /// Input action for the pen position.
        /// </summary>
        private InputAction positionAction;

        /// <summary>
        /// Creates a new <see cref="AbstractArchitectureAction"/> for this type of action.
        /// </summary>
        /// <returns>The new instance</returns>
        public static AbstractArchitectureAction NewInstance()
        {
            return new DrawArchitectureAction();
        }

        public override void Awake()
        {
            pathPrefab = Resources.Load<GameObject>(GESTURE_PATH_PREFAB_PATH);
            actions = new ArchitectureInputActions();
            
            actions.Drawing.DrawBegin.performed += OnDrawBegin;
            actions.Drawing.Draw.performed += OnDraw;
            positionAction = actions.Drawing.Position;
            actions.Drawing.DrawEnd.performed += OnDrawEnd;
        }

        /// <summary>
        /// Event handler method for the DrawEnd mapping from <see cref="ArchitectureInputActions.DrawingActions"/>
        /// </summary>
        private void OnDrawEnd(InputAction.CallbackContext _)
        {
            actionState.isDrawing = false;
            if (actionState.pathInstance == null)
            {
                actionState = new ActionState();
                return;
            }

            if (DollarPGestureRecognizer.TryRecognizeGesture(actionState.pathInstance,
                out DollarPGestureRecognizer.RecognizerResult result, out Vector3[] rawPoints))
            {
                if (actionState.parentObject.ContainingCity())
                {
                    if (TryRaycastWhiteboard(out RaycastHit hit))
                    {
                        GestureContext ctx = new GestureContext()
                        {
                            HeightOffset = HeightOffset,
                            ParentObject = actionState.parentObject,
                            Source = actionState.sourceNode,
                            Target = hit.collider.gameObject
                        };
                        GestureHandler.HandleGesture(result, rawPoints, ctx);
                    }
                }
            }
            Destroyer.DestroyGameObject(actionState.pathInstance);
            actionState = new ActionState();
        }

        
        
        /// <summary>
        /// Event handler method for the DrawBegin mapping from <see cref="ArchitectureInputActions.DrawingActions"/>.
        /// If the raycast target is a node instantiate the gesture path prefab and assign parentObject and sourceNode.
        /// </summary>
        private void OnDrawBegin(InputAction.CallbackContext _)
        {
            actionState.isDrawing = true;
            if (TryRaycastWhiteboard(out RaycastHit hit))
            {
                actionState.pathInstance =
                    GameObject.Instantiate(pathPrefab, hit.point + HeightOffset, Quaternion.identity);
                actionState.parentObject = hit.transform.gameObject;
                actionState.sourceNode = hit.transform.gameObject;
            }
        }
        
        /// <summary>
        /// Event handler method for the Draw mapping from <see cref="ArchitectureInputActions.DrawingActions"/>,
        /// </summary>
        private void OnDraw(InputAction.CallbackContext _)
        {
            actionState.isDrawing = true;
        }


        public override void Update()
        {
            // Update the pathInstance according to the pen movement.
            if (actionState.isDrawing && actionState.pathInstance != null && TryRaycastWhiteboard(out RaycastHit hit))
            {
                actionState.pathInstance.transform.position = hit.point + HeightOffset;
                actionState.parentObject = hit.collider.gameObject;
            }
        }

        public override void Start()
        {
            actions.Enable();
            actions.Drawing.DrawBegin.performed += OnDrawBegin;
            actions.Drawing.Draw.performed += OnDraw;
            actions.Drawing.DrawEnd.performed += OnDrawEnd;
        }

        public override void Stop()
        {
            actions.Disable();
            actions.Drawing.DrawBegin.performed -= OnDrawBegin;
            actions.Drawing.Draw.performed -= OnDraw;
            actions.Drawing.DrawEnd.performed -= OnDrawEnd;

        }

        /// <summary>
        /// Performs raycast to find a game object with a <see cref="NodeRef"/> or the whiteboard game object
        /// with tag <see cref="Tags.Whiteboard"/>.
        /// </summary>
        /// <param name="hit">the hit struct</param>
        /// <returns>True if the target is a node or the whiteboard object, false otherwise.</returns>
        private bool TryRaycastWhiteboard(out RaycastHit hit)
        { 
            hit = default(RaycastHit);
            Vector2 pointerPosition = positionAction.ReadValue<Vector2>();
            if(Raycasting.RaycastAnything(out hit, pointerPosition))
            {
                GameObject go = hit.collider.gameObject;
                if (go.HasNodeRef() || go.CompareTag(Tags.Whiteboard))
                {
                    return true;
                }
            }

            return false;
        }
    }
}