using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.UI.Notification;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using SEE.Utils.History;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This action allows the user to move a point of a <see cref="LineConf"/>.
    /// It searches for the nearest point based on the mouse position at the moment of selecting.
    /// </summary>
    public class MovePointAction : DrawableAction
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
            /// The selected line.
            /// </summary>
            public GameObject Line;
            /// <summary>
            /// The drawable surface on which the line is placed.
            /// </summary>
            public readonly DrawableConfig Surface;
            /// <summary>
            /// The ID of the line.
            /// </summary>
            public readonly string ID;
            /// <summary>
            /// The indices of the found nearest position. There can be more then one,
            /// because points can overlap.
            /// </summary>
            public readonly List<int> Indices;
            /// <summary>
            /// The old position of the selected point.
            /// </summary>
            public readonly Vector3 OldPointPosition;
            /// <summary>
            /// The new position for the selected point.
            /// </summary>
            public readonly Vector3 NewPointPosition;

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="line">the selected line</param>
            /// <param name="surface">The drawable surface on which the line is placed.</param>
            /// <param name="id">the ID of the selected line</param>
            /// <param name="indices">The Indices of the founded nearest position.
            /// It can be more then one, because points can overlap.</param>
            /// <param name="oldPointPosition">The old position of the selected points</param>
            /// <param name="newPointPosition">The new position for the selected points</param>
            public Memento(GameObject line, GameObject surface, string id, List<int> indices,
                Vector3 oldPointPosition, Vector3 newPointPosition)
            {
                Line = line;
                Surface = DrawableConfigManager.GetDrawableConfig(surface);
                ID = id;
                Indices = indices;
                OldPointPosition = oldPointPosition;
                NewPointPosition = newPointPosition;
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
        /// The selected line whose point should be moved.
        /// </summary>
        private GameObject selectedLine;
        /// <summary>
        /// The old point position.
        /// </summary>
        private Vector3 oldPointPosition;
        /// <summary>
        /// The index of the nearest found points. There can be more because
        /// points can be overlap.
        /// </summary>
        private List<int> Indices;
        /// <summary>
        /// The new point position.
        /// </summary>
        private Vector3 newPointPosition;

        /// <summary>
        /// This method manages the player's interaction with the mode
        /// <see cref="ActionStateType.MovePoint"/>.
        /// It moves a point of a line.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        public override bool Update()
        {
            Cancel();

            if (!Raycasting.IsMouseOverGUI())
            {
                switch (progressState)
                {
                    /// Selects a line and searches the nearest point(s).
                    case ProgressState.SelectLine:
                        SelectLine();
                        break;

                    /// Moves the point of the line to the desired point.
                    case ProgressState.MovePoint:
                        MovePoint();
                        break;

                    /// Ends the action, creates a memento, and finalizes the progress.
                    case ProgressState.Finish:
                        memento = new Memento(selectedLine, GameFinder.GetDrawableSurface(selectedLine),
                            selectedLine.name, Indices, oldPointPosition, newPointPosition);
                        CurrentState = IReversibleAction.Progress.Completed;
                        return true;
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// Provides the option to cancel the action.
        /// </summary>
        private void Cancel()
        {
            if (selectedLine != null && SEEInput.Cancel())
            {
                ShowNotification.Info("Canceled", "The action was canceled by the user.");
                BlinkEffect.Deactivate(selectedLine);
                if (progressState != ProgressState.Finish && selectedLine != null)
                {
                    GameObject surface = GameFinder.GetDrawableSurface(selectedLine);
                    string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
                    GameMoveRotator.MovePoint(selectedLine, Indices, oldPointPosition);
                    new MovePointNetAction(surface.name, surfaceParentName, selectedLine.name, Indices,
                        oldPointPosition).Execute();
                }
                selectedLine = null;
                progressState = ProgressState.SelectLine;
            }
        }

        /// <summary>
        /// Allows the user to select a line, and based on the mouse position
        /// at the moment of the click, it searches for the nearest point on the line.
        /// Additionally, the blink effect is activated to show which line has been chosen.
        /// </summary>
        private void SelectLine()
        {
            if (Selector.SelectQueryHasDrawableSurface(out RaycastHit raycastHit)
                && raycastHit.collider.gameObject.CompareTag(Tags.Line))
            {
                selectedLine = raycastHit.collider.gameObject;
                Surface = GameFinder.GetDrawableSurface(selectedLine);

                selectedLine.AddOrGetComponent<BlinkEffect>();
                NearestPoints.GetNearestPoints(selectedLine, raycastHit.point,
                    out List<Vector3> positionsList, out List<int> matchedIndices);
                Indices = matchedIndices;
                oldPointPosition = positionsList[Indices[0]];
            }
            if (SEEInput.MouseUp(MouseButton.Left) && selectedLine != null)
            {
                progressState = ProgressState.MovePoint;
            }
        }

        /// <summary>
        /// Moves the selected points to the mouse cursor position.
        /// </summary>
        private void MovePoint()
        {
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(Surface);
            if (selectedLine.GetComponent<BlinkEffect>() != null)
            {
                if (Raycasting.RaycastAnything(out RaycastHit hit))
                {
                    if (GameFinder.IsOrHasDrawableSurface(hit.collider.gameObject))
                    {
                        newPointPosition = selectedLine.transform.InverseTransformPoint(hit.point);
                        GameMoveRotator.MovePoint(selectedLine, Indices, newPointPosition);
                        new MovePointNetAction(Surface.name, surfaceParentName, selectedLine.name,
                            Indices, newPointPosition).Execute();
                    }
                }

                if (SEEInput.LeftMouseInteraction())
                {
                    selectedLine.GetComponent<BlinkEffect>().Deactivate();
                }
            }
            /// Left click when the desired point has been reached.
            /// Then the action will be completed in the next steps.
            if (SEEInput.MouseUp(MouseButton.Left)
                && selectedLine.GetComponent<BlinkEffect>() == null)
            {
                progressState = ProgressState.Finish;
                GameMoveRotator.MovePoint(selectedLine, Indices, newPointPosition);
                new MovePointNetAction(Surface.name, surfaceParentName, selectedLine.name, Indices,
                    newPointPosition).Execute();
            }
        }

        /// <summary>
        /// Destroys the blink effect, if it is still active.
        /// Resets the changes, if the progress is not finished.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            BlinkEffect.Deactivate(selectedLine);

            if (progressState != ProgressState.Finish && selectedLine != null)
            {
                GameObject surface = GameFinder.GetDrawableSurface(selectedLine);
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
                GameMoveRotator.MovePoint(selectedLine, Indices, oldPointPosition);
                new MovePointNetAction(surface.name, surfaceParentName, selectedLine.name, Indices,
                    oldPointPosition).Execute();
            }
        }

        /// <summary>
        /// Reverts this action, i.e., it moves the point back to its original point.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (memento.Line == null && memento.ID != null)
            {
                memento.Line = GameFinder.FindChild(memento.Surface.GetDrawableSurface(), memento.ID);
            }

            if (memento.Line != null)
            {
                GameMoveRotator.MovePoint(memento.Line, memento.Indices, memento.OldPointPosition);
                new MovePointNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.Line.name,
                    memento.Indices, memento.OldPointPosition).Execute();
            }
        }

        /// <summary>
        /// Repeats this action, i.e., moves the point again.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (memento.Line == null && memento.ID != null)
            {
                memento.Line = GameFinder.FindChild(memento.Surface.GetDrawableSurface(), memento.ID);
            }
            if (memento.Line != null)
            {
                GameMoveRotator.MovePoint(memento.Line, memento.Indices, memento.NewPointPosition);
                new MovePointNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.Line.name,
                    memento.Indices, memento.NewPointPosition).Execute();
            }
        }

        /// <summary>
        /// A new instance of <see cref="MovePointAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MovePointAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new MovePointAction();
        }

        /// <summary>
        /// A new instance of <see cref="MovePointAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MovePointAction"/></returns>
        public override IReversibleAction NewInstance()
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
        /// </summary>
        /// <returns>the ID of the line which point was moved.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento.Line == null)
            {
                return new();
            }
            else
            {
                return new()
                {
                    memento.Line.name
                };
            }
        }
    }
}
