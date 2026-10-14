using System;
using FirstLight.Game.Utils;
using FirstLight.Game.Localization;
using UnityEngine;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;

namespace FirstLight.Game.UIElements.Kit
{
	[UxmlElement]
	public partial class KitButton : ImageButton
	{
		private static string PRIMARY = "-primary";
		private static string SECONDARY = "-secondary";
		private static string SUCCESS = "-success";
		private static string ERROR = "-error";
		private static string USS_BUTTON_CLASS = "flg-btn";
		private static string USS_BUTTON_TEXT_CLASS = USS_BUTTON_CLASS + "-text";
		private static string USS_BUTTON_ICON = USS_BUTTON_CLASS + "-icon";
		private static string USS_BUTTON_SHAPE_SQUARE = USS_BUTTON_CLASS + "-square";
		private static string USS_BUTTON_SHAPE_LONG = USS_BUTTON_CLASS + "-long";
		private static string USS_BUTTON_STYLE_SOLID = USS_BUTTON_CLASS + "-solid";
		private static string USS_BUTTON_STYLE_TRANSPARENT = USS_BUTTON_CLASS + "-transparent";
		private static string USS_BUTTON_COLOR_PRIMARY = USS_BUTTON_CLASS + PRIMARY;
		private static string USS_BUTTON_COLOR_SECONDARY = USS_BUTTON_CLASS + SECONDARY;
		private static string USS_BUTTON_COLOR_SUCCESS = USS_BUTTON_CLASS + SUCCESS;
		private static string USS_BUTTON_COLOR_ERROR = USS_BUTTON_CLASS + ERROR;
		private static string USS_BUTTON_DEFAULT_GAP = USS_BUTTON_CLASS + "-default-gap"; // This is a hack because unity doesn't support gap property

		[UxmlAttribute("btn-style")]
		public ButtonStyle BtnStyle
		{
			get => _btnStyle;
			set
			{
				_btnStyle = value;
				UpdateClasses();
			}
		}

		[UxmlAttribute("btn-color")]
		public ButtonColor BtnColor
		{
			get => _btnColor;
			set
			{
				_btnColor = value;
				UpdateClasses();
			}
		}

		[UxmlAttribute("btn-shape")]
		public ButtonShape BtnShape
		{
			get => _btnShape;
			set
			{
				_btnShape = value;
				UpdateClasses();
			}
		}

		/// <see cref="FirstLight.Game.UIElements.Kit"/>
		[UxmlAttribute("btn-icon")]
		public string BtnIcon
		{
			get => _btnIcon;
			set
			{
				_btnIcon = value;
				UpdateClasses();
			}
		}

		[UxmlAttribute("btn-text")]
		public string BtnText
		{
			get => _text;
			set
			{
				_text = value;
				UpdateText();
			}
		}

		private VisualElement _iconElement;
		private Label _textElement;
		private ButtonStyle _btnStyle = ButtonStyle.Solid;
		private ButtonColor _btnColor = ButtonColor.Primary;
		private ButtonShape _btnShape = ButtonShape.Square;

		private string _btnIcon = "";
		private string _text = "";

		public KitButton()
		{
			UpdateClasses();
		}

		private void UpdateClasses()
		{
			ClearClassList();
			switch (_btnShape)
			{
				case ButtonShape.Square:
					AddToClassList(USS_BUTTON_SHAPE_SQUARE);
					break;
				case ButtonShape.Long:
					AddToClassList(USS_BUTTON_SHAPE_LONG);
					break;
				default:
					throw new NotSupportedException("Button format " + BtnShape.ToString() + " not supported yet!");
			}

			switch (_btnColor)
			{
				case ButtonColor.Primary:
					AddToClassList(USS_BUTTON_COLOR_PRIMARY);
					break;
				case ButtonColor.Secondary:
					AddToClassList(USS_BUTTON_COLOR_SECONDARY);
					break;
				case ButtonColor.Success:
					AddToClassList(USS_BUTTON_COLOR_SUCCESS);
					break;
				case ButtonColor.Error:
					AddToClassList(USS_BUTTON_COLOR_ERROR);
					break;
				default:
					throw new NotSupportedException("Button color " + BtnColor.ToString() + " not supported yet!");
			}

			switch (_btnStyle)
			{
				case ButtonStyle.Solid:
					AddToClassList(USS_BUTTON_STYLE_SOLID);
					break;
				case ButtonStyle.Transparency:
					AddToClassList(USS_BUTTON_STYLE_TRANSPARENT);
					break;
				default:
					throw new NotSupportedException("Button style " + BtnStyle.ToString() + " not supported yet!");
			}

			if (_iconElement != null)
			{
				Remove(_iconElement);
				_iconElement = null;
			}

			if (string.IsNullOrWhiteSpace(BtnIcon))
			{
				return;
			}

			_iconElement = new VisualElement() {name = "Icon"}.AddClass(USS_BUTTON_ICON);
			_iconElement.AddToClassList("icon--" + BtnIcon);
			Add(_iconElement);
		}

		private void UpdateText()
		{
			if (string.IsNullOrWhiteSpace(_text))
			{
				if (_textElement == null) return;
				Remove(_textElement);
				_textElement = null;
				return;
			}

			if (_textElement == null)
			{
				_textElement = new LabelOutlined(_text);
				_textElement.AddToClassList(USS_BUTTON_TEXT_CLASS);
			}
			else
			{
				_textElement.text = _text;
			}

			Add(_textElement);
		}

		public void AddDefaultGap()
		{
			this.AddToClassList(USS_BUTTON_DEFAULT_GAP);
		}

		public void Localize(string key)
		{
			if (string.IsNullOrWhiteSpace(key)) return;
			if (!GameLocalization.TryGetTranslation(key, out var translation))
			{
				translation = key;
				Debug.LogWarning($"Could not find translation for key {key} in element " + name);
			}

			BtnText = translation;
		}
	}

	public enum ButtonShape
	{
		Square,
		Long,
		[Obsolete("Not supported yet")] Round,
	}

	public enum ButtonColor
	{
		Primary,
		Secondary,
		Success,
		Error
	}

	public enum ButtonStyle
	{
		Transparency,
		Solid,
	}
}
