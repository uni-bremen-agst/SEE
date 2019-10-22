using SEE.DataModel;
using SEE.Layout;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractCCAObjectManager
{
    private readonly BlockFactory _blockFactory;

    public BlockFactory BlockFactory => _blockFactory;

    public AbstractCCAObjectManager(BlockFactory blockFactory)
    {
        _blockFactory = blockFactory;
    }

    public abstract bool GetRoot(out GameObject root);
    public abstract bool GetInnerNode(Node node, out GameObject innerNode);
    public abstract bool GetLeaf(Node node, out GameObject leaf);

    public abstract bool RemoveNode(Node node, out GameObject gameObject);

    public abstract void Clear();
}
