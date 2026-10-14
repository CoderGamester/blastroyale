using GameLovers.GameData;
using FirstLight.FLogger;
using FirstLight.Game.Infos;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A button with a title text and a price / icon deisplayed above.
	/// </summary>
	[UxmlElement]
	public partial class PriceButton : ImageButton
	{
		private const string UssButtonStyle = "button-long";
		private const string UssBlock = "price-button";
		private const string UssHolder = UssBlock + "__holder";
		private const string UssOffsetTitle = UssBlock + "__offset-title";
		private const string UssPriceHolder = UssBlock + "__price-holder";
		private const string UssPrice = UssBlock + "__price";
		private const string UssPriceInsufficient = UssPrice + "--insufficient";
		private const string UssIcon = UssBlock + "__icon";
		private const string UssSpriteIcon = "sprite-shared__icon-currency-{0}";

		private Label _price;
		private VisualElement _icon;
		private VisualElement _priceHolder;
		private VisualElement _holder;
		private Label _title;

		private string _localizationKey;
		[UxmlAttribute("localization-key")]
		private string localizationKey
		{
			get => _localizationKey;
			set { _localizationKey = value; _title.text = value.LocalizeKey(); }
		}

		public PriceButton()
		{
			AddToClassList(UssButtonStyle);
			AddToClassList(UssBlock);

			var holder = new VisualElement {name = "holder"};
			holder.AddToClassList(UssHolder);
			Add(holder);
			{
				var priceHolder = new VisualElement {name = "price-holder"};
				priceHolder.AddToClassList(UssPriceHolder);
				holder.Add(priceHolder);
				{
					priceHolder.Add(_price = new Label("123") {name = "price"});
					_price.AddToClassList(UssPrice);

					priceHolder.Add(_icon = new VisualElement {name = "icon"});
					_icon.AddToClassList(UssIcon);
					_icon.AddToClassList(string.Format(UssSpriteIcon, "bpp"));
				}

				_holder = holder;
				_priceHolder = priceHolder;

				holder.Add(_title = new Label("REPAIR") {name = "title"});
			}
		}

		/// <summary>
		/// Sets the price amount and icon.
		/// </summary>
		public void SetPrice(Pair<GameId, uint> price, bool insufficient = false, bool overrideDisablePrice = false)
		{
			_price.text = price.Value.ToString();
			_price.RemoveModifiers();
			if (insufficient)
			{
				_price.AddToClassList(UssPriceInsufficient);
			}

			_icon.RemoveSpriteClasses();
			_icon.AddToClassList(string.Format(UssSpriteIcon, price.Key.ToString().ToLowerInvariant()));

			_priceHolder.SetDisplay(true);
			_title.EnableInClassList(UssOffsetTitle, false);

			if (overrideDisablePrice)
			{
				_priceHolder.SetDisplay(false);
				_title.EnableInClassList(UssOffsetTitle, true);
			}
		}
	}
}
