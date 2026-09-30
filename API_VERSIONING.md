# PoliticalWorldAPI versioning

Current public API: **1.19.0**  
Current Political World release: **1.11.0**

PoliticalWorldAPI uses a major/minor compatibility model.

- Major changes are reserved for breaking compatibility.
- Minor releases add public capabilities while preserving existing contracts where practical.
- Addons should declare the minimum API version they actually require.
- Internal Political World classes are not part of the compatibility contract.

For the current surface, see [API 1.19 Reference](docs/en/API_REFERENCE_1_19.md) or the canonical source under `src/PoliticalWorld/API/`.
