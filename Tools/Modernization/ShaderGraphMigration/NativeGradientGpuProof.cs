using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace FirstLight.Modernization
{
	public static class NativeGradientGpuProof
	{
		public static void Run()
		{
			var evidence = Environment.GetEnvironmentVariable("BLAST_SHADER_EVIDENCE_DIR");
			var halfDiagnostic = Environment.GetEnvironmentVariable("BLAST_SHADER_PROBE_HALF") == "1";
			Require(!string.IsNullOrEmpty(evidence), "Evidence path required.");
			Require(PlayerSettings.colorSpace == ColorSpace.Gamma && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null, "Actual Gamma GPU device required.");
			var rows = new List<Row>();
			foreach (var name in new[] { "SG_Indicator_AimCone", "SG_Indicator_AimLine" })
			{
				var before = File.ReadAllText(Path.Combine(evidence, name + "-before.shader"));
				var after = File.ReadAllText(Path.Combine(evidence, name + "-after.shader"));
				var declaration = Regex.Match(before, @"static Gradient (Gradient_[a-zA-Z0-9_]+) = [^;]+;");
				Require(declaration.Success && after.Contains(declaration.Value), "Authored native gradient declaration must be identical.");
				var nativeFunctions = Function(before, "Unity_SampleGradientV0_float") + "\n" + Function(after, "Unity_SampleGradientV1_float") + "\n" + Function(after, "Unity_ColorspaceConversion_RGB_Linear_float") + "\n" + Function(after, "Unity_Combine_float");
				var source = "Shader \"Hidden/FirstLight/" + name + "NativeGradientProof\" { Properties { _ProofMode (\"Mode\", Float) = 0 } SubShader { Pass { ZTest Always Cull Off ZWrite Off HLSLPROGRAM\n#pragma vertex vert\n#pragma fragment frag\n#pragma target 4.5\n#include \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl\"\n#include \"Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl\"\n#include \"Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl\"\n" + declaration.Value + "\n" + nativeFunctions + "\nfloat _ProofMode;\nstruct Attributes { float4 vertex : POSITION; float2 uv : TEXCOORD0; };\nstruct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };\nVaryings vert(Attributes input) { Varyings output; output.positionCS = TransformObjectToHClip(input.vertex.xyz); output.uv = input.uv; return output; }\nfloat4 frag(Varyings input) : SV_Target { float4 sampled; if (_ProofMode < 0.5) { Unity_SampleGradientV0_float(" + declaration.Groups[1].Value + ", input.uv.x, sampled); return sampled; } Unity_SampleGradientV1_float(" + declaration.Groups[1].Value + ", input.uv.x, sampled); if (_ProofMode < 1.5) return sampled; float3 rgb; Unity_ColorspaceConversion_RGB_Linear_float(sampled.rgb, rgb); float4 rgba; float3 unusedRgb; float2 unusedRg; Unity_Combine_float(rgb.r, rgb.g, rgb.b, sampled.a, rgba, unusedRgb, unusedRg); return rgba; }\nENDHLSL } } }";
				File.WriteAllText(Path.Combine(evidence, name + "-native-gpu-proof.shader"), source);
				// ShaderLab program delimiters occupy their own lines for native source mapping.
				source = source.Replace(" { Properties { _ProofMode (\"Mode\", Float) = 0 } SubShader { Pass { ZTest Always Cull Off ZWrite Off HLSLPROGRAM", "\n{\n Properties { _ProofMode (\"Mode\", Float) = 0 }\n SubShader\n {\n  Pass\n  {\n   Name \"NativeGradientProof\"\n   ZTest Always\n   Cull Off\n   ZWrite Off\n   HLSLPROGRAM");
				source = source.Replace("ENDHLSL } } }", "ENDHLSL\n  }\n }\n}");
				if (halfDiagnostic) source = source.Replace("#include \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl\"", "#define UNITY_UNIFIED_SHADER_PRECISION_MODEL 1\n#include \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl\"");
				File.WriteAllText(Path.Combine(evidence, name + "-native-gpu-proof.shader"), source);
				Directory.CreateDirectory("Assets/Editor/NativeGradientGpuProof");
				var shaderPath = "Assets/Editor/NativeGradientGpuProof/" + name + "Proof.shader";
				File.WriteAllText(shaderPath, source);
				AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
				var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
				Require(shader != null && shader.isSupported, "Native GPU probe shader must import and be supported.");
				var material = new Material(shader);
				try
				{
					var original = Render(material, 0);
					var naive = Render(material, 1);
					var preserved = Render(material, 2);
					var rgbError = 0f;
					var alphaError = 0f;
					var naiveError = 0f;
					for (var i = 0; i < original.Length; i++)
					{
						rgbError = Mathf.Max(rgbError, Mathf.Abs(original[i].r - preserved[i].r), Mathf.Abs(original[i].g - preserved[i].g), Mathf.Abs(original[i].b - preserved[i].b));
						alphaError = Mathf.Max(alphaError, Mathf.Abs(original[i].a - preserved[i].a));
						naiveError = Mathf.Max(naiveError, Mathf.Abs(original[i].r - naive[i].r));
					}
					Require(original.Max(c => c.r) - original.Min(c => c.r) > 0.99f, "Native probe did not sample the full authored gradient.");
					Require(naiveError > 0.1f, "The native probe must detect the uncorrected Gamma behavior change.");
					if (!halfDiagnostic) Require(rgbError < 0.000002f && alphaError == 0, "Preserved native gradient exceeded numeric tolerance or changed alpha.");
					var messages = ShaderUtil.GetShaderMessages(shader);
					File.WriteAllText(Path.Combine(evidence, name + "-native-gpu-compiler-messages.json"), JsonUtility.ToJson(new CompilerReport { Messages = messages.Select(m => new CompilerMessage { Severity = m.severity.ToString(), Message = m.message, File = m.file, Line = m.line }).ToArray(), MaxRgbError = rgbError, MaxAlphaError = alphaError, UncorrectedMaxRgbError = naiveError }, true));
					Require(!messages.Any(m => m.severity == ShaderCompilerMessageSeverity.Error || m.severity == ShaderCompilerMessageSeverity.Warning), "Native GPU probe shader compiler messages remain.");
					rows.Add(new Row { Graph = name, Samples = original.Length, MaxRgbError = rgbError, MaxAlphaError = alphaError, UncorrectedV1MaxRgbError = naiveError, NativeShaderMessages = messages.Length });
				}
				finally { UnityEngine.Object.DestroyImmediate(material); }
			}
			File.WriteAllText(Path.Combine(evidence, halfDiagnostic ? "native-gradient-gpu-half-diagnostic.json" : "native-gradient-gpu-proof.json"), JsonUtility.ToJson(new Report { GraphicsDevice = SystemInfo.graphicsDeviceName, GraphicsApi = SystemInfo.graphicsDeviceType.ToString(), ColorSpace = PlayerSettings.colorSpace.ToString(), HalfPrecisionDiagnostic = halfDiagnostic, Rows = rows.ToArray() }, true));
			Debug.Log(halfDiagnostic ? "Actual native gradient half-precision diagnostic completed; compare recorded errors." : "Actual native gradient GPU proof passed for both authored graphs.");
		}
		private static Color[] Render(Material material, int mode)
		{
			material.SetFloat("_ProofMode", mode);
			var target = new RenderTexture(4096, 1, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
			var texture = new Texture2D(4096, 1, TextureFormat.RGBAFloat, false, true);
			var previous = RenderTexture.active;
			var previousWrite = GL.sRGBWrite;
			try
			{
				target.Create();
				GL.sRGBWrite = false;
				Graphics.Blit(Texture2D.whiteTexture, target, material, 0);
				RenderTexture.active = target;
				texture.ReadPixels(new Rect(0, 0, 4096, 1), 0, 0);
				texture.Apply();
				return texture.GetPixels();
			}
			finally { RenderTexture.active = previous; GL.sRGBWrite = previousWrite; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture); }
		}
		private static string Function(string source, string name)
		{
			var start = source.IndexOf("void " + name + "(", StringComparison.Ordinal);
			Require(start >= 0, "Native generated function missing: " + name);
			var brace = source.IndexOf('{', start);
			var depth = 0;
			for (var end = brace; end < source.Length; end++)
			{
				if (source[end] == '{') depth++;
				if (source[end] == '}' && --depth == 0) return source.Substring(start, end - start + 1);
			}
			throw new InvalidDataException("Unbalanced native generated function.");
		}
		private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
		[Serializable] public sealed class Report { public string GraphicsDevice, GraphicsApi, ColorSpace; public bool HalfPrecisionDiagnostic; public Row[] Rows; }
		[Serializable] public sealed class Row { public string Graph; public int Samples, NativeShaderMessages; public float MaxRgbError, MaxAlphaError, UncorrectedV1MaxRgbError; }
		[Serializable] public sealed class CompilerReport { public CompilerMessage[] Messages; public float MaxRgbError, MaxAlphaError, UncorrectedMaxRgbError; }
		[Serializable] public sealed class CompilerMessage { public string Severity, Message, File; public int Line; }
	}
}
