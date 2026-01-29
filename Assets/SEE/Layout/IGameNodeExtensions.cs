using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Extension methods for IGameNode.
    /// </summary>
    public static class IGameNodeExtensions
    {
        /// <summary>
        /// Prints all properties of the IGameNode to the console.
        /// </summary>
        /// <param name="node">The node to print.</param>
        public static string Print(this IGameNode node)
        {
            return $"IGameNode Properties:\n" +
                      $"  ID: {node.ID}\n" +
                      $"  AbsoluteScale: {node.AbsoluteScale}\n" +
                      $"  CenterPosition: {node.CenterPosition}\n" +
                      $"  Rotation: {node.Rotation}\n" +
                      $"  Roof: {node.Roof}\n" +
                      $"  Ground: {node.Ground}";

        }
    }
}