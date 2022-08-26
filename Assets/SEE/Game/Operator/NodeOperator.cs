using System;
using System.Linq;
using DG.Tweening;
using SEE.DataModel.DG;
using SEE.GO;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.Operator
{
    public class NodeOperator : AbstractOperator
    {
        // We split up movement on the three axes because it makes sense in certain situations.
        // For example, when dragging a node along the XZ-axis, if we want to place it on top
        // of the node we hover over, we might want to animate this while still letting the user
        // drag the node.

        private TweenOperation<float> PositionX;
        private TweenOperation<float> PositionY;
        private TweenOperation<float> PositionZ;

        private TweenOperation<Vector3> Scale;

        private float? updateEdgeLayoutDuration;

        public Vector3 TargetPosition => new Vector3(PositionX.TargetValue, PositionY.TargetValue, PositionZ.TargetValue);
        public Vector3 TargetScale => Scale.TargetValue;

        #region Public API

        public void MoveXTo(float newXPosition, float duration)
        {
            PositionX.AnimateTo(newXPosition, duration);
            updateEdgeLayoutDuration = duration;
        }

        public void MoveYTo(float newYPosition, float duration)
        {
            PositionY.AnimateTo(newYPosition, duration);
            updateEdgeLayoutDuration = duration;
        }

        public void MoveZTo(float newZPosition, float duration)
        {
            PositionZ.AnimateTo(newZPosition, duration);
            updateEdgeLayoutDuration = duration;
        }

        public void MoveTo(Vector3 newPosition, float duration)
        {
            PositionX.AnimateTo(newPosition.x, duration);
            PositionY.AnimateTo(newPosition.y, duration);
            PositionZ.AnimateTo(newPosition.z, duration);
            updateEdgeLayoutDuration = duration;
        }

        public void ScaleTo(Vector3 newScale, float duration)
        {
            Scale.AnimateTo(newScale, duration);
            updateEdgeLayoutDuration = duration;
        }

        public void UpdateAttachedEdges(float duration)
        {
            updateEdgeLayoutDuration = duration;
        }

        #endregion

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
                // Add new target edge, we'll animate the current edge to it
                GameObject gameEdge = GameObject.Find(edge.ID);
                if (gameEdge == null)
                {
                    // FIXME: How is this possible?
                    continue;
                }
                GameObject source = edge.Source == node ? gameObject : edge.Source.RetrieveGameNode();
                GameObject target = edge.Target == node ? gameObject : edge.Target.RetrieveGameNode();
                GameObject newEdge = GameEdgeAdder.Add(source, target, edge.Type, edge);

                EdgeOperator edgeOperator = gameEdge.AddOrGetComponent<EdgeOperator>();
                if (newEdge.TryGetComponentOrLog(out SEESpline newSpline))
                {
                    edgeOperator.MorphTo(newSpline, duration);
                }
            }

            // Once we're done, we reset the gameObject to its original position.
            transform.position = oldPosition;
            transform.localScale = oldScale;
        }

        private void OnEnable()
        {
            Vector3 currentPosition = transform.position;
            Tween[] AnimateToXAction(float x, float d) => new Tween[] { transform.DOMoveX(x, d).Play() };
            Tween[] AnimateToYAction(float y, float d) => new Tween[] { transform.DOMoveY(y, d).Play() };
            Tween[] AnimateToZAction(float z, float d) => new Tween[] { transform.DOMoveZ(z, d).Play() };
            PositionX = new TweenOperation<float>(AnimateToXAction, currentPosition.x);
            PositionY = new TweenOperation<float>(AnimateToYAction, currentPosition.y);
            PositionZ = new TweenOperation<float>(AnimateToZAction, currentPosition.z);

            Vector3 currentScale = transform.localScale;
            Tween[] AnimateToScaleAction(Vector3 s, float d) => new Tween[] { transform.DOScale(s, d).Play() };
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