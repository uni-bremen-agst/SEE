// Copyright 2021 Ruben Smidt
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

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
        public List<GameObject> Pages = new List<GameObject>();

        /// <summary>
        /// The index of the initially active page.
        /// </summary>
        public int StartIndex;

        private int previousPageCount;
        private Transform tabOutlet;
        private void Start()
        {
            tabOutlet = gameObject.transform.Find("TabOutlet");
            RefreshPages();
        }

        private void RefreshPages()
        {
            Pages.Clear();
            foreach (Transform child in tabOutlet)
            {
                child.gameObject.SetActive(false);
                Pages.Add(child.gameObject);
            }
            previousPageCount = tabOutlet.childCount;
            Pages.ElementAtOrDefault(StartIndex)?.SetActive(true);
        }

        private void Update()
        {
            if (tabOutlet.childCount != previousPageCount)
            {
                RefreshPages();
            }
        }

        /// <summary>
        /// Sets the page that corresponds to the requested index active and all other
        /// pages inactive.
        /// </summary>
        /// <param name="requestedIndex">The requested index of the page that should be set active.</param>
        public void OnIndexUpdate(int requestedIndex)
        {
            for (int i = 0; i < Pages.Count; i++)
            {
                Pages[i].SetActive(i == requestedIndex);
            }
        }
    }
}
