using System;
using FirstLight.Game.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Copied from Shader Graph package
	/// </summary>
	[UxmlElement]
	public partial class TabButton : VisualElement
	{

		static readonly string s_UssClassName = "unity-tab-button";
		static readonly string s_UssActiveClassName = s_UssClassName + "--active";

		private Label m_Label;

		[UxmlAttribute("text")]
		public string Text { get => m_Label.text; set => m_Label.text = value; }

		public bool IsCloseable { get; set; }
		[UxmlAttribute("target")]
		public string TargetId { get; private set; }
		public VisualElement Target { get; set; }
		private string _localizationKey;
		[UxmlAttribute("localization-key")]
		private string localizationKey
		{
			get => _localizationKey;
			set => LocalizeLabel(value);
		}

		public event Action<TabButton> OnSelect;
		public event Action<TabButton> OnClose;

		public TabButton()
		{
			Init();
			LocalizeLabel(null);
		}

		public TabButton(string text, VisualElement target)
		{
			Init();
			m_Label.text = text;
			Target = target;
		}

		public void LocalizeLabel(string labelKey)
		{
			_localizationKey = labelKey;
			m_Label.text = GameLocalization.TryGetTranslation(labelKey, out var translation)
				? translation
				: $"#{labelKey}#";
		}

		private void PopulateContextMenu(ContextualMenuPopulateEvent populateEvent)
		{
			DropdownMenu dropdownMenu = populateEvent.menu;

			if (IsCloseable)
			{
				dropdownMenu.AppendAction("Close Tab", e => OnClose(this));
			}
		}

		private void CreateContextMenu(VisualElement visualElement)
		{
			ContextualMenuManipulator menuManipulator = new ContextualMenuManipulator(PopulateContextMenu);

			visualElement.focusable = true;
			visualElement.pickingMode = PickingMode.Position;
			visualElement.AddManipulator(menuManipulator);

			visualElement.AddManipulator(menuManipulator);
		}

		private void Init()
		{
			AddToClassList(s_UssClassName);
			// styleSheets.Add(Resources.Load<StyleSheet>($"Styles/{styleName}"));
			//
			// VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>($"UXML/{UxmlName}");
			// visualTree.CloneTree(this);

			// m_Label = this.Q<Label>("Label");

			var topBar = new VisualElement();
			Add(topBar);
			topBar.AddToClassList("unity-tab-button__top-bar");

			var content = new VisualElement();
			Add(content);
			content.AddToClassList("unity-tab-button__content");

			m_Label = new LabelOutlined("Text") {name = "Label"};
			content.Add(m_Label);
			m_Label.AddToClassList("unity-tab-button__content-label");

			CreateContextMenu(this);

			RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
		}

		public void Select()
		{
			AddToClassList(s_UssActiveClassName);

			if (Target != null)
			{
				Target.style.display = DisplayStyle.Flex;
				Target.style.flexGrow = 1;
			}
		}

		public void Deselect()
		{
			RemoveFromClassList(s_UssActiveClassName);
			MarkDirtyRepaint();

			if (Target != null)
			{
				Target.style.display = DisplayStyle.None;
				Target.style.flexGrow = 0;
			}
		}

		private void OnMouseDownEvent(MouseDownEvent e)
		{
			switch (e.button)
			{
				case 0:
				{
					OnSelect?.Invoke(this);
					break;
				}

				case 2 when IsCloseable:
				{
					OnClose?.Invoke(this);
					break;
				}
			} // End of switch.

			e.StopImmediatePropagation();
		}
	}
}
