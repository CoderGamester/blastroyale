using GameLovers.GameData;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A button with a title text and a price / icon deisplayed above.
	/// </summary>
	[UxmlElement]
	public partial class MultiPriceButton : ImageButton
	{
		private const string UssButtonStyle = "button-long";
		private const string UssBlock = "multi-price-button";
		private const string UssHolder = UssBlock + "__holder";
		private const string UssOffsetTitle = UssBlock + "__offset-title";
		private const string UssPrice = UssBlock + "__price";
		private const string UssPriceInsufficient = UssPrice + "--insufficient";
		private const string UssIcon = UssBlock + "__icon";
		private const string UssSpriteIcon = "sprite-shared__icon-currency-{0}";

		private VisualElement _holder;
		private Label _title;

		private string _localizationKey;
		[UxmlAttribute("localization-key")]
		private string localizationKey
		{
			get => _localizationKey;
			set { _localizationKey = value; _title.text = value.LocalizeKey(); }
		}

		public MultiPriceButton()
		{
			AddToClassList(UssButtonStyle);
			AddToClassList(UssBlock);

			Add(_holder = new VisualElement { name = "holder" });
			_holder.AddToClassList(UssHolder);

			Add(_title = new Label("REPAIR") { name = "title" });
		}

		/// <summary>
		/// Sets the price amounts and icons for each cost.
		/// </summary>
		public void SetPrice(Pair<GameId, uint>[] prices, bool[] insufficient, bool overrideDisablePrice = false)
		{
			_holder.Clear();

			for(int i = 0; i < prices.Length; i++)
			{
				Label currentPrice;
				_holder.Add(currentPrice = new Label("123") { name = "price" });
				currentPrice.AddToClassList(UssPrice);
				currentPrice.text = prices[i].Value.ToString();

				VisualElement currentIcon;
				_holder.Add(currentIcon = new VisualElement { name = "icon" });
				currentIcon.AddToClassList(UssIcon);
				currentIcon.AddToClassList(string.Format(UssSpriteIcon, prices[i].Key.ToString().ToLowerInvariant()));

				if (insufficient[i])
				{
					currentPrice.AddToClassList(UssPriceInsufficient);
				}
			}

			_holder.SetDisplay(true);
			_title.EnableInClassList(UssOffsetTitle, false);

			if (overrideDisablePrice)
			{
				_holder.SetDisplay(false);
				_title.EnableInClassList(UssOffsetTitle, true);
				_holder.Clear();
			}
		}

	}
}
