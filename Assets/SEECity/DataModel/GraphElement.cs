using UnityEngine;

namespace SEE.DataModel
{
    [System.Serializable]
    public abstract class GraphElement : Attributable, IGraphElement
    {
        [SerializeField]
        private string type;

        [SerializeField]
        public string Type
        {
            get
            {
                return type;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    type = value;
                }
                else
                {
                    type = "Unknown";
                }
            }
        }

        public override string ToString()
        {
            return " \"type\": " + type + "\",\n" + base.ToString();
        }
    }
}