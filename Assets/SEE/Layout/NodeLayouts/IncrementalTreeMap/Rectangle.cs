namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    /// <summary>
    /// 
    /// </summary>
    public class Rectangle
    {
        public Rectangle(double x, double z, double width, double depth)
        {
            X = x;
            Z = z;
            Width = width;
            Depth = depth;
        }

        public double X;      // x co-ordinate at corner 
        public double Z;      // z co-ordinate at corner
        public double Width;  // width
        public double Depth;  // depth

        public double AspectRatio()
        {
            return Width >= Depth ? Width / Depth : Depth / Width;
        }

        public double Area()
        {
            return Width*Depth;
        }
        public Rectangle Clone()
        {
            return new Rectangle(x:X,z:Z,width:Width,depth:Depth);
        }
    }
}