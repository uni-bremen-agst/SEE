using System.Collections.Generic;

public abstract class GraphElement : Attributable, IGraphElement
{
    private string type;

    string IGraphElement.Type
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

