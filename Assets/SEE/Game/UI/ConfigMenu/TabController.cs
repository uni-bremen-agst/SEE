using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.UI.ConfigMenu
{
    public class TabController : MonoBehaviour
    {
        public List<GameObject> pages = new List<GameObject>();
        public int startIndex;

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