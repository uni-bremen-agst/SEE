using UnityEngine;
using UnityEngine.UI;

namespace SEE
{

    public class AdditionalInformationShowSource : MonoBehaviour
    {
        private Text source;
        public string Source
        {
            get
            {
                return source.text;
            }
            set
            {
                source.text = value;
            }
        }

        void Awake()
        {
            source = gameObject.GetComponentInChildren<Text>();
        }
    }

}// namespace SEE
