using SEE.UI.Notification;
using System;
using System.Collections.Generic;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class stores rules for the concurrency handling and
    /// provides helpers that are not to be serialized.
    /// </summary>
    public static class ConcurrentNetRules
    {
        /// <summary>
        /// Stores the set of safe concurrent or versioned actions.
        /// </summary>
        public static readonly HashSet<Type> IsSafeAction = new()
        {
            typeof(MoveNetAction),
            typeof(EditNodeNetAction),
            typeof(ResizeNodeNetAction),
            typeof(RotateNodeNetAction),
            typeof(ScaleNodeNetAction)
        };

        /// <summary>
        /// Used to determine whether it is a delete or revive action.
        /// </summary>
        public static readonly HashSet<Type> IsDeleteOrRevive = new()
        {
            typeof(DeleteNetAction),
            typeof(ReviveNetAction),
            typeof(RestoreNetAction),
            typeof(RegenerateNetAction)
        };

        /// <summary>
        /// This Dictionary provides the corresponding action name.
        /// </summary>
        public static readonly Dictionary<Type, string> RollbackMessage = new()
        {
            { typeof(AddEdgeNetAction),     "Add Edge Action" },
            { typeof(AddNodeNetAction),     "Add Node Action" },
            { typeof(DeleteNetAction),      "Delete Action" },
            { typeof(EditNodeNetAction),    "Edit Node Action" },
            { typeof(MoveNetAction),        "Move Action" },
            { typeof(ResizeNodeNetAction),  "Resize Node Action" },
            { typeof(RestoreNetAction),     "Restore Action" },
            { typeof(ReviveNetAction),      "Revive Action" },
            { typeof(RotateNodeNetAction),  "Rotate Node Action" },
            { typeof(ScaleNodeNetAction),   "Scale Node Action" },
            { typeof(SetParentNetAction),   "Set Parent Action" },
            { typeof(SetSelectNetAction),   "Set Select Action" },
        };

        /// <summary>
        /// Creates a rollback notification for the client whose action was reversed.
        /// </summary>
        /// <param name="action">The NetAction that got undone.</param>
        public static void RollbackNotification(ConcurrentNetAction action)
        {
            if (RollbackMessage.TryGetValue(action.GetType(), out string actionName))
            {
                ShowNotification.Warn("Network Conflict", "Your last '" + actionName + "' got undone because of a conflict.");
            }
            ShowNotification.Warn("Network Conflict", "Your last NetAction got undone because of a conflict.");
        }

    }
}
