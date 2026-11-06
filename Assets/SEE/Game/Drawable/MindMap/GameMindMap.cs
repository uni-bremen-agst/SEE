using SEE.Game.Drawable.Configurations;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.Drawable.MindMap
{
    /// <summary>
    /// Provides operations that coordinate multiple components of a Mind Map.
    /// </summary>
    public static class GameMindMap
    {
        /// <summary>
        /// Returns all available Mind Map node kinds.
        /// </summary>
        /// <returns>All available Mind Map node kinds.</returns>
        public static List<MindMapNodeKind> GetNodeKinds()
        {
            return Enum.GetValues(typeof(MindMapNodeKind)).Cast<MindMapNodeKind>().ToList();
        }

        /// <summary>
        /// Recreates a Mind Map node and restores its branch line to its parent.
        /// </summary>
        /// <param name="surface">The drawable surface on which the node should be displayed.</param>
        /// <param name="conf">The node configuration to restore.</param>
        /// <returns>The recreated Mind Map node.</returns>
        public static GameObject ReCreate(GameObject surface, MindMapNodeConf conf)
        {
            GameObject createdNode = GameMindMapNode.Restore(surface, conf);
            GameObject attachedObjects = GameFinder.GetAttachedObjectsObject(surface);

            if (attachedObjects != null && conf.ParentNode != "")
            {
                GameObject parent = GameFinder.FindAttachedOrLocalDescendant(
                    attachedObjects, conf.ParentNode);

                if (parent != null)
                {
                    GameMindMapBranch.CreateBranchLine(
                        createdNode, parent, conf.BranchLineToParent);
                }
            }

            return createdNode;
        }

        /// <summary>
        /// Redraws the border of the given Mind Map node and updates its connected
        /// branch lines.
        /// </summary>
        /// <param name="node">The node whose border should be redrawn.</param>
        public static void ReDrawBorder(GameObject node)
        {
            if (node.CompareTag(Tags.MindMapNode))
            {
                GameMindMapNode.UpdateBorder(node);
                GameMindMapBranch.ReDrawBranchLines(node);
            }
        }

        /// <summary>
        /// Returns the ID of a drawable type name.
        /// </summary>
        /// <param name="name">
        /// The drawable type name from which the ID should be extracted.
        /// </param>
        /// <returns>The extracted ID.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the name does not contain a valid ID.
        /// </exception>
        public static string GetIDofName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The drawable name must not be empty.", nameof(name));
            }

            string[] parts = name.Split('-');

            if (parts.Length < 2 || string.IsNullOrEmpty(parts[1]))
            {
                throw new ArgumentException(
                    $"Invalid drawable name: {name}", nameof(name));
            }

            return parts[1];
        }

        /// <summary>
        /// Renames mind map nodes and their associated drawable objects
        /// if their names already exist on the destination drawable.
        /// Updates parent, child, and branch line references consistently.
        /// </summary>
        /// <param name="config">
        /// The drawable configuration containing the nodes and branch lines.
        /// </param>
        /// <param name="attachedObject">
        /// The attached objects object of the destination drawable.
        /// </param>
        public static void RenameMindMap(DrawableConfig config, GameObject attachedObject)
        {
            if (config == null || attachedObject == null)
            {
                return;
            }

            Dictionary<string, string> nodeNames = new();
            Dictionary<string, string> branchNames = new();
            HashSet<string> reservedNames = new(StringComparer.Ordinal);

            foreach (MindMapNodeConf node in config.MindMapNodeConfigs)
            {
                if (string.IsNullOrEmpty(node.ID)
                    || !reservedNames.Add(node.ID))
                {
                    throw new InvalidOperationException(
                        $"Invalid or duplicate mind map node ID: {node.ID}");
                }

                reservedNames.Add(node.BorderConf.ID);
                reservedNames.Add(node.TextConf.ID);
            }

            foreach (LineConf line in config.LineConfigs)
            {
                reservedNames.Add(line.ID);
            }

            // Rename nodes if any associated drawable name already exists.
            foreach (MindMapNodeConf node in config.MindMapNodeConfigs)
            {
                bool hasCollision =
                    GameFinder.FindAttachedOrLocalDescendant(
                        attachedObject, node.ID) != null
                    || GameFinder.FindAttachedOrLocalDescendant(
                        attachedObject, node.BorderConf.ID) != null
                    || GameFinder.FindAttachedOrLocalDescendant(
                        attachedObject, node.TextConf.ID) != null
                    || (!string.IsNullOrEmpty(node.BranchLineToParent)
                        && GameFinder.FindAttachedOrLocalDescendant(
                            attachedObject, node.BranchLineToParent) != null);

                if (!hasCollision)
                {
                    continue;
                }

                string oldName = node.ID;
                string prefix = GameMindMapNode.GetPrefix(node.NodeKind);

                string newName;
                string newID;
                string newBorderName;
                string newTextName;

                do
                {
                    // GetRandomString already includes the leading hyphen.
                    newName = prefix + RandomStrings.GetRandomString(8);
                    newID = GetIDofName(newName);

                    newBorderName = ValueHolder.LinePrefix + "-" + newID;
                    newTextName = ValueHolder.TextPrefix + "-" + newID;
                }
                while (reservedNames.Contains(newName)
                       || reservedNames.Contains(newBorderName)
                       || reservedNames.Contains(newTextName)
                       || GameFinder.FindAttachedOrLocalDescendant(
                           attachedObject, newName) != null
                       || GameFinder.FindAttachedOrLocalDescendant(
                           attachedObject, newBorderName) != null
                       || GameFinder.FindAttachedOrLocalDescendant(
                           attachedObject, newTextName) != null);

                reservedNames.Add(newName);
                reservedNames.Add(newBorderName);
                reservedNames.Add(newTextName);

                nodeNames.Add(oldName, newName);

                node.ID = newName;
                node.BorderConf.ID = newBorderName;
                node.TextConf.ID = newTextName;
            }

            // Update parent references and parent branch line IDs.
            foreach (MindMapNodeConf node in config.MindMapNodeConfigs)
            {
                if (string.IsNullOrEmpty(node.ParentNode))
                {
                    continue;
                }

                if (nodeNames.TryGetValue(
                        node.ParentNode, out string newParentName))
                {
                    node.ParentNode = newParentName;
                }

                string oldBranchName = node.BranchLineToParent;

                string newBranchName = ValueHolder.MindMapBranchLine
                    + "-" + GetIDofName(node.ParentNode)
                    + "-" + GetIDofName(node.ID);

                if (!string.IsNullOrEmpty(oldBranchName)
                    && oldBranchName != newBranchName)
                {
                    if (branchNames.TryGetValue(
                            oldBranchName, out string existingName)
                        && existingName != newBranchName)
                    {
                        throw new InvalidOperationException(
                            $"Conflicting branch line ID: {oldBranchName}");
                    }

                    branchNames[oldBranchName] = newBranchName;
                }

                node.BranchLineToParent = newBranchName;

                if (node.BranchLineConf != null)
                {
                    node.BranchLineConf.ID = newBranchName;
                }
            }

            // Update independently stored branch line configurations.
            foreach (LineConf line in config.LineConfigs)
            {
                if (branchNames.TryGetValue(
                        line.ID, out string newBranchName))
                {
                    line.ID = newBranchName;
                }
            }

            // Update child and branch line references in each node.
            foreach (MindMapNodeConf node in config.MindMapNodeConfigs)
            {
                node.RenameChildReferences(nodeNames, branchNames);
            }
        }
    }
}
