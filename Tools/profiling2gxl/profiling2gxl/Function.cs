namespace profiling2gxl
{
    public class Function
    {
        /// <summary>
        /// A Unique ID - will be used for Linkage.Name
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// The CPU-time the function took excluding subfunctions (in secs or percent depending on the profiler). If both values are provided use <see cref="PercentageTime"/> for the time in percent. 
        /// </summary>
        public float Self { get; set; }
        /// <summary>
        /// The CPU-time the function took in percentage.
        /// </summary>
        public float PercentageTime { get; set; }
        /// <summary>
        /// The CPU-time the function took including subfunctions (in secs or percent depending on the profiler).
        /// </summary>
        public float Descendants { get; set; }
        /// <summary>
        /// The amount the function was called.
        /// </summary>
        public int Called { get; set; }
        /// <summary>
        /// The functions name.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The functions module/class.
        /// </summary>
        public string Module { get; set; }
        /// <summary>
        /// The functions parents (callers).
        /// </summary>
        public List<Function> Parents { get; set; }
        /// <summary>
        /// The functions children (callees/subfunctions) ids. 
        /// </summary>
        public List<string> Children { get; set; }
        /// <summary>
        /// Memory stored
        /// </summary>
        public float MemStore { get; set; }
        /// <summary>
        /// Memory load
        /// </summary>
        public float MemLoad { get; set; }
        /// <summary>
        /// Allocation heap
        /// </summary>
        public int AllocHeap { get; set; }
        /// <summary>
        /// Path to the file containing the function.
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// The filename containing the function.
        /// </summary>
        public string Filename { get; set; }

        public Function()
        {
            Parents = new();
            Children = new();
            Id = "";
            Name = "";
            Module = "Unknown";
            Path = "";
            Filename = "";
        }
    }
}
