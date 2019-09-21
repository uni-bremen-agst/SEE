using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Abstract super class of all block modifiers. A block modifier allows us to 
    /// retrieve information from blocks (cubes or CScape buildings) visualizing nodes 
    /// in the scene and also to manipulate them. 
    /// They are components in game objects. Those game objects are also
    /// containing the block representation (cube or CScape building) as their immediate
    /// child. 
    /// This class encapsulates how to deal with blocks (cubes and CScape buildings),
    /// which are different with respect to size and scaling.
    /// </summary>
    public abstract class BlockModifier : MonoBehaviour
    {
        /// <summary>
        /// Yields the size of the block representing a node. The size is always
        /// double the extent of the block.
        /// </summary>
        /// <returns>size of the block representing a node</returns>
        public Vector3 GetSize()
        {
            return GetExtent() * 2.0f;
        }

        /// <summary>
        /// Yields the extent of the block representing a node. The extent is always
        /// half the size of the block.
        /// </summary>
        /// <returns>size of the block representing a node</returns>
        public abstract Vector3 GetExtent();

        /// <summary>
        /// Scales the block representing a node to the given scale.
        /// </summary>
        /// <param name="scale">scaling factor</param>
        public virtual void Scale(Vector3 scale)
        {
            // This is the default implementation. CScape buildings are not scaled this
            // way. They are scaled in the number of floors instead.
            gameObject.transform.localScale = scale;
        }

        /// <summary>
        /// Returns the first immediate child of parent with given tag or null
        /// if none exists.
        /// </summary>
        /// <param name="parent">parent whose children are to be searched</param>
        /// <param name="tag">search tag</param>
        /// <returns>first immediate child of parent with given tag or null</returns>
        protected static GameObject GetChild(GameObject parent, string tag)
        {
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                Transform child = parent.transform.GetChild(i);
                if (child.tag == tag)
                {
                    return child.gameObject;
                }
            }
            return null;
        }
    }
}
