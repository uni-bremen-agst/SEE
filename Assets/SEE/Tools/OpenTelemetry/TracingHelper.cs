using System.Collections.Generic;
using System.Diagnostics;
using SEE.Controls.Actions;
using SEE.Utils.History;
using UnityEngine;

namespace SEE.Tools.OpenTelemetry
{
    /// <summary>
    /// Provides static methods for tracing key user and system actions within SEE.
    /// Each method is tailored to record metadata about specific types of actions,
    /// supporting observability and behavioral analysis via OpenTelemetry.
    /// </summary>
    public class TracingHelper
    {
        private readonly ActivitySource activitySource;

        public TracingHelper(string sourceName = "SEE.Tracing")
        {
            activitySource = new ActivitySource(sourceName);
        }

        /// <summary>
        /// Traces the addition of a reversible action to the global action history.
        /// Ignores MoveAction since it is tracked separately.
        /// </summary>
        public void TrackAddToHistory(IReversibleAction action, string actionID, HashSet<string> changedObjects)
        {
            if (action is MoveAction) return;

            var tags = new Dictionary<string, object>
            {
                { "action.id", actionID },
                { "action.type", action.GetType().FullName }
            };

            int index = 0;
            foreach (string objectId in changedObjects)
            {
                GameObject obj = GameObject.Find(objectId);
                if (obj != null)
                {
                    tags[$"action.affectedObjects[{index}].id"] = obj.name;
                    tags[$"action.affectedObjects[{index}].instanceID"] = obj.GetInstanceID();
                    tags[$"action.affectedObjects[{index}].position"] = obj.transform.position.ToString();
                    index++;
                }
            }

            using var activity = activitySource.StartActivity("AddToGlobalHistory");
            if (activity != null)
            {
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }

        /// <summary>
        /// Traces the removal of an action from the global history.
        /// </summary>
        public void TrackRemoveFromHistory(ActionHistory.GlobalHistoryEntry action)
        {
            var tags = new Dictionary<string, object>
            {
                { "action.id", action.ActionID },
                { "action.type", action.GetType().FullName }
            };

            int index = 0;
            foreach (string objectId in action.ChangedObjects)
            {
                GameObject obj = GameObject.Find(objectId);
                if (obj != null)
                {
                    tags[$"action.affectedObjects[{index}].id"] = obj.name;
                    tags[$"action.affectedObjects[{index}].instanceID"] = obj.GetInstanceID();
                    tags[$"action.affectedObjects[{index}].position"] = obj.transform.position.ToString();
                    index++;
                }
            }

            using var activity = activitySource.StartActivity("RemoveFromGlobalHistory");
            if (activity != null)
            {
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }

        /// <summary>
        /// Traces a MoveAction, capturing details about the moved object and its new parent and position.
        /// </summary>
        public void TrackMoveAction(GameObject grabbedObject, Vector3 finalPosition, GameObject grabbedObjectNewParent)
        {
            if (grabbedObject == null) return;

            var tags = new Dictionary<string, object>
            {
                { "action.type", "MoveAction" },
                { "newParent", grabbedObjectNewParent?.name ?? "None" },
                { "finalPosition", finalPosition.ToString() },
                { "action.objectId", grabbedObject.name },
                { "action.instanceID", grabbedObject.GetInstanceID() }
            };

            using var activity = activitySource.StartActivity("MoveAction");
            if (activity != null)
            {
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }

        /// <summary>
        /// Traces a key press event.
        /// </summary>
        public void TrackKeyPress(string actionName)
        {
            using var activity = activitySource.StartActivity("KeyPress");
            activity?.SetTag("action.name", actionName);
        }

        /// <summary>
        /// Traces a player's movement by recording both the start and end positions.
        /// </summary>
        public void TrackMovement(Vector3 startPosition, Vector3 endPosition)
        {
            var tags = new Dictionary<string, object>
            {
                { "action.type", "PlayerMovement" },
                { "startPosition", startPosition.ToString() },
                { "endPosition", endPosition.ToString() }
            };

            using var activity = activitySource.StartActivity("PlayerMovement");
            if (activity != null)
            {
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }
    }
}
