using Assets.SEE.Game;
using Assets.SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Game;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using SEE.Game.Drawable.Configurations;
using System.Linq;

namespace SEE.Controls.Actions.Drawable
{
    class EraseAction : AbstractPlayerAction
    {
        /// <summary>
        /// A list of memento's for this action.
        /// It will be needed, because a memento saves one deleted drawable object.
        /// </summary>
        private List<Memento> mementoList = new List<Memento>();

        /// <summary>
        /// Saves all the information needed to revert or repeat this action on one drawable object.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This class can store all the information needed to revert or repeat a <see cref="EraseAction"/>.
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
            public DrawableType drawableType;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="drawable">The drawable on that the drawable type is displayed</param>
            /// <param name="drawableType">The drawable type of the deleted object</param>
            public Memento(GameObject drawable, DrawableType drawableType)
            {
                this.drawable = drawable;
                this.drawableType = drawableType;
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.Erase"/>.
        /// It deletes one or more drawable object's.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                /// Block to get the drawable object to delete.
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) &&
                    Raycasting.RaycastAnythingBackface(out RaycastHit raycastHit) &&
                    GameDrawableFinder.hasDrawable(raycastHit.collider.gameObject))
                {
                    GameObject hittedObject = raycastHit.collider.gameObject;

                    if (Tags.DrawableTypes.Contains(hittedObject.tag))
                    {
                        memento = new Memento(GameDrawableFinder.FindDrawable(hittedObject), new DrawableType().Get(hittedObject));
                        mementoList.Add(memento);

                        new EraseNetAction(memento.drawable.name, memento.drawable.transform.parent.name, hittedObject.name).Execute();
                        Destroyer.Destroy(hittedObject);
                    }
                }
                /// Completes this action run.
                if (Input.GetMouseButtonUp(0))
                {
                    currentState = ReversibleAction.Progress.Completed;
                    return true;
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// Reverts this action, i.e., it recovers the deletes drawable object's.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            foreach (Memento mem in mementoList)
            {
                string drawableParent = GameDrawableFinder.GetDrawableParentName(mem.drawable);
                if (mem.drawableType is Line line)
                {
                    GameDrawer.ReDrawLine(mem.drawable, line);
                    new DrawOnNetAction(mem.drawable.name, drawableParent, line).Execute();
                }
                else if (mem.drawableType is Text text)
                {
                    GameTexter.ReWriteText(mem.drawable, text);
                    new WriteTextNetAction(mem.drawable.name, drawableParent, text).Execute();
                }
            }
        }

        /// <summary>
        /// Repeats this action, i.e., it deletes again the chosen drawable object's.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            foreach (Memento mem in mementoList)
            {
                GameObject toDelete = GameDrawableFinder.FindChild(mem.drawable, mem.drawableType.id); ;
                new EraseNetAction(mem.drawable.name, GameDrawableFinder.GetDrawableParentName(mem.drawable), mem.drawableType.id).Execute();
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
            return new EraseAction();
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
        /// <returns><see cref="ActionStateType.Erase"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Erase;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// Because this action does not actually change any game object, 
        /// an empty set is always returned.
        /// </summary>
        /// <returns>the id's of the deletes object's</returns>
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
                    changedObjects.Add(mem.drawableType.id);
                }
                return changedObjects;
            }
        }
    }
}
