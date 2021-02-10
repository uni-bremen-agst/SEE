using System.Collections.Generic;
using UnityEngine;

namespace SEE.UI.Tab
{
    public class TabGroup : MonoBehaviour
    {
        public List<TabButton> tabButtons = new List<TabButton>();
        public TabController tabController;
        private TabButton _activeButton;

        // Start is called before the first frame update
        void Start()
        {
            tabController = GetComponentInParent<TabController>();
            if (!tabController)
            {
                Debug.LogError("TabGroup needs to be inside a TabController.");
            }
        }

        public void Subscribe(TabButton button)
        {
            tabButtons.Add(button);
        }

        public void OnTabSelected(TabButton button)
        {
            _activeButton = button;
            button.SetActive();
            ResetButtons();
            tabController.OnIndexUpdate(button.transform.GetSiblingIndex());
        }

        void ResetButtons()
        {
            foreach (var tabButton in tabButtons)
            {
                if (tabButton == _activeButton)
                {
                    continue;
                }

                tabButton.ResetStyles();
            }
        }
    }
}