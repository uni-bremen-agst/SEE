namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    public class TRectangle : System.ICloneable
    {
        public TRectangle(double x, double z, double width, double depth)
        {
            this.x = x;
            this.z = z;
            this.width = width;
            this.depth = depth;
        }
        public double x;      // x co-ordinate at corner
        public double z;      // z co-ordinate at corner
        public double width;  // width
        public double depth;  // depth

        public double AspectRatio()
        {
            return width >= depth ? width / depth : depth / width;
        }

        public double Area()
        {
            return width*depth;
        }
        public object Clone()
        {
            return new TRectangle(x:x,z:z,width:width,depth:depth);
        }
    }
}