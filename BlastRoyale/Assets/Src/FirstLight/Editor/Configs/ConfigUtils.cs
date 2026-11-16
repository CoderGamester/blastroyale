using System;
using System.Collections.Generic;
using GameLoversEditor.GoogleSheetImporter;
using UnityEditor;

namespace FirstLight.Editor.Configs
{
	/// <summary>
	/// Helper utils for Configs / importing.
	/// </summary>
	public static class ConfigUtils
	{
		/// <summary>
		/// Returns a dictionary of all importers and the types they import.
		/// </summary>
		public static Dictionary<Type, IGoogleSheetConfigsImporter> GetAllImporters()
		{
			var soImporterInterface = typeof(IScriptableObjectImporter);
			var importers = new Dictionary<Type, IGoogleSheetConfigsImporter>();

			foreach (var type in TypeCache.GetTypesDerivedFrom<IGoogleSheetConfigsImporter>())
			{
				if (!type.IsAbstract && !type.IsInterface && soImporterInterface.IsAssignableFrom(type))
				{
					var importer = Activator.CreateInstance(type) as IGoogleSheetConfigsImporter;
					var soType = ((IScriptableObjectImporter) importer)!.ScriptableObjectType;

					importers.Add(soType, Activator.CreateInstance(type) as IGoogleSheetConfigsImporter);
				}
			}

			return importers;
		}
	}
}
