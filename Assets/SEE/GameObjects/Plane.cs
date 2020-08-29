using SEE.DataModel;
using UnityEngine;

namespace SEE
{
    /// <summary>
    /// Represents a plane on which the city objects can be placed defined as
    /// follows:
    ///    (MinX, MinZ) denotes the left front corner
    ///    (MaxX, MaxZ) denotes the right back corner
    ///    YPosition denotes the y co-ordinate of the plane
    /// </summary>
    public class Plane : MonoBehaviour
    {
        /// <summary>
        /// The left front corner of the plane.
        /// Note: the y co-ordinate of the resulting Vector2 denotes a value in the z axis.
        /// </summary>
        public Vector2 LeftFrontCorner
        {
            get
            {
                Vector3 position = Instance.gameObject.transform.position;
                Vector3 scale = Instance.gameObject.transform.lossyScale;
                float MinX = position.x - scale.x / 2.0f;
                float MinZ = position.z - scale.z / 2.0f;
                return new Vector2(MinX, MinZ);
            }
        }

        /// <summary>
        /// The right back corner of the plane.
        /// Note: the y co-ordinate of the resulting Vector2 denotes a value in the z axis.
        /// </summary>
        public Vector2 RightBackCorner
        {
            get
            {
                Vector3 position = Instance.gameObject.transform.position;
                Vector3 scale = Instance.gameObject.transform.lossyScale;
                float MaxX = position.x + scale.x / 2.0f;
                float MaxZ = position.z + scale.z / 2.0f;
                return new Vector2(MaxX, MaxZ);
            }
        }

        /// <summary>
        /// The center point of the plane in the x/z plane.
        /// </summary>
        public Vector2 CenterXZ
        {
            get
            {
                Vector3 position = Instance.gameObject.transform.position;
                return new Vector2(position.x, position.z);
            }
        }

        /// <summary>
        /// The min length of the plane in either x or z direction.
        /// </summary>
        public float MinLengthXZ
        {
            get
            {
                Vector3 scale = Instance.gameObject.transform.lossyScale;
                return scale.x < scale.z ? scale.x : scale.z;
            }
        }

        /// <summary>
        /// The unique instance of a plane.
        /// FIXME: We need to support an arbitrary number of planes.
        /// </summary>
        private static Plane instance = null;

        private const string SearchTag = Tags.CodeCity;

        /// <summary>
        /// The instance of the plane.
        /// </summary>
        public static Plane Instance 
        {
            get
            {
                if (instance == null)
                {
                    // FIXME: We are iterating over all objects. There should be a better way
                    // to obtain the Plane object. Moreover, we should allow multiple such
                    // objects anyhow.
                    foreach (GameObject plane in GameObject.FindGameObjectsWithTag(SearchTag))
                    {
                        if (instance != null)
                        {
                            Debug.LogErrorFormat("There is yet another game object tagged by named {0}.\n", SearchTag, plane.name);                            
                        }
                        else
                        {
                            instance = plane.GetComponent<Plane>();
                            // Note: this will also handle the case in which the plane
                            // has no component Plane. Then we will simply continue
                            // and try the next game object tagged accordingly.

                            if (instance != null)
                            {
                                Debug.LogFormat("plane {0} size={1}\n", plane.name, plane.transform.lossyScale);
                            }
                        }
                    }
                    if (instance == null)
                    {
                        throw new System.Exception("There is no game object tagged by " + SearchTag
                            + " with a component Plane");
                    }
                }
                return instance;
            }
            private set
            {
                instance = value;
            }
        }

        /// <summary>
        /// The center of the plane's roof (plus some very small y delta).
        /// </summary>
        public Vector3 CenterTop
        {
            get
            {
                Vector3 scale = Instance.transform.lossyScale;
                return Instance.transform.position + new Vector3(0.0f, scale.y / 2.0f + float.Epsilon, 0.0f);
            }
        }
    }
}
