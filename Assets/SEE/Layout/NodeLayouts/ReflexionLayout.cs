using SEE.Tools.ReflexionAnalysis;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts
{
    public class ReflexionLayout : NodeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
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

        public override Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> layoutNodes, Vector2 rectangle)
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
            Split(roots[0], 0.6f, out Area implementionArea, out Area architectureArea); // FIXME: 0.5f is a placeholder

            implementationRoot.Parent = null;
            architectureRoot.Parent = null;

            ICollection<ILayoutNode> implementationNodes = ILayoutNodeHierarchy.DescendantsOf(implementationRoot);
            Debug.Log($"implementationNodes.Count= {implementationNodes.Count}\n");
            Dictionary<ILayoutNode, NodeTransform> result = implementationLayout.Layout(implementationNodes, rectangle);

            ICollection<ILayoutNode> architectureNodes = ILayoutNodeHierarchy.DescendantsOf(architectureRoot);
            Debug.Log($"architectureNodes.Count= {architectureNodes.Count}\n");

            implementationRoot.Parent = roots[0];
            architectureRoot.Parent = roots[0];

            result.Union(architectureLayout.Layout(architectureNodes, rectangle)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return result;
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
            /// <param name="scale">The scale of the area in world space.</param>
            internal Area(Vector3 position, Vector3 scale)
            {
                Position = position;
                Scale = scale;
            }
            /// <summary>
            /// The center point of the area in world space.
            /// </summary>
            internal Vector3 Position;
            /// <summary>
            /// The scale of the area in world space. Only its x and z components
            /// are relevant.
            /// </summary>
            internal Vector3 Scale;

            /// <summary>
            /// Applies the area to the given <paramref name="layoutNode"/>.
            /// After this method, the position and scale of <paramref name="layoutNode"/>
            /// will be the same as the position and scale of this area (in world space).
            /// </summary>
            /// <param name="layoutNode">the object to which apply the area to</param>
            //internal readonly void ApplyTo(ILayoutNode layoutNode)
            //{
            //    layoutNode.CenterPosition = Position;
            //    layoutNode.SetAbsoluteScale(Scale, false);
            //}
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
        void Split(ILayoutNode root, float architectureLayoutProportion,
            out Area implementionArea, out Area architectureArea)
        {
            bool xIsLongerEdge = root.AbsoluteScale.x >= root.AbsoluteScale.z;

            if (architectureLayoutProportion <= 0)
            {
                // the implemenation takes all the available space
                implementionArea = new(root.CenterPosition, root.AbsoluteScale);
                // the architecture sits at the end of the longer edge of the implementation with zero space
                Vector3 architecturePos = implementionArea.Position;
                if (xIsLongerEdge)
                {
                    architecturePos.x = implementionArea.Position.x + implementionArea.Scale.x / 2;
                }
                else
                {
                    architecturePos.z = implementionArea.Position.z + implementionArea.Scale.z / 2;
                }
                architectureArea = new(architecturePos, Vector3.zero);
            }
            else if (architectureLayoutProportion >= 1)
            {
                // the architecture takes all the available space
                architectureArea = new(root.CenterPosition, root.AbsoluteScale);
                // the implementation sits at the begin of the longer edge of the architecture with zero space
                Vector3 implementationPos = architectureArea.Position;
                if (xIsLongerEdge)
                {
                    implementationPos.x = architectureArea.Position.x - architectureArea.Scale.x / 2;
                }
                else
                {
                    implementationPos.z = architectureArea.Position.z - architectureArea.Scale.z / 2;
                }
                implementionArea = new(implementationPos, Vector3.zero);
            }
            else
            {
                if (xIsLongerEdge)
                {
                    // The reference point from which to start laying out the areas.
                    Vector3 referencePoint = root.CenterPosition;
                    // The mid point of the left edge of the code city.
                    referencePoint.x -= root.AbsoluteScale.x / 2;

                    // The implementationArea.
                    {
                        float length = root.AbsoluteScale.x * (float)(1 - architectureLayoutProportion);
                        Vector3 position = referencePoint;
                        position.x += length / 2;
                        Vector3 scale = new(length, root.AbsoluteScale.y, root.AbsoluteScale.z);
                        implementionArea = new(position, scale);
                        // Move the reference point to the right end of the implementation area.
                        referencePoint.x += length;
                    }

                    // The architectureArea.
                    {
                        float length = root.AbsoluteScale.x * architectureLayoutProportion;
                        Vector3 position = referencePoint;
                        position.x += length / 2;
                        Vector3 scale = new(length, root.AbsoluteScale.y, root.AbsoluteScale.z);
                        architectureArea = new(position, scale);
                    }
                }
                else
                {
                    // The reference point from which to start laying out the areas.
                    Vector3 referencePoint = root.CenterPosition;
                    // The mid point of the lower edge of the code city.
                    referencePoint.z -= root.AbsoluteScale.z / 2;

                    // The implementationArea.
                    {
                        float length = root.AbsoluteScale.z * (float)(1 - architectureLayoutProportion);
                        Vector3 position = referencePoint;
                        position.z += length / 2;
                        Vector3 scale = new(root.AbsoluteScale.x, root.AbsoluteScale.y, length);
                        implementionArea = new(position, scale);
                        // Move the reference point to the lower end of the implementation area.
                        referencePoint.z += length;
                    }

                    // The architectureArea.
                    {
                        float length = root.AbsoluteScale.z * architectureLayoutProportion;
                        Vector3 position = referencePoint;
                        position.z += length / 2;
                        Vector3 scale = new(root.AbsoluteScale.x, root.AbsoluteScale.y, length);
                        architectureArea = new(position, scale);
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