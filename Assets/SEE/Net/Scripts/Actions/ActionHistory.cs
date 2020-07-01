using System.Collections.Generic;
using UnityEngine;

namespace SEE.Net
{

    /// <summary>
    /// The complete history of all buffered actions.
    /// </summary>
    internal static class ActionHistory
    {
        /// <summary>
        /// All buffered actions that have been executed to far in correct execution
        /// order.
        /// </summary>
        internal static List<AbstractAction> actions = new List<AbstractAction>();

        /// <summary>
        /// All history elements, used for UI. Each element at given index corresponds
        /// directly with an action in <see cref="actions"/> of same index.
        /// </summary>
        internal static List<GameObject> actionHistoryElements = new List<GameObject>();

        /// <summary>
        /// Saves the executed action and creates a UI-element for it.
        /// </summary>
        /// <param name="action">The executed action.</param>
        internal static void DebugOnExecute(AbstractAction action)
        {
            GameObject prefab = Resources.Load<GameObject>("DebugActionHistoryElement");
            prefab.GetComponentInChildren<UnityEngine.UI.Text>().text = action.GetType().Name;

            GameObject canvas = UnityEngine.Object.FindObjectOfType<Canvas>().gameObject;
            Transform parent = null;
            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                GameObject child = canvas.transform.GetChild(i).gameObject;
                if (child.name.Equals("Scroll View"))
                {
                    for (int j = 0; j < child.transform.childCount; j++)
                    {
                        UnityEngine.UI.VerticalLayoutGroup verticalLayoutGroup = child.GetComponentInChildren<UnityEngine.UI.VerticalLayoutGroup>();
                        if (verticalLayoutGroup != null)
                        {
                            parent = verticalLayoutGroup.transform;
                            break;
                        }
                    }

                    if (parent != null)
                    {
                        break;
                    }
                }
            }
            GameObject actionHistoryElement = UnityEngine.Object.Instantiate(prefab, parent);
            actionHistoryElement.GetComponent<DebugActionHistoryElement>().index = actions.Count;

            actions.Add(action);
            actionHistoryElements.Add(actionHistoryElement);
        }

        /// <summary>
        /// Undos the action at given index.
        /// </summary>
        /// <param name="index">The index of the action to undo.</param>
        internal static void Undo(int index)
        {
            actions[index].Undo();
        }

        /// <summary>
        /// Redos the action at given index.
        /// </summary>
        /// <param name="index">The index of the action to redo.</param>
        internal static void Redo(int index)
        {
            actions[index].Redo();
        }
    }

}
