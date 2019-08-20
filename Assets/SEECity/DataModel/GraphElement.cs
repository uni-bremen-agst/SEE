using UnityEngine;

namespace SEE.DataModel
{
    /// <summary>
    /// A type graph element. Either a node or an edge.
    /// </summary>
    [System.Serializable]
    public abstract class GraphElement : Attributable
    {
        [SerializeField]
        private string type;

        /// <summary>
        /// The type of this graph element.
        /// </summary>
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