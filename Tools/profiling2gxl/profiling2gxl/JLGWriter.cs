using System.Data;

namespace profiling2gxl
{
    internal static class JLGWriter
    {
        /// <summary>
        /// Saves given <paramref name="graph"/> in GXL format in a file with given <paramref name="filename"/>.
        /// The parent-child relation between nodes is stored as edges with the type <paramref name="hierarchicalEdgeType"/>.
        /// The attributes of the <paramref name="graph"/> itself are not stored to stay compatible
        /// with the GXL files by Axivion.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="functions"></param>
        /// <exception cref="Exception"></exception>
        public static void Save(string filename, List<Function> functions)
        {
            using StreamWriter sw = new(filename);
            sw.WriteLine(ToFilesEntry(functions));
            var locations = ToLocations(functions);
            var startFunction = functions.Find(f => f.Parents.Count == 0);
            startFunction ??= functions.Find(f => f.Name == "Unknown");
            if (startFunction == null)
            {
                throw new Exception("Could not found the start entry");
            }
            var orderedFunctionCalls = ToOrderedFunctionCalls(startFunction, new(), functions);
            foreach (var funcId in orderedFunctionCalls)
            {
                if (!funcId.StartsWith("/-"))
                {
                    sw.WriteLine("-/" + locations[funcId] + ">1");
                }
                else
                {
                    var id = funcId.Remove(0, 2);
                    sw.WriteLine("/-" + locations[id] + ">1");
                    sw.WriteLine("=>unknown");
                }
            }
            sw.WriteLine(ToLocationsEntry(locations));
        }

        private static string ToFilesEntry(List<Function> functions)
        {
            var files = functions.Where(f => !string.IsNullOrEmpty(f.Module)).ToList().Select(f => f.Module).GroupBy(f => f).Select(f => f.First()).ToList();
            var filesEntry = "$[" + string.Join(", ", files) + "]";
            return filesEntry;
        }

        private static Dictionary<string, string> ToLocations(List<Function> functions)
        {
            Dictionary<string, string> locations = new();
            int counter = 0;
            foreach (var f in functions)
            {
                locations[f.Id] = $"{counter}";
                counter++;
            }
            return locations;
        }

        private static string ToLocationsEntry(Dictionary<string, string> locations)
        {
            var locationsEntry = "*";
            foreach (var kv in locations)
            {
                var funcCall = kv.Key;
                if (!funcCall.EndsWith(")"))
                {
                    funcCall += "()";
                }
                locationsEntry += $"-{kv.Value}={funcCall};";
            }
            return locationsEntry;
        }

        private static List<string> ToOrderedFunctionCalls(Function func, List<string> calledFunctions, List<Function> functions)
        {
            if (!calledFunctions.Contains(func.Id)) {
                calledFunctions.Add(func.Id);
                if (func.Children.Count > 0)
                {
                    foreach(var child in func.Children)
                    {
                        var childFunc = functions.Find(f =>  f.Id == child.Id);
                        if (childFunc != null)
                        {
                            calledFunctions = ToOrderedFunctionCalls(childFunc, calledFunctions, functions);
                        }
                    }
                }
                calledFunctions.Add("/-" + func.Id); //This line marks the exit of the called function.
            }
            return calledFunctions;
        }
    }
}
