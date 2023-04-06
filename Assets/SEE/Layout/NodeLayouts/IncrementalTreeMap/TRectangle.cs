namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    public class TRectangle
    {
        public TRectangle(float x, float z, float width, float depth)
        {
            this.x = x;
            this.z = z;
            this.width = width;
            this.depth = depth;
        }
        public float x;      // x co-ordinate at corner
        public float z;      // z co-ordinate at corner
        public float width;  // width
        public float depth;  // depth

        public float aspect_ratio()
        {
            return width >= depth ? width / depth : depth / width;
        }

        public float area()
        {
            return width*depth;
        }

    }
}