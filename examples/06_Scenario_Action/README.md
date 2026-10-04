# 06 — Scenario Action

Registers an explicit action that adds 5 stability when the condition allows it.

Registration alone does not execute the action. Another tool or addon must deliberately call the public action execution surface. That keeps scenario-style mutations explicit instead of silently running every tick.

Minimum API used: **1.6+**.
