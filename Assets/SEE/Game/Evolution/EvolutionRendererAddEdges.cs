using SEE.DataModel.DG;
using SEE.GO;
using UnityEngine;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// Part of the <see cref="EvolutionRenderer"/> taking care of adding new edges.
    /// The code following here implements phase (5).
    /// </summary>
    public partial class EvolutionRenderer
    {
        /// <summary>
        /// Implements the fifth phase in the transition from the <see cref="currentCity"/>
        /// to the <paramref name="nextCity"/>.
        /// In this phase, all <see cref="addedEdges"/> will be rendered by new game
        /// objects. When their animated appearance has finished,
        /// <see cref="OnAnimationsFinished"/> will be called.
        /// </summary>
        private void Phase5AddNewEdges()
        {
            if (edgesAreDrawn)
            {
                Debug.Log($"Phase 5: Adding {addedEdges.Count} new edges.\n");
                animationWatchDog.Await(addedEdges.Count, OnAnimationsFinished);
                City.EdgeAnimationKind animationKind = Renderer.Settings.EdgeLayoutSettings.AnimationKind;
                foreach (Edge edge in addedEdges)
                {
                    objectManager.GetEdge(edge, out GameObject edgeObject);
                    if (edgeObject.TryGetComponent(out SEESpline spline))
                    {
                        spline.Spline = nextCity.EdgeLayout[edge.ID].Spline;
                    }
                    edgeObject.EdgeOperator()
                              .Show(animationKind, AnimationLagFactor)
                              .OnComplete(animationWatchDog.Finished);
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
