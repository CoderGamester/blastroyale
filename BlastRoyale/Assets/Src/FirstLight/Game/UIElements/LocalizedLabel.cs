using System;
using FirstLight.Game.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A label that has it's text set from a bundled game translation key.
	/// </summary>
	[UxmlElement]
	public partial class LocalizedLabel : LabelOutlined
	{
		protected string _localizationKey;

		[UxmlAttribute("localization-key")]
		public string LocalizationKey
		{
			get => _localizationKey;
			set
			{
				_localizationKey = value;
				if (string.IsNullOrWhiteSpace(_localizationKey)) return;
				if (!GameLocalization.TryGetTranslation(_localizationKey, out var translation))
				{
					translation = _localizationKey;
					Debug.LogWarning($"Could not find translation for key {_localizationKey} in element " + name);
				}

				text = translation;
			}
		}

		public LocalizedLabel()
		{
		}

		public LocalizedLabel(string localizationKey) : base($"#{localizationKey}#")
		{
			LocalizationKey = localizationKey;
		}

		/// <summary>
		/// Sets the text to the localized string from <paramref name="key"/>.
		/// </summary>
		public void Localize(string key)
		{
			LocalizationKey = key;
		}
	}
}
