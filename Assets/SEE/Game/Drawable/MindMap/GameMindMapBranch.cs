using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.Line;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Drawable.MindMap
{
    /// <summary>
    /// Provides creation, redrawing, and parent-changing behavior
    /// for branch lines between Mind Map nodes.
    /// </summary>
    public static class GameMindMapBranch
    {
        /// <summary>
        /// Creates or recreates a branch line between the given child and parent node.
        /// The branch line is placed behind both connected nodes and its collider is disabled.
        /// The parent-child relationship in the corresponding
        /// <see cref="MMNodeValueHolder"/> components is updated as well.
        /// </summary>
        /// <param name="child">The child Mind Map node.</param>
        /// <param name="parent">The parent Mind Map node.</param>
        /// <param name="name">
        /// The branch-line name. If empty, a name is generated from the parent
        /// and child identifiers.
        /// </param>
        /// <returns>The created or recreated branch line.</returns>
        public static GameObject CreateBranchLine(
            GameObject child,
            GameObject parent,
            string name = "")
        {
            Vector3 endPoint =
                NearestPoints.GetNearestPoint(parent, child.transform.position);

            Vector3 startPoint =
                NearestPoints.GetNearestPoint(child, endPoint);

            Vector3[] positions =
            {
                startPoint,
                endPoint
            };

            child.GetRootParent().transform.InverseTransformPoints(positions);

            GameObject surface = GameFinder.GetDrawableSurface(child);

            if (name == "")
            {
                name = ValueHolder.MindMapBranchLine + "-"
                    + GameMindMap.GetIDofName(parent.name) + "-"
                    + GameMindMap.GetIDofName(child.name);
            }

            GameObject branchLine = GameLineDrawer.DrawLine(
                surface,
                name,
                positions,
                ColorKind.Monochrome,
                Color.black,
                ValueHolder.CurrentSecondaryColor,
                ValueHolder.StandardLineThickness,
                true,
                LineKind.Solid,
                ValueHolder.StandardLineTiling,
                increaseCurrentOrder: false);

            int order = GetBranchLineOrder(child, parent);

            GameLayerChanger.ChangeOrderInLayer(
                branchLine,
                order,
                GameLayerChanger.LayerChangerStates.Decrease,
                false);

            MMNodeValueHolder parentValueHolder =
                parent.GetComponent<MMNodeValueHolder>();

            parentValueHolder.AddChild(child, branchLine);

            MMNodeValueHolder childValueHolder =
                child.GetComponent<MMNodeValueHolder>();

            childValueHolder.SetParent(parent, branchLine);

            GameMindMapHierarchy.UpdateMindMapLayer(childValueHolder, parentValueHolder);

            // Disable the branch-line collider because branch lines belong to the
            // Mind Map relation and should not be edited as regular drawable lines.
            branchLine.GetComponent<MeshCollider>().enabled = false;

            return branchLine;
        }

        /// <summary>
        /// Calculates the rendering order of a branch line.
        /// The branch line is placed one order below the lower order
        /// of its child and parent node, but never below zero.
        /// </summary>
        /// <param name="child">The child Mind Map node.</param>
        /// <param name="parent">The parent Mind Map node.</param>
        /// <returns>The calculated order in layer.</returns>
        private static int GetBranchLineOrder(
            GameObject child,
            GameObject parent)
        {
            int childOrder =
                child.GetComponent<OrderInLayerValueHolder>().OrderInLayer;

            int parentOrder =
                parent.GetComponent<OrderInLayerValueHolder>().OrderInLayer;

            return Mathf.Max(
                0,
                Mathf.Min(childOrder, parentOrder) - 1);
        }

        /// <summary>
        /// Redraws the branch line connecting the given node with its parent.
        /// If the node has no parent branch line, nothing is changed.
        /// </summary>
        /// <param name="node">
        /// The Mind Map node whose parent branch line should be redrawn.
        /// </param>
        public static void ReDrawParentBranchLine(GameObject node)
        {
            if (!node.CompareTag(Tags.MindMapNode))
            {
                return;
            }

            MMNodeValueHolder valueHolder =
                node.GetComponent<MMNodeValueHolder>();

            if (valueHolder.GetParentBranchLine() != null)
            {
                CreateBranchLine(
                    node,
                    valueHolder.GetParent(),
                    valueHolder.GetParentBranchLine().name);
            }
        }

        /// <summary>
        /// Redraws all branch lines directly connected to the given Mind Map node.
        /// This includes the branch to its parent and all branches to its direct children.
        /// </summary>
        /// <param name="node">
        /// The Mind Map node whose connected branch lines should be redrawn.
        /// </param>
        /// <returns>
        /// True if the given object is a Mind Map node and its branch lines were processed;
        /// otherwise false.
        /// </returns>
        public static bool ReDrawBranchLines(GameObject node)
        {
            if (!node.CompareTag(Tags.MindMapNode))
            {
                return false;
            }

            MMNodeValueHolder valueHolder =
                node.GetComponent<MMNodeValueHolder>();

            ReDrawParentBranchLine(node);

            foreach (KeyValuePair<GameObject, GameObject> pair
                     in valueHolder.GetChildren())
            {
                CreateBranchLine(
                    pair.Key,
                    node,
                    pair.Value.name);
            }

            return true;
        }

        /// <summary>
        /// Changes the parent of the given Mind Map node.
        /// The previous parent relationship and branch line are removed,
        /// a new branch line is created, and the previous branch-line appearance
        /// is restored if a corresponding configuration exists.
        /// </summary>
        /// <param name="child">The Mind Map node whose parent should be changed.</param>
        /// <param name="parent">The new parent Mind Map node.</param>
        public static void ChangeParent(
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
                || !GameMindMapHierarchy.ParentChangeIsValid(child, parent))
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
                CreateBranchLine(child, parent);

            if (oldBranchLine != null)
            {
                GameEdit.ChangeLine(
                    newBranchLine,
                    oldBranchLine);
            }
        }
    }
}
