using SEE.Tools.ReflexionAnalysis;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout.NodeLayouts
{
    /// <summary>
    /// A layout for the reflexion analysis of a code city where there are two sublayouts,
    /// one for the architecture and one for the implementation.
    /// </summary>
    public class ReflexionLayout : NodeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="architectureProportion">the proportion of the longer edge of the available space that
        /// is occupied by the architecture; must be in [0, 1]</param>
        /// <param name="implementationLayout">the layout to applied to the implementation nodes;
        /// if none is given <see cref="TreemapLayout"/> will be used</param>
        /// <param name="architectureLayout">the layout to applied to the architecture nodes;
        /// if none is given <see cref="TreemapLayout"/> will be used</param>
        /// <exception cref="ArgumentException">thrown if <paramref name="architectureProportion"/> is not in [0, 1]</exception>
        public ReflexionLayout(float architectureProportion, NodeLayout implementationLayout = null, NodeLayout architectureLayout = null)
        {
            if (architectureProportion < 0 || architectureProportion > 1)
            {
                throw new ArgumentException("Architecture proportion must be in [0, 1].");
            }
            this.architectureProportion = architectureProportion;
            this.implementationLayout = implementationLayout ?? new TreemapLayout();
            this.architectureLayout = architectureLayout ?? new TreemapLayout();
        }

        static ReflexionLayout()
        {
            Name = "Reflexion";
        }

        /// <summary>
        /// The proportion of the longer edge of the available space that is occupied by
        /// the architecture; must be in [0, 1].
        /// </summary>
        private readonly float architectureProportion;
        /// <summary>
        /// The layout to applied to the implementation nodes.
        /// </summary>
        private readonly NodeLayout implementationLayout;
        /// <summary>
        /// The layout to applied to the architecture nodes.
        /// </summary>
        private readonly NodeLayout architectureLayout;

        /// <summary>
        /// See <see cref="NodeLayout.Layout"/>.
        ///
        /// Preconditions:
        /// There must be only one root node that has exactly two children. One of them is the architecture
        /// and the other is the implementation. The architecture node has the node type
        /// <see cref="ReflexionGraph.ArchitectureType"/> and the implementation node has the node type
        /// <see cref="ReflexionGraph.ImplementationType"/>.
        /// </summary>
        /// <exception cref="ArgumentException">thrown in case the preconditions are not met</exception>
        protected override Dictionary<ILayoutNode, NodeTransform> Layout
            (IEnumerable<ILayoutNode> layoutNodes,
            Vector3 centerPosition,
            Vector2 rectangle)
        {
            // There should be one root that has exactly two children. One of them is the architecture
            // and the other is the implementation.
            IList<ILayoutNode> roots = LayoutNodes.GetRoots(layoutNodes);
            if (roots.Count == 0)
            {
                // Nothing to be laid out.
                return new Dictionary<ILayoutNode, NodeTransform>();
            }
            if (roots.Count > 1)
            {
                throw new ArgumentException("Graph has more than one root node.");
            }
            ICollection<ILayoutNode> children = roots[0].Children();
            if (children.Count != 2)
            {
                throw new ArgumentException("Root node has not exactly two children.");
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
                    throw new ArgumentException("Root node has a child that is neither architecture nor implementation.");
                }
            }
            if (architectureRoot == null)
            {
                throw new ArgumentException("Root node has no architecture child.");
            }
            if (implementationRoot == null)
            {
                throw new ArgumentException("Root node has no implementation child.");
            }
            if (architectureRoot == implementationRoot)
            {
                throw new ArgumentException("Root node has the two children that are both architecture or implementation, respectively.");
            }

            Split(centerPosition, rectangle.x, rectangle.y, architectureProportion, out Area implementionArea, out Area architectureArea);

            // We will temporarily remove the architecture and implementation nodes as children from the root node
            // because the sublayouts would be unable to detect a root.
            roots[0].RemoveChild(architectureRoot);
            roots[0].RemoveChild(implementationRoot);

            // Laying out the implementation.
            ICollection<ILayoutNode> implementationNodes = ILayoutNodeHierarchy.DescendantsOf(implementationRoot);
            Dictionary<ILayoutNode, NodeTransform> result
                = implementationLayout.Create(implementationNodes,
                                              implementionArea.Position,
                                              new Vector2(implementionArea.Width, implementionArea.Depth));

            // Laying out the architecture.
            ICollection<ILayoutNode> architectureNodes = ILayoutNodeHierarchy.DescendantsOf(architectureRoot);

            Union(result, architectureLayout.Create(architectureNodes,
                                                    architectureArea.Position,
                                                    new Vector2(architectureArea.Width, architectureArea.Depth)));

            // Adding the architecture and implementation nodes back as children to the root node.
            roots[0].AddChild(architectureRoot);
            roots[0].AddChild(implementationRoot);

            // The root node was not laid out by the sublayouts, hence, we need to add it manually,
            // occupying the complete available space.
            result[roots[0]] = new NodeTransform(centerPosition.x, centerPosition.z, new Vector3(rectangle.x, roots[0].AbsoluteScale.y, rectangle.y));
            return result;

            // Adds the layout to the result.
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
            /// Draws a cube representing the area. Can be used for debugging.
            /// </summary>
            /// <param name="name">the name of the created cube</param>
            internal readonly void Draw(string name)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.position = Position;
                go.transform.localScale = new Vector3(Width, 0.01f, Depth);
            }
        }

        /// <summary>
        /// Splits the available rectangle for the code city into two areas - one for the
        /// implementation and one for the architecture - along the longer edge of the
        /// rectangle in the proportion of <paramref name="architectureLayoutProportion"/>.
        ///
        /// The rectangle is specified by its center position <paramref name="centerPosition"/>,
        /// and its <paramref name="width"/> and <paramref name="depth"/>.
        ///
        /// The area of the implementation is returned in <paramref name="implementionArea"/>
        /// and the area of the architecture in <paramref name="architectureArea"/>.
        ///
        /// The edge length of <paramref name="architectureArea"/> is the length of the longer edge
        /// of the rectangle multiplied by <paramref name="architectureLayoutProportion"/>.
        ///
        /// <paramref name="implementionArea"/> and <paramref name="architectureArea"/> together
        /// occupy exactly the space of the rectangle.
        ///
        /// <paramref name="architectureLayoutProportion"/> is assumed to be in [0, 1]. If
        /// <paramref name="architectureLayoutProportion"/> is 0 or less, the implementation takes
        /// all the space and the architecture area sits at the end of the longer edge of the
        /// implementation with zero scale. If <paramref name="architectureLayoutProportion"/>
        /// is 1 or greater, the architecture takes all the space and the implementation area sits at the
        /// begin of the longer edge of the architecture with zero scale.
        /// </summary>
        /// <param name="centerPosition">the center position of the rectangle in which to place
        /// the implementation and architecture</param>
        /// <param name="width">the width of the rectangle in which to place them</param>
        /// <param name="depth">the depth of the rectangle in which to place them</param>
        /// <param name="architectureLayoutProportion">the proportion of the longer edge of the available
        /// space that should be allocated for the architecture; must be in [0, 1]</param>
        /// <param name="implementionArea">the resulting area of the implementation</param>
        /// <param name="architectureArea">the resulting area of the architecture</param>
        private static void Split(Vector3 centerPosition,
                                  float width,
                                  float depth,
                                  float architectureLayoutProportion,
                                  out Area implementionArea,
                                  out Area architectureArea)
        {
            if (architectureLayoutProportion <= 0)
            {
                // the implemenation takes all the available space
                implementionArea = new(centerPosition, width, depth);
                // the architecture sits at the end of the longer edge of the implementation with zero space
                Vector3 architecturePos = implementionArea.Position;
                architecturePos.z = implementionArea.Position.z + implementionArea.Depth / 2;
                architectureArea = new(architecturePos, 0, 0);
            }
            else if (architectureLayoutProportion >= 1)
            {
                // the architecture takes all the available space
                architectureArea = new(centerPosition, width, depth);
                // the implementation sits at the begin of the longer edge of the architecture with zero space
                Vector3 implementationPos = architectureArea.Position;
                implementationPos.z = architectureArea.Position.z - architectureArea.Depth / 2;
                implementionArea = new(implementationPos, 0, 0);
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
    }
}
