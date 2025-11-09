using SEE.DataModel.DG;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Layout;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.Evolution
{
  /// <summary>
  /// Part of the <see cref="EvolutionRenderer"/> taking care of adjusting
  /// the scale and style of graph elements existing in the currently shown
  /// graph and the next one.
  /// The code following here implements phase (3).
  /// </summary>
  public partial class EvolutionRenderer
  {
    /// <summary>
    /// Implements the third phase in the transition from the <see cref="currentCity"/>
    /// to the <paramref name="nextCity"/>.
    /// In this phase, the scale and style of all existing nodes (nodes in both graphs,
    /// no matter whether they were changed or not) will adjusted.
    /// When this phase has been completed, <see cref="Phase4AddNewNodes"/>
    /// will be called.
    /// </summary>
    private void Phase3AdjustExistingGraphElements()
    {
      // Even the equal nodes need adjustments because the layout could have
      // changed their dimensions. The treemap layout, for instance, may do that.
      int changedElements = equalNodes.Count + changedNodes.Count;
      Debug.Log($"Phase 3: Adjusting {changedElements} changed graph elements.\n");
      animationWatchDog.Await(changedElements, Phase4AddNewNodes);
      if (changedElements > 0)
      {
        equalNodes.ForEach(AdjustExistingNode);
        changedNodes.ForEach(AdjustExistingNode);
      }
    }

    /// <summary>
    /// Adjusts the scale and style of the game object corresponding to <paramref name="graphNode"/>.
    /// At the end of the animation, <see cref="OnScalingFinished"/> will be called.
    /// </summary>
    /// <param name="graphNode">graph node whose game node is to be adjusted</param>
    private void AdjustExistingNode(Node graphNode)
    {
      Assert.IsNotNull(graphNode);
      ILayoutNode layoutNode = NextLayoutToBeShown[graphNode.ID];
      // The game node representing the graphNode if there is any; null if there is none
      Node formerGraphNode = objectManager.GetNode(graphNode, out GameObject gameNode);
      Assert.IsTrue(gameNode.HasNodeRef());
      Assert.IsNotNull(formerGraphNode);
      Assert.AreEqual(gameNode.transform.parent, gameObject.transform);

      // There is a change. It may or may not be the metric determining the style.
      // We will not further check that and just call the following method.
      // If there is no change, this method does not need to be called because then
      // we know that the metric values determining the style and antenna of the former
      // and the new graph node are the same.
      Renderer.AdjustStyle(gameNode);

      ScaleTo(gameNode, layoutNode);

      void ScaleTo(GameObject gameNode, ILayoutNode layoutNode)
      {
        // layoutNode.LocalScale is in world space, while the animation by iTween
        // is in local space. Our game objects may be nested in other game objects,
        // hence, the two spaces may be different.
        // We may need to transform layoutNode.LocalScale from world space to local space.
        Vector3 localScale = gameNode.transform.parent == null ?
                                 layoutNode.AbsoluteScale
                               : gameNode.transform.parent.InverseTransformVector(layoutNode.AbsoluteScale);

        gameNode.NodeOperator()
                .ScaleTo(localScale, AnimationLagFactor, updateEdges: false)
                .OnComplete(() => OnScalingFinished(gameNode));
      }
    }

    /// <summary>
    /// Adjusts the antenna and the marker of <paramref name="gameNode"/> because
    /// its height might have changed.
    /// Notifies <see cref="animationWatchDog"/> that the animation of this <paramref name="gameNode"/>
    /// has finished.
    /// </summary>
    /// <param name="gameNode">game node whose antenna and marker need to be adjusted</param>
    private void OnScalingFinished(object gameNode)
    {
      if (gameNode is GameObject go)
      {
        Renderer.AdjustAntenna(go);
        markerFactory.AdjustMarkerY(go);
      }
      animationWatchDog.Finished();
    }
  }
}
