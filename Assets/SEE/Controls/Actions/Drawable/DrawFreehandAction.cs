using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.UI.Menu.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This action allows drawing on a drawable.
    /// </summary>
    class DrawFreehandAction : AbstractPlayerAction
    {
        /// <summary>
        /// The different progress states of this action.
        /// </summary>
        private enum ProgressState
        {
            StartDrawing,
            Drawing,
            FinishDrawing
        }

        /// <summary>
        /// Holds the current progress state.
        /// </summary>
        private ProgressState progressState;

        /// <summary>
        /// The line game object. It holds the line renderer and the mesh collider.
        /// </summary>
        private GameObject line;

        /// <summary>
        /// The drawable on that the line should be displayed.
        /// </summary>
        private GameObject drawable;

        /// <summary>
        /// The positions of the line in local space.
        /// It's used for the line renderer.
        /// </summary>
        private Vector3[] positions = new Vector3[1];

        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// The state
        /// </summary>
        private bool drawing = false;

        /// <summary>
        /// Represents that drawing has been finished.
        /// It will be needed to ensure, that completes this action.
        /// </summary>
        private bool finishDrawing = false;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="DrawFreehandAction"/>.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The drawable on that the line should be displayed.
            /// </summary>
            public readonly DrawableConfig drawable;
            /// <summary>
            /// The line. The line configuration <see cref="LineConf"/> contains all required values to redraw.
            /// </summary>
            public LineConf line;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="drawable">The drawable where the line should be placed</param>
            /// <param name="line">Line configuration for redrawing.</param>
            public Memento(GameObject drawable, LineConf line)
            {
                this.drawable = DrawableConfigManager.GetDrawableConfig(drawable);
                this.line = line;
            }
        }

        /// <summary>
        /// Starts the <see cref="DrawFreehandAction"/>.
        /// It sets the progress state to start drawing.
        /// </summary>
        public override void Start()
        {
            progressState = ProgressState.StartDrawing;
        }

        /// <summary>
        /// Enables the line menu and initializes the required Handler.
        /// </summary>
        public override void Awake()
        {
            LineMenu.EnableForDrawing();
        }

        /// <summary>
        /// Stops the <see cref="DrawFreehandAction"/> and hides the line menu.
        /// </summary>
        public override void Stop()
        {
            LineMenu.DisableLineMenu();
            if (progressState != ProgressState.FinishDrawing)
            {
                Destroyer.Destroy(line);
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.DrawOn"/>.
        /// Specifically: Allows the user to draw. 
        /// For this, the left mouse button must be held down as long as you want to draw. 
        /// To finish, release the left mouse button.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                /// This block draws the line when the left mouse button is held down.
                /// Drawing is only possible when targeting a drawable or an object placed on a drawable, 
                /// and the drawable remains unchanged during drawing.
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && !finishDrawing 
                    && Raycasting.RaycastAnything(out RaycastHit raycastHit) 
                    && (raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ||
                        GameFinder.hasDrawable(raycastHit.collider.gameObject))
                    && (drawable == null || drawable != null 
                        && GameFinder.GetDrawable(raycastHit.collider.gameObject).Equals(drawable)))
                {
                    switch (progressState)
                    {
                        case ProgressState.StartDrawing:
                            StartDrawing(raycastHit);
                            break;

                        case ProgressState.Drawing:
                            Drawing(raycastHit);
                            break;
                    }
                }

                /// This block is executed when the drawing should be completed.
                if ((Input.GetMouseButtonUp(0) || !Input.GetMouseButton(0)) && drawing)
                {
                    return FinishDrawing();
                }
            }
            return false;
        }

        /// <summary>
        /// Initialize drawing on the drawable. 
        /// It creates the line with the first hitpoint. 
        /// Since the Line Renderer has to work with local positions, 
        /// the hitpoint is converted to a local position after creation.
        /// </summary>
        /// <param name="raycastHit">The raycast hit on the drawable.</param>
        private void StartDrawing(RaycastHit raycastHit)
        {
            /// Find the drawable for this line.
            drawable = raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ?
                    raycastHit.collider.gameObject : GameFinder.GetDrawable(raycastHit.collider.gameObject);
            drawing = true;
            progressState = ProgressState.Drawing;
            positions[0] = raycastHit.point;
            /// Create the line object.
            line = GameDrawer.StartDrawing(drawable, positions, ValueHolder.currentColorKind,
                ValueHolder.currentPrimaryColor, ValueHolder.currentSecondaryColor, ValueHolder.currentThickness,
                ValueHolder.currentLineKind, ValueHolder.currentTiling);
            /// Transform the first position in local space.
            /// Beforehand, it's not possible because there is no line object on which 'InverseTransformPoint' can be applied.
            positions[0] = line.transform.InverseTransformPoint(positions[0]) - ValueHolder.distanceToDrawable;
        }

        /// <summary>
        /// Extend the line created in <see cref="StartDrawing(RaycastHit)"/> with the new hitpoint.
        /// However, the new point must be different from the last one added.
        /// Because, as mentioned earlier, the Line Renderer operates with local positions, 
        /// the point is first transformed into a local coordinate.
        /// </summary>
        /// <param name="raycastHit">The raycast hit on the drawable</param>
        private void Drawing(RaycastHit raycastHit)
        {
            /// The position at which to continue the line in local space.
            /// To maintain the distance from the drawable, the minimum distance on the Z-axis is subtracted.
            Vector3 newPosition = line.transform.InverseTransformPoint(raycastHit.point) - ValueHolder.distanceToDrawable;
            if (newPosition != positions.Last())
            {
                /// Add newPosition to the line renderer and and start drawing over the network.
                Vector3[] newPositions = new Vector3[positions.Length + 1];
                Array.Copy(sourceArray: positions, destinationArray: newPositions, length: positions.Length);
                newPositions[newPositions.Length - 1] = newPosition;
                positions = newPositions;

                GameDrawer.Drawing(line, positions);
                new DrawFreehandNetAction(drawable.name, GameFinder.GetDrawableParentName(drawable), 
                    LineConf.GetLine(line)).Execute();
            }
        }

        /// <summary>
        /// Finish drawing on the drawable. 
        /// Since a Mesh Collider requires at least three different vertices to function, 
        /// it is first checked whether the collider of the line meets this criterion. 
        /// If not, the line is deleted, and the action is reset. 
        /// Otherwise, the drawing is completed, and the pivot point of the line is set. 
        /// Subsequently, a Memento is created, and the progress state is completed.
        /// </summary>
        /// <returns>Whether drawing has been completed or not</returns>
        private bool FinishDrawing()
        {
            progressState = ProgressState.FinishDrawing;
            drawing = false;

            if (progressState == ProgressState.FinishDrawing)
            {
                if (GameDrawer.DifferentMeshVerticesCounter(line) >= 3)
                {
                    finishDrawing = true;
                    line = GameDrawer.SetPivot(line);
                    LineConf currentLine = LineConf.GetLine(line);
                    memento = new Memento(drawable, currentLine);
                    new DrawFreehandNetAction(memento.drawable.ID, memento.drawable.ParentID, 
                        currentLine).Execute();
                    currentState = ReversibleAction.Progress.Completed;
                    return true;
                }
                else
                {
                    Destroyer.Destroy(line);
                    progressState = ProgressState.StartDrawing;
                    positions = new Vector3[1];
                }
            }
            return false;
        }

        /// <summary>
        /// Reverts this action, i.e., deletes the drawed line.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (line == null)
            {
                line = GameFinder.FindChild(memento.drawable.GetDrawable(), memento.line.id);
            }
            if (line != null)
            {
                new EraseNetAction(memento.drawable.ID, memento.drawable.ParentID, 
                    memento.line.id).Execute();
                Destroyer.Destroy(line);
                line = null;
            }
        }

        /// <summary>
        /// Repeats this action, i.e., redraws the line.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            line = GameDrawer.ReDrawLine(memento.drawable.GetDrawable(), memento.line);
            if (line != null)
            {
                new DrawFreehandNetAction(memento.drawable.ID, memento.drawable.ParentID, 
                    LineConf.GetLine(line)).Execute();
            }
        }

        /// <summary>
        /// A new instance of <see cref="DrawFreehandAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawFreehandAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new DrawFreehandAction();
        }

        /// <summary>
        /// A new instance of <see cref="DrawFreehandAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawFreehandAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.DrawOnWhiteboard"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.DrawFreehand;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// Because this action does not actually change any game object, 
        /// an empty set is always returned.
        /// </summary>
        /// <returns>an empty set or the drawable id and the line id</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento.drawable == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return new HashSet<string>
                {
                    memento.drawable.ID,
                    memento.line.id
                };
            }
        }
    }
}