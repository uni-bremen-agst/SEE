using Assets.SEE.Game;
using Assets.SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using SEE.Game.Drawable.ActionHelpers;

namespace SEE.Controls.Actions.Drawable
{
    public class MovePointAction : AbstractPlayerAction
    {
        private Memento memento;
        private bool start = false;
        private bool didSomething = false;
        private bool isDone = false;

        private static GameObject selectedLine;
        private static bool isActive = false;
        private static Vector3 oldPointPosition;
        private static List<int> indexes;
        private Vector3 newPointPosition;
        private static GameObject drawable;

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;

            if (!Raycasting.IsMouseOverGUI())
            {
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
                    && !isActive && !didSomething && !isDone && Raycasting.RaycastAnythingBackface(out RaycastHit raycastHit) &&
                    GameDrawableFinder.hasDrawable(raycastHit.collider.gameObject) &&
                    raycastHit.collider.gameObject.CompareTag(Tags.Line))
                {
                    selectedLine = raycastHit.collider.gameObject;
                    drawable = GameDrawableFinder.FindDrawable(selectedLine);
                    start = true;

                    BlinkEffect effect = selectedLine.AddOrGetComponent<BlinkEffect>();
                    effect.SetAllowedActionStateType(GetActionStateType());
                    effect.Activate(selectedLine);

                    LineRenderer lineRenderer = selectedLine.GetComponent<LineRenderer>();
                    Vector3[] positions = new Vector3[lineRenderer.positionCount];
                    lineRenderer.GetPositions(positions);

                    Vector3[] transformedPositions = new Vector3[positions.Length];
                    Array.Copy(sourceArray: positions, destinationArray: transformedPositions, length: positions.Length);
                    selectedLine.transform.TransformPoints(transformedPositions);
                    indexes = NearestPoints.GetNearestIndexes(transformedPositions, raycastHit.point);

                    oldPointPosition = positions[indexes[0]];
                }
                if (Input.GetMouseButtonUp(0) && start)
                {
                    isActive = true;
                }

                if (selectedLine != null && selectedLine.GetComponent<BlinkEffect>() != null && selectedLine.GetComponent<BlinkEffect>().GetLoopStatus())
                {
                    string drawableParentName = GameDrawableFinder.GetDrawableParentName(drawable);

                    if (Raycasting.RaycastAnything(out RaycastHit hit))
                    {
                        if (hit.collider.gameObject.CompareTag(Tags.Drawable) || GameDrawableFinder.hasDrawable(hit.collider.gameObject))
                        {
                            didSomething = true;
                            newPointPosition = selectedLine.transform.InverseTransformPoint(hit.point);
                            GameMoveRotator.MovePoint(selectedLine, indexes, newPointPosition);
                            new MovePointNetAction(drawable.name, drawableParentName, selectedLine.name, indexes, newPointPosition).Execute();
                        }
                    }
                }

                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && selectedLine != null && isActive && didSomething)
                {
                    string drawableParentName = GameDrawableFinder.GetDrawableParentName(drawable);
                    GameMoveRotator.MovePoint(selectedLine, indexes, newPointPosition);
                    new MovePointNetAction(drawable.name, drawableParentName, selectedLine.name, indexes, newPointPosition).Execute();
                    memento = new Memento(selectedLine, GameDrawableFinder.FindDrawable(selectedLine), selectedLine.name,
                        indexes, oldPointPosition, newPointPosition);
                    isActive = false;
                    isDone = true;
                    didSomething = false;
                    start = false;
                    selectedLine.GetComponent<BlinkEffect>().Deactivate();

                    selectedLine = null;
                    oldPointPosition = new Vector3();
                    result = true;
                    currentState = ReversibleAction.Progress.Completed;
                }
                return Input.GetMouseButtonUp(0);
            }
            return result;
        }

        private struct Memento
        {
            public GameObject line;
            public readonly GameObject drawable;
            public readonly string id;
            public readonly List<int> indexes;
            public readonly Vector3 oldPointPosition;
            public readonly Vector3 newPointPosition;

            public Memento(GameObject selectedObject, GameObject drawable, string id, List<int> indexes,
                Vector3 oldPointPosition, Vector3 newPointPosition)
            {
                this.line = selectedObject;
                this.drawable = drawable;
                this.id = id;
                this.indexes = indexes;
                this.oldPointPosition = oldPointPosition;
                this.newPointPosition = newPointPosition;
            }
        }

        /// <summary>
        /// Destroys the drawn line.
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            if (memento.line == null && memento.id != null)
            {
                memento.line = GameDrawableFinder.FindChild(memento.drawable, memento.id);
            }

            if (memento.line != null)
            {
                GameObject drawable = GameDrawableFinder.FindDrawable(memento.line);
                string drawableParent = GameDrawableFinder.GetDrawableParentName(drawable);
                GameMoveRotator.MovePoint(memento.line, memento.indexes, memento.oldPointPosition);
                new MovePointNetAction(drawable.name, drawableParent, memento.line.name, memento.indexes, memento.oldPointPosition).Execute();
            }
            if (memento.line != null && memento.line.TryGetComponent<BlinkEffect>(out BlinkEffect currentEffect))
            {
                currentEffect.Deactivate();
            }
        }

        /// <summary>
        /// Redraws the drawn line (setting up <see cref="line"/> and adds <see cref="renderer"/> 
        /// before that).
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Redo()
        {
            base.Redo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            if (memento.line == null && memento.id != null)
            {
                memento.line = GameDrawableFinder.FindChild(memento.drawable, memento.id);
            }
            if (memento.line != null)
            {
                GameObject drawable = GameDrawableFinder.FindDrawable(memento.line);
                string drawableParent = GameDrawableFinder.GetDrawableParentName(drawable);
                GameMoveRotator.MovePoint(memento.line, memento.indexes, memento.newPointPosition);
                new MovePointNetAction(drawable.name, drawableParent, memento.line.name, memento.indexes, memento.newPointPosition).Execute();
            }

            if (memento.line != null && memento.line.TryGetComponent<BlinkEffect>(out BlinkEffect currentEffect))
            {
                currentEffect.Deactivate();
            }
        }

        /// <summary>
        /// A new instance of <see cref="EditAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EditAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new MovePointAction();
        }

        /// <summary>
        /// A new instance of <see cref="EditAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EditAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.MovePoint;
        }

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

        internal static void Reset()
        {
            isActive = false;
            selectedLine = null;
            oldPointPosition = new Vector3();
        }
    }
}