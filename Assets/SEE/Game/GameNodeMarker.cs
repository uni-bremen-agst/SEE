using System;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Toggle a marking for a node.
    /// The mark is a white sphere, hovering over the node.
    /// </summary>
    public static class GameNodeMarker
    {

        /// <summary>
        /// Toggles the marking of a node <paramref name="parent"/> at the
        /// given <paramref name="position"/> with the given <paramref name="worldSpaceScale"/>.
        /// The marking is a new GameObject, added as a child of the Node.
        /// The GameObject is represented by a white hovering sphere above the node which is marked.
        ///
        /// Precondition: <paramref name="parent"/> must have a valid node reference.
        /// </summary>
        /// <param name="parent">node which marking will be toggled.</param>
        /// <param name="position">the position in world space for the center point of the mark.</param>
        /// <param name="worldSpaceScale">the scale in world space of the mark.</param>
        /// <exception cref="Exception">thrown if <paramref name="parent"/> is not contained in a code city.</exception>
        public static void Mark(GameObject parent, Vector3 position, Vector3 worldSpaceScale)
        {
            SEECity city = parent.ContainingCity() as SEECity;
            if (city != null)
            {
                /// Gets the actual GameObject which represents the node.
                Node parentNode = parent.GetNode();
                /// Mark if the node is not marked.
                if (!parentNode.IsMarked) {
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.transform.localScale = worldSpaceScale;
                    /// The sphere is located at the golden ratio with the smaller part being the distance to the sphere, and the bigger part being the sphere.
                    /// Mathematical explanation of the position:
                    ///     parent.transform.position.y = middle of node.
                    ///     parent.transform.lossyScale.y/2 = half of the height of the node.
                    ///     worldSpaceScale.y*0.5 = half of the height of the sphere.
                    ///     because the position of the sphere describes the middle of itself, we add all 3 values together, to put the mark ontop of the node.
                    ///     We finally add worldSpaceScale.y*0.38196601125F to get the smaller part of the golden ratio in relation to the size of the sphere as distance to the node.
                    ///     Because "worldSpaceScale.y*0.5 + worldSpaceScale.y*0.38196601125F" is the same result as "worldSpaceScale.y*0.88196601125F", we simplify it.
                    sphere.transform.position = new Vector3(position.x, parent.transform.position.y + parent.transform.lossyScale.y/2 + worldSpaceScale.y* 0.88196601125F, position.z);
                    sphere.transform.SetParent(parent.transform);
                    Portal.SetPortal(city.gameObject, gameObject: sphere);
                    parentNode.IsMarked = true;
                    parentNode.Marking = sphere;
                    return;
                }
                /// unmark if the node is marked.
                else
                {
                    parentNode.IsMarked = false;
                    GameObject marking = parentNode.Marking;
                    GameObject.Destroy(marking);
                    return;
                }
                
            }
            else
            {
                throw new Exception($"The node {parent.name} is not contained in a code city.");
            }
        }
    }
}
