using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;
using P5ATests = XMLDocNormalizerTests.Execution.Semantic.ExternalCompilationProvenanceDescriptorFactoryTests;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Tests fail-closed reconstruction of C# options from P5A provenance.
    /// </summary>
    public sealed class ExternalCSharpCompilationConfigurationFactoryTests
    {
        /// <summary>
        /// Applies the project argument-null convention.
        /// </summary>
        [Fact]
        public void NullProvenance_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => ExternalCSharpCompilationConfigurationFactory.TryCreate(
                    null!,
                    out _));
        }

        /// <summary>
        /// Rejects provenance without a Compilation Options CDI.
        /// </summary>
        [Fact]
        public void MissingCompilationOptionsCdi_FailsClosed()
        {
            ExternalCompilationProvenanceDescriptor provenance =
                CreateProvenance(compilationOptions: null);

            Assert.False(ExternalCSharpCompilationConfigurationFactory.TryCreate(
                provenance,
                out ExternalCSharpCompilationConfiguration configuration));
            Assert.Null(configuration);
        }

        /// <summary>
        /// Reconstructs all specification-defined defaults explicitly and
        /// applies the documented parse-analysis policies.
        /// </summary>
        [Fact]
        public void OptionalOptionsMissing_UseSpecificationDefaultsAndAnalysisPolicies()
        {
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration();

            Assert.Equal(LanguageVersion.CSharp12, configuration.ParseOptions.LanguageVersion);
            Assert.Equal(
                LanguageVersion.CSharp12,
                configuration.ParseOptions.SpecifiedLanguageVersion);
            Assert.Empty(configuration.ParseOptions.PreprocessorSymbolNames);
            Assert.Equal(DocumentationMode.Parse, configuration.ParseOptions.DocumentationMode);
            Assert.Equal(SourceCodeKind.Regular, configuration.ParseOptions.Kind);
            Assert.Empty(configuration.ParseOptions.Features);
            Assert.Empty(configuration.ParseOptions.Errors);

            Assert.Equal(
                OutputKind.DynamicallyLinkedLibrary,
                configuration.CompilationOptions.OutputKind);
            Assert.Equal(
                OptimizationLevel.Debug,
                configuration.CompilationOptions.OptimizationLevel);
            Assert.False(configuration.CompilationOptions.CheckOverflow);
            Assert.False(configuration.CompilationOptions.AllowUnsafe);
            Assert.Equal(Platform.AnyCpu, configuration.CompilationOptions.Platform);
            Assert.Equal(
                NullableContextOptions.Disable,
                configuration.CompilationOptions.NullableContextOptions);
            Assert.Same(
                AssemblyIdentityComparer.Default,
                configuration.CompilationOptions.AssemblyIdentityComparer);
            Assert.Empty(configuration.CompilationOptions.Errors);

            Assert.Equal("compiler-version-exact", configuration.CompilerVersion);
            Assert.Equal("runtime-version-exact", configuration.RuntimeVersion);
            Assert.Equal(1, configuration.SourceFileCount);
            Assert.Null(configuration.DefaultEncodingWebName);
            Assert.Null(configuration.FallbackEncodingWebName);
        }

        /// <summary>
        /// Rejects each required schema or semantic option independently.
        /// </summary>
        /// <param name="key">The required key to remove.</param>
        [Theory]
        [InlineData("version")]
        [InlineData("language")]
        [InlineData("compiler-version")]
        [InlineData("runtime-version")]
        [InlineData("source-file-count")]
        [InlineData("output-kind")]
        [InlineData("platform")]
        [InlineData("language-version")]
        public void MissingRequiredOption_FailsClosed(string key)
        {
            Assert.False(TryCreate(out _, (key, null)));
        }

        /// <summary>
        /// Rejects an unsupported or malformed compilation-options schema.
        /// </summary>
        /// <param name="version">The serialized schema version.</param>
        [Theory]
        [InlineData("")]
        [InlineData("1")]
        [InlineData("3")]
        [InlineData("02")]
        public void UnsupportedOptionsVersion_FailsClosed(string version)
        {
            Assert.False(TryCreate(out _, ("version", version)));
        }

        /// <summary>
        /// Rejects unknown keys while leaving P5A's preserved provenance
        /// behavior unchanged.
        /// </summary>
        [Fact]
        public void UnknownOptionKey_FailsClosed()
        {
            Assert.False(TryCreate(out _, ("future-semantic-option", "enabled")));
        }

        /// <summary>
        /// Requires the exact canonical C# language value.
        /// </summary>
        /// <param name="language">A noncanonical or unsupported language.</param>
        [Theory]
        [InlineData("Visual Basic")]
        [InlineData("c#")]
        [InlineData("CSharp")]
        [InlineData("")]
        public void WrongLanguage_FailsClosed(string language)
        {
            Assert.False(TryCreate(out _, ("language", language)));
        }

        /// <summary>
        /// Requires nonempty compiler and runtime provenance values.
        /// </summary>
        /// <param name="key">The version key to empty.</param>
        [Theory]
        [InlineData("compiler-version")]
        [InlineData("runtime-version")]
        public void EmptyCompilerOrRuntimeVersion_FailsClosed(string key)
        {
            Assert.False(TryCreate(out _, (key, string.Empty)));
        }

        /// <summary>
        /// Preserves valid source-file counts parsed with invariant syntax.
        /// </summary>
        /// <param name="serialized">The serialized count.</param>
        /// <param name="expected">The expected value.</param>
        [Theory]
        [InlineData("0", 0)]
        [InlineData("42", 42)]
        [InlineData("2147483647", int.MaxValue)]
        public void SourceFileCount_ValidNonnegativeIntegerIsPreserved(
            string serialized,
            int expected)
        {
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                ("source-file-count", serialized));

            Assert.Equal(expected, configuration.SourceFileCount);
        }

        /// <summary>
        /// Rejects malformed, signed, spaced, negative, and overflowing counts.
        /// </summary>
        /// <param name="serialized">The invalid serialized count.</param>
        [Theory]
        [InlineData("-1")]
        [InlineData("+1")]
        [InlineData(" 1")]
        [InlineData("1 ")]
        [InlineData("1.0")]
        [InlineData("2147483648")]
        [InlineData("not-a-number")]
        public void SourceFileCount_InvalidValueFailsClosed(string serialized)
        {
            Assert.False(TryCreate(out _, ("source-file-count", serialized)));
        }

        /// <summary>
        /// Maps stable numeric language versions through Roslyn's public API.
        /// </summary>
        /// <param name="serialized">The stable numeric PDB value.</param>
        /// <param name="expected">The expected Roslyn enum.</param>
        [Theory]
        [InlineData("1", LanguageVersion.CSharp1)]
        [InlineData("7.0", LanguageVersion.CSharp7)]
        [InlineData("7.1", LanguageVersion.CSharp7_1)]
        [InlineData("12", LanguageVersion.CSharp12)]
        [InlineData("14.0", LanguageVersion.CSharp14)]
        public void LanguageVersion_StableNumericValueIsMapped(
            string serialized,
            LanguageVersion expected)
        {
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                ("language-version", serialized));

            Assert.Equal(expected, configuration.ParseOptions.LanguageVersion);
            Assert.Equal(expected, configuration.ParseOptions.SpecifiedLanguageVersion);
        }

        /// <summary>
        /// Rejects aliases, preview/default selectors, future versions, and
        /// malformed numeric shapes without approximation.
        /// </summary>
        /// <param name="serialized">The unsupported language-version value.</param>
        [Theory]
        [InlineData("latest")]
        [InlineData("latestMajor")]
        [InlineData("preview")]
        [InlineData("default")]
        [InlineData("999.0")]
        [InlineData("12.00")]
        [InlineData(".1")]
        [InlineData("12.")]
        [InlineData("+12")]
        [InlineData(" 12.0")]
        public void LanguageVersion_UnstableFutureOrMalformedValueFailsClosed(
            string serialized)
        {
            Assert.False(TryCreate(out _, ("language-version", serialized)));
        }

        /// <summary>
        /// Treats a missing or empty define value as an empty symbol sequence.
        /// </summary>
        /// <param name="serialized">The absent or empty define value.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Define_MissingOrEmptyProducesNoSymbols(string? serialized)
        {
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                ("define", serialized));

            Assert.Empty(configuration.ParseOptions.PreprocessorSymbolNames);
        }

        /// <summary>
        /// Preserves define order, case, and duplicate symbols exactly.
        /// </summary>
        [Fact]
        public void Define_ValidSymbolsPreserveOrderCaseAndDuplicates()
        {
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                ("define", "ZETA,alpha,ZETA,Δelta"));

            Assert.Equal(
                new[] { "ZETA", "alpha", "ZETA", "Δelta" },
                configuration.ParseOptions.PreprocessorSymbolNames);
        }

        /// <summary>
        /// Rejects empty, spaced, keyword, and otherwise invalid define items.
        /// </summary>
        /// <param name="serialized">The invalid define list.</param>
        [Theory]
        [InlineData("DEBUG,,TRACE")]
        [InlineData(",DEBUG")]
        [InlineData("DEBUG,")]
        [InlineData("DEBUG, TRACE")]
        [InlineData("1INVALID")]
        public void Define_InvalidSymbolFailsClosed(string serialized)
        {
            Assert.False(TryCreate(out _, ("define", serialized)));
        }

        /// <summary>
        /// Maps only the canonical checked values and its specified default.
        /// </summary>
        /// <param name="serialized">The absent or canonical value.</param>
        /// <param name="expected">The expected overflow-check setting.</param>
        [Theory]
        [InlineData(null, false)]
        [InlineData("False", false)]
        [InlineData("True", true)]
        public void Checked_CanonicalValueIsMapped(string? serialized, bool expected)
        {
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                ("checked", serialized));

            Assert.Equal(expected, configuration.CompilationOptions.CheckOverflow);
        }

        /// <summary>
        /// Rejects noncanonical checked values.
        /// </summary>
        /// <param name="serialized">The invalid Boolean spelling.</param>
        [Theory]
        [InlineData("true")]
        [InlineData("false")]
        [InlineData("1")]
        [InlineData("yes")]
        public void Checked_NoncanonicalValueFailsClosed(string serialized)
        {
            Assert.False(TryCreate(out _, ("checked", serialized)));
        }

        /// <summary>
        /// Maps only the canonical unsafe values and its specified default.
        /// </summary>
        /// <param name="serialized">The absent or canonical value.</param>
        /// <param name="expected">The expected unsafe setting.</param>
        [Theory]
        [InlineData(null, false)]
        [InlineData("False", false)]
        [InlineData("True", true)]
        public void Unsafe_CanonicalValueIsMapped(string? serialized, bool expected)
        {
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                ("unsafe", serialized));

            Assert.Equal(expected, configuration.CompilationOptions.AllowUnsafe);
        }

        /// <summary>
        /// Rejects noncanonical unsafe values.
        /// </summary>
        /// <param name="serialized">The invalid Boolean spelling.</param>
        [Theory]
        [InlineData("true")]
        [InlineData("false")]
        [InlineData("0")]
        [InlineData("no")]
        public void Unsafe_NoncanonicalValueFailsClosed(string serialized)
        {
            Assert.False(TryCreate(out _, ("unsafe", serialized)));
        }

        /// <summary>
        /// Maps every canonical nullable mode and the specified missing default.
        /// </summary>
        /// <param name="serialized">The absent or canonical value.</param>
        /// <param name="expected">The expected nullable context.</param>
        [Theory]
        [InlineData(null, NullableContextOptions.Disable)]
        [InlineData("Disable", NullableContextOptions.Disable)]
        [InlineData("Warnings", NullableContextOptions.Warnings)]
        [InlineData("Annotations", NullableContextOptions.Annotations)]
        [InlineData("Enable", NullableContextOptions.Enable)]
        public void Nullable_CanonicalValueIsMapped(
            string? serialized,
            NullableContextOptions expected)
        {
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                ("nullable", serialized));

            Assert.Equal(expected, configuration.CompilationOptions.NullableContextOptions);
        }

        /// <summary>
        /// Rejects unknown and noncanonical nullable modes.
        /// </summary>
        /// <param name="serialized">The invalid nullable value.</param>
        [Theory]
        [InlineData("enabled")]
        [InlineData("enable")]
        [InlineData("Unknown")]
        public void Nullable_NoncanonicalValueFailsClosed(string serialized)
        {
            Assert.False(TryCreate(out _, ("nullable", serialized)));
        }

        /// <summary>
        /// Maps the two exactly reconstructable optimization levels and the
        /// specified missing default.
        /// </summary>
        /// <param name="serialized">The absent or canonical value.</param>
        /// <param name="expected">The expected optimization level.</param>
        [Theory]
        [InlineData(null, OptimizationLevel.Debug)]
        [InlineData("debug", OptimizationLevel.Debug)]
        [InlineData("release", OptimizationLevel.Release)]
        public void Optimization_SupportedValueIsMapped(
            string? serialized,
            OptimizationLevel expected)
        {
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                ("optimization", serialized));

            Assert.Equal(expected, configuration.CompilationOptions.OptimizationLevel);
        }

        /// <summary>
        /// Rejects debug-plus modes and other unknown optimization values
        /// because Roslyn 5.0.0 exposes no lossless public setter.
        /// </summary>
        /// <param name="serialized">The unsupported optimization value.</param>
        [Theory]
        [InlineData("debug-plus")]
        [InlineData("release-debug-plus")]
        [InlineData("Release")]
        [InlineData("unknown")]
        public void Optimization_UnsupportedValueFailsClosed(string serialized)
        {
            Assert.False(TryCreate(out _, ("optimization", serialized)));
        }

        /// <summary>
        /// Maps every output kind in the referenced Roslyn 5.0.0 API explicitly.
        /// </summary>
        /// <param name="serialized">The exact Portable PDB value.</param>
        /// <param name="expected">The expected Roslyn enum.</param>
        [Theory]
        [InlineData("ConsoleApplication", OutputKind.ConsoleApplication)]
        [InlineData("WindowsApplication", OutputKind.WindowsApplication)]
        [InlineData("DynamicallyLinkedLibrary", OutputKind.DynamicallyLinkedLibrary)]
        [InlineData("NetModule", OutputKind.NetModule)]
        [InlineData("WindowsRuntimeMetadata", OutputKind.WindowsRuntimeMetadata)]
        [InlineData("WindowsRuntimeApplication", OutputKind.WindowsRuntimeApplication)]
        public void OutputKind_KnownValueIsMapped(string serialized, OutputKind expected)
        {
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                ("output-kind", serialized));

            Assert.Equal(expected, configuration.CompilationOptions.OutputKind);
        }

        /// <summary>
        /// Rejects unknown and noncanonical output-kind values.
        /// </summary>
        /// <param name="serialized">The invalid value.</param>
        [Theory]
        [InlineData("dynamicallylinkedlibrary")]
        [InlineData("Library")]
        [InlineData("Unknown")]
        public void OutputKind_UnknownValueFailsClosed(string serialized)
        {
            Assert.False(TryCreate(out _, ("output-kind", serialized)));
        }

        /// <summary>
        /// Maps every platform in the referenced Roslyn 5.0.0 API explicitly.
        /// </summary>
        /// <param name="serialized">The exact Portable PDB value.</param>
        /// <param name="expected">The expected Roslyn enum.</param>
        [Theory]
        [InlineData("AnyCpu", Platform.AnyCpu)]
        [InlineData("AnyCpu32BitPreferred", Platform.AnyCpu32BitPreferred)]
        [InlineData("X86", Platform.X86)]
        [InlineData("X64", Platform.X64)]
        [InlineData("Arm", Platform.Arm)]
        [InlineData("Arm64", Platform.Arm64)]
        [InlineData("Itanium", Platform.Itanium)]
        public void Platform_KnownValueIsMapped(string serialized, Platform expected)
        {
            string outputKind = expected == Platform.AnyCpu32BitPreferred
                ? "ConsoleApplication"
                : "DynamicallyLinkedLibrary";
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                ("output-kind", outputKind),
                ("platform", serialized));

            Assert.Equal(expected, configuration.CompilationOptions.Platform);
        }

        /// <summary>
        /// Rejects unknown and noncanonical platform values.
        /// </summary>
        /// <param name="serialized">The invalid value.</param>
        [Theory]
        [InlineData("anycpu")]
        [InlineData("AnyCPU")]
        [InlineData("Unknown")]
        public void Platform_UnknownValueFailsClosed(string serialized)
        {
            Assert.False(TryCreate(out _, ("platform", serialized)));
        }

        /// <summary>
        /// Rejects a known output/platform combination that Roslyn reports as
        /// invalid through the public options diagnostics.
        /// </summary>
        [Fact]
        public void InvalidOutputAndPlatformCombination_FailsClosed()
        {
            Assert.False(TryCreate(
                out _,
                ("output-kind", "DynamicallyLinkedLibrary"),
                ("platform", "AnyCpu32BitPreferred")));
        }

        /// <summary>
        /// Supports the missing and explicit default portability policy.
        /// </summary>
        /// <param name="serialized">The absent or supported value.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("0")]
        public void PortabilityPolicy_DefaultIsSupported(string? serialized)
        {
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                ("portability-policy", serialized));

            Assert.Same(
                AssemblyIdentityComparer.Default,
                configuration.CompilationOptions.AssemblyIdentityComparer);
        }

        /// <summary>
        /// Rejects nondefault, malformed, and out-of-range portability values
        /// that cannot be represented directly by the public API.
        /// </summary>
        /// <param name="serialized">The unsupported value.</param>
        [Theory]
        [InlineData("1")]
        [InlineData("2")]
        [InlineData("3")]
        [InlineData("4")]
        [InlineData("-1")]
        [InlineData(" 0")]
        [InlineData("invalid")]
        public void PortabilityPolicy_UnsupportedValueFailsClosed(string serialized)
        {
            Assert.False(TryCreate(out _, ("portability-policy", serialized)));
        }

        /// <summary>
        /// Preserves either or both encoding web names without resolving them.
        /// </summary>
        /// <param name="defaultEncoding">The optional default web name.</param>
        /// <param name="fallbackEncoding">The optional fallback web name.</param>
        [Theory]
        [InlineData(null, null)]
        [InlineData("iso-8859-1", null)]
        [InlineData(null, "utf-16")]
        [InlineData("historical-default", "historical-fallback")]
        public void EncodingWebNames_ArePreservedWithoutResolution(
            string? defaultEncoding,
            string? fallbackEncoding)
        {
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                ("default-encoding", defaultEncoding),
                ("fallback-encoding", fallbackEncoding));

            Assert.Equal(defaultEncoding, configuration.DefaultEncodingWebName);
            Assert.Equal(fallbackEncoding, configuration.FallbackEncodingWebName);
        }

        /// <summary>
        /// Rejects an explicitly present empty encoding web name.
        /// </summary>
        /// <param name="key">The encoding key to empty.</param>
        [Theory]
        [InlineData("default-encoding")]
        [InlineData("fallback-encoding")]
        public void EmptyEncodingWebName_FailsClosed(string key)
        {
            Assert.False(TryCreate(out _, (key, string.Empty)));
        }

        /// <summary>
        /// Looks up options by key rather than depending on P5A blob order.
        /// </summary>
        [Fact]
        public void ReversedOptionOrder_ReconstructsIdentically()
        {
            ExternalCompilationOptionsDescriptor options = new(
                CreateBaseOptions().Reverse().ToImmutableArray());
            ExternalCompilationProvenanceDescriptor provenance =
                CreateProvenance(options);

            Assert.True(ExternalCSharpCompilationConfigurationFactory.TryCreate(
                provenance,
                out ExternalCSharpCompilationConfiguration configuration));
            Assert.Equal(LanguageVersion.CSharp12, configuration.ParseOptions.LanguageVersion);
            Assert.Equal(1, configuration.SourceFileCount);
        }

        /// <summary>
        /// Reconstructs deliberately nondefault options through the real
        /// P4A/P4B/P5A Portable PDB path.
        /// </summary>
        [Fact]
        public void RealPortablePdb_NondefaultOptionsRoundTripThroughP5AAndP5G()
        {
            CSharpParseOptions originalParseOptions = new(
                LanguageVersion.CSharp12,
                preprocessorSymbols: new[] { "FEATURE_A", "TRACE" });
            CSharpCompilationOptions originalCompilationOptions = new(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                checkOverflow: true,
                allowUnsafe: true,
                platform: Platform.X64,
                nullableContextOptions: NullableContextOptions.Enable,
                deterministic: true);
            P5ATests.PortablePdbTestData testData = P5ATests.EmitPortablePdb(
                "P5GRealOptions",
                "#if FEATURE_A\npublic unsafe sealed class SelectedType { public int* Value; }\n#endif",
                originalParseOptions,
                originalCompilationOptions);
            ExternalCompilationProvenanceDescriptor provenance =
                P5ATests.ReadRequiredDescriptor(testData);
            ExternalCompilationOptionsDescriptor serialized = Assert.IsType<
                ExternalCompilationOptionsDescriptor>(provenance.CompilationOptions);

            Assert.True(ExternalCSharpCompilationConfigurationFactory.TryCreate(
                provenance,
                out ExternalCSharpCompilationConfiguration configuration));
            Assert.Equal(
                originalParseOptions.SpecifiedLanguageVersion,
                configuration.ParseOptions.SpecifiedLanguageVersion);
            Assert.Equal(
                originalParseOptions.PreprocessorSymbolNames,
                configuration.ParseOptions.PreprocessorSymbolNames);
            Assert.Equal(
                originalCompilationOptions.OutputKind,
                configuration.CompilationOptions.OutputKind);
            Assert.Equal(
                originalCompilationOptions.OptimizationLevel,
                configuration.CompilationOptions.OptimizationLevel);
            Assert.Equal(
                originalCompilationOptions.CheckOverflow,
                configuration.CompilationOptions.CheckOverflow);
            Assert.Equal(
                originalCompilationOptions.AllowUnsafe,
                configuration.CompilationOptions.AllowUnsafe);
            Assert.Equal(
                originalCompilationOptions.Platform,
                configuration.CompilationOptions.Platform);
            Assert.Equal(
                originalCompilationOptions.NullableContextOptions,
                configuration.CompilationOptions.NullableContextOptions);
            Assert.False(configuration.CompilationOptions.Deterministic);
            Assert.True(serialized.TryGetValue("compiler-version", out string compilerVersion));
            Assert.True(serialized.TryGetValue("runtime-version", out string runtimeVersion));
            Assert.Equal(compilerVersion, configuration.CompilerVersion);
            Assert.Equal(runtimeVersion, configuration.RuntimeVersion);
            Assert.Equal(1, configuration.SourceFileCount);
        }

        /// <summary>
        /// Verifies real Roslyn Portable PDB strings for representative output
        /// kinds before reconstructing them.
        /// </summary>
        /// <param name="outputKind">The emitted and expected output kind.</param>
        [Theory]
        [InlineData(OutputKind.DynamicallyLinkedLibrary)]
        [InlineData(OutputKind.ConsoleApplication)]
        [InlineData(OutputKind.NetModule)]
        public void RealPortablePdb_OutputKindIsReconstructed(OutputKind outputKind)
        {
            string source = outputKind == OutputKind.ConsoleApplication
                ? "public static class Entry { public static void Main() { } }"
                : "public sealed class LibraryType { }";
            P5ATests.PortablePdbTestData testData = P5ATests.EmitPortablePdb(
                "P5GOutputKind" + outputKind,
                source,
                new CSharpParseOptions(LanguageVersion.CSharp12),
                new CSharpCompilationOptions(outputKind, deterministic: true));

            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                P5ATests.ReadRequiredDescriptor(testData));

            Assert.Equal(outputKind, configuration.CompilationOptions.OutputKind);
        }

        /// <summary>
        /// Verifies real Roslyn Portable PDB strings for representative
        /// platforms before reconstructing them.
        /// </summary>
        /// <param name="platform">The emitted and expected platform.</param>
        [Theory]
        [InlineData(Platform.AnyCpu)]
        [InlineData(Platform.AnyCpu32BitPreferred)]
        [InlineData(Platform.X86)]
        [InlineData(Platform.X64)]
        public void RealPortablePdb_PlatformIsReconstructed(Platform platform)
        {
            OutputKind outputKind = platform == Platform.AnyCpu32BitPreferred
                ? OutputKind.ConsoleApplication
                : OutputKind.DynamicallyLinkedLibrary;
            string source = outputKind == OutputKind.ConsoleApplication
                ? "public static class Entry { public static void Main() { } }"
                : "public sealed class LibraryType { }";
            P5ATests.PortablePdbTestData testData = P5ATests.EmitPortablePdb(
                "P5GPlatform" + platform,
                source,
                new CSharpParseOptions(LanguageVersion.CSharp12),
                new CSharpCompilationOptions(
                    outputKind,
                    platform: platform,
                    deterministic: true));

            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                P5ATests.ReadRequiredDescriptor(testData));

            Assert.Equal(platform, configuration.CompilationOptions.Platform);
        }

        /// <summary>
        /// Confirms through a real emit that default and fallback encoding keys
        /// may coexist and are both preserved exactly.
        /// </summary>
        [Fact]
        public void RealPortablePdb_DefaultAndFallbackEncodingsAreBothPreserved()
        {
            EmitOptions emitOptions = new(
                debugInformationFormat: DebugInformationFormat.PortablePdb);
            emitOptions = emitOptions
                .WithDefaultSourceFileEncoding(Encoding.Latin1)
                .WithFallbackSourceFileEncoding(Encoding.Unicode);
            P5ATests.PortablePdbTestData testData = P5ATests.EmitPortablePdb(
                "P5GEncodings",
                "public sealed class EncodedType { }",
                new CSharpParseOptions(LanguageVersion.CSharp12),
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    deterministic: true),
                emitOptions: emitOptions);

            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                P5ATests.ReadRequiredDescriptor(testData));

            Assert.Equal(Encoding.Latin1.WebName, configuration.DefaultEncodingWebName);
            Assert.Equal(Encoding.Unicode.WebName, configuration.FallbackEncodingWebName);
        }

        /// <summary>
        /// Uses reconstructed symbols to select the intended conditional source.
        /// </summary>
        [Fact]
        public void ParseOptions_DefineSelectsExpectedSourceBranch()
        {
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                ("define", "FEATURE_A"));
            SyntaxTree tree = CSharpSyntaxTree.ParseText(
                "#if FEATURE_A\npublic class SelectedType { }\n#else\npublic class WrongType { }\n#endif",
                configuration.ParseOptions);
            string[] classNames = tree.GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Select(declaration => declaration.Identifier.ValueText)
                .ToArray();

            Assert.Equal(new[] { "SelectedType" }, classNames);
        }

        /// <summary>
        /// Demonstrates that the concrete reconstructed language version
        /// changes language-feature diagnostics without using Latest.
        /// </summary>
        [Fact]
        public void ParseOptions_LanguageVersionControlsFeatureDiagnostics()
        {
            const string source = "public sealed class Sample(int value) { }";
            ExternalCSharpCompilationConfiguration csharp11 = CreateConfiguration(
                ("language-version", "11.0"));
            ExternalCSharpCompilationConfiguration csharp12 = CreateConfiguration(
                ("language-version", "12.0"));
            SyntaxTree oldTree = CSharpSyntaxTree.ParseText(
                source,
                csharp11.ParseOptions);
            SyntaxTree currentTree = CSharpSyntaxTree.ParseText(
                source,
                csharp12.ParseOptions);
            ImmutableArray<Diagnostic> oldDiagnostics = CSharpCompilation.Create(
                "CSharp11FeatureCheck",
                new[] { oldTree },
                MetadataReferences.Default,
                csharp11.CompilationOptions).GetDiagnostics();
            ImmutableArray<Diagnostic> currentDiagnostics = CSharpCompilation.Create(
                "CSharp12FeatureCheck",
                new[] { currentTree },
                MetadataReferences.Default,
                csharp12.CompilationOptions).GetDiagnostics();

            Assert.Contains(
                oldDiagnostics,
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            Assert.DoesNotContain(
                currentDiagnostics,
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }

        /// <summary>
        /// Invokes P5G and requires a successful result.
        /// </summary>
        private static ExternalCSharpCompilationConfiguration CreateConfiguration(
            params (string Key, string? Value)[] changes)
        {
            Assert.True(TryCreate(out ExternalCSharpCompilationConfiguration configuration, changes));
            return configuration;
        }

        /// <summary>
        /// Invokes P5G for already validated P5A provenance.
        /// </summary>
        private static ExternalCSharpCompilationConfiguration CreateConfiguration(
            ExternalCompilationProvenanceDescriptor provenance)
        {
            Assert.True(ExternalCSharpCompilationConfigurationFactory.TryCreate(
                provenance,
                out ExternalCSharpCompilationConfiguration configuration));
            return configuration;
        }

        /// <summary>
        /// Applies keyed replacements or removals to canonical valid options.
        /// </summary>
        private static bool TryCreate(
            out ExternalCSharpCompilationConfiguration configuration,
            params (string Key, string? Value)[] changes)
        {
            List<ExternalCompilationOption> options = CreateBaseOptions().ToList();

            foreach ((string key, string? value) in changes)
            {
                options.RemoveAll(option => option.Key == key);

                if (value != null)
                {
                    options.Add(new ExternalCompilationOption(key, value));
                }
            }

            ExternalCompilationProvenanceDescriptor provenance = CreateProvenance(
                new ExternalCompilationOptionsDescriptor(options.ToImmutableArray()));
            return ExternalCSharpCompilationConfigurationFactory.TryCreate(
                provenance,
                out configuration!);
        }

        /// <summary>
        /// Creates a valid P4A/P4B shell around explicit P5A options.
        /// </summary>
        /// <param name="compilationOptions">The optional compilation options.</param>
        /// <returns>The synthetic but structurally valid provenance.</returns>
        private static ExternalCompilationProvenanceDescriptor CreateProvenance(
            ExternalCompilationOptionsDescriptor? compilationOptions)
        {
            ExternalPeDebugDirectoryDescriptor debugDirectory = new(
                new ExternalModuleIdentity("synthetic.dll", Guid.Empty),
                isDeterministic: false,
                ImmutableArray<ExternalCodeViewPdbReference>.Empty,
                ImmutableArray<System.Reflection.Metadata.BlobContentId>.Empty,
                ImmutableArray<ExternalPdbChecksum>.Empty);
            ExternalPortablePdbDescriptor portablePdb = new(
                default,
                PortablePdbValidationKind.Identity,
                ImmutableArray<ExternalSourceDocumentDescriptor>.Empty,
                sourceLink: null);
            return new ExternalCompilationProvenanceDescriptor(
                debugDirectory,
                portablePdb,
                compilationOptions,
                metadataReferences: null);
        }

        /// <summary>
        /// Creates the minimum canonical Roslyn 5.0.0 option provenance.
        /// </summary>
        private static ImmutableArray<ExternalCompilationOption> CreateBaseOptions()
        {
            return
            [
                new ExternalCompilationOption("version", "2"),
                new ExternalCompilationOption("compiler-version", "compiler-version-exact"),
                new ExternalCompilationOption("language", "C#"),
                new ExternalCompilationOption("source-file-count", "1"),
                new ExternalCompilationOption(
                    "output-kind",
                    "DynamicallyLinkedLibrary"),
                new ExternalCompilationOption("platform", "AnyCpu"),
                new ExternalCompilationOption("runtime-version", "runtime-version-exact"),
                new ExternalCompilationOption("language-version", "12.0")
            ];
        }
    }
}
