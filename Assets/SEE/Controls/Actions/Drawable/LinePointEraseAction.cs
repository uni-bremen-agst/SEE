using Assets.SEE.Game;
using Assets.SEE.Game.Drawable;
using Assets.SEE.Net.Actions.Whiteboard;
using RTG;
using SEE.DataModel;
using SEE.Game;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Allows to create drawings by the mouse cursor.
    /// It serves as an example for a continuous action that modifies the
    /// scene while active.
    /// </summary>
    class LinePointEraseAction : AbstractPlayerAction
    {

        private static bool isActive = false;
        private static Vector3 hitPoint = Vector3.zero;
        /// <summary>
        /// Continues the line at the point of the mouse position and draws it.
        /// See <see cref="ReversibleAction.Awake"/>.
        /// </summary>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            { 
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) &&
                    Raycasting.RaycastAnythingBackface(out RaycastHit raycastHit) &&
                    GameDrawableFinder.hasDrawableParent(raycastHit.collider.gameObject))
                {
                    GameObject hittedObject = raycastHit.collider.gameObject;
                    if (hittedObject.CompareTag(Tags.Line))
                    {
                        LineRenderer lineRenderer = hittedObject.GetComponent<LineRenderer>();
                        Vector3[] positions = new Vector3[lineRenderer.positionCount];
                        lineRenderer.GetPositions(positions);
                        List<Vector3> positionsList = positions.ToList();
                        Line originLine = Line.GetLine(hittedObject);

                        Vector3[] transformedPositions = positions;
                        hittedObject.transform.TransformPoints(transformedPositions);
                        List<Line> lines = new();
                        List<int> matchedIndexes = DrawableHelper.GetNearestIndexes(transformedPositions, raycastHit.point);
                        isActive = GameLineSplit.GetSplittedPositions(isActive, originLine, matchedIndexes, positionsList, lines, true);

                        memento = new Memento(hittedObject, GameDrawableFinder.FindDrawableParent(hittedObject), lines);
                        mementoList.Add(memento);
                        new FastEraseNetAction(memento.drawable.name, memento.drawable.transform.parent.name, memento.originalLine.id).Execute();
                        Destroyer.Destroy(hittedObject.transform.parent.gameObject);

                    }
                }
                bool isMouseButtonUp = Input.GetMouseButtonUp(0);
                // The action is considered complete if the mouse button is no longer pressed.
                if (isMouseButtonUp)
                {
                    isActive = false;
                    currentState = ReversibleAction.Progress.Completed;
                }
                // The action is considered complete if the mouse button is no longer pressed.
                return isMouseButtonUp;
            }
            return false;
        }

        private List<Memento> mementoList = new List<Memento>();
        private Memento memento;

        private class Memento
        {
            public Line originalLine;
            public readonly GameObject drawable;
            public List<Line> lines;

            public Memento(GameObject originalLine, GameObject drawable, List<Line> lines)
            {
                this.originalLine = Line.GetLine(originalLine);
                this.drawable = drawable;
                this.lines = lines;
            }
        }

        /// <summary>
        /// Destroys the drawn line.
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            List<Memento> reverseList = new(mementoList);
            reverseList.Reverse();
            foreach (Memento mem in reverseList)
            {

                mem.originalLine.gameObject = GameDrawer.ReDrawLine(mem.drawable, mem.originalLine.id, mem.originalLine.rendererPositions,
                    mem.originalLine.color, mem.originalLine.thickness, mem.originalLine.orderInLayer,
                    mem.originalLine.position, mem.originalLine.parentEulerAngles);
                new DrawOnNetAction(mem.drawable.name, GameDrawableFinder.GetDrawableParentName(mem.drawable), mem.originalLine.id,
                    mem.originalLine.rendererPositions, mem.originalLine.color, mem.originalLine.thickness,
                    mem.originalLine.orderInLayer, mem.originalLine.position, mem.originalLine.parentEulerAngles).Execute();

                foreach (Line line in mem.lines)
                {
                    Line refreshed;
                    if (line.gameObject == null && line.id != null)
                    {
                        refreshed = Line.GetLine(GameDrawableFinder.FindChild(mem.drawable, line.id));
                    }
                    else
                    {
                        refreshed = Line.GetLine(line.gameObject);
                    }
                    new FastEraseNetAction(mem.drawable.name, GameDrawableFinder.GetDrawableParentName(mem.drawable), refreshed.id).Execute();
                    Destroyer.Destroy(refreshed.gameObject.transform.parent.gameObject);
                }
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
            foreach (Memento mem in mementoList)
            {
                Line origin;
                if (mem.originalLine.gameObject == null && mem.originalLine.id != null)
                {
                    origin = Line.GetLine(GameDrawableFinder.FindChild(mem.drawable, mem.originalLine.id));
                }
                else
                {
                    origin = Line.GetLine(mem.originalLine.gameObject);
                }
                new FastEraseNetAction(mem.drawable.name, GameDrawableFinder.GetDrawableParentName(mem.drawable), origin.id).Execute();
                Destroyer.Destroy(origin.gameObject.transform.parent.gameObject);

                foreach (Line line in mem.lines)
                {
                    line.gameObject = GameDrawer.ReDrawLine(mem.drawable, line.id, line.rendererPositions, line.color, line.thickness,
                    line.orderInLayer, line.position, line.parentEulerAngles);
                    new DrawOnNetAction(mem.drawable.name, GameDrawableFinder.GetDrawableParentName(mem.drawable), line.id, line.rendererPositions, line.color, line.thickness,
                        line.orderInLayer, line.position, line.parentEulerAngles).Execute();
                }
            }
        }

        /// <summary>
        /// A new instance of <see cref="EraseAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EraseAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new LinePointEraseAction();
        }

        /// <summary>
        /// A new instance of <see cref="EraseAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EraseAction"/></returns>
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
            return ActionStateTypes.LinePointErase;
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
            if (memento == null || memento.drawable == null)
            {
                return new HashSet<string>();
            }
            else
            {
                HashSet<string> changedObjects = new HashSet<string>();
                changedObjects.Add(memento.drawable.name);
                // foreach(Memento mem in mementoList)
                // {
                changedObjects.Add(memento.originalLine.id);
                // }
                return changedObjects;
            }
        }
    }
}
