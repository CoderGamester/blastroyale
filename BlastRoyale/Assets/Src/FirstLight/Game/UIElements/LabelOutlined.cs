using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	[UxmlElement]
	public partial class LabelOutlined : Label
	{
		private Label _internalLabel;

		public override VisualElement contentContainer => outlineHack ? _internalLabel : this;

		private bool _outlineHack;
		private bool _callbacksRegistered;
		[UxmlAttribute("outline-hack")]
		protected bool outlineHack
		{
			get => _outlineHack;
			set
			{
				if (_outlineHack == value) return;
				_outlineHack = value;
				Init();
			}
		}

#if UNITY_EDITOR
		private IVisualElementScheduledItem _scheduledEditorUpdate;
#endif
		protected void Init()
		{
			if (_internalLabel != null)
			{
				hierarchy.Remove(_internalLabel);
				_internalLabel = null;
			}

			if (!_callbacksRegistered)
			{
				_callbacksRegistered = true;
				this.RegisterValueChangedCallback(valueChange =>
				{
					if (_internalLabel == null || valueChange.target != this) return;
					_internalLabel.text = valueChange.newValue;
					if (resolvedStyle.textOverflow == TextOverflow.Ellipsis) SetTextEllipticEnd(valueChange.newValue);
				});
				RegisterCallback<GeometryChangedEvent>(_ => Sync());
			}
#if UNITY_EDITOR
			_scheduledEditorUpdate?.Pause();
#endif
			if (outlineHack)
			{
				_internalLabel = new Label()
				{
					name = "Text",
					text = "Initial Value"
				};
				_internalLabel.AddToClassList("internal-label");
				_internalLabel.style.unityTextOutlineWidth = 0;
				_internalLabel.style.textShadow = new TextShadow()
				{
					color = Color.clear
				};
				hierarchy.Add(_internalLabel);

				Sync();

				_internalLabel.RegisterCallback<AttachToPanelEvent>((_) =>
				{
					Sync();
				});
#if UNITY_EDITOR
				_scheduledEditorUpdate?.Pause();
				_scheduledEditorUpdate = schedule.Execute(() =>
				{
					if (Application.isPlaying) return;
					Sync();
				}).Every(500);
#endif
			}
		}

		private string _lastResolvedEliptic;
		private float _lastResolvedSize;

		public void SetTextEllipticEnd(string param)
		{
			if (_lastResolvedEliptic == param && _lastResolvedSize == contentRect.width) return;
			_lastResolvedSize = contentRect.width;
			var index = param.Length;

			while (index > 3)
			{
				var ellipsed = param[..index];
				if (index != param.Length)
				{
					ellipsed += "...";
				}

				var textSize = MeasureTextSize(ellipsed,
					float.MaxValue, MeasureMode.AtMost, float.MaxValue, MeasureMode.AtMost);
				if (textSize.x == 0) return;
				if (textSize.x > contentRect.width)
				{
					if (index == param.Length)
					{
						index -= 3;
					}
					else
					{
						index--;
					}
				}
				else
				{
					_lastResolvedEliptic = text = ellipsed;
					return;
				}
			}

			text = text;
		}

		/// <summary>
		/// Creates the default outlined label for UXML and programmatic use.
		/// </summary>
		public LabelOutlined() : this(string.Empty)
		{
		}

		public LabelOutlined(string text, bool outlineHack = true)
		{
			_outlineHack = outlineHack;
			this.text = text;
			Init();
		}

		private void Sync()
		{
			if (_internalLabel == null) return;
			_internalLabel.text = text;
			_internalLabel.style.whiteSpace = resolvedStyle.whiteSpace;
			_internalLabel.parseEscapeSequences = parseEscapeSequences;
			_internalLabel.style.textOverflow = resolvedStyle.textOverflow;
			_internalLabel.style.flexDirection = resolvedStyle.flexDirection;
			_internalLabel.style.alignItems = resolvedStyle.alignItems;
			_internalLabel.style.alignContent = resolvedStyle.alignContent;
			_internalLabel.style.justifyContent = resolvedStyle.justifyContent;
			if (_internalLabel.resolvedStyle.textOverflow == TextOverflow.Ellipsis)
			{
				SetTextEllipticEnd(text);
			}
		}
	}
}
