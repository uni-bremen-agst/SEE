using System.Text.RegularExpressions;

namespace profiling2gxl
{
    /// <summary>
    /// Parser for Java hprof output.
    /// java -agentlib:hprof=cpu=samples myapp
    ///    or
    /// java -agentlib:hprof=heap=sites myapp
    /// </summary>
    internal class HprofParser : Parser
    {
        /// <summary>
        /// The regular expression for the hprof trace entry header.
        /// </summary>
        private readonly Regex _traceHeaderRegex = new (@"^TRACE (\d+):$", RegexOptions.Compiled);

        /// <summary>
        /// The regular expression for the hprof trace entry.
        /// </summary>
        private readonly Regex _traceEntryRegex = new(@"^\s+(?<name>.*)\((?<module>.*):[0-9a-zA-Z\s]+\)$", RegexOptions.Compiled);

        /// <summary>
        /// The regular expression for a performance entry in the cpu samples ranking.
        /// </summary>
        private readonly Regex _performanceEntryRegex = new(@"^\s*(?<rank>[0-9]+)\s+(?<self>[0-9]+\.[0-9]+)%\s+(?<accum>[0-9]+\.[0-9]+)%\s+(?<called>[0-9]+)\s(?<trace>[0-9]+)\s+(?<name>.*)$", RegexOptions.Compiled);

        /// <summary>
        /// The regular expression for a heap allocation entry in the heap allocation ranking.
        /// </summary>
        private readonly Regex _heapAllocEntryRegex = new(@"^\s*(?<rank>[0-9]+)\s+(?<self>[0-9]+\.[0-9]+)%\s+(?<accum>[0-9]+\.[0-9]+)%\s+(?<live_bytes>[0-9]+)\s+(?<live_objs>[0-9]+)\s+(?<alloc_bytes>[0-9]+)\s+(?<alloc_objs>[0-9]+)\s+(?<trace>[0-9]+)\s+(?<name>.*)$", RegexOptions.Compiled);

        public HprofParser(StreamReader Sr) : base(Sr) { }

        /// <summary>
        /// Reads a line of characters from the stream reader, removes trailing \r and \n and returns the data as a string.
        /// </summary>
        /// <returns>The next line from the stream reader, or  <see langword="null"/> if the end of the stream reader is reached.</returns>
        /// <exception cref="OutOfMemoryException"></exception>
        /// <exception cref="IOException"></exception>
        private string? ReadLine()
        {
            var line = Sr.ReadLine();
            if (line == null) return line;
            char[] charsToRemove = { '\r', '\n' };
            line = line.TrimEnd(charsToRemove);
            return line;
        }

        private void ParseCg(List<Match> mos, string traceId)
        {
            Callee? callee = null;
            for (int i = 0; i < mos.Count; i++)
            {
                var mo = mos[i];
                if (mo.Groups.TryGetValue("name", out var nameGroup) && mo.Groups.TryGetValue("module", out var moduleGroup))
                {
                    var name = nameGroup.Value;
                    var module = moduleGroup.Value;
                    if (module == "<Unknown Source>")
                    {
                        module = "UnknownClass";
                    }

                    var func = Functions.Find(f => f.Id == $"{module}:{name}");
                    if (func == null)
                    {
                        func = new()
                        {
                            Name = name,
                            Module = module,
                            Id = $"{module}:{name}"
                        };
                        Functions.Add(func);
                    }

                    if (i == 0)
                    {
                        func.TraceIds.Add(int.Parse(traceId));
                    }

                    if (callee != null && func.Id != callee.Id && func.Children.Find(child => child.Id == callee.Id) == null)
                    {
                        func.Children.Add(callee);
                        var calleeFunc = Functions.Find(f => f.Id == callee.Id);
                        calleeFunc?.Parents.Add(func);
                    }

                    callee = new(func.Id);
                }
            }
        }

        private Function getFunctionFromTraceId(string traceId, string name)
        {
            var func = Functions.Find(f => f.TraceIds.Contains(int.Parse(traceId)));

            if (func == null)
            {
                func = new()
                {
                    Id = $"UnknownClass:{name}",
                    Name = name,
                    Module = "UnknownClass",
                };
                Functions.Add(func);
            }

            return func;
        }

        private void ParsePerformaceEntry(Match mo)
        {
            if (mo.Groups.TryGetValue("name", out var nameGroup) && mo.Groups.TryGetValue("trace", out var traceGroup))
            {
                var name = nameGroup.Value;
                var traceId = traceGroup.Value;

                var func = getFunctionFromTraceId(traceId, name);

                var calledGroup = mo.Groups.GetValueOrDefault("called");
                if (calledGroup != null)
                {
                    func.Called += int.Parse(calledGroup.Value);
                }

                var selfGroup = mo.Groups.GetValueOrDefault("self");
                if (selfGroup != null)
                {
                    func.Self += float.Parse(selfGroup.Value.Replace(".", ","));
                }
            }
        }

        private void ParseHeapAllocEntry(Match mo)
        {
            if (mo.Groups.TryGetValue("name", out var nameGroup) && mo.Groups.TryGetValue("trace", out var traceGroup))
            {
                var name = nameGroup.Value;
                var traceId = traceGroup.Value;

                var func = getFunctionFromTraceId(traceId, name);

                var allocBytesGroup = mo.Groups.GetValueOrDefault("alloc_bytes");
                if (allocBytesGroup != null)
                {
                    func.AllocHeap += int.Parse(allocBytesGroup.Value);
                }

                var selfGroup = mo.Groups.GetValueOrDefault("self");
                if (selfGroup != null)
                {
                    func.Self += float.Parse(selfGroup.Value.Replace(".", ","));
                }
            }
        }

        public override List<Function> parse()
        {
            var line = ReadLine();
            List<Match> mos = new();
            bool isTraceMatch = false;
            string traceId = ""; 
            while (line != null)
            {
                Match mo = _traceEntryRegex.Match(line);
                if (mo.Success)
                {
                    mos.Add(mo);
                    isTraceMatch = true;
                }
                else if (isTraceMatch && mos.Count > 0 && !string.IsNullOrEmpty(traceId))
                {
                    ParseCg(mos, traceId);
                    mos.Clear();
                    traceId = "";
                    isTraceMatch = false;
                }
                mo = _traceHeaderRegex.Match(line);
                if (mo.Success)
                {
                    traceId = mo.Groups[1].Value;
                }
                mo = _heapAllocEntryRegex.Match(line);
                if (mo.Success)
                {
                    ParseHeapAllocEntry(mo);
                }
                else
                {
                    mo = _performanceEntryRegex.Match(line);
                    if (mo.Success) {
                        ParsePerformaceEntry(mo);
                    }
                }
                line = ReadLine();
            }
            Functions.ForEach(function =>
            {
                if (function.Called == 0)
                {
                    function.Called = 1;
                }
            });
            return Functions;
        }
    }
}
