using DG.Tweening;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.Layout;
using UnityEngine;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// Part of the <see cref="EvolutionRenderer"/> taking care of moving
    /// graph elements existing in the currently shown graph and the next
    /// one to their new position.
    /// </summary>
    public partial class EvolutionRenderer
    {
        /// <summary>
        /// A watchdog awaiting all animations of the second phase to be finished. The second
        /// phase is dedicated to drawing all graph elements present in the graph next to
        /// be drawn.When all deletion animations have completed, <see cref="OnAnimationsFinished"/>
        /// will be called.
        /// </summary>
        private class Phase2AnimationWatchDog : AnimationWatchDog
        {
            protected override string Name { get => nameof(Phase2AnimationWatchDog); }

            /// <summary>
            /// Constructor setting the <see cref="EvolutionRenderer"/> whose method should
            /// be called when there are no more outstanding animations. The number of
            /// outstanding animations is assumed to be zero at this point. The correct
            /// value can be set by <see cref="Await(int)"/> later.
            /// </summary>
            /// <param name="evolutionRenderer"><see cref="EvolutionRenderer"/> whose method should be called
            /// when there are no more outstanding animations</param>
            public Phase2AnimationWatchDog(EvolutionRenderer evolutionRenderer)
                : base(evolutionRenderer)
            { }

            /// <summary>
            /// If there are no more other animations to be awaited, <see cref="OnAnimationsFinished"/>
            /// will be called.
            /// </summary>
            protected override void Continue()
            {
                evolutionRenderer.OnAnimationsFinished();
            }
        }

        /// <summary>
        /// Implements the second phase in the transition from the <see cref="currentCity"/>
        /// to the <paramref name="next"/> graph.
        /// In this phase, all nodes in <paramref name="next"/> will be drawn. These may be
        /// either new or existing nodes (the latter being nodes that have been present in the
        /// currently drawn graph). When this phase has been completed, <see cref="OnAnimationsFinished"/>
        /// will be called eventually.
        ///
        /// To make sure that <see cref="OnAnimationsFinished"/> is called only when all nodes
        /// have been drawn, <see cref="phase2AnimationWatchDog"/> will be used. It will be
        /// set up to await the number of nodes present in <paramref name="next"/>. Then
        /// <see cref="RenderNode(Node)"/> or <see cref="IgnoreNode(Node)"/>, respectively, will be
        /// called for each such node. These in turn will signal <see cref="phase2AnimationWatchDog"/>
        /// when they have finished. When all outstanding node additions have been completed,
        /// <see cref="phase2AnimationWatchDog"/> will call <see cref="OnAnimationsFinished"/>.
        ///
        /// Note: <paramref name="next"/> will be a graph for the previous revision of
        /// the currently drawn graph in the graph series when the evolution visualization
        /// is played backward.
        /// </summary>
        /// <param name="next">the next graph to be drawn</param>
        private void Phase2AddNewAndExistingGraphElements(LaidOutGraph next)
        {
            /// We need to assign nextCity because the callbacks <see cref="RenderPlane"/>
            /// and <see cref="RenderNode(Node)"/> will access it. More precisely,
            /// <see cref="RenderNode(Node)"/> uses <see cref="NextLayoutToBeShown"/>
            /// which in turn uses <see cref="nextCity"/>.
            nextCity = next;

            NodeAnimation(next);
            CreateEdges(next);
            if (edgesAreDrawn)
            {
                EdgeAnimation(next);
            }

            // We have made the transition to the next graph.
            currentCity = next;

            void CreateEdges(LaidOutGraph next)
            {
                foreach (Edge edge in next.Graph.Edges())
                {
                    objectManager.GetEdge(edge, out GameObject edgeObject);
                    if (edgeObject.TryGetComponent(out SEESpline spline))
                    {
                        spline.Spline = next.EdgeLayout[edge.ID].Spline;
                    }
                    edgeObject.SetActive(true); // Make visible
                }
            }

            void EdgeAnimation(LaidOutGraph next)
            {
                // Create (or read from cache) the edge objects of the next
                // visible graph, update their spline, and make the objects
                // visible.
                if (currentCity != null)
                {
                    // We are transitioning to another graph.
                    edgeTweens.Clear();
                    foreach (Edge edge in next.Graph.Edges())
                    {
                        if (!next.EdgeLayout.TryGetValue(edge.ID, out ILayoutEdge<ILayoutNode> target))
                        {
                            Debug.LogWarning($"Missing layout edge for graph edge with id '{edge.ID}'; skipping it.\n");
                            continue;
                        }
                        if (currentCity.EdgeLayout.TryGetValue(edge.ID, out ILayoutEdge<ILayoutNode> source))
                        {
                            objectManager.GetEdge(edge, out GameObject edgeObject);
                            if (!edgeObject.TryGetComponent(out SplineMorphism morphism))
                            {
                                morphism = edgeObject.AddComponent<SplineMorphism>();
                            }
                            // We can adjust the duration later using the timeScale attribute.
                            edgeTweens[edge.Source] = morphism.CreateTween(source.Spline, target.Spline, 1f);
                        }
                    }
                }
            }

            void NodeAnimation(LaidOutGraph next)
            {
                phase2AnimationWatchDog.Await(next.Graph.NodeCount);
                // Draw all nodes of next graph.
                if (ignoreInnerNodes)
                {
                    // FIXME: The root could be a leaf.
                    next.Graph.Traverse(IgnoreNode, IgnoreNode, RenderNode);
                }
                else
                {
                    next.Graph.Traverse(RenderNode, RenderNode, RenderNode);
                }
            }

            /// Note: <see cref="OnAnimationsFinished"/> will be called by <see cref="phase2AnimationWatchDog"/>
            /// when phase 2 has completed.
        }

        /// <summary>
        /// Event function that adds the given <paramref name="gameNode"/>
        /// to <see cref="gameObject"/> as a child if <paramref name="gameNode"/>
        /// is a <see cref="GameObject"/> and has no parent yet. Informs
        /// <see cref="phase2AnimationWatchDog"/> that this animation has been finished.
        /// Called as a callback when the animation of new and existing
        /// nodes is finished. <see cref="RenderNode(Node)"/>.
        /// </summary>
        /// <param name="gameNode">new or existing game object representing a graph node</param>
        private void OnAnimationNodeAnimationFinished(object gameNode)
        {
            if (gameNode is GameObject go)
            {
                graphRenderer.AdjustAntenna(go);
                markerFactory.AdjustMarkerY(go);

                if (go.transform.parent == null)
                {
                    /// We will just put this game object under <see cref="gameObject"/>
                    /// (the game object representing the city as a whole) as a child. When
                    /// the animation is over and all nodes have reached their destination,
                    /// <see cref="UpdateGameNodeHierarchy"/> will put this node to its
                    /// actual logical game-node parent.
                    go.transform.SetParent(gameObject.transform);
                }
            }
            phase2AnimationWatchDog.Finished();
        }

        /// <summary>
        /// Starts the animation of <paramref name="animatorTween"/> with given <paramref name="duration"/>.
        /// </summary>
        /// <param name="animatorTween">Animator tween to start</param>
        /// <param name="duration">Duration of the animation</param>
        private void OnEdgeAnimationStart(Tween animatorTween, float duration)
        {
            Debug.Log($"OnEdgeAnimationStart {animatorTween != null}\n");
            if (animatorTween != null)
            {
                // We previously set the duration to 1 second (i.e., setDuration = 1)
                // and now want to change it to `duration` (henceforth actualDuration):
                // actualDuration = setDuration / timeScale
                // <=> actualDuration = 1 / timeScale
                // <=> timeScale = 1 / actualDuration
                animatorTween.timeScale = 1.0f / duration;
                animatorTween.PlayForward();
            }
        }
    }
}
