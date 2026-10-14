using System.Collections.Generic;
using FirstLight.Game.Localization;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A button that has it's text set from a bundled game translation key.
	/// </summary>
	[UxmlElement]
	public partial class LocalizedSliderInt : SliderInt
	{
		private string _localizationKey;
		[UxmlAttribute("localization-key")]
		protected string localizationKey
		{
			get => _localizationKey;
			set => Localize(value);
		}

		public LocalizedSliderInt() : this(string.Empty)
		{
		}

		public LocalizedSliderInt(string key)
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
