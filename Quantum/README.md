# Quantum 3 Simulation

In Photon Quantum 3 (`3.0.14 Stable`), the deterministic simulation lives inside the Unity project rather than a split Quantum 2 `.NET Framework` solution:

- **Simulation source & `.qtn` DSL:** `BlastRoyale/Assets/QuantumUser/Simulation/`
- **Native Quantum 3 session, command, math, and replay checksum tests:** `BlastRoyale/Assets/QuantumUser/Tests/EditMode/` (`BlastRoyale.Quantum3.EditMode.Tests`)
- **SDK restoration & code generation instructions:** [`Docs/Modernization/quantum.md`](../Docs/Modernization/quantum.md)

Licensed Photon Quantum and Bot SDK files are installed under `BlastRoyale/Assets/Photon/` and stay gitignored.
