using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// An element to show a map / gun / mutator...
	/// </summary>
	[UxmlElement]
	public partial class MatchSettingsSelectionElement : ImageButton
	{
		private const string USS_BLOCK = "match-settings-selection";
		private const string USS_TITLE = USS_BLOCK + "__title";
		private const string USS_SUBTITLE = USS_BLOCK + "__subtitle";
		private const string USS_TEXT_CONTAINER = USS_BLOCK + "__text-container";
		private const string USS_IMAGE = USS_BLOCK + "__image";

		private string _titleLocalizationKey;
		[UxmlAttribute("title-localization-key")]
		public string titleLocalizationKey
		{
			get => _titleLocalizationKey;
			set { _titleLocalizationKey = value; _title.Localize(value); }
		}
		private string _subtitleLocalizationKey;
		[UxmlAttribute("subtitle-localization-key")]
		public string subtitleLocalizationKey
		{
			get => _subtitleLocalizationKey;
			set
			{
				_subtitleLocalizationKey = value;
				_subtitle.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
				_subtitle.Localize(value);
			}
		}

		private LocalizedLabel _title;
		private LocalizedLabel _subtitle;
		private VisualElement _image;

		public MatchSettingsSelectionElement() : this("DISTRICT DASH", "Small map, shorter\nmatches.")
		{
		}

		public MatchSettingsSelectionElement(string titleKey, string subtitleKey)
		{
			AddToClassList(USS_BLOCK);

			var textContainer = new VisualElement {name = "text-container"};
			Add(textContainer);
			textContainer.AddToClassList(USS_TEXT_CONTAINER);
			{
				textContainer.Add(_title = new LocalizedLabel(titleKey) {name = "title"});
				_title.AddToClassList(USS_TITLE);

				{
					textContainer.Add(_subtitle = new LocalizedLabel(subtitleKey) {name = "subtitle"});
					_subtitle.AddToClassList(USS_SUBTITLE);
					_subtitle.style.display = string.IsNullOrEmpty(subtitleKey) ? DisplayStyle.None : DisplayStyle.Flex;
				}


			}

			Add(_image = new VisualElement {name = "image"});
			_image.AddToClassList(USS_IMAGE);
		}

		public void SetImage(Sprite sprite)
		{
			_image.style.backgroundImage = new StyleBackground(sprite);
		}
	}
}
