using GameLovers.GameData;
using FirstLight.Game.Ids;
using GameLoversEditor.GameData;
using UnityEditor;

namespace FirstLight.Editor.Inspector
{
	/// <summary>
	/// This is the inspector property drawer for the <see cref="GuidId"/>
	/// </summary>
	[CustomPropertyDrawer(typeof(EnumSelector<GuidId>))]
	public class GuidIdEnumSelectorPropertyDrawer : EnumSelectorPropertyDrawer<GuidId>
	{
	}
}
