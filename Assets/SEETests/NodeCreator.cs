using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
  /// <summary>
  /// Creates nodes for testing.
  /// </summary>
  public static class NodeCreator
  {
    /// <summary>
    /// Creates and returns <paramref name="howManyRootNodes"/> root nodes
    /// where each root node is the root of a <paramref name="howDeeplyNested"/>
    /// binary tree.
    /// </summary>
    /// <param name="howManyRootNodes">how many root nodes to be created</param>
    /// <param name="howDeeplyNested">the nesting levels of each binary tree</param>
    /// <returns><paramref name="howManyRootNodes"/> new root nodes, each is a root of a <paramref name="howDeeplyNested"/> binary tree</returns>
    public static ICollection<ILayoutNode> CreateNodes(int howManyRootNodes = 100, int howDeeplyNested = 3)
    {
      Vector3 initialSize = Vector3.one;
      int id = 0;

      LayoutVertex root = new(initialSize, id);
      id++;

      ICollection<ILayoutNode> gameObjects = new List<ILayoutNode>
            {
                root
            };

      for (int i = 1; i <= howManyRootNodes; i++)
      {
        initialSize *= 1.01f;
        AddChild(gameObjects, root, initialSize, ref id);
        AddChildren(gameObjects, root, initialSize * 0.49f, ref id, howDeeplyNested);
      }
      return gameObjects;
    }

    private static LayoutVertex AddChild(ICollection<ILayoutNode> gameObjects, LayoutVertex parent, Vector3 scale, ref int id)
    {
      LayoutVertex child = new(scale, id);
      gameObjects.Add(child);
      parent.AddChild(child);
      id++;
      return child;
    }

    private static void AddChildren(ICollection<ILayoutNode> gameObjects, LayoutVertex parent, Vector3 scale, ref int id, int howDeeplyNested)
    {
      if (howDeeplyNested >= 1)
      {
        for (int i = 1; i <= 2; i++)
        {
          LayoutVertex child = AddChild(gameObjects, parent, scale, ref id);
          AddChildren(gameObjects, child, scale * 0.49f, ref id, howDeeplyNested - 1);
        }
      }
    }
  }
}