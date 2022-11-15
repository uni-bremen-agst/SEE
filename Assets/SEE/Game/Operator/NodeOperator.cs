using System;
using System.Linq;
using DG.Tweening;
using SEE.DataModel.DG;
using SEE.GO;
using TinySpline;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SEE.Game.Operator
{
    /// <summary>
    /// A component managing operations done on the node it is attached to.
    /// Available operations consist of the public methods exported by this class.
    /// Operations can be animated or executed directly, by setting the duration to 0.
    /// </summary>
    public class NodeOperator : AbstractOperator
    {
        // We split up movement on the three axes because it makes sense in certain situations.
        // For example, when dragging a node along the XZ-axis, if we want to place it on top
        // of the node we hover over, we might want to animate this Y-axis movement while
        // still letting the user drag the node.

        /// <summary>
        /// Operation handling X-axis movement.
        /// </summary>
        private TweenOperation<float> PositionX;

        /// <summary>
        /// Operation handling Y-axis movement.
        /// </summary>
        private TweenOperation<float> PositionY;

        /// <summary>
        /// Operation handling Z-axis movement.
        /// </summary>
        private TweenOperation<float> PositionZ;

        /// <summary>
        /// Operation handling node scaling (specifically, localScale).
        /// </summary>
        private TweenOperation<Vector3> Scale;

        /// <summary>
        /// If this isn't null, represents the duration in seconds the edge layout update should take,
        /// and if this is null, the edge layout shall not be updated.
        /// </summary>
        private float? updateEdgeLayoutDuration;

        /// <summary>
        /// The position this node is supposed to be at.
        /// </summary>
        /// <seealso cref="AbstractOperator"/>
        public Vector3 TargetPosition => new Vector3(PositionX.TargetValue, PositionY.TargetValue, PositionZ.TargetValue);

        /// <summary>
        /// The scale this node is supposed to be at.
        /// </summary>
        /// <seealso cref="AbstractOperator"/>
        public Vector3 TargetScale => Scale.TargetValue;

        #region Public API

        /// <summary>
        /// Moves the node to the <paramref name="newXPosition"/>, taking <paramref name="duration"/> seconds.
        /// </summary>
        /// <param name="newXPosition">the desired new target X coordinate</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> MoveXTo(float newXPosition, float duration)
        {
            updateEdgeLayoutDuration = duration;
            return PositionX.AnimateTo(newXPosition, duration);
        }

        /// <summary>
        /// Moves the node to the <paramref name="newYPosition"/>, taking <paramref name="duration"/> seconds.
        /// </summary>
        /// <param name="newYPosition">the desired new target Y coordinate</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> MoveYTo(float newYPosition, float duration)
        {
            updateEdgeLayoutDuration = duration;
            return PositionY.AnimateTo(newYPosition, duration);
        }

        /// <summary>
        /// Moves the node to the <paramref name="newZPosition"/>, taking <paramref name="duration"/> seconds.
        /// </summary>
        /// <param name="newZPosition">the desired new target Z coordinate</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> MoveZTo(float newZPosition, float duration)
        {
            updateEdgeLayoutDuration = duration;
            return PositionZ.AnimateTo(newZPosition, duration);
        }

        /// <summary>
        /// Moves the node to the <paramref name="newPosition"/>, taking <paramref name="duration"/> seconds.
        /// </summary>
        /// <param name="newPosition">the desired new target position</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> MoveTo(Vector3 newPosition, float duration)
        {
            updateEdgeLayoutDuration = duration;
            return new AndCombinedOperationCallback<Action>(new[]
            {
                PositionX.AnimateTo(newPosition.x, duration),
                PositionY.AnimateTo(newPosition.y, duration),
                PositionZ.AnimateTo(newPosition.z, duration)
            }, a => a);
        }

        /// <summary>
        /// Scales the node to the given <paramref name="newLocalScale"/>, taking <paramref name="duration"/> seconds.
        /// </summary>
        /// <param name="newLocalScale">the desired new target scale, more precisely, factor by which the game object
        /// should be scaled relative to its parent (i.e., local scale)</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> ScaleTo(Vector3 newLocalScale, float duration)
        {
            updateEdgeLayoutDuration = duration;
            return Scale.AnimateTo(newLocalScale, duration);
        }

        /// <summary>
        /// Updates the edges attached to this node during the next <see cref="Update"/> cycle.
        /// This involves recalculating the edge layout for each attached edge.
        /// Note that this is already automatically done when the node is moved or scaled.
        /// </summary>
        /// <param name="duration">Time in seconds the animation should take.</param>
        public void UpdateAttachedEdges(float duration)
        {
            updateEdgeLayoutDuration = duration;
        }

        #endregion

        /// <summary>
        /// Updates the edges attached to this node.
        /// This involves recalculating the edge layout for each attached edge.
        /// </summary>
        /// <param name="duration">Time in seconds the animation should take.</param>
        private void UpdateEdgeLayout(float duration)
        {
            // We remember the old position and scale, and move the node to the new position and scale so that
            // edge layouts (dependent on position and scale) can be correctly calculated.
            Vector3 oldPosition = transform.position;
            transform.position = new Vector3(PositionX.TargetValue, PositionY.TargetValue, PositionZ.TargetValue);
            Vector3 oldScale = transform.localScale;
            transform.localScale = Scale.TargetValue;
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

            // Recalculate edge layout and animate edges due to new node positioning.
            // TODO: Iterating over all game edges is currently very costly,
            //       consider adding a cached mapping either here or in SceneQueries.
            //       Alternatively, we can iterate over game edges instead.
            foreach (Edge edge in node.Incomings.Union(node.Outgoings).Where(x => !x.HasToggle(Edge.IsVirtualToggle)))
            {
                // Add new target edge, we'll animate the current edge to it.
                GameObject gameEdge = GraphElementIDMap.Find(edge.ID);
                if (gameEdge == null)
                {
                    // TODO: How is this possible?
                    continue;
                }

                GameObject source = edge.Source == node ? gameObject : edge.Source.RetrieveGameNode();
                GameObject target = edge.Target == node ? gameObject : edge.Target.RetrieveGameNode();
                // NOTE: newEdge will not be added to the GraphElementIDMap.
                GameObject newEdge = GameEdgeAdder.Add(source, target, edge.Type, edge);

                EdgeOperator edgeOperator = gameEdge.AddOrGetComponent<EdgeOperator>();
                newEdge.MustGetComponent(out SEESpline newSpline);
                BSpline targetSpline = newSpline.Spline;

                // NOTE: We *must not* use the Destroyer class here. newEdge hasn't been added to the GraphElementIDMap,
                //       and newEdge.name == gameEdge.name, so it would delete the original gameEdge from the map.
                // TODO: Instead of this work around, there should be a way to work on BSpline's alone, without
                //       having to use SEESpline as a wrapper.
                Destroy(newEdge);
                edgeOperator.MorphTo(targetSpline, duration);
            }

            // Once we're done, we reset the gameObject to its original position.
            transform.position = oldPosition;
            transform.localScale = oldScale;  // FIXME: This is suspicious. What if the node is scaled at the same time?
        }

        private void OnEnable()
        {
            Vector3 currentPosition = transform.position;
            Tween[] AnimateToXAction(float x, float d) => new Tween[] {transform.DOMoveX(x, d).Play()};
            Tween[] AnimateToYAction(float y, float d) => new Tween[] {transform.DOMoveY(y, d).Play()};
            Tween[] AnimateToZAction(float z, float d) => new Tween[] {transform.DOMoveZ(z, d).Play()};
            PositionX = new TweenOperation<float>(AnimateToXAction, currentPosition.x);
            PositionY = new TweenOperation<float>(AnimateToYAction, currentPosition.y);
            PositionZ = new TweenOperation<float>(AnimateToZAction, currentPosition.z);

            Vector3 currentScale = transform.localScale;
            Tween[] AnimateToScaleAction(Vector3 s, float d) => new Tween[] {transform.DOScale(s, d).Play()};
            Scale = new TweenOperation<Vector3>(AnimateToScaleAction, currentScale);
        }

        private void OnDisable()
        {
            PositionX.KillAnimator();
            PositionX = null;
            PositionY.KillAnimator();
            PositionY = null;
            PositionZ.KillAnimator();
            PositionZ = null;
            Scale.KillAnimator();
            Scale = null;
        }

        private void Update()
        {
            if (updateEdgeLayoutDuration.HasValue)
            {
                UpdateEdgeLayout(updateEdgeLayoutDuration.Value);
                updateEdgeLayoutDuration = null;
            }
        }
    }
}