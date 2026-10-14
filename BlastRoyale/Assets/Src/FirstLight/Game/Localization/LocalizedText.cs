using TMPro;
using UnityEngine;

namespace FirstLight.Game.Localization
{
	/// <summary>Localizes the legacy TextMesh Pro prefab text when enabled or language changes.</summary>
	[RequireComponent(typeof(TMP_Text))]
	public sealed class LocalizedText : MonoBehaviour
	{
		[SerializeField] private string _term;
		[SerializeField] private bool _uppercase;

		private void OnEnable()
		{
			GameLocalization.LanguageChanged += Refresh;
			Refresh();
		}

		private void OnDisable() => GameLocalization.LanguageChanged -= Refresh;

		private void Refresh()
		{
			var text = GameLocalization.GetTranslation(_term);
			GetComponent<TMP_Text>().text = _uppercase ? text.ToUpperInvariant() : text;
		}
	}
}
