using UnityEngine;
using System.Collections.Generic;

namespace SEE.Layout
{
    /// <summary>
    /// Creates nodes for testing.
    /// </summary>
    public static class NodeCreator
    {
        /// <summary>
        /// Creates and returns <paramref name="howManyNodes"/> nodes.
        /// </summary>
        /// <param name="howManyNodes"></param>
        /// <returns><paramref name="howManyNodes"/> new nodes</returns>
        public static ICollection<ILayoutNode> CreateNodes(int howManyNodes = 500)
        {            
            Vector3 initialSize = Vector3.one;
            LayoutVertex root = new LayoutVertex(initialSize, 0);

            ICollection<ILayoutNode> gameObjects = new List<ILayoutNode>();
            gameObjects.Add(root);

            for (int i = 1; i <= howManyNodes; i++)
            {
                initialSize *= 1.01f;
                LayoutVertex child = new LayoutVertex(initialSize, i);
                gameObjects.Add(child);
                root.AddChild(child);
            }
            return gameObjects;
        }
    }
}