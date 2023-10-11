using Assets.SEE.Game;
using Assets.SEE.Game.Drawable;
using SEE.Game;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using SEE.Game.Drawable.Configurations;
using System.Linq;

namespace SEE.Controls.Actions.Drawable
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
                    } else if (GameDrawableFinder.hasDrawable(hittedObject))
                    {
                        DeleteDrawableChilds(GameDrawableFinder.FindDrawable(hittedObject));
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
                if (Tags.DrawableTypes.Contains(child.tag))
                {
                    memento = new Memento(drawable, new DrawableType().Get(child));
                    mementoList.Add(memento);

                    new EraseNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), memento.type.id).Execute();
                    Destroyer.Destroy(child);
                }
                /*
                if (child.CompareTag(Tags.Line))
                {
                    LineRenderer lineRenderer = child.GetComponent<LineRenderer>();
                    Vector3[] positions = new Vector3[lineRenderer.positionCount];
                    lineRenderer.GetPositions(positions);
                    memento = new Memento(child, drawable, DrawableTypesEnum.Line, child.name);
                    memento.line = Line.GetLine(child);
                    mementoList.Add(memento);

                    new EraseNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), memento.id).Execute();
                    Destroyer.Destroy(child);
                    //Destroyer.Destroy(child.transform.parent.gameObject);  
                }*/
            }
            currentState = ReversibleAction.Progress.Completed;
        }

        private List<Memento> mementoList = new List<Memento>();
        private Memento memento;

        private class Memento
        {
           // public GameObject gameObject;
            public readonly GameObject drawable;
            public readonly DrawableType type;
           // public readonly DrawableTypesEnum type;
           // public readonly string id;
           // public Line line;
           public Memento(GameObject drawable, DrawableType type)
            {
                this.drawable = drawable;
                this.type = type;
            }
            /*
            public Memento(GameObject gameObject, GameObject drawable, DrawableTypesEnum type, string id)
            {
                this.gameObject = gameObject;
                this.drawable = drawable;
                this.type = type;
                this.id = id;
            }*/
        }

        /// <summary>
        /// Destroys the drawn line.
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            foreach (Memento mem in mementoList)
            {
                string drawableParent = GameDrawableFinder.GetDrawableParentName(mem.drawable);
                if (mem.type is Line line)
                {
                    GameDrawer.ReDrawLine(mem.drawable, line);
                    new DrawOnNetAction(mem.drawable.name, drawableParent, line).Execute();
                }
                else if (mem.type is Text text)
                {
                    GameTexter.ReWriteText(mem.drawable, text);
                    new WriteTextNetAction(mem.drawable.name, drawableParent, text).Execute();
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
            base.Redo();
            foreach (Memento mem in mementoList)
            {
                GameObject toDelete = GameDrawableFinder.FindChild(mem.drawable, mem.type.id); ;
                new EraseNetAction(mem.drawable.name, GameDrawableFinder.GetDrawableParentName(mem.drawable), mem.type.id).Execute();
                Destroyer.Destroy(toDelete);
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
                    changedObjects.Add(mem.type.id);
                }
                return changedObjects;
            }
        }
    }
}