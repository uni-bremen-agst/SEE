using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;

namespace SEE.Layout.IO
{
    /// <summary>
    /// Reads layout information from GVL files.
    /// </summary>
    public class GVLReader
    {
        /// <summary>
        /// Reads layout information from given GVL file with given <paramref name="filename"/>.
        /// The given position and scale of the <paramref name="gameNodes"/> are updated 
        /// according to the layout data contained therein. The x and z co-ordinates will
        /// be set according to the layout information contained in the GVL file; the
        /// y co-ordinate will be zero (that information is not contained in the GVL file).
        /// 
        /// TODO: We need to extend GVL for a third dimension.
        /// 
        /// Precondition: <paramref name="filename"/> must exist and its content conform to GVL.
        /// </summary>
        /// <param name="filename">name of GVL file</param>
        /// <param name="gameNodes">the game nodes whose position and scale are to be updated</param>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        /// <param name="logger">logger used to emit errors, warnings, etc.</param>
        public GVLReader(string filename, ICollection<IGameNode> gameNodes, float groundLevel, SEE.Utils.ILogger logger = null)
        {
            this.filename = filename;
            this.logger = logger;
            this.groundLevel = groundLevel;
            reader = new XmlTextReader(filename)
            {
                WhitespaceHandling = WhitespaceHandling.None
            };
            this.gameNodes = ToMap(gameNodes);
            // The CS (icon size) values of the nested nodes. The icon size
            // of a node N is the CS value of its immediate parent node.
            // Root nodes do not have a parent, but will try to access the CS
            // value of their parent nevertheless. For these, we will provide
            // a default value here on the stack.
            nodes = new Stack<ParentNode>();
            nodes.Push(new ParentNode(14.14f, null));
            Load();
        }

        /// <summary>
        /// The y co-ordinate setting the ground level; all nodes will be placed on this level.
        /// </summary>
        private readonly float groundLevel;

        /// <summary>
        /// The minimal height for a loaded game object.
        /// </summary>
        private const float MinimalHeight = 0.00001f;

        /// <summary>
        /// Returns a mapping from the IDs of all <paramref name="gameNodes"/> onto
        /// those <paramref name="gameNodes"/>. This mapping allows us to quickly
        /// identify the nodes by their IDs.
        /// </summary>
        /// <param name="gameNodes">game nodes that are to be mapped</param>
        /// <returns>mapping from the IDs onto <paramref name="gameNodes"/></returns>
        private static Dictionary<string, IGameNode> ToMap(ICollection<IGameNode> gameNodes)
        {
            Dictionary<string, IGameNode> result = new Dictionary<string, IGameNode>();
            foreach (IGameNode gameNode in gameNodes)
            {
                result[gameNode.ID] = gameNode;
            }
            return result;
        }

        /// <summary>
        /// Nodes can be nested in GVL and layout data of nested nodes depend upon
        /// layout data of their containing node. ParentNode stores the necessary
        /// information obtained for a parent and requested by its children.
        /// </summary>
        protected struct ParentNode
        {
            /// <summary>
            /// The icon size attribute of the parent. This value specifies the
            /// icon size of its children.
            /// </summary>
            public readonly float CS;
            /// <summary>
            /// The parent game node. It is needed to obtain the parent's position,
            /// because the position of nested nodes are offsets relative to the
            /// parent.
            /// </summary>
            public readonly IGameNode gameNode;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="CS">icon size attribute CS of the parent</param>
            /// <param name="gameNode">parent game node</param>
            public ParentNode(float CS, IGameNode gameNode)
            {
                this.CS = CS;
                this.gameNode = gameNode;
            }
        }

        /// <summary>
        /// The nested XML context.
        /// </summary>
        protected Stack<State> context = new Stack<State>();
        /// <summary>
        /// Layout information of all ascendants of the current node.
        /// </summary>
        protected readonly Stack<ParentNode> nodes;
        /// <summary>
        /// The name of the GVL file read.
        /// </summary>
        protected readonly string filename;
        /// <summary>
        /// The XML reader used to process the XML content of the GVL file.
        /// </summary>
        protected readonly XmlTextReader reader;
        /// <summary>
        /// The logger used to emit errors, warnings, etc.
        /// </summary>
        protected readonly SEE.Utils.ILogger logger;
        /// <summary>
        /// A mapping of the IDs of all gameNodes onto the gameNodes. This 
        /// mapping allows us to quickly identify the nodes by their IDs.
        /// </summary>
        protected readonly Dictionary<string, IGameNode> gameNodes;

