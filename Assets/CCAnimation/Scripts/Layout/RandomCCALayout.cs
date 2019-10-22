using System.Collections;
using System.Collections.Generic;
using SEE;
using SEE.DataModel;
using SEE.Layout;
using UnityEngine;

public class RandomCCALayout : AbstractCCALayout
{
    public RandomCCALayout(GraphSettings set, BlockFactory blockFactory, IScale scaler) : base(set, blockFactory, scaler)
    {

    }

    public override void Draw(Graph graph)
    {
        base.Draw(graph);
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return base.ToString();
    }

    protected override void DrawEdges(Graph graph)
    {
        base.DrawEdges(graph);
    }

    protected override void DrawNodes(Graph graph)
    {
        base.DrawNodes(graph);
    }
}
