using System;
using FirstLight.Game.Utils;
using FirstLight.Game.UI;
using Photon.Realtime;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays the common header element with a title and a subtitle.
	/// </summary>
	[UxmlElement]
	public partial class ScreenHeaderElement : VisualElement
	{
		private const string USS_BLOCK = "screen-header";

		private const string USS_SAFE_AREA_HOLDER = USS_BLOCK + "__safe-area-holder";
		private const string USS_DISABLE_BUTTONS_MODIFIER = USS_BLOCK + "--disable-buttons";
		private const string USS_TITLE = USS_BLOCK + "__title";
		private const string USS_SUB_TITLE = USS_BLOCK + "__subtitle";
		private const string USS_BACK = USS_BLOCK + "__back";
		private const string USS_SEPARATOR = USS_BLOCK + "__separator";
		private const string USS_NO_BUTTON = USS_BLOCK + "__no-button";

		/// <summary>
		/// Triggered when the back button is clicked.
		/// </summary>
		public Action backClicked;

		private string _titleKey;
		[UxmlAttribute("title-key")]
		private string titleKey
		{
			get => _titleKey;
			set { _titleKey = value; _title.text = string.IsNullOrWhiteSpace(value) ? "" : value.LocalizeKey(); }
		}
		private string _subtitleKey;
		[UxmlAttribute("subtitle-key")]
		private string subtitleKey
		{
			get => _subtitleKey;
			set { _subtitleKey = value; SetSubtitle(string.IsNullOrWhiteSpace(value) ? "" : value.LocalizeKey()); }
		}

		public VisualElement Title => _title;

		private readonly Label _title;
		private readonly Label _subTitle;
		private readonly ImageButton _back;
		private SafeAreaElement _safeAreaContainer;

		public ScreenHeaderElement()
		{
			focusable = true;
			AddToClassList(USS_BLOCK);
			// AddToClassList("anim-delay-0");
			// AddToClassList("anim-fade");

			// This doesn't seem to work at the moment - picking mode has to be manually set to Ignore
			// on the Header element in UXML if you want to have interactive elements behind it.
			pickingMode = PickingMode.Ignore;

			_safeAreaContainer = new SafeAreaElement(false, false, false, true);
			_safeAreaContainer.AddToClassList(USS_SAFE_AREA_HOLDER);
			Add(_safeAreaContainer);

			_safeAreaContainer.Add(_back = new ImageButton {name = "back"});
			_back.AddToClassList(USS_BACK);
			_back.AddToClassList(FirstLight.Game.UI.UIService.SFX_CLICK_BACKWARDS);
			_back.clicked += () => backClicked?.Invoke();

			_safeAreaContainer.Add(_title = new LabelOutlined("Title") {name = "title"});
			_title.AddToClassList(USS_TITLE);

			_safeAreaContainer.Add(_subTitle = new LabelOutlined("Subtitle") {name = "subtitle"});
			_subTitle.AddToClassList(USS_SUB_TITLE);

			var centerContent = new VisualElement {name = "separator", pickingMode = PickingMode.Ignore};
			centerContent.AddToClassList(USS_SEPARATOR);
			_safeAreaContainer.Add(centerContent);
			SetTitle("");
		}

		public void AdjustLabelWidthConsidering(float offset, params VisualElement[] elements)
		{
			var width = resolvedStyle.width - _back.resolvedStyle.width - _back.resolvedStyle.marginLeft + offset;
			width -= _safeAreaContainer.resolvedStyle.marginLeft;
			width -= _safeAreaContainer.resolvedStyle.marginRight;
			foreach (var visualElement in elements)
			{
				width -= visualElement.resolvedStyle.width;
			}

			this._title.style.width = width;
		}
		/// <summary>
		/// Show or hide back button based on the shouldShow parameter
		/// </summary>
		public void SetButtonsVisibility(bool shouldShow)
		{
			EnableInClassList(USS_DISABLE_BUTTONS_MODIFIER, !shouldShow);

			if (!shouldShow)
			{
				_title.AddToClassList(USS_NO_BUTTON);
				_subTitle.AddToClassList(USS_NO_BUTTON);
			}
		}

		/// <summary>
		/// Sets the title and optional subtitle of the header element (should be already localized).
		/// </summary>
		public void SetTitle(string title, string subtitle = "")
		{
			_title.text = title;
			SetSubtitle(subtitle);
		}

		/// <summary>
		/// Sets the subtitle of the header element (should be localized)
		/// </summary>
		public void SetSubtitle(string subtitle)
		{
			if (string.IsNullOrWhiteSpace(subtitle))
			{
				_subTitle.style.display = DisplayStyle.None;
			}
			else
			{
				_subTitle.style.display = DisplayStyle.Flex;
				_subTitle.text = subtitle;
			}
		}
	}
}
