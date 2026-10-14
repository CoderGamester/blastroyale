// Compile-only collaborators for this isolated assembly, never imported into Unity.
// Game controls and their generated UxmlSerializedData are the actual linked source.
using UnityEngine.UIElements;
namespace FirstLight.Game.Utils
{
 public static class CompileHelpers
 {
  public static T AddClass<T>(this T element, string name) where T : VisualElement { element.AddToClassList(name); return element; }
  public static void SetDisplay(this VisualElement element, bool display) => element.style.display = display ? DisplayStyle.Flex : DisplayStyle.None;
  public static string LocalizeKey(this string key) => FirstLight.Game.Localization.GameLocalization.GetTranslation(key);
 }
 public static class UIUtils { public static void SaveToClipboard(string value) => UnityEngine.GUIUtility.systemCopyBuffer = value; }
}
namespace FirstLight.Game.UI { public static class UIService { public const string SFX_CLICK_BACKWARDS = "click-backwards"; } }
namespace Photon.Realtime { }
