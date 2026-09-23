using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Reconstructs supported C# parse and compilation semantics from P5A
    /// Portable PDB compilation-option provenance.
    /// </summary>
    internal static class ExternalCSharpCompilationConfigurationFactory
    {
        /// <summary>
        /// The Portable PDB compilation-options schema emitted by the
        /// repository's Roslyn version.
        /// </summary>
        private const string SupportedOptionsVersion = "2";

        /// <summary>
        /// Tries to reconstruct supported C# analysis options without reading
        /// the Portable PDB or any source or reference file again.
        /// </summary>
        /// <param name="compilationProvenance">
        /// The P5A provenance containing validated compilation options.
        /// </param>
        /// <param name="configuration">
        /// The reconstructed C# analysis configuration when successful.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when required provenance is present and all
        /// serialized semantics can be represented exactly by the supported
        /// public Roslyn API; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Unknown keys and known but unsupported semantic values fail closed.
        /// The parse-option documentation mode and source-code kind are set to
        /// <see cref="DocumentationMode.Parse"/> and
        /// <see cref="SourceCodeKind.Regular"/> as explicit source-analysis
        /// policies rather than reconstructed original compiler switches.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="compilationProvenance"/> is
        /// <see langword="null"/>.
        /// </exception>
        public static bool TryCreate(
            ExternalCompilationProvenanceDescriptor compilationProvenance,
            out ExternalCSharpCompilationConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(compilationProvenance);

            ExternalCompilationOptionsDescriptor? options =
                compilationProvenance.CompilationOptions;

            if (options == null || ContainsUnknownKey(options))
            {
                configuration = null!;
                return false;
            }

            if (!TryGetRequiredValue(options, "version", out string version)
                || version != SupportedOptionsVersion
                || !TryGetRequiredValue(options, "language", out string language)
                || language != LanguageNames.CSharp
                || !TryGetRequiredNonemptyValue(
                    options,
                    "compiler-version",
                    out string compilerVersion)
                || !TryGetRequiredNonemptyValue(
                    options,
                    "runtime-version",
                    out string runtimeVersion)
                || !TryParseSourceFileCount(options, out int sourceFileCount)
                || !TryParseOutputKind(options, out OutputKind outputKind)
                || !TryParsePlatform(options, out Platform platform)
                || !TryParseLanguageVersion(options, out LanguageVersion languageVersion)
                || !TryParseDefines(options, out ImmutableArray<string> defines)
                || !TryParseBooleanOption(options, "checked", out bool checkOverflow)
                || !TryParseBooleanOption(options, "unsafe", out bool allowUnsafe)
                || !TryParseNullableContext(options, out NullableContextOptions nullable)
                || !TryParseOptimization(options, out OptimizationLevel optimization)
                || !HasSupportedPortabilityPolicy(options)
                || !TryGetOptionalNonemptyValue(
                    options,
                    "default-encoding",
                    out string? defaultEncoding)
                || !TryGetOptionalNonemptyValue(
                    options,
                    "fallback-encoding",
                    out string? fallbackEncoding))
            {
                configuration = null!;
                return false;
            }

            CSharpParseOptions parseOptions = new(
                languageVersion,
                DocumentationMode.Parse,
                SourceCodeKind.Regular,
                defines);
            CSharpCompilationOptions compilationOptions = new(
                outputKind,
                optimizationLevel: optimization,
                checkOverflow: checkOverflow,
                allowUnsafe: allowUnsafe,
                platform: platform,
                assemblyIdentityComparer: AssemblyIdentityComparer.Default,
                nullableContextOptions: nullable);

            if (!TryApplySemanticSigning(
                    compilationProvenance.DebugDirectory.SigningProvenance,
                    compilationOptions,
                    out compilationOptions)
                || !parseOptions.Errors.IsEmpty
                || !compilationOptions.Errors.IsEmpty)
            {
                configuration = null!;
                return false;
            }

            configuration = new ExternalCSharpCompilationConfiguration(
                parseOptions,
                compilationOptions,
                compilationProvenance.DebugDirectory.SigningProvenance,
                compilerVersion,
                runtimeVersion,
                sourceFileCount,
                defaultEncoding,
                fallbackEncoding);
            return true;
        }

        /// <summary>
        /// Reconstructs only the public key needed for semantic assembly identity.
        /// </summary>
        /// <param name="signing">The exact target PE signing provenance.</param>
        /// <param name="options">The otherwise reconstructed compilation options.</param>
        /// <param name="signedOptions">The semantic options when the shape is supported.</param>
        /// <returns>
        /// <see langword="true"/> for unsigned or fully signed targets. Delay-signed,
        /// public-signed, and inconsistent shapes remain unsupported.
        /// </returns>
        private static bool TryApplySemanticSigning(
            ExternalAssemblySigningProvenance signing,
            CSharpCompilationOptions options,
            out CSharpCompilationOptions signedOptions)
        {
            if (!signing.IsSemanticReconstructionSupported)
            {
                signedOptions = null!;
                return false;
            }

            if (signing.State == ExternalAssemblySigningState.Unsigned)
            {
                signedOptions = options;
                return signing.PublicKey.IsEmpty
                    && signing.PublicKeyToken.IsEmpty;
            }

            if (signing.State != ExternalAssemblySigningState.FullySigned)
            {
                signedOptions = null!;
                return false;
            }

            signedOptions = options.WithCryptoPublicKey(signing.PublicKey);
            return signedOptions.CryptoPublicKey.AsSpan().SequenceEqual(
                    signing.PublicKey.AsSpan())
                && signedOptions.CryptoKeyFile == null
                && signedOptions.CryptoKeyContainer == null
                && signedOptions.DelaySign == null
                && !signedOptions.PublicSign
                && signedOptions.StrongNameProvider == null;
        }

        /// <summary>
        /// Determines whether P5A preserved a key outside this package's
        /// explicitly supported Portable PDB schema.
        /// </summary>
        /// <param name="options">The validated P5A option provenance.</param>
        /// <returns>
        /// <see langword="true"/> when any key is unsupported; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool ContainsUnknownKey(ExternalCompilationOptionsDescriptor options)
        {
            foreach (ExternalCompilationOption option in options.Options)
            {
                if (!IsKnownKey(option.Key))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Recognizes only exact case-sensitive Portable PDB option names.
        /// </summary>
        /// <param name="key">The serialized option key.</param>
        /// <returns>
        /// <see langword="true"/> when the key is supported by P5G;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsKnownKey(string key)
        {
            return key is "version"
                or "language"
                or "compiler-version"
                or "runtime-version"
                or "source-file-count"
                or "optimization"
                or "portability-policy"
                or "default-encoding"
                or "fallback-encoding"
                or "output-kind"
                or "platform"
                or "language-version"
                or "define"
                or "checked"
                or "nullable"
                or "unsafe";
        }

        /// <summary>
        /// Gets one required value while preserving empty-string semantics.
        /// </summary>
        /// <param name="options">The validated P5A option provenance.</param>
        /// <param name="key">The exact required key.</param>
        /// <param name="value">The serialized value when present.</param>
        /// <returns>
        /// <see langword="true"/> when the key is present; otherwise
        /// <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        private static bool TryGetRequiredValue(
            ExternalCompilationOptionsDescriptor options,
            string key,
            out string value)
        {
            return options.TryGetValue(key, out value!);
        }

        /// <summary>
        /// Gets one required nonempty provenance value.
        /// </summary>
        /// <param name="options">The validated P5A option provenance.</param>
        /// <param name="key">The exact required key.</param>
        /// <param name="value">The serialized nonempty value when valid.</param>
        /// <returns>
        /// <see langword="true"/> when the key has a nonempty value;
        /// otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        private static bool TryGetRequiredNonemptyValue(
            ExternalCompilationOptionsDescriptor options,
            string key,
            out string value)
        {
            return options.TryGetValue(key, out value!) && value.Length > 0;
        }

        /// <summary>
        /// Gets an optional nonempty web name without resolving an encoding.
        /// </summary>
        /// <param name="options">The validated P5A option provenance.</param>
        /// <param name="key">The exact optional encoding key.</param>
        /// <param name="value">
        /// The serialized web name, or <see langword="null"/> when absent.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the key is absent or nonempty;
        /// otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        private static bool TryGetOptionalNonemptyValue(
            ExternalCompilationOptionsDescriptor options,
            string key,
            out string? value)
        {
            if (!options.TryGetValue(key, out string serializedValue))
            {
                value = null;
                return true;
            }

            value = serializedValue;
            return serializedValue.Length > 0;
        }

        /// <summary>
        /// Parses the required nonnegative invariant source-file count.
        /// </summary>
        /// <param name="options">The validated P5A option provenance.</param>
        /// <param name="sourceFileCount">The parsed count when valid.</param>
        /// <returns>
        /// <see langword="true"/> when the complete value is a nonnegative
        /// invariant integer; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryParseSourceFileCount(
            ExternalCompilationOptionsDescriptor options,
            out int sourceFileCount)
        {
            sourceFileCount = default;

            return options.TryGetValue("source-file-count", out string value)
                && int.TryParse(
                    value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out sourceFileCount)
                && sourceFileCount >= 0;
        }

        /// <summary>
        /// Parses a required stable numeric C# language version.
        /// </summary>
        /// <param name="options">The validated P5A option provenance.</param>
        /// <param name="languageVersion">
        /// The concrete Roslyn language version when supported.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the numeric value is represented by
        /// the current public Roslyn API; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryParseLanguageVersion(
            ExternalCompilationOptionsDescriptor options,
            out LanguageVersion languageVersion)
        {
            if (!options.TryGetValue("language-version", out string value)
                || !IsStableNumericLanguageVersion(value)
                || !LanguageVersionFacts.TryParse(value, out languageVersion)
                || languageVersion is LanguageVersion.Default
                    or LanguageVersion.LatestMajor
                    or LanguageVersion.Preview
                    or LanguageVersion.Latest)
            {
                languageVersion = default;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates the specification's ASCII numeric language-version form.
        /// </summary>
        /// <param name="value">The exact serialized language-version value.</param>
        /// <returns>
        /// <see langword="true"/> for one or more ASCII digits optionally
        /// followed by a decimal point and more digits; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsStableNumericLanguageVersion(string value)
        {
            if (value.Length == 0)
            {
                return false;
            }

            bool hasDecimalPoint = false;
            bool hasDigit = false;
            bool needsDigit = false;

            foreach (char character in value)
            {
                if (character is >= '0' and <= '9')
                {
                    hasDigit = true;
                    needsDigit = false;
                }
                else if (character == '.'
                         && hasDigit
                         && !hasDecimalPoint
                         && !needsDigit)
                {
                    hasDecimalPoint = true;
                    needsDigit = true;
                }
                else
                {
                    return false;
                }
            }

            return hasDigit && !needsDigit;
        }

        /// <summary>
        /// Parses and validates the optional ordered preprocessor-symbol list.
        /// </summary>
        /// <param name="options">The validated P5A option provenance.</param>
        /// <param name="defines">
        /// The symbols in exact serialized order when valid.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every comma-separated item is a valid
        /// C# preprocessor identifier; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryParseDefines(
            ExternalCompilationOptionsDescriptor options,
            out ImmutableArray<string> defines)
        {
            if (!options.TryGetValue("define", out string value) || value.Length == 0)
            {
                defines = ImmutableArray<string>.Empty;
                return true;
            }

            string[] symbols = value.Split(',', StringSplitOptions.None);

            foreach (string symbol in symbols)
            {
                if (!SyntaxFacts.IsValidIdentifier(symbol))
                {
                    defines = default;
                    return false;
                }
            }

            defines = ImmutableArray.CreateRange(symbols);
            return true;
        }

        /// <summary>
        /// Parses an optional canonical Portable PDB Boolean value.
        /// </summary>
        /// <param name="options">The validated P5A option provenance.</param>
        /// <param name="key">The exact Boolean option key.</param>
        /// <param name="result">The parsed value or specified default.</param>
        /// <returns>
        /// <see langword="true"/> when absent or exactly <c>True</c> or
        /// <c>False</c>; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        private static bool TryParseBooleanOption(
            ExternalCompilationOptionsDescriptor options,
            string key,
            out bool result)
        {
            if (!options.TryGetValue(key, out string value) || value == "False")
            {
                result = false;
                return true;
            }

            if (value == "True")
            {
                result = true;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Maps the optional exact nullable-context value.
        /// </summary>
        /// <param name="options">The validated P5A option provenance.</param>
        /// <param name="nullable">
        /// The mapped nullable context or specified default.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when absent or exactly one supported mode;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryParseNullableContext(
            ExternalCompilationOptionsDescriptor options,
            out NullableContextOptions nullable)
        {
            if (!options.TryGetValue("nullable", out string value))
            {
                nullable = NullableContextOptions.Disable;
                return true;
            }

            nullable = value switch
            {
                "Disable" => NullableContextOptions.Disable,
                "Warnings" => NullableContextOptions.Warnings,
                "Annotations" => NullableContextOptions.Annotations,
                "Enable" => NullableContextOptions.Enable,
                _ => default
            };
            return value is "Disable" or "Warnings" or "Annotations" or "Enable";
        }

        /// <summary>
        /// Maps the optional exact optimization value.
        /// </summary>
        /// <param name="options">The validated P5A option provenance.</param>
        /// <param name="optimization">
        /// The mapped optimization level or specified default.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for absent, <c>debug</c>, or
        /// <c>release</c>; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryParseOptimization(
            ExternalCompilationOptionsDescriptor options,
            out OptimizationLevel optimization)
        {
            if (!options.TryGetValue("optimization", out string value) || value == "debug")
            {
                optimization = OptimizationLevel.Debug;
                return true;
            }

            if (value == "release")
            {
                optimization = OptimizationLevel.Release;
                return true;
            }

            optimization = default;
            return false;
        }

        /// <summary>
        /// Accepts only the default portability policy supported exactly by
        /// Roslyn's public API. Policies 1 through 3 fail closed.
        /// </summary>
        /// <param name="options">The validated P5A option provenance.</param>
        /// <returns>
        /// <see langword="true"/> when the policy is absent or exactly zero;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasSupportedPortabilityPolicy(
            ExternalCompilationOptionsDescriptor options)
        {
            return !options.TryGetValue("portability-policy", out string value)
                || value == "0";
        }

        /// <summary>
        /// Maps every exact output-kind value exposed by Roslyn 5.0.0.
        /// </summary>
        /// <param name="options">The validated P5A option provenance.</param>
        /// <param name="outputKind">The mapped output kind when valid.</param>
        /// <returns>
        /// <see langword="true"/> when the required value is supported;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryParseOutputKind(
            ExternalCompilationOptionsDescriptor options,
            out OutputKind outputKind)
        {
            if (!options.TryGetValue("output-kind", out string value))
            {
                outputKind = default;
                return false;
            }

            outputKind = value switch
            {
                "ConsoleApplication" => OutputKind.ConsoleApplication,
                "WindowsApplication" => OutputKind.WindowsApplication,
                "DynamicallyLinkedLibrary" => OutputKind.DynamicallyLinkedLibrary,
                "NetModule" => OutputKind.NetModule,
                "WindowsRuntimeMetadata" => OutputKind.WindowsRuntimeMetadata,
                "WindowsRuntimeApplication" => OutputKind.WindowsRuntimeApplication,
                _ => default
            };
            return value is "ConsoleApplication"
                or "WindowsApplication"
                or "DynamicallyLinkedLibrary"
                or "NetModule"
                or "WindowsRuntimeMetadata"
                or "WindowsRuntimeApplication";
        }

        /// <summary>
        /// Maps every exact platform value exposed by Roslyn 5.0.0.
        /// </summary>
        /// <param name="options">The validated P5A option provenance.</param>
        /// <param name="platform">The mapped platform when valid.</param>
        /// <returns>
        /// <see langword="true"/> when the required value is supported;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryParsePlatform(
            ExternalCompilationOptionsDescriptor options,
            out Platform platform)
        {
            if (!options.TryGetValue("platform", out string value))
            {
                platform = default;
                return false;
            }

            platform = value switch
            {
                "AnyCpu" => Platform.AnyCpu,
                "AnyCpu32BitPreferred" => Platform.AnyCpu32BitPreferred,
                "X86" => Platform.X86,
                "X64" => Platform.X64,
                "Arm" => Platform.Arm,
                "Arm64" => Platform.Arm64,
                "Itanium" => Platform.Itanium,
                _ => default
            };
            return value is "AnyCpu"
                or "AnyCpu32BitPreferred"
                or "X86"
                or "X64"
                or "Arm"
                or "Arm64"
                or "Itanium";
        }
    }
}
