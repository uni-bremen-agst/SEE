using UnityEngine;

namespace SEE.Net
{

    public abstract class View : MonoBehaviour
    {
        public ViewContainer viewContainer;

        public void Initialize(ViewContainer viewContainer)
        {
            if (viewContainer == null)
            {
                Debug.LogWarning("'viewContainer' is null! Destroying this view!");
                Destroy(this);
            }
            this.viewContainer = viewContainer;
            InitializeImpl();
        }

        void Update()
        {
            UpdateImpl();
        }

        protected abstract void InitializeImpl();
        protected abstract void UpdateImpl();
    }

}
