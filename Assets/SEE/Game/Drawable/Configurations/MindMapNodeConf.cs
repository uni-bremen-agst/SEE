using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.Utils.Config;
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
        public int Layer;

        /// <summary>
        /// The name of the parent node.
        /// </summary>
        public string ParentNode;

        /// <summary>
        /// The name of the branch line to the parent.
        /// </summary>
        public string BranchLineToParent;

        /// <summary>
        /// The node kind of the node.
        /// </summary>
        public GameMindMap.NodeKind NodeKind;

        /// <summary>
        /// The configuration of the mind map border (line).
        /// </summary>
        public LineConf BorderConf;

        /// <summary>
        /// The configuration of the mind map description (text).
        /// </summary>
        public TextConf TextConf;

        /// <summary>
        /// The configuration of the branch line to parent.
        /// </summary>
        public LineConf BranchLineConf;

        /// <summary>
        /// The dictionary with the childs and the branch lines to them.
        /// </summary>
        public IDictionary<GameObject, GameObject> Children;
        /// <summary>
        /// The dictionary with the children names and the branch line names.
        /// </summary>
        private Dictionary<string, string> childrenNames = new();

        /// <summary>
        /// Creates a <see cref="MindMapNodeConf"/> for the given game object,
        /// if it is a mind map node, otherwise the result is null.
        /// </summary>
        /// <param name="obj">The game object with the <see cref="Tags.MindMapNode"/>.</param>
        /// <returns>The created <see cref="MindMapNodeConf"/> object.</returns>
        public static MindMapNodeConf GetNodeConf(GameObject obj)
        {
            MindMapNodeConf conf = null;
            if (obj != null && obj.CompareTag(Tags.MindMapNode))
            {
                MMNodeValueHolder valueHolder = obj.GetComponent<MMNodeValueHolder>();
                conf = new()
                {
                    ID = obj.name,
                    AssociatedPage = obj.GetComponent<AssociatedPageHolder>().AssociatedPage,
                    Position = obj.transform.localPosition,
                    Scale = obj.transform.localScale,
                    EulerAngles = obj.transform.localEulerAngles,
                    OrderInLayer = obj.GetComponent<OrderInLayerValueHolder>().OrderInLayer,
                    Layer = valueHolder.Layer,
                    NodeKind = valueHolder.NodeKind,
                    BorderConf = LineConf.GetLine(obj.FindDescendantWithTag(Tags.Line)),
                    TextConf = TextConf.GetText(obj.FindDescendantWithTag(Tags.DText)),
                    Children = valueHolder.GetChildren()
                };
                /// Set the parent information.
                if (valueHolder.GetParent() != null)
                {
                    conf.ParentNode = valueHolder.GetParent().name;
                    conf.BranchLineToParent = valueHolder.GetParentBranchLine().name;
                    conf.BranchLineConf = LineConf.GetLine(valueHolder.GetParentBranchLine());
                } else
                {
                    conf.ParentNode = "";
                    conf.BranchLineToParent = "";
                }
                /// Converts the children in a pair of strings based on their ids.
                foreach (var item in conf.Children)
                {
                    if (item.Key != null && item.Value != null)
                    {
                        conf.childrenNames.Add(item.Key.name, item.Value.name);
                    }
                }
            }
            return conf;
        }

        /// <summary>
        /// Clones the mind map node configuration object.
        /// </summary>
        /// <returns>A copy of this configuration object.</returns>
        public object Clone()
        {
            return new MindMapNodeConf
            {
                ID = this.ID,
                AssociatedPage = this.AssociatedPage,
                Position = this.Position,
                Scale = this.Scale,
                EulerAngles = this.EulerAngles,
                OrderInLayer = this.OrderInLayer,
                Layer = this.Layer,
                ParentNode = this.ParentNode,
                BranchLineToParent = this.BranchLineToParent,
                NodeKind = this.NodeKind,
                Children = this.Children,
                childrenNames = this.childrenNames,
                BorderConf = this.BorderConf,
                TextConf = this.TextConf
            };
        }

        #region Config I/O

        /// <summary>
        /// Label in the configuration file for the mind map layer of the node.
        /// </summary>
        private const string layerLabel = "LayerLabel";

        /// <summary>
        /// Label in the configuration file for the parent id of the node.
        /// </summary>
        private const string parentIDLabel = "ParentIDLabel";

        /// <summary>
        /// Label in the configuration file for the parent branch line.
        /// </summary>
        private const string parentBranchLineLabel = "ParentBranchLineLabel";

        /// <summary>
        /// Label in the configuration file for the node kind.
        /// </summary>
        private const string nodeKindLabel = "NodeKindLabel";

        /// <summary>
        /// Label in the configuration file for the mind map node border.
        /// </summary>
        private const string borderLabel = "BorderLabel";

        /// <summary>
        /// Label in the configuration file for the mind map node text.
        /// </summary>
        private const string textLabel = "TextLabel";

        /// <summary>
        /// Label in the configuration file for the childs.
        /// </summary>
        private const string childrenLabel = "ChildrenLabel";

        /// <summary>
        /// Saves this instance's attributes using the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes.</param>
        protected override void SaveAttributes(ConfigWriter writer)
        {
            writer.Save(Layer, layerLabel);
            writer.Save(ParentNode, parentIDLabel);
            writer.Save(BranchLineToParent, parentBranchLineLabel);
            writer.Save(NodeKind.ToString(), nodeKindLabel);

            /// Writes the border configuration (line configuration)
            writer.BeginList(borderLabel);
            BorderConf.Save(writer);
            writer.EndList();

            /// Writes the text configuration
            writer.BeginList(textLabel);
            TextConf.Save(writer);
            writer.EndList();

            /// Writes the pair of the children names and their branch lines to the parent node.
            writer.SaveAsStrings(childrenNames, childrenLabel);
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

            /// Try to restore the mind map node layer.
            if (!ConfigIO.Restore(attributes, layerLabel, ref Layer))
            {
                errors = true;
            }

            /// Try to restore the parent id.
            if (attributes.TryGetValue(parentIDLabel, out object pName))
            {
                ParentNode = (string)pName;
            }
            else
            {
                errors = true;
            }

            /// Try to restore the parent branch line id.
            if (attributes.TryGetValue(parentBranchLineLabel, out object pBranch))
            {
                BranchLineToParent = (string)pBranch;
            }
            else
            {
                errors = true;
            }

            /// Try to restore the node kind.
            if (attributes.TryGetValue(nodeKindLabel, out object kind)
                && Enum.TryParse<GameMindMap.NodeKind>((string)kind, out GameMindMap.NodeKind result))
            {
                NodeKind = result;
            }
            else
            {
                NodeKind = GameMindMap.NodeKind.Theme;
                errors = true;
            }

            /// Try to restore the mind map border (line configuration).
            if (attributes.TryGetValue(borderLabel, out object lineList))
            {
                foreach (object item in (List<object>)lineList)
                {
                    Dictionary<string, object> lineDict = (Dictionary<string, object>)item;
                    LineConf config = new();
                    config.Restore(lineDict);
                    BorderConf = config;
                }
            }

            /// Try to restore the mind map text (text configuration).
            if (attributes.TryGetValue(textLabel, out object textList))
            {
                foreach (object item in (List<object>)textList)
                {
                    Dictionary<string, object> textDict = (Dictionary<string, object>)item;
                    TextConf config = new();
                    config.Restore(textDict);
                    TextConf = config;
                }
            }

            /// Try to restore the pair of child names and their branch lines.
            if (attributes.TryGetValue(childrenLabel, out object childrenDict))
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
                    childrenNames.Add(key, value);
                }
            }
            else
            {
                errors = true;
            }
            return !errors;
        }

        #endregion
    }
}