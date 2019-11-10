using UnityEngine;
using SEE.DataModel;
using SEE.Layout;

namespace SEE
{

    public class SearchManager : MonoBehaviour
    {
        private ListItem[] listItems = null;

        public void Initialize()
        {
            GameObject listItemPrefab = Resources.Load("Prefabs/ListItem") as GameObject;

            NodeRef[] nodeRefs = FindObjectsOfType<NodeRef>();
            listItems = new ListItem[nodeRefs.Length];
            
            for (int i = 0; i < nodeRefs.Length; i++)
            {
                GameObject go = GameObject.Instantiate(listItemPrefab, transform);
                ListItem li = go.GetComponent<ListItem>();
                listItems[i] = li;
                GameObject nodeRefGO = nodeRefs[i].gameObject;
                li.NodeGameObject = nodeRefGO;
            }
        }

        public void Filter(string filterString)
        {
            for (int i = 0; i < listItems.Length; i++)
            {
                if (listItems[i].Contains(filterString))
                {
                    listItems[i].gameObject.SetActive(true);
                }
                else
                {
                    listItems[i].gameObject.SetActive(false);
                }
            }
        }
    }

}// namespace SEE
