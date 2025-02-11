using SEE.Tools.ReflexionAnalysis;
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
                throw new System.Exception("Graph has more than one root node.");
            }
            ICollection<ILayoutNode> children = roots[0].Children();
            if (children.Count != 2)
            {
                throw new System.Exception("Root node has not exactly two children.");
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
                    throw new System.Exception("Root node has a child that is neither architecture nor implementation.");
                }
            }
            if (architectureRoot == null)
            {
                throw new System.Exception("Root node has no architecture child.");
            }
            if (implementationRoot == null)
            {
                throw new System.Exception("Root node has no implementation child.");
            }
            if (architectureRoot == implementationRoot)
            {
                throw new System.Exception("Root node has the two children that are both architecture or implementation, respectively.");
            }

            // The available space is retrieved from the root node.
            Split(centerPosition, rectangle.x, rectangle.y, 0.6f, out Area implementionArea, out Area architectureArea); // FIXME: 0.6f is a placeholder

            architectureArea.ApplyTo(architectureRoot);
            implementionArea.ApplyTo(implementationRoot);

            ICollection<ILayoutNode> implementationNodes = ILayoutNodeHierarchy.DescendantsOf(implementationRoot);
            Dictionary<ILayoutNode, NodeTransform> result
                = implementationLayout.Create(implementationNodes,
                                              implementationRoot.CenterPosition,
                                              new Vector2(implementionArea.Width, implementionArea.Depth));
            Debug.Log($"implementationLayout.Count= {result.Count}\n");

            ICollection<ILayoutNode> architectureNodes = ILayoutNodeHierarchy.DescendantsOf(architectureRoot);

            Union(result, architectureLayout.Create(architectureNodes,
                                                    architectureRoot.CenterPosition,
                                                    new Vector2(architectureArea.Width, architectureArea.Depth)));

            result[roots[0]] = new NodeTransform(centerPosition.y, centerPosition.z, new Vector3(rectangle.x, roots[0].AbsoluteScale.y, rectangle.y));
            return result;

            static void Union(Dictionary<ILayoutNode, NodeTransform> result, Dictionary<ILayoutNode, NodeTransform> layout)
            {
                Debug.Log($"architectureLayout.Count= {layout.Count}\n");
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
                layoutNode.LocalScale = new Vector3(Width, layoutNode.AbsoluteScale.y, Depth);
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

#if false

        /// <summary>
        /// Draws <paramref name="graph"/>.
        /// Precondition: The <paramref name="graph"/> and its metrics have been loaded.
        /// </summary>
        /// <param name="graph">graph to be drawn</param>
        /// <param name="codeCity">the game object representing the code city and holding
        /// a <see cref="SEEReflexionCity"/> component</param>
        protected async UniTaskVoid RenderReflexionGraphAsync(ReflexionGraph graph, GameObject codeCity)
        {
            if (codeCity.TryGetComponent(out SEEReflexionCity reflexionCity))
            {
                // The original real-world position and scale of codeCity.
                Area codeCityArea = new(codeCity.CenterPosition, codeCity.AbsoluteScale);

                try
                {
                    using (LoadingSpinner.ShowDeterminate($"Drawing reflexion city \"{codeCity.name}\"", out Action<float> updateProgress))
                    {
                        void ReportProgress(float x)
                        {
                            ProgressBar = x;
                            updateProgress(x);
                        }

                        (Graph implementation, Graph architecture, _) = graph.Disassemble();

                        // There should be no more than one root.
                        Node reflexionRoot = graph.GetRoots().FirstOrDefault();

                        // There could be no root at all in case the architecture and implementation
                        // graphs are both empty.
                        if (reflexionRoot != null)
                        {
                            Split(codeCity, reflexionCity.ArchitectureLayoutProportion,
                                  out Area implementionArea, out Area architectureArea);

                            // The parent of the two game object hierarchies for the architecture and implementation.
                            GameObject reflexionCityRoot;

                            // Draw implementation.
                            {
                                GraphRenderer renderer = new(this, implementation);
                                // reflexionCityRoot will be the direct and only child of gameObject
                                reflexionCityRoot = renderer.DrawNode(reflexionRoot, codeCity);
                                codeCityArea.ApplyTo(reflexionCityRoot);

                                Debug.Log($"[reflexionCityRoot] position={reflexionCityRoot.CenterPosition} lossyScale={reflexionCityRoot.AbsoluteScale}\n");

                                reflexionCityRoot.transform.SetParent(codeCity.transform);

                                /*
                                implementionArea.ApplyTo(codeCity);
                                await renderer.DrawGraphAsync(implementation, codeCity, ReportProgress, cancellationTokenSource.Token);
                                RestoreCodeCity();
                                */
                            }
                            /*


                            // Draw implementation.
                            {
                                GraphRenderer renderer = new(this, implementation);



                                // Render the implementation graph under reflexionCityRoot.
                                await renderer.DrawGraphAsync(implementation, reflexionCityRoot, ReportProgress, cancellationTokenSource.Token);
                            }

                            // We need to temporarily unlink the implementation graph from reflexionCityRoot
                            // because graph renderering assumes that the parent has no other child.
                            GameObject implementationRoot = reflexionCityRoot.transform.GetChild(0).gameObject;
                            implementationRoot.transform.SetParent(null);

                            // Draw architecture.
                            {
                                GraphRenderer renderer = new(this, architecture);
                                await renderer.DrawGraphAsync(architecture, reflexionCityRoot, ReportProgress, cancellationTokenSource.Token);
                            }

                            implementationRoot.transform.SetParent(reflexionCityRoot.transform);
                            */
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    ShowNotification.Warn("Drawing cancelled", "Drawing was cancelled.\n", log: true);
                    throw;
                }
                finally
                {
                    RestoreCodeCity();
                }

                return;

                // Restores codeCity to its original osition and scale.
                void RestoreCodeCity()
                {
                    codeCityArea.ApplyTo(codeCity);
                }
#endif

}