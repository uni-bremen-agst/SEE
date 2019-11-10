using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SEE
{

    public class SearchInputField : MonoBehaviour
    {
        public GameObject scrollView;
        public ListItemManager searchManager;
        public InputField searchField;

        private bool isSelected;

        void Update()
        {
            if (EventSystem.current.currentSelectedGameObject != null)
            {
                if (!isSelected)
                {
                    scrollView.SetActive(true);
                }
                isSelected = true;
            }
            else
            {
                if (isSelected)
                {
                    // TODO properly deactivate if click back into game etc
                }
                isSelected = false;
            }
        }

        public void OnValueChanged()
        {
            searchManager.Filter(searchField.text);
        }
    }

}// namespace SEE
