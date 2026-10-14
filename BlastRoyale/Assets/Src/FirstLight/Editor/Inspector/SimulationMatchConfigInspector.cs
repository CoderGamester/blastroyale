using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs;
using Quantum;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.Inspector
{
	/// <summary>Native drawers retain the former config choices and numeric constraints.</summary>
	[CustomPropertyDrawer(typeof(MetaItemDropOverwrite))]
	public class MetaItemDropOverwriteDrawer : NativeConfigDrawer
	{
		private static readonly GameId[] ValidMetaItems =
		{
			GameId.NOOB, GameId.COIN, GameId.BPP, GameId.BlastBuck, GameId.NOOBGolden, GameId.NOOBRainbow, GameId.NOOBSilver,
			GameId.PartnerANCIENT8, GameId.PartnerAPECOIN, GameId.PartnerBEAM, GameId.PartnerBLOCKLORDS,
			GameId.PartnerBLOODLOOP, GameId.PartnerCROSSTHEAGES, GameId.PartnerFARCANA, GameId.PartnerGAM3SGG,
			GameId.PartnerIMMUTABLE, GameId.PartnerMOCAVERSE, GameId.PartnerNYANHEROES, GameId.PartnerPIRATENATION,
			GameId.PartnerPIXELMON, GameId.PartnerPLANETMOJO, GameId.PartnerSEEDIFY, GameId.PartnerWILDERWORLD,
			GameId.PartnerXBORG, GameId.PartnerBREED, GameId.PartnerMEME, GameId.PartnerYGG, GameId.FestiveSNOWFLAKE,
			GameId.EventTicket, GameId.FestiveLUNARCOIN, GameId.FestiveFEATHER, GameId.FestiveLANTERN, GameId.FestiveEGG
		};
		protected override void DrawField(Rect position, SerializedProperty property)
		{
			if (property.name != nameof(MetaItemDropOverwrite.Id)) { base.DrawField(position, property); return; }
			var values = ValidMetaItems.Select(value => (int) value).ToArray();
			var names = ValidMetaItems.Select(value => value.ToString()).ToArray();
			property.intValue = EditorGUI.IntPopup(position, property.displayName, property.intValue, names, values);
		}
	}

	[CustomPropertyDrawer(typeof(SimulationMatchConfig))]
	public class SimulationMatchConfigDrawer : NativeConfigDrawer
	{
		protected override void DrawField(Rect position, SerializedProperty property)
		{
			switch (property.name)
			{
				case nameof(SimulationMatchConfig.GameModeID):
					DrawStringChoice(position, property, GameConfigsLoader.EditorConfigProvider.GetProvider().GetConfigsList<QuantumGameModeConfig>().Select(config => config.Id));
					break;
				case nameof(SimulationMatchConfig.TeamSize):
					property.longValue = EditorGUI.IntSlider(position, property.displayName, (int) property.longValue, 1, 6);
					break;
				case nameof(SimulationMatchConfig.MapId):
					DrawStringChoice(position, property, new[] { GameId.Any.ToString() }.Concat(GameConfigsLoader.EditorConfigProvider.GetProvider().GetConfigsList<QuantumMapConfig>().Select(config => config.Map.ToString())));
					break;
				case nameof(SimulationMatchConfig.Mutators):
					property.intValue = (int) (Mutator) EditorGUI.EnumPopup(position, property.displayName, (Mutator) property.intValue);
					break;
				case nameof(SimulationMatchConfig.BotOverwriteDifficulty):
				{
					var gameMode = property.serializedObject.FindProperty(property.propertyPath.Substring(0, property.propertyPath.LastIndexOf('.') + 1) + nameof(SimulationMatchConfig.GameModeID))?.stringValue;
					var configs = GameConfigsLoader.EditorConfigProvider.LoadFromAddressable<BotConfigs>();
					var values = new[] { -1 }.Concat(configs.Configs.Where(config => config.GameMode == gameMode).Select(config => (int) config.Difficulty)).Distinct().ToArray();
					var names = values.Select(value => value == -1 ? "Automatic" : "Difficulty " + value).ToArray();
					property.intValue = EditorGUI.IntPopup(position, property.displayName, property.intValue, names, values);
					break;
				}
				case nameof(SimulationMatchConfig.MaxPlayersOverwrite):
				{
					var values = Enumerable.Range(0, 49).ToArray();
					property.intValue = EditorGUI.IntPopup(position, property.displayName, property.intValue, values.Select(value => value == 0 ? "Automatic" : value + " Players").ToArray(), values);
					break;
				}
				case nameof(SimulationMatchConfig.UniqueConfigId):
					using (new EditorGUI.DisabledScope(true)) base.DrawField(position, property);
					break;
				default: base.DrawField(position, property); break;
			}
		}

		private static void DrawStringChoice(Rect position, SerializedProperty property, IEnumerable<string> options)
		{
			var values = options.Distinct().ToList();
			// Preserve a remote/custom value rather than silently replacing it when local catalogues differ.
			if (!values.Contains(property.stringValue)) values.Insert(0, property.stringValue);
			var selected = EditorGUI.Popup(position, property.displayName, values.IndexOf(property.stringValue), values.ToArray());
			if (selected >= 0) property.stringValue = values[selected];
		}
	}

	public abstract class NativeConfigDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var height = EditorGUIUtility.singleLineHeight;
			if (property.isExpanded)
				foreach (var child in Children(property)) height += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing;
			return height;
		}
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);
			if (property.isExpanded)
			{
				EditorGUI.indentLevel++;
				foreach (var child in Children(property))
				{
					line.y += line.height + EditorGUIUtility.standardVerticalSpacing;
					line.height = EditorGUI.GetPropertyHeight(child, true);
					DrawField(line, child);
				}
				EditorGUI.indentLevel--;
			}
			EditorGUI.EndProperty();
		}
		protected virtual void DrawField(Rect position, SerializedProperty property) => EditorGUI.PropertyField(position, property, true);
		private static IEnumerable<SerializedProperty> Children(SerializedProperty property)
		{
			var child = property.Copy();
			var end = property.GetEndProperty();
			if (!child.NextVisible(true)) yield break;
			do
			{
				if (SerializedProperty.EqualContents(child, end) || child.depth <= property.depth) yield break;
				yield return child.Copy();
			} while (child.NextVisible(false));
		}
	}
}
