using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls
{
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

        private void Update()
        {
            if (CurrentMapping != null)
            {
                CurrentMapping.CheckInput();
            }
            else
            {
                Debug.LogError("There is no input-action mapping selected in the Interaction Behavior. You must select one in the inspector.\n");
            }
        }

        /// <summary>
        /// Activates the mapping represented by its name.
        /// </summary>
        /// <param name="name"></param>
        public void ActivateSet(string name)
        {
            CurrentMapping = GetSet(name);
        }

        /// <summary>
        /// Gets a mapping by its name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ActionMapping GetSet(string name)
        {
            for(int i = 0; i < mappings.Count; i++)
            {
                if(mappings[i].GetName() == name)
                {
                    return mappings[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Adds a given mapping to the list if its name doesnt already exist.
        /// </summary>
        /// <param name="mapping"></param>
        public void SetSet(ActionMapping mapping)
        {
            if(GetSet(mapping.GetName()) == null)
            {
                mappings.Add(mapping);
            }
        }

        /// <summary>
        /// Adds a mapping to the list.
        /// </summary>
        /// <param name="mapping"></param>
        public void AddMapping(ActionMapping mapping)
        {
            mappings.Add(mapping);
        }

        /// <summary>
        /// Removes a mapping by the given position from the list.
        /// </summary>
        /// <param name="digit"></param>
        public void RemoveMapping(int digit)
        {
            mappings.RemoveAt(digit);
        }

        public void SetActive(int digit)
        {
            CurrentMapping = mappings[digit];
        }
    }
}
