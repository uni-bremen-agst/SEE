using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using MoreLinq;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GameObjects;
using SEE.GO;
using SEE.Layout;
using SEE.Tools.ReflexionAnalysis;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.Operator
{
    /// <summary>
    /// A component managing operations done on the node it is attached to.
    /// Available operations consist of the public methods exported by this class.
    /// Operations can be animated or executed directly, by setting the duration to 0.
    /// </summary>
    public partial class NodeOperator : GraphElementOperator<Color>
    {
        // We split up movement on the three axes because it makes sense in certain situations.
        // For example, when dragging a node along the XZ-axis, if we want to place it on top
        // of the node we hover over, we might want to animate this Y-axis movement while
        // still letting the user drag the node.

        /// <summary>
        /// Operation handling X-axis movement in world space.
        /// </summary>
        private TweenOperation<float> positionX;

        /// <summary>
        /// Operation handling Y-axis movement in world space.
        /// </summary>
        private TweenOperation<float> positionY;

        /// <summary>
        /// Operation handling Z-axis movement in world space.
        /// </summary>
        private TweenOperation<float> positionZ;

        /// <summary>
        /// Operation handling node rotation.
        /// </summary>
        private TweenOperation<Quaternion> rotation;

        /// <summary>
        /// Operation handling animated label display.
        /// </summary>
        private TweenOperation<float> labelAlpha;

        /// <summary>
        /// Operation handling the starting position of the label's line.
        /// </summary>
        private TweenOperation<Vector3> labelStartLinePosition;

        /// <summary>
        /// Operation handling the end position of the label's line.
        /// </summary>
        private TweenOperation<Vector3> labelEndLinePosition;

        /// <summary>
        /// Operation handling the position of the label's text.
        /// </summary>
        private TweenOperation<Vector3> labelTextPosition;

        /// <summary>
        /// Operation handling node scaling (specifically, localScale).
        /// </summary>
        private TweenOperation<Vector3> scale;

        /// <summary>
        /// The node to which this node operator belongs.
        ///
        /// <em>Be aware that this may be null if the node operator is attached to an artificial node.</em>
        /// </summary>
        public Node Node
        {
            get;
            private set;
        }

        /// <summary>
        /// If this isn't null, represents the duration in seconds the layout update should take,
        /// and if this is null, the layout shall not be updated.
        /// Note that the layout includes the edge layout and the positioning of the node label, if it exists.
        /// </summary>
        private float? updateLayoutDuration;

        /// <summary>
        /// If true, the incoming and outgoing edges will be adjusted, too.
        /// </summary>
        private bool updateEdges;

        /// <summary>
        /// The material of the node.
        /// </summary>
        private Material material;

        /// <summary>
        /// The position this node is supposed to be at.
        /// </summary>
        /// <seealso cref="AbstractOperator"/>
        public Vector3 TargetPosition => new(positionX.TargetValue, positionY.TargetValue, positionZ.TargetValue);

        /// <summary>
        /// The scale this node is supposed to be at.
        /// </summary>
        /// <seealso cref="AbstractOperator"/>
        public Vector3 TargetScale => scale.TargetValue;

        #region Public API

        /// <summary>
        /// Moves the node to the <paramref name="newXPosition"/> in world space.
        /// </summary>
        /// <param name="newXPosition">the desired new target X coordinate in world space</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <param name="updateEdges">if true, the connecting edges will be moved along with the node</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> MoveXTo(float newXPosition, float factor = 1, bool updateEdges = true)
        {
            float duration = ToDuration(factor);
            updateLayoutDuration = duration;
            this.updateEdges = updateEdges;
            return positionX.AnimateTo(newXPosition, duration);
        }

        /// <summary>
        /// Moves the node to the <paramref name="newYPosition"/> in world space.
        /// </summary>
        /// <param name="newYPosition">the desired new target Y coordinate in world space</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <param name="updateEdges">if true, the connecting edges will be moved along with the node</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> MoveYTo(float newYPosition, float factor = 1, bool updateEdges = true)
        {
            float duration = ToDuration(factor);
            updateLayoutDuration = duration;
            this.updateEdges = updateEdges;
            return positionY.AnimateTo(newYPosition, duration);
        }

        /// <summary>
        /// Moves the node to the <paramref name="newZPosition"/> in world space.
        /// </summary>
        /// <param name="newZPosition">the desired new target Z coordinate in world space</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <param name="updateEdges">if true, the connecting edges will be moved along with the node</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> MoveZTo(float newZPosition, float factor = 1, bool updateEdges = true)
        {
            float duration = ToDuration(factor);
            updateLayoutDuration = duration;
            this.updateEdges = updateEdges;
            return positionZ.AnimateTo(newZPosition, duration);
        }

        /// <summary>
        /// Moves the node to the <paramref name="newPosition"/> in world space.
        /// </summary>
        /// <param name="newPosition">the desired new target position in world space</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <param name="updateEdges">if true, the connecting edges will be moved along with the node</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> MoveTo(Vector3 newPosition, float factor = 1, bool updateEdges = true)
        {
            float duration = ToDuration(factor);
            updateLayoutDuration = duration;
            this.updateEdges = updateEdges;
            return new AndCombinedOperationCallback<Action>(new[]
            {
                positionX.AnimateTo(newPosition.x, duration),
                positionY.AnimateTo(newPosition.y, duration),
                positionZ.AnimateTo(newPosition.z, duration)
            }, a => a);
        }

        /// <summary>
        /// Moves and scales the node at the same time.
        /// <para>
        /// If <paramref name="reparentChildren"/> is <c>true></c> (default), children are not scaled and moved along.
        /// For this purpose they are reparented to their grandparent during the animation and back to the original
        /// parent after the animation has completed.
        /// </para>
        /// </summary>
        /// <param name="newLocalScale">the desired new local scale</param>
        /// <param name="newPosition">the desired new target position in world space</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <param name="reparentChildren">if <c>true</c>, the children are not moved and scaled along with their parent</param>
        /// <param name="updateEdges">if true, the connecting edges will be moved along with the node</param>
        /// <param name="updateLayers">if true, layers will be updated via <see cref="InteractableObject.UpdateLayer"/>.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> ResizeTo(
            Vector3 newLocalScale,
            Vector3 newPosition,
            float factor = 1,
            bool updateEdges = true,
            bool reparentChildren = true,
            bool updateLayers = false)
        {
            float duration = ToDuration(factor);
            updateLayoutDuration = duration;
            this.updateEdges = updateEdges;

            List<Transform> children = null;
            Transform originalParent = transform;
            Transform tempParent = transform.parent;
            if (reparentChildren)
            {
                children = new(transform.childCount);
                foreach (Transform child in transform)
                {
                    if (child.gameObject.IsNodeAndActiveSelf())
                    {
                        children.Add(child);
                    }
                }
                reparent(tempParent);
            }

            IOperationCallback<Action> animation = new AndCombinedOperationCallback<Action>
                (new[]
                  {
                    positionX.AnimateTo(newPosition.x, duration),
                    positionY.AnimateTo(newPosition.y, duration),
                    positionZ.AnimateTo(newPosition.z, duration),
                    scale.AnimateTo(newLocalScale, duration)
                  },
                 a => a);
            animation.OnComplete(() => onEnd(originalParent));
            animation.OnKill(() => onEnd(originalParent));
            return animation;

            void onEnd(Transform originalParent)
            {
                if (reparentChildren)
                {
                    reparent(originalParent);
                }
                if (updateLayers)
                {
                    updatePortalLayers();
                }
            }

            void reparent(Transform newParent)
            {
                foreach (Transform child in children)
                {
                    child.SetParent(newParent);
                }
            }

            void updatePortalLayers()
            {
                InteractableObject[] children = transform.GetComponentsInChildren<InteractableObject>();
                foreach (InteractableObject child in children)
                {
                    child.UpdateLayer();
                }
            }
        }

        /// <summary>
        /// Rotates the node to the given quaternion <paramref name="newRotation"/>.
        /// </summary>
        /// <param name="newRotation">the desired new target rotation in world space</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> RotateTo(Quaternion newRotation, float factor = 1)
        {
            return rotation.AnimateTo(newRotation, ToDuration(factor));
        }

        /// <summary>
        /// Rotates the node around the given <paramref name="axis"/> by the given <paramref name="angle"/>.
        /// </summary>
        /// <param name="axis">the axis to rotate around</param>
        /// <param name="angle">the angle to rotate by</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> RotateTo(Vector3 axis, float angle, float factor = 1)
        {
            Quaternion rotation = Quaternion.AngleAxis(angle, axis);
            return RotateTo(rotation, factor);
        }

        /// <summary>
        /// Scales the node to the given <paramref name="newLocalScale"/>.
        /// </summary>
        /// <param name="newLocalScale">the desired new target scale, more precisely, factor by which the game object
        /// should be scaled relative to its parent (i.e., local scale)</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <param name="updateEdges">if true, the connecting edges will be moved along with the node</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> ScaleTo(Vector3 newLocalScale, float factor = 1, bool updateEdges = true)
        {
            float duration = ToDuration(factor);
            updateLayoutDuration = duration;
            this.updateEdges = updateEdges;
            return scale.AnimateTo(newLocalScale, duration);
        }

        /// <summary>
        /// Shows the label with given <paramref name="alpha"/> value if it is greater than zero.
        /// Otherwise, hides the label.
        /// </summary>
        /// <param name="alpha">the desired target alpha value for the label.</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> FadeLabel(float alpha, Vector3? labelBase = null, float factor = 1)
        {
            float duration = ToDuration(factor);

            if (labelBase.HasValue)
            {
                DesiredLabelStartLinePosition = labelBase.Value;
            }
            return new AndCombinedOperationCallback<Action>(new[]
            {
                // NOTE: Order is important, because the line's end position target depends on the text position target,
                //       and the text position target depends on the alpha value's target!
                labelAlpha.AnimateTo(alpha, duration),
                labelTextPosition.AnimateTo(DesiredLabelTextPosition, duration),
                labelStartLinePosition.AnimateTo(DesiredLabelStartLinePosition, duration),
                labelEndLinePosition.AnimateTo(DesiredLabelEndLinePosition, duration)
            });
        }

        #endregion

        /// <summary>
        /// Updates the layout of attached objects (edges, labels) immediately.
        /// </summary>
        /// <param name="duration">Time in seconds the animation should take.</param>
        private void UpdateLayout(float duration)
        {
            if (Node != null)
            {
                Assert.IsNotNull(Node, $"[{nameof(NodeOperator)}]{gameObject.FullName()} has undefined graph node.");
                if (!Node.IsRoot())
                {
                    // If we are moving the root node, the whole graph will be moved,
                    // hence, the layout of the edges does not need to be updated.
                    if (updateEdges && City.EdgeLayoutSettings.Kind != EdgeLayoutKind.None)
                    {
                        // The edge layout needs to be updated only if we actually have an edge layout.
                        UpdateEdgeLayout(duration);

                        // If the operator was invoked in a BranchCity, the author-sphere edges should be moved, too.
                        if (City is BranchCity)
                        {
                            if (gameObject.TryGetComponent(out AuthorRef authorRef))
                            {
                                foreach ((GameObject, int) edge in authorRef.Edges)
                                {
                                    SEESpline seeSpline = edge.Item1.GetComponent<SEESpline>();
                                    seeSpline.UpdateEndPosition(gameObject.transform.position);
                                }
                            }
                            else
                            {
                                foreach (AuthorRef child in gameObject.GetComponentsInChildren<AuthorRef>())
                                {
                                    child.Edges.ForEach(x =>
                                        x.Item1.GetComponent<SEESpline>()
                                            .UpdateEndPosition(child.gameObject.transform.position));
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the edges attached to this node.
        /// This involves recalculating the edge layout for each attached edge.
        /// </summary>
        /// <param name="duration">Time in seconds the animation should take.</param>
        private void UpdateEdgeLayout(float duration)
        {
            // We remember the old position and scale, and move the node to the new position and scale so that
            // edge layouts (dependent on position and scale) can be correctly calculated.
            Transform ourTransform = transform; // cache transform for performance
            Vector3 oldPosition = ourTransform.position;
            ourTransform.position = new Vector3(positionX.TargetValue, positionY.TargetValue, positionZ.TargetValue);
            Vector3 oldScale = ourTransform.localScale;
            ourTransform.localScale = scale.TargetValue;
            Node node;
            try
            {
                node = gameObject.GetNode();
            }
            catch (NullReferenceException)
            {
                // Node may be an artificial root without a proper node reference.
                return;
            }

            static IEnumerable<Node> GetMappedNodes(Node mappedNode)
            {
                if (mappedNode.IsInArchitecture())
                {
                    // This node may be mapped to. We need to expand it to include the node we're mapped to.
                    return mappedNode.Incomings.Where(ReflexionGraphTools.IsInMapping)
                                     .SelectMany(x => x.Source.PostOrderDescendants()).Append(mappedNode);
                }

                return new[] { mappedNode };
            }

            // Recalculate edge layout and animate edges due to new node positioning.
            // TODO: This is currently a very costly method. We should iterate over game edges instead, that way
            //       we can use the hierarchy we're actually interested in and don't need to, e.g., look at
            //       mapped nodes which may be relevant. Before implementing this, measure actual performance, though.
            IEnumerable<Edge> relevantEdges = node.PostOrderDescendants()
                                                  .SelectMany(GetMappedNodes)
                                                  .SelectMany(n => n.Incomings.Union(n.Outgoings))
                                                  .Where(edge => !edge.HasToggle(GraphElement.IsVirtualToggle))
                                                  .Distinct();
            MorphEdges(relevantEdges, duration);

            // Once we're done, we reset the gameObject to its original position.
            ourTransform.position = oldPosition;
            ourTransform.localScale = oldScale;
        }

        /// <summary>
        /// Morphs all <paramref name="edges"/> while the node is moving.
        /// </summary>
        /// <param name="edges">edges to be morphed</param>
        /// <param name="duration">the duration of the animation in seconds</param>
        private void MorphEdges(IEnumerable<Edge> edges, float duration)
        {
            // All game edges corresponding to the graph edges.
            IList<GameObject> gameEdges = new List<GameObject>();

            // Gather all gameEdges.
            foreach (Edge edge in edges)
            {
                GameObject gameEdge = GraphElementIDMap.Find(edge.ID);
                if (gameEdge == null)
                {
                    // TODO: How is this possible?
                    Debug.LogWarning($"Edge {edge.ToShortString()} has no associated GameObject!\n");
                    continue;
                }
                gameEdges.Add(gameEdge);
            }

            // Calculate the layout for all gameEdges.
            IDictionary<string, ILayoutEdge<ILayoutNode>> layoutEdges = City.Renderer.LayoutEdges(gameEdges);

            // Let each game edge morph to the splines according to the layout.
            foreach (GameObject gameEdge in gameEdges)
            {
                gameEdge.EdgeOperator().MorphTo(layoutEdges[gameEdge.name].Spline, ToFactor(duration));
            }
        }

        protected override TweenOperation<Color> InitializeColorOperation()
        {
            return new TweenOperation<Color>(AnimateToColorAction, material.color);

            Tween[] AnimateToColorAction(Color color, float d)
            {
                return new Tween[]
                {
                    material.DOColor(color, d).Play()
                };
            }
        }

        protected override Tween[] BlinkAction(int count, float duration)
        {
            // If we're interrupting another blinking, we need to make sure the color still has the correct value.
            if (material == null)
            {
                return new Tween[] { };
            }

            material.color = Color.TargetValue;

            if (count != 0)
            {
                return new Tween[]
                {
                    material.DOColor(Color.TargetValue.Invert(), duration / (2 * Mathf.Abs(count))).SetEase(Ease.Linear).SetLoops(2 * count, LoopType.Yoyo).Play()
                };
            }
            else
            {
                return new Tween[] { };
            }
        }

        protected override Color ModifyColor(Color color, Func<Color, Color> modifier)
        {
            return modifier(color);
        }

        protected override IEnumerable<Color> AsEnumerable(Color color)
        {
            return new[] { color };
        }

        protected override void OnEnable()
        {
            // Material needs to be assigned before calling base.OnEnable()
            // because it is used in InitializeColorOperation().
            material = GetRenderer(gameObject).material;
            base.OnEnable();
            Node = GetNode(gameObject);
            Vector3 currentPosition = transform.position;
            Vector3 currentScale = transform.localScale;
            Quaternion currentRotation = transform.rotation;

            positionX = new TweenOperation<float>(AnimateToXAction, currentPosition.x);
            positionY = new TweenOperation<float>(AnimateToYAction, currentPosition.y);
            positionZ = new TweenOperation<float>(AnimateToZAction, currentPosition.z);
            rotation = new TweenOperation<Quaternion>(AnimateToRotationAction, currentRotation);
            scale = new TweenOperation<Vector3>(AnimateToScaleAction, currentScale);

            PrepareLabel();
            labelAlpha = new TweenOperation<float>(AnimateLabelAlphaAction, 0f);
            labelTextPosition = new TweenOperation<Vector3>(AnimateLabelTextPositionAction, DesiredLabelTextPosition);
            labelStartLinePosition = new TweenOperation<Vector3>(AnimateLabelStartLinePositionAction, DesiredLabelStartLinePosition);
            labelEndLinePosition = new TweenOperation<Vector3>(AnimateLabelEndLinePositionAction, DesiredLabelEndLinePosition);
            return;

            Tween[] AnimateToXAction(float x, float d) => new Tween[] { transform.DOMoveX(x, d).Play() };
            Tween[] AnimateToYAction(float y, float d) => new Tween[] { transform.DOMoveY(y, d).Play() };
            Tween[] AnimateToZAction(float z, float d) => new Tween[] { transform.DOMoveZ(z, d).Play() };
            Tween[] AnimateToRotationAction(Quaternion r, float d) => new Tween[] { transform.DORotateQuaternion(r, d).Play() };
            Tween[] AnimateToScaleAction(Vector3 s, float d) => new Tween[] { transform.DOScale(s, d).Play() };

            static Node GetNode(GameObject gameObject)
            {
                // We allow a null value for artificial nodes, but at least a NodeRef must be attached.
                if (!gameObject.TryGetComponent(out NodeRef nodeRef))
                {
                    throw new InvalidOperationException($"NodeOperator-operated object {gameObject.FullName()} must have {nameof(NodeRef)} attached!");
                }

                return nodeRef.Value;
            }
        }

        /// <summary>
        /// Returns the <see cref="Renderer"/> of the given <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">the <see cref="GameObject"/> whose <see cref="Renderer"/> to return</param>
        /// <returns>the <see cref="Renderer"/> of the given <paramref name="gameObject"/></returns>
        /// <exception cref="InvalidOperationException">
        /// if the <paramref name="gameObject"/> has no <see cref="Renderer"/>
        /// </exception>
        private static Renderer GetRenderer(GameObject gameObject)
        {
            if (!gameObject.TryGetComponent(out Renderer renderer))
            {
                throw new InvalidOperationException($"NodeOperator-operated object {gameObject.FullName()} must have a Renderer component!");
            }

            return renderer;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            positionX.KillAnimator();
            positionX = null;
            positionY.KillAnimator();
            positionY = null;
            positionZ.KillAnimator();
            positionZ = null;
            rotation.KillAnimator();
            rotation = null;
            scale.KillAnimator();
            scale = null;
            labelAlpha.KillAnimator();
            labelAlpha = null;
            labelTextPosition.KillAnimator();
            labelTextPosition = null;
            labelStartLinePosition.KillAnimator();
            labelStartLinePosition = null;
            labelEndLinePosition.KillAnimator();
            labelEndLinePosition = null;
            // NOTE: Calling Destroy(nodeLabel) will not lead to the nodeLabel being immediately destroyed.
            //       Instead, it will be destroyed at the end of the frame. Thus, if PrepareLabel() is called
            //       before the end of the frame, the nodeLabel will still be there and we will not create
            //       a new one. This leads to the bug described in #660. To avoid this, we *disable* the nodeLabel
            //       instead of destroying it, which happens immediately.
            nodeLabel.SetActive(false);
            nodeLabel = null;
        }

        private void Update()
        {
            if (updateLayoutDuration.HasValue)
            {
                UpdateLayout(updateLayoutDuration.Value);
                updateLayoutDuration = null;
                updateEdges = true;
            }
        }
    }
}
