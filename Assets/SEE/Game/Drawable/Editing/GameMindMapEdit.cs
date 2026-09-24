using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.MindMap;
using SEE.GO;
using UnityEngine;

namespace SEE.Game.Drawable.Editing
{
    /// <summary>
    /// Provides editing operations for Mind Map nodes.
    /// </summary>
    internal static class GameMindMapEdit
    {
        /// <summary>
        /// Changes all editable values of a Mind Map node.
        /// </summary>
        /// <param name="node">The node whose values should be changed.</param>
        /// <param name="conf">Contains the new values.</param>
        internal static void ChangeMindMapNode(GameObject node, MindMapNodeConf conf)
        {
            if (node.CompareTag(Tags.MindMapNode))
            {
                GameMindMap.ChangeNodeKind(node, conf.NodeKind, conf.BorderConf);

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

                GameMindMapBranch.ChangeParent(node, parent);

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

                    GameLineEdit.ChangeLine(branch, conf.BranchLineConf);
                    branch.GetComponent<MeshCollider>().enabled = false;
                }

                GameLayerChanger.SetOrderInLayer(node, conf.OrderInLayer);
            }
        }
    }
}
