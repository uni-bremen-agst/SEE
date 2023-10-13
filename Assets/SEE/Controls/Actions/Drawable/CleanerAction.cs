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
        /// <summary>
        /// A list of memento's for this action.
        /// It will be needed, because a memento saves one deleted drawable type.
        /// </summary>
        private List<Memento> mementoList = new();

        /// <summary>
        /// Saves all the information needed to revert or repeat this action on one drawable type.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This class can store all the information needed to revert or repeat a <see cref="CleanerAction"/>.
        /// </summary>
        private class Memento
        {
            /// <summary>
            /// The drawable on that the drawable type is displayed.
            /// </summary>
            public readonly GameObject drawable;
            /// <summary>
            /// The drawable type object.
            /// </summary>
            public readonly DrawableType type;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="drawable">The drawable on that the drawable type is displayed</param>
            /// <param name="drawableType">The drawable type of the deleted object</param>
            public Memento(GameObject drawable, DrawableType type)
            {
                this.drawable = drawable;
                this.type = type;
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.Cleaner"/>.
        /// It cleans a complete drawable. In other words: it's deletes all drawable types that are on a selected drawable.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
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
                        return DeleteDrawableChilds(hittedObject);
                    } else if (GameDrawableFinder.hasDrawable(hittedObject))
                    {
                        return DeleteDrawableChilds(GameDrawableFinder.FindDrawable(hittedObject));
                    }
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// This method finds and deletes all drawable types that are placed on the given drawable.
        /// </summary>
        /// <param name="drawable">is the drawable which should be cleaned.</param>
        /// <returns>true if the drawable was successfull cleaned, false if the drawable was already cleaned</returns>
        private bool DeleteDrawableChilds(GameObject drawable)
        {
            Transform[] allChildren = GameDrawableFinder.GetAttachedObjectsObject(drawable).GetComponentsInChildren<Transform>();
            /// A cleaned drawable has only one transform (AttachedObject-Transform itself)
            if (allChildren.Length == 1)
            {
                return false;
            }
            foreach (Transform childsTransform in allChildren)
            {
                GameObject child = childsTransform.gameObject;
                if (Tags.DrawableTypes.Contains(child.tag))
                {
                    memento = new Memento(drawable, new DrawableType().Get(child));
                    mementoList.Add(memento);

                    new EraseNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), memento.type.id).Execute();
                    Destroyer.Destroy(child);
                }
            }
            currentState = ReversibleAction.Progress.Completed;
            return true;
        }

        /// <summary>
        /// Reverts this action, i.e., restores the deleted drawable types of the selected drawable.
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
        /// Repeats this action, i.e., deletes again all the drawable types that are placed on the selected drawable.
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
        /// A new instance of <see cref="CleanerAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="CleanerAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new CleanerAction();
        }

        /// <summary>
        /// A new instance of <see cref="CleanerAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="CleanerAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Cleaner"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Cleaner;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// Because this action does not actually change any game object, 
        /// an empty set is always returned.
        /// </summary>
        /// <returns>id's of the deletes drawable types</returns>
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