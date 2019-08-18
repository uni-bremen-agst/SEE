using System.Collections.Generic;
using UnityEngine;

namespace SEE.DataModel
{
    [System.Serializable]
    public class Node : GraphElement, INode
    {
        // Important note: Nodes should be created only by calling IGraph.newNode().
        // Do not use 'new Node()'.

        private const string linknameAttribute = "Linkage.Name";

        [SerializeField]
        public string LinkName
        {
            get => GetString(linknameAttribute);
            // This will only set the linkname attribute, but does not alter the
            // hashed linknames of the underlying graph. You will likely want to
            // use Graph.SetLinkname instead. Otherwise expect inconsistencies.
            // This setter should only be called by Graph.SetLinkname.
            set => SetString(linknameAttribute, value);
        }

        private const string sourcenameAttribute = "Source.Name";

        public string SourceName
        {
            get => GetString(sourcenameAttribute);
            set => SetString(sourcenameAttribute, value);
        }

        [SerializeField]
        private INode parent;

        [SerializeField]
        public INode Parent
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

        [SerializeField]
        private List<INode> children = new List<INode>();

        public int NumberOfChildren()
        {
            return children.Count;
        }

        public List<INode> Children()
        {
            return children;
        }

        public void AddChild(INode child)
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
}