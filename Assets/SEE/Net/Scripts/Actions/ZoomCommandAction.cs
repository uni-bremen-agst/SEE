using UnityEngine;

namespace SEE.Net
{
    public class ZoomCommandAction : AbstractAction
    {
        public Vector2 zoomCenter;
        public int zoomSteps;
        public float duration;

        public ZoomCommandAction(Vector2 zoomCenter, int zoomSteps, float duration) : base(false)
        {
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
            GameObject gameObject = GameObject.FindGameObjectWithTag("MainCamera");
            bool result = false;

            if (gameObject)
            {
                Controls.NavigationAction navigationAction = gameObject.GetComponent<Controls.NavigationAction>();
                if (navigationAction)
                {
                    navigationAction.PushZoomCommand(zoomCenter, zoomSteps, duration);
                    result = true;
                }
                else
                {
                    Debug.LogError("ZoomAction could not be executed! NavigationAction was null!");
                }
            }
            else
            {
                Debug.LogError("ZoomAction could not be executed! GameObject with tag 'MainCamera' was null!");
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
