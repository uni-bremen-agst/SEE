using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.Editing;
using SEE.Game.Drawable.MindMap;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using SEE.UI.Menu.Drawable.MindMap;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable.CutCopyPaste
{
    /// <summary>
    /// Handles creating a pasted mind-map subtree and integrating its root node
    /// into the target mind map.
    /// </summary>
    internal sealed class CutCopyPasteMindMapPasteOperation
    {
        /// <summary>
        /// The current state of the parent selection.
        /// </summary>
        internal enum ParentSelectionResult
        {
            InProgress,
            Completed,
            Reset
        }

        /// <summary>
        /// Contains the current result of the mind-map paste operation.
        /// </summary>
        internal readonly struct Result
        {
            /// <summary>
            /// The root object of the pasted subtree.
            /// </summary>
            internal readonly GameObject NewObject;

            /// <summary>
            /// The configuration of the pasted root node.
            /// </summary>
            internal readonly MindMapNodeConf NewValueHolder;

            /// <summary>
            /// The configuration containing the pasted subtree and its branch lines.
            /// </summary>
            internal readonly DrawableConfig NewNodesHolder;

            /// <summary>
            /// The original configuration of the branch line to the parent, if one existed.
            /// </summary>
            internal readonly LineConf OldBranchLineConfig;

            /// <summary>
            /// Creates a result for the current mind-map paste operation.
            /// </summary>
            /// <param name="newObject">The root object of the pasted subtree.</param>
            /// <param name="newValueHolder">The configuration of the pasted root node.</param>
            /// <param name="newNodesHolder">The configuration of the pasted subtree.</param>
            /// <param name="oldBranchLineConfig">
            /// The original configuration of the branch line to the parent.
            /// </param>
            internal Result(GameObject newObject, MindMapNodeConf newValueHolder,
                DrawableConfig newNodesHolder, LineConf oldBranchLineConfig)
            {
                NewObject = newObject;
                NewValueHolder = newValueHolder;
                NewNodesHolder = newNodesHolder;
                OldBranchLineConfig = oldBranchLineConfig;
            }
        }

        /// <summary>
        /// The original surface.
        /// </summary>
        private GameObject oldSurface;

        /// <summary>
        /// The target surface.
        /// </summary>
        private GameObject newSurface;

        /// <summary>
        /// The original drawable configuration.
        /// </summary>
        private DrawableType oldValueHolder;

        /// <summary>
        /// The original mind-map subtree configuration.
        /// </summary>
        private DrawableConfig oldNodesHolder;

        /// <summary>
        /// The current pasted mind-map subtree configuration.
        /// </summary>
        private DrawableConfig newNodesHolder;

        /// <summary>
        /// The pasted root object.
        /// </summary>
        private GameObject newObject;

        /// <summary>
        /// The configuration of the pasted root node.
        /// </summary>
        private MindMapNodeConf newValueHolder;

        /// <summary>
        /// The original branch-line configuration.
        /// </summary>
        private LineConf oldBranchLineConfig;

        /// <summary>
        /// Whether the original object was cut.
        /// </summary>
        private bool wasCut;

        /// <summary>
        /// Whether the original branch-line appearance has already been applied
        /// while selecting a new parent.
        /// </summary>
        private bool editToOldBranchLine;

        /// <summary>
        /// The current result of this operation.
        /// </summary>
        internal Result CurrentResult => new(
            newObject,
            newValueHolder,
            newNodesHolder,
            oldBranchLineConfig);

        /// <summary>
        /// Creates and positions a copy of the selected mind-map subtree.
        /// </summary>
        /// <param name="selectedObject">The selected root node.</param>
        /// <param name="oldSurface">The original drawable surface.</param>
        /// <param name="oldValueHolder">The original root-node configuration.</param>
        /// <param name="oldNodesHolder">The original subtree configuration.</param>
        /// <param name="nodesToPaste">The subtree configuration to paste.</param>
        /// <param name="newSurface">The target drawable surface.</param>
        /// <param name="newPosition">The requested world position.</param>
        /// <param name="wasCut">Whether the original subtree is being cut.</param>
        internal void Paste(GameObject selectedObject, GameObject oldSurface,
            DrawableType oldValueHolder, DrawableConfig oldNodesHolder,
            DrawableConfig nodesToPaste, GameObject newSurface,
            Vector3 newPosition, bool wasCut)
        {
            this.oldSurface = oldSurface;
            this.newSurface = newSurface;
            this.oldValueHolder = oldValueHolder;
            this.oldNodesHolder = oldNodesHolder;
            this.wasCut = wasCut;

            oldBranchLineConfig = null;
            editToOldBranchLine = false;

            MMNodeValueHolder selectedValueHolder =
                selectedObject.GetComponent<MMNodeValueHolder>();

            if (selectedValueHolder.GetParentBranchLine() != null)
            {
                oldBranchLineConfig =
                    LineConf.GetLine(selectedValueHolder.GetParentBranchLine());
            }

            nodesToPaste.MindMapNodeConfigs[0].BranchLineToParent = "";
            nodesToPaste.MindMapNodeConfigs[0].ParentNode = "";

            GameMindMap.RenameMindMap(
                nodesToPaste,
                GameFinder.GetAttachedObjectsObject(newSurface));

            int currentPage =
                newSurface.GetComponent<DrawableHolder>().CurrentPage;

            foreach (DrawableType type in nodesToPaste.GetAllDrawableTypes())
            {
                type.AssociatedPage = currentPage;
                DrawableType.Restore(type, newSurface);
            }

            newObject = GameFinder.FindAttachedOrLocalDescendant(
                newSurface,
                nodesToPaste.MindMapNodeConfigs[0].ID);

            CutCopyPastePositioning.MoveToWorldPosition(
                selectedObject,
                newObject,
                newSurface,
                newPosition);

            newNodesHolder =
                GameMindMapHierarchy.SummarizeSelectedNodeIncChildren(newObject);

            newValueHolder = MindMapNodeConf.GetNodeConf(newObject);
        }

        /// <summary>
        /// Opens the parent-selection menu for the pasted root node.
        /// </summary>
        /// <returns>Whether the parent-selection menu could be opened.</returns>
        internal bool TryOpenParentSelection()
        {
            GameObject attachedObjects =
                GameFinder.GetAttachedObjectsObject(newSurface);

            if (attachedObjects == null)
            {
                return false;
            }

            MindMapParentSelectionMenu.EnableForEditing(
                attachedObjects,
                newObject,
                newValueHolder,
                null,
                true);

            return true;
        }

        /// <summary>
        /// Updates the parent selection for the pasted root node.
        /// </summary>
        /// <returns>The current parent-selection state.</returns>
        internal ParentSelectionResult UpdateParentSelection()
        {
            if (MindMapParentSelectionMenu.TryGetParent(out GameObject parent))
            {
                MindMapParentSelectionMenu.Instance.Destroy();

                newValueHolder.ParentNode = parent.name;

                GameObject branchLineToParent =
                    parent.GetComponent<MMNodeValueHolder>()
                        .GetChildren()[newObject];

                newValueHolder.BranchLineToParent =
                    branchLineToParent.name;

                if (oldBranchLineConfig != null)
                {
                    ApplyOldBranchLineAppearance(branchLineToParent);
                }

                newNodesHolder =
                    GameMindMapHierarchy.SummarizeSelectedNodeIncChildren(newObject);

                return ParentSelectionResult.Completed;
            }

            if (!MindMapParentSelectionMenu.Instance.IsOpen())
            {
                RollBackPaste();
                return ParentSelectionResult.Reset;
            }

            if (oldBranchLineConfig != null && !editToOldBranchLine)
            {
                GameObject branchLineToParent =
                    newObject.GetComponent<MMNodeValueHolder>()
                        .GetParentBranchLine();

                ApplyOldBranchLineAppearance(branchLineToParent);
                editToOldBranchLine = true;
            }

            return ParentSelectionResult.InProgress;
        }

        /// <summary>
        /// Applies the original branch-line appearance to the given branch line.
        /// </summary>
        /// <param name="branchLineToParent">The branch line to update.</param>
        private void ApplyOldBranchLineAppearance(GameObject branchLineToParent)
        {
            GameLineEdit.ChangeLine(
                branchLineToParent,
                oldBranchLineConfig);

            new EditLineNetAction(
                newSurface.name,
                GameFinder.GetDrawableSurfaceParentName(newSurface),
                LineConf.GetLineWithoutRenderPos(branchLineToParent)).Execute();
        }

        /// <summary>
        /// Cancels the current mind-map paste operation, closes the parent-selection menu,
        /// and restores the state from before the paste.
        /// </summary>
        internal void Cancel()
        {
            if (MindMapParentSelectionMenu.Instance.IsOpen())
            {
                MindMapParentSelectionMenu.Instance.Destroy();
            }

            RollBackPaste();
        }

        /// <summary>
        /// Removes the pasted root node from its temporary parent relationship
        /// and deletes the corresponding parent branch line.
        /// </summary>
        private void DetachPastedRootFromParent()
        {
            if (newObject == null)
            {
                return;
            }

            MMNodeValueHolder valueHolder =
                newObject.GetComponent<MMNodeValueHolder>();

            if (valueHolder == null)
            {
                return;
            }

            GameObject parent = valueHolder.GetParent();

            if (parent != null)
            {
                parent.GetComponent<MMNodeValueHolder>().RemoveChild(newObject);

                new MindMapRemoveChildNetAction(
                    newSurface.name,
                    GameFinder.GetDrawableSurfaceParentName(newSurface),
                    MindMapNodeConf.GetNodeConf(newObject)).Execute();
            }

            GameObject branchLineToParent =
                valueHolder.GetParentBranchLine();

            if (branchLineToParent != null)
            {
                new EraseNetAction(
                    newSurface.name,
                    GameFinder.GetDrawableSurfaceParentName(newSurface),
                    branchLineToParent.name).Execute();

                Destroyer.Destroy(branchLineToParent);
            }

            valueHolder.SetParent(null, null);
        }

        /// <summary>
        /// Reverts the pasted subtree and restores the original subtree if it was cut.
        /// </summary>
        private void RollBackPaste()
        {
            DetachPastedRootFromParent();

            foreach (DrawableType type in newNodesHolder.GetAllDrawableTypes())
            {
                GameObject typeObject =
                    GameFinder.FindAttachedOrLocalDescendant(
                        newSurface,
                        type.ID);

                new EraseNetAction(
                    newSurface.name,
                    GameFinder.GetDrawableSurfaceParentName(newSurface),
                    typeObject.name).Execute();

                Destroyer.Destroy(typeObject);
            }

            if (!wasCut)
            {
                return;
            }

            foreach (DrawableType type in oldNodesHolder.GetAllDrawableTypes())
            {
                DrawableType.Restore(type, oldSurface);
            }

            if (oldBranchLineConfig == null)
            {
                return;
            }

            GameObject oldObject =
                GameFinder.FindAttachedOrLocalDescendant(
                    oldSurface,
                    oldValueHolder.ID);

            GameObject branchLineToParent =
                oldObject.GetComponent<MMNodeValueHolder>()
                    .GetParentBranchLine();

            GameLineEdit.ChangeLine(
                branchLineToParent,
                oldBranchLineConfig);

            new EditLineNetAction(
                oldSurface.name,
                GameFinder.GetDrawableSurfaceParentName(oldSurface),
                LineConf.GetLineWithoutRenderPos(branchLineToParent)).Execute();
        }
    }
}
