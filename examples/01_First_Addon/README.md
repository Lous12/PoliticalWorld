# 01 — First Addon

The smallest useful Political World addon example.

It shows how to:
- check the minimum API version the addon actually needs;
- register an addon with a namespaced ID;
- record that minimum requirement in `AddonDefinition`;
- print the framework diagnostics report.

The example requires API **1.6+** even though the current repository ships API **1.19.0**. That is intentional: this example does not use newer features.
