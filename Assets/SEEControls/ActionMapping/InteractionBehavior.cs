using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Manages all available input-to-action mappings as well as the currently active one.
    /// Calls CheckInput() on the current mapping periodically at each frame cycle.
    /// </summary>
    public class InteractionBehavior : MonoBehaviour
    {
        /// <summary>
        /// The currently active mapping.
        /// </summary>
        [SerializeField]
        public ActionMapping CurrentMapping;

        /// <summary>
        /// List of all available mappings.
        /// </summary>
        [SerializeField]
        private List<ActionMapping> mappings = new List<ActionMapping>();

        private void Start()
        {
            if (CurrentMapping == null)
            {
                Debug.LogError("No action mapping has been activated.\n");
            }
            else
            {
                Debug.LogFormat("InteractionBehavior.Start with mapping {0}\n", CurrentMapping.Name);
                ShowDevices();

                HashSet<string> names = new HashSet<string>();

                for (int i = 0; i < mappings.Count; i++)
                {
                    ActionMapping mapping = mappings[i];
                    if (mapping == null)
                    {
                        Debug.LogErrorFormat("Mapping at index {0} is null.\n", i);
                    }
                    else if (string.IsNullOrEmpty(mapping.Name))
                    {
                        Debug.LogErrorFormat("Mapping at index {0} has no name.\n", i);
                    }
                    else if (names.Contains(mapping.Name))
                    {
                        Debug.LogErrorFormat("Mapping named {0} at index {1} has another mapping with the same name.\n",
                                             mapping.Name, i);
                    }
                }
            }
        }

        private void ShowDevices()
        {
            var inputDevices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevices(inputDevices);

            Debug.LogFormat("Number of devices found: {0}\n", inputDevices.Count);
            foreach (var device in inputDevices)
            {
                Debug.Log(string.Format("Device found with name '{0}' and role '{1}'.\n", 
                          device.name, device.characteristics.ToString()));
            }
        }

        /// <summary>
        /// Calls CheckInput() on the current mapping periodically at each frame cycle.
        /// </summary>
        private void Update()
        {
            if (CurrentMapping != null)
            {
                CurrentMapping.CheckInput();
            }
        }

        /// <summary>
        /// Activates the mapping with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">name of the mapping to be activated</param>
        public void ActivateSet(string name)
        {
            CurrentMapping = GetSet(name);
        }

        /// <summary>
        /// Gets a mapping with the given <paramref name="name"/>. Returns null if
        /// there is no such <paramref name="name"/>.
        /// </summary>
        /// <param name="name">name of the requested mapping</param>
        /// <returns>mapping with given <paramref name="name"/> or null</returns>
        public ActionMapping GetSet(string name)
        {
            for (int i = 0; i < mappings.Count; i++)
            {
                if (mappings[i].Name == name)
                {
                    return mappings[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Adds the given <paramref name="mapping"/> to the list if its name does not already exist.
        /// If it exists already, an exception is thrown. Likewise, if <paramref name="mapping"/> is
        /// null, an exception is thrown.
        /// </summary>
        /// <param name="mapping">mapping to be added; must not be null</param>
        public void AddMapping(ActionMapping mapping)
        {
            if (mapping == null)
            {
                throw new System.Exception("An action mapping must not be null.");
            }
            else if (GetSet(mapping.Name) == null)
            {
                mappings.Add(mapping);
            }
            else
            {
                throw new System.Exception("An action mapping named " + mapping.Name+ " exists already.");
            }
        }

        /// <summary>
        /// Removes a mapping by the given position from the list of mappings.
        /// </summary>
        /// <param name="index">index of the mapping to be removed</param>
        public void RemoveMapping(int index)
        {
            mappings.RemoveAt(index);
        }

        public void SetActive(int index)
        {
            CurrentMapping = mappings[index];
        }

        public ActionMapping GetActive()
        {
            return CurrentMapping;
        }

        public ActionMapping GetMapping(int index)
        {
            return mappings[index];
        }
    }
}
