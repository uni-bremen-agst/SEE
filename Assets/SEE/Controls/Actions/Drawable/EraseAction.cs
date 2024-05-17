using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using SEE.Utils.History;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This class provides the action to erase (delete) a <see cref="DrawableType"/> object.
    /// </summary>
    class EraseAction : DrawableAction
    {
        /// <summary>
        /// A list of mementos for this action.
        /// It will be needed, because a memento saves one deleted drawable object.
        /// </summary>
        private List<Memento> mementoList = new();

        /// <summary>
        /// Saves all the information needed to revert or repeat this action for one
        /// drawable type object.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This class can store all the information needed to revert or repeat the <see cref="EraseAction"/>
        /// for one drawable type object.
        /// </summary>
        private class Memento
        {
            /// <summary>
            /// The drawable on which the drawable type is displayed.
            /// </summary>
            public readonly DrawableConfig Drawable;
            /// <summary>
            /// The drawable type object.
            /// </summary>
            public DrawableType DrawableType;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="drawable">The drawable on which the drawable type is displayed</param>
            /// <param name="drawableType">The drawable type of the deleted object</param>
            public Memento(GameObject drawable, DrawableType drawableType)
            {
                Drawable = DrawableConfigManager.GetDrawableConfig(drawable);
                DrawableType = drawableType;
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.Erase"/>.
        /// It deletes one or more drawable objects.
        /// With the mouse button held down, more than one Drawable Type Object can be deleted.
        /// Therefore, a list of mementos is needed.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                /// Block to get the drawable object to delete.
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) &&
                    Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                    GameFinder.HasDrawable(raycastHit.collider.gameObject))
                {
                    GameObject hittedObject = raycastHit.collider.gameObject;

                    if (Tags.DrawableTypes.Contains(hittedObject.tag))
                    {
                        memento = new Memento(GameFinder.GetDrawable(hittedObject),
                            DrawableType.Get(hittedObject));
                        mementoList.Add(memento);

                        if (memento.DrawableType is MindMapNodeConf conf)
                        {
                            DeleteChildren(hittedObject);
                        }

                        new EraseNetAction(memento.Drawable.ID, memento.Drawable.ParentID,
                            hittedObject.name).Execute();
                        Destroyer.Destroy(hittedObject);
                    }
                }
                /// Completes this action run.
                if (Input.GetMouseButtonUp(0))
                {
                    CurrentState = IReversibleAction.Progress.Completed;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// This method is needed for Mind Map Nodes.
        /// It deletes the child nodes and the branch lines associated with them.
        /// </summary>
        /// <param name="node">The parent node to be deleted.</param>
        /// <param name="delete">Recursive iteration variable, needed to include the child nodes in the memento list.</param>
        private void DeleteChildren(GameObject node, bool delete = false)
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

                new EraseNetAction(memento.Drawable.ID, memento.Drawable.ParentID,
                    pBranchLine.name).Execute();
                Destroyer.Destroy(pBranchLine);
            }
            /// Removes the node from the children list of the parent node.
            if (valueHolder.GetParent() != null)
            {
                valueHolder.GetParent().GetComponent<MMNodeValueHolder>().RemoveChild(node);
                new MindMapRemoveChildNetAction(memento.Drawable.ID, memento.Drawable.ParentID,
                    MindMapNodeConf.GetNodeConf(node)).Execute();
            }

            /// Deletes children nodes recursively and iteratively by recursively
            /// calling the <see cref="DeleteChildren"/> function.
            foreach (var pair in childs)
            {
                DeleteChildren(pair.Key, true);
            }

            /// Adds the node to the memento list and deletes them.
            /// Is only executed for child nodes in recursively calling.
            if (delete)
            {
                memento = new Memento(drawable, DrawableType.Get(node));
                mementoList.Add(memento);

                new EraseNetAction(memento.Drawable.ID, memento.Drawable.ParentID, node.name).Execute();
                Destroyer.Destroy(node);
            }
        }

        /// <summary>
        /// Reverts this action, i.e., it recovers the deleted drawable objects.
        /// However, the Memento list is sorted twice before that.
        /// First, it is sorted by the types of the Drawable Type Object,
        /// and then the Mind Map Nodes are sorted based on the node layer indices.
        /// This is because parent nodes must be created before child nodes to avoid errors.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            mementoList = mementoList.OrderBy(m => DrawableType.OrderOnType(m.DrawableType)).
                ThenBy(m => DrawableType.OrderMindMap(m.DrawableType)).ToList();
            foreach (Memento mem in mementoList)
            {
                GameObject drawable = mem.Drawable.GetDrawable();
                DrawableType.Restore(mem.DrawableType, drawable);
            }
        }

        /// <summary>
        /// Repeats this action, i.e., it deletes again the chosen drawable objects.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            foreach (Memento mem in mementoList)
            {
                GameObject toDelete = GameFinder.FindChild(mem.Drawable.GetDrawable(), mem.DrawableType.Id);
                if (mem.DrawableType is MindMapNodeConf conf)
                {
                    MMNodeValueHolder valueHolder = toDelete.GetComponent<MMNodeValueHolder>();
                    if (valueHolder.GetParent() != null)
                    {
                        valueHolder.GetParent().GetComponent<MMNodeValueHolder>().RemoveChild(toDelete);
                        new MindMapRemoveChildNetAction(memento.Drawable.ID, memento.Drawable.ParentID,
                            MindMapNodeConf.GetNodeConf(toDelete)).Execute();
                    }
                }
                new EraseNetAction(mem.Drawable.ID, mem.Drawable.ParentID, mem.DrawableType.Id).Execute();
                Destroyer.Destroy(toDelete);
            }
        }

        /// <summary>
        /// A new instance of <see cref="EraseAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EraseAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new EraseAction();
        }

        /// <summary>
        /// A new instance of <see cref="EraseAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EraseAction"/></returns>
        public override IReversibleAction NewInstance()
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
        /// </summary>
        /// <returns>the id's of the deletes object's</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento == null || memento.Drawable == null)
            {
                return new();
            }
            else
            {
                HashSet<string> changedObjects = new()
                {
                    memento.Drawable.ID
                };
                foreach (Memento mem in mementoList)
                {
                    changedObjects.Add(mem.DrawableType.Id);
                }
                return changedObjects;
            }
        }
    }
}
