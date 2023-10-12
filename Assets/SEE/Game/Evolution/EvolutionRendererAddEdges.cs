using SEE.DataModel.DG;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// Part of the <see cref="EvolutionRenderer"/> taking care of adding new edges.
    /// The code following here implements phase (5).
    /// </summary>
    public partial class EvolutionRenderer
    {
        /// <summary>
        /// A <see cref="CountingJoin"/> triggering the next animation phase when phase 5 has been
        /// completed, that is, if all awaited events have occurred.
        /// </summary>
        private CountingJoin phase5Join;

        /// <summary>
        /// Implements the fifth phase in the transition from the <see cref="currentCity"/>
        /// to the <paramref name="nextCity"/>.
        /// In this phase, all <see cref="addedEdges"/> will be rendered by new game
        /// objects. When their animated appearance has finished,
        /// <see cref="OnAnimationsFinished"/> will be called.
        /// </summary>
        /// <param name="next">the next graph to be drawn</param>
        private void Phase5AddNewEdges(LaidOutGraph next)
        {
            if (edgesAreDrawn)
            {
                AssertAllJoinsAreZero();
                phase5Join.Await(addedEdges.Count, OnAnimationsFinished, $"Graph {next.Graph.Name} Phase 5: Adding {addedEdges.Count} new edges.");
                if (addedEdges.Count > 0)
                {
                    City.EdgeAnimationKind animationKind = Renderer.Settings.EdgeLayoutSettings.AnimationKind;
                    foreach (Edge edge in addedEdges)
                    {
                        objectManager.GetEdge(edge, out GameObject edgeObject);
                        if (edgeObject.TryGetComponent(out SEESpline spline))
                        {
                            spline.Spline = nextCity.EdgeLayout[edge.ID].Spline;
                        }
                        edgeObject.AddOrGetComponent<EdgeOperator>()
                                  .Show(animationKind, AnimationLagFactor)
                                  .SetOnComplete(phase5Join.Finished);
                    }
                }
            }
            else
            {
                Debug.Log($"Phase 5: No edge layout enbabled. No new edges.\n");
                OnAnimationsFinished();
            }
        }
    }
}
