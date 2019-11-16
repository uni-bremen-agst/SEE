using UnityEngine;

namespace SEE
{

    public class SearchMenu : MonoBehaviour
    {
        public GameObject listItemManager { get; private set; }
        public GameObject additionalInfo { get; private set; }

        public void Initialize()
        {
            listItemManager = GameObject.Find("SearchResultsPanel");
            additionalInfo = GameObject.Find("AdditionalInfoPanel");

            listItemManager.GetComponentInChildren<ListItemManager>().Initialize();
            listItemManager.SetActive(true);
            additionalInfo.SetActive(false);
        }

        public void ShowAdditionalInformation(GameObject gameObject)
        {
            additionalInfo.SetActive(true);
            gameObject.SetActive(true);
            gameObject.transform.SetParent(additionalInfo.transform.GetChild(0));
            gameObject.transform.localScale = Vector3.one;
        }
    }

}
