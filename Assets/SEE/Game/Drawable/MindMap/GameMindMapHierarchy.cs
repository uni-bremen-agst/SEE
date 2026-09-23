using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Drawable.MindMap
{
    /// <summary>
    /// Provides hierarchy-related operations for Mind Map nodes.
    /// This includes layer propagation, parent-change validation,
    /// node-kind transition validation, and subtree summarization.
    /// </summary>
    public static class GameMindMapHierarchy
    {
        /// <summary>
        /// Recursively updates the layer of the given Mind Map node and all of its descendants.
        /// The layer of <paramref name="holder"/> is set to the layer of
        /// <paramref name="parent"/> plus one.
        /// </summary>
        /// <param name="holder">
        /// The value holder of the node whose layer should be updated.
        /// </param>
        /// <param name="parent">
        /// The value holder of the parent node from which the new layer is derived.
        /// </param>
        internal static void UpdateMindMapLayer(
            MMNodeValueHolder holder,
            MMNodeValueHolder parent)
        {
            int oldLayer = holder.Layer;
            holder.Layer = parent.Layer + 1;

            if (holder.Layer != oldLayer && holder.GetChildren().Count > 0)
            {
                foreach (GameObject child in holder.GetChildren().Keys)
                {
                    MMNodeValueHolder childHolder =
                        child.GetComponent<MMNodeValueHolder>();

                    UpdateMindMapLayer(childHolder, holder);
                }
            }
        }

        /// <summary>
        /// Checks whether <paramref name="parent"/> can become the parent of
        /// <paramref name="child"/> without creating a cycle in the Mind Map hierarchy.
        /// </summary>
        /// <param name="child">
        /// The Mind Map node whose parent should be changed.
        /// </param>
        /// <param name="parent">
        /// The proposed new parent node.
        /// </param>
        /// <param name="result">
        /// The accumulated result used during the recursive traversal.
        /// Callers normally use the default value.
        /// </param>
        /// <returns>
        /// True if both objects are Mind Map nodes and assigning
        /// <paramref name="parent"/> would not create a cycle; otherwise false.
        /// </returns>
        public static bool ParentChangeIsValid(
            GameObject child,
            GameObject parent,
            bool result = true)
        {
            if (child.CompareTag(Tags.MindMapNode)
                && parent.CompareTag(Tags.MindMapNode))
            {
                MMNodeValueHolder valueHolder =
                    child.GetComponent<MMNodeValueHolder>();

                if (child == parent)
                {
                    result = false;
                }

                foreach (KeyValuePair<GameObject, GameObject> pair
                         in valueHolder.GetChildren())
                {
                    result = result
                        && ParentChangeIsValid(
                            pair.Key,
                            parent,
                            result);
                }

                return result;
            }

            return false;
        }

        /// <summary>
        /// Checks whether a Mind Map node may be changed from
        /// <paramref name="oldNodeKind"/> to <paramref name="newNodeKind"/>
        /// according to the structural rules of the Mind Map hierarchy.
        /// </summary>
        /// <param name="node">
        /// The Mind Map node whose kind should be changed.
        /// </param>
        /// <param name="newNodeKind">
        /// The requested new node kind.
        /// </param>
        /// <param name="oldNodeKind">
        /// The current node kind.
        /// </param>
        /// <returns>
        /// True if the requested node-kind transition is structurally valid;
        /// otherwise false.
        /// </returns>
        /// <remarks>
        /// A leaf may be changed to any node kind.
        /// A subtheme may become a theme at any time and may become a leaf
        /// only if it has no children.
        /// A theme may become a subtheme or leaf only if another valid theme
        /// can act as its parent. Changing a theme to a leaf additionally
        /// requires that it has no children.
        /// </remarks>
        public static bool CheckValidNodeKindChange(
            GameObject node,
            MindMapNodeKind newNodeKind,
            MindMapNodeKind oldNodeKind)
        {
            MMNodeValueHolder valueHolder =
                node.GetComponent<MMNodeValueHolder>();

            if (oldNodeKind == MindMapNodeKind.Theme)
            {
                return (newNodeKind == MindMapNodeKind.Leaf
                        && valueHolder.GetChildren().Count == 0
                        || newNodeKind == MindMapNodeKind.Subtheme)
                    && ChangeIsPossible(node);
            }

            if (oldNodeKind == MindMapNodeKind.Subtheme)
            {
                return newNodeKind == MindMapNodeKind.Theme
                    || newNodeKind == MindMapNodeKind.Leaf
                    && valueHolder.GetChildren().Count == 0;
            }

            return true;
        }

        /// <summary>
        /// Checks whether another theme node on the same drawable can act
        /// as a valid parent for <paramref name="selectedNode"/>.
        /// </summary>
        /// <param name="selectedNode">
        /// The Mind Map node for which a new parent is required.
        /// </param>
        /// <returns>
        /// True if at least one valid theme parent exists; otherwise false.
        /// </returns>
        private static bool ChangeIsPossible(GameObject selectedNode)
        {
            GameObject attachedObjects =
                GameFinder.GetAttachedObjectsObject(selectedNode);

            foreach (GameObject node
                     in attachedObjects.FindAllDescendantsWithTag(Tags.MindMapNode))
            {
                if (node.GetComponent<MMNodeValueHolder>().NodeKind
                        == MindMapNodeKind.Theme
                    && ParentChangeIsValid(selectedNode, node))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a drawable configuration containing the selected Mind Map node,
        /// all of its descendants, and the branch lines connecting these descendants.
        /// </summary>
        /// <param name="node">
        /// The selected Mind Map node that should form the root of the summarized subtree.
        /// </param>
        /// <returns>
        /// A drawable configuration containing the selected node, all descendants,
        /// and their branch lines, or null if <paramref name="node"/> is not a Mind Map node.
        /// </returns>
        public static DrawableConfig SummarizeSelectedNodeIncChildren(
            GameObject node)
        {
            if (node.CompareTag(Tags.MindMapNode))
            {
                DrawableConfig conf =
                    DrawableConfigManager.GetDrawableConfig(
                        GameFinder.GetDrawableSurface(node));

                conf.TextConfigs.Clear();
                conf.ImageConfigs.Clear();

                List<LineConf> selectedBranchLines = new();

                List<MindMapNodeConf> selectedNodes = new()
                {
                    MindMapNodeConf.GetNodeConf(node)
                };

                MMNodeValueHolder valueHolder =
                    node.GetComponent<MMNodeValueHolder>();

                foreach (KeyValuePair<GameObject, GameObject> pair
                         in valueHolder.GetAllChildren())
                {
                    selectedNodes.Add(
                        MindMapNodeConf.GetNodeConf(pair.Key));

                    selectedBranchLines.Add(
                        LineConf.GetLine(pair.Value));
                }

                conf.LineConfigs = selectedBranchLines;
                conf.MindMapNodeConfigs = selectedNodes;

                return conf;
            }

            return null;
        }
    }
}
