using System.Collections.Generic;

namespace SEE
{
    public abstract class GraphElement : Attributable, IGraphElement
    {
        private string type;

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