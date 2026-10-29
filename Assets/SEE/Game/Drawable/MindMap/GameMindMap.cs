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
        /// <param name="name">The drawable type name from which the ID should be extracted.</param>
        /// <returns>The extracted ID.</returns>
        public static string GetIDofName(string name)
        {
            return name.Split('-')[1];
        }

        /// <summary>
        /// Renames Mind Map nodes and their branch lines if their names conflict
        /// with objects that already exist on the drawable.
        /// </summary>
        /// <param name="config">The drawable configuration containing the Mind Map.</param>
        /// <param name="attachedObject">
        /// The attached objects object containing the already existing drawable objects.
        /// </param>
        public static void RenameMindMap(DrawableConfig config, GameObject attachedObject)
        {
            if (attachedObject != null)
            {
                Dictionary<string, string> nameDictionary = new();
                Dictionary<string, string> idDictionary = new();

                foreach (MindMapNodeConf node in config.MindMapNodeConfigs)
                {
                    RenameNode(node, attachedObject, nameDictionary, idDictionary);
                }

                foreach (LineConf branchLine in config.LineConfigs)
                {
                    if (branchLine.ID.StartsWith(ValueHolder.MindMapBranchLine))
                    {
                        RenameBranchLine(branchLine, idDictionary);
                    }
                }
            }
        }

        /// <summary>
        /// Renames the given Mind Map node configuration if its ID is already in use.
        /// </summary>
        /// <param name="conf">The node configuration that should be checked and renamed.</param>
        /// <param name="attachedObjects">
        /// The attached objects object containing the already existing drawable objects.
        /// </param>
        /// <param name="nameDictionary">
        /// Maps old node names to their replacement names.
        /// </param>
        /// <param name="idDictionary">
        /// Maps old node IDs to their corresponding replacement names.
        /// </param>
        private static void RenameNode(MindMapNodeConf conf, GameObject attachedObjects,
            Dictionary<string, string> nameDictionary, Dictionary<string, string> idDictionary)
        {
            string prefix = GameMindMapNode.GetPrefix(conf.NodeKind);

            if (GameFinder.FindAttachedOrLocalDescendant(attachedObjects, conf.ID) != null)
            {
                string id = RandomStrings.GetRandomString(8);
                string newName = prefix + id;

                while (GameFinder.FindAttachedOrLocalDescendant(attachedObjects, newName) != null)
                {
                    id = RandomStrings.GetRandomString(8);
                    newName = prefix + id;
                }

                nameDictionary.Add(conf.ID, newName);
                idDictionary.Add(GetIDofName(conf.ID), newName);

                conf.ID = newName;
                conf.BorderConf.ID = ValueHolder.LinePrefix + id;
                conf.TextConf.ID = ValueHolder.TextPrefix + id;

                if (conf.ParentNode != "")
                {
                    conf.ParentNode = nameDictionary[conf.ParentNode];
                    conf.BranchLineToParent = ValueHolder.MindMapBranchLine + "-"
                        + GetIDofName(conf.ParentNode) + "-" + GetIDofName(conf.ID);

                    if (conf.BranchLineConf != null)
                    {
                        conf.BranchLineConf.ID = conf.BranchLineToParent;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the ID of the given branch line configuration according to renamed
        /// Mind Map node IDs.
        /// </summary>
        /// <param name="conf">The branch line configuration that should be renamed.</param>
        /// <param name="idDictionary">
        /// Maps old node IDs to their corresponding replacement names.
        /// </param>
        private static void RenameBranchLine(LineConf conf, Dictionary<string, string> idDictionary)
        {
            string[] splitOfOldName = conf.ID.Split("-");

            string newParentID = splitOfOldName[1];
            if (idDictionary.TryGetValue(newParentID, out string nPID))
            {
                newParentID = GetIDofName(nPID);
            }

            string newChildID = splitOfOldName[2];
            if (idDictionary.TryGetValue(newChildID, out string nCID))
            {
                newChildID = GetIDofName(nCID);
            }

            conf.ID = ValueHolder.MindMapBranchLine + "-" + newParentID + "-" + newChildID;
        }
    }
}
