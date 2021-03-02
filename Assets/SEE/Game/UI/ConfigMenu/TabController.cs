using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.UI.ConfigMenu
{
    public class TabController : MonoBehaviour
    {
        public List<GameObject> pages = new List<GameObject>();
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
