using SEE.Controls.Actions;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{
    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractAction"/> before modifying this class!
    /// </summary>
    public class SyncCitiesAction : AbstractAction
    {
        public int navigationActionID;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
        public float currentTargetZoomSteps;

        public SyncCitiesAction(NavigationAction navigationAction)
        {
            Assert.IsNotNull(navigationAction);

            navigationActionID = navigationAction.ID;
            position = navigationAction.CityTransform.position;
            rotation = navigationAction.CityTransform.rotation;
            localScale = navigationAction.CityTransform.localScale;
            currentTargetZoomSteps = navigationAction.zoomState.currentTargetZoomSteps;
        }

        protected override void ExecuteOnServer()
        {
        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                NavigationAction navigationAction = NavigationAction.Get(navigationActionID);
                if (navigationAction)
                {
                    if (!navigationAction.CityTransform)
                    {
                        navigationAction.Update();
                    }
                    if (navigationAction.CityTransform)
                    {
                        navigationAction.CityTransform.position = position;
                        navigationAction.CityTransform.rotation = rotation;
                        navigationAction.CityTransform.localScale = localScale;
                        navigationAction.zoomState.currentTargetZoomSteps = currentTargetZoomSteps;
                    }
                    else
                    {
                        Debug.LogWarning("SyncCitiesAction does not have an initialized city attached!");
                    }
                }
                else
                {
                    Debug.LogWarning("SyncCitiesAction could not be found!");
                }
            }
        }
    }
}
