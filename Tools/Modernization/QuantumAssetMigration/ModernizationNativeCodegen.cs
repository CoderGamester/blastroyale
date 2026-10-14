using Quantum.CodeGen;
using Quantum.Editor;
using UnityEngine;
public static class ModernizationNativeCodegen {
  public static void Run() {
    QuantumCodeGenQtn.Run(verbose: true, options: new GeneratorOptions { LegacyCodeGenOptions = default });
    Debug.Log("[Modernization Native CodeGen] Real SDK generator completed with zero legacy flags");
  }
}
