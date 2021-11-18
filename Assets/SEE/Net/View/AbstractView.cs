using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// An abstract view synchronizes elements of a game object.
    /// </summary>
    public abstract class AbstractView : MonoBehaviour
    {
        /// <summary>
        /// The view container, that contains this view.
        /// </summary>
        public ViewContainer viewContainer;

        /// <summary>
        /// Initializes this view with given view container.
        /// </summary>
        /// <param name="viewContainer">The view container, that contains this view.
        /// </param>
        public void Initialize(ViewContainer viewContainer)
        {
            if (viewContainer == null)
            {
                Debug.LogWarning("'viewContainer' is null! Destroying this view!\n");
                Destroy(this);
                return;
            }
            this.viewContainer = viewContainer;
            InitializeImpl();
        }

        private void Update()
        {
            UpdateImpl();
        }

        /// <summary>
        /// Initializes the concrete view.
        /// </summary>
        protected abstract void InitializeImpl();

        /// <summary>
        /// Updates the concrete view.
        /// </summary>
        protected abstract void UpdateImpl();
    }

}
