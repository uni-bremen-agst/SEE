using SEE.Controls;
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

        public SyncCitiesAction(NavigationAction navigationAction) : base(false)
        {
            Assert.IsNotNull(navigationAction);

            navigationActionID = navigationAction.ID;
            position = navigationAction.CityTransform.position;
            rotation = navigationAction.CityTransform.rotation;
            localScale = navigationAction.CityTransform.localScale;
            currentTargetZoomSteps = navigationAction.zoomState.currentTargetZoomSteps;
        }

        protected override bool ExecuteOnClient()
        {
            bool result = false;

            if (IsRequester())
            {
                result = true;
            }
            else
            {
                NavigationAction navigationAction = NavigationAction.Get(navigationActionID);
                if (navigationAction)
                {
                    result = true;

                    navigationAction.CityTransform.position = position;
                    navigationAction.CityTransform.rotation = rotation;
                    navigationAction.CityTransform.localScale = localScale;
                    navigationAction.zoomState.currentTargetZoomSteps = currentTargetZoomSteps;
                }
                else
                {
                    Debug.LogWarning("NavigationAction could not be found!");
                }
            }

            return result;
        }

        protected override bool ExecuteOnServer()
        {
            return true;
        }

        protected override bool UndoOnServer()
        {
            throw new System.NotImplementedException();
        }

        protected override bool UndoOnClient()
        {
            throw new System.NotImplementedException();
        }

        protected override bool RedoOnServer()
        {
            throw new System.NotImplementedException();
        }

        protected override bool RedoOnClient()
        {
            throw new System.NotImplementedException();
        }
    }
}
