using System.Reflection;
using System.Text.Json;
using XMLDocNormalizer.HistoricalWorker;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>Tests the shared neutral contract without loading the historical assembly.</summary>
    public sealed class HistoricalWorkerProtocolTests
    {
        [Fact]
        public void Request_RoundTripsStrictlyAndDeterministically()
        {
            WorkerRequest request = new(1, "analyze", new("public class C {}", "C", "M"));
            string json = JsonSerializer.Serialize(request, WorkerProtocol.JsonOptions);
            Assert.Equal(request, JsonSerializer.Deserialize<WorkerRequest>(json, WorkerProtocol.JsonOptions));
            Assert.Equal(json, JsonSerializer.Serialize(request, WorkerProtocol.JsonOptions));
            Assert.Contains("\"protocolVersion\":1", json);
        }

        [Theory]
        [InlineData("InvalidRequest")]
        [InlineData("UnsupportedProtocolVersion")]
        [InlineData("MalformedInput")]
        [InlineData("CompilationFailure")]
        [InlineData("AnalysisFailure")]
        [InlineData("UnexpectedWorkerFailure")]
        public void Failure_RoundTripsWithoutResult(string failureCode)
        {
            WorkerFailureCode code = Enum.Parse<WorkerFailureCode>(failureCode);
            WorkerResponse response = new(1, "invalid", false, Failure: new(code, "failed", ["detail"]));
            string json = JsonSerializer.Serialize(response, WorkerProtocol.JsonOptions);
            WorkerResponse copy = JsonSerializer.Deserialize<WorkerResponse>(json, WorkerProtocol.JsonOptions)!;
            Assert.False(copy.Success);
            Assert.Null(copy.Result);
            Assert.Equal(code, copy.Failure!.Code);
            Assert.Equal(new[] { "detail" }, copy.Failure.Details);
            Assert.DoesNotContain("\"result\"", json);
        }

        [Fact]
        public void ContractGraph_IsRoslynFreeIncludingExistingCanonicalResultOwner()
        {
            Queue<Type> pending = new([typeof(WorkerRequest), typeof(WorkerResponse)]);
            HashSet<Type> seen = [];
            while (pending.TryDequeue(out Type? type))
            {
                if (!seen.Add(type))
                {
                    continue;
                }

                Assert.False(type.Namespace?.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal) == true, type.FullName);
                Assert.NotEqual(typeof(object), type);
                if (type.IsArray)
                {
                    pending.Enqueue(type.GetElementType()!);
                }
                else if (Nullable.GetUnderlyingType(type) is Type element)
                {
                    pending.Enqueue(element);
                }
                else if (!type.IsPrimitive && !type.IsEnum && type != typeof(string))
                {
                    foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        pending.Enqueue(property.PropertyType);
                    }
                }
            }

            Assert.Contains(seen, type => type.Name == "CanonicalExceptionFlowAnalysisResult");
            Assert.Contains(seen, type => type.Name == "CanonicalTypeIdentity");
        }

        [Fact]
        public void Request_RejectsUnknownFieldsAndIntegerFailureEnums()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkerRequest>(
                "{\"protocolVersion\":1,\"operation\":\"identity\",\"unexpected\":true}", WorkerProtocol.JsonOptions));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkerFailure>(
                "{\"code\":0,\"message\":\"bad\",\"details\":[]}", WorkerProtocol.JsonOptions));
        }
    }
}
