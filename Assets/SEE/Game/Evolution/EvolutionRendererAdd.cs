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
    /// Part of the <see cref="EvolutionRenderer"/> taking care of adding new
    /// graph elements.
    /// The code following here implements phase (4).
    /// </summary>
    public partial class EvolutionRenderer
    {
        /// <summary>
        /// Implements the fourth phase in the transition from the <see cref="currentCity"/>
        /// to the <paramref name="nextCity"/>.
        /// In this phase, all <see cref="addedNodes"/> and <see cref="addedEdges"/> will
        /// be rendered by new game objects. When their animated appearance has
        /// finished, <see cref="OnAnimationsFinished"/> will be called.
        /// </summary>
        private void Phase4AddNewGraphElements()
        {
            int addedElements = addedNodes.Count + addedEdges.Count;
            Debug.Log($"Phase4AddNewGraphElements: {addedElements}\n");
            animationWatchDog.Await(addedElements, OnAnimationsFinished);
            if (addedElements > 0)
            {
                addedNodes.ForEach(AddNode);
            }
        }

        /// <summary>
        /// Creates a game node for the given <paramref name="graphNode"/>. Its appearance
        /// will be animated as if it were flying down from the sky.
        /// </summary>
        /// <param name="graphNode">graph node for which to create a game node</param>
        private void AddNode(Node graphNode)
        {
            Assert.IsNotNull(graphNode);
            ILayoutNode layoutNode = NextLayoutToBeShown[graphNode.ID];
            // The game node representing the graphNode if there is any; null if there is none
            Node formerGraphNode = objectManager.GetNode(graphNode, out GameObject gameNode);
            Assert.IsTrue(gameNode.HasNodeRef());
            Assert.IsNull(formerGraphNode);

            Add(gameNode, layoutNode);

            void Add(GameObject gameNode, ILayoutNode layoutNode)
            {
                // A new node has no layout applied to it yet.
                // If the node is new, we animate it by moving it out from the sky.
                Vector3 initialPosition = layoutNode.CenterPosition;
                initialPosition.y = SkyLevel + layoutNode.AbsoluteScale.y;
                gameNode.transform.position = initialPosition;

                gameNode.SetAbsoluteScale(layoutNode.AbsoluteScale, animate: false);

                // The node is new. Hence, it has no parent yet. It must be contained
                // in a code city though; otherwise the NodeOperator would not work.
                gameNode.transform.SetParent(gameObject.transform);

                gameNode.AddOrGetComponent<NodeOperator>()
                    .MoveTo(layoutNode.CenterPosition, AnimationLagPerPhase(), updateEdges: edgesAreDrawn)
                    .SetOnComplete(animationWatchDog.Finished);
            }
        }
    }
}
