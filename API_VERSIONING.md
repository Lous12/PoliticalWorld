# PoliticalWorldAPI versioning

Current public API: **1.19.0**  
Current Political World release: **1.11.0**

PoliticalWorldAPI uses a major/minor compatibility model.

- Major changes are reserved for breaking compatibility.
- Minor releases add public capabilities while preserving existing contracts where practical.
- Addons should declare the **minimum API version they actually require**, not automatically the newest version.
- Internal Political World classes are not part of the compatibility contract.

## Minimum version does not mean current version

An addon that only uses contracts available since API 1.6 can legitimately keep:

```csharp
PoliticalWorldAPI.IsCompatible(1, 6)
```

while running on PoliticalWorldAPI 1.19.0.

Do not mass-replace old minimum requirements just because the current API number is higher. Raise the minimum only when the addon starts using a newer contract.

For newer addons, also record requirements in `AddonDefinition`:

```csharp
RequiredApiMajor = 1,
RequiredApiMinor = 19,
RequiredCapabilities = new[] { "framework.requirements-check" }
```

Capability requirements are useful when an addon depends on a specific surface rather than only a version number.

## Public contract vs implementation

The compatibility contract is the public `PoliticalWorldAPI` surface. `Main`, `ScenarioBridge`, Harmony patches, UI internals and runtime caches may change without preserving private behavior.

If an addon needs a missing internal capability, prefer a narrow Public API addition over reflection into core implementation.

For the current surface, see [API 1.19 Reference](docs/en/API_REFERENCE_1_19.md), `src/PoliticalWorld/API/README.md`, or the canonical source under `src/PoliticalWorld/API/`.
