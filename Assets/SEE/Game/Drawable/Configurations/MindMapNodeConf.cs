using Assets.SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Assets.SEE.Game.Drawable.GameDrawer;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// The configuration class for a drawable line.
    /// </summary>
    [Serializable]
    public class MindMapNodeConf : DrawableType, ICloneable
    {
        /// <summary>
        /// The position of the node.
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// The scale of the node.
        /// </summary>
        public Vector3 scale;

        /// <summary>
        /// The euler angles of the node.
        /// </summary>
        public Vector3 eulerAngles;

        /// <summary>
        /// The order in layer for this drawable object.
        /// </summary>
        public int orderInLayer;

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
        /// Label in the configuration file for the id of a node.
        /// </summary>
        private const string IDLabel = "IDLabel";

        /// <summary>
        /// Label in the configuration file for the position of a node.
        /// </summary>
        private const string PositionLabel = "PositionLabel";

        /// <summary>
        /// Label in the configuration file for the scale of a node.
        /// </summary>
        private const string ScaleLabel = "ScaleLabel";

        /// <summary>
        /// Label in the configuration file for the euler angles of a line.
        /// </summary>
        private const string EulerAnglesLabel = "EulerAnglesLabel";

        /// <summary>
        /// Label in the configuration file for the order in layer of a line.
        /// </summary>
        private const string OrderInLayerLabel = "OrderInLayerLabel";

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
                conf = new();
                conf.id = obj.name;
                conf.position = obj.transform.localPosition;
                conf.scale = obj.transform.localScale;
                conf.eulerAngles = obj.transform.localEulerAngles;
                conf.orderInLayer = obj.GetComponent<OrderInLayerValueHolder>().GetOrderInLayer();

                MMNodeValueHolder valueHolder = obj.GetComponent<MMNodeValueHolder>();
                conf.layer = valueHolder.GetLayer();
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
                conf.nodeKind = valueHolder.GetNodeKind();
                conf.borderConf = LineConf.GetLine(GameFinder.FindChildWithTag(obj, Tags.Line));
                conf.textConf = TextConf.GetText(GameFinder.FindChildWithTag(obj, Tags.DText));
                conf.children = valueHolder.GetChildren();
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
        internal void Save(ConfigWriter writer)
        {

            writer.BeginGroup();
            writer.Save(id, IDLabel);
            writer.Save(position, PositionLabel);
            writer.Save(scale, ScaleLabel);
            writer.Save(eulerAngles, EulerAnglesLabel);
            writer.Save(orderInLayer, OrderInLayerLabel);
            writer.Save(layer, LayerLabel);
            writer.Save(parentNode, ParentIDLabel);
            writer.Save(branchLineToParent, ParentBranchLineLabel);
            writer.Save(nodeKind.ToString(), NodeKindLabel);

            writer.BeginList(BorderLabel);
            borderConf.Save(writer);
            writer.EndList();

            writer.BeginList(TextLabel);
            textConf.Save(writer);
            writer.EndList();

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
        internal bool Restore(Dictionary<string, object> attributes)
        {
            bool errors = false;
            
            if (attributes.TryGetValue(IDLabel, out object name))
            {
                id = (string)name;
            }
            else
            {
                errors = true;
            }
            Vector3 loadedPosition = Vector3.zero;
            if (ConfigIO.Restore(attributes, PositionLabel, ref loadedPosition))
            {
                position = loadedPosition;
            }
            else
            {
                position = Vector3.zero;
                errors = true;
            }
            Vector3 loadedScale = Vector3.zero;
            if (ConfigIO.Restore(attributes, ScaleLabel, ref loadedScale))
            {
                scale = loadedScale;
            }
            else
            {
                scale = Vector3.zero;
                errors = true;
            }
            Vector3 loadedEulerAngles = Vector3.zero;
            if (ConfigIO.Restore(attributes, EulerAnglesLabel, ref loadedEulerAngles))
            {
                eulerAngles = loadedEulerAngles;
            }
            else
            {
                eulerAngles = Vector3.zero;
                errors = true;
            }
            if (!ConfigIO.Restore(attributes, OrderInLayerLabel, ref orderInLayer))
            {
                errors = true;
            }
            if (!ConfigIO.Restore(attributes, LayerLabel, ref layer))
            {
                errors = true;
            }
            if (attributes.TryGetValue(ParentIDLabel, out object pName))
            {
                parentNode = (string)pName;
            }
            else
            {
                errors = true;
            }
            if (attributes.TryGetValue(ParentBranchLineLabel, out object pBranch))
            {
                branchLineToParent = (string)pBranch;
            }
            else
            {
                errors = true;
            }
            if (attributes.TryGetValue(NodeKindLabel, out object kind) && Enum.TryParse<GameMindMap.NodeKind>((string)kind, out GameMindMap.NodeKind result))
            {
                nodeKind = result;
            }
            else
            {
                nodeKind = GameMindMap.NodeKind.Theme;
                errors = true;
            }
            if (attributes.TryGetValue(BorderLabel, out object lineList))
            {
                foreach (object item in (List<object>)lineList)
                {
                    Dictionary<string, object> lineDict = (Dictionary<string, object>)item;
                    LineConf config = new LineConf();
                    config.Restore(lineDict);
                    borderConf = config;
                }
            }
            if (attributes.TryGetValue(TextLabel, out object textList))
            {
                foreach (object item in (List<object>)textList)
                {
                    Dictionary<string, object> textDict = (Dictionary<string, object>)item;
                    TextConf config = new TextConf();
                    config.Restore(textDict);
                    textConf = config;
                }
            }

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