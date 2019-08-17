
using System.Collections.Generic;

public class Node : GraphElement, INode
{
    // Important note: Nodes should be created only by calling IGraph.newNode().
    // Do not use 'new Node()'.

    private const string linknameAttribute = "Linkage.Name";

    string INode.LinkName
    {
        get => GetString(linknameAttribute);
        // This will only set the linkname attribute, but does not alter the
        // hashed linknames of the underlying graph. You will likely want to
        // use Graph.SetLinkname instead. Otherwise expect inconsistencies.
        // This setter should only be called by Graph.SetLinkname.
        set => SetString(linknameAttribute, value);
    }

    private const string sourcenameAttribute = "Source.Name";
    
    string INode.SourceName
    {
        get => GetString(sourcenameAttribute);
        set => SetString(sourcenameAttribute, value);
    }

    private INode parent;

    INode INode.Parent
    {
        get => parent;
        set => parent = value;
    }

    public override string ToString()
    {
        string result = "{\n";
        result += " \"kind\": node,\n";
        result += base.ToString();
        result += "}";
        return result;
    }

    private List<INode> children = new List<INode>();

    int INode.NumberOfChildren()
    {
        return children.Count;
    }

    List<INode> INode.Children()
    {
        return children;
    }

    void INode.AddChild(INode child)
    {
        if (child.Parent == null)
        {
            children.Add(child);
            child.Parent = this;
        }
        else
        {
            throw new System.Exception("Hierarchical edges do not form a tree. Node with multiple parents: "
                + child.LinkName);
        }
    }
}
