# A10 Phase 0: protected Core obj drift (S1)

Decision: **S1 — benign generated drift**. This is a content attribution, NOT
an identification of the writing process. No manifest, protected file, index,
stash, source, or test is repaired, regenerated, or accepted as a new baseline.
The earlier protection verifier's exit 1 remains historically correct.

## Exact targets

Both files belong to
`D:\Repository\SEE\Tools\XMLDocNormalizer\src\XMLDocNormalizer.ExceptionFlow.Core\XMLDocNormalizer.ExceptionFlow.Core.csproj`,
configuration Debug, target framework `net8.0`.

| Absolute path | Size | LastWriteTimeUtc | Manifest SHA256 | Current SHA256 |
|---|---:|---|---|---|
| `D:\Repository\SEE\Tools\XMLDocNormalizer\src\XMLDocNormalizer.ExceptionFlow.Core\obj\Debug\net8.0\XMLDocNormalizer.ExceptionFlow.Core.AssemblyInfo.cs` | 1347 | 2026-10-23T07:16:17.3607204Z | `E947C48320ED008527522DDB32FB1B7C907525F5BD7DF26EDF36D4C9F850FB42` | `A6B602AD3DABBB3B7AD41B26C8B73CE2C65E8EE2D09DB60DBFF811440F1647E8` |
| `D:\Repository\SEE\Tools\XMLDocNormalizer\src\XMLDocNormalizer.ExceptionFlow.Core\obj\Debug\net8.0\XMLDocNormalizer.ExceptionFlow.Core.AssemblyInfoInputs.cache` | 66 | 2026-10-23T07:16:17.3515708Z | `4CC4AB60E2764F626C38F891FAF436A7DCBAB491FF192E32AE533693CA7FD500` | `1284572B6EEED1C6B6668BC02AF762B4DE5040B2D9F343AC90E54F4529B32BCD` |

`git ls-files -- <both paths>` is empty: neither is tracked.
`git check-ignore -v --no-index` identifies the Core-local `.gitignore:3:obj/`
for both. They are generated files, not missing tracked production sources.

## Byte-exact explanation (no build or regeneration)

The previous file is reconstructed **in memory**, not assumed from timestamps:
replace only the 40-character commit ID in the current UTF-8/CRLF text.
The resulting SHA256 equals the frozen manifest SHA256 exactly. Therefore the
entire textual difference, including encoding, line endings, and all other
attributes, is:

```diff
-[assembly: System.Reflection.AssemblyInformationalVersionAttribute("1.0.0+c777f9642c145a57c112c28216c3996128c6234b")]
+[assembly: System.Reflection.AssemblyInformationalVersionAttribute("1.0.0+9b76e179a568c64ae972cc3a45980a094e44245e")]
```

The cache is **text**, not an opaque binary: 64 lowercase hex characters plus
CRLF (66 bytes). The independent calculation below reproduces BOTH complete
cache-file hashes, not merely the current visible cache value:

| Informational revision | Attribute digest / cache text (before CRLF) | Complete cache SHA256 |
|---|---|---|
| c777f9642c145a57c112c28216c3996128c6234b | `f40ef25c50d27936df56517689e709b511384c5a9e5b09769bc08911af30537c` | `4CC4AB60E2764F626C38F891FAF436A7DCBAB491FF192E32AE533693CA7FD500` |
| 9b76e179a568c64ae972cc3a45980a094e44245e | `ad95bedee2d8522dcf60ff999589c86bf0b974a05de1699f1d9e4dfc86294fe8` | `1284572B6EEED1C6B6668BC02AF762B4DE5040B2D9F343AC90E54F4529B32BCD` |

Calculation: in original order, concatenate each of the ten attribute type
names with its first argument; terminate **every** item with U+2028; compute
SHA256 over UTF-8; lowercase hex plus CRLF is the cache file. No hash task,
MSBuild target, project evaluation, compiler, or existing test is executed to
produce these files. The algorithm's encoding/separator were inspected from
the installed MSBuild Hash type; the content calculation uses only in-memory
strings and SHA256. Two exploratory attempts to execute that task in Windows
PowerShell failed on its System.Buffers dependency; they yielded no usable
hash and are NOT proof. The subsequent independent calculations above match.

The installed SDK primary implementation is
`C:\Program Files\dotnet\sdk\8.0.418\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.GenerateAssemblyInfo.targets`:

- lines 62–72: `AddSourceRevisionToInformationalVersion` appends SourceRevisionId;
- lines 171–182: `CreateGeneratedAssemblyInfoInputsCacheFile` hashes ordered
  AssemblyAttribute identity/parameters and writes only when different;
- lines 188–204: `CoreGenerateAssemblyInfo` consumes that cache and uses
  `WriteCodeFragment` to generate AssemblyInfo.cs.

Normal producer: SDK/MSBuild assembly-info generation, invoked by an ordinary
build or an applicable IDE design-time build. VBCSCompiler consumes compiler
inputs; this is not evidence that VBCSCompiler itself writes SDK AssemblyInfo.

The `.cs` file is generated **assembly metadata** and subsequently a compiler
input; the cache is an incremental **generation input digest**. Neither change
alters authored analyzer code, tests, A9 evidence, project properties, friend
assemblies, AssemblyVersion, or AssemblyFileVersion. The metadata revision is
observable if rebuilt, so this is not a claim of byte-identical future DLLs.
The other 21 Core-manifest entries, including existing DLL/PDB outputs, retain
their original hashes; the 1,092 frozen original files and the 340-file test
inventory also remain unchanged. No new Core compilation is inferred.

## Tooling timeline and attribution limit

Read-only logs under
`C:\Users\Krause\AppData\Roaming\Code\logs\20261023T091548\window1\exthost` show:

- `ms-dotnettools.csdevkit/C# Dev Kit.log`: solution opening 09:16:06.452,
  SDK 8.0.418 selected, automatic solution restore 09:16:13.111–09:16:14.797;
- `ms-dotnettools.csharp/C#.log:20`: Core project loaded by C# Dev Kit at
  **09:16:16.961 CEST**;
- cache write **09:16:17.3515708 CEST**, then generated `.cs` write
  **09:16:17.3607204 CEST**.

This order and content are compatible with IDE/MSBuild assembly-info
generation after HEAD changed. The logs do not name the writer of either file.
The user-reported BuildHost PID 36508 and VBCSCompiler PID 16512 are historical
context; a read-only Get-Process lookup found no current instance for either.
No file-writer trace, crash dump, or correlation to a specific 0xe0434352 dialog
was obtained. WER/ProcDump absence does not establish a cause. No process,
service, registry setting, debugger, or dump configuration was changed.

## Protection handling, without rewriting history

Retain the historical manifest and exit-1 record verbatim. The two raw hash
mismatches remain two mismatches; S1 explains them rather than erasing them.
For a future separately approved protection-policy revision, distinguish frozen
authored inputs/evidence from explicitly inventoried generated metadata. Record
old and new hashes plus the permitted generated attribute delta and producer
rule; fail on any other delta or unexplained generated drift. Never exclude
all Core files, silently refresh a manifest, or treat arbitrary obj changes as
benign. **No such policy or manifest change is made in this run.**
