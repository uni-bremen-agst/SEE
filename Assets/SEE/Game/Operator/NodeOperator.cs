using DG.Tweening;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.Layout;
using SEE.Tools.ReflexionAnalysis;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <param name="newXPosition">The desired new target X coordinate in world space.</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <param name="updateEdges">If true, the connecting edges will be moved along with the node.</param>
        /// <param name="updateLayers">If true, layers will be updated via <see cref="InteractableObject.UpdateLayer"/>.</param>
        /// <returns>An operation callback for the requested animation.</returns>
        public IOperationCallback<Action> MoveXTo(
            float newXPosition,
            float factor = 1,
            bool updateEdges = true,
            bool updateLayers = true)
        {
            float duration = ToDuration(factor);
            updateLayoutDuration = duration;
            this.updateEdges = updateEdges;
            IOperationCallback<Action> animation = positionX.AnimateTo(newXPosition, duration);
            animation.OnComplete(() => OnEnd());
            animation.OnKill(() => OnEnd());
            return animation;

            void OnEnd()
            {
                if (updateLayers)
                {
                    transform.gameObject.UpdateInteractableLayers();
                }
            }
        }

        /// <summary>
        /// Moves the node to the <paramref name="newYPosition"/> in world space.
        /// </summary>
        /// <param name="newYPosition">The desired new target Y coordinate in world space.</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <param name="updateEdges">If true, the connecting edges will be moved along with the node.</param>
        /// <param name="updateLayers">If true, layers will be updated via <see cref="InteractableObject.UpdateLayer"/>.</param>
        /// <returns>An operation callback for the requested animation.</returns>
        public IOperationCallback<Action> MoveYTo(
            float newYPosition,
            float factor = 1,
            bool updateEdges = true,
            bool updateLayers = true)
        {
            float duration = ToDuration(factor);
            updateLayoutDuration = duration;
            this.updateEdges = updateEdges;
            IOperationCallback<Action> animation = positionY.AnimateTo(newYPosition, duration);
            animation.OnComplete(() => OnEnd());
            animation.OnKill(() => OnEnd());
            return animation;

            void OnEnd()
            {
                if (updateLayers)
                {
                    transform.gameObject.UpdateInteractableLayers();
                }
            }
        }

        /// <summary>
        /// Moves the node to the <paramref name="newZPosition"/> in world space.
        /// </summary>
        /// <param name="newZPosition">The desired new target Z coordinate in world space.</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <param name="updateEdges">If true, the connecting edges will be moved along with the node.</param>
        /// <param name="updateLayers">If true, layers will be updated via <see cref="InteractableObject.UpdateLayer"/>.</param>
        /// <returns>An operation callback for the requested animation.</returns>
        public IOperationCallback<Action> MoveZTo(
            float newZPosition,
            float factor = 1,
            bool updateEdges = true,
            bool updateLayers = true)
        {
            float duration = ToDuration(factor);
            updateLayoutDuration = duration;
            this.updateEdges = updateEdges;
            IOperationCallback<Action> animation = positionZ.AnimateTo(newZPosition, duration);
            animation.OnComplete(() => OnEnd());
            animation.OnKill(() => OnEnd());
            return animation;

            void OnEnd()
            {
                if (updateLayers)
                {
                    transform.gameObject.UpdateInteractableLayers();
                }
            }
        }

        /// <summary>
        /// Moves the node to the <paramref name="newPosition"/> in world space.
        /// </summary>
        /// <param name="newPosition">The desired new target position in world space.</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <param name="updateEdges">If true, the connecting edges will be moved along with the node.</param>
        /// <param name="updateLayers">If true, layers will be updated via <see cref="InteractableObject.UpdateLayer"/>.</param>
        /// <returns>An operation callback for the requested animation.</returns>
        public IOperationCallback<Action> MoveTo(
            Vector3 newPosition,
            float factor = 1,
            bool updateEdges = true,
            bool updateLayers = true)
        {
            float duration = ToDuration(factor);
            updateLayoutDuration = duration;
            this.updateEdges = updateEdges;
            IOperationCallback<Action> animation = new AndCombinedOperationCallback<Action>(new[]
            {
                positionX.AnimateTo(newPosition.x, duration),
                positionY.AnimateTo(newPosition.y, duration),
                positionZ.AnimateTo(newPosition.z, duration)
            }, a => a);
            animation.OnComplete(() => OnEnd());
            animation.OnKill(() => OnEnd());
            return animation;

            void OnEnd()
            {
                if (updateLayers)
                {
                    transform.gameObject.UpdateInteractableLayers();
                }
            }
        }

        /// <summary>
        /// Moves and scales the node at the same time.
        /// <para>
        /// If <paramref name="reparentChildren"/> is true (default), children are not scaled and moved along.
        /// For this purpose they are reparented to their grandparent during the animation and back to the original
        /// parent after the animation has completed.
        /// </para>
        /// </summary>
        /// <param name="newLocalScale">The desired new local scale.</param>
        /// <param name="newPosition">The desired new target position in world space.</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <param name="reparentChildren">If true, the children are not moved and scaled along with their parent.</param>
        /// <param name="updateEdges">If true, the connecting edges will be moved along with the node.</param>
        /// <param name="updateLayers">If true, layers will be updated via <see cref="InteractableObject.UpdateLayer"/>.</param>
        /// <returns>An operation callback for the requested animation.</returns>
        public IOperationCallback<Action> ResizeTo(
            Vector3 newLocalScale,
            Vector3 newPosition,
            float factor = 1,
            bool updateEdges = true,
            bool reparentChildren = true,
            bool updateLayers = true)
        {
            float duration = ToDuration(factor);
            updateLayoutDuration = duration;
            this.updateEdges = updateEdges;

            IList<Transform> children = null;
            Transform originalParent = transform;
            Transform tempParent = transform.parent;
            if (reparentChildren)
            {
                children = transform.ReparentChildren(tempParent, child => child.gameObject.IsNodeAndActiveSelf());
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
            animation.OnComplete(() => OnEnd(originalParent));
            animation.OnKill(() => OnEnd(originalParent));
            return animation;

            void OnEnd(Transform originalParent)
            {
                if (reparentChildren)
                {
                    originalParent.SetChildren(children);
                }
                if (updateLayers)
                {
                    transform.gameObject.UpdateInteractableLayers();
                }
            }
        }

        /// <summary>
        /// Rotates the node to the given quaternion <paramref name="newRotation"/>.
        /// </summary>
        /// <param name="newRotation">The desired new target rotation in world space.</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <param name="updateLayers">If true, layers will be updated via <see cref="InteractableObject.UpdateLayer"/>.</param>
        /// <returns>An operation callback for the requested animation.</returns>
        public IOperationCallback<Action> RotateTo(
            Quaternion newRotation,
            float factor = 1,
            bool updateLayers = true)
        {
            IOperationCallback<Action> animation = rotation.AnimateTo(newRotation, ToDuration(factor));
            animation.OnComplete(() => OnEnd());
            animation.OnKill(() => OnEnd());
            return animation;

            void OnEnd()
            {
                if (updateLayers)
                {
                    transform.gameObject.UpdateInteractableLayers();
                }
            }
        }

        /// <summary>
        /// Rotates the node around the given <paramref name="axis"/> by the given <paramref name="angle"/>.
        /// </summary>
        /// <param name="axis">The axis to rotate around.</param>
        /// <param name="angle">The angle to rotate by.</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// <param name="updateLayers">If true, layers will be updated via <see cref="InteractableObject.UpdateLayer"/>.</param>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation.</returns>
        public IOperationCallback<Action> RotateTo(
            Vector3 axis,
            float angle,
            float factor = 1,
            bool updateLayers = true)
        {
            Quaternion rotation = Quaternion.AngleAxis(angle, axis);
            IOperationCallback<Action> animation = RotateTo(rotation, factor);
            animation.OnComplete(() => OnEnd());
            animation.OnKill(() => OnEnd());
            return animation;

            void OnEnd()
            {
                if (updateLayers)
                {
                    transform.gameObject.UpdateInteractableLayers();
                }
            }
        }

        /// <summary>
        /// Scales the node to the given <paramref name="newLocalScale"/>.
        /// </summary>
        /// <param name="newLocalScale">The desired new target scale, more precisely, factor by which the game object
        /// should be scaled relative to its parent (i.e., local scale).</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <param name="updateEdges">If true, the connecting edges will be moved along with the node.</param>
        /// <param name="updateLayers">If true, layers will be updated via <see cref="InteractableObject.UpdateLayer"/>.</param>
        /// <returns>An operation callback for the requested animation.</returns>
        public IOperationCallback<Action> ScaleTo(
            Vector3 newLocalScale,
            float factor = 1,
            bool updateEdges = true,
            bool updateLayers = true)
        {
            float duration = factor > 0 ? ToDuration(factor) : 0;
            updateLayoutDuration = duration;
            this.updateEdges = updateEdges;
            IOperationCallback<Action> animation = scale.AnimateTo(newLocalScale, duration);
            animation.OnComplete(() => OnEnd());
            animation.OnKill(() => OnEnd());
            return animation;

            void OnEnd()
            {
                if (updateLayers)
                {
                    transform.gameObject.UpdateInteractableLayers();
                }
            }
        }

        /// <summary>
        /// Shows the label with given <paramref name="alpha"/> value if it is greater than zero.
        /// Otherwise, hides the label.
        /// </summary>
        /// <param name="alpha">The desired target alpha value for the label.</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation.</returns>
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
                    if (updateEdges)
                    {
                        if (City.EdgeLayoutSettings.Kind != EdgeLayoutKind.None)
                        {
                            // The edge layout needs to be updated only if we actually have an edge layout.
                            UpdateEdgeLayout(duration);
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
        public void UpdateEdgeLayout(float duration)
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
        /// <param name="edges">Edges to be morphed.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
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

            Vector3 currentPosition = transform.position;
            Vector3 currentScale = transform.localScale;
            Quaternion currentRotation = transform.rotation;

            positionX = new TweenOperation<float>(AnimateToXAction, currentPosition.x);
            positionY = new TweenOperation<float>(AnimateToYAction, currentPosition.y);
            positionZ = new TweenOperation<float>(AnimateToZAction, currentPosition.z);
            rotation = new TweenOperation<Quaternion>(AnimateToRotationAction, currentRotation);
            scale = new TweenOperation<Vector3>(AnimateToScaleAction, currentScale);

            // We allow a null value for artificial nodes, but at least a NodeRef must be attached.
            if (!gameObject.TryGetComponent(out NodeRef nodeRef))
            {
                throw new InvalidOperationException($"NodeOperator-operated object {gameObject.FullName()} must have {nameof(NodeRef)} attached!");
            }

            // A valid NodeRef is one whose Value differs from null.
            // For the BranchCity(if created in the Editor) it can happen that we
            // create a NodeOperator before the graph is actually deserialized and all
            // NodeRefs properly set, that is, when the NodeRef is not yet valid.
            // In that case, we postpone the label preparation until it becomes
            // available. The label preparation needs to know the node to retrieve
            // the name of the node to be shown.
            Node = nodeRef.Value;
            if (Node != null)
            {
                PrepareLabel();
            }
            else
            {
                nodeRef.OnValueSet += DelayedPrepareLabel;
            }

            return;

            void DelayedPrepareLabel(Node node)
            {
                nodeRef.OnValueSet -= DelayedPrepareLabel;
                Node = node;
                PrepareLabel();
            }

            Tween[] AnimateToXAction(float x, float d) => new Tween[] { transform.DOMoveX(x, d).Play() };
            Tween[] AnimateToYAction(float y, float d) => new Tween[] { transform.DOMoveY(y, d).Play() };
            Tween[] AnimateToZAction(float z, float d) => new Tween[] { transform.DOMoveZ(z, d).Play() };
            Tween[] AnimateToRotationAction(Quaternion r, float d) => new Tween[] { transform.DORotateQuaternion(r, d).Play() };
            Tween[] AnimateToScaleAction(Vector3 s, float d) => new Tween[] { transform.DOScale(s, d).Play() };
        }

        /// <summary>
        /// Returns the <see cref="Renderer"/> of the given <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> whose <see cref="Renderer"/> to return.</param>
        /// <returns>The <see cref="Renderer"/> of the given <paramref name="gameObject"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// If the <paramref name="gameObject"/> has no <see cref="Renderer"/>.
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
            positionX?.KillAnimator();
            positionX = null;
            positionY?.KillAnimator();
            positionY = null;
            positionZ?.KillAnimator();
            positionZ = null;
            rotation?.KillAnimator();
            rotation = null;
            scale?.KillAnimator();
            scale = null;
            labelAlpha?.KillAnimator();
            labelAlpha = null;
            labelTextPosition?.KillAnimator();
            labelTextPosition = null;
            labelStartLinePosition?.KillAnimator();
            labelStartLinePosition = null;
            labelEndLinePosition?.KillAnimator();
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
