# G5 exact signed external compilation reconstruction

## Scope and result

G5 reconstructs the exact public assembly identity of a validated, fully signed external target for source-backed semantic analysis. It does not reconstruct a private key, the historical strong-name signature, a signing-key source, or an emitted replacement binary.

The rule is target-derived and library-independent: P4A reads the signing shape from the already validated PE snapshot, P5G applies the complete manifest public key through Roslyn's public `CSharpCompilationOptions.WithCryptoPublicKey` API, P5K composes the compilation, and P5L requires the complete reconstructed `AssemblyIdentity`, public key, and public-key token to equal P3. Unsigned targets retain their prior path. Delay-signed, public-signed, inconsistent, and unreadable shapes fail closed.

## Previous rejection and root cause

There was no library-name check and no direct `if (signed)` rejection. P5G created `CSharpCompilationOptions` without signing data. Roslyn consequently gave the reconstructed compilation an unsigned assembly identity. P5K's existing complete `Compilation.Assembly.Identity == target AssemblyIdentity` postcondition then rejected Newtonsoft.Json, Scrutor, Serilog, and Polly because their original identities contain full public keys.

The path was:

`target PE -> P3 full AssemblyIdentity -> P4A PE/debug provenance -> P5G unsigned options -> P5K unsigned reconstructed identity -> complete identity mismatch -> fail closed`

After G5 it is:

`target PE -> P3 full AssemblyIdentity + P4A signing provenance -> P5G CryptoPublicKey -> P5K exact public identity -> P5L key/token/full-identity validation -> P6A registration`

## Identity, signature, and emit distinction

- Assembly identity includes name, version, culture, complete public key, derived public-key token, retargetability, and content type. P5L compares Roslyn's complete identity rather than treating an equal token as sufficient.
- The complete public key can affect semantic binding. The controlled `InternalsVisibleTo` test grants internal access only to the target with the exact friend public key; missing and different keys are rejected while ordinary public generic binding remains available.
- The public-key token is derived from the complete key and retained as an independent postcondition. It is not used as an approximation for the key.
- `StrongNameSigned` is an observable CLI-header flag. The signature-directory location/size and a SHA-256 digest of its bytes are retained as PE provenance. G5 does not claim to cryptographically verify or reproduce those bytes.
- A private key, `CryptoKeyFile`, `CryptoKeyContainer`, and `StrongNameProvider` are inputs for producing a signed PE. The supported semantic path leaves all of them absent.
- `DelaySign` and `PublicSign` describe emit shapes. Controlled PE tests distinguish them from fully signed and unsigned binaries. They remain unsupported instead of being silently normalized to the fully signed path.
- `CryptoPublicKey` alone causes Roslyn 5.0 to expose the exact full public assembly identity needed by semantic analysis. It requires neither a private key nor a provider and produces no diagnostics in the controlled semantic tests.
- Production G5 never invokes `Emit`. A controlled no-emit P5K/P6A/P6B test proves that the public-key-only compilation can be registered and its source method resolved across compilations.

## Real-world signing matrix

All four targets are culture-neutral, carry a 160-byte full public key, declare manifest `AssemblyFlags.PublicKey`, declare CLI `ILOnly | StrongNameSigned`, and contain a 128-byte nonzero strong-name signature blob. The complete keys, signature digests, reconstructed option values, and before/after identities are emitted to `signing-matrix.json` and `signing-matrix.md` by every evaluation run.

