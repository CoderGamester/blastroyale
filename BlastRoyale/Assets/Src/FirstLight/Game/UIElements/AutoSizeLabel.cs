using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// !!EXPERIMENTAL!!
	/// A label that automatically resizes text to fit it's bounds.
	///
	/// IMPORTANT NOTE:
	/// This element allegedly doesn't work with flexGrow as it leads to undefined behaviour (recursion).
	/// Use Size/Width[%] and Size/Height attributes instead
	/// </summary>
	[UxmlElement]
	public sealed partial class AutoSizeLabel : LabelOutlined
	{
		private int _minFontSize = 10;
		[UxmlAttribute("min-font-size"), Min(1)]
		private int minFontSize
		{
			get => _minFontSize;
			set { _minFontSize = Mathf.Max(value, 1); UpdateFontSize(); }
		}
		private int _maxFontSize = 200;
		[UxmlAttribute("max-font-size"), Min(1)]
		private int maxFontSize
		{
			get => _maxFontSize;
			set { _maxFontSize = Mathf.Max(value, 1); UpdateFontSize(); }
		}

		public override string text
		{
			get => base.text;
			set
			{
				base.text = value;
				UpdateFontSize();
			}
		}

		public AutoSizeLabel() : this(string.Empty)
		{
		}

		public AutoSizeLabel(string text, int minFontSize = 10, int maxFontSize = 200) : base(text)
		{
			this.minFontSize = minFontSize;
			this.maxFontSize = maxFontSize;

			RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
		}

		private void OnGeometryChanged(GeometryChangedEvent evt)
		{
			UpdateFontSize();
		}

		private void UpdateFontSize()
		{
			var textSize = MeasureTextSize(text,
				float.MaxValue, MeasureMode.AtMost, float.MaxValue, MeasureMode.AtMost);

			// Unity can return a font size of 0 which would break the auto fit
			// Should probably wait till the end of frame to get the real font size
			var fontSize = Mathf.Max(style.fontSize.value.value, 1);
			var heightDictatedFontSize = Mathf.Abs(contentRect.height);
			var widthDictatedFontSize = Mathf.Abs(contentRect.width / textSize.x) * fontSize;
			var newFontSize = (float) Mathf.FloorToInt(Mathf.Min(heightDictatedFontSize, widthDictatedFontSize));
			newFontSize = Mathf.Clamp(newFontSize, minFontSize, maxFontSize);

			if (Mathf.Abs(newFontSize - fontSize) > 1)
			{
				style.fontSize = new StyleLength(new Length(newFontSize));
			}
		}
	}
}
