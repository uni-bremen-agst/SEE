using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// A reference to a block.
    /// </summary>
    [System.Serializable]
    public class BlockRef : MonoBehaviour
    {
        [SerializeField]
        public GameObject Block;
    }
}
