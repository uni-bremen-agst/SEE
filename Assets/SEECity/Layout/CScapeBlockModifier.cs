using CScape;
using SEE.DataModel;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// A block modifier for CScape buildings. It is intended to be attached
    /// to the container game object containing a CScape building. It encapsulates
    /// the specifics of CScape buildings with respect to size and scaling.
    /// </summary>
    public class CScapeBlockModifier : BlockModifier
    {
        public override Vector3 GetExtent()
        {
            // gameObject is the logical node with tag Tags.Node; it may have different
            // visual representations. Currently, we have cubes, which are tagged by 
            // Tags.Block, and CScape buildings, which are tagged by Tags.Building.
            // Both of them are immediate children of gameObject.
            GameObject child = GetChild(gameObject, Tags.Building);
            if (child != null)
            {
                // It is a CScape building which has no renderer. We use its collider instead.
                Collider collider = child.GetComponent<Collider>();
                if (collider != null)
                {
                    return collider.bounds.extents;
                }
                else
                {
                    Debug.LogErrorFormat("CScape building {0} without collider.\n", gameObject.name);
                    return Vector3.one;
                }
            }
            else
            {
                return Vector3.zero;
            }
        }

        public override void Scale(Vector3 scale)
        {
            // Scale by the number of floors of a building.
            BuildingModifier bm = gameObject.GetComponent<BuildingModifier>();
            if (bm == null)
            {
                Debug.LogErrorFormat("CScape building {0} has no building modifier.\n", gameObject.name);
            }
            else
            {
                bm.buildingWidth = (int)scale.x;
                bm.floorNumber = (int)scale.y;
                bm.buildingDepth = (int)scale.z;
            }
        }
    }
}
