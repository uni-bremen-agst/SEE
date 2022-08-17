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

        private readonly TweenOperation<float> PositionX = new TweenOperation<float>();
        private readonly TweenOperation<float> PositionY = new TweenOperation<float>();
        private readonly TweenOperation<float> PositionZ = new TweenOperation<float>();

        private readonly TweenOperation<Vector3> Scale = new TweenOperation<Vector3>();
        // TODO: private readonly OperationCategory<Color> Color = new OperationCategory<Color>();

        private float? updateEdgeLayoutDuration;

        #region Public API

        public void MoveNodeX(float newXPosition, float duration)
        {
            PositionX.AnimateTo(newXPosition, duration);
            updateEdgeLayoutDuration = duration;
        }

        public void MoveNodeY(float newYPosition, float duration)
        {
            PositionY.AnimateTo(newYPosition, duration);
            updateEdgeLayoutDuration = duration;
        }

        public void MoveNodeZ(float newZPosition, float duration)
        {
            PositionZ.AnimateTo(newZPosition, duration);
            updateEdgeLayoutDuration = duration;
        }
        
        public void MoveNode(Vector3 newPosition, float duration)
        {
            PositionX.AnimateTo(newPosition.x, duration);
            PositionY.AnimateTo(newPosition.y, duration);
            PositionZ.AnimateTo(newPosition.z, duration);
            updateEdgeLayoutDuration = duration;
        }

        public void ScaleNode(Vector3 newScale, float duration)
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
            Node node = gameObject.GetNode();

            // Recalculate edge layout and animate edges due to new node positioning.
            // TODO: Iterating over all game edges is currently very costly,
            //       consider adding a cached mapping either here or in SceneQueries.
            //       Alternatively, we can iterate over game edges instead.
            foreach (Edge edge in node.Incomings.Union(node.Outgoings).Where(x => !x.HasToggle(Edge.IsVirtualToggle)))
            {
                GameObject gameEdge = GameObject.Find(edge.ID);
                Assert.IsNotNull(gameEdge);
                GameObject source = edge.Source == node ? gameObject : edge.Source.RetrieveGameNode();
                GameObject target = edge.Target == node ? gameObject : edge.Target.RetrieveGameNode();
                GameObject newEdge = GameEdgeAdder.Add(source, target, edge.Type, edge);
                AddEdgeSplineAnimation(gameEdge, newEdge, duration);
            }

            // Once we're done, we reset the gameObject to its original position.
            transform.position = oldPosition;
            transform.localScale = oldScale;

            #region Local Methods

            static void AddEdgeSplineAnimation(GameObject sourceEdge, GameObject targetEdge, float duration)
            {
                // TODO: Move this to EdgeOperator
                if (targetEdge.TryGetComponentOrLog(out SEESpline splineTarget)
                    && sourceEdge.TryGetComponentOrLog(out SEESpline splineSource))
                {
                    // We deactivate the target edge first so it's not visible.
                    targetEdge.SetActive(false);
                    // We now use the EdgeAnimator and SplineMorphism to actually move the edge.
                    if (!splineSource.gameObject.TryGetComponent(out SplineMorphism morphism))
                    {
                        morphism = splineSource.gameObject.AddComponent<SplineMorphism>();
                    }

                    if (morphism.IsActive())
                    {
                        // A tween already exists, we simply need to change its target.
                        morphism.ChangeTarget(splineTarget.Spline);
                    }
                    else
                    {
                        morphism.CreateTween(splineSource.Spline, splineTarget.Spline, duration)
                                .OnComplete(() => Destroy(targetEdge)).Play();
                    }
                }
            }

            #endregion
        }

        private void Awake()
        {
            Vector3 currentPosition = transform.position;
            PositionX.AnimateToAction = (x, d) => transform.DOMoveX(x, d).Play();
            PositionX.TargetValue = currentPosition.x;
            PositionY.AnimateToAction = (y, d) => transform.DOMoveY(y, d).Play();
            PositionY.TargetValue = currentPosition.y;
            PositionZ.AnimateToAction = (z, d) => transform.DOMoveZ(z, d).Play();
            PositionZ.TargetValue = currentPosition.z;

            Vector3 currentScale = transform.localScale;
            Scale.AnimateToAction = (s, d) => transform.DOScale(s, d).Play();
            Scale.TargetValue = currentScale;
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