        /// <summary>
        /// Thrown in case of malformed GVL.
        /// </summary>
        [Serializable]
        public class SyntaxError : Exception
        {
            public SyntaxError()
            {
            }

            public SyntaxError(string message)
                : base(message)
            {
            }

            public SyntaxError(string message, Exception inner)
                : base(message, inner)
            {
            }
        }

        protected virtual void LogDebug(string message)
        {
            if (logger != null)
            {
                logger.LogDebug(message);
            }
        }

        protected virtual void LogError(string message)
        {
            if (logger != null)
            {
                IXmlLineInfo xmlInfo = reader;
                int lineNumber = xmlInfo.LineNumber - 1;
                logger.LogError(filename + ":" + lineNumber + ": " + message + "\n");
            }
        }

        protected virtual void LogWarning(string message)
        {
            if (logger != null)
            {
                IXmlLineInfo xmlInfo = reader;
                int lineNumber = xmlInfo.LineNumber - 1;
                logger.LogWarning(filename + ":" + lineNumber + ": " + message + "\n");
            }
        }

        /// <summary>
        /// Checks whether the XML closing element is as expected according to the 
        /// current context.
        /// </summary>
        private void Expected()
        {
            State actual = ToState(reader.Name);
            State expected = context.Pop();

            if (actual != expected)
            {
                LogError("syntax error: <\\" + ToString(expected) + "> expected. Actual: " + reader.Name);
                throw new SyntaxError("mismatched tags");
            }
        }

        /// <summary>
        /// The different XML elements we may be traversing.
        /// </summary>
        protected enum State
        {
            undefined = 0,
            inLayout = 1,
            inVisualization = 2,
            inHiddenNodeTypes = 3,
            inNode = 4,
            inOption = 5,
        }

