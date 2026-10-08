using System.Reflection;
using System.Text.Json;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.HistoricalWorker;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>Tests the shared neutral contract without loading the historical assembly.</summary>
    public sealed class HistoricalWorkerProtocolTests
    {
        [Fact]
        public void Request_RoundTripsStrictlyAndDeterministically()
        {
            WorkerRequest request = new(WorkerProtocol.Version, "analyze", new("public class C {}", "C", "M"));
            string json = JsonSerializer.Serialize(request, WorkerProtocol.JsonOptions);
            Assert.Equal(request, JsonSerializer.Deserialize<WorkerRequest>(json, WorkerProtocol.JsonOptions));
            Assert.Equal(json, JsonSerializer.Serialize(request, WorkerProtocol.JsonOptions));
            Assert.Contains("\"protocolVersion\":2", json);
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
            WorkerResponse response = new(WorkerProtocol.Version, "invalid", false, Failure: new(code, "failed", ["detail"]));
            string json = JsonSerializer.Serialize(response, WorkerProtocol.JsonOptions);
            WorkerResponse copy = JsonSerializer.Deserialize<WorkerResponse>(json, WorkerProtocol.JsonOptions)!;
            Assert.False(copy.Success);
            Assert.Null(copy.Result);
            Assert.Equal(code, copy.Failure!.Code);
            Assert.Equal(new[] { "detail" }, copy.Failure.Details);
            Assert.DoesNotContain("\"result\"", json);
        }

        [Fact]
        public void FutureInputCategories_AreTypedAndSerializableButNotSilentlySupported()
        {
            WorkerSourceDocument source = new("logical.cs", "public class C {}", new string('A', 64));
            WorkerReferenceImage reference = new("reference.dll", [1, 2, 3], new string('B', 64));
            WorkerSupportingCompilation supporting = new("supporting-project", [source], [reference], new(), "validated-manifest-id");
            WorkerAnalysisInput input = new("public class Root {}", "Root", "M",
                new(AdditionalSources: [source], References: [reference], SupportingCompilations: [supporting]));
            string json = JsonSerializer.Serialize(input, WorkerProtocol.JsonOptions);
            WorkerAnalysisInput copy = JsonSerializer.Deserialize<WorkerAnalysisInput>(json, WorkerProtocol.JsonOptions)!;
            Assert.Equal(json, JsonSerializer.Serialize(copy, WorkerProtocol.JsonOptions));
            Assert.False(WorkerInputValidation.IsSupported(copy));
            Assert.Equal("validated-manifest-id", Assert.Single(copy.Context!.SupportingCompilations!).Provenance);
            Assert.Equal(typeof(HistoricalWorkerClient).Assembly, typeof(WorkerRequest).Assembly);
        }

        [Fact]
        public void ContractGraph_IsRoslynFreeIncludingExistingCanonicalResultOwner()
        {
            Queue<Type> pending = new([typeof(WorkerRequest), typeof(WorkerResponse), typeof(HistoricalWorkerCallResult)]);
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
                else if (!type.IsPrimitive && !type.IsEnum && type != typeof(string) && type != typeof(Guid))
                {
                    // Permit only our closed DTO/domain graph, not opaque BCL bags/interfaces/delegates.
                    Assert.True(typeof(WorkerRequest).Assembly == type.Assembly, type.FullName);
                    Assert.False(type.IsInterface || type.IsAbstract, type.FullName);
                    foreach (Type argument in type.GenericTypeArguments) { pending.Enqueue(argument); }
                    foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        pending.Enqueue(property.PropertyType);
                    }
                }
            }

            Assert.Contains(seen, type => type.Name == "CanonicalExceptionFlowAnalysisResult");
            Assert.Contains(seen, type => type.Name == "CanonicalTypeIdentity");
            Assert.Contains(seen, type => type.Name == "WorkerSupportingCompilation");
            Assert.Contains(seen, type => type.Name == "WorkerReferenceImage");
            Assert.Contains(seen, type => type.Name == "CanonicalCallableIdentity");
        }

        [Fact]
        public void Request_RejectsUnknownFieldsAndIntegerFailureEnums()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkerRequest>(
                "{\"protocolVersion\":2,\"operation\":\"identity\",\"unexpected\":true}", WorkerProtocol.JsonOptions));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkerFailure>(
                "{\"code\":0,\"message\":\"bad\",\"details\":[]}", WorkerProtocol.JsonOptions));
        }
    }
}
