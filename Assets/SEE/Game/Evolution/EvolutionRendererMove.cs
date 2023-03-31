using DG.Tweening;
using SEE.DataModel.DG;
using SEE.Game.Charts;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Layout;
using Sirenix.Utilities;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// Part of the <see cref="EvolutionRenderer"/> taking care of moving
    /// graph elements existing in the currently shown graph and the next
    /// one to their new position.
    ///
    /// The transition from the current graph to the next one is organized in
    /// the following phases:
    ///
    /// (1) Remove deleted nodes and edges from the scene.
    /// (2) Move existing nodes and edges to their new position in the scene.
    /// (3) Adjust existing changed nodes and edges in the scene.
    /// (4) Add newly created nodes and edges to the scene.
    ///
    /// The code following here implements phase (2).
    /// </summary>
    public partial class EvolutionRenderer
    {
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
        private void Phase2MoveExistingGraphElements(LaidOutGraph next)
        {
            if (OLD)
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
                    phase2AnimationWatchDog.Await(next.Graph.NodeCount, () => OnAnimationsFinished());
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
            else
            {
                /// We need to assign nextCity because the callbacks <see cref="RenderPlane"/>
                /// and <see cref="RenderExistingNode(Node)"/> will access it.
                nextCity = next;


                int existingElements = equalNodes.Count + changedNodes.Count /* + equalEdges.Count + changedEdges.Count */;
                phase2AnimationWatchDog.Await(existingElements, () => OnAnimationsFinished());
                if (existingElements == 0)
                {
                    phase2AnimationWatchDog.Skip();
                }
                else
                {
                    equalNodes.ForEach(RenderExistingNode);
                    changedNodes.ForEach(RenderExistingNode);
                }
            }
        }

        private void RenderExistingNode(Node graphNode)
        {
            Assert.IsNotNull(graphNode);
            ILayoutNode layoutNode = NextLayoutToBeShown[graphNode.ID];
            // The game node representing the graphNode if there is any; null if there is none
            Node formerGraphNode = objectManager.GetNode(graphNode, out GameObject gameNode);
            Assert.IsTrue(gameNode.HasNodeRef());
            Assert.IsNotNull(formerGraphNode);

            // We want the animator to move each node separately, which is why we
            // remove each from the hierarchy; later the node hierarchy will be
            // re-established. It still needs to be a child of the code city,
            // however, because methods called in the course of the animation
            // will try to retrieve the code city from the game node.
            gameNode.transform.SetParent(gameObject.transform);
            MoveTo(gameNode, layoutNode);
        }

        private void MoveTo(GameObject gameNode, ILayoutNode layoutNode)
        {
            NodeOperator nodeOperator = gameObject.AddOrGetComponent<NodeOperator>();
            Debug.Log($"[MoveTo] Move {gameNode.name} from {gameNode.transform.position} to {layoutNode.CenterPosition}.\n");

            IOperationCallback<Action> callback = nodeOperator.MoveTo(layoutNode.CenterPosition, AnimationLag, updateEdges: true);
            callback.SetOnComplete(phase2AnimationWatchDog.Finished);
        }

        /// <summary>
        /// Ignores the given <paramref name="node"/> in rendering. This method can
        /// be used if inner or leaf nodes are to be ignored (e.g., for non-hierarchical
        /// layouts).
        /// </summary>
        /// <param name="node">node to be displayed</param>
        private void IgnoreNode(Node node)
        {
            phase2AnimationWatchDog.Finished();
        }

        /// <summary>
        /// Renders the game object corresponding to the given <paramref name="graphNode"/>.
        /// </summary>
        /// <param name="graphNode">graph node to be displayed</param>
        private void RenderNode(Node graphNode)
        {
            // The layout to be applied to graphNode
            ILayoutNode layoutNode = NextLayoutToBeShown[graphNode.ID];
            // The game node representing the graphNode if there is any; null if there is none
            Node formerGraphNode = objectManager.GetNode(graphNode, out GameObject currentGameNode);
            Assert.IsTrue(currentGameNode.HasNodeRef());
            Debug.Log($"[RenderNode] {graphNode.ID} is new {formerGraphNode == null}: position={layoutNode.CenterPosition} scale={layoutNode.AbsoluteScale}\n");

            Difference difference;
            if (formerGraphNode == null)
            {
                Debug.Log($"[RenderNode] {graphNode.ID} is new: position={layoutNode.CenterPosition} scale={layoutNode.AbsoluteScale}\n");
                // The node is new. It has no layout applied to it yet.
                // If the node is new, we animate it by moving it out of the ground.
                // Note: layoutNode.position.y denotes the ground position of
                // a game object, not its center.
                Vector3 position = layoutNode.CenterPosition;
                position.y -= layoutNode.AbsoluteScale.y;
                layoutNode.CenterPosition = position;

                // Revert the change to the y co-ordindate.
                position.y += layoutNode.AbsoluteScale.y;
                layoutNode.CenterPosition = position;
                difference = Difference.Added;

                // Set the layout for the copied node.
                currentGameNode.SetAbsoluteScale(layoutNode.AbsoluteScale, animate: false);
                currentGameNode.transform.position = layoutNode.CenterPosition;
            }
            else
            {
                // Node existed before.
                if (diff.AreDifferent(formerGraphNode, graphNode))
                {
                    difference = Difference.Changed;
                }
                else
                {
                    difference = Difference.None;
                }
            }
            switch (difference)
            {
                case Difference.Changed:
                    NodeChangesBuffer.GetSingleton().changedNodeIDs.Add(currentGameNode.name);
                    markerFactory.MarkChanged(currentGameNode);
                    // There is a change. It may or may not be the metric determining the style.
                    // We will not further check that and just call the following method.
                    // If there is no change, this method does not need to be called because then
                    // we know that the metric values determining the style and antenna of the former
                    // and the new graph node are the same.
                    graphRenderer.AdjustStyle(currentGameNode);
                    break;
                case Difference.Added:
                    NodeChangesBuffer.GetSingleton().addedNodeIDs.Add(currentGameNode.name);
                    markerFactory.MarkBorn(currentGameNode);
                    break;
            }
            // We want the animator to move each node separately, which is why we
            // remove each from the hierarchy; later the node hierarchy will be
            // re-established. It still needs to be a child of the code city,
            // however, because methods called in the course of the animation
            // will try to retrieve the code city from the game node.
            currentGameNode.transform.SetParent(gameObject.transform);

            // currentGameNode is shifted to its new position through the animator.
            Action<float> onEdgeAnimationStart = null;
            if (edgeTweens.TryGetValue(graphNode, out Tween tween))
            {
                Debug.Log($"onEdgeAnimationStart set for {graphNode.ID} {currentGameNode.name}\n");
                onEdgeAnimationStart = duration => OnEdgeAnimationStart(tween, duration);
            }
            Debug.Log($"Move {currentGameNode.name} from {currentGameNode.transform.position} to {layoutNode.CenterPosition}.\n");
            changeAndBirthAnimator.AnimateTo(gameObject: currentGameNode,
                                             layoutNode: layoutNode,
                                             callbackWhenAnimationFinished: OnAnimationNodeAnimationFinished,
                                             moveCallback: onEdgeAnimationStart);
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
