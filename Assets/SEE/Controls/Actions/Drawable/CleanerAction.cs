using Assets.SEE.Game;
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
                    } else if (GameDrawableFinder.hasDrawableParent(hittedObject))
                    {
                        DeleteDrawableChilds(GameDrawableFinder.FindDrawableParent(hittedObject));
                    }
                }
                // The action is considered complete if the mouse button is no longer pressed.
                return Input.GetMouseButtonUp(0);
            }
            return false;
        }

        private void DeleteDrawableChilds(GameObject drawable)
        {
            Transform[] allChildren = GameDrawableFinder.GetAttachedObjectsObject(drawable).GetComponentsInChildren<Transform>();
            foreach (Transform childsTransform in allChildren)
            {
                GameObject child = childsTransform.gameObject;
                // TODO implement the other types
                if (child.CompareTag(Tags.Line))
                {
                    LineRenderer lineRenderer = child.GetComponent<LineRenderer>();
                    Vector3[] positions = new Vector3[lineRenderer.positionCount];
                    lineRenderer.GetPositions(positions);

                    memento = new Memento(child, drawable, positions, child.name, DrawableTypes.Line, child.transform.position, child.transform.parent.localEulerAngles, lineRenderer.loop);
                    memento.color = lineRenderer.material.color;
                    memento.thickness = lineRenderer.startWidth;
                    memento.orderInLayer = lineRenderer.sortingOrder;
                    mementoList.Add(memento);

                    new CleanerNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), memento.id, DrawableTypes.Line).Execute();
                    Destroyer.Destroy(child.transform.parent.gameObject);
                    
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

            public Vector3 position;

            public Vector3 eulerAngles;

            public bool loop;

            public Memento(GameObject gameObject, GameObject drawable, Vector3[] positions, string id, DrawableTypes type, Vector3 position, Vector3 eulerAngles, bool loop)
            {
                this.gameObject = gameObject;
                this.drawable = drawable;
                this.positions = positions;
                this.id = id;
                this.type = type;
                this.position = position;
                this.eulerAngles = eulerAngles;
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
            foreach (Memento mem in mementoList)
            {
                // TODO implement the other types
                switch (mem.type)
                {
                    case DrawableTypes.Line:
                        mem.gameObject = GameDrawer.ReDrawLine(mem.drawable, mem.id, mem.positions, mem.color, mem.thickness, mem.orderInLayer, mem.position, mem.eulerAngles, mem.loop);
                        new DrawOnNetAction(mem.drawable.name, GameDrawableFinder.GetDrawableParentName(mem.drawable), mem.id, mem.positions, 
                            mem.color, mem.thickness, mem.orderInLayer, mem.position, mem.eulerAngles, mem.loop).Execute();
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
                if (mem.gameObject == null && mem.id != null)
                {
                    mem.gameObject = GameDrawableFinder.FindChild(mem.drawable, mem.id);
                }
                new CleanerNetAction(mem.drawable.name, GameDrawableFinder.GetDrawableParentName(mem.drawable), mem.id, mem.type).Execute();
                if (mem.gameObject.CompareTag(Tags.Line))
                {
                    Destroyer.Destroy(mem.gameObject.transform.parent.gameObject);
                }
                else
                {
                    Destroyer.Destroy(mem.gameObject);
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