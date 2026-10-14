using System;
using FirstLight.Game.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A button that has it's text set from a bundled game translation key.
	/// </summary>
	[UxmlElement]
	public sealed partial class LocalizedButton : ButtonOutlined
	{
		private string _localizationKey;

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

		public LocalizedButton()
		{
		}

		public LocalizedButton(string localizationKey = null, Action action = null) : base(localizationKey, action)
		{
			LocalizationKey = localizationKey;
		}
	}
}
