using System;
using UnityEngine;
using UnityEngine.UIElements;
namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A visual element that sets it's margins to account for safe area on mobile devices.
	/// </summary>
	[UxmlElement]
	public partial class SafeAreaElement : VisualElement
	{
		private bool _applyTop = true;
		[UxmlAttribute("apply-top")]
		private bool applyTop
		{
			get => _applyTop;
			set { _applyTop = value; if (panel != null) LayoutChanged(null); }
		}
		private bool _applyBottom = true;
		[UxmlAttribute("apply-bottom")]
		private bool applyBottom
		{
			get => _applyBottom;
			set { _applyBottom = value; if (panel != null) LayoutChanged(null); }
		}
		private bool _applyLeft = true;
		[UxmlAttribute("apply-left")]
		private bool applyLeft
		{
			get => _applyLeft;
			set { _applyLeft = value; if (panel != null) LayoutChanged(null); }
		}
		private bool _applyRight = true;
		[UxmlAttribute("apply-right")]
		private bool applyRight
		{
			get => _applyRight;
			set { _applyRight = value; if (panel != null) LayoutChanged(null); }
		}
		private bool _invert = false;
		[UxmlAttribute("invert")]
		private bool invert
		{
			get => _invert;
			set { _invert = value; if (panel != null) LayoutChanged(null); }
		}

		public SafeAreaElement() : this(true)
		{
		}

		public SafeAreaElement(bool applyTop = true, bool applyBottom = true, bool applyLeft = true,
							   bool applyRight = true)
		{
			this.applyTop = applyTop;
			this.applyBottom = applyBottom;
			this.applyLeft = applyLeft;
			this.applyRight = applyRight;

			style.flexGrow = 1;
			style.flexShrink = 1;

			pickingMode = PickingMode.Ignore; // To allow raycasts to pass through it

			RegisterCallback<GeometryChangedEvent>(LayoutChanged);
		}

		private void LayoutChanged(GeometryChangedEvent e)
		{
			var safeArea = Screen.safeArea;

			try
			{
				var leftTop =
					RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safeArea.xMin, Screen.height - safeArea.yMax));
				var rightBottom =
					RuntimePanelUtils.ScreenToPanel(panel, new Vector2(Screen.width - safeArea.xMax, safeArea.yMin));

				style.marginTop = applyTop ? (invert ? -leftTop.y : leftTop.y) : 0;
				style.marginBottom = applyBottom ? (invert ? -rightBottom.y : rightBottom.y) : 0;
				style.marginLeft = applyLeft ? (invert ? -leftTop.x : leftTop.x) : 0;
				style.marginRight = applyRight ? (invert ? -rightBottom.x : rightBottom.x) : 0;
			}
			catch (InvalidCastException)
			{
				// Can be ignored, only for editor.
			}
		}
	}
}
