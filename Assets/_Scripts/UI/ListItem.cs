using UnityEngine;
using UnityEngine.UI;
using SEE.DataModel;
using SEE.Layout;

namespace SEE
{

    public class ListItem : MonoBehaviour
    {
        private Text linkageName;

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
        }

        public bool Contains(string s)
        {
            return linkageName.text.Contains(s);
        }

        public void OnTeleport()
        {
            Camera.main.transform.position = go.transform.position;
        }

    }

}// namespace SEE
