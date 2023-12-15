using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This action allows the user to move a point of a <see cref="LineConf"/>.
    /// It searched for the nearest point based on the mouse position at the moment of selecting.
    /// </summary>
    public class MovePointAction : AbstractPlayerAction
    {
        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="MovePointAction"/>
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="MovePointAction"/>
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The selected line
            /// </summary>
            public GameObject line;
            /// <summary>
            /// The drawable on that the line is placed.
            /// </summary>
            public readonly DrawableConfig drawable;
            /// <summary>
            /// The id of the line.
            /// </summary>
            public readonly string id;
            /// <summary>
            /// The Indices of the founded nearest position. It can be more then one, because points can overlap.
            /// </summary>
            public readonly List<int> Indices;
            /// <summary>
            /// The old position of the selected points.
            /// </summary>
            public readonly Vector3 oldPointPosition;
            /// <summary>
            /// The new position for the selected points
            /// </summary>
            public readonly Vector3 newPointPosition;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="line">the selected line</param>
            /// <param name="drawable">The drawable on that the line is placed.</param>
            /// <param name="id">the id of the selected line</param>
            /// <param name="Indices">The Indices of the founded nearest position. 
            /// It can be more then one, because points can overlap.</param>
            /// <param name="oldPointPosition">The old position of the selected points</param>
            /// <param name="newPointPosition">The new position for the selected points</param>
            public Memento(GameObject line, GameObject drawable, string id, List<int> Indices,
                Vector3 oldPointPosition, Vector3 newPointPosition)
            {
                this.line = line;
                this.drawable = DrawableConfigManager.GetDrawableConfig(drawable);
                this.id = id;
                this.Indices = Indices;
                this.oldPointPosition = oldPointPosition;
                this.newPointPosition = newPointPosition;
            }
        }

        /// <summary>
        /// Holds the current progress state.
        /// </summary>
        private ProgressState progressState = ProgressState.SelectLine;

        /// <summary>
        /// The progress states of the <see cref="MovePointAction"/>
        /// </summary>
        private enum ProgressState
        {
            SelectLine,
            MovePoint,
            Finish
        }
        /// <summary>
        /// The selected line which point should be moved.
        /// </summary>
        private GameObject selectedLine;
        /// <summary>
        /// The old point position.
        /// </summary>
        private Vector3 oldPointPosition;
        /// <summary>
        /// The index of the nearest founded points. It can be more because points can be overlap.
        /// </summary>
        private List<int> Indices;
        /// <summary>
        /// The new point position.
        /// </summary>
        private Vector3 newPointPosition;
        /// <summary>
        /// The drawable on that the line is displayed.
        /// </summary>
        private GameObject drawable;

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.MovePoint"/>.
        /// It moves a point of a line.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                switch (progressState)
                {
                    /// Selects a line and searches the nearest point(s).
                    case ProgressState.SelectLine:
                        SelectLine();
                        break;

                    /// With this block the user can move the point of the line to the desired point.
                    case ProgressState.MovePoint:
                        MovePoint();
                        break;

                    /// Ends the action, creates a memento, and finalizes the progress.
                    case ProgressState.Finish:
                        memento = new Memento(selectedLine, GameFinder.GetDrawable(selectedLine), 
                            selectedLine.name, Indices, oldPointPosition, newPointPosition);
                        currentState = ReversibleAction.Progress.Completed;
                        return true;
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// Allows the user to select a line, and based on the mouse position 
        /// at the moment of the click, it searches for the nearest point on the line. 
        /// Additionally, the blink effect is activated to illustrate which line has been chosen.
        /// </summary>
        private void SelectLine()
        {
            if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
                && Raycasting.RaycastAnything(out RaycastHit raycastHit) 
                && GameFinder.hasDrawable(raycastHit.collider.gameObject) 
                && raycastHit.collider.gameObject.CompareTag(Tags.Line))
            {
                selectedLine = raycastHit.collider.gameObject;
                drawable = GameFinder.GetDrawable(selectedLine);

                selectedLine.AddOrGetComponent<BlinkEffect>();
                NearestPoints.GetNearestPoints(selectedLine, raycastHit.point,
                    out List<Vector3> positionsList, out List<int> matchedIndices);
                Indices = matchedIndices;
                oldPointPosition = positionsList[Indices[0]];
            }
            if (Input.GetMouseButtonUp(0) && selectedLine != null)
            {
                progressState = ProgressState.MovePoint;
            }
        }

        /// <summary>
        /// Moves the selected points to the mouse cursor position.
        /// </summary>
        public void MovePoint()
        {
            string drawableParentName = GameFinder.GetDrawableParentName(drawable);
            if (selectedLine.GetComponent<BlinkEffect>() != null)
            {
                if (Raycasting.RaycastAnything(out RaycastHit hit))
                {
                    if (hit.collider.gameObject.CompareTag(Tags.Drawable) 
                        || GameFinder.hasDrawable(hit.collider.gameObject))
                    {
                        newPointPosition = selectedLine.transform.InverseTransformPoint(hit.point);
                        GameMoveRotator.MovePoint(selectedLine, Indices, newPointPosition);
                        new MovePointNetAction(drawable.name, drawableParentName, selectedLine.name, 
                            Indices, newPointPosition).Execute();
                    }
                }

                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)))
                {
                    selectedLine.GetComponent<BlinkEffect>().Deactivate();
                }
            }
            /// Left click when the desired point has reached. 
            /// Then the action will be complete in the next steps
            if (Input.GetMouseButtonUp(0) && selectedLine.GetComponent<BlinkEffect>() == null)
            {
                progressState = ProgressState.Finish;
                GameMoveRotator.MovePoint(selectedLine, Indices, newPointPosition);
                new MovePointNetAction(drawable.name, drawableParentName, selectedLine.name, Indices, 
                    newPointPosition).Execute();

            }
        }

        /// <summary>
        /// Destroys the blink effect, if it is still active.
        /// </summary>
        public override void Stop()
        {
            if (selectedLine != null && selectedLine.GetComponent<BlinkEffect>() != null)
            {
                selectedLine.GetComponent<BlinkEffect>().Deactivate();
            }
        }

        /// <summary>
        /// Reverts this action, i.e., it moves the point back to it's original point.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (memento.line == null && memento.id != null)
            {
                memento.line = GameFinder.FindChild(memento.drawable.GetDrawable(), memento.id);
            }

            if (memento.line != null)
            {
                GameMoveRotator.MovePoint(memento.line, memento.Indices, memento.oldPointPosition);
                new MovePointNetAction(memento.drawable.ID, memento.drawable.ParentID, memento.line.name, 
                    memento.Indices, memento.oldPointPosition).Execute();
            }
        }

        /// <summary>
        /// Repeats this action, i.e., moves the point again.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (memento.line == null && memento.id != null)
            {
                memento.line = GameFinder.FindChild(memento.drawable.GetDrawable(), memento.id);
            }
            if (memento.line != null)
            {
                GameMoveRotator.MovePoint(memento.line, memento.Indices, memento.newPointPosition);
                new MovePointNetAction(memento.drawable.ID, memento.drawable.ParentID, memento.line.name, 
                    memento.Indices, memento.newPointPosition).Execute();
            }
        }

        /// <summary>
        /// A new instance of <see cref="MovePointAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MovePointAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new MovePointAction();
        }

        /// <summary>
        /// A new instance of <see cref="MovePointAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MovePointAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.MovePoint"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.MovePoint;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// Because this action does not actually change any game object, 
        /// an empty set is always returned.
        /// </summary>
        /// <returns>the id of the line which point was moved.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento.line == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return new HashSet<string>
                {
                    memento.line.name
                };
            }
        }
    }
}