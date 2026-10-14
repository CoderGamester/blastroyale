using FirstLight.Game.Localization;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays the base popup
	/// </summary>
	[UxmlElement]
	public partial class PopupElement : VisualElement
	{
		private string _titleLocalizationKey;
		[UxmlAttribute("title-localization-key")]
		private string titleLocalizationKey
		{
			get => _titleLocalizationKey;
			set => Localize(value);
		}

		private const string UssPopup = "popup-element";
		private const string UssPopupFront = UssPopup + "__front";
		private const string UssPopupHeader = UssPopup + "__header";
		private const string UssPopupHeaderBackground = UssPopupHeader + "__background";
		private const string UssPopupHeaderTitle = UssPopupHeader + "__title";
		private const string UssPopupContainer = UssPopup + "__container";
		private const string UssPopupBlocker = UssPopup + "__blocker";

		private VisualElement _container;
		public override VisualElement contentContainer => _container;
		private LabelOutlined _title;

		public PopupElement()
		{
			AddToClassList(UssPopup);
			AddToClassList("anim-delay-0");
			AddToClassList("anim-translate");
			AddToClassList("anim-translate--down-xxxl");

			VisualElement frontPopup = new VisualElement();
			frontPopup.AddToClassList(UssPopupFront);
			hierarchy.Add(frontPopup);

			VisualElement header = new VisualElement();
			header.AddToClassList(UssPopupHeader);
			frontPopup.Add(header);

			VisualElement dotsBg = new VisualElement();
			header.Add(dotsBg);
			dotsBg.AddToClassList(UssPopupHeaderBackground);

			_title = new LabelOutlined("Title") {name = "title"};
			header.Add(_title);
			_title.AddToClassList(UssPopupHeaderTitle);

			_container = new VisualElement();
			_container.AddToClassList(UssPopupContainer);
			frontPopup.Add(_container);

			VisualElement blocker = new VisualElement();
			blocker.name = "Blocker";
			blocker.AddToClassList(UssPopupBlocker);
			frontPopup.Add(blocker);
			Localize(null);
		}

		public void Localize(string key)
		{
			_titleLocalizationKey = key;
			_title.text = GameLocalization.TryGetTranslation(key, out var translation) ? translation : $"#{key}#";
		}
	}
}
