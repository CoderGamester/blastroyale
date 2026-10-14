using GameLovers.GameData;
using GameLovers.GoogleSheetImporter;
using System.Collections.Generic;
using FirstLight.Editor.EditorTools;
using GameLoversEditor.GoogleSheetImporter;
using UnityEngine;

namespace FirstLight.Editor.SheetImporters
{
	// Quantum config rows may be reference types. Keep the real package's asset
	// creation/import lifecycle and parser without adopting its struct-only row helper.
	public abstract class GameSheetConfigsImporter<TConfig, TScriptableObject> :
		GoogleSheetScriptableObjectImportContainer<TScriptableObject>
		where TScriptableObject : ScriptableObject, IConfigsContainer<TConfig>
	{
		protected override void OnImport(TScriptableObject scriptableObject, List<Dictionary<string, string>> data)
		{
			var configs = new List<TConfig>(data.Count);
			foreach (var row in data) configs.Add(Deserialize(row));
			scriptableObject.Configs = configs;
		}

		protected virtual TConfig Deserialize(Dictionary<string, string> data) => CsvParser.DeserializeTo<TConfig>(data);
	}

	/// <inheritdoc />
	/// <remarks>
	/// This google sheet importer extends the behaviour to help with quantum data loading
	/// </remarks>
	public abstract class GoogleSheetQuantumConfigsImporter<TConfig, TScriptableObject> : GameSheetConfigsImporter<TConfig, TScriptableObject>
		where TScriptableObject : ScriptableObject, IConfigsContainer<TConfig>
	{
		protected override TConfig Deserialize(Dictionary<string, string> data)
		{
			return QuantumDeserializer.DeserializeTo<TConfig>(data);
		}
	}
}
