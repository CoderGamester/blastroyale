using System.Linq;
using FirstLight.Editor.EditorTools;
using FirstLight.Game.Utils.Attributes;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.Inspector
{
	[CustomPropertyDrawer(typeof(SpriteClassAttribute))]
	public class SpriteClassPropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			StringPropertyPicker.Draw(position, property, label, "Generated Sprite Classes",
				EditorShortcuts.GetAllGeneratedClassNames().OrderBy(pair => pair.Key));
		}
	}
}
