using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.MindMap;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// Provides shared Mind Map types and configuration-level naming operations.
    /// </summary>
    public static class GameMindMap
    {
        /// <summary>
        /// The different kinds of a mind map node.
        /// </summary>
        [Serializable]
        public enum NodeKind
        {
            Theme,
            Subtheme,
            Leaf
        }

        /// <summary>
        /// Returns the list of the different node kinds.
        /// </summary>
        /// <returns>A list of the node kinds.</returns>
        public static List<NodeKind> GetNodeKinds()
        {
            return Enum.GetValues(typeof(NodeKind)).Cast<NodeKind>().ToList();
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
        /// Renames the mind map nodes and branchlines.
        /// </summary>
        /// <param name="config">The drawable configuration that holds the nodes and branch lines.</param>
        /// <param name="attachedObject">The attached objects object where the drawable types should be placed.</param>
        public static void RenameMindMap(DrawableConfig config, GameObject attachedObject)
        {
            if (attachedObject != null)
            {
                Dictionary<string, string> nameDictionary = new();
                Dictionary<string, string> idDictionary = new();
                /// Block to rename the nodes.
                foreach (MindMapNodeConf node in config.MindMapNodeConfigs)
                {
                    RenameNode(node, attachedObject, nameDictionary, idDictionary);
                }
                /// Block to rename the branch lines.
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
        /// Renames a mind map node.
        /// </summary>
        /// <param name="conf">The node configuration that should be renamed.</param>
        /// <param name="attachedObjects">The attached objects object where the drawable types should be placed.</param>
        /// <param name="nameDictionary">The dictionary that holds the old name and the new name.</param>
        /// <param name="idDictionary">Dictionary that holds the old IDs and the new names.</param>
        private static void RenameNode(MindMapNodeConf conf, GameObject attachedObjects,
            Dictionary<string, string> nameDictionary, Dictionary<string, string> idDictionary)
        {
            string prefix = GameMindMapNode.GetPrefix(conf.NodeKind);

            if (GameFinder.FindAttachedOrLocalDescendant(attachedObjects, conf.ID) != null)
            {
                /// Gets a new id for the object based on a random string.
                string id = RandomStrings.GetRandomString(8);
                string newName = prefix + id;

                /// Check if the name is already in use. If so, generate a new name.
                while (GameFinder.FindAttachedOrLocalDescendant(attachedObjects, newName) != null)
                {
                    id = RandomStrings.GetRandomString(8);
                    newName = prefix + id;
                }

                /// Adds the old and the new name to a dictionary.
                nameDictionary.Add(conf.ID, newName);

                /// Adds a pair of the old id and the new name in a other dictionary.
                idDictionary.Add(GetIDofName(conf.ID), newName);

                /// Change the names to the new name.
                conf.ID = newName;
                conf.BorderConf.ID = ValueHolder.LinePrefix + id;
                conf.TextConf.ID = ValueHolder.TextPrefix + id;

                /// If the node has a parent, replace the old parent name with the new one.
                if (conf.ParentNode != "")
                {
                    conf.ParentNode = nameDictionary[conf.ParentNode];
                    /// Rename the branch line with the new id's of the parent and the node.
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
        /// Renames a branch line.
        /// </summary>
        /// <param name="conf">The branch line config.</param>
        /// <param name="idDictionary">Dictionary that holds the old IDs and the new names.</param>
        private static void RenameBranchLine(LineConf conf, Dictionary<string, string> idDictionary)
        {
            string prefix = ValueHolder.MindMapBranchLine;

            /// Splits the old name into three parts.
            string[] splitOfOldName = conf.ID.Split("-");

            /// Get the new parent id.
            string newParentID = splitOfOldName[1];
            if (idDictionary.TryGetValue(newParentID, out string nPID))
            {
                newParentID = GetIDofName(nPID);
            }

            /// Get the new node id.
            string newChildID = splitOfOldName[2];
            if (idDictionary.TryGetValue(newChildID, out string nCID))
            {
                newChildID = GetIDofName(nCID);
            }

            /// Rename the branch line.
            conf.ID = prefix + "-" + newParentID + "-" + newChildID;
        }
    }
}
