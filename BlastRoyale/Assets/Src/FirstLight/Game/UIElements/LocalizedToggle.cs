using FirstLight.Game.Localization;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	[UxmlElement]
	public partial class LocalizedToggle : Toggle
	{
		private string _localizationKey;
		[UxmlAttribute("localization-key")]
		protected string localizationKey
		{
			get => _localizationKey;
			set => Localize(value);
		}
		private Label _label;

		public LocalizedToggle() : this(string.Empty)
		{
		}

		public LocalizedToggle(string key)
		{
			_label = new LabelOutlined("##");
			_label.AddToClassList("localized-toggle__label");
			Insert(0, _label);
			Localize(key);
		}

		/// <summary>
		/// Sets the text to the localized string from <paramref name="key"/>.
		/// </summary>
		public void Localize(string key)
		{
			_localizationKey = key;
			_label.text = GameLocalization.TryGetTranslation(key, out var translation) ? translation : $"#{key}#";
		}
	}
}
