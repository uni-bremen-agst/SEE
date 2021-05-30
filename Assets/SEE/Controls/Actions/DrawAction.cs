using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Allows to create drawings by the mouse cursor.
    /// It serves as an example for a continuous action that modifies the
    /// scene while active.
    /// </summary>
    class DrawAction : AbstractPlayerAction
    {
        /// <summary>
        /// A new instance of <see cref="DrawAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new DrawAction();
        }

        /// <summary>
        /// A new instance of <see cref="DrawAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawAction"/></returns>
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
        /// The positions of the line in world space.
        /// </summary>
        private Vector3[] positions;

        /// <summary>
        /// Static constructor initializing <see cref="material"/>.
        /// </summary>
        static DrawAction()
        {
            // A random color.
            Color color = UnityEngine.Random.ColorHSV();
            // A range of exactly that single random color.
            ColorRange colorRange = new ColorRange(color, color, 1);
            // The materials factory for exactly that single random color.
            Materials materials = new Materials(Materials.ShaderType.TransparentLine, colorRange);
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
            line = new GameObject("line");
            renderer = line.AddComponent<LineRenderer>();
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
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
                {
                    // We create the line on demand so that there is no left-over
                    // when the drawing action has never actually started to draw anything.
                    if (line == null)
                    {
                        SetUpRenderer();
                    }
                    // FIXME: This would needed to be adjusted to VR and AR.
                    // The position at which to continue the line.
                    Vector3 newPosition = Input.mousePosition;
                    newPosition.z = 1.0f;
                    newPosition = Camera.main.ScreenToWorldPoint(newPosition);

                    // Add newPosition to the line renderer.
                    Vector3[] newPositions = new Vector3[positions.Length + 1];
                    Array.Copy(sourceArray: positions, destinationArray: newPositions, length: positions.Length);
                    newPositions[newPositions.Length - 1] = newPosition;
                    positions = newPositions;

                    DrawLine();
                    // The line has been continued so this action has had a visible effect.
                    currentState = ReversibleAction.Progress.Completed;
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
        }

        /// <summary>
        /// Destroys the drawn line.
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            Destroyer.DestroyGameObject(line);
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
            DrawLine();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Draw"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Draw;
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
