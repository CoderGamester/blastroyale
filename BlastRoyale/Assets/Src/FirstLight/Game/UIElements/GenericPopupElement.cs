using System;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Utils;
using FirstLight.Game.Localization;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// The default popup element for the game.
	/// </summary>
	[UxmlElement]
	public partial class GenericPopupElement : VisualElement
	{
		private const string USS_BLOCK = "generic-popup";
		private const string USS_NO_PADDING_MODIFIER = USS_BLOCK + "--no-padding";
		private const string USS_HEADER = USS_BLOCK + "__header";
		private const string USS_TITLE = USS_BLOCK + "__title";
		private const string USS_CONTENT = USS_BLOCK + "__content";
		private const string USS_GLOW_HOLDER = USS_CONTENT + "__glow-holder";
		private const string USS_GLOW = USS_CONTENT + "__glow";
		private const string USS_CLOSE_BUTTON = USS_BLOCK + "__close-button";
		private const string USS_CLOSE_BUTTON_CONTAINER = USS_CLOSE_BUTTON + "-container";
		private string _TitleLocalizationKey;
		[UxmlAttribute("title-localization-key")]
		private string TitleLocalizationKey
		{
			get => _TitleLocalizationKey;
			set => LocalizeTitle(value);
		}
		private bool _glowEffect;
		[UxmlAttribute("glow-effect")]
		private bool GlowEffect
		{
			get => _glowEffect;
			set { _glowEffect = value; SetGlowEffect(value); }
		}
		private bool _disableCloseButton;
		[UxmlAttribute("disable-close-button")]
		private bool DisableCloseButton
		{
			get => _disableCloseButton;
			set { _disableCloseButton = value; SetCloseButton(!value); }
		}

		private readonly VisualElement _header;
		private readonly Label _title;
		private ImageButton _closeButton;
		private readonly VisualElement _content;
		private VisualElement _glowHolder;

		public event Action CloseClicked;

		public override VisualElement contentContainer { get; }

		public GenericPopupElement()
		{
			contentContainer = this;

			AddToClassList(USS_BLOCK);

			// Header
			Add(_header = new VisualElement {name = "header"});
			_header.AddToClassList(USS_HEADER);
			{
				_header.Add(_title = new LabelOutlined("Title") {name = "title"});
				_title.AddToClassList(USS_TITLE);
			}
			Add(_content = new VisualElement {name = "content"});
			_content.AddToClassList(USS_CONTENT);
			contentContainer = _content;
			SetCloseButton(!DisableCloseButton);
			SetGlowEffect(GlowEffect);
			LocalizeTitle(null);
		}

		public async UniTask AnimateOpen()
		{
			var complete = new UniTaskCompletionSource();
			var tmp = experimental.animation.Start(0, 1, 166, (el, v) =>
			{
				el.style.scale = new StyleScale(new Vector2(v, v));
			}).Ease(Easing.OutBack);
			tmp.OnCompleted(() =>
			{
				complete.TrySetResult();
			});
			await complete.Task;
		}

		public async UniTask AnimateClose()
		{
			var complete = new UniTaskCompletionSource();
			var tmp = experimental.animation.Start(1, 0, 166, (el, v) =>
			{
				el.style.scale = new StyleScale(new Vector2(v, v));
			}).Ease(Easing.InCubic);
			tmp.OnCompleted(() =>
			{
				complete.TrySetResult();
			});
			await complete.Task;
		}

		public void SetCloseButton(bool on)
		{
			if (!on && _closeButton == null) return;
			if (on && _closeButton != null) return;
			if (on)
			{
				_closeButton = new ImageButton {name = "close-button-container"};
				_closeButton.AddToClassList(USS_CLOSE_BUTTON_CONTAINER);
				var icon = new VisualElement() {name = "close-button"};
				icon.AddToClassList(USS_CLOSE_BUTTON);
				_closeButton.Add(icon);
				_closeButton.clicked += () => CloseClicked?.Invoke();
				_header.Add(_closeButton);
			}
			else
			{
				_header.Remove(_closeButton);
				_closeButton = null;
			}
		}

		public void SetGlowEffect(bool on)
		{
			if (!on && _glowHolder == null) return;
			if (on && _glowHolder != null) return;
			if (on)
			{
				_glowHolder = new VisualElement {name = "glow-holder"};
				_glowHolder.AddToClassList(USS_GLOW_HOLDER);
				var glow = new VisualElement() {name = "glow"};
				glow.AddToClassList(USS_GLOW);
				_glowHolder.Add(glow);
				_content.Insert(0, _glowHolder);
			}
			else
			{
				_content.Remove(_glowHolder);
				_glowHolder = null;
			}
		}

		public GenericPopupElement EnablePadding(bool padding)
		{
			EnableInClassList(USS_NO_PADDING_MODIFIER, !padding);
			return this;
		}

		public void LocalizeTitle(string labelKey)
		{
			_TitleLocalizationKey = labelKey;
			_title.text = GameLocalization.TryGetTranslation(labelKey, out var translation)
				? translation
				: $"#{labelKey}#";
		}

		public void SetTitle(string title)
		{
			_TitleLocalizationKey = null;
			_title.text = title;
		}
	}
}
