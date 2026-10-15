using System.Text.Json;

// Adversarial pipe peer only: no Analyzer, Worker contract copy, reference or input-source execution.
using JsonDocument request = JsonDocument.Parse(await Console.In.ReadToEndAsync());
if (request.RootElement.GetProperty("payload").TryGetProperty("context", out JsonElement context)
    && (context.TryGetProperty("declaredPolicy", out JsonElement policy) && policy.TryGetProperty("documentation", out _)
        || context.TryGetProperty("finding", out JsonElement finding) && finding.TryGetProperty("documentation", out _)))
{
    // B21: a real process with copied genuine identity/binding JSON is not the executed Historical Worker.
    Console.Write(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "b21-replayed-worker-response.json")));
    return 0;
}
string? scenario = request.RootElement.GetProperty("payload").GetProperty("source").GetString();
switch (scenario)
{
    case "hang":
        await Task.Delay(Timeout.InfiniteTimeSpan);
        break;
    case "crash":
        Console.Error.Write("test crash diagnostic");
        return 23;
    case "malformed":
        Console.Write("not-json");
        break;
    case "mismatch":
        Console.Write("{\"protocolVersion\":99}");
        break;
    case "nonzero":
        Console.Write("{\"protocolVersion\":2,\"operation\":\"analyze\",\"success\":true,\"result\":{\"entries\":[],\"uncertainties\":[]}}");
        return 7;
    case "flood":
        Console.Write(new string('x', 1048576 + 8192));
        break;
    case "stderr-flood":
        Console.Error.Write(new string('x', 16384 + 8192));
        break;
    case "utf8":
        await Console.OpenStandardOutput().WriteAsync(new byte[] { 0xff });
        break;
}
return 0;
