using SEE.Game.Drawable.ValueHolders;
using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// The configuration class for a drawable line.
    /// </summary>
    [Serializable]
    public class MindMapNodeConf : DrawableType, ICloneable
    {
        /// <summary>
        /// The mind map layer of the node.
        /// </summary>
        public int layer;

        /// <summary>
        /// The name of the parent node.
        /// </summary>
        public string parentNode;

        /// <summary>
        /// The name of the branch line to the parent.
        /// </summary>
        public string branchLineToParent;

        /// <summary>
        /// The node kind of the node.
        /// </summary>
        public GameMindMap.NodeKind nodeKind;

        /// <summary>
        /// The configuration of the mind map border (line)
        /// </summary>
        public LineConf borderConf;

        /// <summary>
        /// The configuration of the mind map description (text)
        /// </summary>
        public TextConf textConf;

        /// <summary>
        /// The configuration of the branch line to parent.
        /// </summary>
        public LineConf branchLineConf;

        /// <summary>
        /// The dictionary with the childs and the branch lines to them.
        /// </summary>
        public Dictionary<GameObject, GameObject> children;
        /// <summary>
        /// The dictionary with the children names and the branch line names.
        /// </summary>
        private Dictionary<string, string> childrenStrings = new();

        /// <summary>
        /// Label in the configuration file for the mind map layer of the node.
        /// </summary>
        private const string LayerLabel = "LayerLabel";

        /// <summary>
        /// Label in the configuration file for the parent id of the node.
        /// </summary>
        private const string ParentIDLabel = "ParentIDLabel";

        /// <summary>
        /// Label in the configuration file for the parent branch line.
        /// </summary>
        private const string ParentBranchLineLabel = "ParentBranchLineLabel";

        /// <summary>
        /// Label in the configuration file for the node kind.
        /// </summary>
        private const string NodeKindLabel = "NodeKindLabel";

        /// <summary>
        /// Label in the configuration file for the mind map node border.
        /// </summary>
        private const string BorderLabel = "BorderLabel";

        /// <summary>
        /// Label in the configuration file for the mind map node text.
        /// </summary>
        private const string TextLabel = "TextLabel";

        /// <summary>
        /// Label in the configuration file for the childs.
        /// </summary>
        private const string ChildrenLabel = "ChildrenLabel";

        /// <summary>
        /// Creates a <see cref="MindMapNodeConf"/> for the given game object.
        /// If it is a mind map node, otherwise it is null.
        /// </summary>
        /// <param name="obj">The game object with the <see cref="Tags.MindMapNode"/></param>
        /// <returns>The created <see cref="MindMapNodeConf"/> object</returns>
        public static MindMapNodeConf GetNodeConf(GameObject obj)
        {
            MindMapNodeConf conf = null;
            if (obj != null && obj.CompareTag(Tags.MindMapNode))
            {
                MMNodeValueHolder valueHolder = obj.GetComponent<MMNodeValueHolder>();
                conf = new()
                {
                    id = obj.name,
                    position = obj.transform.localPosition,
                    scale = obj.transform.localScale,
                    eulerAngles = obj.transform.localEulerAngles,
                    orderInLayer = obj.GetComponent<OrderInLayerValueHolder>().GetOrderInLayer(),
                    layer = valueHolder.GetLayer(),
                    nodeKind = valueHolder.GetNodeKind(),
                    borderConf = LineConf.GetLine(GameFinder.FindChildWithTag(obj, Tags.Line)),
                    textConf = TextConf.GetText(GameFinder.FindChildWithTag(obj, Tags.DText)),
                    children = valueHolder.GetChildren()
                };
                /// Set the parent information.
                if (valueHolder.GetParent() != null)
                {
                    conf.parentNode = valueHolder.GetParent().name;
                    conf.branchLineToParent = valueHolder.GetParentBranchLine().name;
                    conf.branchLineConf = LineConf.GetLine(valueHolder.GetParentBranchLine());
                } else
                {
                    conf.parentNode = "";
                    conf.branchLineToParent = "";
                }
                /// Converts the children in a pair of strings based on their ids.
                foreach (var item in conf.children)
                {
                    if (item.Key != null && item.Value != null)
                    {
                        conf.childrenStrings.Add(item.Key.name, item.Value.name);
                    }
                }
            }
            return conf;
        }

        /// <summary>
        /// Clons the mind map node configuration object.
        /// </summary>
        /// <returns>A copy of this configuration object.</returns>
        public object Clone()
        {
            return new MindMapNodeConf
            { 
                id = this.id,
                position = this.position,
                scale = this.scale,
                eulerAngles = this.eulerAngles,
                orderInLayer = this.orderInLayer,
                layer = this.layer,
                parentNode = this.parentNode,
                branchLineToParent = this.branchLineToParent,
                nodeKind = this.nodeKind,
                children = this.children,
                childrenStrings = this.childrenStrings,
                borderConf = this.borderConf,
                textConf = this.textConf
            };
        }

        /// <summary>
        /// Writes this instances' attributes into the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes into.</param>
        override internal void Save(ConfigWriter writer)
        {

            writer.BeginGroup();
            base.Save(writer);
            writer.Save(layer, LayerLabel);
            writer.Save(parentNode, ParentIDLabel);
            writer.Save(branchLineToParent, ParentBranchLineLabel);
            writer.Save(nodeKind.ToString(), NodeKindLabel);

            /// Writes the border configuration (line configuration)
            writer.BeginList(BorderLabel);
            borderConf.Save(writer);
            writer.EndList();

            /// Writes the text configuration
            writer.BeginList(TextLabel);
            textConf.Save(writer);
            writer.EndList();

            /// Writes the pair of the children names and their branch lines to the parent node.
            writer.SaveAsStrings(childrenStrings, ChildrenLabel);
            writer.EndGroup();
        }

        /// <summary>
        /// Given the representation of a <see cref="MindMapNodeConf"/> as created by the <see cref="ConfigWriter"/>, this
        /// method parses the attributes from that representation and puts them into this <see cref="MindMapNodeConf"/>
        /// instance.
        /// </summary>
        /// <param name="attributes">A list of labels (strings) of attributes and their values (objects). This
        /// has to be the representation of a <see cref="MindMapNodeConf"/> as created by
        /// <see cref="ConfigWriter"/>.</param>
        /// <returns>Whether or not the <see cref="MindMapNodeConf"/> was loaded without errors.</returns>
        override internal bool Restore(Dictionary<string, object> attributes)
        {
            bool errors = base.Restore(attributes);

            /// Try to restores the mind map node layer.
            if (!ConfigIO.Restore(attributes, LayerLabel, ref layer))
            {
                errors = true;
            }

            /// Try to restores the parent id.
            if (attributes.TryGetValue(ParentIDLabel, out object pName))
            {
                parentNode = (string)pName;
            }
            else
            {
                errors = true;
            }

            /// Try to restores the parent branch line id.
            if (attributes.TryGetValue(ParentBranchLineLabel, out object pBranch))
            {
                branchLineToParent = (string)pBranch;
            }
            else
            {
                errors = true;
            }

            /// Try to restores the node kind.
            if (attributes.TryGetValue(NodeKindLabel, out object kind) 
                && Enum.TryParse<GameMindMap.NodeKind>((string)kind, out GameMindMap.NodeKind result))
            {
                nodeKind = result;
            }
            else
            {
                nodeKind = GameMindMap.NodeKind.Theme;
                errors = true;
            }

            /// Try to restores the mind map border (line configuration).
            if (attributes.TryGetValue(BorderLabel, out object lineList))
            {
                foreach (object item in (List<object>)lineList)
                {
                    Dictionary<string, object> lineDict = (Dictionary<string, object>)item;
                    LineConf config = new();
                    config.Restore(lineDict);
                    borderConf = config;
                }
            }

            /// Try to restores the mind map text (text configuration).
            if (attributes.TryGetValue(TextLabel, out object textList))
            {
                foreach (object item in (List<object>)textList)
                {
                    Dictionary<string, object> textDict = (Dictionary<string, object>)item;
                    TextConf config = new();
                    config.Restore(textDict);
                    textConf = config;
                }
            }

            /// Try to restores the pair of child names and their branch lines.
            if (attributes.TryGetValue(ChildrenLabel, out object childrenDict))
            {
                foreach (object dict in (List<object>)childrenDict)
                {
                    string key = "", value = "";
                    int i = 0;
                    foreach (object item in (List<object>)dict)
                    {
                        if (i == 0)
                        {
                            key = (string)item;
                            i++;
                        } else
                        {
                            value = (string)item;
                        }
                    }
                    childrenStrings.Add(key, value);
                }
            }
            else
            {
                errors = true;
            }
            return !errors;
        }
    }
}