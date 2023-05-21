namespace profiling2gxl
{
    public class Function
    {
        public string Id { get; set; }
        public float Self { get; set; }
        public float Descendants { get; set; }
        public int Called { get; set; }
        public int CalledTotal { get; set; }
        public string Name { get; set; }
        public string Module { get; set; }
        public List<int> TraceIds { get; set; }
        public List<Function> Parents { get; set; }
        public List<Callee> Children { get; set; }
        public float MemStore { get; set; }
        public float MemLoad { get; set; }
        public int AllocHeap { get; set; }
        public string Path { get; set; }
        public string Filename { get; set; }

        public Function()
        {
            Parents = new();
            Children = new();
            TraceIds = new();
            Id = "";
            Name = "";
            Module = "";
            Path = "";
            Filename = "";
        }
    }
}
