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

        /// <summary>
        /// Creates deep copies of attributes where necessary. Is called by
        /// Clone() once the copy is created. Must be extended by every 
        /// subclass that adds fields that should be cloned, too.
        /// </summary>
        /// <param name="clone">the clone receiving the copied attributes</param>
        protected override void HandleCloned(object clone)
        {
            base.HandleCloned(clone);
            GraphElement target = (GraphElement)clone;
            target.type = this.type;
        }
    }
}