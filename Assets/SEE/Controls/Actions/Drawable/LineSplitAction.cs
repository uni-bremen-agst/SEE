using Assets.SEE.Game;
using Assets.SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Game;
using SEE.Net.Actions;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// Allows to create drawings by the mouse cursor.
    /// It serves as an example for a continuous action that modifies the
    /// scene while active.
    /// </summary>
    class LineSplitAction : AbstractPlayerAction
    {

        private static bool isActive = false;
        private static bool isRunning = false;

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
                    GameDrawableFinder.hasDrawable(raycastHit.collider.gameObject))
                {

                    GameObject hittedObject = raycastHit.collider.gameObject;

                    if (hittedObject.CompareTag(Tags.Line) && !isRunning)
                    {
                        isRunning = true;
                        LineRenderer lineRenderer = hittedObject.GetComponent<LineRenderer>();
                        Vector3[] positions = new Vector3[lineRenderer.positionCount];
                        lineRenderer.GetPositions(positions);
                        List<Vector3> positionsList = positions.ToList();
                        Line originLine = Line.GetLine(hittedObject);

                        Vector3[] transformedPositions = new Vector3[positions.Length];
                        Array.Copy(sourceArray: positions, destinationArray: transformedPositions, length: positions.Length);
                        hittedObject.transform.TransformPoints(transformedPositions);
                        List<Line> lines = new();
                        List<int> matchedIndexes = NearestPoints.GetNearestIndexes(transformedPositions, raycastHit.point);
                        isActive = GameLineSplit.GetSplittedPositions(isActive, GameDrawableFinder.FindDrawable(hittedObject), originLine, matchedIndexes, positionsList, lines, false);

                        memento = new Memento(hittedObject, GameDrawableFinder.FindDrawable(hittedObject), lines, lineRenderer.loop);
                        new EraseNetAction(memento.drawable.name, memento.drawable.transform.parent.name, memento.originalLine.id).Execute();
                        Destroyer.Destroy(hittedObject);
                    }
                }
                bool isMouseButtonUp = Input.GetMouseButtonUp(0);
                // The action is considered complete if the mouse button is no longer pressed.
                if (isMouseButtonUp)
                {
                    isActive = false;
                    isRunning = false;
                    if (memento != null)
                    {
                        currentState = ReversibleAction.Progress.Completed;
                    }
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
            public readonly Line originalLine;
            public readonly bool loop;
            public readonly GameObject drawable;
            public readonly List<Line> lines;

            public Memento(GameObject originalLine, GameObject drawable, List<Line> lines, bool loop)
            {
                this.originalLine = Line.GetLine(originalLine);
                this.drawable = drawable;
                this.lines = lines;
                this.loop = loop;
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
            //memento.originalLine.gameObject = GameDrawer.ReDrawLine(memento.drawable, memento.originalLine);
            GameDrawer.ReDrawLine(memento.drawable, memento.originalLine);
            new DrawOnNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), memento.originalLine).Execute();

            foreach (Line line in memento.lines)
            {
                GameObject lineObj = GameDrawableFinder.FindChild(memento.drawable, line.id);
                /*
                Line refreshed;
                if (line.gameObject == null && line.id != null)
                {
                    refreshed = Line.GetLine(GameDrawableFinder.FindChild(memento.drawable, line.id));
                }
                else
                {
                    refreshed = Line.GetLine(line.gameObject);
                }*/
                new EraseNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), line.id).Execute();
                Destroyer.Destroy(lineObj);//.transform.parent.gameObject);
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
            GameObject originObj = GameDrawableFinder.FindChild(memento.drawable, memento.originalLine.id);
            /*
            Line origin;
            if (memento.originalLine.gameObject == null && memento.originalLine.id != null)
            {
                origin = Line.GetLine(GameDrawableFinder.FindChild(memento.drawable, memento.originalLine.id));
            }
            else
            {
                origin = Line.GetLine(memento.originalLine.gameObject);
            }*/
            new EraseNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), memento.originalLine.id).Execute();
            Destroyer.Destroy(originObj);//.transform.parent.gameObject);

            foreach (Line line in memento.lines)
            {
                //line.gameObject = GameDrawer.ReDrawLine(memento.drawable, line);
                GameDrawer.ReDrawLine(memento.drawable, line);
                new DrawOnNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), line).Execute();
            }
        }

        /// <summary>
        /// A new instance of <see cref="EraseAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EraseAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new LineSplitAction();
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
            return ActionStateTypes.LineSplit;
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
