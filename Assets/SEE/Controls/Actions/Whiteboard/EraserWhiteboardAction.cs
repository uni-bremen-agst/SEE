using SEE.DataModel;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Allows to create drawings by the mouse cursor.
    /// It serves as an example for a continuous action that modifies the
    /// scene while active.
    /// </summary>
    class EraserWhiteboardAction : AbstractPlayerAction
    {
        /// <summary>
        /// A new instance of <see cref="EraserWhiteboardAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EraserWhiteboardAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new EraserWhiteboardAction();
        }

        /// <summary>
        /// A new instance of <see cref="EraserWhiteboardAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EraserWhiteboardAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// The material for all lines drawn by this action. Will be generated randomly
        /// (see the static constructor).
        /// </summary>
        private static Material material;

        /// <summary>
        /// The width of the line.
        /// </summary>
        private float startWidth;

        /// <summary>
        /// If enabled, the lins are defined in world space.
        /// </summary>
        private bool useWorldSpace;

        /// <summary>
        /// The object holding the line renderer.
        /// </summary>
        private GameObject line;

        /// <summary>
        /// The renderer used to draw the line.
        /// </summary>
        private LineRenderer renderer;

        /// <summary>
        /// The positions of the line in world space.
        /// </summary>
        private Vector3[] positions;

        /// <summary>
        /// The collider of the line.
        /// </summary>
        private MeshCollider meshCollider;

        /// <summary>
        /// Static constructor initializing <see cref="material"/>.
        /// </summary>
        static EraserWhiteboardAction() {}

        /// <summary>
        /// Initializes <see cref="positions"/>, <see cref="line"/>, and <see cref="renderer"/>.
        /// See <see cref="ReversibleAction.Awake"/>.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// Creates <see cref="line"/> and adds <see cref="renderer"/> to it.
        /// The matrial for the line will be <see cref="material"/>.
        /// Sets the attributes of the line. Does not actually draw anything.
        /// </summary>
        private void SetUpRenderer()
        {
            line = new GameObject("line");
            line.tag = Tags.Line;
            renderer = line.AddComponent<LineRenderer>();
            meshCollider = line.AddComponent<MeshCollider>();
            renderer.sharedMaterial = material; // all lines share the same material
            renderer.startWidth = startWidth;
            renderer.endWidth = renderer.startWidth;
            renderer.useWorldSpace = useWorldSpace;
            renderer.positionCount = positions.Length;
        }

        /// <summary>
        /// Continues the line at the point of the mouse position and draws it.
        /// See <see cref="ReversibleAction.Awake"/>.
        /// </summary>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                GameObject[] paintingReceivers;
                paintingReceivers = GameObject.FindGameObjectsWithTag("Drawable");
                ArrayList paintingReceiversList = new ArrayList(paintingReceivers);

                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) &&
                    Raycasting.RaycastAnything(out RaycastHit raycastHit))
                {
                    GameObject hittedObject = raycastHit.collider.gameObject;
                    if (hittedObject.tag == Tags.Line) {
                        LineRenderer deletedRenderer = hittedObject.GetComponent<LineRenderer>();
                        positions = new Vector3[deletedRenderer.positionCount];
                        deletedRenderer.GetPositions(positions);
                        material = deletedRenderer.material;
                        startWidth = deletedRenderer.startWidth;
                        useWorldSpace = deletedRenderer.useWorldSpace;

                        GameObject.Destroy(hittedObject);
                        currentState = ReversibleAction.Progress.Completed;
                    }
                }
                // The action is considered complete if the mouse button is no longer pressed.
                return Input.GetMouseButtonUp(0);
            }
            return false;
        }

        /// <summary>
        ///  Draws the line given the <see cref="positions"/>.
        /// </summary>
        private void DrawLine()
        {   
            renderer.positionCount = positions.Length;
            renderer.SetPositions(positions);
            Mesh mesh = new Mesh();
            renderer.BakeMesh(mesh, true);
            meshCollider.sharedMesh = mesh;
        }

        /// <summary>
        /// Destroys the drawn line.
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            //Ersetzen durch Netzwerk Action, damit Undo richtig funktioniert. Sonst problem mit Undo der wiederhergestellten Linien.
            SetUpRenderer();
            DrawLine();
        }

        /// <summary>
        /// Redraws the drawn line (setting up <see cref="line"/> and adds <see cref="renderer"/> 
        /// before that).
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Redo()
        {
            base.Redo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            Destroyer.Destroy(line);
            line = null;
            renderer = null;

        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.DrawOnWhiteboard"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.EraserOnWhiteboard;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// Because this action does not actually change any game object, 
        /// an empty set is always returned.
        /// </summary>
        /// <returns>an empty set</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>();
        }
    }
}
