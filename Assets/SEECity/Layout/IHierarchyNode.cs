using System.Collections.Generic;

namespace SEE.Layout
{
    public interface IHierarchyNode<T>
    {
        /// <summary>
        /// The parent of the node. Is null if the node is a root.
        /// </summary>
        T Parent { get; }

        /// <summary>
        /// The level of the node in the node hierarchy. A root node has
        /// level 0. For all other nodes, the level is the distance from
        /// the node to its root.
        /// </summary>
        int Level { get; set; }

        /// <summary>
        /// True if the given node is to be interpreted as a leaf by the layouter.
        /// 
        /// Note: Even leaves may have children. What to do with those is the decision of the
        /// layouter. It may or may not lay them out.
        /// </summary>
        bool IsLeaf { get; }

        /// <summary>
        /// The set of children of this node. Note: Even nodes for which IsLeaf
        /// returns true, may still have children. Layouts may refuse to layout
        /// the children of a node for which IsLeaf returns true.
        /// </summary>
        /// <returns>children of this node</returns>
        ICollection<T> Children();
    }

    interface IHierarchynode<T>
    {
        T Parent { get; }
        ICollection<T> Children { get; }
        int Level { get; }
    }

    interface IGraphnode<T>
    {
        ICollection<T> Successors { get; }
    }

    interface IGamenode
    {
        int Scale { get; set; }
    }

    interface ILayoutnode : IGamenode, IHierarchynode<ILayoutnode>, IGraphnode<ILayoutnode>
    {
    }

    abstract class AbstractLayoutnode : ILayoutnode
    {
        public ILayoutnode Parent => throw new System.NotImplementedException();

        public ICollection<ILayoutnode> Successors => throw new System.NotImplementedException();

        public abstract ICollection<ILayoutnode> Children { get; }
        public abstract int Level { get; }
        public int Scale { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    }

    class Layoutnode : AbstractLayoutnode
    {
        public override ICollection<ILayoutnode> Children => throw new System.NotImplementedException();

        public override int Level => throw new System.NotImplementedException();
    }

    class LCAfinder<T> where T : IHierarchynode<T>
    {
        public T foo(T t)
        {
            ICollection<T> kids = t.Children;
            T p = t.Parent;
            return p;

        }
    }

    class LCAfinderClient
    {
        void bar()
        {
            LCAfinder<ILayoutnode> g = new LCAfinder<ILayoutnode>();
            AbstractLayoutnode y = new Layoutnode();
            ILayoutnode x = g.foo(y);
        }
    }

    abstract class NLayout
    {
        public abstract Dictionary<IGamenode, NodeTransform> Layout(ICollection<IGamenode> layoutNodes);
    }

    /*
    class HierarchyLayout : NLayout
    {
        public override Dictionary<IGamenode, NodeTransform> Layout(ICollection<ILayoutnode> layoutNodes)
        {
            throw new System.NotImplementedException();
        }
    }
    */
}