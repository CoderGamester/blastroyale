using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters.BattlePass
{
	/// <summary>
	/// Displays a glint over an element. Must be used with a vector mask for shaped glints.
	/// </summary>
	[UxmlElement]
	public partial class BattlepassSegmentButtonElement : VisualElement
	{
		private const string UssSpriteRarityModifier = "--sprite-home__pattern-rewardglow-";
		private const string UssClaimableButton = "reward__button-claimable";
		private const string UssClaimedButton = "reward__button--claimed";
		private const string UssUnclaimedFree = "reward__button--unclaimed-free";
		private const string UssUnclaimedPaid = "reward__button--unclaimed-paid";
		private const string UssRootModifierSmall = "reward__root--small";

		public event Action<BattlepassSegmentButtonElement> Clicked;
		private VisualElement _rewardRoot;
		private VisualElement _blocker;
		private VisualElement _claimBubble;
		private VisualElement _rewardImage;
		private VisualElement _imageContainer;
		private VisualElement _readyToClaimOutline;
		private VisualElement _claimedCheckmark;
		private AutoSizeLabel _title;
		private AutoSizeLabel _type;
		private ImageButton _button;
		private VisualElement _lock;

		public BattlePassSegmentData SegmentData { get; private set; }
		[UxmlAttribute("unlocked-premium")]
		public bool UnlockedPremium
		{
			get => _uxmlUnlockedPremium;
			private set { _uxmlUnlockedPremium = value; ScheduleUxmlPreview(); }
		}
		[UxmlAttribute("state")]
		public RewardState RewardState
		{
			get => _uxmlRewardState;
			private set { _uxmlRewardState = value; ScheduleUxmlPreview(); }
		}

		[UxmlAttribute("pass-type")]
		public PassType PassType
		{
			get => _uxmlPassType;
			private set { _uxmlPassType = value; ScheduleUxmlPreview(); }
		}
		[UxmlAttribute("reward")]
		private GameId PreviewReward
		{
			get => _uxmlPreviewReward;
			set { _uxmlPreviewReward = value; ScheduleUxmlPreview(); }
		}
		[UxmlAttribute("debug-mode")]
		private bool DebugMode
		{
			get => _uxmlDebugMode;
			set { _uxmlDebugMode = value; ScheduleUxmlPreview(); }
		}
		private bool _uxmlUnlockedPremium = false;
		private RewardState _uxmlRewardState = RewardState.Claimable;
		private PassType _uxmlPassType = PassType.Free;
		private GameId _uxmlPreviewReward = GameId.PlayerSkinBrandMale;
		private bool _uxmlDebugMode = false;
		private bool _hasRuntimeData;

		private IVisualElementScheduledItem _previewRefresh;

		private void ScheduleUxmlPreview()
		{
			if (_hasRuntimeData) return;
			_previewRefresh?.Pause();
			_previewRefresh = schedule.Execute(() =>
			{
				if (!DebugMode || _hasRuntimeData) return;
				ApplyData(new BattlePassSegmentData
				{
					PassType = PassType,
					RewardConfig = new EquipmentRewardConfig { GameId = PreviewReward, Amount = 1 },
					SegmentLevel = 1, LevelAfterClaiming = 2, PointsAfterClaiming = 2
				}, RewardState, UnlockedPremium, false);
			});
		}

		public BattlepassSegmentButtonElement()
		{
			// TODO: Move to unitask and addressables
			var a = Resources.Load<VisualTreeAsset>("BattlepassSegmentButtonElement");
			a.CloneTree(this);
			var element = this;
			_rewardRoot = element.Q("Reward").Required();
			_blocker = element.Q("Blocker").Required();
			_readyToClaimOutline = element.Q("ReadyToClaimOutline").Required();
			_claimBubble = element.Q("ClaimBubble").Required();
			_claimedCheckmark = element.Q("Checkmark").Required();
			_button = element.Q<ImageButton>("Button").Required();
			_rewardImage = element.Q("RewardImage").Required();
			_title = element.Q<AutoSizeLabel>("Title").Required();
			_type = element.Q<AutoSizeLabel>("Type").Required();
			_lock = element.Q("Lock").Required();
			_imageContainer = _rewardImage.parent;
			_button.clicked += () => Clicked?.Invoke(this);
			RegisterCallback<AttachToPanelEvent>(_ => ScheduleUxmlPreview());
		}

		/// <summary>
		/// Sets the data needed to fill the segment visuals
		/// </summary>
		public void SetData(BattlePassSegmentData data, RewardState state, bool unlockedPremium, bool small = false)
		{
			_hasRuntimeData = true;
			_previewRefresh?.Pause();
			ApplyData(data, state, unlockedPremium, small);
		}

		private void ApplyData(BattlePassSegmentData data, RewardState state, bool unlockedPremium, bool small)
		{
			SegmentData = data;
			_uxmlRewardState = state;
			_uxmlUnlockedPremium = unlockedPremium;
			SetStatusVisuals();
			_rewardRoot.RemoveModifiers();
			if (small)
			{
				_rewardRoot.AddToClassList(UssRootModifierSmall);
			}
		}

		private void SetStatusVisuals()
		{
			DrawIcon();
			_rewardRoot.RemoveModifiers();

			var locked = SegmentData.PassType == PassType.Paid && !UnlockedPremium;
			_lock.SetDisplay(locked);
			_readyToClaimOutline.SetDisplay(RewardState == RewardState.Claimable && !locked);
			_claimBubble.SetDisplay(RewardState == RewardState.Claimable && !locked);
			_blocker.SetDisplay(RewardState == RewardState.Claimed || locked);
			_claimedCheckmark.SetDisplay(RewardState == RewardState.Claimed);
			_button.RemoveModifiers();
			var isClaimable = RewardState == RewardState.Claimable && !locked;
			_readyToClaimOutline.SetDisplay(isClaimable);
			_claimBubble.SetDisplay(isClaimable);
			if (isClaimable)
			{
				_claimBubble.AnimatePingRepeating(1.3f, 500);
			}

			if (RewardState == RewardState.Claimed) _button.AddToClassList(UssClaimedButton);
			else if (RewardState == RewardState.Claimable) _button.AddToClassList(UssClaimableButton);
			else if (SegmentData.PassType == PassType.Free) _button.AddToClassList(UssUnclaimedFree);
			else if (SegmentData.PassType == PassType.Paid) _button.AddToClassList(UssUnclaimedPaid);
		}

		private void DrawIcon()
		{
			var reward = SegmentData.RewardConfig;
			if (reward.GameId == GameId.Random)
			{
				return;
			}

			var item = ItemFactory.Legacy(new LegacyItemData()
			{
				RewardId = reward.GameId,
				Value = reward.Amount
			});
			var itemView = item.GetViewModel();
			itemView.DrawIcon(_rewardImage);
			_type.text = itemView.ItemTypeDisplayName.ToUpperInvariant();
			// Dirty hack to only show amount of currencies
			if (itemView is CurrencyItemViewModel cv)
			{
				_title.text = cv.Amount.ToString();
				return;
			}

			_title.text = itemView.GameId.IsInGroup(GameIdGroup.ProfilePicture) ? "" : itemView.DisplayName.ToUpperInvariant();
		}
	}

	/// <summary>
	/// This class holds the data used to update BattlePassSegmentViews
	/// </summary>
	public class BattlePassSegmentData
	{
		public uint SegmentLevel;
		public uint LevelAfterClaiming;
		public uint PointsAfterClaiming;
		public EquipmentRewardConfig RewardConfig;
		public PassType PassType;
		public uint LevelNeededToClaim => SegmentLevel + 1;
	}

	public enum RewardState
	{
		Claimed,
		Claimable,
		NotReached
	}
}
