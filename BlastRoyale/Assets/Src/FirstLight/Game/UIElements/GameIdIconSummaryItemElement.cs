using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	[UxmlElement]
	public partial class GameIdIconSummaryItemElement : RewardSummaryItemElement
	{
		public override RewardSummaryItemElement SetReward(IItemViewModel itemViewModel)
		{
			itemViewModel.DrawIcon(_icon);
			_label.text = itemViewModel.DisplayName;
			return this;
		}

	}
}
