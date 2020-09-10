using SEE.Controls;
using UnityEngine;

namespace SEE.Net
{
    public class ZoomCommandAction : AbstractAction
    {
        public int navigationActionID;
        public Vector2 zoomCenter;
        public int zoomSteps;
        public float duration;

        public ZoomCommandAction(NavigationAction navigationAction, Vector2 zoomCenter, int zoomSteps, float duration) : base(false)
        {
            navigationActionID = navigationAction.ID;
            this.zoomCenter = zoomCenter;
            this.zoomSteps = zoomSteps;
            this.duration = duration;
        }

        protected override bool ExecuteOnServer()
        {
            return true;
        }

        protected override bool ExecuteOnClient()
        {
            bool result = false;

            NavigationAction navigationAction = NavigationAction.Get(navigationActionID);
            if (navigationAction)
            {
                navigationAction.PushZoomCommand(zoomCenter, zoomSteps, duration);
                result = true;
            }
            else
            {
                Debug.LogError("ZoomAction could not be executed! NavigationAction was null!");
            }

            return result;
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
