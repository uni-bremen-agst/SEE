using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand.Demo
{
    public class ToggleHandProjection : MonoBehaviour
    {
        public void DisableGripProjection()
        {
#if (UNITY_2020_3_OR_NEWER)
            var projections = FindObjectsOfType<HandProjector>(true);
#else
            var projections = FindObjectsOfType<HandProjector>();
#endif

            foreach (var projection in projections)
            {
                projection.gameObject.SetActive(false);
                if (projection.useGrabTransition)
                    projection.enabled = false;
            }
        }

        public void EnableGripProjection()
        {
#if (UNITY_2020_3_OR_NEWER)
            var projections = FindObjectsOfType<HandProjector>(true);
#else
            var projections = FindObjectsOfType<HandProjector>();
#endif
            foreach (var projection in projections)
            {
                projection.gameObject.SetActive(true);
                if (projection.useGrabTransition)
                    projection.enabled = true;
            }
        }

        public void DisableHighlightProjection()
        {
#if (UNITY_2020_3_OR_NEWER)
            var projections = FindObjectsOfType<HandProjector>(true);
#else
            var projections = FindObjectsOfType<HandProjector>();
#endif
            foreach (var projection in projections)
            {
                projection.gameObject.SetActive(false);
                if (!projection.useGrabTransition)
                    projection.enabled = false;
            }
        }

        public void EnableHighlightProjection()
        {
#if (UNITY_2020_3_OR_NEWER)
            var projections = FindObjectsOfType<HandProjector>(true);
#else
            var projections = FindObjectsOfType<HandProjector>();
#endif
            foreach (var projection in projections)
            {
                projection.gameObject.SetActive(true);
                if (!projection.useGrabTransition)
                    projection.enabled = true;
            }
        }
    }
}