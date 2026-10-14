using System;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A button that does not have a text.
	/// </summary>
	[UxmlElement]
	public partial class ImageButton : VisualElement
	{
		[UxmlAttribute("enabled")]
		public bool enabled
		{
			get => enabledSelf;
			set => SetEnabled(value);
		}

		private Clickable _clickable;

		/// <summary>
		/// Clickable MouseManipulator for this Button.
		/// </summary>
		public Clickable clickable
		{
			get => _clickable;
			set
			{
				if (_clickable != null && _clickable.target == this)
					this.RemoveManipulator(_clickable);
				_clickable = value;
				if (_clickable == null)
					return;
				this.AddManipulator(_clickable);
			}
		}

		/// <summary>
		/// Triggered when the button is clicked
		/// </summary>
		public event Action clicked
		{
			add
			{
				if (_clickable == null)
					clickable = new Clickable(value);
				else
					_clickable.clicked += value;
			}
			remove
			{
				if (_clickable == null)
					return;
				_clickable.clicked -= value;
			}
		}

		/// <summary>
		/// Constructs a Button.
		/// </summary>
		public ImageButton() : this(null)
		{
		}

		/// <summary>
		/// Constructs a button with an Action that is triggered when the button is clicked.
		/// </summary>
		public ImageButton(Action clickEvent)
		{
			clickable = new Clickable(clickEvent);
			focusable = true;
			SetEnabled(enabled);
		}
	}
}
