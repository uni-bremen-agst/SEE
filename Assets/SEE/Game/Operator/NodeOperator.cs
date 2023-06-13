using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using SEE.DataModel.DG;
using SEE.Game.City;
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
        private TweenOperation<float> PositionX;

        /// <summary>
        /// Operation handling Y-axis movement in world space.
        /// </summary>
        private TweenOperation<float> PositionY;

        /// <summary>
        /// Operation handling Z-axis movement in world space.
        /// </summary>
        private TweenOperation<float> PositionZ;

        /// <summary>
        /// Operation handling node rotation.
        /// </summary>
        private TweenOperation<Quaternion> Rotation;

        /// <summary>
        /// Operation handling animated label display.
        /// </summary>
        private TweenOperation<float> LabelAlpha;

        /// <summary>
        /// Operation handling the blinking of the node.
        /// The parameter specifies the number of blinks.
        /// </summary>
        private TweenOperation<int> Blinking;

        /// <summary>
        /// Operation handling the starting position of the label's line.
        /// </summary>
        private TweenOperation<Vector3> LabelStartLinePosition;

        /// <summary>
        /// Operation handling the end position of the label's line.
        /// </summary>
        private TweenOperation<Vector3> LabelEndLinePosition;

        /// <summary>
        /// Operation handling the position of the label's text.
        /// </summary>
        private TweenOperation<Vector3> LabelTextPosition;

        /// <summary>
        /// Operation handling node scaling (specifically, localScale).
        /// </summary>
        private TweenOperation<Vector3> Scale;

        /// <summary>
        /// The node to which this node operator belongs.
        /// </summary>
        public Node Node
        {
            get;
            private set;
        }

        /// <summary>
        /// The node to which the <see cref="Node"/> belongs.
        /// </summary>
        public AbstractSEECity City
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
        /// The position this node is supposed to be at.
        /// </summary>
        /// <seealso cref="AbstractOperator"/>
        public Vector3 TargetPosition => new(PositionX.TargetValue, PositionY.TargetValue, PositionZ.TargetValue);

        /// <summary>
        /// The scale this node is supposed to be at.
        /// </summary>
        /// <seealso cref="AbstractOperator"/>
        public Vector3 TargetScale => Scale.TargetValue;

        #region Public API

        /// <summary>
        /// Moves the node to the <paramref name="newXPosition"/> in world space, taking <paramref name="duration"/> seconds.
        /// </summary>
        /// <param name="newXPosition">the desired new target X coordinate in world space</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <param name="updateEdges">if true, the connecting edges will be moved along with the node</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> MoveXTo(float newXPosition, float duration = DefaultAnimationDuration, bool updateEdges = true)
        {
            updateLayoutDuration = duration;
            this.updateEdges = updateEdges;
            return PositionX.AnimateTo(newXPosition, duration);
        }

        /// <summary>
        /// Moves the node to the <paramref name="newYPosition"/> in world space, taking <paramref name="duration"/> seconds.
        /// </summary>
        /// <param name="newYPosition">the desired new target Y coordinate in world space</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <param name="updateEdges">if true, the connecting edges will be moved along with the node</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> MoveYTo(float newYPosition, float duration = DefaultAnimationDuration, bool updateEdges = true)
        {
            updateLayoutDuration = duration;
            this.updateEdges = updateEdges;
            return PositionY.AnimateTo(newYPosition, duration);
        }

        /// <summary>
        /// Moves the node to the <paramref name="newZPosition"/> in world space, taking <paramref name="duration"/> seconds.
        /// </summary>
        /// <param name="newZPosition">the desired new target Z coordinate in world space</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <param name="updateEdges">if true, the connecting edges will be moved along with the node</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> MoveZTo(float newZPosition, float duration = DefaultAnimationDuration, bool updateEdges = true)
        {
            updateLayoutDuration = duration;
            this.updateEdges = updateEdges;
            return PositionZ.AnimateTo(newZPosition, duration);
        }

        /// <summary>
        /// Moves the node to the <paramref name="newPosition"/> in world space, taking <paramref name="duration"/> seconds.
        /// </summary>
        /// <param name="newPosition">the desired new target position in world space</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <param name="updateEdges">if true, the connecting edges will be moved along with the node</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> MoveTo(Vector3 newPosition, float duration = DefaultAnimationDuration, bool updateEdges = true)
        {
            updateLayoutDuration = duration;
            this.updateEdges = updateEdges;
            return new AndCombinedOperationCallback<Action>(new[]
            {
                PositionX.AnimateTo(newPosition.x, duration),
                PositionY.AnimateTo(newPosition.y, duration),
                PositionZ.AnimateTo(newPosition.z, duration)
            }, a => a);
        }

        /// <summary>
        /// Rotates the node to the given quaternion <paramref name="newRotation"/>,
        /// taking <paramref name="duration"/> seconds.
        /// </summary>
        /// <param name="newRotation">the desired new target rotation in world space</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> RotateTo(Quaternion newRotation, float duration = DefaultAnimationDuration)
        {
            return Rotation.AnimateTo(newRotation, duration);
        }

        /// <summary>
        /// Rotates the node around the given <paramref name="axis"/> by the given <paramref name="angle"/>, taking
        /// <paramref name="duration"/> seconds.
        /// </summary>
        /// <param name="axis">the axis to rotate around</param>
        /// <param name="angle">the angle to rotate by</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> RotateTo(Vector3 axis, float angle, float duration = DefaultAnimationDuration)
        {
            Quaternion rotation = Quaternion.AngleAxis(angle, axis);
            return RotateTo(rotation, duration);
        }

        /// <summary>
        /// Scales the node to the given <paramref name="newLocalScale"/>, taking <paramref name="duration"/> seconds.
        /// </summary>
        /// <param name="newLocalScale">the desired new target scale, more precisely, factor by which the game object
        /// should be scaled relative to its parent (i.e., local scale)</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <param name="updateEdges">if true, the connecting edges will be moved along with the node</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> ScaleTo(Vector3 newLocalScale, float duration = DefaultAnimationDuration, bool updateEdges = true)
        {
            updateLayoutDuration = duration;
            this.updateEdges = updateEdges;
            return Scale.AnimateTo(newLocalScale, duration);
        }

        /// <summary>
        /// Makes the node blink <paramref name="blinkCount"/> times, taking <paramref name="duration"/> seconds.
        /// </summary>
        /// <param name="blinkCount">The number of times the node should blink.</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the blinking is stopped before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> Blink(int blinkCount, float duration = DefaultAnimationDuration)
        {
            return Blinking.AnimateTo(blinkCount, duration);
        }

        /// <summary>
        /// Shows the label with given <paramref name="alpha"/> value if it is greater than zero.
        /// Otherwise, hides the label.
        /// </summary>
        /// <param name="alpha">the desired target alpha value for the label.</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> FadeLabel(float alpha, float duration = DefaultAnimationDuration) =>
            new AndCombinedOperationCallback<Action>(new[]
            {
                // NOTE: Order is important, because the line's end position target depends on the text position target,
                //       and the text position target depends on the alpha value's target!
                LabelAlpha.AnimateTo(alpha, duration),
                LabelTextPosition.AnimateTo(DesiredLabelTextPosition, duration),
                LabelStartLinePosition.AnimateTo(DesiredLabelStartLinePosition, duration),
                LabelEndLinePosition.AnimateTo(DesiredLabelEndLinePosition, duration)
            });

        #endregion

        /// <summary>
        /// Updates the layout of attached objects (edges, labels) immediately.
        /// </summary>
        /// <param name="duration">Time in seconds the animation should take.</param>
        private void UpdateLayout(float duration)
        {
            if (Node != null)
            {
                Assert.IsNotNull(Node, $"[{nameof(NodeOperator)}]{gameObject.FullName()} has undefined graph node");
                if (!Node.IsRoot())
                {
                    // If we are moving the root node, the whole graph will be moved,
                    // hence, the layout of the edges does not need to be updated.
                    if (updateEdges && City.EdgeLayoutSettings.Kind != EdgeLayoutKind.None)
                    {
                        // The edge layout needs to be updated only if we actually have an edge layout.
                        UpdateEdgeLayout(duration);
                    }
                }
                UpdateLabelLayout(duration);
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
            ourTransform.position = new Vector3(PositionX.TargetValue, PositionY.TargetValue, PositionZ.TargetValue);
            Vector3 oldScale = ourTransform.localScale;
            ourTransform.localScale = Scale.TargetValue;
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
                EdgeOperator edgeOperator = gameEdge.AddOrGetComponent<EdgeOperator>();
                edgeOperator.MorphTo(layoutEdges[gameEdge.name].Spline, duration);
            }
        }

        protected override TweenOperation<Color> InitializeColorOperation()
        {
            Material material = GetRenderer(gameObject).material;

            Tween[] AnimateToColorAction(Color color, float d)
            {
                return new Tween[]
                {
                    material.DOColor(color, d).Play()
                };
            }

            return new TweenOperation<Color>(AnimateToColorAction, material.color);
        }

        protected override Color ModifyColor(Color color, Func<Color, Color> modifier)
        {
            return modifier(color);
        }

        protected override IEnumerable<Color> AsEnumerable(Color color)
        {
            return new[] { color };
        }

        protected void OnEnable()
        {
            Node = GetNode(gameObject);
            City = GetCity(gameObject);
            Material material = GetRenderer(gameObject).material;
            Vector3 currentPosition = transform.position;
            Vector3 currentScale = transform.localScale;
            Quaternion currentRotation = transform.rotation;

            Tween[] AnimateToXAction(float x, float d) => new Tween[] { transform.DOMoveX(x, d).Play() };
            Tween[] AnimateToYAction(float y, float d) => new Tween[] { transform.DOMoveY(y, d).Play() };
            Tween[] AnimateToZAction(float z, float d) => new Tween[] { transform.DOMoveZ(z, d).Play() };
            Tween[] AnimateToRotationAction(Quaternion r, float d) => new Tween[] { transform.DORotateQuaternion(r, d).Play() };
            PositionX = new TweenOperation<float>(AnimateToXAction, currentPosition.x);
            PositionY = new TweenOperation<float>(AnimateToYAction, currentPosition.y);
            PositionZ = new TweenOperation<float>(AnimateToZAction, currentPosition.z);
            Rotation = new TweenOperation<Quaternion>(AnimateToRotationAction, currentRotation);

            Tween[] AnimateToScaleAction(Vector3 s, float d) => new Tween[] { transform.DOScale(s, d).Play() };
            Scale = new TweenOperation<Vector3>(AnimateToScaleAction, currentScale);

            PrepareLabel();
            LabelAlpha = new TweenOperation<float>(AnimateLabelAlphaAction, 0f);
            LabelTextPosition = new TweenOperation<Vector3>(AnimateLabelTextPositionAction, DesiredLabelTextPosition);
            LabelStartLinePosition = new TweenOperation<Vector3>(AnimateLabelStartLinePositionAction, DesiredLabelStartLinePosition);
            LabelEndLinePosition = new TweenOperation<Vector3>(AnimateLabelEndLinePositionAction, DesiredLabelEndLinePosition);

            Tween[] BlinkAction(int count, float duration)
            {
                if (color.IsRunning)
                {
                    color.KillAnimator(true);
                }
                // If we're interrupting another blinking, we need to make sure the color still has the correct value.
                material.color = color.TargetValue;

                return new Tween[]
                {
                    material.DOColor(color.TargetValue.Invert(), duration / (2 * count)).SetEase(Ease.Linear).SetLoops(2 * count, LoopType.Yoyo).Play()
                };
            }

            Blinking = new TweenOperation<int>(BlinkAction, 0, equalityComparer: new AlwaysFalseEqualityComparer<int>());

            base.OnEnable();

            #region Local Methods

            static Node GetNode(GameObject gameObject)
            {
                if (!gameObject.TryGetComponent(out NodeRef nodeRef) || nodeRef.Value == null)
                {
                    throw new InvalidOperationException($"NodeOperator-operated object {gameObject.FullName()} must have {nameof(NodeRef)} attached!");
                }

                return nodeRef.Value;
            }

            static AbstractSEECity GetCity(GameObject gameObject)
            {
                GameObject codeCityObject = SceneQueries.GetCodeCity(gameObject.transform)?.gameObject;
                if (codeCityObject == null || !codeCityObject.TryGetComponent(out AbstractSEECity city))
                {
                    throw new InvalidOperationException($"NodeOperator-operated object {gameObject.FullName()}"
                                                        + $" in code city {CodeCityName(codeCityObject)}"
                                                        + $" must have an {nameof(AbstractSEECity)} component!");
                }

                return city;

                static string CodeCityName(GameObject codeCityObject)
                {
                    return codeCityObject ? codeCityObject.FullName() : "<null>";
                }
            }

            #endregion
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
            PositionX.KillAnimator();
            PositionX = null;
            PositionY.KillAnimator();
            PositionY = null;
            PositionZ.KillAnimator();
            PositionZ = null;
            Rotation.KillAnimator();
            Rotation = null;
            Scale.KillAnimator();
            Scale = null;
            LabelAlpha.KillAnimator();
            LabelAlpha = null;
            LabelTextPosition.KillAnimator();
            LabelTextPosition = null;
            LabelStartLinePosition.KillAnimator();
            LabelStartLinePosition = null;
            LabelEndLinePosition.KillAnimator();
            LabelEndLinePosition = null;
            Destroyer.Destroy(nodeLabel);
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