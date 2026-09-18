using Michsky.UI.ModernUIPack;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Controls whether child nodes of a Mind Map node are included while moving
    /// the selected node.
    /// </summary>
    internal sealed class MindMapMoveChildrenController
    {
        /// <summary>
        /// The instantiated move menu containing the child inclusion controls.
        /// </summary>
        private readonly GameObject menu;

        /// <summary>
        /// The object that is currently moved.
        /// </summary>
        private readonly GameObject selectedObject;

        /// <summary>
        /// Whether child nodes should be included in the current movement.
        /// </summary>
        internal bool IncludeChildren { get; private set; }

        /// <summary>
        /// Creates a controller for the child inclusion option of the move menu.
        /// </summary>
        /// <param name="menu">The instantiated move menu.</param>
        /// <param name="selectedObject">The object that is currently moved.</param>
        internal MindMapMoveChildrenController(GameObject menu, GameObject selectedObject)
        {
            this.menu = menu;
            this.selectedObject = selectedObject;
        }

        /// <summary>
        /// Initializes the child inclusion controls for the selected object.
        /// The controls are hidden for objects that are not Mind Map nodes.
        /// </summary>
        internal void SetUp()
        {
            GameObject childrenArea = GameFinder.FindAttachedOrLocalDescendant(
                menu, "Content").transform.Find("Children").gameObject;

            if (!selectedObject.CompareTag(Tags.MindMapNode))
            {
                childrenArea.SetActive(false);
                IncludeChildren = false;
                return;
            }

            childrenArea.SetActive(true);

            GameObject surface = GameFinder.GetDrawableSurface(selectedObject);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

            /// The old position of the object is needed if child inclusion is
            /// activated after the movement has already started.
            Vector3 oldPosition = selectedObject.transform.localPosition;

            MMNodeValueHolder valueHolder = selectedObject.GetComponent<MMNodeValueHolder>();

            /// Save the original positions of the children. They are restored if
            /// child inclusion is disabled again during the movement.
            Dictionary<GameObject, Vector3> oldPositions = new();
            foreach (KeyValuePair<GameObject, GameObject> pair in valueHolder.GetAllChildren())
            {
                oldPositions[pair.Key] = pair.Key.transform.localPosition;
            }

            SwitchManager childrenSwitch = GameFinder.FindAttachedOrLocalDescendant(
                menu, "ChildrenSwitch").GetComponent<SwitchManager>();

            bool inclusionWasEnabled = false;

            childrenSwitch.OnEvents.RemoveAllListeners();
            childrenSwitch.OnEvents.AddListener(() =>
            {
                IncludeChildren = true;

                /// Move the children to the current position of the parent node.
                /// The parent node is first returned to its original position before
                /// being moved with the children to the new position. This preserves
                /// the node arrangement.
                if (valueHolder.GetChildren().Count > 0)
                {
                    Vector3 newPosition = selectedObject.transform.localPosition;

                    GameMoveRotator.SetPosition(selectedObject, oldPosition, false);
                    new MoveNetAction(surface.name, surfaceParentName, selectedObject.name, oldPosition, false).Execute();

                    GameMoveRotator.SetPosition(selectedObject, newPosition, true);
                    new MoveNetAction(surface.name, surfaceParentName, selectedObject.name, newPosition, true).Execute();
                }

                inclusionWasEnabled = true;
            });

            childrenSwitch.OffEvents.RemoveAllListeners();
            childrenSwitch.OffEvents.AddListener(() =>
            {
                IncludeChildren = false;

                if (inclusionWasEnabled)
                {
                    /// Restore the original positions of the child nodes.
                    foreach (KeyValuePair<GameObject, Vector3> pair in oldPositions)
                    {
                        GameMoveRotator.SetPosition(pair.Key, pair.Value, false);
                        new MoveNetAction(surface.name, surfaceParentName, pair.Key.name, pair.Value, false).Execute();
                    }
                }
            });
        }
    }
}