| Target | Assembly identity | Public-key token | Public-key SHA-256 | Signature SHA-256 | P5L |
| --- | --- | --- | --- | --- | --- |
| Newtonsoft.Json 13.0.3 | Newtonsoft.Json 13.0.0.0 | `30ad4fe6b2a6aeed` | `CEB17DFE574356CDA15155E0F302E6861B65CE7FCC3DE81204862E9AFBE6A051` | `251B766CE84BA2BE69BE1BF1B9A3F511DA7CA3F7AE4B76B68651803E78C36F92` | Exact identity reconstructed; emit signing not reconstructed |
| Scrutor 4.2.2 | Scrutor 4.0.0.0 | `167d3117ac9a6821` | `A99C16DB20AB13BF02AA4F0305EEDE97FDE28E4E950295D248CCDC7C103A9243` | `98212131B1FCF1FE1E03EA9F835C5B4A16195D08933ED257B8FD472E0B93D0A8` | Exact identity reconstructed; emit signing not reconstructed |
| Serilog 3.1.1 | Serilog 2.0.0.0 | `24c2f752a8e58a10` | `E67F77933FFB629C8FBC61E4DBD3C14FB433DA2067F0C25DBA607ED7F9DAB203` | `309A3A902FB1EDBB818E7A15FC55B1BF646E1F3487254118357579462B8BF0C2` | Exact identity reconstructed; emit signing not reconstructed |
| Polly 7.2.4 | Polly 7.0.0.0 | `c8a3ffc3f8f825cc` | `529A4490452F86ECDE79DD36805BCA9686E09F64E587120B76940A3E1657F866` | `6FF8975862986DB23B0ACD18ADD132824E5261535EFD9F03619EF8D1E702631F` | Exact identity reconstructed; emit signing not reconstructed |

## Real-world progression

The G5 profile uses `VerifiedLineEndings` and `BoundedRemoteArtifacts`.

| Target | Exact sources | Exact references | G5 result | P6A | P6B/body probe | Finding diff |
| --- | ---: | ---: | --- | --- | --- | --- |
| Newtonsoft.Json | 235/235 (2 direct, 233 reconstructed) | 159/159 (159 remote) | Exact signed compilation identity | Reached | Not exercised; no manifest callable probe | +0 / -0 |
| Scrutor | 38/38 | 124/124 (11 remote) | Exact signed compilation identity | Reached | Not exercised; no manifest callable probe | +0 / -0 |
| Serilog | 147/147 | 118/118 (5 remote) | Exact signed compilation identity | Reached | Not exercised; no manifest callable probe | +0 / -0 |
| Polly | 171/171 (2 direct, 169 reconstructed) | 113/113 local | Exact signed compilation identity | Reached | Not exercised; no manifest callable probe | +0 / -0 |

The false `SourceBodyUsed` stage for these four candidates is not a resolver failure: unlike Semver, their manifest entries intentionally contain no consumer callable probe. A controlled signed P6B regression resolves a metadata method to the exact signed source compilation. No new independent fail-closed boundary was encountered in the four real-world reconstruction pipelines.

Semver remains fully reconstructed and produces its existing one added evaluation-only DOC611 finding. Dapper still fails at `optimization=release-debug-plus`. OneOf still fails because no exact Portable PDB is available. G5 does not alter either boundary.

## Public API and trust boundary

The implementation uses Roslyn 5.0 public APIs only: `CryptoPublicKey`, `CryptoKeyFile`, `CryptoKeyContainer`, `DelaySign`, `PublicSign`, and `StrongNameProvider` are inspected through `CSharpCompilationOptions`. No reflection, internal Roslyn API, runtime patch, diagnostic suppression, private-key search, substitute key, PE patch, or library-specific condition is used.

The official .NET friend-assembly documentation requires a signed friend declaration to carry the full public key, not only the token. The compiler security-option documentation distinguishes full signing, delay signing, public signing, and key sources. These distinctions match the controlled Roslyn and PE tests.

## S1 readiness

G5 supplies exact signed identity reconstruction, existing G3A source acquisition, existing G4B reference acquisition, P5K/P5L fidelity, P6A registration, and controlled signed P6B resolution. The four signed real-world candidates do not themselves exercise P6B because they have no callable probes, so an S1 run must still report whether its actual external call sites resolve and use source bodies. G5 does not execute S1.
