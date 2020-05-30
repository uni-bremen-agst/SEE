using System.Collections.Generic;
using UnityEngine;

namespace SEE.Net
{

    internal static class ActionHistory
    {
        internal static List<AbstractAction> actions = new List<AbstractAction>();
        internal static List<GameObject> actionHistoryElements = new List<GameObject>();

        internal static void OnExecute(AbstractAction action)
        {
            GameObject prefab = Resources.Load<GameObject>("ActionHistoryElement");
            prefab.GetComponentInChildren<UnityEngine.UI.Text>().text = action.GetType().Name;
            Transform parent = UnityEngine.Object.FindObjectOfType<UnityEngine.UI.VerticalLayoutGroup>().transform;
            GameObject actionHistoryElement = UnityEngine.Object.Instantiate(prefab, parent);
            actionHistoryElement.GetComponent<ActionHistoryElement>().index = actions.Count;

            actions.Add(action);
            actionHistoryElements.Add(actionHistoryElement);
        }

        internal static void Undo(int index)
        {
            actions[index].Undo();
        }

        internal static void Redo(int index)
        {
            actions[index].Redo();
        }
    }

}
