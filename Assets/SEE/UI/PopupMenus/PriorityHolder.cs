using UnityEngine;

namespace SEE.UI.PopupMenus
{
    /// <summary>
    /// Component which holds the priority of a <see cref="PopupMenuEntry"/>.
    /// </summary>
    public class PriorityHolder : MonoBehaviour
    {
        /// <summary>
        /// The priority of a <see cref="PopupMenuEntry"/>.
        /// </summary>
        public int Priority { get; set; } = 0;
    }
}
