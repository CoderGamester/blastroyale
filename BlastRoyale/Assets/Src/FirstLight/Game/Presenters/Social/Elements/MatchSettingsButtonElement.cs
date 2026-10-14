using FirstLight.Game.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A button element for the match settings view.
	/// </summary>
	[UxmlElement]
	public partial class MatchSettingsButtonElement : ImageButton
	{
		private const string USS_BLOCK = "match-settings-button";
		private const string USS_TITLE = USS_BLOCK + "__title";
		private const string USS_VALUE = USS_BLOCK + "__value";
		private const string USS_EDIT_ICON = USS_BLOCK + "__edit-icon";

		private const string USS_BUTTON_NUMERICAL = USS_BLOCK + "--numerical";

		[UxmlAttribute("numerical")]
		private bool Numerical
		{
			get => _numerical;
			set
			{
				_numerical = value;
				EnableInClassList(USS_BUTTON_NUMERICAL, _numerical);
				if (!Application.isPlaying && _numerical) _value.text = "1";
			}
		}

		private string _titleLocalizationKey;
		[UxmlAttribute("title-localization-key")]
		private string TitleLocalizationKey
		{
			get => _titleLocalizationKey;
			set
			{
				_titleLocalizationKey = value;
				_title.text = GameLocalization.TryGetTranslation(value, out var translation) ? translation : $"#{value}#";
			}
		}

		private readonly Label _title;
		private readonly Label _value;
		private bool _numerical;

		public MatchSettingsButtonElement()
		{
			AddToClassList(USS_BLOCK);

			Add(_title = new LabelOutlined("MODE") {name = "title"});
			_title.AddToClassList(USS_TITLE);

			Add(_value = new LabelOutlined("BATTLE ROYALE") {name = "value"});
			_value.AddToClassList(USS_VALUE);

			var editIcon = new VisualElement {name = "edit-icon"};
			Add(editIcon);
			editIcon.AddToClassList(USS_EDIT_ICON);
		}

		public void SetValue(string value)
		{
			_value.text = value;
		}
	}
}
