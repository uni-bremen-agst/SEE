using UnityEngine;
using UnityEngine.UI;
using SEE.DataModel;
using SEE.Layout;

namespace SEE
{

    public class ListItem : MonoBehaviour
    {
        public SearchMenu searchMenu { get; set; }
        private Text linkageName;
        private GameObject showSourcePrefab;

        private GameObject go;
        public GameObject NodeGameObject
        {
            get
            {
                return this.go;
            }

            set
            {
                this.go = value;
                Node node = value.GetComponent<NodeRef>().node;
                linkageName.text = node.GetString(Node.LinknameAttribute);
                name = "ListItem " + linkageName.text;
            }
        }

        void Awake()
        {
            linkageName = GetComponentInChildren<Text>();
            showSourcePrefab = Resources.Load("Prefabs/ShowSource") as GameObject;
        }

        public bool Contains(string s)
        {
            return linkageName.text.Contains(s);
        }

        public void OnShowSource()
        {
            string source = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
            for (int i = 0; i < 10; i++)
            {
                source += source;
            }
            GameObject showSource = GameObject.Instantiate(showSourcePrefab, Vector3.zero, Quaternion.identity);
            showSource.GetComponent<AdditionalInformationShowSource>().Source = source;
            searchMenu.ShowAdditionalInformation(showSource);
        }

        public void OnTeleport()
        {
            Camera.main.transform.position = go.transform.position;
        }

    }

}
