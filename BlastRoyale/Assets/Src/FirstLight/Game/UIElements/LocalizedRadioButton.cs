using FirstLight.Game.Localization;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	[UxmlElement]
	public partial class LocalizedRadioButton : RadioButton
	{
		private string _localizationKey;
		[UxmlAttribute("localization-key")]
		protected string localizationKey
		{
			get => _localizationKey;
			set => Localize(value);
		}

		public LocalizedRadioButton() : this(string.Empty)
		{
		}

		public LocalizedRadioButton(string key)
		{
			Localize(key);
		}

		/// <summary>
		/// Sets the text to the localized string from <paramref name="key"/>.
		/// </summary>
		public void Localize(string key)
		{
			_localizationKey = key;
			label = GameLocalization.TryGetTranslation(key, out var translation) ? translation : $"#{key}#";
		}
	}
}
