using SEE.Game;
using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using SEE.Net.Actions.Drawable;
using Assets.SEE.Game.Drawable;
using Assets.SEE.Game;
using System.Linq;
using SEE.Controls.Actions;
using Assets.SEE.Game.UI.Drawable;
using SEE.Game.Drawable.Configurations;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This action allows drawing on a drawable.
    /// </summary>
    class DrawOnAction : AbstractPlayerAction
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
        /// Starts the <see cref="DrawOnAction"/>.
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
        /// Stops the <see cref="DrawOnAction"/> and hides the line menu.
        /// </summary>
        public override void Stop()
        {
            LineMenu.disableLineMenu();
            if (progressState != ProgressState.FinishDrawing)
            {
                Destroyer.Destroy(line);
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.DrawOn"/>.
        /// Specifically: Allows the user to draw. For this, the left mouse button must be held down as long as you want to draw. 
        /// To finish, release the left mouse button.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                /// This block draws the line when the left mouse button is held down.
                /// Drawing is only possible when targeting a drawable or an object placed on a drawable, and the drawable remains unchanged during drawing.
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && !finishDrawing &&
                    Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                    (raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ||
                    GameDrawableFinder.hasDrawable(raycastHit.collider.gameObject))
                    && (drawable == null || drawable != null && GameDrawableFinder.FindDrawable(raycastHit.collider.gameObject).Equals(drawable)))
                {
                    switch (progressState)
                    {
                        case ProgressState.StartDrawing:
                            /// Find the drawable for this line.
                            drawable = raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ?
                                    raycastHit.collider.gameObject : GameDrawableFinder.FindDrawable(raycastHit.collider.gameObject);
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
                            break;

                        case ProgressState.Drawing:
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
                                new DrawOnNetAction(drawable.name, GameDrawableFinder.GetDrawableParentName(drawable), LineConf.GetLine(line)).Execute();
                            }
                            break;
                    }
                }

                /// This block is executed when the drawing should be completed.
                if ((Input.GetMouseButtonUp(0) || !Input.GetMouseButton(0)) && drawing)
                {
                    progressState = ProgressState.FinishDrawing;
                    drawing = false;

                    if (progressState == ProgressState.FinishDrawing)
                    {
                        /// A functional mesh requires a minimum of three vertices. 
                        /// Therefore, lines with fewer than three points are deleted, and the drawing cycle starts again. 
                        /// If at least three vertices exist, the pivot of the line is set to the middle of the line, and the action is completed.
                        if (GameDrawer.DifferentMeshVerticesCounter(line) >= 3)
                        {
                            finishDrawing = true;
                            line = GameDrawer.SetPivot(line);
                            LineConf currentLine = LineConf.GetLine(line);
                            memento = new Memento(drawable, currentLine);
                            new DrawOnNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), currentLine).Execute();
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
            }
            return false;
        }

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="DrawOnAction"/>.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The drawable on that the line should be displayed.
            /// </summary>
            public readonly GameObject drawable;
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
                this.drawable = drawable;
                this.line = line;
            }
        }

        /// <summary>
        /// Reverts this action, i.e., deletes the drawed line.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (line == null)
            {
                line = GameDrawableFinder.FindChild(memento.drawable, memento.line.id);
            }
            if (line != null)
            {
                new EraseNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), memento.line.id).Execute();
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
            line = GameDrawer.ReDrawLine(memento.drawable, memento.line);
            if (line != null)
            {
                new DrawOnNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), LineConf.GetLine(line)).Execute();
            }
        }

        /// <summary>
        /// A new instance of <see cref="DrawOnAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawOnAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new DrawOnAction();
        }

        /// <summary>
        /// A new instance of <see cref="DrawOnAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawOnAction"/></returns>
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
            return ActionStateTypes.DrawOn;
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
                    memento.drawable.name,
                    memento.line.id
                };
            }
        }
    }
}