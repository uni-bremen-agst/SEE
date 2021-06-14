using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.UI.ConfigMenu
{
    /// <summary>
    /// Controls all pages that can be accessed by the user.
    /// </summary>
    public class TabController : MonoBehaviour
    {
        /// <summary>
        /// A list of all page GameObject that can be accessed.
        /// </summary>
        public List<GameObject> pages = new List<GameObject>();

        /// <summary>
        /// The index of the initially active page.
        /// </summary>
        public int startIndex;

        private int _previousPageCount;
        private Transform _tabOutlet;

        void Start()
        {
            _tabOutlet = gameObject.transform.Find("TabOutlet");
            RefreshPages();
        }

        private void RefreshPages()
        {
            pages.Clear();
            foreach (Transform child in _tabOutlet)
            {
                child.gameObject.SetActive(false);
                pages.Add(child.gameObject);
            }
            _previousPageCount = _tabOutlet.childCount;
            pages.ElementAtOrDefault(startIndex)?.SetActive(true);
        }

        private void Update()
        {
            if (_tabOutlet.childCount != _previousPageCount)
            {
                RefreshPages();
            }
        }

        /// <summary>
        /// Sets the page that corresponds to the requested index active and set all other
        /// pages inactive.
        /// </summary>
        /// <param name="requestedIndex">The index of the page that should be set active.</param>
        public void OnIndexUpdate(int requestedIndex)
        {
            for (var i = 0; i < pages.Count; i++)
            {
                if (i == requestedIndex)
                {
                    pages[i].SetActive(true);
                }
                else
                {
                    pages[i].SetActive(false);
                }
            }
        }
    }
}