        /// <summary>
        /// Returns the XML element name for the given <paramref name="state"/>.
        /// This value is used in the GVL file.
        /// </summary>
        /// <param name="state">what kind of GVL element we are currently in</param>
        /// <returns></returns>
        protected static string ToString(State state)
        {
            switch (state)
            {
                case State.undefined: return "undefined";
                case State.inLayout: return "Gravis2_Layout";
                case State.inVisualization: return "Visualization";
                case State.inNode: return "Node";
                case State.inHiddenNodeTypes: return "Hidden_Node_Types";
                case State.inOption: return "Option";
                default: throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Yields the state corresponding to the given GVL element <paramref name="name"/>.
        /// The <paramref name="name"/> is used in the GVL file.
        /// </summary>
        /// <param name="name">name of a GVL element</param>
        /// <returns></returns>
        protected static State ToState(string name)
        {
            if (name == "Gravis2_Layout")
            {
                return State.inLayout;
            }
            else if (name == "Visualization")
            {
                return State.inVisualization;
            }
            else if (name == "Node")
            {
                return State.inNode;
            }
            else if (name == "Hidden_Node_Types")
            {
                return State.inHiddenNodeTypes;
            }
            else if (name == "Option")
            {
                return State.inOption;
            }
            else
            {
                return State.undefined;
            }
        }

        /// <summary>
        /// Walks through the GVL nested elements and gathers the layout data.
        /// Updates gameNodes accordingly. This method implements the traversal,
        /// the actual handling of the elements is deferred to the called 
        /// methods (see below).
        /// </summary>
        protected virtual void Load()
        {
            try
            {
                while (reader.Read())
                {
                    // LogDebug("XML processing: name=" + reader.Name + " nodetype=" + reader.NodeType + " value=" + reader.Value + "\n");

                    // See https://docs.microsoft.com/de-de/dotnet/api/system.xml.xmlnodetype?view=netframework-4.8
                    // for information on the XML reader.
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            // An element(for example, <item> ).
                            {
                                State state = ToState(reader.Name);
                                if (!reader.IsEmptyElement)
                                {
                                    // This is not a self-closing (empty) element, e.g., <item/>. 
                                    // Note: A corresponding EndElement node is not generated for empty elements. 
                                    // That is why we must push an expected EndElement onto the context stack
                                    // only if the element is not self-closing.
                                    context.Push(state);
                                }
                                switch (state)
                                {
                                    case State.undefined: StartUndefined(); break;
                                    case State.inLayout: StartLayout(); break;
                                    case State.inHiddenNodeTypes: StartHiddenNodeTypes(); break;
                                    case State.inNode: StartNode(); break;
                                    case State.inVisualization: StartVisualization(); break;
                                    case State.inOption: StartOption(); break;
                                    default: throw new NotImplementedException();
                                }
                            }
                            break;
                        case XmlNodeType.Text:
                            // The text content of a node. (e.g., "this text" in <item>this text</item>
                            break;
                        case XmlNodeType.EndElement:
                            // An end element tag (for example, </item> ).
                            Expected();
                            switch (ToState(reader.Name))
                            {
                                case State.undefined: EndUndefined(); break;
                                case State.inLayout: EndLayout(); break;
                                case State.inHiddenNodeTypes: EndHiddenNodeTypes(); break;
                                case State.inNode: EndNode(); break;
                                case State.inVisualization: EndVisualization(); break;
                                case State.inOption: EndOption(); break;
                                default: throw new NotImplementedException();
                            }
                            break;
                        case XmlNodeType.None:
                            // This is returned by the XmlReader if a Read method has not been called.
                            break;
                        case XmlNodeType.Attribute:
                            // An attribute (for example, id='123').
                            break;
                        case XmlNodeType.CDATA:
                            // A CDATA section (for example, <![CDATA[my escaped text]]>).
                            break;
                        case XmlNodeType.EntityReference:
                            // A reference to an entity (for example, &num;).
                            break;
                        case XmlNodeType.Entity:
                            // An entity declaration (for example, <!ENTITY...> ).
                            break;
                        case XmlNodeType.ProcessingInstruction:
                            // A processing instruction (for example, <?pi test?>).
                            break;
                        case XmlNodeType.Comment:
                            //  A comment (for example, <!--my comment--> ).
                            break;
                        case XmlNodeType.Document:
                            // A document object that, as the root of the document tree, provides access to the entire XML document.
                            break;
                        case XmlNodeType.DocumentType:
                            // The document type declaration, indicated by the following tag (for example, <!DOCTYPE...>).
                            break;
                        case XmlNodeType.DocumentFragment:
                            // A document fragment.
                            break;
                        case XmlNodeType.Notation:
                            // A notation in the document type declaration (for example, <!NOTATION...>).
                            break;
                        case XmlNodeType.Whitespace:
                            // White space between markup.
                            break;
                        case XmlNodeType.SignificantWhitespace:
                            // White space between markup in a mixed content model or white space within the xml:space = "preserve" scope.
                            break;
                        case XmlNodeType.EndEntity:
                            // Returned when XmlReader gets to the end of the entity replacement as a result of a call to ResolveEntity().
                            break;
                        case XmlNodeType.XmlDeclaration:
                            // The XML declaration (for example, <?xml version='1.0'?>).
                            break;
                        default:
                            LogDebug("unparsed");
                            break;
                    }
                }
            }
            finally
            {
                reader.Close();
            }
            if (context.Count > 0)
            {
                LogError("XML parser is still expecting input in state " + context.Peek());
                throw new SyntaxError("missing closing " + ToString(context.Peek()) + " tag");
            }
        }

        private void EndOption()
        {
            // will be ignored
        }

        private void StartOption()
        {
            // will be ignored
        }

        protected virtual void StartLayout()
        {
            // will be ignored
        }

        protected virtual void EndLayout()
        {
            // will be ignored
        }

        protected virtual void StartVisualization()
        {
            // will be ignored
        }

        protected virtual void EndVisualization()
        {
            // will be ignored
        }

        private void StartHiddenNodeTypes()
        {
            // will be ignored
        }


        protected virtual void EndHiddenNodeTypes()
        {
            // will be ignored
        }

        protected virtual void StartNode()
        {
            // Here is an example how layout information of nodes
            // is represented in GVL:
            //
            // <Node Id="L1" X="-1360" Y="-635" W="246" H="147" CS="14.14" Exp="True">
            //   <Node Id="L2" X="45" Y="97" W="0" H="0" CS="10">
            //   </Node>
            // </Node>
            //
            // The nesting of nodes is expressed as nested XML node
            // elements in GVL. Every node has a unique Id, which is
            // a node's linkname with a leading L or the node's 
            // source name with a leading S. This way, the node of a 
            // graph from a GXL file and its layout information from
            // a GVL file are connected.
            // 
            // In the example, we have a node with Id "1" and another
            // node with Id "2" where "2" is nested in "1".
            //
            // Gravis has a two-dimensional canvas. The two axes are 
            // named X (horizontal) and Y (vertical), respectively.
            // While Gravis's X axis increases from left to right, 
            // Gravis's Y axis increases from top to bottom. That is,
            // the orientation of Gravis's and Unity's x axes conform
            // to each other, Gravis's Y axis and the corresponding 
            // Z axis in Unity are inverse to each other.
            //
            // All nodes are logical rectangles with respect to the 
            // layout. That is, they have two co-ordinates and a
            // width and height.
            //
            // The X and Y co-ordinates of a node refer to the left
            // upper corner of the node. For root nodes, they are 
            // relative to a unique point of reference of the canvas in 
            // dots. This point of reference is arbitrary but the same
            // for all root nodes on the canvas. Negative co-ordinates are possible.
            // For nodes nested in other nodes, X and Y denote the offset
            // to the X and Y co-ordinates of their containing 
            // node in dot. These can never be negative.
            //
            // In the example above, the composite node "1" is located at
            // (-1360, -633). The nested node "2" has the offset (45, 97), 
            // thus, is located at (-1360+45, -633+97).
            //
            // Every node (composite or not) may or may not be expanded indicated
            // by the attribute "Exp". If the attribute value is false or missing 
            // altogether, the node is not expanded. 
            //
            // For instance, node "1" is expanded, but "2" is not.
            //
            // A node has two different appearances depending upon whether
            // it is expanded or not. If a node is not expanded, only an
            // icon is shown. If a node is expanded, the icon and a rectangle
            // are shown in which the children are nested. 
            // The two attributes W and H specify the width and height of
            // this rectangle in dots. These size measures apply when the node is
            // expanded. The size of the icon is specified by the attribute
            // CS (for "child size"). Every node has such an attribute, but
            // that value applies only to its immediate children. That is
            // why it is called "child size" in the first place. In other
            // words, the size of the icon of a node is the value of the 
            // attribute CS of the node immediately containing it.
            // 
            // In the example above, the expanded composite node "1" has width 246 
            // and height 147, whereas the unexpanded leaf node "2" has width
            // and height 14.14 (all values in dots).

            string ID = GetID(reader); // mandatory

            if (!gameNodes.TryGetValue(ID, out IGameNode gameNode))
            {
                LogWarning(String.Format("Unknown id {0}", ID));
                Debug.LogError(String.Format("Unknown id {0}", ID));
                // We create a new game node so that we can continue as normal.
                gameNode = new LayoutVertex(ID);
            }
            float X = GetFloat(reader, "X");   // mandatory x co-ordinate
            float Y = GetFloat(reader, "Y");   // mandatory y co-ordinate; Gravis's Y is Unity's inverted z axis
            float W = GetFloat(reader, "W");   // mandatory width
            float H = GetFloat(reader, "H");   // mandatory height

            ParentNode parent = nodes.Peek();
            float CS = parent.CS; // icon size for this node
            if (!reader.IsEmptyElement)
            {
                // push current gameNode along with the mandatory icon size for its children
                // onto the nodes stack but only if this XML element is not self-closing.
                // For a self-closing element <Node .... />, no corresponding EndNode()
                // will be called to pop off this element.
                nodes.Push(new ParentNode(GetFloat(reader, "CS"), gameNode));
            }
            bool Exp = reader.GetAttribute("Exp") == "True"; // optional expansion

            // The resulting scale of the node; the y co-ordinate remains zero.
            Vector3 scale = Vector3.zero;
            scale.y = gameNode.AbsoluteScale.y; // We maintain the original height of the node.
            if (Exp && W != 0 && H != 0)
            {
                scale.x = W;
                scale.z = H; // Gravis's height is Unity's z axis
            }
            else
            {
                scale.x = CS;
                scale.z = CS;
            }

            // The resulting position of the node; the y co-ordinate remains zero.
            Vector3 position = Vector3.zero;
            // (X, Y) define the left upper corner. In Unity, we use the center of a node.
            // If this node is a root, (X, Y) relates to a common reference point on 
            // the canvas. If this node is contained in another node, they are an offset
            // to their parent node.
            if (parent.gameNode != null)
            {
                // Node is a nested node, that is, not at top-level.
                // (X, Y) denote the offset of the nested node to its parent's left upper corner
                Vector3 parentPosition = parent.gameNode.CenterPosition; // world space in Unity
                Vector3 parentScale = parent.gameNode.AbsoluteScale;     // world space in Unity
                //Debug.LogFormat("parent {0} center position={1} absolute scale={2}\n", parent.gameNode.ID, parentPosition, parentScale);
                // Transform parent's center position to its left upper corner.
                parentPosition.x -= parentScale.x / 2.0f;
                parentPosition.z += parentScale.z / 2.0f;
                //Debug.LogFormat("parent {0} left upper corner={1} absolute scale={2}\n", parent.gameNode.ID, parentPosition, parentScale);
                // Calculate left upper corner of node.
                position.x = parentPosition.x + X;
                position.z = parentPosition.z - Y; // Gravis's Y axis is inverse to Unity's z axis

                //Debug.LogFormat("child {0}'s left upper corner ({1}, {2}) where X={3} Y={4})\n",
                //                ID, position.x, position.z, X, Y);
                // position must refer to the center of the node.
                position.x += scale.x / 2.0f;
                //position.y =  scale.y / 2.0f; // lift y center so that the node stants on ground zero
                position.z -= scale.z / 2.0f;
                //Debug.LogFormat("child {0}'s center position ({1}, {2})\n", ID, position.x, position.z);

                // Lift y center so that the node stands on its parent's roof.
                position.y = (parentPosition.y + parentScale.y / 2.0f) + Mathf.Max(MinimalHeight, scale.y) / 2.0f;
            }
            else
            {
                // Node is a root node.
                // assert: (X, Y) relates to absolute world space of the left upper corner
                // Transform to center position for Unity.
                position.x = X + scale.x / 2.0f;
                // Lift y center so that the node stands on ground zero.
                position.y = groundLevel + Mathf.Max(MinimalHeight, scale.y) / 2.0f;
                // Gravis's Y is Unity's inverted z axis, i.e., we need to mirror Y
                // as follows: z = -Y. By mirroring Y, the left upper corner of a node
                // becomes its left lower corner.
                position.z = -Y - scale.z / 2.0f;

                //Debug.LogFormat("root {0}'s center position ({1}, {2}) with size {3} where X={4} Y={5}\n", 
                //                ID, position.x, position.z, scale, X, Y);
            }

            // Although we assign the local scale here, the node is not yet contained in any
            // other node, in which case local scale and world-space scale are the same.
            // Nodes will be nested later by the client of this layout.
            gameNode.LocalScale = scale;
            gameNode.CenterPosition = position;

            //Debug.LogFormat("result for node {0} center={1} left upper corner={2} scale={3}\n", 
            //                gameNode.ID, gameNode.CenterPosition, ToLeftUpperCorner(gameNode.CenterPosition, gameNode.LocalScale), gameNode.LocalScale);
        }

        /// <summary>
        /// Returns the float value of the given XML <paramref name="attribute"/>.
        /// If no such attribute exists, a syntax error is thrown.
        /// 
        /// Precondition: The value of <paramref name="attribute"/> must be a float.
        /// </summary>
        /// <param name="reader">reader processing the XML data</param>
        /// <param name="attribute">name of the float attribute</param>
        /// <returns>float value of the given XML <paramref name="attribute"/></returns>
        private float GetFloat(XmlTextReader reader, string attribute)
        {
            string value = reader.GetAttribute(attribute);
            if (value.Length == 0)
            {
                throw new SyntaxError("Node does not have an attribute " + attribute + ".");
            }
            return ToFloat(value);
        }

        /// <summary>
        /// Returns the value of the Id attribute. If no such attribute exists, a 
        /// syntax error is thrown.
        /// 
        /// </summary>
        /// <param name="reader">reader processing the XML data</param>
        /// <returns>value of the Id attribute</returns>
        private string GetID(XmlTextReader reader)
        {
            string ID = reader.GetAttribute("Id");
            if (ID.Length == 0)
            {
                throw new SyntaxError("Node does not have an Id.");
            }
            // The first letter is either an 'S' for Source.Name or
            // an 'L' for Linkage.Name. It will be ignored.
            return ID.Substring(1);
        }

        protected virtual void EndNode()
        {
            nodes.Pop();
        }

        protected virtual void StartUndefined()
        {

        }

        protected virtual void EndUndefined()
        {

        }

        /// <summary>
        /// Returns the <paramref name="value"/> as a float.
        /// 
        /// Precondition: <paramref name="value"/> must conform to the
        /// CultureInfo.InvariantCulture.NumberFormat for floats.
        /// </summary>
        /// <param name="value">float value as a string</param>
        /// <returns>float value</returns>
        private static float ToFloat(string value)
        {
            return float.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
        }
    }
}