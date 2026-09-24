using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.MindMap;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.Drawable.Editing
{
    /// <summary>
    /// Provides editing operations for Mind Map nodes.
    /// </summary>
    internal static class GameMindMapEdit
    {
        /// <summary>
        /// Changes the kind of the given Mind Map node if the requested transition
        /// is structurally valid.
        /// </summary>
        /// <param name="node">The node whose kind should be changed.</param>
        /// <param name="newNodeKind">The new node kind.</param>
        /// <param name="borderConf">
        /// The previous border configuration that should be preserved when applicable.
        /// </param>
        /// <returns>The resulting node kind.</returns>
        internal static MindMapNodeKind ChangeNodeKind(
            GameObject node,
            MindMapNodeKind newNodeKind,
            LineConf borderConf = null)
        {
            MMNodeValueHolder nodeValueHolder =
                node.GetComponent<MMNodeValueHolder>();

            if (nodeValueHolder.NodeKind != newNodeKind
                && GameMindMapHierarchy.CheckValidNodeKindChange(
                    node,
                    newNodeKind,
                    nodeValueHolder.NodeKind))
            {
                if (newNodeKind == MindMapNodeKind.Theme)
                {
                    // Themes cannot have a parent.
                    if (nodeValueHolder.GetParent() != null)
                    {
                        nodeValueHolder.GetParent()
                            .GetComponent<MMNodeValueHolder>()
                            .RemoveChild(node);
                    }

                    Destroyer.Destroy(
                        nodeValueHolder.GetParentBranchLine());

                    nodeValueHolder.SetParent(null, null);
                }

                GameMindMapNode.ApplyNodeKindAppearance(
                    node,
                    newNodeKind,
                    borderConf);

                nodeValueHolder.NodeKind = newNodeKind;
                GameMindMapBranch.ReDrawBranchLines(node);
            }

            return nodeValueHolder.NodeKind;
        }

        /// <summary>
        /// Changes the parent of the given Mind Map node.
        /// The previous parent relationship and branch line are removed,
        /// a new branch line is created, and the previous branch-line appearance
        /// is restored if a corresponding configuration exists.
        /// </summary>
        /// <param name="child">The Mind Map node whose parent should be changed.</param>
        /// <param name="parent">The new parent Mind Map node.</param>
        internal static void ChangeParent(
            GameObject child,
            GameObject parent)
        {
            if (!child.CompareTag(Tags.MindMapNode)
                || parent == null
                || !parent.CompareTag(Tags.MindMapNode))
            {
                return;
            }

            MMNodeValueHolder childValueHolder =
                child.GetComponent<MMNodeValueHolder>();

            if (childValueHolder.GetParent() == parent
                || !GameMindMapHierarchy.ParentChangeIsValid(
                    child,
                    parent))
            {
                return;
            }

            if (childValueHolder.GetParent() != null)
            {
                childValueHolder.GetParent()
                    .GetComponent<MMNodeValueHolder>()
                    .RemoveChild(child);
            }

            LineConf oldBranchLine = null;

            if (childValueHolder.GetParentBranchLine() != null)
            {
                oldBranchLine =
                    LineConf.GetLine(
                        childValueHolder.GetParentBranchLine());
            }

            Destroyer.Destroy(
                childValueHolder.GetParentBranchLine());

            GameObject newBranchLine =
                GameMindMapBranch.CreateBranchLine(
                    child,
                    parent);

            if (oldBranchLine != null)
            {
                GameLineEdit.ChangeLine(
                    newBranchLine,
                    oldBranchLine);
            }
        }

        /// <summary>
        /// Changes all editable values of a Mind Map node.
        /// </summary>
        /// <param name="node">The node whose values should be changed.</param>
        /// <param name="conf">Contains the new values.</param>
        internal static void ChangeMindMapNode(GameObject node, MindMapNodeConf conf)
        {
            if (node.CompareTag(Tags.MindMapNode))
            {
                ChangeNodeKind(
                    node,
                    conf.NodeKind,
                    conf.BorderConf);

                GameLineEdit.ChangeLine(
                    node.FindDescendantWithTag(Tags.Line),
                    conf.BorderConf);

                GameTextEdit.ChangeText(
                    node.FindDescendantWithTag(Tags.DText),
                    conf.TextConf);

                GameObject attachedObjects =
                    GameFinder.GetAttachedObjectsObject(
                        GameFinder.GetDrawableSurface(node));

                GameObject parent =
                    GameFinder.FindAttachedOrLocalDescendant(
                        attachedObjects,
                        conf.ParentNode);

                ChangeParent(node, parent);

                GameMindMapNode.ChangeBoxSize(node);

                node.FindDescendantWithTag(Tags.Line)
                    .GetComponent<MeshCollider>().enabled = false;

                node.FindDescendantWithTag(Tags.DText)
                    .GetComponent<MeshCollider>().enabled = false;

                if (conf.BranchLineToParent != "")
                {
                    GameObject branch =
                        GameFinder.FindAttachedOrLocalDescendant(
                            attachedObjects,
                            conf.BranchLineToParent);

                    GameLineEdit.ChangeLine(
                        branch,
                        conf.BranchLineConf);

                    branch.GetComponent<MeshCollider>().enabled = false;
                }

                GameLayerChanger.SetOrderInLayer(
                    node,
                    conf.OrderInLayer);
            }
        }
    }
}
