using MainInstaller = FirstLight.Game.Utils.MainInstaller;
using GameLovers.GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using GameLovers.Services;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A small widget displaying the selected currency with it's icon. Also handles flying VFX for it.
	/// </summary>
	[UxmlElement("CurrencyDisplayElement", new Type[0])]
	public partial class CurrencyDisplayElement : VisualElement
	{
		/* Class names are at the top in const fields */
		private const string UssBlock = "currency-display";
		private const string UssIcon = UssBlock + "__icon";
		private const string UssIconOutline = UssBlock + "__icon-outline";
		private const string LabelUssClassName = UssBlock + "__label";

		/* UXML attributes */
		private GameId _currency = GameId.CS;
		[UxmlAttribute("currency")]
		private GameId Currency
		{
			get => _currency;
			set
			{
				if (!value.IsInGroup(GameIdGroup.Currency))
					throw new ArgumentOutOfRangeException(nameof(value), value, "UXML currency must be a currency GameId.");
				SetCurrency(value);
			}
		}
		private bool _hideIfPlayerDoesntHaveIt = false;

		/* VisualElements created within this element */
		private readonly VisualElement _icon;
		private readonly VisualElement _iconOutline;
		private readonly Label _label;

		/* Services, providers etc... */
		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;
		private CurrencyItemViewModel _currencyView;

		/* Other private variables */
		private CurrencyAnimationHandler _animationHandler;

		/* The internal structure of the element is created in the constructor. */
		public CurrencyDisplayElement()
		{
			_animationHandler = new CurrencyAnimationHandler();
			AddToClassList(UssBlock);

			// Icon outline
			_iconOutline = new VisualElement();
			_iconOutline.AddToClassList(UssIconOutline);
			Add(_iconOutline);

			// Currency icon
			_icon = new VisualElement();
			_icon.AddToClassList(UssIcon);
			_iconOutline.Add(_icon);

			// Currency label
			_label = new LabelOutlined("1234") {name = "CurrencyAmount"};
			_label.AddToClassList(LabelUssClassName);
			Add(_label);

			RegisterCallback<ClickEvent>(OnClicked);
			UpdateCurrencyView();
		}

		public void SetCurrency(GameId gameId)
		{
			_currency = gameId;
			UpdateCurrencyView();
		}

		public void SetCurrency(GameId gameId, ulong amount)
		{
			_currency = gameId;
			UpdateCurrencyView();
			_label.text = amount.ToString();
		}

		private void UpdateCurrencyView()
		{
			_currencyView = (CurrencyItemViewModel) ItemFactory.Currency(Currency, 0).GetViewModel();

			_icon.ClearClassList();
			_icon.AddToClassList(UssIcon);
			_currencyView.DrawIcon(_icon);

			_iconOutline.ClearClassList();
			_iconOutline.AddToClassList(UssIconOutline);
			_currencyView.DrawIcon(_iconOutline);
		}

		private void OnClicked(ClickEvent evt)
		{
			this.OpenTooltip(panel.visualTree, Currency.GetDescriptionLocalization());
		}

		public void Init(IGameDataProvider gameDataProvider, IGameServices gameServices)
		{
			_gameDataProvider = gameDataProvider;
			_services = gameServices;
		}

		public void SubscribeToEvents()
		{
			_gameDataProvider.CurrencyDataProvider.Currencies.Observe(Currency, OnCurrencyChanged);
			var amount = _gameDataProvider.CurrencyDataProvider.GetCurrencyAmount(Currency);
			this.SetDisplay(amount > 0 || !_hideIfPlayerDoesntHaveIt);
			_label.text = amount.ToString();
		}

		public void UnsubscribeFromEvents()
		{
			_gameDataProvider.CurrencyDataProvider.Currencies.StopObservingAll(this);
		}

		/// <summary>
		/// Sets the origin of the currency flying animation starting at another visual element
		/// </summary>
		public void SetData(VisualElement animationOrigin, bool hideIfPlayerDoesntHaveIt = false, CancellationToken cancellationToken = default)
		{
			_animationHandler.CancellationToken = cancellationToken;
			_animationHandler.Origin = animationOrigin;
			_animationHandler.Root = this.GetRoot();
			_animationHandler.Target = _label;
			_animationHandler.GameId = Currency;

			_hideIfPlayerDoesntHaveIt = hideIfPlayerDoesntHaveIt;
		}

		private void OnCurrencyChanged(GameId id, ulong previous, ulong current, ObservableUpdateType type)
		{
			OnCurrencyChanged(previous, current);
		}

		private void OnCurrencyChanged(ulong previous, ulong current)
		{
			this.SetDisplay(current > 0 || !_hideIfPlayerDoesntHaveIt);
			if (!_animationHandler.Playing && current > previous)
			{
				_animationHandler.AnimateCurrency(previous, current).Forget();
			}
			else
			{
				_label.text = current.ToString();
			}
		}

		public class CurrencyAnimationHandler
		{
			public bool Playing;

			public GameId GameId;
			public CancellationToken CancellationToken;
			public VisualElement Root;
			public VisualElement Origin;
			public VisualElement Target;

			public UIVFXService UIVFXService => MainInstaller.ResolveServices().UIVFXService;
			public IAudioFxService<AudioId> AudioFxService => MainInstaller.ResolveServices().AudioFxService;

			public async UniTaskVoid AnimateCurrency(ulong previous,
													 ulong current, bool updateText = true)
			{
				Playing = true;
				if (updateText && Target is Label lbl)
				{
					lbl.text = previous.ToString();
				}

				await UniTask.WaitUntil(() => Target == null || Target.worldBound.Overlaps(Root.worldBound), cancellationToken: CancellationToken);
				if (Target == null) return;
				// Wait for currency view animation to finish
				await UniTask.Delay(500, cancellationToken: CancellationToken);
				var labelPosition = Target.GetPositionOnScreen(Root);
				for (int i = 0; i < Mathf.Min(10, current - previous); i++)
				{
					var originPosition = Origin != null
						? Origin.GetPositionOnScreen(Root)
						: Root.GetPositionOnScreen(Root) + Random.insideUnitCircle * 100;

					UIVFXService.PlayVfx(GameId,
						i * 0.1f,
						originPosition,
						labelPosition,
						() =>
						{
							if (updateText && Target is Label lbl)
							{
								DOVirtual.Float(previous, current, 0.3f, val => { lbl.text = val.ToString("F0"); });
							}
							AudioFxService.PlayClip2D(AudioId.CounterTick1);
						});
				}

				Playing = false;
			}
		}
	}
}
