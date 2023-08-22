using System.Xml.Linq;

namespace profiling2gxl;

internal class JProfilerParser : Parser
{
    public JProfilerParser(StreamReader Sr) : base(Sr) { }

    // Maps from function ID to function.
    private readonly IDictionary<string, Function> foundFunctions = new Dictionary<string, Function>();

    public override List<Function> parse()
    {
        XDocument xmlDocument = Helper.loadXML(Sr);
        foreach (XNode descendantNode in xmlDocument.DescendantNodes())
        {
            // Only handle if this is a <node> element
            if (descendantNode is XElement element && element.Name == "node")
            {
                XElement? parent = null;
                if (element.Parent != null && element.Parent.Name == "node")
                {
                    parent = element.Parent;
                }
                handleNode(element, parent);
            }
        }
        return foundFunctions.Values.ToList();
    }

    private static string nodeFunctionId(XElement node)
    {
        // NOTE: We assume these three will always exist. Otherwise, an NRE will be thrown.
        return $"{node.Attribute("class")!.Value}:{node.Attribute("methodName")!.Value}"
            + $":{node.Attribute("methodSignature")!.Value}";
    }

    private void handleNode(XElement node, XElement? parentFunctionElement = null)
    {
        string functionId = nodeFunctionId(node);
        Function function;
        if (foundFunctions.TryGetValue(functionId, out Function? foundFunction))
        {
            function = foundFunction;
            function.Called += int.Parse(node.Attribute("count")!.Value);
            function.Self += float.Parse(node.Attribute("selfTime")!.Value);
            function.Descendants += float.Parse(node.Attribute("time")!.Value);
            // TODO: Add percentage time if it's not a child of itself
        }
        else
        {
            function = new()
            {
                Id = functionId,
                Name = node.Attribute("methodName")!.Value,
                Module = node.Attribute("class")!.Value,
                Called = int.Parse(node.Attribute("count")!.Value),
                PercentageTime = float.Parse(node.Attribute("percent")!.Value),
                Self = float.Parse(node.Attribute("selfTime")!.Value),
                Descendants = float.Parse(node.Attribute("time")!.Value),
            };
            foundFunctions.Add(functionId, function);
        }
        if (parentFunctionElement != null)
        {
            // Due to the order we go through the tree, we know that the parent must be here already.
            Function parentFunction = foundFunctions[nodeFunctionId(parentFunctionElement)];
            function.Parents.Add(parentFunction);
            // We also need to add this child to the parent.
            parentFunction.Children.Add(function.Id);
        }
    }
}