using Assets.SEE.Net.Actions.Whiteboard;
using SEE.Controls.Actions;
using SEE.Game;
using SEE.Net.Actions;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.SEE.Controls.Actions.Whiteboard
{
    public class CleanerAction : AbstractPlayerAction
    {       
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) &&
                    Raycasting.RaycastAnything(out RaycastHit raycastHit))
                {
                    GameObject hittedObject = raycastHit.collider.gameObject;

                    if (hittedObject.CompareTag(Tags.Drawable))
                    {
                        DeleteDrawableChilds(hittedObject);
                    } else if (hittedObject.transform.parent != null && hittedObject.transform.parent.gameObject.CompareTag(Tags.Drawable))
                    {
                        DeleteDrawableChilds(hittedObject.transform.parent.gameObject);
                    }
                }
                // The action is considered complete if the mouse button is no longer pressed.
                return Input.GetMouseButtonUp(0);
            }
            return false;
        }

        private void DeleteDrawableChilds(GameObject drawable)
        {
            foreach (Transform childsTransform in drawable.transform)
            {
                GameObject child = childsTransform.gameObject;
                // TODO implement the other types
                if (child.CompareTag(Tags.Line))
                {
                    LineRenderer lineRenderer = child.GetComponent<LineRenderer>();
                    Vector3[] positions = new Vector3[lineRenderer.positionCount];
                    lineRenderer.GetPositions(positions);

                    memento = new Memento(child, drawable, positions, child.name, DrawableTypes.Line);
                    memento.color = lineRenderer.material.color;
                    memento.thickness = lineRenderer.startWidth;
                    memento.orderInLayer = lineRenderer.sortingOrder;
                    mementoList.Add(memento);

                    new CleanerNetAction(memento.drawable.name, memento.drawable.transform.parent.name, memento.id, DrawableTypes.Line).Execute();
                    Destroyer.Destroy(child);
                    
                }
            }
            currentState = ReversibleAction.Progress.Completed;
        }

        private List<Memento> mementoList = new List<Memento>();
        private Memento memento;
        private class Memento
        {
            public GameObject gameObject;
            public readonly GameObject drawable;

            public Vector3[] positions;

            public readonly string id;

            public readonly DrawableTypes type;

            public Color color;

            public float thickness;

            public int orderInLayer;

            public Memento(GameObject gameObject, GameObject drawable, Vector3[] positions, string id, DrawableTypes type)
            {
                this.gameObject = gameObject;
                this.drawable = drawable;
                this.positions = positions;
                this.id = id;
                this.type = type;
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
                // TODO implement the other types
                switch (mem.type)
                {
                    case DrawableTypes.Line:
                        mem.gameObject = GameDrawer.ReDrawLine(mem.drawable, mem.id, mem.positions, mem.color, mem.thickness, mem.orderInLayer);
                        new DrawOnNetAction(mem.drawable.name, mem.drawable.transform.parent.name, mem.id, mem.positions, mem.color, mem.thickness, mem.orderInLayer).Execute();
                        break;
                    default: break;
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
                new CleanerNetAction(mem.drawable.name, mem.drawable.transform.parent.name, mem.id, mem.type).Execute();
                Destroyer.Destroy(mem.gameObject);
            }
        }

        /// <summary>
        /// A new instance of <see cref="LineEraseAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="LineEraseAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new CleanerAction();
        }


        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Cleaner;
        }

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
                foreach (Memento mem in mementoList)
                {
                    changedObjects.Add(mem.id);
                }
                return changedObjects;
            }
        }
    }
}