using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.UI.Tab
{
    public class TabController : MonoBehaviour
    {
        public List<GameObject> pages = new List<GameObject>();
        public int startIndex = 0;

        void Start()
        {
            var tabOultet = GameObject.FindGameObjectWithTag("TabOutlet");
            foreach (Transform child in tabOultet.transform)
            {
                child.gameObject.SetActive(false);
                pages.Add(child.gameObject);
            }

            pages.ElementAtOrDefault(startIndex)?.SetActive(true);
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