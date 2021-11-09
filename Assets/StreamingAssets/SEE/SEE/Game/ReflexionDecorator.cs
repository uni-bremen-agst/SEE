using SEE.DataModel;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game
{
    internal class ReflexionDecorator
    {

        internal ReflexionDecorator
            (GameObject absencePrefab,
             GameObject convergencePrefab,
             GameObject divergencePrefab,
             Vector2 leftFrontCorner,
             Vector2 rightBackCorner)
        {
            this.absencePrefab = absencePrefab;
            this.convergencePrefab = convergencePrefab;
            this.divergencePrefab = divergencePrefab;
            this.leftFrontCorner = leftFrontCorner;
            this.rightBackCorner = rightBackCorner;
        }

        /// <summary>
        /// Left front corner of the culling portal.
        /// </summary>
        private readonly Vector2 leftFrontCorner;
        /// <summary>
        /// Right front corner of the culling portal.
        /// </summary>
        private readonly Vector2 rightBackCorner;

        /// <summary>
        /// Prefab for absences.
        /// </summary>
        private readonly GameObject absencePrefab;
        /// <summary>
        /// Prefab for convergences.
        /// </summary>
        private readonly GameObject convergencePrefab;
        /// <summary>
        /// Prefab for divergences.
        /// </summary>
        private readonly GameObject divergencePrefab;

        /// <summary>
        /// Decorates the game object representing the given <paramref name="edge"/> edge
        /// with the decorations created from the given <paramref name="prefab"/>. The
        /// decorations are created along and just above the line with some distance
        /// inbetween them. All created decoration objects are added as children to
        /// <paramref name="edge"/>. Their object name will be the given <paramref name="name"/>.
        /// They are tagged by Tags.Decoration.
        /// </summary>
        /// <param name="edge">graph edge whose visualizing game object (with a line renderer)
        /// should be decorated</param>
        /// <param name="prefab">the prefab from which to instantiate the decoration objects</param>
        /// <param name="name">the game-object name for all created decoration objects</param>
        private void DecorateEdge(GameObject gameEdge, GameObject prefab, string name)
        {
            LineRenderer line = gameEdge.GetComponent<LineRenderer>();
            // Demeter of the spheres to be used as decoration along the line.
            float demeter = 2 * line.startWidth; // We assume the line has the same width everywhere.
            // The distance between two subsequent spheres along the line.
            float distanceBetweenDecorations = 5 * demeter;

            float distance = 0.0f;
            Vector3 positionOfLastDecoration = line.GetPosition(0);
            for (int i = 1; i < line.positionCount; i++)
            {
                Vector3 currentPosition = line.GetPosition(i);
                distance += Vector3.Distance(positionOfLastDecoration, currentPosition);
                if (distance >= distanceBetweenDecorations)
                {
                    GameObject decoration = Object.Instantiate(prefab, gameEdge.transform, true);
                    Vector3 dotPosition = currentPosition;
                    decoration.transform.localScale = Vector3.one * demeter;
                    dotPosition.y += line.startWidth + decoration.transform.lossyScale.y / 2.0f; // above the line
                    decoration.transform.position = dotPosition;
                    decoration.tag = Tags.Decoration;
                    decoration.name = name;
                    Portal.SetPortal(decoration.transform, leftFrontCorner, rightBackCorner);
                    distance = 0;
                }
                positionOfLastDecoration = currentPosition;
            }
        }

        /// <summary>
        /// Removes all decorations from given <paramref name="gameEdge"/> that were added
        /// by DecorateEdge().
        /// </summary>
        /// <param name="gameEdge">edge whose decorations are to be removed</param>
        private void UndecorateEdge(GameObject gameEdge)
        {
            foreach (Transform child in gameEdge.transform)
            {
                if (child.CompareTag(Tags.Decoration))
                {
                    Destroyer.DestroyGameObject(child.gameObject);
                }
            }
        }

        /// <summary>
        /// Decorates the given <paramref name="architectureDependency"/> by decorations 
        /// objects created from AbsenceMaterial.
        /// </summary>
        /// <param name="architectureDependency">architecture dependency to be decorated as absence</param>
        internal void DecorateAbsence(GameObject gameEdge)
        {
            DecorateEdge(gameEdge, absencePrefab, "absence");
        }

        /// <summary>
        /// Intended to decorate the given <paramref name="architectureDependency"/> by decorations 
        /// objects created from ConvergenceMaterial. Currently, we do not decorate convergences.
        /// They are expected and just add clutter. A user wants to focus on the discrepancies 
        /// (absences and divergences). However, the edge could previously have been decorated
        /// as absence, in which case we need to remove those decorations.
        /// </summary>
        /// <param name="architectureDependency">architecture dependency to be decorated as convergence</param>
        internal void DecorateConvergence(GameObject gameEdge)
        {
            // No need to decorate convergences. 
            //DecorateEdge(edge, ConvergenceMaterial, "convergence");
        }

        /// <summary>
        /// Decorates the given <paramref name="propagatedDependency"/> by decorations 
        /// objects created from DivergenceMaterial.
        /// </summary>
        /// <param name="propagatedDependency">implementation dependency propagated to the 
        /// architecture to be decorated as divergence</param>
        internal void DecorateDivergence(GameObject gameEdge)
        {
            DecorateEdge(gameEdge, divergencePrefab, "divergence");
        }

        internal void DecorateAllowedAbsence(GameObject gameEdge)
        {
            // Nothing to be done. Will not be decorated because it is allowed to be absent.
        }

        internal void DecorateAllowed(GameObject gameEdge)
        {
            // Nothing to be done. Will not be decorated because it is allowed.
        }

        internal void DecorateImplicitlyAllowed(GameObject gameEdge)
        {
            // Nothing to be done. Will not be decorated because it is allowed.
        }

        internal void UndecorateAbsence(GameObject gameEdge)
        {
            UndecorateEdge(gameEdge);
        }

        internal void UndecorateConvergence(GameObject gameEdge)
        {
            UndecorateEdge(gameEdge);
        }

        internal void UndecorateAllowedAbsence(GameObject gameEdge)
        {
            UndecorateEdge(gameEdge);
        }

        internal void UndecorateDivergence(GameObject gameEdge)
        {
            UndecorateEdge(gameEdge);
        }

        internal void UndecorateAllowed(GameObject gameEdge)
        {
            UndecorateEdge(gameEdge);
        }

        internal void UndecorateImplicitlyAllowed(GameObject gameEdge)
        {
            UndecorateEdge(gameEdge);
        }

    }
}