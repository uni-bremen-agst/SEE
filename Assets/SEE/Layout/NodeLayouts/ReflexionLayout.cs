using SEE.Tools.ReflexionAnalysis;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout.NodeLayouts
{
    public class ReflexionLayout : NodeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="architectureLayout">the layout to applied to the architecture nodes</param>
        /// <param name="implementationLayout">the layout to applied to the implementation nodes</param>
        public ReflexionLayout(NodeLayout implementationLayout = null, NodeLayout architectureLayout = null)
        {
            this.implementationLayout = implementationLayout ?? new CirclePackingNodeLayout();
            this.architectureLayout = architectureLayout ?? new CirclePackingNodeLayout();
        }

        static ReflexionLayout()
        {
            Name = "Reflexion";
        }

        private readonly NodeLayout implementationLayout;
        private readonly NodeLayout architectureLayout;

        protected override Dictionary<ILayoutNode, NodeTransform> Layout
            (IEnumerable<ILayoutNode> layoutNodes,
            Vector3 centerPosition,
            Vector2 rectangle)
        {
            // There should be one root that has exactly two children. One of them is the architecture
            // and the other is the implementation. The architecture node has the node type Architecture
            // and the implementation node has the node type Implementation.

            IList<ILayoutNode> roots = LayoutNodes.GetRoots(layoutNodes);
            if (roots.Count == 0)
            {
                // Nothing to be laid out.
                return new Dictionary<ILayoutNode, NodeTransform>();
            }
            if (roots.Count > 1)
            {
                throw new Exception("Graph has more than one root node.");
            }
            ICollection<ILayoutNode> children = roots[0].Children();
            if (children.Count != 2)
            {
                throw new Exception("Root node has not exactly two children.");
            }
            ILayoutNode architectureRoot = null;
            ILayoutNode implementationRoot = null;

            // We cannot use indexing, hence, we need to iterate over the children.
            foreach (ILayoutNode child in children)
            {
                if (child.HasType(ReflexionGraph.ArchitectureType))
                {
                    architectureRoot = child;
                }
                else if (child.HasType(ReflexionGraph.ImplementationType))
                {
                    implementationRoot = child;
                }
                else
                {
                    throw new Exception("Root node has a child that is neither architecture nor implementation.");
                }
            }
            if (architectureRoot == null)
            {
                throw new Exception("Root node has no architecture child.");
            }
            if (implementationRoot == null)
            {
                throw new Exception("Root node has no implementation child.");
            }
            if (architectureRoot == implementationRoot)
            {
                throw new Exception("Root node has the two children that are both architecture or implementation, respectively.");
            }

            // The available space is retrieved from the root node.
            Split(centerPosition, rectangle.x, rectangle.y, 0.6f, out Area implementionArea, out Area architectureArea); // FIXME: 0.6f is a placeholder

            roots[0].RemoveChild(architectureRoot);
            roots[0].RemoveChild(implementationRoot);

            ICollection<ILayoutNode> implementationNodes = ILayoutNodeHierarchy.DescendantsOf(implementationRoot);
            Dictionary<ILayoutNode, NodeTransform> result
                = implementationLayout.Create(implementationNodes,
                                              implementionArea.Position,
                                              new Vector2(implementionArea.Width, implementionArea.Depth));

            ICollection<ILayoutNode> architectureNodes = ILayoutNodeHierarchy.DescendantsOf(architectureRoot);

            Union(result, architectureLayout.Create(architectureNodes,
                                                    architectureArea.Position,
                                                    new Vector2(architectureArea.Width, architectureArea.Depth)));

            roots[0].AddChild(architectureRoot);
            roots[0].AddChild(implementationRoot);

            result[roots[0]] = new NodeTransform(centerPosition.x, centerPosition.z, new Vector3(rectangle.x, roots[0].AbsoluteScale.y, rectangle.y));
            return result;

            static void Union(Dictionary<ILayoutNode, NodeTransform> result, Dictionary<ILayoutNode, NodeTransform> layout)
            {
                foreach (KeyValuePair<ILayoutNode, NodeTransform> entry in layout)
                {
                    result[entry.Key] = entry.Value;
                }
            }
        }

        /// <summary>
        /// Represents a plane in 3D space where to draw a code city for reflexion analysis,
        /// that is, an area for the implementation city or the architecture city.
        /// </summary>
        struct Area
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="position">The center point of the area in world space.</param>
            /// <param name="width">The width of the area in world space.</param>
            /// <param name="depth">The depth of the area in world space.</param>
            internal Area(Vector3 position, float width, float depth)
            {
                Position = position;
                Width = width;
                Depth = depth;
            }
            /// <summary>
            /// The center point of the area in world space.
            /// </summary>
            internal Vector3 Position;
            /// <summary>
            /// The width of the area in world space.
            /// </summary>
            internal float Width;
            /// <summary>
            /// The depth of the area in world space.
            /// </summary>
            internal float Depth;

            /// <summary>
            /// Applies the area to the given <paramref name="layoutNode"/>.
            /// After this method, the position and scale of <paramref name="layoutNode"/>
            /// will be the same as the position and scale of this area (in world space).
            /// </summary>
            /// <param name="layoutNode">the object to which apply the area to</param>
            internal readonly void ApplyTo(ILayoutNode layoutNode)
            {
                layoutNode.CenterPosition = Position;
                layoutNode.AbsoluteScale = new Vector3(Width, layoutNode.AbsoluteScale.y, Depth);
            }

            internal void Draw(string name)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.position = Position;
                go.transform.localScale = new Vector3(Width, 0.01f, Depth);
            }
        }

        /// <summary>
        /// Splits the code city into two areas - one for the implementation and one for the
        /// architecture - along the longer edge of the code city in the proportion of
        /// architectureLayoutProportion.
        ///
        /// The area of the implementation is returned in implemenationArea and the area of the
        /// architecture in architectureArea.
        ///
        /// The edge length of architectureArea is the length of the longer edge of code city multiplied
        /// by architectureLayoutProportion.
        ///
        /// implementationArea and architectureArea together occupy exactly the space of codeCity.
        ///
        /// architectureLayoutProportion is assumed to be in [0, 1]. If architectureLayoutProportion
        /// is 0 or less, the implementation takes all the space and the architecture area sits at the
        /// end of the longer edge of the implementation with zero scale. If architectureLayoutProportion
        /// is 1 or greater, the architecture takes all the space and the implementation area sits at the
        /// begin of the longer edge of the architecture with zero scale.
        /// </summary>
        private static void Split(Vector3 centerPosition,
                                  float width,
                                  float depth,
                                  float architectureLayoutProportion,
                                  out Area implementionArea,
                                  out Area architectureArea)
        {
            bool xIsLongerEdge = width >= depth;

            if (architectureLayoutProportion <= 0)
            {
                // the implemenation takes all the available space
                implementionArea = new(centerPosition, width, depth);
                // the architecture sits at the end of the longer edge of the implementation with zero space
                Vector3 architecturePos = implementionArea.Position;
                if (xIsLongerEdge)
                {
                    architecturePos.x = implementionArea.Position.x + implementionArea.Width / 2;
                }
                else
                {
                    architecturePos.z = implementionArea.Position.z + implementionArea.Depth / 2;
                }
                architectureArea = new(architecturePos, 0, 0);
            }
            else if (architectureLayoutProportion >= 1)
            {
                // the architecture takes all the available space
                architectureArea = new(centerPosition, width, depth);
                // the implementation sits at the begin of the longer edge of the architecture with zero space
                Vector3 implementationPos = architectureArea.Position;
                if (xIsLongerEdge)
                {
                    implementationPos.x = architectureArea.Position.x - architectureArea.Width / 2;
                }
                else
                {
                    implementationPos.z = architectureArea.Position.z - architectureArea.Depth / 2;
                }
                implementionArea = new(implementationPos, 0, 0);
            }
            else
            {
                if (xIsLongerEdge)
                {
                    // The reference point from which to start laying out the areas.
                    Vector3 referencePoint = centerPosition;
                    // The mid point of the left edge of the code city.
                    referencePoint.x -= width / 2;

                    // The implementationArea.
                    {
                        float length = width * (float)(1 - architectureLayoutProportion);
                        Vector3 position = referencePoint;
                        position.x += length / 2;
                        implementionArea = new(position, length, depth);
                        // Move the reference point to the right end of the implementation area.
                        referencePoint.x += length;
                    }

                    // The architectureArea.
                    {
                        float length = width * architectureLayoutProportion;
                        Vector3 position = referencePoint;
                        position.x += length / 2;
                        architectureArea = new(position, length, depth);
                    }
                }
                else
                {
                    // The reference point from which to start laying out the areas.
                    Vector3 referencePoint = centerPosition;
                    // The mid point of the lower edge of the code city.
                    referencePoint.z -= depth / 2;

                    // The implementationArea.
                    {
                        float length = depth * (float)(1 - architectureLayoutProportion);
                        Vector3 position = referencePoint;
                        position.z += length / 2;
                        implementionArea = new(position, width, length);
                        // Move the reference point to the lower end of the implementation area.
                        referencePoint.z += length;
                    }

                    // The architectureArea.
                    {
                        float length = depth * architectureLayoutProportion;
                        Vector3 position = referencePoint;
                        position.z += length / 2;
                        architectureArea = new(position, width, length);
                    }
                }
            }
            //implementionArea.Draw("implementation"); // FIXME: Remove this line.
            //architectureArea.Draw("architecture");
        }
    }
}