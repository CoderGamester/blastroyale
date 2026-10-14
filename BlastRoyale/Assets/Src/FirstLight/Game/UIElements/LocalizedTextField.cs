using System;
using FirstLight.Game.Utils;
using FirstLight.Game.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A button that has it's text set from a bundled game translation key.
	/// </summary>
	[UxmlElement]
	public partial class LocalizedTextField : TextField
	{
		private string _labelLocalizationKey;
		[UxmlAttribute("label-localization-key")]
		private string labelLocalizationKey
		{
			get => _labelLocalizationKey;
			set => LocalizeLabel(value);
		}
		private bool _showCopyButton;
		[UxmlAttribute("show-copy-button")]
		private bool showCopyButton
		{
			get => _showCopyButton;
			set => EnableCopyButton(value);
		}
		public event Action OnCopied;

		private ImageButton _copyButton;

		public LocalizedTextField() : this(string.Empty)
		{
		}

		public LocalizedTextField(string labelKey)
		{
			LocalizeLabel(labelKey);
		}

		/// <summary>
		/// Sets the label text to the localized string from <paramref name="key"/>.
		/// </summary>
		public void LocalizeLabel(string labelKey)
		{
			_labelLocalizationKey = labelKey;
			label = string.IsNullOrEmpty(labelKey) ? string.Empty :
				GameLocalization.TryGetTranslation(labelKey, out var translation) ? translation : $"#{labelKey}#";
		}

		/// <summary>
		/// Enables or disables the "copy to clipboard" button.
		/// </summary>
		public void EnableCopyButton(bool enable)
		{
			_showCopyButton = enable;
			if (enable && _copyButton == null)
			{
				_copyButton = new ImageButton {name = "copy-button"};
				Add(_copyButton);
				_copyButton.AddToClassList("unity-text-field__copy-button");

				_copyButton.clicked += () =>
				{
					UIUtils.SaveToClipboard(value);
					OnCopied?.Invoke();
				};
			}
			else if (!enable && _copyButton != null)
			{
				_copyButton.RemoveFromHierarchy();
				_copyButton = null;
			}
		}
	}
}
