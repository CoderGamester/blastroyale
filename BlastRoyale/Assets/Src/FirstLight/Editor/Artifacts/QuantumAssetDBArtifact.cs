using Quantum.Editor;
using UnityEngine;
using System.IO;

namespace FirstLight.Editor.Artifacts
{
	public class QuantumAssetDBArtifact : IArtifact
	{
		public void CopyTo(string directory)
		{
			QuantumUnityDBUtilities.ExportAsJson(Path.Combine(directory, "assetDatabase.json"));
			Debug.Log("Exported Quantum asset database");
		}
	}
}
