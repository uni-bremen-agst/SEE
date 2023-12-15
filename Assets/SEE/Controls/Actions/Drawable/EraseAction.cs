using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This class provides the action to erase (delete) a <see cref="DrawableType"/> object.
    /// </summary>
    class EraseAction : AbstractPlayerAction
    {
        /// <summary>
        /// A list of memento's for this action.
        /// It will be needed, because a memento saves one deleted drawable object.
        /// </summary>
        private List<Memento> mementoList = new();

        /// <summary>
        /// Saves all the information needed to revert or repeat this action for one drawable type object.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This class can store all the information needed to revert or repeat the <see cref="EraseAction"/>
        /// for one drawable type object.
        /// </summary>
        private class Memento
        {
            /// <summary>
            /// The drawable on that the drawable type is displayed.
            /// </summary>
            public readonly DrawableConfig drawable;
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
                this.drawable = DrawableConfigManager.GetDrawableConfig(drawable);
                this.drawableType = drawableType;
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.Erase"/>.
        /// It deletes one or more drawable object's.
        /// With the mouse button held down, more than one Drawable Type Object can be deleted. 
        /// Therefore, a list of mementos is needed.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                /// Block to get the drawable object to delete.
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) &&
                    Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                    GameFinder.hasDrawable(raycastHit.collider.gameObject))
                {
                    GameObject hittedObject = raycastHit.collider.gameObject;

                    if (Tags.DrawableTypes.Contains(hittedObject.tag))
                    {
                        memento = new Memento(GameFinder.GetDrawable(hittedObject), 
                            DrawableType.Get(hittedObject));
                        mementoList.Add(memento);

                        if (memento.drawableType is MindMapNodeConf conf)
                        {
                            DeleteChilds(hittedObject);
                        }

                        new EraseNetAction(memento.drawable.ID, memento.drawable.ParentID, 
                            hittedObject.name).Execute();
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
        /// This method is needed for Mind Map Nodes. 
        /// It deletes the child nodes and the branch lines to them.
        /// </summary>
        /// <param name="node">The parent node to be deleted.</param>
        /// <param name="delete">Recursive iteration variable, needed to include the child nodes in the Memento list.</param>
        private void DeleteChilds(GameObject node, bool delete = false)
        {
            GameObject drawable = GameFinder.GetDrawable(node);
            MMNodeValueHolder valueHolder = node.GetComponent<MMNodeValueHolder>();
            Dictionary<GameObject, GameObject> childs = valueHolder.GetChildren().
                ToDictionary(entry => entry.Key, entry => entry.Value);
            /// The following block deletes the parent branch line of the node.
            if (valueHolder.GetParentBranchLine() != null)
            {
                GameObject pBranchLine = valueHolder.GetParentBranchLine();
                memento = new Memento(drawable, DrawableType.Get(pBranchLine));
                mementoList.Add(memento);

                new EraseNetAction(memento.drawable.ID, memento.drawable.ParentID, 
                    pBranchLine.name).Execute();
                Destroyer.Destroy(pBranchLine);
            }
            /// Removes the node from the children list of the parent node.
            if (valueHolder.GetParent() != null)
            {
                valueHolder.GetParent().GetComponent<MMNodeValueHolder>().RemoveChild(node);
                new MindMapRemoveChildNetAction(memento.drawable.ID, memento.drawable.ParentID, 
                    MindMapNodeConf.GetNodeConf(node)).Execute();
            }

            /// Deletes children nodes recursively and iteratively by recursively calling the <see cref="DeleteChilds"/> function.
            foreach (KeyValuePair<GameObject, GameObject> pair in childs)
            {
                DeleteChilds(pair.Key, true);
            }

            /// Adds the node to the memento list and deletes them.
            /// Is only executed for child nodes in recursively calling.
            if (delete)
            {
                memento = new Memento(drawable, DrawableType.Get(node));
                mementoList.Add(memento);

                new EraseNetAction(memento.drawable.ID, memento.drawable.ParentID, node.name).Execute();
                Destroyer.Destroy(node);
            }
        }

        /// <summary>
        /// Reverts this action, i.e., it recovers the deletes drawable object's.
        /// However, the Memento list is sorted twice before that. 
        /// First, it is sorted by the types of the Drawable Type Object, 
        /// and then the Mind Map Nodes are sorted based on the node layer indices. 
        /// This is because parent nodes must be created before child nodes to avoid errors.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            mementoList = mementoList.OrderBy(m => DrawableType.OrderOnType(m.drawableType)).
                ThenBy(m => DrawableType.OrderMindMap(m.drawableType)).ToList();
            foreach (Memento mem in mementoList)
            {
                GameObject drawable = mem.drawable.GetDrawable();
                DrawableType.Restore(mem.drawableType, drawable);
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
                GameObject toDelete = GameFinder.FindChild(mem.drawable.GetDrawable(), mem.drawableType.id);
                if (mem.drawableType is MindMapNodeConf conf)
                {
                    MMNodeValueHolder valueHolder = toDelete.GetComponent<MMNodeValueHolder>();
                    if (valueHolder.GetParent() != null)
                    {
                        valueHolder.GetParent().GetComponent<MMNodeValueHolder>().RemoveChild(toDelete);
                        new MindMapRemoveChildNetAction(memento.drawable.ID, memento.drawable.ParentID, 
                            MindMapNodeConf.GetNodeConf(toDelete)).Execute();
                    }
                }
                new EraseNetAction(mem.drawable.ID, mem.drawable.ParentID, mem.drawableType.id).Execute();
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
                changedObjects.Add(memento.drawable.ID);
                foreach (Memento mem in mementoList)
                {
                    changedObjects.Add(mem.drawableType.id);
                }
                return changedObjects;
            }
        }
    }
}
