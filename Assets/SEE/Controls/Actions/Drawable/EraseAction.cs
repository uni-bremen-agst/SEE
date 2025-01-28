using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
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
        /// It will be needed because a memento saves one deleted drawable object.
        /// </summary>
        private List<Memento> mementoList = new();

        /// <summary>
        /// This class can store all the information needed to revert or repeat
        /// the <see cref="EraseAction"/> for one drawable type object.
        /// </summary>
        private class Memento
        {
            /// <summary>
            /// The drawable surface on which the drawable type is displayed.
            /// </summary>
            public readonly DrawableConfig Surface;
            /// <summary>
            /// The drawable type object.
            /// </summary>
            public DrawableType DrawableType;

            /// <summary>
            /// The constructor setting its fields.
            /// </summary>
            /// <param name="surface">The drawable surface on which the drawable type is displayed</param>
            /// <param name="drawableType">The drawable type of the deleted object</param>
            public Memento(GameObject surface, DrawableType drawableType)
            {
                Surface = DrawableConfigManager.GetDrawableConfig(surface);
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
                if (Selector.SelectQueryHasDrawableSurface(out RaycastHit raycastHit))
                {
                    GameObject hitObject = GameFinder.GetDrawableTypObject(raycastHit.collider.gameObject);

                    if (Tags.DrawableTypes.Contains(hitObject.tag))
                    {
                        mementoList.Add(new Memento(GameFinder.GetDrawableSurface(hitObject),
                            DrawableType.Get(hitObject)));

                        if (mementoList.Last().DrawableType is MindMapNodeConf)
                        {
                            DeleteChildren(hitObject);
                        }

                        new EraseNetAction(mementoList.Last().Surface.ID,
                            mementoList.Last().Surface.ParentID,
                            hitObject.name).Execute();
                        Destroyer.Destroy(hitObject);
                    }
                }
                /// Completes this action run.
                if (SEEInput.MouseUp(MouseButton.Left))
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
        /// <param name="delete">Recursive iteration variable, needed to include the child nodes in
        /// the memento list.</param>
        private void DeleteChildren(GameObject node, bool delete = false)
        {
            GameObject surface = GameFinder.GetDrawableSurface(node);
            MMNodeValueHolder valueHolder = node.GetComponent<MMNodeValueHolder>();
            Dictionary<GameObject, GameObject> childs = valueHolder.GetChildren().
                ToDictionary(entry => entry.Key, entry => entry.Value);
            /// The following block deletes the parent branch line of the node.
            if (valueHolder.GetParentBranchLine() != null)
            {
                GameObject pBranchLine = valueHolder.GetParentBranchLine();
                mementoList.Add(new Memento(surface, DrawableType.Get(pBranchLine)));

                new EraseNetAction(mementoList.Last().Surface.ID, mementoList.Last().Surface.ParentID,
                    pBranchLine.name).Execute();
                Destroyer.Destroy(pBranchLine);
            }
            /// Removes the node from the children list of the parent node.
            if (valueHolder.GetParent() != null)
            {
                valueHolder.GetParent().GetComponent<MMNodeValueHolder>().RemoveChild(node);
                new MindMapRemoveChildNetAction(mementoList.Last().Surface.ID,
                    mementoList.Last().Surface.ParentID,
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
                mementoList.Add(new Memento(surface, DrawableType.Get(node)));

                new EraseNetAction(mementoList.Last().Surface.ID,
                    mementoList.Last().Surface.ParentID, node.name).Execute();
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
                GameObject surface = mem.Surface.GetDrawableSurface();
                DrawableType.Restore(mem.DrawableType, surface);
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
                GameObject toDelete = GameFinder.FindChild(mem.Surface.GetDrawableSurface(), mem.DrawableType.Id);
                if (mem.DrawableType is MindMapNodeConf conf)
                {
                    MMNodeValueHolder valueHolder = toDelete.GetComponent<MMNodeValueHolder>();
                    if (valueHolder.GetParent() != null)
                    {
                        valueHolder.GetParent().GetComponent<MMNodeValueHolder>().RemoveChild(toDelete);
                        new MindMapRemoveChildNetAction(mem.Surface.ID, mem.Surface.ParentID,
                            MindMapNodeConf.GetNodeConf(toDelete)).Execute();
                    }
                }
                new EraseNetAction(mem.Surface.ID, mem.Surface.ParentID, mem.DrawableType.Id).Execute();
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
            if (mementoList.Count == 0)
            {
                return new();
            }
            else
            {
                HashSet<string> changedObjects = new();
                foreach (Memento mem in mementoList)
                {
                    changedObjects.Add(mem.Surface.ID);
                    changedObjects.Add(mem.DrawableType.Id);
                }
                return changedObjects;
            }
        }
    }
}
