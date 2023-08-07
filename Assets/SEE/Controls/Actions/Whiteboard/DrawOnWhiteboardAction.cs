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
    class DrawOnWhiteboardAction : AbstractPlayerAction
    {
        /// <summary>
        /// A new instance of <see cref="DrawOnWhiteboardAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawOnWhiteboardAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new DrawOnWhiteboardAction();
        }

        /// <summary>
        /// A new instance of <see cref="DrawOnWhiteboardAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawOnWhiteboardAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// The material for all lines drawn by this action. Will be generated randomly
        /// (see the static constructor).
        /// </summary>
        private static readonly Material material;

        /// <summary>
        /// The object holding the line renderer.
        /// </summary>
        private GameObject line;

        /// <summary>
        /// The renderer used to draw the line.
        /// </summary>
        private LineRenderer renderer;

        /// <summary>
        /// The collider of the line.
        /// </summary>
        private MeshCollider meshCollider;

        /// <summary>
        /// The positions of the line in world space.
        /// </summary>
        private Vector3[] positions;

        private bool drawing = false;

        /// <summary>
        /// Static constructor initializing <see cref="material"/>.
        /// </summary>
        static DrawOnWhiteboardAction()
        {
            // A random color.
            Color color = UnityEngine.Color.red;
            // A range of exactly that single random color.
            ColorRange colorRange = new ColorRange(color, color, 1);
            // The materials factory for exactly that single random color.
            Materials materials = new Materials(Materials.ShaderType.PortalFree, colorRange); //ShaderType.TransparentLine
            // The material for exactly that single random color.
            material = materials.Get(0, 0);
        }

        /// <summary>
        /// Initializes <see cref="positions"/>, <see cref="line"/>, and <see cref="renderer"/>.
        /// See <see cref="ReversibleAction.Awake"/>.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            positions = new Vector3[0];
        }

        /// <summary>
        /// Creates <see cref="line"/> and adds <see cref="renderer"/> to it.
        /// The matrial for the line will be <see cref="material"/>.
        /// Sets the attributes of the line. Does not actually draw anything.
        /// </summary>
        private void SetUpRenderer()
        {
            line = new("line");
            line.tag = Tags.Line;
            renderer = line.AddComponent<LineRenderer>();
            meshCollider = line.AddComponent<MeshCollider>();
            renderer.sharedMaterial = material; // all lines share the same material
            renderer.startWidth = 0.01f;
            renderer.endWidth = renderer.startWidth;
            renderer.useWorldSpace = true;
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
                // FIXME: GameObject.FindGameObjectsWithTag is a very expensive operation
                // and must not be used within Update() as it would otherwise be called
                // for each frame. That will decrease our framerate a lot!
                paintingReceivers = GameObject.FindGameObjectsWithTag("Drawable"); // FIXME: Avoid magic literals.
                ArrayList paintingReceiversList = new(paintingReceivers);

                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
                    && Raycasting.RaycastAnything(out RaycastHit raycastHit)
                    && paintingReceiversList.Contains(raycastHit.collider.gameObject))
                {
                    drawing = true;
                    // We create the line on demand so that there is no left-over
                    // when the drawing action has never actually started to draw anything.
                    if (line == null)
                    {
                        SetUpRenderer();
                    }
                    // FIXME: This would needed to be adjusted to VR and AR.
                    // The position at which to continue the line.
                    Vector3 newPosition = raycastHit.point;

                    // Add newPosition to the line renderer.
                    Vector3[] newPositions = new Vector3[positions.Length + 1];
                    Array.Copy(sourceArray: positions, destinationArray: newPositions, length: positions.Length);
                    newPositions[newPositions.Length - 1] = newPosition;
                    positions = newPositions;

                    DrawLine();
                    // The line has been continued so this action has had a visible effect.
                    currentState = ReversibleAction.Progress.InProgress;
                }
                // The action is considered complete if the mouse button is no longer pressed.
                bool isMouseButtonUp = Input.GetMouseButtonUp(0);
                if (isMouseButtonUp && drawing)
                {
                    drawing = false;
                    Mesh mesh = new();
                    renderer.BakeMesh(mesh, true);
                    meshCollider.sharedMesh = mesh;
                }
                if (isMouseButtonUp)
                {
                    currentState = ReversibleAction.Progress.Completed;
                }
                return isMouseButtonUp;
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
        }

        private void ReDrawLine()
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
            Destroyer.Destroy(line);
            line = null;
            renderer = null;
        }

        /// <summary>
        /// Redraws the drawn line (setting up <see cref="line"/> and adds <see cref="renderer"/>
        /// before that).
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Redo()
        {
            base.Redo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            SetUpRenderer();
            ReDrawLine();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.DrawOnWhiteboard"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.DrawOnWhiteboard;
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
