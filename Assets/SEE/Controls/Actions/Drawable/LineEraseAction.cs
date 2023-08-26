using Assets.SEE.Game;
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
    class LineEraseAction : AbstractPlayerAction
    {


        /// <summary>
        /// Continues the line at the point of the mouse position and draws it.
        /// See <see cref="ReversibleAction.Awake"/>.
        /// </summary>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) &&
                    Raycasting.RaycastAnything(out RaycastHit raycastHit))
                   // Raycasting.RaycastAnything2D(out RaycastHit2D raycastHit))
                {
                    GameObject hittedObject = raycastHit.collider.gameObject;
                    Debug.Log("Hitted: " + hittedObject);
                    if (hittedObject.CompareTag(Tags.Line)) {
                        LineRenderer lineRenderer = hittedObject.GetComponent<LineRenderer>();
                        Vector3[] positions = new Vector3[lineRenderer.positionCount];
                        lineRenderer.GetPositions(positions);

                        memento = new Memento(hittedObject, GameDrawableFinder.FindDrawableParent(hittedObject), hittedObject.name, positions, lineRenderer.material.color, hittedObject.name, 
                            lineRenderer.startWidth, lineRenderer.sortingOrder, hittedObject.transform.position, hittedObject.transform.eulerAngles);
                        mementoList.Add(memento);

                        new LineEraseNetAction(memento.drawable.name, memento.drawable.transform.parent.name, memento.id).Execute();
                        Destroyer.Destroy(hittedObject.transform.parent.gameObject);
                    }
                }
                bool isMouseButtonUp = Input.GetMouseButtonUp(0);
                // The action is considered complete if the mouse button is no longer pressed.
                if (isMouseButtonUp)
                {
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
            public GameObject line;
            public readonly GameObject drawable;
            public readonly string lineName;

            public Vector3[] positions;

            public readonly Color color;

            public readonly string id;

            public readonly float thickness;

            public readonly int orderInLayer;

            public readonly Vector3 position;
            public readonly Vector3 eulerAngles;

            public Memento(GameObject line, GameObject drawable, string lineName, Vector3[] positions, Color color, String id, float thickness, int orderInLayer, Vector3 position, Vector3 eulerAngles)
            {
                this.line = line;
                this.drawable = drawable;
                this.lineName = lineName;
                this.positions = positions;
                this.color = color;
                this.id = id;
                this.thickness = thickness;
                this.orderInLayer = orderInLayer;
                this.position = position;
                this.eulerAngles = eulerAngles;
            }
        }

        /// <summary>
        /// Destroys the drawn line.
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            foreach (Memento mem in mementoList)
            {
                mem.line = GameDrawer.ReDrawLine(mem.drawable, mem.id, mem.positions, mem.color, mem.thickness, mem.orderInLayer, mem.position, mem.eulerAngles);

                new DrawOnNetAction(mem.drawable.name, GameDrawableFinder.GetDrawableParentName(mem.drawable), mem.id, mem.positions, mem.color, mem.thickness, 
                    mem.orderInLayer, mem.position, mem.eulerAngles).Execute();
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
                if (mem.line == null && mem.lineName != null)
                {
                    mem.line = GameDrawableFinder.FindChild(mem.drawable, mem.lineName);
                }
                new LineEraseNetAction(mem.drawable.name, GameDrawableFinder.GetDrawableParentName(mem.drawable), mem.id).Execute();
                Destroyer.Destroy(mem.line.transform.parent.gameObject);
            }
        }

        /// <summary>
        /// A new instance of <see cref="LineEraseAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="LineEraseAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new LineEraseAction();
        }

        /// <summary>
        /// A new instance of <see cref="LineEraseAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="LineEraseAction"/></returns>
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
            return ActionStateTypes.LineErase;
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
                foreach(Memento mem in mementoList)
                {
                    changedObjects.Add(mem.id);
                }
                return changedObjects;
            }
        }
    }
}
