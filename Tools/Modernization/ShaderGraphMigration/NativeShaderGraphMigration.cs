using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace FirstLight.Modernization
{
	// Compiled with the installed Unity.ShaderGraph.Editor assembly through an asmref.
	// All graph reads, upgrades, connections, serialization and shader generation use native APIs.
	public static class NativeShaderGraphMigration
	{
		public static void ValidateSaved()
		{
			var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Environment.GetEnvironmentVariable("BLAST_SHADER_MIGRATION_MANIFEST")));
			var evidence = Environment.GetEnvironmentVariable("BLAST_SHADER_EVIDENCE_DIR");
			var rows = new List<GraphReport>();
			foreach (var spec in manifest.Graphs)
			{
				Require(!string.IsNullOrEmpty(spec.AfterSha256) && Hash(spec.Path) == spec.AfterSha256, "Saved native graph hash changed.");
				Require(Hash(spec.Path + ".meta") == spec.MetaSha256 && AssetDatabase.AssetPathToGUID(spec.Path) == spec.Guid, "Saved native metadata changed.");
				AssetDatabase.ImportAsset(spec.Path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
				var generated = ShaderGraphImporter.GetShaderText(spec.Path, out _, new AssetCollection(), out var graph);
				var warnings = graph.messageManager.ErrorStrings(severity: ShaderCompilerMessageSeverity.Warning).ToArray();
				var errors = graph.messageManager.ErrorStrings().ToArray();
				Require(warnings.Length == 0 && errors.Length == 0, "Saved graph has native validation messages.");
				Require(graph.GetNodes<ViewDirectionNode>().All(n => n.sgVersion == n.latestVersion) && graph.GetNodes<SampleGradient>().All(n => n.sgVersion == n.latestVersion), "Saved graph contains deprecated target nodes.");
				Require(generated == File.ReadAllText(Path.Combine(evidence, Path.GetFileNameWithoutExtension(spec.Path) + "-after.shader")), "Saved native graph generated shader changed after restart.");
				var messages = ShaderUtil.GetShaderMessages(AssetDatabase.LoadAssetAtPath<Shader>(spec.Path));
				Require(messages.Length == 0, "Saved native shader has compiler messages.");
				rows.Add(new GraphReport { Path = spec.Path, AfterSha256 = spec.AfterSha256, MetaSha256 = spec.MetaSha256, Guid = spec.Guid, WarningsAfter = warnings, ErrorsAfter = errors, NativeShaderMessages = messages.Length, GeneratedShaderEqual = true });
			}
			File.WriteAllText(Path.Combine(evidence, "native-shadergraph-postrestart.json"), JsonUtility.ToJson(new Report { Graphs = rows.ToArray() }, true));
			Debug.Log("Native ShaderGraph post-restart import/generation proof passed for all five graphs.");
		}

		public static void Apply()
		{
			var manifestPath = Environment.GetEnvironmentVariable("BLAST_SHADER_MIGRATION_MANIFEST");
			var evidence = Environment.GetEnvironmentVariable("BLAST_SHADER_EVIDENCE_DIR");
			Require(!string.IsNullOrEmpty(manifestPath) && !string.IsNullOrEmpty(evidence), "Manifest and evidence paths are required.");
			Require(UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().Single(p => p.name == "com.unity.shadergraph").version == "17.3.0", "This native migration targets ShaderGraph 17.3.0.");
			Require(PlayerSettings.colorSpace == ColorSpace.Gamma, "Original game Gamma color space is required.");
			Directory.CreateDirectory(evidence);
			var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
			foreach (var spec in manifest.Graphs)
			{
				Require(Hash(spec.Path) == spec.BeforeSha256, "Graph input hash changed: " + spec.Path);
				Require(Hash(spec.Path + ".meta") == spec.MetaSha256, "Graph metadata hash changed: " + spec.Path);
				Require(AssetDatabase.AssetPathToGUID(spec.Path) == spec.Guid, "Graph GUID changed: " + spec.Path);
			}
			var reports = new List<GraphReport>();
			foreach (var spec in manifest.Graphs)
			{
				var baseline = ShaderGraphImporter.GetShaderText(spec.Path, out _, new AssetCollection(), out var graph);
				Require(!baseline.Contains("Hidden/GraphErrorShader"), "Original graph failed native shader generation.");
				Require(!graph.messageManager.ErrorStrings().Any() && graph.GetNodes<SubGraphNode>().All(n => n.asset != null), "Original graph or authored subgraph dependency failed native validation.");
				var originalNodeIds = graph.GetNodes<AbstractMaterialNode>().Select(n => n.objectId).OrderBy(x => x).ToArray();
				var originalPropertyJson = graph.properties.Select(p => UnityEditor.ShaderGraph.Serialization.MultiJson.Serialize(p)).ToArray();
				var warningsBefore = graph.messageManager.ErrorStrings(severity: ShaderCompilerMessageSeverity.Warning).ToArray();
				File.WriteAllText(Path.Combine(evidence, Path.GetFileNameWithoutExtension(spec.Path) + "-before.shader"), baseline);
				var added = new List<string>();
				if (spec.NodeType == "ViewDirection")
				{
					var view = graph.GetNodes<ViewDirectionNode>().Single();
					Require(view.sgVersion == 0 && view.latestVersion == 1, "Expected original ViewDirection version.");
					view.ChangeVersion(view.latestVersion);
					view.UpdateNodeAfterDeserialization();
				}
				else if (spec.NodeType == "SampleGradient")
				{
					var sample = graph.GetNodes<SampleGradient>().Single();
					Require(sample.sgVersion == 0 && sample.latestVersion == 1, "Expected original SampleGradient version.");
					Require(sample.concretePrecision == ConcretePrecision.Single, "Author gradient uses float precision.");
					Require(graph.properties.OfType<GradientShaderProperty>().All(p => p.value.mode == GradientMode.Blend || p.value.mode == GradientMode.Fixed), "Original legacy gradient mode must be Blend or Fixed.");
					var outgoing = graph.GetEdges(sample.GetSlotReference(2)).Where(e => e.outputSlot.node == sample).Select(e => e.inputSlot).ToArray();
					Require(outgoing.Length > 0, "Original gradient output has no consumers.");
					sample.ChangeVersion(sample.latestVersion);
					sample.UpdateNodeAfterDeserialization();
					var conversion = Add(graph, sample, new ColorspaceConversionNode { conversion = new ColorspaceConversion(Colorspace.RGB, Colorspace.Linear) }, "Legacy Gradient RGB", 300, 0);
					var rgb = Add(graph, sample, new SplitNode(), "Preserved Gradient RGB", 580, 0);
					var alpha = Add(graph, sample, new SplitNode(), "Original Gradient Alpha", 300, 240);
					var combine = Add(graph, sample, new CombineNode(), "Preserved Gradient RGBA", 850, 0);
					foreach (var edge in graph.GetEdges(sample.GetSlotReference(2)).Where(e => e.outputSlot.node == sample).ToArray()) graph.RemoveEdge(edge);
					graph.Connect(sample.GetSlotReference(2), conversion.GetSlotReference(0));
					graph.Connect(conversion.GetSlotReference(1), rgb.GetSlotReference(0));
					graph.Connect(sample.GetSlotReference(2), alpha.GetSlotReference(0));
					for (var channel = 0; channel < 3; channel++) graph.Connect(rgb.GetSlotReference(channel + 1), combine.GetSlotReference(channel));
					graph.Connect(alpha.GetSlotReference(4), combine.GetSlotReference(3));
					foreach (var destination in outgoing) graph.Connect(combine.GetSlotReference(4), destination);
					added.AddRange(new[] { conversion.objectId, rgb.objectId, alpha.objectId, combine.objectId });
				}
				else throw new InvalidDataException("Unexpected node migration.");
				graph.ValidateGraph();
				var generated = ShaderGraphImporter.GetShaderText(spec.Path, out _, new AssetCollection(), graph);
				var warningsAfter = graph.messageManager.ErrorStrings(severity: ShaderCompilerMessageSeverity.Warning).ToArray();
				var errorsAfter = graph.messageManager.ErrorStrings().ToArray();
				Require(warningsAfter.Length == 0 && errorsAfter.Length == 0, "Native graph validation messages remain: " + spec.Path);
				Require(originalNodeIds.All(id => graph.GetNodeFromId(id) != null), "An original node was removed.");
				Require(originalPropertyJson.SequenceEqual(graph.properties.Select(p => UnityEditor.ShaderGraph.Serialization.MultiJson.Serialize(p))), "An authored shader property changed.");
				Require(spec.NodeType != "ViewDirection" || baseline == generated, "ViewDirection generated code changed.");
				File.WriteAllText(Path.Combine(evidence, Path.GetFileNameWithoutExtension(spec.Path) + "-after.shader"), generated);
				Require(FileUtilities.WriteShaderGraphToDisk(spec.Path, graph) != null, "Native graph serialization failed.");
				AssetDatabase.ImportAsset(spec.Path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
				var shader = AssetDatabase.LoadAssetAtPath<Shader>(spec.Path);
				Require(shader != null, "Native imported shader is missing.");
				var shaderMessages = ShaderUtil.GetShaderMessages(shader);
				Require(!shaderMessages.Any(m => m.severity == ShaderCompilerMessageSeverity.Error || m.severity == ShaderCompilerMessageSeverity.Warning), "Native shader compiler messages remain.");
				Require(Hash(spec.Path + ".meta") == spec.MetaSha256 && AssetDatabase.AssetPathToGUID(spec.Path) == spec.Guid, "Native save changed graph metadata.");
				reports.Add(new GraphReport { Path = spec.Path, BeforeSha256 = spec.BeforeSha256, AfterSha256 = Hash(spec.Path), MetaSha256 = spec.MetaSha256, Guid = spec.Guid, NodeType = spec.NodeType, OriginalNodesRetained = originalNodeIds.Length, AddedNodeIds = added.ToArray(), WarningsBefore = warningsBefore, WarningsAfter = warningsAfter, ErrorsAfter = errorsAfter, GeneratedShaderEqual = baseline == generated, NativeShaderMessages = shaderMessages.Length });
			}
			File.WriteAllText(Path.Combine(evidence, "native-shadergraph-migration.json"), JsonUtility.ToJson(new Report { Graphs = reports.ToArray() }, true));
			Debug.Log("Native ShaderGraph migration passed: five graphs, unchanged metadata, zero graph/shader messages.");
		}

		private static T Add<T>(GraphData graph, SampleGradient sample, T node, string role, float x, float y) where T : AbstractMaterialNode
		{
			node.name = role;
			node.precision = Precision.Single;
			node.group = sample.group;
			node.OverrideObjectId(graph.assetGuid, sample.objectId + ":" + role);
			var draw = node.drawState;
			draw.position = new Rect(sample.drawState.position.x + x, sample.drawState.position.y + y, 210, 150);
			node.drawState = draw;
			graph.AddNode(node);
			var slots = new List<MaterialSlot>();
			node.GetSlots(slots);
			foreach (var slot in slots) slot.OverrideObjectId(graph.assetGuid, node.objectId + ":slot:" + slot.id);
			return node;
		}
		private static string Hash(string path) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
		private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
		[Serializable] public sealed class Manifest { public GraphSpec[] Graphs; }
		[Serializable] public sealed class GraphSpec { public string Path, BeforeSha256, AfterSha256, MetaSha256, Guid, NodeType; }
		[Serializable] public sealed class Report { public GraphReport[] Graphs; }
		[Serializable] public sealed class GraphReport { public string Path, BeforeSha256, AfterSha256, MetaSha256, Guid, NodeType; public int OriginalNodesRetained, NativeShaderMessages; public string[] AddedNodeIds, WarningsBefore, WarningsAfter, ErrorsAfter; public bool GeneratedShaderEqual; }
	}
}
